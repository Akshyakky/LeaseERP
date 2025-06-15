// LeaseERP.Core/Services/Reports/Receipts/ReceiptListTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.Receipts
{
    public class ReceiptListTemplate : BaseReportTemplate
    {
        public override string ReportType => "receipt-list";

        public ReceiptListTemplate(
            ILogger<ReceiptListTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "receipt-list", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "LEASE RECEIPT LIST REPORT",
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
                    HeaderColor = "#059669", // Green color for receipts
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var receiptListData = ParseReceiptListData(data.DataSet);
            if (receiptListData?.Receipts == null || !receiptListData.Receipts.Any())
            {
                container.AlignCenter().Padding(20).Text("No receipts found matching the specified criteria").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Filters Section
                if (config.Header.ShowFilters)
                {
                    RenderFiltersSection(column, receiptListData, config);
                    column.Item().PaddingVertical(10);
                }

                // Status Summary Cards
                RenderStatusSummaryCards(column, receiptListData, config);
                column.Item().PaddingVertical(10);

                // Receipts Table
                RenderReceiptsTable(column, receiptListData, config);

                // Summary Section
                column.Item().PaddingVertical(15);
                RenderSummarySection(column, receiptListData, config);
            });
        }

        private void RenderFiltersSection(ColumnDescriptor column, ReceiptListData receiptListData, ReportConfiguration config)
        {
            if (receiptListData.AppliedFilters == null) return;

            var filters = receiptListData.AppliedFilters;
            var hasFilters = !string.IsNullOrEmpty(filters.SearchText) ||
                           !string.IsNullOrEmpty(filters.PaymentStatus) ||
                           !string.IsNullOrEmpty(filters.PaymentType) ||
                           !string.IsNullOrEmpty(filters.CustomerName) ||
                           !string.IsNullOrEmpty(filters.PropertyName) ||
                           !string.IsNullOrEmpty(filters.UnitNo) ||
                           !string.IsNullOrEmpty(filters.BankName) ||
                           !string.IsNullOrEmpty(filters.ReceivedByUser) ||
                           filters.FromDate.HasValue ||
                           filters.ToDate.HasValue ||
                           filters.DepositFromDate.HasValue ||
                           filters.DepositToDate.HasValue ||
                           filters.PostedOnly.HasValue ||
                           filters.UnpostedOnly.HasValue ||
                           filters.AdvanceOnly.HasValue ||
                           filters.AmountFrom.HasValue ||
                           filters.AmountTo.HasValue;

            if (!hasFilters) return;

            column.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(filterColumn =>
            {
                filterColumn.Item().Text("Applied Filters:").FontSize(10).SemiBold();

                var filterItems = new List<string>();

                if (!string.IsNullOrEmpty(filters.SearchText))
                    filterItems.Add($"Search: {filters.SearchText}");

                if (!string.IsNullOrEmpty(filters.PaymentStatus))
                    filterItems.Add($"Status: {filters.PaymentStatus}");

                if (!string.IsNullOrEmpty(filters.PaymentType))
                    filterItems.Add($"Type: {filters.PaymentType}");

                if (!string.IsNullOrEmpty(filters.CustomerName))
                    filterItems.Add($"Customer: {filters.CustomerName}");

                if (!string.IsNullOrEmpty(filters.PropertyName))
                    filterItems.Add($"Property: {filters.PropertyName}");

                if (!string.IsNullOrEmpty(filters.UnitNo))
                    filterItems.Add($"Unit: {filters.UnitNo}");

                if (!string.IsNullOrEmpty(filters.BankName))
                    filterItems.Add($"Bank: {filters.BankName}");

                if (!string.IsNullOrEmpty(filters.ReceivedByUser))
                    filterItems.Add($"Received By: {filters.ReceivedByUser}");

                if (filters.FromDate.HasValue)
                    filterItems.Add($"From: {filters.FromDate.Value:dd/MM/yyyy}");

                if (filters.ToDate.HasValue)
                    filterItems.Add($"To: {filters.ToDate.Value:dd/MM/yyyy}");

                if (filters.DepositFromDate.HasValue)
                    filterItems.Add($"Deposit From: {filters.DepositFromDate.Value:dd/MM/yyyy}");

                if (filters.DepositToDate.HasValue)
                    filterItems.Add($"Deposit To: {filters.DepositToDate.Value:dd/MM/yyyy}");

                if (filters.PostedOnly == true)
                    filterItems.Add("Posted Only: Yes");

                if (filters.UnpostedOnly == true)
                    filterItems.Add("Unposted Only: Yes");

                if (filters.AdvanceOnly == true)
                    filterItems.Add("Advance Only: Yes");

                if (filters.AmountFrom.HasValue)
                    filterItems.Add($"Amount From: {filters.AmountFrom.Value:N2}");

                if (filters.AmountTo.HasValue)
                    filterItems.Add($"Amount To: {filters.AmountTo.Value:N2}");

                filterColumn.Item().Text(string.Join(" | ", filterItems)).FontSize(9);
            });
        }

        private void RenderStatusSummaryCards(ColumnDescriptor column, ReceiptListData receiptListData, ReportConfiguration config)
        {
            column.Item().Row(row =>
            {
                // Total Receipts Card
                row.RelativeItem().Border(1).BorderColor(Colors.Green.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("TOTAL RECEIPTS").FontSize(10).SemiBold().FontColor(Colors.Green.Darken2);
                    col.Item().AlignCenter().Text(receiptListData.Summary.TotalReceipts.ToString("N0"))
                        .FontSize(16).SemiBold().FontColor(Colors.Green.Darken2);
                    col.Item().AlignCenter().Text($"Value: {receiptListData.Summary.TotalReceiptValue:N2}")
                        .FontSize(8).FontColor(Colors.Green.Darken1);
                });

                // Advance Payments Card
                row.RelativeItem().Border(1).BorderColor(Colors.Blue.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("ADVANCE PAYMENTS").FontSize(10).SemiBold().FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text(receiptListData.Summary.AdvancePaymentCount.ToString("N0"))
                        .FontSize(16).SemiBold().FontColor(Colors.Blue.Darken2);
                    col.Item().AlignCenter().Text($"Amount: {receiptListData.Summary.TotalAdvanceAmount:N2}")
                        .FontSize(8).FontColor(Colors.Blue.Darken1);
                });

                // Security Deposits Card
                row.RelativeItem().Border(1).BorderColor(Colors.Purple.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("SECURITY DEPOSITS").FontSize(10).SemiBold().FontColor(Colors.Purple.Darken2);
                    col.Item().AlignCenter().Text(receiptListData.Summary.TotalSecurityDeposit.ToString("N2"))
                        .FontSize(16).SemiBold().FontColor(Colors.Purple.Darken2);
                    if (receiptListData.Summary.TotalPenaltyAmount > 0)
                    {
                        col.Item().AlignCenter().Text($"Penalties: {receiptListData.Summary.TotalPenaltyAmount:N2}")
                            .FontSize(8).FontColor(Colors.Red.Darken1);
                    }
                });

                // Pending Deposits Card
                row.RelativeItem().Border(1).BorderColor(Colors.Orange.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("PENDING DEPOSITS").FontSize(10).SemiBold().FontColor(Colors.Orange.Darken2);
                    col.Item().AlignCenter().Text(receiptListData.Summary.PendingDepositCount.ToString("N0"))
                        .FontSize(16).SemiBold().FontColor(Colors.Orange.Darken2);
                    col.Item().AlignCenter().Text($"Amount: {receiptListData.Summary.PendingDepositAmount:N2}")
                        .FontSize(8).FontColor(Colors.Orange.Darken1);
                });

                // Posted/Unposted Card
                row.RelativeItem().Border(1).BorderColor(Colors.Grey.Medium).Padding(8).Column(col =>
                {
                    col.Item().AlignCenter().Text("POSTING STATUS").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken2);
                    col.Item().AlignCenter().Text($"Posted: {receiptListData.Summary.PostedCount:N0}")
                        .FontSize(9).FontColor(Colors.Green.Darken2);
                    col.Item().AlignCenter().Text($"Unposted: {receiptListData.Summary.UnpostedCount:N0}")
                        .FontSize(9).FontColor(Colors.Orange.Darken2);
                });
            });
        }

        private void RenderReceiptsTable(ColumnDescriptor column, ReceiptListData receiptListData, ReportConfiguration config)
        {
            var tableConfig = new TableConfiguration<ReceiptSummaryInfo>
            {
                Columns = new List<TableColumn<ReceiptSummaryInfo>>
                {
                    new() { Header = "Receipt No", PropertyName = "ReceiptNo", Width = 2.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.ReceiptNo).FontSize(8) },
                    new() { Header = "Date", PropertyName = "ReceiptDate", Width = 1.8f,
                            RenderCell = (c, item, cfg) => c.Text(item.ReceiptDate.ToString("dd/MM/yyyy")).FontSize(8) },
                    new() { Header = "Customer", PropertyName = "CustomerName", Width = 3f,
                            RenderCell = (c, item, cfg) => c.Text(item.CustomerName).FontSize(8) },
                    new() { Header = "Invoice No", PropertyName = "InvoiceNo", Width = 2f,
                            RenderCell = (c, item, cfg) => c.Text(item.InvoiceNo ?? "-").FontSize(8) },
                    new() { Header = "Property", PropertyName = "PropertyName", Width = 2.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.PropertyName ?? "-").FontSize(8) },
                    new() { Header = "Unit", PropertyName = "UnitNo", Width = 1.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.UnitNo ?? "-").FontSize(8) },
                    new() { Header = "Type", PropertyName = "PaymentType", Width = 1.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.PaymentType).FontSize(8) },
                    new() { Header = "Status", PropertyName = "PaymentStatus", Width = 1.5f,
                            RenderCell = (c, item, cfg) => c.Text(item.PaymentStatus).FontSize(8)
                                .FontColor(GetStatusColor(item.PaymentStatus)) },
                    new() { Header = "Amount", PropertyName = "ReceivedAmount", Width = 2f, Alignment = TextAlignment.Right,
                            RenderCell = (c, item, cfg) => c.AlignRight().Text(item.ReceivedAmount.ToString("N2")).FontSize(8) },
                    new() { Header = "Security Dep.", PropertyName = "SecurityDepositAmount", Width = 2f, Alignment = TextAlignment.Right,
                            RenderCell = (c, item, cfg) => c.AlignRight().Text((item.SecurityDepositAmount ?? 0).ToString("N2")).FontSize(8)
                                .FontColor(item.SecurityDepositAmount > 0 ? Colors.Purple.Darken2 : Colors.Black) },
                    new() { Header = "Deposit Date", PropertyName = "DepositDate", Width = 1.8f,
                            RenderCell = (c, item, cfg) => c.Text(item.DepositDate?.ToString("dd/MM/yyyy") ?? "-").FontSize(8) },
                    new() { Header = "Flags", PropertyName = "StatusFlags", Width = 1.5f, Alignment = TextAlignment.Center,
                            RenderCell = (c, item, cfg) => RenderStatusFlags(c, item) }
                },
                ShowTotals = true,
                TotalCalculators = new Dictionary<string, Func<IEnumerable<ReceiptSummaryInfo>, string>>
                {
                    { "ReceivedAmount", receipts => receipts.Sum(r => r.ReceivedAmount).ToString("N2") },
                    { "SecurityDepositAmount", receipts => receipts.Sum(r => r.SecurityDepositAmount ?? 0).ToString("N2") }
                }
            };

            RenderTable(column.Item(), receiptListData.Receipts, tableConfig);
        }

        private void RenderStatusFlags(IContainer container, ReceiptSummaryInfo receipt)
        {
            container.Column(col =>
            {
                if (receipt.IsAdvancePayment)
                {
                    col.Item().AlignCenter().Text("A").FontColor(Colors.Blue.Darken2).FontSize(10).SemiBold();
                }
                if (receipt.RequiresDeposit)
                {
                    col.Item().AlignCenter().Text("⚠").FontColor(Colors.Orange.Darken2).FontSize(10);
                }
                if (receipt.IsPosted)
                {
                    col.Item().AlignCenter().Text("✓").FontColor(Colors.Green.Darken2).FontSize(8);
                }
                else
                {
                    col.Item().AlignCenter().Text("○").FontColor(Colors.Orange.Darken1).FontSize(8);
                }
            });
        }

        private void RenderSummarySection(ColumnDescriptor column, ReceiptListData receiptListData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(12).Row(row =>
            {
                // Financial Summary
                row.RelativeItem().Column(summaryCol =>
                {
                    summaryCol.Item().Text("FINANCIAL SUMMARY").FontSize(12).SemiBold();
                    summaryCol.Item().PaddingTop(5).Text($"Total Receipt Value: {receiptListData.Summary.TotalReceiptValue:N2}").FontSize(9);
                    summaryCol.Item().Text($"Total Security Deposits: {receiptListData.Summary.TotalSecurityDeposit:N2}").FontSize(9);
                    summaryCol.Item().Text($"Total Advance Payments: {receiptListData.Summary.TotalAdvanceAmount:N2}").FontSize(9);
                    summaryCol.Item().Text($"Total Penalty Collected: {receiptListData.Summary.TotalPenaltyAmount:N2}").FontSize(9);
                    summaryCol.Item().Text($"Total Discounts Given: {receiptListData.Summary.TotalDiscountAmount:N2}").FontSize(9);

                    var depositPercentage = receiptListData.Summary.TotalReceipts > 0
                        ? (receiptListData.Summary.PendingDepositCount / (decimal)receiptListData.Summary.TotalReceipts) * 100
                        : 0;
                    summaryCol.Item().PaddingTop(3).Text($"Pending Deposit Rate: {depositPercentage:N1}%").FontSize(9)
                        .FontColor(depositPercentage > 20 ? Colors.Red.Darken1 : depositPercentage > 10 ? Colors.Orange.Darken1 : Colors.Green.Darken2);
                });

                // Status Breakdown
                if (receiptListData.Summary.StatusBreakdown.Any())
                {
                    row.RelativeItem().Column(statusCol =>
                    {
                        statusCol.Item().Text("STATUS BREAKDOWN").FontSize(12).SemiBold();
                        statusCol.Item().PaddingTop(5);

                        foreach (var status in receiptListData.Summary.StatusBreakdown.OrderByDescending(x => x.Value))
                        {
                            var percentage = receiptListData.Summary.TotalReceipts > 0
                                ? (status.Value / (decimal)receiptListData.Summary.TotalReceipts) * 100
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

                // Payment Type Breakdown
                if (receiptListData.Summary.PaymentTypeBreakdown.Any())
                {
                    row.RelativeItem().Column(typeCol =>
                    {
                        typeCol.Item().Text("PAYMENT TYPE BREAKDOWN").FontSize(12).SemiBold();
                        typeCol.Item().PaddingTop(5);

                        foreach (var paymentType in receiptListData.Summary.PaymentTypeBreakdown.OrderByDescending(x => x.Value))
                        {
                            var percentage = receiptListData.Summary.TotalReceipts > 0
                                ? (paymentType.Value / (decimal)receiptListData.Summary.TotalReceipts) * 100
                                : 0;
                            var amount = receiptListData.Summary.PaymentTypeAmountBreakdown.GetValueOrDefault(paymentType.Key, 0);

                            typeCol.Item().Row(typeRow =>
                            {
                                typeRow.RelativeItem().Text($"{paymentType.Key}:").FontSize(9);
                                typeRow.ConstantItem(40).AlignRight().Text($"{paymentType.Value:N0}").FontSize(9);
                                typeRow.ConstantItem(50).AlignRight().Text($"({percentage:N1}%)").FontSize(8);
                            });
                            typeCol.Item().Row(amountRow =>
                            {
                                amountRow.RelativeItem();
                                amountRow.ConstantItem(90).AlignRight().Text($"Amount: {amount:N2}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });
                }

                // Performance Metrics
                row.RelativeItem().Column(metricsCol =>
                {
                    metricsCol.Item().Text("PERFORMANCE METRICS").FontSize(12).SemiBold();
                    metricsCol.Item().PaddingTop(5);

                    var postingRate = receiptListData.Summary.TotalReceipts > 0
                        ? (receiptListData.Summary.PostedCount / (decimal)receiptListData.Summary.TotalReceipts) * 100
                        : 0;
                    metricsCol.Item().Text($"Posting Rate: {postingRate:N1}%").FontSize(9)
                        .FontColor(postingRate >= 90 ? Colors.Green.Darken2 : postingRate >= 70 ? Colors.Orange.Darken1 : Colors.Red.Darken1);

                    var avgReceiptAmount = receiptListData.Summary.TotalReceipts > 0
                        ? receiptListData.Summary.TotalReceiptValue / receiptListData.Summary.TotalReceipts
                        : 0;
                    metricsCol.Item().Text($"Average Receipt: {avgReceiptAmount:N2}").FontSize(9);

                    var advanceRate = receiptListData.Summary.TotalReceipts > 0
                        ? (receiptListData.Summary.AdvancePaymentCount / (decimal)receiptListData.Summary.TotalReceipts) * 100
                        : 0;
                    metricsCol.Item().Text($"Advance Payment Rate: {advanceRate:N1}%").FontSize(9);

                    metricsCol.Item().Text($"Posted Amount: {receiptListData.Summary.PostedAmount:N2}").FontSize(9);
                    metricsCol.Item().Text($"Unposted Amount: {receiptListData.Summary.UnpostedAmount:N2}").FontSize(9);
                });
            });
        }

        private string GetStatusColor(string status)
        {
            return status?.ToUpper() switch
            {
                "RECEIVED" => Colors.Orange.Darken1,
                "DEPOSITED" => Colors.Blue.Darken1,
                "CLEARED" => Colors.Green.Darken2,
                "BOUNCED" => Colors.Red.Darken2,
                "CANCELLED" => Colors.Red.Darken1,
                "REVERSED" => Colors.Purple.Darken1,
                _ => Colors.Black
            };
        }

        private ReceiptListData ParseReceiptListData(DataSet dataSet)
        {
            var receiptListData = new ReceiptListData();

            if (dataSet?.Tables?.Count == 0) return receiptListData;

            // Parse receipts from the first table
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    receiptListData.Receipts.Add(new ReceiptSummaryInfo
                    {
                        LeaseReceiptID = Convert.ToInt64(row["LeaseReceiptID"]),
                        ReceiptNo = row["ReceiptNo"]?.ToString() ?? "",
                        ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]),
                        PaymentType = row["PaymentType"]?.ToString() ?? "",
                        PaymentStatus = row["PaymentStatus"]?.ToString() ?? "",
                        ReceivedAmount = Convert.ToDecimal(row["ReceivedAmount"]),
                        SecurityDepositAmount = row["SecurityDepositAmount"] != DBNull.Value ? Convert.ToDecimal(row["SecurityDepositAmount"]) : null,
                        PenaltyAmount = row["PenaltyAmount"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyAmount"]) : null,
                        DiscountAmount = row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : null,
                        IsAdvancePayment = Convert.ToBoolean(row["IsAdvancePayment"]),
                        TransactionReference = row["TransactionReference"]?.ToString(),
                        ChequeNo = row["ChequeNo"]?.ToString(),
                        DepositDate = row["DepositDate"] != DBNull.Value ? Convert.ToDateTime(row["DepositDate"]) : null,
                        IsPosted = Convert.ToBoolean(row["IsPosted"]),
                        Notes = row["Notes"]?.ToString(),
                        CustomerName = row["CustomerName"]?.ToString() ?? "",
                        InvoiceNo = row["InvoiceNo"]?.ToString(),
                        InvoiceDate = row["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(row["InvoiceDate"]) : null,
                        //PropertyName = row["PropertyName"]?.ToString(),
                        //UnitNo = row["UnitNo"]?.ToString(),
                        CurrencyCode = row["CurrencyCode"]?.ToString() ?? "",
                        BankName = row["BankName"]?.ToString(),
                        ReceivedByUser = row["ReceivedByUser"]?.ToString(),
                        //DaysToDeposit = row["DaysToDeposit"] != DBNull.Value ? Convert.ToInt32(row["DaysToDeposit"]) : 0,
                        //RequiresDeposit = row["RequiresDeposit"] != DBNull.Value ? Convert.ToBoolean(row["RequiresDeposit"]) : false
                    });
                }

                // Calculate summary
                receiptListData.Summary = new ReceiptListSummary
                {
                    TotalReceipts = receiptListData.Receipts.Count,
                    TotalReceiptValue = receiptListData.Receipts.Sum(r => r.ReceivedAmount),
                    TotalAdvanceAmount = receiptListData.Receipts.Where(r => r.IsAdvancePayment).Sum(r => r.ReceivedAmount),
                    TotalSecurityDeposit = receiptListData.Receipts.Sum(r => r.SecurityDepositAmount ?? 0),
                    TotalPenaltyAmount = receiptListData.Receipts.Sum(r => r.PenaltyAmount ?? 0),
                    TotalDiscountAmount = receiptListData.Receipts.Sum(r => r.DiscountAmount ?? 0),
                    PostedCount = receiptListData.Receipts.Count(r => r.IsPosted),
                    UnpostedCount = receiptListData.Receipts.Count(r => !r.IsPosted),
                    PostedAmount = receiptListData.Receipts.Where(r => r.IsPosted).Sum(r => r.ReceivedAmount),
                    UnpostedAmount = receiptListData.Receipts.Where(r => !r.IsPosted).Sum(r => r.ReceivedAmount),
                    PendingDepositCount = receiptListData.Receipts.Count(r => r.RequiresDeposit),
                    PendingDepositAmount = receiptListData.Receipts.Where(r => r.RequiresDeposit).Sum(r => r.ReceivedAmount),
                    AdvancePaymentCount = receiptListData.Receipts.Count(r => r.IsAdvancePayment),
                    StatusBreakdown = receiptListData.Receipts
                        .GroupBy(r => r.PaymentStatus)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    StatusAmountBreakdown = receiptListData.Receipts
                        .GroupBy(r => r.PaymentStatus)
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.ReceivedAmount)),
                    PaymentTypeBreakdown = receiptListData.Receipts
                        .GroupBy(r => r.PaymentType)
                        .ToDictionary(g => g.Key, g => g.Count()),
                    PaymentTypeAmountBreakdown = receiptListData.Receipts
                        .GroupBy(r => r.PaymentType)
                        .ToDictionary(g => g.Key, g => g.Sum(r => r.ReceivedAmount))
                };
            }

            return receiptListData;
        }
    }
}