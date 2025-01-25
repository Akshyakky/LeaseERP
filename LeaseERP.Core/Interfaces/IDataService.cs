using LeaseERP.Shared.DTOs;

namespace LeaseERP.Core.Interfaces
{
    public interface IDataService
    {
        Task<ApiResponse<dynamic>> ExecuteStoredProcedureAsync(string procedureName, BaseRequest request);
    }
}
