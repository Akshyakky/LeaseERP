// LeaseERP.Core/Services/Reports/BaseReportDataProvider.cs
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeaseERP.Core.Services.Reports
{
    public abstract class BaseReportDataProvider : IReportDataProvider
    {
        protected readonly IDataService _dataService;
        protected readonly IConfiguration _configuration;
        protected readonly ILogger _logger;

        protected BaseReportDataProvider(
            IDataService dataService,
            IConfiguration configuration,
            ILogger logger)
        {
            _dataService = dataService;
            _configuration = configuration;
            _logger = logger;
        }

        public abstract string GetStoredProcedureName();
        public abstract Dictionary<string, object> BuildParameters(ReportRequest request);

        public virtual async Task<ReportData> GetDataAsync(ReportRequest request)
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

                // Add common metadata
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

        protected virtual async Task EnrichWithMetadata(ReportData reportData, ReportRequest request)
        {
            // Add common metadata that all reports might need
            reportData.Metadata["GeneratedBy"] = request.ActionBy;
            reportData.Metadata["GeneratedOn"] = DateTime.Now;
            reportData.Metadata["ReportType"] = request.ReportType;

            // Add company information
            reportData.Metadata["CompanyInfo"] = await GetCompanyInfo();
        }

        protected virtual async Task<CompanyInfo> GetCompanyInfo()
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

        protected virtual Dictionary<string, object> GetBaseParameters(ReportRequest request)
        {
            var parameters = new Dictionary<string, object>
            {
                { "@CurrentUserName", request.ActionBy }
            };

            // Add user ID if available (you might need to resolve this from the request context)
            // parameters.Add("@CurrentUserID", GetCurrentUserId(request));

            return parameters;
        }
    }
}