using Dapper;
using LeaseERP.Core.Interfaces;
using LeaseERP.Shared.DTOs;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace LeaseERP.Core.Services
{
    public class DataService : IDataService
    {
        private readonly IDbConnection _db;
        private readonly ILogger<DataService> _logger;

        private static readonly string[] DateFormats = {
            "yyyy-MM-dd",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "dd-MM-yyyy",
            "MM-dd-yyyy",
            "yyyy-MM-dd HH:mm:ss"
        };

        private static readonly HashSet<string> BinaryParameterSuffixes = new(StringComparer.OrdinalIgnoreCase)
        {
            "Image", "Photo", "Picture", "Attachment", "File", "Document", "Binary", "Blob"
        };

        private static readonly Dictionary<string, DbType> ParameterTypeHints = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ID", DbType.Int64 },
            { "Amount", DbType.Decimal },
            { "Price", DbType.Decimal },
            { "Date", DbType.DateTime2 },
            { "Time", DbType.Time },
            { "IsActive", DbType.Boolean },
            { "IsDeleted", DbType.Boolean },
            { "Status", DbType.Int32 },
            { "Code", DbType.String },
            { "Description", DbType.String },
            { "XML", DbType.Xml },
            { "JSON", DbType.String },
            { "Timestamp", DbType.DateTime2 }
        };

        public DataService(IDbConnection db, ILogger<DataService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ApiResponse<dynamic>> ExecuteStoredProcedureAsync(string procedureName, BaseRequest request)
        {
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("@Mode", (int)request.Mode, DbType.Int32);

                // Add parameters if they exist
                if (request.Parameters?.Any() == true)
                {
                    foreach (var param in request.Parameters)
                    {
                        var (value, dbType) = ConvertParameter(param.Key, param.Value);
                        parameters.Add($"@{param.Key}", value, dbType);
                    }
                }

                // Add ActionBy only for operations that require it
                if (request.Mode is OperationType.Insert or OperationType.Update or OperationType.Delete)
                {
                    parameters.Add("@ActionBy", request.ActionBy ?? "System", DbType.String);
                }

                var result = await _db.QueryAsync(procedureName, parameters,
                    commandType: CommandType.StoredProcedure);

                return new ApiResponse<dynamic>
                {
                    Success = true,
                    Data = result,
                    Message = "Operation completed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing stored procedure {ProcedureName}", procedureName);
                return new ApiResponse<dynamic>
                {
                    Success = false,
                    Message = "An error occurred while processing your request",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        private (object Value, DbType DbType) ConvertParameter(string parameterName, object value)
        {
            if (value == null)
                return (DBNull.Value, DbType.String);

            if (value is JsonElement element)
            {
                return ConvertJsonElement(parameterName, element);
            }

            return (value, InferDbType(value));
        }

        private (object Value, DbType DbType) ConvertJsonElement(string parameterName, JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => (DBNull.Value, InferDbTypeFromName(parameterName)),
                JsonValueKind.String => ConvertStringValue(parameterName, element.GetString()),
                JsonValueKind.Number => ConvertNumberValue(parameterName, element),
                JsonValueKind.True => (true, DbType.Boolean),
                JsonValueKind.False => (false, DbType.Boolean),
                JsonValueKind.Array => ConvertArrayValue(parameterName, element),
                JsonValueKind.Object => ConvertObjectValue(parameterName, element),
                _ => (DBNull.Value, DbType.String)
            };
        }

        private (object Value, DbType DbType) ConvertStringValue(string parameterName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return (DBNull.Value, InferDbTypeFromName(parameterName));

            // Check for Binary/File data
            if (IsBinaryParameter(parameterName) && IsBase64String(value))
            {
                return (Convert.FromBase64String(value), DbType.Binary);
            }

            // Check for GUID
            if (Guid.TryParse(value, out Guid guidValue))
            {
                return (guidValue, DbType.Guid);
            }

            // Check for DateTime
            if (DateTime.TryParseExact(value, DateFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime dateValue))
            {
                return (dateValue, DbType.DateTime2);
            }

            // Check for TimeSpan
            if (TimeSpan.TryParse(value, out TimeSpan timeValue))
            {
                return (timeValue, DbType.Time);
            }

            // Check for XML
            if (IsValidXml(value) && parameterName.EndsWith("XML", StringComparison.OrdinalIgnoreCase))
            {
                return (value, DbType.Xml);
            }

            return (value, DbType.String);
        }

        private (object Value, DbType DbType) ConvertNumberValue(string parameterName, JsonElement element)
        {
            // For known decimal fields, always use decimal
            if (ShouldBeDecimal(parameterName))
            {
                if (element.TryGetDecimal(out decimal decimalValue))
                    return (decimalValue, DbType.Decimal);
            }

            // Try parsing as different numeric types
            if (element.TryGetInt32(out int intValue))
                return (intValue, DbType.Int32);

            if (element.TryGetInt64(out long longValue))
                return (longValue, DbType.Int64);

            if (element.TryGetDouble(out double doubleValue))
                return (doubleValue, DbType.Double);

            if (element.TryGetDecimal(out decimal fallbackDecimal))
                return (fallbackDecimal, DbType.Decimal);

            return (0, DbType.Int32);
        }

        private (object Value, DbType DbType) ConvertArrayValue(string parameterName, JsonElement element)
        {
            if (IsBinaryParameter(parameterName))
            {
                var byteArrays = element.EnumerateArray()
                    .Select(e => ConvertToByteArray(e))
                    .Where(b => b != null)
                    .ToArray();
                return (byteArrays, DbType.Binary);
            }

            var convertedArray = element.EnumerateArray()
                .Select(e => ConvertJsonElement(parameterName, e).Value)
                .ToArray();
            return (convertedArray, DbType.Object);
        }

        private (object Value, DbType DbType) ConvertObjectValue(string parameterName, JsonElement element)
        {
            var dictionary = element.EnumerateObject()
                .ToDictionary(
                    p => p.Name,
                    p => ConvertJsonElement(p.Name, p.Value).Value
                );
            return (dictionary, DbType.Object);
        }

        private DbType InferDbTypeFromName(string parameterName)
        {
            foreach (var hint in ParameterTypeHints)
            {
                if (parameterName.EndsWith(hint.Key, StringComparison.OrdinalIgnoreCase))
                    return hint.Value;
            }

            return DbType.String;
        }

        private DbType InferDbType(object value) => value switch
        {
            int => DbType.Int32,
            long => DbType.Int64,
            decimal => DbType.Decimal,
            double => DbType.Double,
            float => DbType.Single,
            bool => DbType.Boolean,
            DateTime => DbType.DateTime2,
            TimeSpan => DbType.Time,
            Guid => DbType.Guid,
            byte[] => DbType.Binary,
            char => DbType.StringFixedLength,
            BigInteger => DbType.Decimal,
            _ => DbType.String
        };

        private bool IsBinaryParameter(string parameterName)
        {
            return BinaryParameterSuffixes.Any(suffix =>
                parameterName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldBeDecimal(string parameterName)
        {
            return parameterName.EndsWith("Amount", StringComparison.OrdinalIgnoreCase) ||
                   parameterName.EndsWith("Price", StringComparison.OrdinalIgnoreCase) ||
                   parameterName.EndsWith("Cost", StringComparison.OrdinalIgnoreCase) ||
                   parameterName.EndsWith("Rate", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsBase64String(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            try
            {
                var normalizedValue = value
                    .Replace('-', '+')
                    .Replace('_', '/');

                switch (normalizedValue.Length % 4)
                {
                    case 2: normalizedValue += "=="; break;
                    case 3: normalizedValue += "="; break;
                }

                Convert.FromBase64String(normalizedValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidXml(string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return false;
                System.Xml.Linq.XDocument.Parse(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private byte[] ConvertToByteArray(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                if (IsBase64String(value))
                {
                    return Convert.FromBase64String(value);
                }
            }
            return null;
        }
    }
}