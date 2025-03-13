using LeaseERP.Core.Interfaces;
using LeaseERP.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace LeaseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MasterController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MasterController> _logger;
        private readonly IEncryptionService _encryptionService;

        public MasterController(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<MasterController> logger,
            IEncryptionService encryptionService)
        {
            _dataService = dataService;
            _configuration = configuration;
            _logger = logger;
            _encryptionService = encryptionService;
        }

        [HttpPost("{entityType}")]
        public async Task<IActionResult> ExecuteOperation(string entityType, [FromBody] BaseRequest request)
        {
            try
            {
                // Check if the entity type is supported in configuration
                string configKey = $"StoredProcedures:{entityType.ToLower()}";
                string spName = _configuration[configKey];

                if (string.IsNullOrEmpty(spName))
                {
                    return BadRequest(new { Success = false, Message = $"Entity type '{entityType}' is not supported." });
                }

                _logger.LogInformation("Executing {EntityType} operation with mode {Mode}", entityType, request.Mode);
                return await ExecuteStoredProcedure(spName, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {EntityType} operations with mode {Mode}", entityType, request.Mode);
                return StatusCode(500, new { Success = false, Message = "An error occurred while processing your request." });
            }
        }

        #region Helper Methods

        private async Task<IActionResult> ExecuteStoredProcedure(string spName, BaseRequest request)
        {
            if (string.IsNullOrEmpty(spName))
            {
                return BadRequest(new { Success = false, Message = "Stored procedure not configured." });
            }

            // Convert the mode enum to int
            var parameters = new Dictionary<string, object>
            {
                { "@Mode", (int)request.Mode }
            };

            // Add user tracking parameters
            if (!string.IsNullOrEmpty(request.ActionBy))
            {
                parameters.Add("@CurrentUserName", request.ActionBy);

                // Try to get UserID from claims
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
                {
                    parameters.Add("@CurrentUserID", userId);
                }
            }

            // Add all other parameters from the dictionary
            if (request.Parameters != null)
            {
                foreach (var param in request.Parameters)
                {
                    // Skip parameters that are already added
                    if (!parameters.ContainsKey($"@{param.Key}"))
                    {
                        // Convert JsonElement to appropriate .NET type
                        var convertedValue = ConvertJsonElementToNativeType(param.Value);

                        // Encrypt UserPassword parameter
                        if (param.Key.Equals("UserPassword", StringComparison.OrdinalIgnoreCase) &&
                            convertedValue != null &&
                            convertedValue != DBNull.Value)
                        {
                            // Encrypt the password
                            string passwordStr = convertedValue.ToString();
                            if (!string.IsNullOrEmpty(passwordStr))
                            {
                                convertedValue = _encryptionService.Encrypt(passwordStr);
                                _logger.LogInformation("Password encrypted for parameter UserPassword");
                            }
                        }

                        parameters.Add($"@{param.Key}", convertedValue);
                    }
                }
            }

            var result = await _dataService.ExecuteStoredProcedureAsync(spName, parameters);
            return ProcessDataSet(result);
        }

        private object ConvertJsonElementToNativeType(object value)
        {
            if (value is System.Text.Json.JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case System.Text.Json.JsonValueKind.String:
                        return element.GetString();
                    case System.Text.Json.JsonValueKind.Number:
                        // Try to parse as integer first, then as double
                        if (element.TryGetInt64(out long longValue))
                            return longValue;
                        if (element.TryGetInt32(out int intValue))
                            return intValue;
                        if (element.TryGetDouble(out double doubleValue))
                            return doubleValue;
                        return element.GetRawText();
                    case System.Text.Json.JsonValueKind.True:
                        return true;
                    case System.Text.Json.JsonValueKind.False:
                        return false;
                    case System.Text.Json.JsonValueKind.Null:
                        return DBNull.Value;
                    case System.Text.Json.JsonValueKind.Array:
                    case System.Text.Json.JsonValueKind.Object:
                        // For complex types, convert to string
                        return element.GetRawText();
                    default:
                        return null;
                }
            }
            return value ?? DBNull.Value;
        }

        private IActionResult ProcessDataSet(DataSet ds)
        {
            if (ds == null || ds.Tables.Count == 0)
            {
                return BadRequest(new { Success = false, Message = "No results returned." });
            }

            // Check for status message in the last table
            DataTable statusTable = ds.Tables[ds.Tables.Count - 1];
            bool success = false;
            string message = "Operation completed";

            if (statusTable.Columns.Contains("Status") && statusTable.Columns.Contains("Message") &&
                statusTable.Rows.Count > 0)
            {
                success = Convert.ToInt32(statusTable.Rows[0]["Status"]) == 1;
                message = statusTable.Rows[0]["Message"].ToString();

                // If there's only one table and it's the status table
                if (ds.Tables.Count == 1)
                {
                    return Ok(new { Success = success, Message = message });
                }
            }

            // Process data tables
            var result = new Dictionary<string, object>();

            // Add success and message
            result["success"] = success;
            result["message"] = message;

            // Process data tables (excluding the last one if it's a status table)
            int tablesToProcess = statusTable.Columns.Contains("Status") ? ds.Tables.Count - 1 : ds.Tables.Count;

            if (tablesToProcess == 1)
            {
                // Single data table, return as "data"
                result["data"] = DataTableToList(ds.Tables[0]);
            }
            else
            {
                // Multiple data tables
                for (int i = 0; i < tablesToProcess; i++)
                {
                    result[$"table{i + 1}"] = DataTableToList(ds.Tables[i]);
                }
            }

            // Check for additional data in status table
            if (statusTable.Columns.Count > 2 && statusTable.Rows.Count > 0)
            {
                var row = statusTable.Rows[0];
                for (int i = 0; i < statusTable.Columns.Count; i++)
                {
                    var columnName = statusTable.Columns[i].ColumnName;
                    if (columnName != "Status" && columnName != "Message")
                    {
                        result[char.ToLowerInvariant(columnName[0]) + columnName.Substring(1)] = row[i];
                    }
                }
            }

            return Ok(result);
        }

        private List<Dictionary<string, object>> DataTableToList(DataTable dt)
        {
            var list = new List<Dictionary<string, object>>();

            foreach (DataRow row in dt.Rows)
            {
                var dict = new Dictionary<string, object>();

                foreach (DataColumn col in dt.Columns)
                {
                    // Convert DBNull to null
                    var value = row[col];
                    dict[col.ColumnName] = value == DBNull.Value ? null : value;
                }

                list.Add(dict);
            }

            return list;
        }

        #endregion
    }
}