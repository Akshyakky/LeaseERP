// LeaseERP.Core/Services/Reports/Components/StandardFooterComponent.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LeaseERP.Core.Services.Reports.Components
{
    public class StandardFooterComponent : IReportFooterComponent
    {
        private readonly ILogger<StandardFooterComponent> _logger;
        private readonly IConfiguration _configuration;

        public string ComponentName => "StandardFooter";
        public bool IsRequired => false;

        public StandardFooterComponent(ILogger<StandardFooterComponent> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void Render(IContainer container, ReportData data, ReportConfiguration config)
        {
            container.Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
            {
                // Left side - Report information
                row.RelativeItem().AlignLeft().Text(x =>
                {
                    if (config.Footer.ShowReportTitle)
                    {
                        x.Span(config.Title).FontSize(8);
                    }

                    if (config.Footer.ShowGenerationInfo)
                    {
                        if (config.Footer.ShowReportTitle)
                        {
                            x.Span(" | ");
                        }

                        var generatedOn = data.Metadata.TryGetValue("GeneratedOn", out var genOnObj) && genOnObj is DateTime genOn
                            ? genOn
                            : DateTime.Now;

                        x.Span($"Generated: {generatedOn:dd/MM/yyyy HH:mm}").FontSize(8);

                        var generatedBy = data.Metadata.GetValueOrDefault("GeneratedBy", "System")?.ToString();
                        if (!string.IsNullOrEmpty(generatedBy) && generatedBy != "System")
                        {
                            x.Span($" by {generatedBy}").FontSize(8);
                        }
                    }
                });

                // Center - Company footer info (optional)
                row.RelativeItem().AlignCenter().Text(x =>
                {
                    if (data.Metadata.TryGetValue("CompanyInfo", out var companyObj) && companyObj is CompanyInfo company)
                    {
                        if (!string.IsNullOrEmpty(company.CompanyWebsite))
                        {
                            x.Span(company.CompanyWebsite).FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                    }
                    else
                    {
                        x.Span("www.leaseerp.com").FontSize(8).FontColor(Colors.Grey.Darken1);
                    }
                });

                // Right side - Page numbers
                row.RelativeItem().AlignRight().Text(x =>
                {
                    if (config.Footer.ShowPageNumbers)
                    {
                        x.Span("Page ").FontSize(8);
                        x.CurrentPageNumber().FontSize(8);
                        x.Span(" of ").FontSize(8);
                        x.TotalPages().FontSize(8);
                    }
                });
            });

            // Optional: Add a separator line above the footer
            container.PaddingTop(1).BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2);
        }
    }
}