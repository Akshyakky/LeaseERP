using LeaseERP.Core.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace LeaseERP.Core.Services
{
    public class DataService : IDataService
    {
        private readonly IDbConnection _connection;
        private readonly bool _showDetailedErrors;

        public DataService(IDbConnection connection, IConfiguration configuration)
        {
            _connection = connection;
            _showDetailedErrors = configuration["AppSettings:ShowDetailedErrors"] == "true";
        }

        public async Task<DataSet> ExecuteStoredProcedureAsync(string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = procedureName;
                command.CommandType = CommandType.StoredProcedure;

                // Add parameters
                foreach (var param in parameters)
                {
                    var dbParam = (SqlParameter)((SqlCommand)command).Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

                    // Determine parameter direction if needed
                    // if (param.Key.StartsWith("@out_"))
                    // {
                    //     dbParam.Direction = ParameterDirection.Output;
                    // }
                }

                var dataSet = new DataSet();
                using var adapter = new SqlDataAdapter((SqlCommand)command);
                await Task.Run(() => adapter.Fill(dataSet));

                return dataSet;
            }
            catch (Exception ex)
            {
                throw new Exception(_showDetailedErrors
                    ? $"Error executing stored procedure {procedureName}: {ex.Message}"
                    : "An error occurred while accessing the database.");
            }
        }

        public async Task<object> ExecuteScalarAsync(string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = procedureName;
                command.CommandType = CommandType.StoredProcedure;

                // Add parameters
                foreach (var param in parameters)
                {
                    ((SqlCommand)command).Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                return await ((SqlCommand)command).ExecuteScalarAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(_showDetailedErrors
                    ? $"Error executing scalar query {procedureName}: {ex.Message}"
                    : "An error occurred while accessing the database.");
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using var command = _connection.CreateCommand();
                command.CommandText = procedureName;
                command.CommandType = CommandType.StoredProcedure;

                // Add parameters
                foreach (var param in parameters)
                {
                    ((SqlCommand)command).Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                }

                return await ((SqlCommand)command).ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(_showDetailedErrors
                    ? $"Error executing non-query {procedureName}: {ex.Message}"
                    : "An error occurred while accessing the database.");
            }
        }
    }
}