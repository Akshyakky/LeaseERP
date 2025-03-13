using System.Data;

namespace LeaseERP.Core.Interfaces
{
    public interface IDataService
    {
        Task<DataSet> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters);
        Task<object> ExecuteScalarAsync(string procedureName, Dictionary<string, object> parameters);
        Task<int> ExecuteNonQueryAsync(string procedureName, Dictionary<string, object> parameters);
    }
}