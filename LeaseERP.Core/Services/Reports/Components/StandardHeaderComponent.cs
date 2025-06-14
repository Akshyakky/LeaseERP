// LeaseERP.Core/Services/Reports/Components/StandardHeaderComponent.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LeaseERP.Core.Services.Reports.Components
{
    public class StandardHeaderComponent : IReportHeaderComponent
    {
        private readonly ILogger<StandardHeaderComponent> _logger;
        private readonly IConfiguration _configuration;

        public string ComponentName => "StandardHeader";
        public bool IsRequired => false;

        public StandardHeaderComponent(ILogger<StandardHeaderComponent> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public void Render(IContainer container, ReportData data, ReportConfiguration config)
        {
            container.Background(Colors.Grey.Lighten3).Padding(15).Column(column =>
            {
                if (config.Header.ShowCompanyInfo)
                {
                    RenderCompanyInfo(column, data, config);
                }

                if (config.Header.ShowReportTitle)
                {
                    column.Item().PaddingVertical(5).AlignCenter()
                        .Text(config.Title)
                        .FontSize(16).SemiBold()
                        .FontColor(ParseColor(config.Styling.HeaderColor));
                }

                if (config.Header.ShowGenerationInfo)
                {
                    RenderGenerationInfo(column, data, config);
                }

                if (config.Header.ShowFilters)
                {
                    RenderFilters(column, data, config);
                }
            });
        }

        private void RenderCompanyInfo(ColumnDescriptor column, ReportData data, ReportConfiguration config)
        {
            if (data.Metadata.TryGetValue("CompanyInfo", out var companyObj) && companyObj is CompanyInfo company)
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(company.CompanyName).FontSize(18).SemiBold().FontColor(ParseColor(config.Styling.HeaderColor));
                        col.Item().Text(company.CompanyAddress).FontSize(9);
                        col.Item().Text($"Phone: {company.CompanyPhone} | Email: {company.CompanyEmail}").FontSize(9);

                        if (!string.IsNullOrEmpty(company.TaxRegNo))
                        {
                            col.Item().Text($"Tax Reg No: {company.TaxRegNo}").FontSize(8);
                        }

                        if (!string.IsNullOrEmpty(company.CommercialRegNo))
                        {
                            col.Item().Text($"Commercial Reg No: {company.CommercialRegNo}").FontSize(8);
                        }
                    });

                    if (config.Header.ShowLogo)
                    {
                        row.ConstantItem(80).AlignCenter().AlignMiddle().Container().Element(c => RenderLogo(c, company, config));
                    }
                });
            }
            else
            {
                // Fallback company info if not provided in metadata
                RenderDefaultCompanyInfo(column, config);
            }
        }

        private void RenderDefaultCompanyInfo(ColumnDescriptor column, ReportConfiguration config)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("LeaseERP Solutions").FontSize(18).SemiBold().FontColor(ParseColor(config.Styling.HeaderColor));
                    col.Item().Text("123 Business Street, City, Country").FontSize(9);
                    col.Item().Text("Phone: +1-234-567-8900 | Email: info@leaseerp.com").FontSize(9);
                    col.Item().Text("Website: www.leaseerp.com").FontSize(8);
                });

                if (config.Header.ShowLogo)
                {
                    row.ConstantItem(80).AlignCenter().AlignMiddle().Container().Element(c => RenderLogoPlaceholder(c));
                }
            });
        }

        private void RenderLogo(IContainer container, CompanyInfo company, ReportConfiguration config)
        {
            if (!string.IsNullOrEmpty(company.CompanyLogo) && File.Exists(GetAbsoluteLogoPath(company.CompanyLogo)))
            {
                try
                {
                    container.MaxHeight(60).MaxWidth(75).Image(GetAbsoluteLogoPath(company.CompanyLogo)).FitArea();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load company logo from path: {LogoPath}", company.CompanyLogo);
                    RenderLogoPlaceholder(container);
                }
            }
            else
            {
                RenderLogoPlaceholder(container);
            }
        }

        private void RenderLogoPlaceholder(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Medium)
                .Background(Colors.Grey.Lighten4)
                .Padding(8).AlignCenter().AlignMiddle()
                .Text("LOGO").FontSize(8).FontColor(Colors.Grey.Darken1);
        }

        private void RenderGenerationInfo(ColumnDescriptor column, ReportData data, ReportConfiguration config)
        {
            var generatedBy = data.Metadata.GetValueOrDefault("GeneratedBy", "System")?.ToString() ?? "System";
            var generatedOn = data.Metadata.TryGetValue("GeneratedOn", out var genOnObj) && genOnObj is DateTime genOn ? genOn : DateTime.Now;

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Generated by: {generatedBy}").FontSize(8);
                row.RelativeItem().AlignRight().Text($"Generated on: {generatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }

        private void RenderFilters(ColumnDescriptor column, ReportData data, ReportConfiguration config)
        {
            if (!data.Metadata.TryGetValue("AppliedFilters", out var filtersObj) || filtersObj is not Dictionary<string, object> filters)
                return;

            var filterItems = new List<string>();

            foreach (var filter in filters)
            {
                if (filter.Value == null) continue;

                string displayValue = filter.Key switch
                {
                    "SearchText" => $"Search: {filter.Value}",
                    "CustomerName" => $"Customer: {filter.Value}",
                    "ContractStatus" => $"Status: {filter.Value}",
                    "FromDate" when filter.Value is DateTime fromDate => $"From: {fromDate:dd/MM/yyyy}",
                    "ToDate" when filter.Value is DateTime toDate => $"To: {toDate:dd/MM/yyyy}",
                    "UnitNo" => $"Unit: {filter.Value}",
                    "PropertyName" => $"Property: {filter.Value}",
                    _ => null
                };

                if (!string.IsNullOrEmpty(displayValue))
                {
                    filterItems.Add(displayValue);
                }
            }

            if (filterItems.Any())
            {
                column.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(5).Column(filterColumn =>
                {
                    filterColumn.Item().Text("Applied Filters:").FontSize(9).SemiBold();
                    filterColumn.Item().Text(string.Join(" | ", filterItems)).FontSize(8);
                });
            }
        }

        private string ParseColor(string colorCode)
        {
            // Simple color parsing - in a real implementation, you might want more sophisticated parsing
            return colorCode;
        }

        private string GetAbsoluteLogoPath(string logoPath)
        {
            if (string.IsNullOrEmpty(logoPath)) return string.Empty;

            // Handle relative paths starting with ~
            if (logoPath.StartsWith("~"))
            {
                var contentRoot = Directory.GetCurrentDirectory();
                logoPath = logoPath.Replace("~", contentRoot);
            }

            // Normalize path separators
            return logoPath.Replace("\\", Path.DirectorySeparatorChar.ToString())
                          .Replace("/", Path.DirectorySeparatorChar.ToString());
        }
    }
}