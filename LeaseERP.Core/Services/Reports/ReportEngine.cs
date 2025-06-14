// LeaseERP.Core/Services/Reports/ReportEngine.cs
using LeaseERP.Core.Interfaces.Reports;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace LeaseERP.Core.Services.Reports
{
    public class ReportEngine : IReportEngine
    {
        private readonly IReportFactory _reportFactory;
        private readonly ILogger<ReportEngine> _logger;

        public ReportEngine(IReportFactory reportFactory, ILogger<ReportEngine> logger)
        {
            _reportFactory = reportFactory;
            _logger = logger;
        }

        public async Task<byte[]> GenerateReportAsync(ReportRequest request)
        {
            try
            {
                _logger.LogInformation("Generating report: {ReportType}", request.ReportType);

                // Get data
                var reportData = await GetReportDataAsync(request);
                if (!reportData.Success)
                {
                    throw new Exception(reportData.Message);
                }

                // Get template
                var template = _reportFactory.CreateTemplate(request.ReportType);
                if (template == null)
                {
                    throw new Exception($"No template found for report type: {request.ReportType}");
                }

                // Get configuration
                var config = await GetReportConfigurationAsync(request.ReportType);

                // Generate document
                var document = template.CreateDocument(reportData, config);
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {ReportType}", request.ReportType);
                throw;
            }
        }

        public async Task<ReportData> GetReportDataAsync(ReportRequest request)
        {
            var dataProvider = _reportFactory.CreateDataProvider(request.ReportType);
            if (dataProvider == null)
            {
                return new ReportData
                {
                    Success = false,
                    Message = $"No data provider found for report type: {request.ReportType}"
                };
            }

            return await dataProvider.GetDataAsync(request);
        }

        public async Task<ReportConfiguration> GetReportConfigurationAsync(string reportType)
        {
            var template = _reportFactory.CreateTemplate(reportType);
            return template?.GetDefaultConfiguration() ?? new ReportConfiguration();
        }

        public IEnumerable<string> GetAvailableReports()
        {
            // This could be implemented to scan for all available report types
            return new[]
            {
                "contract-slip",
                "contract-list",
                "termination-slip",
                "invoice-slip",
                "receipt-slip"
            };
        }
    }
}