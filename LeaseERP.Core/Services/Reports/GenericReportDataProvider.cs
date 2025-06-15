// LeaseERP.Core/Services/Reports/GenericReportDataProvider.cs
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LeaseERP.Core.Services.Reports
{
    public class GenericReportDataProvider : IReportDataProvider
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GenericReportDataProvider> _logger;
        private readonly string _reportType;

        public GenericReportDataProvider(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<GenericReportDataProvider> logger,
            string reportType)
        {
            _dataService = dataService;
            _configuration = configuration;
            _logger = logger;
            _reportType = reportType;
        }

        public string GetStoredProcedureName()
        {
            // Map report types to stored procedure configuration keys
            var configKey = GetStoredProcedureConfigKey(_reportType);
            return _configuration[configKey];
        }

        public Dictionary<string, object> BuildParameters(ReportRequest request)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@CurrentUserName", request.ActionBy }
            };

            // Add mode based on report type and operation
            parameters.Add("@Mode", GetModeForReportType(_reportType, request));

            // Convert and add all request parameters
            foreach (var param in request.Parameters)
            {
                var parameterName = $"@{param.Key}";

                // Skip if already added
                if (parameters.ContainsKey(parameterName))
                    continue;

                var convertedValue = ConvertJsonElementToNativeType(param.Value, param.Key);
                parameters.Add(parameterName, convertedValue);
            }

            return parameters;
        }

        public async Task<ReportData> GetDataAsync(ReportRequest request)
        {
            try
            {
                var spName = GetStoredProcedureName();
                if (string.IsNullOrEmpty(spName))
                {
                    return new ReportData
                    {
                        Success = false,
                        Message = $"Stored procedure not configured for report type: {request.ReportType}"
                    };
                }

                var parameters = BuildParameters(request);
                var dataSet = await _dataService.ExecuteStoredProcedureAsync(spName, parameters);

                var reportData = new ReportData
                {
                    DataSet = dataSet,
                    Success = true
                };

                // Add metadata
                await EnrichWithMetadata(reportData, request);

                return reportData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving data for report type: {ReportType}", request.ReportType);
                return new ReportData
                {
                    Success = false,
                    Message = $"Error retrieving report data: {ex.Message}"
                };
            }
        }

        private string GetStoredProcedureConfigKey(string reportType)
        {
            return reportType.ToLower() switch
            {
                "contract-slip" => "StoredProcedures:contractmanagement",
                "contract-list" => "StoredProcedures:contractmanagement",
                "termination-slip" => "StoredProcedures:contracttermination",
                "invoice-slip" => "StoredProcedures:contractInvoiceManagement",
                "invoice-list" => "StoredProcedures:contractInvoiceManagement",
                "receipt-slip" => "StoredProcedures:contractReceiptManagement",
                "receipt-list" => "StoredProcedures:contractReceiptManagement",
                "petty-cash-slip" => "StoredProcedures:pettyCash",
                "pettycash-slip" => "StoredProcedures:pettyCash",
                "customer-list" => "StoredProcedures:customer",
                "property-list" => "StoredProcedures:property",
                "unit-list" => "StoredProcedures:unit",
                "payment-voucher-slip" => "StoredProcedures:paymentvoucher",
                _ => $"StoredProcedures:{reportType.Replace("-", "")}"
            };
        }

        private int GetModeForReportType(string reportType, ReportRequest request)
        {
            // For slip reports (single record), use FetchById
            if (reportType.EndsWith("-slip"))
            {
                return (int)OperationType.FetchById;
            }

            // For list reports, use Search if there are filters, otherwise FetchAll
            if (reportType.EndsWith("-list"))
            {
                bool hasFilters = request.Parameters.Any(p =>
                    p.Key.StartsWith("Filter") ||
                    p.Key.Equals("SearchText", StringComparison.OrdinalIgnoreCase));

                return hasFilters ? (int)OperationType.Search : (int)OperationType.FetchAll;
            }

            // Default to FetchAll
            return (int)OperationType.FetchAll;
        }

        private object ConvertJsonElementToNativeType(object value, string paramKey = null)
        {
            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        var stringValue = element.GetString();

                        // Handle date parameters
                        if (paramKey != null && (
                            paramKey.Contains("Date", StringComparison.OrdinalIgnoreCase) ||
                            paramKey.Contains("From", StringComparison.OrdinalIgnoreCase) ||
                            paramKey.Contains("To", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (DateTime.TryParse(stringValue, out DateTime dateValue))
                                return dateValue;
                        }

                        return stringValue;

                    case JsonValueKind.Number:
                        if (element.TryGetInt64(out long longValue))
                            return longValue;
                        if (element.TryGetInt32(out int intValue))
                            return intValue;
                        if (element.TryGetDecimal(out decimal decimalValue))
                            return decimalValue;
                        if (element.TryGetDouble(out double doubleValue))
                            return doubleValue;
                        return element.GetRawText();

                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                        return DBNull.Value;
                    case JsonValueKind.Array:
                    case JsonValueKind.Object:
                        return element.GetRawText();
                    default:
                        return null;
                }
            }

            return value ?? DBNull.Value;
        }

        private async Task EnrichWithMetadata(ReportData reportData, ReportRequest request)
        {
            // Add common metadata
            reportData.Metadata["GeneratedBy"] = request.ActionBy;
            reportData.Metadata["GeneratedOn"] = DateTime.Now;
            reportData.Metadata["ReportType"] = request.ReportType;

            // Add report-specific title
            reportData.Metadata["ReportTitle"] = GetReportTitle(request.ReportType, request.Parameters);

            // Add company information
            reportData.Metadata["CompanyInfo"] = await GetCompanyInfo();

            // Add applied filters for list reports
            if (request.ReportType.EndsWith("-list"))
            {
                var appliedFilters = ExtractAppliedFilters(request.Parameters);
                if (appliedFilters.Any())
                {
                    reportData.Metadata["AppliedFilters"] = appliedFilters;
                }
            }
        }

        private string GetReportTitle(string reportType, Dictionary<string, object> parameters)
        {
            // Check if custom title is provided
            if (parameters.TryGetValue("ReportTitle", out var customTitle) &&
                customTitle != null && !string.IsNullOrEmpty(customTitle.ToString()))
            {
                return customTitle.ToString();
            }

            // Default titles based on report type
            return reportType.ToLower() switch
            {
                "contract-slip" => "CONTRACT SLIP",
                "contract-list" => "CONTRACT LIST REPORT",
                "termination-slip" => "CONTRACT TERMINATION SLIP",
                "invoice-slip" => "LEASE INVOICE",
                "receipt-slip" => "LEASE RECEIPT",
                "customer-list" => "CUSTOMER LIST REPORT",
                "property-list" => "PROPERTY LIST REPORT",
                "unit-list" => "UNIT LIST REPORT",
                _ => reportType.Replace("-", " ").ToUpper() + " REPORT"
            };
        }

        private Dictionary<string, object> ExtractAppliedFilters(Dictionary<string, object> parameters)
        {
            var filters = new Dictionary<string, object>();

            foreach (var param in parameters)
            {
                if (param.Value == null) continue;

                // Extract meaningful filters
                if (param.Key.Equals("SearchText", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(param.Value.ToString()))
                {
                    filters["SearchText"] = param.Value.ToString();
                }
                else if (param.Key.StartsWith("Filter") && param.Value != null)
                {
                    var filterName = param.Key.Substring(6); // Remove "Filter" prefix

                    if (param.Key.Contains("Date"))
                    {
                        if (DateTime.TryParse(param.Value.ToString(), out DateTime dateValue))
                        {
                            filters[filterName] = dateValue;
                        }
                    }
                    else if (param.Key.Contains("ID"))
                    {
                        if (long.TryParse(param.Value.ToString(), out long idValue) && idValue > 0)
                        {
                            filters[filterName] = idValue;
                            // You could resolve names here if needed
                        }
                    }
                    else
                    {
                        filters[filterName] = param.Value.ToString();
                    }
                }
            }

            return filters;
        }

        private async Task<CompanyInfo> GetCompanyInfo()
        {
            // This could be cached or retrieved from a company service
            var logoPath = _configuration["PdfSettings:CompanyLogo"];

            return new CompanyInfo
            {
                CompanyID = 1,
                CompanyName = "LeaseERP Solutions",
                CompanyAddress = "123 Business Street, City, Country",
                CompanyPhone = "+1-234-567-8900",
                CompanyEmail = "info@leaseerp.com",
                CompanyWebsite = "www.leaseerp.com",
                TaxRegNo = "TAX123456789",
                CommercialRegNo = "CR123456789",
                CompanyLogo = logoPath ?? ""
            };
        }
    }

    // Factory class to create generic data providers
    public class GenericReportDataProviderFactory
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GenericReportDataProvider> _logger;

        public GenericReportDataProviderFactory(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<GenericReportDataProvider> logger)
        {
            _dataService = dataService;
            _configuration = configuration;
            _logger = logger;
        }

        public IReportDataProvider CreateDataProvider(string reportType)
        {
            return new GenericReportDataProvider(_dataService, _configuration, _logger, reportType);
        }
    }
}