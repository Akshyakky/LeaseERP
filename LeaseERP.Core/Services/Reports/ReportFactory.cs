// LeaseERP.Core/Services/Reports/ReportFactory.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LeaseERP.Core.Services.Reports
{
    public class ReportFactory : IReportFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReportFactory> _logger;

        public ReportFactory(IServiceProvider serviceProvider, ILogger<ReportFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public IReportTemplate CreateTemplate(string reportType)
        {
            return reportType.ToLower() switch
            {
                "contract-slip" => _serviceProvider.GetRequiredService<ContractSlipTemplate>(),
                "contract-list" => _serviceProvider.GetRequiredService<ContractListTemplate>(),
                // Add more report types as needed
                _ => null
            };
        }

        public IReportDataProvider CreateDataProvider(string reportType)
        {
            return reportType.ToLower() switch
            {
                "contract-slip" => _serviceProvider.GetRequiredService<ContractSlipDataProvider>(),
                "contract-list" => _serviceProvider.GetRequiredService<ContractListDataProvider>(),
                // Add more data providers as needed
                _ => null
            };
        }

        public IEnumerable<IReportComponent> CreateComponents(string reportType)
        {
            // Return standard components for now, could be customized per report type
            return _serviceProvider.GetServices<IReportComponent>();
        }
    }
}