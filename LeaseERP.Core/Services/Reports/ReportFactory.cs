// LeaseERP.Core/Services/Reports/ReportFactory.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports.Contracts;
using LeaseERP.Core.Services.Reports.Termination;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports
{
    public class ReportFactory : IReportFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly GenericReportDataProviderFactory _genericDataProviderFactory;
        private readonly ILogger<ReportFactory> _logger;

        // Define which reports need custom data providers (complex logic)
        private readonly HashSet<string> _customDataProviderReports = new()
        {
            // Add report types that need custom data providers here
            // "complex-contract-analysis",
            // "financial-dashboard"
        };

        // Define which reports need custom templates (complex layouts)
        private readonly HashSet<string> _customTemplateReports = new()
        {
            "contract-slip",
            "contract-list",
            "termination-slip"
            // Add more as needed
        };

        public ReportFactory(
            IServiceProvider serviceProvider,
            GenericReportDataProviderFactory genericDataProviderFactory,
            ILogger<ReportFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _genericDataProviderFactory = genericDataProviderFactory;
            _logger = logger;
        }

        public IReportTemplate CreateTemplate(string reportType)
        {
            try
            {
                // First check if we have a custom template for this report type
                if (_customTemplateReports.Contains(reportType.ToLower()))
                {
                    return reportType.ToLower() switch
                    {
                        "contract-slip" => _serviceProvider.GetRequiredService<ContractSlipTemplate>(),
                        "contract-list" => _serviceProvider.GetRequiredService<ContractListTemplate>(),
                        "termination-slip" => _serviceProvider.GetRequiredService<TerminationSlipTemplate>(),
                        _ => null
                    };
                }

                // For simple reports, use the generic template
                return _serviceProvider.GetService<GenericReportTemplate>()?.Configure(reportType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating template for report type: {ReportType}", reportType);
                return null;
            }
        }

        public IReportDataProvider CreateDataProvider(string reportType)
        {
            try
            {
                // Check if this report type needs a custom data provider
                if (_customDataProviderReports.Contains(reportType.ToLower()))
                {
                    return reportType.ToLower() switch
                    {
                        // Add custom data providers here if needed
                        // "complex-contract-analysis" => _serviceProvider.GetRequiredService<ComplexContractAnalysisDataProvider>(),
                        _ => null
                    };
                }

                // For most reports, use the generic data provider
                return _genericDataProviderFactory.CreateDataProvider(reportType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating data provider for report type: {ReportType}", reportType);
                return null;
            }
        }

        public IEnumerable<IReportComponent> CreateComponents(string reportType)
        {
            // Return standard components for now, could be customized per report type
            return _serviceProvider.GetServices<IReportComponent>();
        }

        public IEnumerable<string> GetAvailableReports()
        {
            // This could be loaded from configuration or database
            return new[]
            {
                "contract-slip",
                "contract-list",
                "termination-slip",
                "invoice-slip",
                "receipt-slip",
                "customer-list",
                "property-list",
                "unit-list",
                "payment-voucher-slip",
                "journal-voucher-slip",
                "general-ledger-report",
                "trial-balance-report"
            };
        }
    }

    // Generic template for simple list reports
    public class GenericReportTemplate : BaseReportTemplate
    {
        private string _reportType = string.Empty;

        public override string ReportType => _reportType;

        public GenericReportTemplate(
            ILogger<GenericReportTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public GenericReportTemplate Configure(string reportType)
        {
            _reportType = reportType;
            return this;
        }

        public override bool CanHandle(string reportType)
        {
            return !string.IsNullOrEmpty(_reportType) &&
                   string.Equals(_reportType, reportType, StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            var isListReport = _reportType.EndsWith("-list");

            return new ReportConfiguration
            {
                Title = GetReportTitle(_reportType),
                Orientation = isListReport ? ReportOrientation.Landscape : ReportOrientation.Portrait,
                Header = new ReportHeaderConfig
                {
                    ShowCompanyInfo = true,
                    ShowLogo = true,
                    ShowReportTitle = true,
                    ShowGenerationInfo = true,
                    ShowFilters = isListReport,
                    Height = isListReport ? 140 : 120
                },
                Footer = new ReportFooterConfig
                {
                    ShowPageNumbers = true,
                    ShowGenerationInfo = true,
                    ShowReportTitle = false,
                    Height = 30
                },
                Styling = new ReportStylingConfig
                {
                    DefaultFontSize = isListReport ? 9 : 10,
                    FontFamily = "Arial",
                    HeaderColor = "#1e40af",
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            if (data?.DataSet?.Tables?.Count == 0)
            {
                container.AlignCenter().Padding(20)
                    .Text("No data available for this report")
                    .FontSize(12);
                return;
            }

            // For simple reports, just render the first table as a basic table
            var mainTable = data.DataSet.Tables[0];
            if (mainTable.Rows.Count == 0)
            {
                container.AlignCenter().Padding(20)
                    .Text("No records found")
                    .FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Element(c => RenderDataTable(c, mainTable, config));
        }

        private void RenderDataTable(IContainer container, DataTable table, ReportConfiguration config)
        {
            container.Table(tableBuilder =>
            {
                // Define columns
                tableBuilder.ColumnsDefinition(columns =>
                {
                    for (int i = 0; i < table.Columns.Count; i++)
                    {
                        columns.RelativeColumn(1);
                    }
                });

                // Header
                tableBuilder.Header(header =>
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        header.Cell()
                            .Background(ParseColor(config.Styling.HeaderColor))
                            .Padding(5)
                            .Text(FormatColumnHeader(column.ColumnName))
                            .FontColor(Colors.White)
                            .FontSize(config.Styling.DefaultFontSize)
                            .SemiBold();
                    }
                });

                // Data rows
                bool isAlternate = false;
                foreach (DataRow row in table.Rows)
                {
                    foreach (DataColumn column in table.Columns)
                    {
                        var cell = tableBuilder.Cell();

                        if (config.Styling.AlternateRowColors && isAlternate)
                        {
                            cell.Background(Colors.Grey.Lighten5);
                        }

                        cell.BorderBottom(0.5f)
                            .BorderColor(ParseColor(config.Styling.BorderColor))
                            .Padding(3)
                            .Text(FormatCellValue(row[column], column.DataType))
                            .FontSize(config.Styling.DefaultFontSize - 1);
                    }
                    isAlternate = !isAlternate;
                }
            });
        }

        private string FormatColumnHeader(string columnName)
        {
            // Convert PascalCase to Title Case with spaces
            return System.Text.RegularExpressions.Regex.Replace(columnName, "([a-z])([A-Z])", "$1 $2");
        }

        private string FormatCellValue(object value, Type dataType)
        {
            if (value == null || value == DBNull.Value)
                return "";

            return dataType.Name switch
            {
                "DateTime" => ((DateTime)value).ToString("dd/MM/yyyy"),
                "Decimal" => ((decimal)value).ToString("N2"),
                "Double" => ((double)value).ToString("N2"),
                "Single" => ((float)value).ToString("N2"),
                "Boolean" => ((bool)value) ? "Yes" : "No",
                _ => value.ToString()
            };
        }

        private string GetReportTitle(string reportType)
        {
            return reportType.Replace("-", " ").ToUpper() + " REPORT";
        }
    }
}