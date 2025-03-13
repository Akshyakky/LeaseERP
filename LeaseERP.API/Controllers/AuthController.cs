using LeaseERP.Core.Interfaces;
using LeaseERP.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthenticationService authService,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = await _authService.AuthenticateAsync(request);
                if (result.Success)
                {
                    _logger.LogInformation("User {UserName} logged in successfully.", request.Username);
                    return Ok(result);
                }

                _logger.LogWarning("Failed login attempt for user {UserName}.", request.Username);
                return Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user {UserName}.", request.Username);
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred during login."
                });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(request);
                if (result.Success)
                {
                    return Ok(result);
                }

                return Unauthorized(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token.");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "An error occurred while refreshing token."
                });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null || Convert.ToInt64(userId) != request.UserID)
                {
                    return Forbid();
                }

                var result = await _authService.ChangePasswordAsync(request);
                if (result)
                {
                    _logger.LogInformation("Password changed successfully for user ID {UserID}.", request.UserID);
                    return Ok(new { Success = true, Message = "Password changed successfully." });
                }

                _logger.LogWarning("Failed password change attempt for user ID {UserID}.", request.UserID);
                return BadRequest(new { Success = false, Message = "Failed to change password. Please check your current password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user ID {UserID}.", request.UserID);
                return StatusCode(500, new { Success = false, Message = "An error occurred while changing password." });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request);
                if (result)
                {
                    _logger.LogInformation("Password reset initiated for username {Username}.", request.Username);
                    return Ok(new { Success = true, Message = "If an account exists with those details, a password reset email has been sent." });
                }

                // We return the same message even if user is not found for security reasons
                _logger.LogWarning("Password reset attempted for non-existent username {Username}.", request.Username);
                return Ok(new { Success = true, Message = "If an account exists with those details, a password reset email has been sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for username {Username}.", request.Username);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }
    }
}