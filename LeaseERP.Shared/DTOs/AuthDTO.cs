namespace LeaseERP.Shared.DTOs
{
    public class LoginRequest : BaseRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UserDTO? User { get; set; }
        public List<CompanyDTO> Companies { get; set; } = new List<CompanyDTO>();
    }

    public class CompanyDTO
    {
        public long CompanyID { get; set; }
        public string CompanyName { get; set; }
        public bool IsDefault { get; set; }
    }

    public class SwitchCompanyRequest
    {
        public long UserID { get; set; }
        public long CompanyID { get; set; }
    }

    public class UserDTO
    {
        public long UserID { get; set; }
        public long CompID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserFullName { get; set; } = string.Empty;
        public string PhoneNo { get; set; } = string.Empty;
        public string EmailID { get; set; } = string.Empty;
        public long? DepartmentID { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public long? RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? UserImage { get; set; }
    }

    public class ChangePasswordRequest : BaseRequest
    {
        public long UserID { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        public string refreshToken { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest : BaseRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}