using BCrypt.Net;
using CaseBridge_Users.Data;
using CaseBridge_Users.Models;
using Dapper;
using Microsoft.Identity.Client;
using System.Data;
using System.Security;
using System.Text.RegularExpressions;

namespace CaseBridge_Users.Repositories
{
    public class UserRepository
    {
        private readonly DapperContext _context;

        public UserRepository(DapperContext dapperContext)
        {
            _context = dapperContext;
        }

        public async Task<bool> RegisterLawyerAsync(User user, LawyerProfile profile, string password, string verificationToken)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var userSql = @"INSERT INTO Users (Email, PasswordHash, FullName, UserType) 
                              VALUES (@Email, @PasswordHash, @FullName, @UserType);
                              SELECT CAST(SCOPE_IDENTITY() as int);";
                user.Id=await connection.ExecuteScalarAsync<int>(userSql, user, transaction);

                var securitySql = @"INSERT INTO UserSecurity (UserId, IsEmailVerified, EmailVerificationToken, AccessFailedCount) 
                            VALUES (@UserId, 0, @MyToken, 0)";

                await connection.ExecuteAsync(securitySql, new
                {
                    UserId = user.Id,
                    MyToken = verificationToken
                }, transaction);

                var profileSql = @"INSERT INTO LawyerProfiles (UserId, EnrollmentNumber, Specialization, SeniorLawyerId, FirmBio) 
                           VALUES (@UserId, @EnrollmentNumber, @Specialization, @SeniorLawyerId, @FirmBio)";

                profile.UserId = user.Id;
                await connection.ExecuteAsync(profileSql, profile, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"DB ERROR: {ex.Message}"); // See if there is a hidden SQL error
                throw;
            }
        }
        public async Task<bool> RegisterClientAsync(User user, string password, string verificationToken)
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var userSql = @"INSERT INTO Users (Email, PasswordHash, FullName, UserType) 
                                VALUES (@Email, @PasswordHash, @FullName, @UserType);
                                SELECT CAST(SCOPE_IDENTITY() as int);";

                user.Id = await connection.ExecuteScalarAsync<int>(userSql, user, transaction);

                var securitySql = @"INSERT INTO UserSecurity (UserId, IsEmailVerified, EmailVerificationToken, AccessFailedCount) 
                                    VALUES (@Id, 0, @Token, 0)";

                await connection.ExecuteAsync(securitySql, new { Id = user.Id, Token = verificationToken }, transaction);

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"DB Error: {ex.Message}");
                return false;
            }
        }

        // 3. GET BOTH (For Login)
        public async Task<(User?, UserSecurity?)> GetUserWithSecurityAsync(string email)
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT u.*, s.* FROM Users u 
                        JOIN UserSecurity s ON u.Id = s.UserId 
                        WHERE u.Email = @Email";

            var result = await connection.QueryAsync<User, UserSecurity, (User, UserSecurity)>(
                sql,
                (user, security) => (user, security),
                new { Email = email },
                splitOn: "UserId"
            );

            return result.FirstOrDefault();
        }

       // Fetch User and Profile together(needed for Senior verification)
        public async Task<(User?, LawyerProfile?)> GetUserAndProfileAsync(int userId)
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT u.*, p.* FROM Users u 
                        LEFT JOIN LawyerProfiles p ON u.Id = p.UserId 
                        WHERE u.Id = @UserId";

            var result = await connection.QueryAsync<User, LawyerProfile, (User, LawyerProfile)>(
                sql,
                (u, p) => (u, p),
                new { UserId = userId },
                splitOn: "Id"
            );

            return result.FirstOrDefault();
        }

        public async Task UpdateRefreshTokenAsync(int userId, string token, DateTime expiry)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE UserSecurity 
                        SET RefreshToken = @Token, 
                            RefreshTokenExpiryTime = @Expiry 
                        WHERE UserId = @UserId";

            await connection.ExecuteAsync(sql, new { UserId = userId, Token = token, Expiry = expiry });
        }

        public async Task<(User?, UserSecurity?)> GetUserByRefreshTokenAsync(string refreshToken)
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT u.*, s.* FROM Users u 
                        JOIN UserSecurity s ON u.Id = s.UserId 
                        WHERE s.RefreshToken = @RefreshToken";

            var result = await connection.QueryAsync<User, UserSecurity, (User, UserSecurity)>(
                sql,
                (user, security) => (user, security),
                new { RefreshToken = refreshToken },
                splitOn: "UserId"
            );

            return result.FirstOrDefault();
        }

        //Bulk Security Update (For Forgot Password/Verification)
        public async Task UpdateSecurityStatusAsync(UserSecurity security)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE UserSecurity 
                        SET IsEmailVerified = @IsEmailVerified, 
                            PasswordResetToken = @PasswordResetToken, 
                            ResetTokenExpiry = @ResetTokenExpiry,
                            AccessFailedCount = @AccessFailedCount,
                            LockoutEnd = @LockoutEnd
                        WHERE UserId = @UserId";
            await connection.ExecuteAsync(sql, security);
        }

        public async Task UpdateUserAsync(User user)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE Users 
                SET FullName = @FullName, 
                    GoogleId = @GoogleId,
                    PasswordHash = @PasswordHash -- ADD THIS LINE
                WHERE Id = @Id";
            await connection.ExecuteAsync(sql, user);
        }
    }
}