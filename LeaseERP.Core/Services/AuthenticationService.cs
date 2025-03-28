using LeaseERP.Core.Interfaces;
using LeaseERP.Shared.DTOs;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LeaseERP.Core.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IDataService _dataService;
        private readonly IEncryptionService _encryptionService;
        private readonly IConfiguration _configuration;
        private readonly string _spUser;

        public AuthenticationService(
            IDataService dataService,
            IEncryptionService encryptionService,
            IConfiguration configuration)
        {
            _dataService = dataService;
            _encryptionService = encryptionService;
            _configuration = configuration;
            _spUser = _configuration["StoredProcedures:user"];
        }

        public async Task<LoginResponse> AuthenticateAsync(LoginRequest request)
        {
            try
            {
                // Encrypt the password
                string encryptedPassword = _encryptionService.Encrypt(request.Password);

                // Prepare parameters
                var parameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.Login },
                    { "@UserName", request.Username },
                    { "@UserPassword", encryptedPassword }
                };

                // Execute the stored procedure
                var result = await _dataService.ExecuteStoredProcedureAsync(_spUser, parameters);

                // Check for successful login
                var statusTable = result.Tables[0];
                if (statusTable.Rows.Count > 0)//&& Convert.ToInt32(statusTable.Rows[0]["Status"]) == 1
                {
                    // User data should be in the first table
                    var userTable = result.Tables[0];
                    if (userTable.Rows.Count > 0)
                    {
                        var user = new UserDTO
                        {
                            UserID = Convert.ToInt64(userTable.Rows[0]["UserID"]),
                            CompID = Convert.ToInt64(userTable.Rows[0]["CompID"]),
                            UserName = userTable.Rows[0]["UserName"].ToString(),
                            UserFullName = userTable.Rows[0]["UserFullName"].ToString(),
                            PhoneNo = userTable.Rows[0]["PhoneNo"]?.ToString() ?? string.Empty,
                            EmailID = userTable.Rows[0]["EmailID"]?.ToString() ?? string.Empty,
                            IsActive = Convert.ToBoolean(userTable.Rows[0]["IsActive"]),
                            CompanyName = userTable.Rows[0]["CompanyName"]?.ToString() ?? string.Empty
                        };

                        // Handle nullable fields
                        if (userTable.Rows[0]["DepartmentID"] != DBNull.Value)
                            user.DepartmentID = Convert.ToInt64(userTable.Rows[0]["DepartmentID"]);

                        if (userTable.Rows[0]["RoleID"] != DBNull.Value)
                            user.RoleID = Convert.ToInt64(userTable.Rows[0]["RoleID"]);

                        user.DepartmentName = userTable.Rows[0]["DepartmentName"]?.ToString() ?? string.Empty;
                        user.RoleName = userTable.Rows[0]["RoleName"]?.ToString() ?? string.Empty;

                        // Generate JWT token
                        string token = GenerateJwtToken(user);

                        // Generate refresh token
                        string refreshToken = GenerateRefreshToken(user);

                        // Calculate expiration
                        int expiryHours = int.Parse(_configuration["JwtSettings:ExpiryHours"] ?? "8");
                        DateTime expiration = DateTime.UtcNow.AddHours(expiryHours);

                        return new LoginResponse
                        {
                            Success = true,
                            Message = "Login successful",
                            Token = token,
                            RefreshToken = refreshToken,
                            Expiration = expiration,
                            User = user
                        };
                    }
                }

                // If we got here, login failed
                string errorMessage = statusTable.Rows.Count > 0
                    ? statusTable.Rows[0]["Message"].ToString()
                    : "Login failed";

                return new LoginResponse
                {
                    Success = false,
                    Message = errorMessage
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Authentication error: {ex.Message}"
                };
            }
        }

        public async Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request)
        {
            try
            {
                // Validate the existing token
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"]);

                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false // Don't validate lifetime as token might be expired
                };

                ClaimsPrincipal principal;
                try
                {
                    principal = tokenHandler.ValidateToken(request.refreshToken, tokenValidationParameters, out var validatedToken);

                    // Additional validation
                    if (validatedToken is not JwtSecurityToken jwtToken ||
                        !jwtToken.Header.Alg.Equals("HS256", StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new SecurityTokenException("Invalid token");
                    }
                }
                catch
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid token"
                    };
                }

                // Extract user ID from claims
                var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid token: missing user ID claim"
                    };
                }

                long userId = Convert.ToInt64(userIdClaim.Value);

                // Get fresh user data
                var parameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.FetchById },
                    { "@UserID", userId }
                };

                var result = await _dataService.ExecuteStoredProcedureAsync(_spUser, parameters);

                // Check if user was found
                var statusTable = result.Tables[1]; // Status is in the second table for FetchById
                if (statusTable.Rows.Count > 0 && Convert.ToInt32(statusTable.Rows[0]["Status"]) == 1)
                {
                    var userTable = result.Tables[0];
                    if (userTable.Rows.Count > 0)
                    {
                        var user = new UserDTO
                        {
                            UserID = Convert.ToInt64(userTable.Rows[0]["UserID"]),
                            CompID = Convert.ToInt64(userTable.Rows[0]["CompID"]),
                            UserName = userTable.Rows[0]["UserName"].ToString(),
                            UserFullName = userTable.Rows[0]["UserFullName"].ToString(),
                            PhoneNo = userTable.Rows[0]["PhoneNo"]?.ToString() ?? string.Empty,
                            EmailID = userTable.Rows[0]["EmailID"]?.ToString() ?? string.Empty,
                            IsActive = Convert.ToBoolean(userTable.Rows[0]["IsActive"]),
                            CompanyName = userTable.Rows[0]["CompanyName"]?.ToString() ?? string.Empty
                        };

                        // Handle nullable fields
                        if (userTable.Rows[0]["DepartmentID"] != DBNull.Value)
                            user.DepartmentID = Convert.ToInt64(userTable.Rows[0]["DepartmentID"]);

                        if (userTable.Rows[0]["RoleID"] != DBNull.Value)
                            user.RoleID = Convert.ToInt64(userTable.Rows[0]["RoleID"]);

                        user.DepartmentName = userTable.Rows[0]["DepartmentName"]?.ToString() ?? string.Empty;
                        user.RoleName = userTable.Rows[0]["RoleName"]?.ToString() ?? string.Empty;

                        // Check if user is still active
                        if (!user.IsActive)
                        {
                            return new LoginResponse
                            {
                                Success = false,
                                Message = "User account is inactive"
                            };
                        }

                        // Generate new JWT token
                        string token = GenerateJwtToken(user);

                        // Generate new refresh token
                        string refreshToken = GenerateRefreshToken(user);

                        // Calculate expiration
                        int expiryHours = int.Parse(_configuration["JwtSettings:ExpiryHours"] ?? "8");
                        DateTime expiration = DateTime.UtcNow.AddHours(expiryHours);

                        return new LoginResponse
                        {
                            Success = true,
                            Message = "Token refreshed successfully",
                            Token = token,
                            RefreshToken = refreshToken,
                            Expiration = expiration,
                            User = user
                        };
                    }
                }

                return new LoginResponse
                {
                    Success = false,
                    Message = "User not found or inactive"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"Token refresh error: {ex.Message}"
                };
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                // First, verify current password
                var verifyParameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.FetchById },
                    { "@UserID", request.UserID }
                };

                var verifyResult = await _dataService.ExecuteStoredProcedureAsync(_spUser, verifyParameters);

                if (verifyResult.Tables[0].Rows.Count == 0)
                {
                    return false; // User not found
                }

                var currentEncryptedPassword = verifyResult.Tables[0].Rows[0]["UserPassword"].ToString();
                var decryptedCurrentPassword = _encryptionService.Decrypt(currentEncryptedPassword);

                if (decryptedCurrentPassword != request.CurrentPassword)
                {
                    return false; // Current password doesn't match
                }

                // Now update with new password
                var newEncryptedPassword = _encryptionService.Encrypt(request.NewPassword);

                var updateParameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.Update },
                    { "@UserID", request.UserID },
                    { "@UserPassword", newEncryptedPassword },
                    { "@CurrentUserID", request.UserID },
                    { "@CurrentUserName", request.ActionBy }
                };

                var updateResult = await _dataService.ExecuteStoredProcedureAsync(_spUser, updateParameters);

                var statusTable = updateResult.Tables[0];
                return statusTable.Rows.Count > 0 && Convert.ToInt32(statusTable.Rows[0]["Status"]) == 1;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                // Find user by username or email
                var findParameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.Search },
                    { "@SearchText", request.Username }
                };

                var findResult = await _dataService.ExecuteStoredProcedureAsync(_spUser, findParameters);

                if (findResult.Tables[0].Rows.Count == 0)
                {
                    return false; // User not found
                }

                // In a real implementation:
                // 1. Generate temp password
                // 2. Encrypt it
                // 3. Update user record
                // 4. Send email

                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GenerateJwtToken(UserDTO user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"]);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim("fullName", user.UserFullName),
                new Claim("companyId", user.CompID.ToString()),
                new Claim("email", user.EmailID ?? string.Empty)
            };

            // Add role if present
            if (user.RoleID.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.Role, user.RoleID.Value.ToString()));
                claims.Add(new Claim("roleName", user.RoleName ?? string.Empty));
            }

            // Add department if present
            if (user.DepartmentID.HasValue)
            {
                claims.Add(new Claim("departmentId", user.DepartmentID.Value.ToString()));
                claims.Add(new Claim("departmentName", user.DepartmentName ?? string.Empty));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(int.Parse(_configuration["JwtSettings:ExpiryHours"] ?? "8")),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a JWT-formatted refresh token
        /// </summary>
        /// <param name="user">The user for whom to generate the refresh token</param>
        /// <returns>JWT-formatted refresh token string</returns>
        private string GenerateRefreshToken(UserDTO user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["JwtSettings:Secret"]);

            // Create limited claims for the refresh token - only include essential information
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
                new Claim("tokenType", "refresh") // Mark this as a refresh token
            };

            // Set longer expiration for refresh token
            // Typically refresh tokens live longer than access tokens
            var refreshTokenExpiry = int.Parse(_configuration["JwtSettings:RefreshExpiryHours"] ?? "168"); // Default 7 days

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(refreshTokenExpiry),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JwtSettings:Issuer"],
                Audience = _configuration["JwtSettings:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}