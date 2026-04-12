using BCrypt.Net;
using CaseBridge_Users.Data;
using CaseBridge_Users.Models;
using Dapper;
using Microsoft.AspNetCore.Identity;
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
            var PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                var userSql = @"INSERT INTO Users (Email, FullName, UserType) 
                              VALUES (@Email, @FullName, @UserType);
                              SELECT CAST(SCOPE_IDENTITY() as int);";
                user.Id = await connection.ExecuteScalarAsync<int>(userSql, user, transaction);

                var securitySql = @"INSERT INTO UserSecurity (UserId, PasswordHash, IsEmailVerified, VerificationToken, FailedLoginAttempts) 
                                    VALUES (@UserId, @Hash, 0, @Token, 0)";

                await connection.ExecuteAsync(securitySql, new
                {
                    UserId = user.Id,
                    Hash = PasswordHash,
                    Token = verificationToken
                }, transaction);

                var profileSql = @"INSERT INTO LawyerProfiles (UserId, EnrollmentNumber, Specialization, SeniorLawyerId, FirmBio, IsVerified) 
                                   VALUES (@UserId, @EnrollmentNumber, @Specialization, @SeniorLawyerId, @FirmBio, 0)";

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
        public async Task<bool> RegisterClientAsync(User user, ClientProfile profile, string password, string verificationToken)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = _context.CreateConnection();
            connection.Open();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Insert into Users
                var userSql = @"INSERT INTO Users (Email, FullName, UserType) 
                        VALUES (@Email, @FullName, 'Client');
                        SELECT CAST(SCOPE_IDENTITY() as int);";
                user.Id = await connection.ExecuteScalarAsync<int>(userSql, user, transaction);

                // Insert into UserSecurity
                var securitySql = @"INSERT INTO UserSecurity (UserId, PasswordHash, IsEmailVerified, VerificationToken, FailedLoginAttempts) 
                            VALUES (@UserId, @Hash, 0, @Token, 0)";
                await connection.ExecuteAsync(securitySql, new { UserId = user.Id, Hash = passwordHash, Token = verificationToken }, transaction);

                // Insert into ClientProfiles
                var profileSql = @"INSERT INTO ClientProfiles (UserId, PhoneNumber, Address, ClientType) 
                           VALUES (@UserId, @PhoneNumber, @Address, @ClientType)";
                profile.UserId = user.Id;
                await connection.ExecuteAsync(profileSql, profile, transaction);

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
                    VerificationToken = @VerificationToken,
                    PasswordHash = @PasswordHash,
                    PasswordResetToken = @PasswordResetToken, 
                    ResetTokenExpiry = @ResetTokenExpiry,
                    FailedLoginAttempts = @FailedLoginAttempts, 
                    LockoutEnd = @LockoutEnd,
                    IsLocked = @IsLocked
                WHERE UserId = @UserId";
            await connection.ExecuteAsync(sql, security);
        }

        public async Task UpdateUserAsync(User user)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE Users 
                SET FullName = @FullName, 
                    GoogleId = @GoogleId
                WHERE Id = @Id";
            await connection.ExecuteAsync(sql, user);
        }

        //List lawyers needing verification
        public async Task<IEnumerable<dynamic>> GetUnverifiedLawyersAsync()
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT u.Id, u.FullName, u.Email, p.EnrollmentNumber 
                FROM Users u
                JOIN LawyerProfiles p ON u.Id = p.UserId
                WHERE p.IsVerified = 0"; // Assuming you added IsVerified column

            return await connection.QueryAsync(sql);
        }

        public async Task<bool> UpdateLawyerVerificationAsync(int userId, bool status)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE LawyerProfiles SET IsVerified = @Status WHERE UserId = @UserId";
            var rows = await connection.ExecuteAsync(sql, new { UserId = userId, Status = status });
            return rows > 0;
        }


        // --- SENIOR FIRM MANAGEMENT METHODS ---

        // Fetch all juniors linked to a specific Senior ID
        public async Task<IEnumerable<dynamic>> GetFirmAssociatesAsync(int seniorId)
        {
            using var connection = _context.CreateConnection();

            // We join Users and Profiles to get a clean summary of the Junior
            var sql = @"SELECT 
                            u.Id, 
                            u.Email, 
                            u.FullName, 
                            p.EnrollmentNumber, 
                            p.Specialization 
                        FROM Users u 
                        JOIN LawyerProfiles p ON u.Id = p.UserId 
                        WHERE p.SeniorLawyerId = @SeniorId";

            return await connection.QueryAsync(sql, new { SeniorId = seniorId });
        }

        // Update the Firm's Bio
        public async Task UpdateFirmBioAsync(int userId, string firmBio)
        {
            using var connection = _context.CreateConnection();
            var sql = @"UPDATE LawyerProfiles 
                        SET FirmBio = @FirmBio 
                        WHERE UserId = @UserId";

            await connection.ExecuteAsync(sql, new { UserId = userId, FirmBio = firmBio });
        }



        // --- Junior FIRM MANAGEMENT METHODS ---

        public async Task<dynamic?> GetSeniorForJuniorAsync(int juniorId)
        {
            using var connection = _context.CreateConnection();
            var sql = @"SELECT senior.FullName, senior.Email, p_senior.Specialization, p_senior.FirmBio
                FROM Users junior
                JOIN LawyerProfiles p_junior ON junior.Id = p_junior.UserId
                JOIN Users senior ON p_junior.SeniorLawyerId = senior.Id
                JOIN LawyerProfiles p_senior ON senior.Id = p_senior.UserId
                WHERE junior.Id = @JuniorId";

            return await connection.QueryFirstOrDefaultAsync(sql, new { JuniorId = juniorId });
        }

    }
}