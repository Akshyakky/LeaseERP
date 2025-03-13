namespace LeaseERP.Shared.Enums
{
    public enum OperationType
    {
        Insert = 1,
        Update = 2,
        FetchAll = 3,
        FetchById = 4,
        Delete = 5,
        Search = 6,

        // Special operations
        GetByCompany = 7,
        ChangeStatus = 8,

        // Authentication operations
        Login = 7,
        Logout = 8,
        RefreshToken = 9,
        ChangePassword = 10,
        ResetPassword = 11,

        // Menu operations
        InsertSubmenu = 7,
        UpdateSubmenu = 8,
        FetchSubmenus = 9,
        FetchSubmenuById = 10,
        DeleteSubmenu = 11,
        GetMenuWithSubmenus = 12,
        GetAuthorizedMenus = 13,

        // Role operations
        GetUsersByRole = 7,
        CloneRole = 8,
        GetRolePermissions = 9,

        // Department operations
        GetUsersByDepartment = 7,
        GetDepartmentStats = 8,

        // Customer operations
        GetByIdentityNo = 7,
        GetByTaxRegNo = 8,
        CheckIdentityNoExists = 9
    }
}
