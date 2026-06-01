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
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = _context.CreateConnection();
            try
            {
                var parameters = new
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    UserType = user.UserType,
                    PasswordHash = passwordHash,
                    VerificationToken = verificationToken,
                    EnrollmentNumber = profile.EnrollmentNumber,
                    Specialization = profile.Specialization,
                    SeniorLawyerId = profile.SeniorLawyerId,
                    FirmBio = profile.FirmBio
                };

                user.Id = await connection.ExecuteScalarAsync<int>(
                    "sp_RegisterLawyer",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                
                profile.UserId = user.Id;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB ERROR: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> RegisterClientAsync(User user, ClientProfile profile, string password, string verificationToken)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            using var connection = _context.CreateConnection();
            try
            {
                var parameters = new
                {
                    Email = user.Email,
                    FullName = user.FullName,
                    PasswordHash = passwordHash,
                    VerificationToken = verificationToken,
                    PhoneNumber = profile.PhoneNumber,
                    Address = profile.Address,
                    ClientType = profile.ClientType
                };

                user.Id = await connection.ExecuteScalarAsync<int>(
                    "sp_RegisterClient",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                profile.UserId = user.Id;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB Error: {ex.Message}");
                return false;
            }
        }

        public async Task<(User?, UserSecurity?)> GetUserWithSecurityAsync(string email)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<User, UserSecurity, (User, UserSecurity)>(
                "sp_GetUserWithSecurity",
                (user, security) => (user, security),
                new { Email = email },
                splitOn: "UserId",
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        public async Task<(User?, LawyerProfile?)> GetUserAndProfileAsync(int userId)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<User, LawyerProfile, (User, LawyerProfile)>(
                "sp_GetUserAndProfile",
                (u, p) => (u, p),
                new { UserId = userId },
                splitOn: "UserId",
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        public async Task UpdateRefreshTokenAsync(int userId, string token, DateTime expiry)
        {
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(
                "sp_UpdateRefreshToken",
                new { UserId = userId, Token = token, Expiry = expiry },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<(User?, UserSecurity?)> GetUserByRefreshTokenAsync(string refreshToken)
        {
            using var connection = _context.CreateConnection();
            var result = await connection.QueryAsync<User, UserSecurity, (User, UserSecurity)>(
                "sp_GetUserByRefreshToken",
                (user, security) => (user, security),
                new { RefreshToken = refreshToken },
                splitOn: "UserId",
                commandType: CommandType.StoredProcedure
            );

            return result.FirstOrDefault();
        }

        public async Task UpdateSecurityStatusAsync(UserSecurity security)
        {
            using var connection = _context.CreateConnection();
            
            var parameters = new
            {
                UserId = security.UserId,
                IsEmailVerified = security.IsEmailVerified,
                VerificationToken = security.VerificationToken,
                PasswordHash = security.PasswordHash,
                PasswordResetToken = security.PasswordResetToken,
                ResetTokenExpiry = security.ResetTokenExpiry,
                FailedLoginAttempts = security.FailedLoginAttempts,
                LockoutEnd = security.LockoutEnd,
                IsLocked = security.IsLocked
            };

            await connection.ExecuteAsync(
                "sp_UpdateSecurityStatus",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateUserAsync(User user)
        {
            using var connection = _context.CreateConnection();
            
            var parameters = new 
            {
                Id = user.Id,
                FullName = user.FullName,
                GoogleId = user.GoogleId
            };

            await connection.ExecuteAsync(
                "sp_UpdateUser",
                parameters,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<IEnumerable<dynamic>> GetUnverifiedLawyersAsync()
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync(
                "sp_GetUnverifiedLawyers",
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> UpdateLawyerVerificationAsync(int userId, bool status)
        {
            using var connection = _context.CreateConnection();
            var rows = await connection.ExecuteAsync(
                "sp_UpdateLawyerVerification",
                new { UserId = userId, Status = status },
                commandType: CommandType.StoredProcedure
            );
            return rows > 0;
        }

        public async Task<IEnumerable<dynamic>> GetFirmAssociatesAsync(int seniorId)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync(
                "sp_GetFirmAssociates",
                new { SeniorId = seniorId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task UpdateFirmBioAsync(int userId, string firmBio)
        {
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(
                "sp_UpdateFirmBio",
                new { UserId = userId, FirmBio = firmBio },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<dynamic?> GetSeniorForJuniorAsync(int juniorId)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryFirstOrDefaultAsync(
                "sp_GetSeniorForJunior",
                new { JuniorId = juniorId },
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task<bool> RemoveJuniorAssociateAsync(int seniorId, int juniorId)
        {
            using var connection = _context.CreateConnection();
            var rowsAffected = await connection.ExecuteAsync(
                "sp_RemoveJuniorAssociate",
                new { SeniorId = seniorId, JuniorId = juniorId },
                commandType: CommandType.StoredProcedure
            );

            return rowsAffected > 0;
        }
    }
}