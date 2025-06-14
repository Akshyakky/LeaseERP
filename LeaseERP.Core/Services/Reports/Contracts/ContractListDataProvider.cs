// LeaseERP.Core/Services/Reports/Contracts/ContractListDataProvider.cs
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
            if (request.Parameters.TryGetValue("SearchText", out var searchText) && !string.IsNullOrEmpty(searchText?.ToString()))
            {
                parameters.Add("@SearchText", searchText.ToString());
            }

            if (request.Parameters.TryGetValue("FilterCustomerID", out var filterCustomerId) && filterCustomerId != null)
            {
                var customerIdValue = Convert.ToInt64(filterCustomerId);
                if (customerIdValue > 0)
                {
                    parameters.Add("@FilterCustomerID", customerIdValue);
                }
            }

            if (request.Parameters.TryGetValue("FilterContractStatus", out var filterContractStatus) && !string.IsNullOrEmpty(filterContractStatus?.ToString()))
            {
                parameters.Add("@FilterContractStatus", filterContractStatus.ToString());
            }

            if (request.Parameters.TryGetValue("FilterFromDate", out var filterFromDate) && filterFromDate != null)
            {
                if (DateTime.TryParse(filterFromDate.ToString(), out var fromDate))
                {
                    parameters.Add("@FilterFromDate", fromDate);
                }
            }

            if (request.Parameters.TryGetValue("FilterToDate", out var filterToDate) && filterToDate != null)
            {
                if (DateTime.TryParse(filterToDate.ToString(), out var toDate))
                {
                    parameters.Add("@FilterToDate", toDate);
                }
            }

            if (request.Parameters.TryGetValue("FilterUnitID", out var filterUnitId) && filterUnitId != null)
            {
                var unitIdValue = Convert.ToInt64(filterUnitId);
                if (unitIdValue > 0)
                {
                    parameters.Add("@FilterUnitID", unitIdValue);
                }
            }

            if (request.Parameters.TryGetValue("FilterPropertyID", out var filterPropertyId) && filterPropertyId != null)
            {
                var propertyIdValue = Convert.ToInt64(filterPropertyId);
                if (propertyIdValue > 0)
                {
                    parameters.Add("@FilterPropertyID", propertyIdValue);
                }
            }

            return parameters;
        }

        protected override async Task EnrichWithMetadata(ReportData reportData, ReportRequest request)
        {
            await base.EnrichWithMetadata(reportData, request);

            // Add contract list specific metadata
            if (request.Parameters.TryGetValue("ReportTitle", out var reportTitle))
            {
                reportData.Metadata["ReportTitle"] = reportTitle?.ToString() ?? "Contract List Report";
            }
            else
            {
                reportData.Metadata["ReportTitle"] = "Contract List Report";
            }

            // Add applied filters to metadata for display
            var appliedFilters = new Dictionary<string, object>();

            if (request.Parameters.TryGetValue("SearchText", out var searchText) && !string.IsNullOrEmpty(searchText?.ToString()))
            {
                appliedFilters["SearchText"] = searchText.ToString();
            }

            if (request.Parameters.TryGetValue("FilterCustomerID", out var filterCustomerId) && filterCustomerId != null)
            {
                var customerIdValue = Convert.ToInt64(filterCustomerId);
                if (customerIdValue > 0)
                {
                    appliedFilters["FilterCustomerID"] = customerIdValue;
                    // In a real implementation, you might want to resolve the customer name
                    appliedFilters["CustomerName"] = await ResolveCustomerName(customerIdValue);
                }
            }

            if (request.Parameters.TryGetValue("FilterContractStatus", out var filterContractStatus) && !string.IsNullOrEmpty(filterContractStatus?.ToString()))
            {
                appliedFilters["ContractStatus"] = filterContractStatus.ToString();
            }

            if (request.Parameters.TryGetValue("FilterFromDate", out var filterFromDate) && filterFromDate != null)
            {
                if (DateTime.TryParse(filterFromDate.ToString(), out var fromDate))
                {
                    appliedFilters["FromDate"] = fromDate;
                }
            }

            if (request.Parameters.TryGetValue("FilterToDate", out var filterToDate) && filterToDate != null)
            {
                if (DateTime.TryParse(filterToDate.ToString(), out var toDate))
                {
                    appliedFilters["ToDate"] = toDate;
                }
            }

            if (request.Parameters.TryGetValue("FilterUnitID", out var filterUnitId) && filterUnitId != null)
            {
                var unitIdValue = Convert.ToInt64(filterUnitId);
                if (unitIdValue > 0)
                {
                    appliedFilters["FilterUnitID"] = unitIdValue;
                    // In a real implementation, you might want to resolve the unit number
                    appliedFilters["UnitNo"] = await ResolveUnitNumber(unitIdValue);
                }
            }

            if (request.Parameters.TryGetValue("FilterPropertyID", out var filterPropertyId) && filterPropertyId != null)
            {
                var propertyIdValue = Convert.ToInt64(filterPropertyId);
                if (propertyIdValue > 0)
                {
                    appliedFilters["FilterPropertyID"] = propertyIdValue;
                    // In a real implementation, you might want to resolve the property name
                    appliedFilters["PropertyName"] = await ResolvePropertyName(propertyIdValue);
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