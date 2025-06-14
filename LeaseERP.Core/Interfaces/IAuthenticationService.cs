using LeaseERP.Shared.DTOs;

namespace LeaseERP.Core.Interfaces
{
    public interface IAuthenticationService
    {
        Task<LoginResponse> AuthenticateAsync(LoginRequest request);
        Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        string GenerateJwtToken(UserDTO user);
        Task<LoginResponse> SwitchCompanyAsync(SwitchCompanyRequest request);
    }
}