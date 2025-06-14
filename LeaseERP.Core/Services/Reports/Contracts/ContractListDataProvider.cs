// LeaseERP.Core/Services/Reports/Contracts/ContractListDataProvider.cs (Fixed)
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeaseERP.Core.Services.Reports.Contracts
{
    public class ContractListDataProvider : BaseReportDataProvider
    {
        public ContractListDataProvider(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<ContractListDataProvider> logger)
            : base(dataService, configuration, logger)
        {
        }

        public override string GetStoredProcedureName()
        {
            return _configuration["StoredProcedures:contractmanagement"];
        }

        public override Dictionary<string, object> BuildParameters(ReportRequest request)
        {
            var parameters = GetBaseParameters(request);

            // Use search mode for contract list
            parameters.Add("@Mode", (int)OperationType.Search);

            // Add search and filter parameters from the request
            if (request.Parameters.TryGetValue("SearchText", out var searchText) && !string.IsNullOrEmpty(ConvertToString(searchText)))
            {
                parameters.Add("@SearchText", ConvertToString(searchText));
            }

            if (request.Parameters.TryGetValue("FilterCustomerID", out var filterCustomerId) && filterCustomerId != null)
            {
                var customerIdValue = ConvertToLong(filterCustomerId);
                if (customerIdValue.HasValue && customerIdValue.Value > 0)
                {
                    parameters.Add("@FilterCustomerID", customerIdValue.Value);
                }
            }

            if (request.Parameters.TryGetValue("FilterContractStatus", out var filterContractStatus) && !string.IsNullOrEmpty(ConvertToString(filterContractStatus)))
            {
                parameters.Add("@FilterContractStatus", ConvertToString(filterContractStatus));
            }

            if (request.Parameters.TryGetValue("FilterFromDate", out var filterFromDate) && filterFromDate != null)
            {
                var fromDate = ConvertToDateTime(filterFromDate);
                if (fromDate.HasValue)
                {
                    parameters.Add("@FilterFromDate", fromDate.Value);
                }
            }

            if (request.Parameters.TryGetValue("FilterToDate", out var filterToDate) && filterToDate != null)
            {
                var toDate = ConvertToDateTime(filterToDate);
                if (toDate.HasValue)
                {
                    parameters.Add("@FilterToDate", toDate.Value);
                }
            }

            if (request.Parameters.TryGetValue("FilterUnitID", out var filterUnitId) && filterUnitId != null)
            {
                var unitIdValue = ConvertToLong(filterUnitId);
                if (unitIdValue.HasValue && unitIdValue.Value > 0)
                {
                    parameters.Add("@FilterUnitID", unitIdValue.Value);
                }
            }

            if (request.Parameters.TryGetValue("FilterPropertyID", out var filterPropertyId) && filterPropertyId != null)
            {
                var propertyIdValue = ConvertToLong(filterPropertyId);
                if (propertyIdValue.HasValue && propertyIdValue.Value > 0)
                {
                    parameters.Add("@FilterPropertyID", propertyIdValue.Value);
                }
            }

            return parameters;
        }

        private string ConvertToString(object value)
        {
            if (value is System.Text.Json.JsonElement element)
            {
                return element.ValueKind == System.Text.Json.JsonValueKind.String ? element.GetString() : element.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        private long? ConvertToLong(object value)
        {
            if (value is System.Text.Json.JsonElement element)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    if (element.TryGetInt64(out long longValue))
                        return longValue;
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                }
                else if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var stringValue = element.GetString();
                    if (long.TryParse(stringValue, out long parsedValue))
                        return parsedValue;
                }
                return null;
            }

            if (value is long l) return l;
            if (value is int i) return i;
            if (value is string s && long.TryParse(s, out long parsed)) return parsed;

            return null;
        }

        private DateTime? ConvertToDateTime(object value)
        {
            if (value is System.Text.Json.JsonElement element)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var stringValue = element.GetString();
                    if (DateTime.TryParse(stringValue, out DateTime dateValue))
                        return dateValue;
                }
                return null;
            }

            if (value is DateTime dt) return dt;
            if (value is string s && DateTime.TryParse(s, out DateTime parsed)) return parsed;

            return null;
        }

        protected override async Task EnrichWithMetadata(ReportData reportData, ReportRequest request)
        {
            await base.EnrichWithMetadata(reportData, request);

            // Add contract list specific metadata
            if (request.Parameters.TryGetValue("ReportTitle", out var reportTitle))
            {
                reportData.Metadata["ReportTitle"] = ConvertToString(reportTitle) ?? "Contract List Report";
            }
            else
            {
                reportData.Metadata["ReportTitle"] = "Contract List Report";
            }

            // Add applied filters to metadata for display
            var appliedFilters = new Dictionary<string, object>();

            if (request.Parameters.TryGetValue("SearchText", out var searchText) && !string.IsNullOrEmpty(ConvertToString(searchText)))
            {
                appliedFilters["SearchText"] = ConvertToString(searchText);
            }

            if (request.Parameters.TryGetValue("FilterCustomerID", out var filterCustomerId) && filterCustomerId != null)
            {
                var customerIdValue = ConvertToLong(filterCustomerId);
                if (customerIdValue.HasValue && customerIdValue.Value > 0)
                {
                    appliedFilters["FilterCustomerID"] = customerIdValue.Value;
                    // In a real implementation, you might want to resolve the customer name
                    appliedFilters["CustomerName"] = await ResolveCustomerName(customerIdValue.Value);
                }
            }

            if (request.Parameters.TryGetValue("FilterContractStatus", out var filterContractStatus) && !string.IsNullOrEmpty(ConvertToString(filterContractStatus)))
            {
                appliedFilters["ContractStatus"] = ConvertToString(filterContractStatus);
            }

            if (request.Parameters.TryGetValue("FilterFromDate", out var filterFromDate) && filterFromDate != null)
            {
                var fromDate = ConvertToDateTime(filterFromDate);
                if (fromDate.HasValue)
                {
                    appliedFilters["FromDate"] = fromDate.Value;
                }
            }

            if (request.Parameters.TryGetValue("FilterToDate", out var filterToDate) && filterToDate != null)
            {
                var toDate = ConvertToDateTime(filterToDate);
                if (toDate.HasValue)
                {
                    appliedFilters["ToDate"] = toDate.Value;
                }
            }

            if (request.Parameters.TryGetValue("FilterUnitID", out var filterUnitId) && filterUnitId != null)
            {
                var unitIdValue = ConvertToLong(filterUnitId);
                if (unitIdValue.HasValue && unitIdValue.Value > 0)
                {
                    appliedFilters["FilterUnitID"] = unitIdValue.Value;
                    // In a real implementation, you might want to resolve the unit number
                    appliedFilters["UnitNo"] = await ResolveUnitNumber(unitIdValue.Value);
                }
            }

            if (request.Parameters.TryGetValue("FilterPropertyID", out var filterPropertyId) && filterPropertyId != null)
            {
                var propertyIdValue = ConvertToLong(filterPropertyId);
                if (propertyIdValue.HasValue && propertyIdValue.Value > 0)
                {
                    appliedFilters["FilterPropertyID"] = propertyIdValue.Value;
                    // In a real implementation, you might want to resolve the property name
                    appliedFilters["PropertyName"] = await ResolvePropertyName(propertyIdValue.Value);
                }
            }

            reportData.Metadata["AppliedFilters"] = appliedFilters;
        }

        private async Task<string> ResolveCustomerName(long customerId)
        {
            try
            {
                // In a real implementation, you would fetch the customer name from the database
                // For now, return a placeholder
                var parameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.FetchById },
                    { "@CustomerID", customerId }
                };

                var customerSpName = _configuration["StoredProcedures:customer"];
                if (!string.IsNullOrEmpty(customerSpName))
                {
                    var result = await _dataService.ExecuteStoredProcedureAsync(customerSpName, parameters);
                    if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        return result.Tables[0].Rows[0]["CustomerFullName"]?.ToString() ?? $"Customer ID: {customerId}";
                    }
                }

                return $"Customer ID: {customerId}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve customer name for ID: {CustomerId}", customerId);
                return $"Customer ID: {customerId}";
            }
        }

        private async Task<string> ResolveUnitNumber(long unitId)
        {
            try
            {
                // In a real implementation, you would fetch the unit number from the database
                var parameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.FetchById },
                    { "@UnitID", unitId }
                };

                var unitSpName = _configuration["StoredProcedures:unit"];
                if (!string.IsNullOrEmpty(unitSpName))
                {
                    var result = await _dataService.ExecuteStoredProcedureAsync(unitSpName, parameters);
                    if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        return result.Tables[0].Rows[0]["UnitNo"]?.ToString() ?? $"Unit ID: {unitId}";
                    }
                }

                return $"Unit ID: {unitId}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve unit number for ID: {UnitId}", unitId);
                return $"Unit ID: {unitId}";
            }
        }

        private async Task<string> ResolvePropertyName(long propertyId)
        {
            try
            {
                // In a real implementation, you would fetch the property name from the database
                var parameters = new Dictionary<string, object>
                {
                    { "@Mode", (int)OperationType.FetchById },
                    { "@PropertyID", propertyId }
                };

                var propertySpName = _configuration["StoredProcedures:property"];
                if (!string.IsNullOrEmpty(propertySpName))
                {
                    var result = await _dataService.ExecuteStoredProcedureAsync(propertySpName, parameters);
                    if (result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        return result.Tables[0].Rows[0]["PropertyName"]?.ToString() ?? $"Property ID: {propertyId}";
                    }
                }

                return $"Property ID: {propertyId}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve property name for ID: {PropertyId}", propertyId);
                return $"Property ID: {propertyId}";
            }
        }
    }
}