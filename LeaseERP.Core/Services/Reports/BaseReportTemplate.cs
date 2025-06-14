// LeaseERP.Core/Services/Reports/BaseReportTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LeaseERP.Core.Services.Reports
{
    public abstract class BaseReportTemplate : IReportTemplate
    {
        protected readonly ILogger _logger;
        protected readonly IConfiguration _configuration;
        protected readonly IEnumerable<IReportComponent> _components;

        protected BaseReportTemplate(
            ILogger logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
        {
            _logger = logger;
            _configuration = configuration;
            _components = components;
        }

        public abstract string ReportType { get; }
        public abstract bool CanHandle(string reportType);
        public abstract ReportConfiguration GetDefaultConfiguration();
        protected abstract void RenderContent(IContainer container, ReportData data, ReportConfiguration config);

        public virtual IDocument CreateDocument(ReportData data, ReportConfiguration config)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    ConfigurePageSettings(page, config);

                    // Header
                    if (config.Header.ShowCompanyInfo || config.Header.ShowReportTitle)
                    {
                        page.Header().Height(config.Header.Height).Container().Element(h => RenderHeader(h, data, config));
                    }

                    // Content
                    page.Content().Element(c => RenderContent(c, data, config));

                    // Footer
                    if (config.Footer.ShowPageNumbers || config.Footer.ShowGenerationInfo)
                    {
                        page.Footer().Height(config.Footer.Height).Container().Element(f => RenderFooter(f, data, config));
                    }
                });
            });
        }

        protected virtual void ConfigurePageSettings(PageDescriptor page, ReportConfiguration config)
        {
            page.Size(config.Orientation == ReportOrientation.Portrait ? PageSizes.A4 : PageSizes.A4.Landscape());
            page.Margin(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(config.Styling.DefaultFontSize).FontFamily(GetFontFamily(config.Styling.FontFamily)));
        }

        protected virtual void RenderHeader(IContainer container, ReportData data, ReportConfiguration config)
        {
            var headerComponent = _components.OfType<IReportHeaderComponent>().FirstOrDefault();
            if (headerComponent != null)
            {
                headerComponent.Render(container, data, config);
            }
            else
            {
                RenderDefaultHeader(container, data, config);
            }
        }

        protected virtual void RenderFooter(IContainer container, ReportData data, ReportConfiguration config)
        {
            var footerComponent = _components.OfType<IReportFooterComponent>().FirstOrDefault();
            if (footerComponent != null)
            {
                footerComponent.Render(container, data, config);
            }
            else
            {
                RenderDefaultFooter(container, data, config);
            }
        }

        protected virtual void RenderDefaultHeader(IContainer container, ReportData data, ReportConfiguration config)
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
            });
        }

        protected virtual void RenderDefaultFooter(IContainer container, ReportData data, ReportConfiguration config)
        {
            container.Background(Colors.Grey.Lighten4).AlignCenter().Text(x =>
            {
                if (config.Footer.ShowPageNumbers)
                {
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                }

                if (config.Footer.ShowReportTitle && config.Footer.ShowPageNumbers)
                {
                    x.Span(" | ");
                }

                if (config.Footer.ShowReportTitle)
                {
                    x.Span(config.Title);
                }

                if (config.Footer.ShowGenerationInfo)
                {
                    x.Span($" | Generated: {DateTime.Now:dd/MM/yyyy HH:mm}");
                }
            });
        }

        protected virtual void RenderCompanyInfo(ColumnDescriptor column, ReportData data, ReportConfiguration config)
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
                    });

                    if (config.Header.ShowLogo)
                    {
                        row.ConstantItem(80).AlignCenter().AlignMiddle().Container().Element(c => RenderLogo(c, company, config));
                    }
                });
            }
        }

        protected virtual void RenderLogo(IContainer container, CompanyInfo company, ReportConfiguration config)
        {
            if (!string.IsNullOrEmpty(company.CompanyLogo) && File.Exists(GetAbsoluteLogoPath(company.CompanyLogo)))
            {
                try
                {
                    container.MaxHeight(60).MaxWidth(75).Image(GetAbsoluteLogoPath(company.CompanyLogo)).FitArea();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load company logo");
                    RenderLogoPlaceholder(container);
                }
            }
            else
            {
                RenderLogoPlaceholder(container);
            }
        }

        protected virtual void RenderLogoPlaceholder(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Grey.Medium)
                .Background(Colors.Grey.Lighten4)
                .Padding(8).AlignCenter().AlignMiddle()
                .Text("LOGO").FontSize(8).FontColor(Colors.Grey.Darken1);
        }

        protected virtual void RenderGenerationInfo(ColumnDescriptor column, ReportData data, ReportConfiguration config)
        {
            var generatedBy = data.Metadata.GetValueOrDefault("GeneratedBy", "System")?.ToString() ?? "System";
            var generatedOn = DateTime.Now;

            column.Item().PaddingTop(5).Row(row =>
            {
                row.RelativeItem().Text($"Generated by: {generatedBy}").FontSize(8);
                row.RelativeItem().AlignRight().Text($"Generated on: {generatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        }

        protected virtual string GetFontFamily(string fontFamily)
        {
            return fontFamily.ToLower() switch
            {
                "arial" => Fonts.Arial,
                "verdana" => Fonts.Verdana,
                "times" => Fonts.TimesRoman,
                "courier" => Fonts.Courier,
                _ => Fonts.Arial
            };
        }

        protected virtual string ParseColor(string colorCode)
        {
            return colorCode;
        }

        protected virtual string GetAbsoluteLogoPath(string logoPath)
        {
            if (string.IsNullOrEmpty(logoPath)) return string.Empty;

            if (logoPath.StartsWith("~"))
            {
                var contentRoot = Directory.GetCurrentDirectory();
                logoPath = logoPath.Replace("~", contentRoot);
            }

            return logoPath.Replace("\\", Path.DirectorySeparatorChar.ToString())
                          .Replace("/", Path.DirectorySeparatorChar.ToString());
        }

        protected virtual void RenderTable<T>(IContainer container, IEnumerable<T> data, TableConfiguration<T> tableConfig)
        {
            if (!data.Any())
            {
                container.AlignCenter().Padding(20)
                    .Text("No data available")
                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                return;
            }

            container.Table(table =>
            {
                // Define columns
                table.ColumnsDefinition(columns =>
                {
                    foreach (var column in tableConfig.Columns)
                    {
                        if (column.IsConstantWidth)
                            columns.ConstantColumn(column.Width);
                        else
                            columns.RelativeColumn(column.Width);
                    }
                });

                // Header
                table.Header(header =>
                {
                    foreach (var column in tableConfig.Columns)
                    {
                        header.Cell()
                            .Background(ParseColor(tableConfig.HeaderBackgroundColor))
                            .Padding(3)
                            .Text(column.Header)
                            .FontColor(Colors.White)
                            .SemiBold()
                            .FontSize(tableConfig.HeaderFontSize);
                    }
                });

                // Data rows
                bool isAlternate = false;
                foreach (var item in data)
                {
                    foreach (var column in tableConfig.Columns)
                    {
                        var cell = table.Cell();

                        if (tableConfig.AlternateRowColors && isAlternate)
                            cell.Background(Colors.Grey.Lighten5);

                        cell.BorderBottom(tableConfig.BorderWidth)
                            .Padding(3)
                            .Element(c => column.RenderCell(c, item, tableConfig));
                    }
                    isAlternate = !isAlternate;
                }

                // Footer if configured
                if (tableConfig.ShowTotals && tableConfig.TotalCalculators.Any())
                {
                    RenderTableTotals(table, data, tableConfig);
                }
            });
        }

        protected virtual void RenderTableTotals<T>(TableDescriptor table, IEnumerable<T> data, TableConfiguration<T> tableConfig)
        {
            foreach (var column in tableConfig.Columns)
            {
                var cell = table.Cell().Background(Colors.Grey.Lighten2).Padding(3);

                if (tableConfig.TotalCalculators.TryGetValue(column.PropertyName, out var calculator))
                {
                    var totalValue = calculator(data);
                    cell.AlignRight().Text(totalValue).SemiBold().FontSize(8);
                }
                else if (column == tableConfig.Columns.First())
                {
                    cell.Text("TOTALS").SemiBold().FontSize(8);
                }
            }
        }
    }

    // Table configuration classes
    public class TableConfiguration<T>
    {
        public List<TableColumn<T>> Columns { get; set; } = new();
        public string HeaderBackgroundColor { get; set; } = "#1e40af";
        public int HeaderFontSize { get; set; } = 9;
        public float BorderWidth { get; set; } = 0.5f;
        public bool AlternateRowColors { get; set; } = true;
        public bool ShowTotals { get; set; } = false;
        public Dictionary<string, Func<IEnumerable<T>, string>> TotalCalculators { get; set; } = new();
    }

    public class TableColumn<T>
    {
        public string Header { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public float Width { get; set; } = 1;
        public bool IsConstantWidth { get; set; } = false;
        public TextAlignment Alignment { get; set; } = TextAlignment.Left;
        public string Format { get; set; } = string.Empty;
        public Action<IContainer, T, TableConfiguration<T>> RenderCell { get; set; } = DefaultCellRenderer;

        public static void DefaultCellRenderer(IContainer container, T item, TableConfiguration<T> config)
        {
            // Default implementation - would need reflection or expression trees for property access
            container.Text(item?.ToString() ?? "");
        }
    }

    public enum TextAlignment
    {
        Left,
        Center,
        Right
    }
}
