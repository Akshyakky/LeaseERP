// LeaseERP.Core/Services/Reports/Invoices/InvoiceListTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.Invoices
{
    public class InvoiceListTemplate : BaseReportTemplate
    {
        public override string ReportType => "invoice-list";

        public InvoiceListTemplate(
            ILogger<InvoiceListTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "invoice-list", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "CONTRACT INVOICE LIST REPORT",
                Orientation = ReportOrientation.Landscape,
                Header = new ReportHeaderConfig
                {
                    ShowCompanyInfo = true,
                    ShowLogo = true,
                    ShowReportTitle = true,
                    ShowGenerationInfo = true,
                    ShowFilters = true,
                    Height = 140
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
                    DefaultFontSize = 9,
                    FontFamily = "Arial",
                    HeaderColor = "#1e40af",
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var invoiceListData = ParseInvoiceListData(data.DataSet);
            if (invoiceListData?.Invoices == null || !invoiceListData.Invoices.Any())
            {
                container.AlignCenter().Padding(20).Text("No invoices found matching the specified criteria").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Filters Section
                if (config.Header.ShowFilters)
                {
                    RenderFiltersSection(column, invoiceListData, config);
                    column.Item().PaddingVertical(10);
                }

                // Status Summary Cards
                RenderStatusSummaryCards(column, invoiceListData, config);
                column.Item().PaddingVertical(10);

                // Invoices Table
                RenderInvoicesTable(column, invoiceListData, config);

                // Financial Summary Section
                column.Item().PaddingVertical(15);
                RenderFinancialSummary(column, invoiceListData, config);
            });
        }

        private void RenderFiltersSection(ColumnDescriptor column, InvoiceListData invoiceListData, ReportConfiguration config)
        {
            if (invoiceListData.AppliedFilters == null) return;

            var filters = invoiceListData.AppliedFilters;
            var hasFilters = !string.IsNullOrEmpty(filters.SearchText) ||
                           !string.IsNullOrEmpty(filters.InvoiceStatus) ||
                           !string.IsNullOrEmpty(filters.InvoiceType) ||
                           !string.IsNullOrEmpty(filters.CustomerName) ||
                           !string.IsNullOrEmpty(filters.ContractNo) ||
                           !string.IsNullOrEmpty(filters.PropertyName) ||
                           !string.IsNullOrEmpty(filters.UnitNo) ||
                           filters.FromDate.HasValue ||
                           filters.ToDate.HasValue ||
                           filters.DueDateFrom.HasValue ||
                           filters.DueDateTo.HasValue ||
                           filters.PostedOnly.HasValue ||
                           filters.UnpostedOnly.HasValue ||
                           filters.OverdueOnly.HasValue;

            if (!hasFilters) return;

            column.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(filterColumn =>
            {
                filterColumn.Item().Text("Applied Filters:").FontSize(10).SemiBold();

                var filterItems = new List<string>();

                if (!string.IsNullOrEmpty(filters.SearchText))
                    filterItems.Add($"Search: {filters.SearchText}");

                if (!string.IsNullOrEmpty(filters.InvoiceStatus))
                    filterItems.Add($"Status: {filters.InvoiceStatus}");

                if (!string.IsNullOrEmpty(filters.InvoiceType))
                    filterItems.Add($"Type: {filters.InvoiceType}");

                if (!string.IsNullOrEmpty(filters.CustomerName))
                    filterItems.Add($"Customer: {filters.CustomerName}");

                if (!string.IsNullOrEmpty(filters.ContractNo))
                    filterItems.Add($"Contract: {filters.ContractNo}");

                if (!string.IsNullOrEmpty(filters.PropertyName))
                    filterItems.Add($"Property: {filters.PropertyName}");

                if (!string.IsNullOrEmpty(filters.UnitNo))
                    filterItems.Add($"Unit: {filters.UnitNo}");

                if (filters.FromDate.HasValue)
                    filterItems.Add($"From: {filters.FromDate.Value:dd/MM/yyyy}");

                if (filters.ToDate.HasValue)
                    filterItems.Add($"To: {filters.ToDate.Value:dd/MM/yyyy}");

                if (filters.DueDateFrom.HasValue)
                    filterItems.Add($"Due From: {filters.DueDateFrom.Value:dd/MM/yyyy}");

                if (filters.DueDateTo.HasValue)
                    filterItems.Add($"Due To: {filters.DueDateTo.Value:dd/MM/yyyy}");

                if (filters.PostedOnly == true)
                    filterItems.Add("Posted Only: Yes");

                if (filters.UnpostedOnly == true)
                    filterItems.Add("Unposted Only: Yes");

                if (filters.OverdueOnly == true)
                    filterItems.Add("Overdue Only: Yes");

                filterColumn.Item().Text(string.Join(" | ", filterItems)).FontSize(9);
            });
        }

        private void RenderStatusSummaryCards(ColumnDescriptor column, InvoiceListData invoiceListData, ReportConfiguration config)
        {
            column.Item().Row(row =>
            {
                // Total Invoices Card
                row.RelativeItem().Border(1).BorderColor(Colors.Blue.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("TOTAL INVOICES").FontSize(10).SemiBold().FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text(invoiceListData.Summary.TotalInvoices.ToString("N0"))
                        .FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text($"Value: {invoiceListData.Summary.TotalInvoiceValue:N2}")
                        .FontSize(8).FontColor(Colors.Blue.Darken1);
                });

                // Paid Amount Card
                row.RelativeItem().Border(1).BorderColor(Colors.Green.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("TOTAL PAID").FontSize(10).SemiBold().FontColor(Colors.Green.Darken2);
                    col.Item().AlignCenter().Text(invoiceListData.Summary.TotalPaidAmount.ToString("N2"))
                        .FontSize(16).SemiBold().FontColor(Colors.Green.Darken2);
                    var paidPercentage = invoiceListData.Summary.TotalInvoiceValue > 0
                        ? (invoiceListData.Summary.TotalPaidAmount / invoiceListData.Summary.TotalInvoiceValue) * 100
                        : 0;
                    col.Item().AlignCenter().Text($"({paidPercentage:N1}%)")
                        .FontSize(8).FontColor(Colors.Green.Darken1);
                });

                // Outstanding Balance Card
                row.RelativeItem().Border(1).BorderColor(Colors.Orange.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("OUTSTANDING").FontSize(10).SemiBold().FontColor(Colors.Orange.Darken2);
                    col.Item().AlignCenter().Text(invoiceListData.Summary.TotalBalanceAmount.ToString("N2"))
                        .FontSize(16).SemiBold().FontColor(Colors.Orange.Darken2);
                    var outstandingPercentage = invoiceListData.Summary.TotalInvoiceValue > 0
                        ? (invoiceListData.Summary.TotalBalanceAmount / invoiceListData.Summary.TotalInvoiceValue) * 100
                        : 0;
                    col.Item().AlignCenter().Text($"({outstandingPercentage:N1}%)")
                        .FontSize(8).FontColor(Colors.Orange.Darken1);
                });

                // Overdue Card
                row.RelativeItem().Border(1).BorderColor(Colors.Red.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("OVERDUE").FontSize(10).SemiBold().FontColor(Colors.Red.Darken2);
                    col.Item().AlignCenter().Text($"{invoiceListData.Summary.OverdueCount:N0}")
                        .FontSize(16).SemiBold().FontColor(Colors.Red.Darken2);
                    col.Item().AlignCenter().Text($"Amount: {invoiceListData.Summary.OverdueAmount:N2}")
                        .FontSize(8).FontColor(Colors.Red.Darken1);
                });

                // Posted/Unposted Card
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("POSTING STATUS").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                    col.Item().AlignCenter().Text($"Posted: {invoiceListData.Summary.PostedCount:N0}")
                        .FontSize(9).FontColor(Colors.Green.Darken2);
                    col.Item().AlignCenter().Text($"Unposted: {invoiceListData.Summary.UnpostedCount:N0}")
                        .FontSize(9).FontColor(Colors.Orange.Darken2);
                });
            });
        }

        private void RenderInvoicesTable(ColumnDescriptor column, InvoiceListData invoiceListData, ReportConfiguration config)
        {
            var tableConfig = new TableConfiguration<InvoiceSummaryInfo>
            {
                Columns = new List<TableColumn<InvoiceSummaryInfo>>
                {
                    new() { Header = "Invoice No", PropertyName = "InvoiceNo", Width = 2.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.InvoiceNo).FontSize(8) },
                    new() { Header = "Date", PropertyName = "InvoiceDate", Width = 2f,
                            RenderCell = (c, item, cfg) => c.Text(item.InvoiceDate.ToString("dd/MM/yyyy")).FontSize(8) },
                    new() { Header = "Due Date", PropertyName = "DueDate", Width = 2f,
                            RenderCell = (c, item, cfg) => c.Text(item.DueDate.ToString("dd/MM/yyyy")).FontSize(8) },
                    new() { Header = "Customer", PropertyName = "CustomerName", Width = 3f,
                            RenderCell = (c, item, cfg) => c.Text(item.CustomerName).FontSize(8) },
                    new() { Header = "Contract", PropertyName = "ContractNo", Width = 2f,
                            RenderCell = (c, item, cfg) => c.Text(item.ContractNo).FontSize(8) },
                    new() { Header = "Property", PropertyName = "PropertyName", Width = 2.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.PropertyName).FontSize(8) },
                    new() { Header = "Unit", PropertyName = "UnitNo", Width = 1.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.UnitNo).FontSize(8) },
                    new() { Header = "Type", PropertyName = "InvoiceType", Width = 1.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.InvoiceType).FontSize(8) },
                    new() { Header = "Status", PropertyName = "InvoiceStatus", Width = 1.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.InvoiceStatus).FontSize(8)
                                .FontColor(GetStatusColor(item.InvoiceStatus)) },
                    new() { Header = "Total", PropertyName = "TotalAmount", Width = 2f, Alignment = TextAlignment.Right,
                            RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TotalAmount.ToString("N2")).FontSize(8) },
                    new() { Header = "Paid", PropertyName = "PaidAmount", Width = 2f, Alignment = TextAlignment.Right,
                            RenderCell = (c, item, cfg) => c.AlignRight().Text(item.PaidAmount.ToString("N2")).FontSize(8)
                                .FontColor(item.PaidAmount > 0 ? Colors.Green.Darken2 : Colors.Black) },
                    new() { Header = "Balance", PropertyName = "BalanceAmount", Width = 2f, Alignment = TextAlignment.Right,
                            RenderCell = (c, item, cfg) => c.AlignRight().Text(item.BalanceAmount.ToString("N2")).FontSize(8)
                                .FontColor(item.BalanceAmount > 0 ? Colors.Red.Darken1 : Colors.Green.Darken2) },
                    new() { Header = "Flags", PropertyName = "StatusFlags", Width = 1.5f, Alignment = TextAlignment.Center,
                            RenderCell = (c, item, cfg) => RenderStatusFlags(c, item) }
                },
                ShowTotals = true,
                TotalCalculators = new Dictionary<string, Func<IEnumerable<InvoiceSummaryInfo>, string>>
                {
                    { "TotalAmount", invoices => invoices.Sum(i => i.TotalAmount).ToString("N2") },
                    { "PaidAmount", invoices => invoices.Sum(i => i.PaidAmount).ToString("N2") },
                    { "BalanceAmount", invoices => invoices.Sum(i => i.BalanceAmount).ToString("N2") }
                }
            };

            RenderTable(column.Item(), invoiceListData.Invoices, tableConfig);
        }

        private void RenderStatusFlags(IContainer container, InvoiceSummaryInfo invoice)
        {
            container.Column(col =>
            {
                if (invoice.IsOverdue)
                {
                    col.Item().AlignCenter().Text("⚠").FontColor(Colors.Red.Darken2).FontSize(10);
                }
                if (invoice.IsPosted)
                {
                    col.Item().AlignCenter().Text("✓").FontColor(Colors.Green.Darken2).FontSize(8);
                }
                if (!invoice.IsPosted)
                {
                    col.Item().AlignCenter().Text("○").FontColor(Colors.Orange.Darken1).FontSize(8);
                }
            });
        }

        private void RenderFinancialSummary(ColumnDescriptor column, InvoiceListData invoiceListData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
            {
                // Financial Summary
                row.RelativeItem().Column(summaryCol =>
                {
                    summaryCol.Item().Text("FINANCIAL SUMMARY").FontSize(12).SemiBold();
                    summaryCol.Item().PaddingTop(5).Text($"Total Invoice Value: {invoiceListData.Summary.TotalInvoiceValue:N2}").FontSize(9);
                    summaryCol.Item().Text($"Total Paid Amount: {invoiceListData.Summary.TotalPaidAmount:N2}").FontSize(9);
                    summaryCol.Item().Text($"Total Outstanding: {invoiceListData.Summary.TotalBalanceAmount:N2}").FontSize(9);
                    summaryCol.Item().Text($"Overdue Amount: {invoiceListData.Summary.OverdueAmount:N2}").FontSize(9);

                    var collectionRate = invoiceListData.Summary.TotalInvoiceValue > 0
                        ? (invoiceListData.Summary.TotalPaidAmount / invoiceListData.Summary.TotalInvoiceValue) * 100
                        : 0;
                    summaryCol.Item().PaddingTop(3).Text($"Collection Rate: {collectionRate:N1}%").FontSize(9).SemiBold()
                        .FontColor(collectionRate >= 80 ? Colors.Green.Darken2 : collectionRate >= 60 ? Colors.Orange.Darken1 : Colors.Red.Darken1);
                });

                // Status Breakdown
                if (invoiceListData.Summary.StatusBreakdown.Any())
                {
                    row.RelativeItem().Column(statusCol =>
                    {
                        statusCol.Item().Text("STATUS BREAKDOWN").FontSize(12).SemiBold();
                        statusCol.Item().PaddingTop(5);

                        foreach (var status in invoiceListData.Summary.StatusBreakdown.OrderByDescending(x => x.Value))
                        {
                            var percentage = invoiceListData.Summary.TotalInvoices > 0
                                ? (status.Value / (decimal)invoiceListData.Summary.TotalInvoices) * 100
                                : 0;
                            statusCol.Item().Row(statusRow =>
                            {
                                statusRow.RelativeItem().Text($"{status.Key}:").FontSize(9);
                                statusRow.ConstantItem(40).AlignRight().Text($"{status.Value:N0}").FontSize(9);
                                statusRow.ConstantItem(50).AlignRight().Text($"({percentage:N1}%)").FontSize(8);
                            });
                        }
                    });
                }

                // Amount Breakdown by Status
                if (invoiceListData.Summary.StatusAmountBreakdown.Any())
                {
                    row.RelativeItem().Column(amountCol =>
                    {
                        amountCol.Item().Text("AMOUNT BY STATUS").FontSize(12).SemiBold();
                        amountCol.Item().PaddingTop(5);

                        foreach (var statusAmount in invoiceListData.Summary.StatusAmountBreakdown.OrderByDescending(x => x.Value))
                        {
                            var percentage = invoiceListData.Summary.TotalInvoiceValue > 0
                                ? (statusAmount.Value / invoiceListData.Summary.TotalInvoiceValue) * 100
                                : 0;
                            amountCol.Item().Row(amountRow =>
                            {
                                amountRow.RelativeItem().Text($"{statusAmount.Key}:").FontSize(9);
                                amountRow.ConstantItem(80).AlignRight().Text($"{statusAmount.Value:N2}").FontSize(9);
                                amountRow.ConstantItem(50).AlignRight().Text($"({percentage:N1}%)").FontSize(8);
                            });
                        }
                    });
                }
            });
        }

        private string GetStatusColor(string status)
        {
            return status?.ToUpper() switch
            {
                "DRAFT" => Colors.Grey.Darken1,
                "PENDING" => Colors.Orange.Darken1,
                "APPROVED" => Colors.Blue.Darken1,
                "ACTIVE" => Colors.Green.Darken1,
                "PAID" => Colors.Green.Darken2,
                "CANCELLED" => Colors.Red.Darken1,
                "VOIDED" => Colors.Red.Darken2,
                _ => Colors.Black
            };
        }

        private InvoiceListData ParseInvoiceListData(DataSet dataSet)
        {
            var invoiceListData = new InvoiceListData();

            if (dataSet?.Tables?.Count == 0) return invoiceListData;

            // Parse invoices from the first table
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    invoiceListData.Invoices.Add(new InvoiceSummaryInfo
                    {
                        LeaseInvoiceID = Convert.ToInt64(row["LeaseInvoiceID"]),
                        InvoiceNo = row["InvoiceNo"]?.ToString() ?? "",
                        InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                        DueDate = Convert.ToDateTime(row["DueDate"]),
                        InvoiceType = row["InvoiceType"]?.ToString() ?? "",
                        InvoiceStatus = row["InvoiceStatus"]?.ToString() ?? "",
                        PeriodFromDate = row["PeriodFromDate"] != DBNull.Value ? Convert.ToDateTime(row["PeriodFromDate"]) : null,
                        PeriodToDate = row["PeriodToDate"] != DBNull.Value ? Convert.ToDateTime(row["PeriodToDate"]) : null,
                        TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                        PaidAmount = Convert.ToDecimal(row["PaidAmount"]),
                        BalanceAmount = Convert.ToDecimal(row["BalanceAmount"]),
                        CustomerName = row["CustomerName"]?.ToString() ?? "",
                        ContractNo = row["ContractNo"]?.ToString() ?? "",
                        UnitNo = row["UnitNo"]?.ToString() ?? "",
                        PropertyName = row["PropertyName"]?.ToString() ?? "",
                        CurrencyCode = row["CurrencyCode"]?.ToString() ?? "",
                        IsOverdue = Convert.ToBoolean(row["IsOverdue"]),
                        DaysOverdue = Convert.ToInt32(row["DaysOverdue"]),
                        IsPosted = Convert.ToBoolean(row["IsPosted"])
                    });
                }

                // Calculate summary
                invoiceListData.Summary = new InvoiceListSummary
                {
                    TotalInvoices = invoiceListData.Invoices.Count,
                    TotalInvoiceValue = invoiceListData.Invoices.Sum(i => i.TotalAmount),
                    TotalPaidAmount = invoiceListData.Invoices.Sum(i => i.PaidAmount),
                    TotalBalanceAmount = invoiceListData.Invoices.Sum(i => i.BalanceAmount),
                    OverdueCount = invoiceListData.Invoices.Count(i => i.IsOverdue),
                    OverdueAmount = invoiceListData.Invoices.Where(i => i.IsOverdue).Sum(i => i.BalanceAmount),
                    PostedCount = invoiceListData.Invoices.Count(i => i.IsPosted),
                    UnpostedCount = invoiceListData.Invoices.Count(i => !i.IsPosted),
                    StatusBreakdown = invoiceListData.Invoices
                        .GroupBy(i => i.InvoiceStatus)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    StatusAmountBreakdown = invoiceListData.Invoices
                        .GroupBy(i => i.InvoiceStatus)
                        .ToDictionary(g => g.Key, g => g.Sum(i => i.TotalAmount))
                };
            }

            return invoiceListData;
        }
    }
}