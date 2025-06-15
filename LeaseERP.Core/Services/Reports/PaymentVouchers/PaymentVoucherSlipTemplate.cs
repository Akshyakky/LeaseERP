// LeaseERP.Core/Services/Reports/PaymentVouchers/PaymentVoucherSlipTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.PaymentVouchers
{
    public class PaymentVoucherSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "payment-voucher-slip";

        public PaymentVoucherSlipTemplate(
            ILogger<PaymentVoucherSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "payment-voucher-slip", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(reportType, "paymentvoucher-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "PAYMENT VOUCHER",
                Orientation = ReportOrientation.Portrait,
                Header = new ReportHeaderConfig
                {
                    ShowCompanyInfo = true,
                    ShowLogo = true,
                    ShowReportTitle = true,
                    ShowGenerationInfo = false,
                    ShowFilters = false,
                    Height = 120
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
                    DefaultFontSize = 10,
                    FontFamily = "Arial",
                    HeaderColor = "#dc2626", // Red color for payments (money going out)
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var paymentData = ParsePaymentVoucherData(data.DataSet);
            if (paymentData?.Voucher == null)
            {
                container.AlignCenter().Padding(20).Text("Payment voucher data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Voucher Header Information
                RenderVoucherHeader(column, paymentData, config);

                column.Item().PaddingVertical(10);

                // Payment and Supplier Information
                RenderPaymentSupplierInfo(column, paymentData, config);

                column.Item().PaddingVertical(10);

                // Payment Method Details
                RenderPaymentMethodDetails(column, paymentData, config);

                column.Item().PaddingVertical(10);

                // Voucher Lines Section
                RenderVoucherLines(column, paymentData, config);

                column.Item().PaddingVertical(10);

                // Financial Summary Section
                RenderFinancialSummary(column, paymentData, config);

                // Attachments Section
                if (paymentData.Attachments.Any())
                {
                    column.Item().PaddingVertical(10);
                    RenderAttachmentsSection(column, paymentData, config);
                }

                // Notes Section
                if (!string.IsNullOrEmpty(paymentData.Voucher.Narration))
                {
                    column.Item().PaddingVertical(10);
                    RenderNotesSection(column, paymentData, config);
                }

                // Approval and Signature Section
                column.Item().PaddingVertical(20);
                RenderApprovalSection(column, paymentData, config);
            });
        }

        private void RenderVoucherHeader(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Background(GetStatusBackgroundColor(paymentData.Voucher.PaymentStatus))
                .Padding(12).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Voucher No: {paymentData.Voucher.VoucherNo}")
                            .SemiBold().FontSize(14).FontColor(GetStatusTextColor(paymentData.Voucher.PaymentStatus));
                        col.Item().Text($"Voucher Type: {paymentData.Voucher.VoucherType}").FontSize(10);
                        col.Item().Text($"Status: {paymentData.Voucher.PaymentStatus}")
                            .SemiBold().FontColor(GetStatusColor(paymentData.Voucher.PaymentStatus));
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Transaction Date: {paymentData.Voucher.TransactionDate:dd/MM/yyyy}").FontSize(10);
                        col.Item().Text($"Posting Date: {paymentData.Voucher.PostingDate:dd/MM/yyyy}").FontSize(10);
                        col.Item().Text($"Payment Type: {paymentData.Voucher.PaymentType}").FontSize(10);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Currency: {paymentData.Voucher.CurrencyName}").FontSize(10);
                        if (paymentData.Voucher.ExchangeRate != 1)
                        {
                            col.Item().AlignRight().Text($"Exchange Rate: {paymentData.Voucher.ExchangeRate:N4}").FontSize(9);
                        }
                        col.Item().AlignRight().Text($"FY: {paymentData.Voucher.FYDescription}").FontSize(9);
                    });
                });
        }

        private void RenderPaymentSupplierInfo(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Border(1).BorderColor(ParseColor(config.Styling.BorderColor))
                .Padding(12).Column(infoColumn =>
                {
                    infoColumn.Item().Text("PAYMENT & PAYEE INFORMATION")
                        .FontSize(12).SemiBold().FontColor(ParseColor(config.Styling.HeaderColor));

                    infoColumn.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Payment Information").FontSize(10).SemiBold();
                            col.Item().Text($"Company: {paymentData.Voucher.CompanyName}").FontSize(9);
                            col.Item().Text($"Payment Account: {paymentData.Voucher.PaymentAccountName}").FontSize(9);
                            col.Item().Text($"Account Code: {paymentData.Voucher.PaymentAccountCode}").FontSize(9);
                            if (!string.IsNullOrEmpty(paymentData.Voucher.RefNo))
                            {
                                col.Item().Text($"Reference No: {paymentData.Voucher.RefNo}").FontSize(9);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Payee Information").FontSize(10).SemiBold();
                            if (!string.IsNullOrEmpty(paymentData.Voucher.SupplierName))
                            {
                                col.Item().Text($"Supplier: {paymentData.Voucher.SupplierName}").FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(paymentData.Voucher.PaidTo))
                            {
                                col.Item().Text($"Paid To: {paymentData.Voucher.PaidTo}").FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(paymentData.Voucher.ReferenceType))
                            {
                                col.Item().Text($"Ref Type: {paymentData.Voucher.ReferenceType}").FontSize(9);
                                if (!string.IsNullOrEmpty(paymentData.Voucher.ReferenceNo))
                                {
                                    col.Item().Text($"Ref No: {paymentData.Voucher.ReferenceNo}").FontSize(9);
                                }
                            }
                        });
                    });
                });
        }

        private void RenderPaymentMethodDetails(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Column(methodColumn =>
            {
                methodColumn.Item().Text("PAYMENT METHOD DETAILS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                methodColumn.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Payment Method").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Cheque/Reference Details").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Bank Information").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Amount Details").FontColor(Colors.White).FontSize(9).SemiBold();
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        col.Item().Text(paymentData.Voucher.PaymentType).FontSize(9).SemiBold();
                        col.Item().Text($"Status: {paymentData.Voucher.PaymentStatus}")
                            .FontSize(8).FontColor(GetStatusColor(paymentData.Voucher.PaymentStatus));
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        if (!string.IsNullOrEmpty(paymentData.Voucher.ChequeNo))
                        {
                            col.Item().Text($"Cheque No: {paymentData.Voucher.ChequeNo}").FontSize(9);
                            if (paymentData.Voucher.ChequeDate.HasValue)
                            {
                                col.Item().Text($"Date: {paymentData.Voucher.ChequeDate.Value:dd/MM/yyyy}").FontSize(8);
                            }
                        }
                        else
                        {
                            col.Item().Text("N/A").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        if (!string.IsNullOrEmpty(paymentData.Voucher.BankName))
                        {
                            col.Item().Text(paymentData.Voucher.BankName).FontSize(9);
                        }
                        else
                        {
                            col.Item().Text("N/A").FontSize(9).FontColor(Colors.Grey.Darken1);
                        }
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        col.Item().Text($"Total: {paymentData.Voucher.CurrencyName} {paymentData.Voucher.TotalAmount:N2}")
                            .FontSize(9).SemiBold();
                        if (paymentData.Voucher.ExchangeRate != 1)
                        {
                            var baseAmount = paymentData.Voucher.TotalAmount * paymentData.Voucher.ExchangeRate;
                            col.Item().Text($"Base: {baseAmount:N2}").FontSize(8);
                        }
                    });
                });
            });
        }

        private void RenderVoucherLines(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Column(linesColumn =>
            {
                linesColumn.Item().Text("PAYMENT BREAKDOWN").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                linesColumn.Item().PaddingTop(5);

                var tableConfig = new TableConfiguration<PaymentVoucherLineInfo>
                {
                    Columns = new List<TableColumn<PaymentVoucherLineInfo>>
                    {
                        new() { Header = "Account", PropertyName = "AccountName", Width = 3,
                                RenderCell = (c, item, cfg) => c.Column(col =>
                                {
                                    col.Item().Text(item.AccountName).FontSize(9);
                                    col.Item().Text($"({item.AccountCode})").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }) },
                        new() { Header = "Description", PropertyName = "LineDescription", Width = 3,
                                RenderCell = (c, item, cfg) => c.Text(item.LineDescription).FontSize(9) },
                        new() { Header = "Cost Center", PropertyName = "CostCenter1Name", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.CostCenter1Name ?? "").FontSize(8) },
                        new() { Header = "Party", PropertyName = "CustomerFullName", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.CustomerFullName ?? item.SupplierName ?? "").FontSize(8) },
                        new() { Header = "Debit", PropertyName = "DebitAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight()
                                    .Text(item.DebitAmount > 0 ? $"{paymentData.Voucher.CurrencyName} {item.DebitAmount:N2}" : "").FontSize(9) },
                        new() { Header = "Credit", PropertyName = "CreditAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight()
                                    .Text(item.CreditAmount > 0 ? $"{paymentData.Voucher.CurrencyName} {item.CreditAmount:N2}" : "").FontSize(9) }
                    },
                    ShowTotals = true,
                    TotalCalculators = new Dictionary<string, Func<IEnumerable<PaymentVoucherLineInfo>, string>>
                    {
                        { "DebitAmount", lines => $"{paymentData.Voucher.CurrencyName} {lines.Sum(l => l.DebitAmount):N2}" },
                        { "CreditAmount", lines => $"{paymentData.Voucher.CurrencyName} {lines.Sum(l => l.CreditAmount):N2}" }
                    }
                };

                RenderTable(linesColumn.Item(), paymentData.Lines, tableConfig);
            });
        }

        private void RenderFinancialSummary(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Border(2).BorderColor(ParseColor(config.Styling.HeaderColor))
                .Padding(15).Column(summaryColumn =>
                {
                    summaryColumn.Item().Text("FINANCIAL SUMMARY").FontSize(14).SemiBold()
                        .FontColor(ParseColor(config.Styling.HeaderColor)).AlignCenter();

                    summaryColumn.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem(3);
                        row.RelativeItem(2).Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total Debits:");
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{paymentData.Voucher.CurrencyName} {paymentData.Voucher.TotalDebitAmount:N2}");
                            });

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total Credits:");
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{paymentData.Voucher.CurrencyName} {paymentData.Voucher.TotalCreditAmount:N2}");
                            });

                            col.Item().BorderTop(1).PaddingTop(8);

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("PAYMENT AMOUNT:").SemiBold().FontSize(12);
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{paymentData.Voucher.CurrencyName} {paymentData.Voucher.TotalAmount:N2}")
                                    .SemiBold().FontSize(12).FontColor(ParseColor(config.Styling.HeaderColor));
                            });

                            col.Item().BorderTop(1).PaddingTop(5);

                            var balanceDifference = Math.Abs(paymentData.Voucher.TotalDebitAmount - paymentData.Voucher.TotalCreditAmount);
                            var isBalanced = balanceDifference < 0.01m;

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("BALANCE CHECK:").SemiBold();
                                r.ConstantItem(120).AlignRight()
                                    .Text(isBalanced ? "BALANCED" : $"DIFF: {balanceDifference:N2}")
                                    .SemiBold().FontColor(isBalanced ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            });

                            // Exchange rate information
                            if (paymentData.Voucher.ExchangeRate != 1)
                            {
                                col.Item().PaddingTop(8).Text("Exchange Rate Information").FontSize(10).SemiBold();
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Rate:");
                                    r.ConstantItem(120).AlignRight().Text($"1 = {paymentData.Voucher.ExchangeRate:N4}").FontSize(9);
                                });
                                var baseAmount = paymentData.Voucher.TotalAmount * paymentData.Voucher.ExchangeRate;
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Base Currency Amount:");
                                    r.ConstantItem(120).AlignRight().Text($"{baseAmount:N2}").FontSize(9);
                                });
                            }
                        });
                    });
                });
        }

        private void RenderAttachmentsSection(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Column(attachColumn =>
            {
                attachColumn.Item().Text("SUPPORTING DOCUMENTS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                attachColumn.Item().PaddingTop(5);

                foreach (var attachment in paymentData.Attachments.OrderBy(a => a.DisplayOrder))
                {
                    attachColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"• {attachment.DocumentName}").FontSize(9);
                        row.ConstantItem(100).Text($"({attachment.DocTypeName})").FontSize(8).FontColor(Colors.Grey.Darken1);
                        if (attachment.FileSize.HasValue)
                        {
                            var sizeKB = attachment.FileSize.Value / 1024.0;
                            row.ConstantItem(60).AlignRight().Text($"{sizeKB:N1} KB").FontSize(8).FontColor(Colors.Grey.Darken1);
                        }
                        if (attachment.IsRequired)
                        {
                            row.ConstantItem(30).AlignRight().Text("*").FontColor(Colors.Red.Darken2).FontSize(10);
                        }
                    });

                    if (!string.IsNullOrEmpty(attachment.DocumentDescription))
                    {
                        attachColumn.Item().PaddingLeft(15).Text(attachment.DocumentDescription).FontSize(8).FontColor(Colors.Grey.Darken2);
                    }
                }

                if (paymentData.Attachments.Any(a => a.IsRequired))
                {
                    attachColumn.Item().PaddingTop(5).Text("* Required documents").FontSize(8).FontColor(Colors.Red.Darken2);
                }
            });
        }

        private void RenderNotesSection(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Column(notesColumn =>
            {
                notesColumn.Item().Text("NARRATION / DESCRIPTION").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));
                notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                    .Text(paymentData.Voucher.Narration).FontSize(9);
            });
        }

        private void RenderApprovalSection(ColumnDescriptor column, PaymentVoucherSlipData paymentData, ReportConfiguration config)
        {
            column.Item().Column(approvalColumn =>
            {
                // Status information
                approvalColumn.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Prepared by: {paymentData.Voucher.CreatedBy}").FontSize(9);
                        col.Item().Text($"Prepared on: {paymentData.Voucher.CreatedOn:dd/MM/yyyy HH:mm}").FontSize(9);
                        if (!string.IsNullOrEmpty(paymentData.Voucher.UpdatedBy))
                        {
                            col.Item().Text($"Last updated by: {paymentData.Voucher.UpdatedBy}").FontSize(8);
                            col.Item().Text($"Updated on: {paymentData.Voucher.UpdatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
                        }
                        if (!string.IsNullOrEmpty(paymentData.Voucher.ApprovedBy))
                        {
                            col.Item().Text($"Approved by: {paymentData.Voucher.ApprovedBy}").FontSize(8);
                            col.Item().Text($"Approved on: {paymentData.Voucher.ApprovedOn:dd/MM/yyyy HH:mm}").FontSize(8);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Status: {paymentData.Voucher.PaymentStatus}")
                            .SemiBold().FontColor(GetStatusColor(paymentData.Voucher.PaymentStatus));

                        if (paymentData.Voucher.PaymentStatus == "Paid")
                        {
                            col.Item().AlignRight().Text("✓ PAYMENT COMPLETED").FontColor(Colors.Green.Darken2).FontSize(9);
                        }
                        else if (paymentData.Voucher.PaymentStatus == "Pending")
                        {
                            col.Item().AlignRight().Text("⏳ PENDING APPROVAL").FontColor(Colors.Orange.Darken2).FontSize(9);
                        }
                        else if (paymentData.Voucher.PaymentStatus == "Draft")
                        {
                            col.Item().AlignRight().Text("📝 DRAFT STATUS").FontColor(Colors.Grey.Darken2).FontSize(9);
                        }
                    });
                });

                // Signature section
                approvalColumn.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                            .Text("Requested By").FontSize(10);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text($"({paymentData.Voucher.CreatedBy})").FontSize(9);
                        col.Item().AlignCenter().Text("Date: ____________________").FontSize(9);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                            .Text("Approved By").FontSize(10);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text("____________________").FontSize(9);
                        col.Item().AlignCenter().Text("Date: ____________________").FontSize(9);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                            .Text("Paid By").FontSize(10);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text("____________________").FontSize(9);
                        col.Item().AlignCenter().Text("Date: ____________________").FontSize(9);
                    });
                });

                // Terms and conditions
                approvalColumn.Item().PaddingTop(20).Column(termsColumn =>
                {
                    termsColumn.Item().Text("TERMS & CONDITIONS").FontSize(10).SemiBold();

                    var terms = new[]
                    {
                        "1. All supporting documents must be attached to this voucher.",
                        "2. This voucher is valid only when properly approved and processed.",
                        "3. Any alterations must be initialed by the preparer and approver.",
                        "4. Payment should only be made after proper verification of supporting documents.",
                        "5. This is a computer generated voucher."
                    };

                    foreach (var term in terms)
                    {
                        termsColumn.Item().Text($"{term}").FontSize(8);
                    }
                });
            });
        }

        private string GetStatusColor(string status)
        {
            return status?.ToUpper() switch
            {
                "DRAFT" => Colors.Grey.Darken1,
                "PENDING" => Colors.Orange.Darken1,
                "PAID" => Colors.Green.Darken2,
                "APPROVED" => Colors.Blue.Darken1,
                "REJECTED" => Colors.Red.Darken1,
                "CANCELLED" => Colors.Red.Darken2,
                _ => Colors.Black
            };
        }

        private string GetStatusBackgroundColor(string status)
        {
            return status?.ToUpper() switch
            {
                "DRAFT" => Colors.Grey.Lighten4,
                "PENDING" => Colors.Orange.Lighten4,
                "PAID" => Colors.Green.Lighten4,
                "APPROVED" => Colors.Blue.Lighten4,
                "REJECTED" => Colors.Red.Lighten4,
                "CANCELLED" => Colors.Red.Lighten4,
                _ => Colors.Grey.Lighten5
            };
        }

        private string GetStatusTextColor(string status)
        {
            return status?.ToUpper() switch
            {
                "PAID" => Colors.Green.Darken2,
                "REJECTED" => Colors.Red.Darken2,
                "CANCELLED" => Colors.Red.Darken2,
                _ => Colors.Black
            };
        }

        private PaymentVoucherSlipData ParsePaymentVoucherData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 3) return null;

            var paymentVoucherSlipData = new PaymentVoucherSlipData();

            // Parse voucher header data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                paymentVoucherSlipData.Voucher = new PaymentVoucherInfo
                {
                    //PostingID = Convert.ToInt64(row["PostingID"]),
                    VoucherNo = row["VoucherNo"]?.ToString() ?? "",
                    VoucherType = row["VoucherType"]?.ToString() ?? "",
                    TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                    PostingDate = Convert.ToDateTime(row["PostingDate"]),
                    CompanyID = Convert.ToInt64(row["CompanyID"]),
                    FiscalYearID = Convert.ToInt64(row["FiscalYearID"]),
                    CurrencyID = Convert.ToInt64(row["CurrencyID"]),
                    ExchangeRate = Convert.ToDecimal(row["ExchangeRate"]),
                    //Description = row["Description"]?.ToString() ?? "",
                    Narration = row["Narration"]?.ToString() ?? "",
                    PaymentType = row["PaymentType"]?.ToString() ?? "",
                    PaymentAccountID = Convert.ToInt64(row["PaymentAccountID"]),
                    SupplierID = row["SupplierID"] != DBNull.Value ? Convert.ToInt64(row["SupplierID"]) : null,
                    //PaidTo = row["PaidTo"]?.ToString(),
                    //RefNo = row["RefNo"]?.ToString(),
                    ChequeNo = row["ChequeNo"]?.ToString(),
                    ChequeDate = row["ChequeDate"] != DBNull.Value ? Convert.ToDateTime(row["ChequeDate"]) : null,
                    BankID = row["BankID"] != DBNull.Value ? Convert.ToInt64(row["BankID"]) : null,
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    //PaymentStatus = row["PaymentStatus"]?.ToString() ?? "",
                    ReferenceType = row["ReferenceType"]?.ToString(),
                    ReferenceID = row["ReferenceID"] != DBNull.Value ? Convert.ToInt64(row["ReferenceID"]) : null,
                    ReferenceNo = row["ReferenceNo"]?.ToString(),

                    // Related entity information
                    CompanyName = row["CompanyName"]?.ToString() ?? "",
                    FYDescription = row["FYDescription"]?.ToString() ?? "",
                    CurrencyName = row["CurrencyName"]?.ToString() ?? "",
                    PaymentAccountName = row["PaymentAccountName"]?.ToString() ?? "",
                    //PaymentAccountCode = row["PaymentAccountCode"]?.ToString() ?? "",
                    SupplierName = row["SupplierName"]?.ToString(),
                    BankName = row["BankName"]?.ToString(),

                    // Audit information
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString(),
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null,
                    //ApprovedBy = row["ApprovedBy"]?.ToString(),
                    //ApprovedOn = row["ApprovedOn"] != DBNull.Value ? Convert.ToDateTime(row["ApprovedOn"]) : null
                };
            }

            // Parse voucher lines data (Table 1)
            if (dataSet.Tables.Count > 1)
            {
                ParseVoucherLinesData(paymentVoucherSlipData, dataSet.Tables[1]);
            }

            // Parse attachments data (Table 2)
            if (dataSet.Tables.Count > 2)
            {
                ParseAttachmentsData(paymentVoucherSlipData, dataSet.Tables[2]);
            }

            // Calculate totals
            if (paymentVoucherSlipData.Lines.Any())
            {
                paymentVoucherSlipData.Voucher.TotalDebitAmount = paymentVoucherSlipData.Lines.Sum(l => l.DebitAmount);
                paymentVoucherSlipData.Voucher.TotalCreditAmount = paymentVoucherSlipData.Lines.Sum(l => l.CreditAmount);
            }

            return paymentVoucherSlipData;
        }

        private void ParseVoucherLinesData(PaymentVoucherSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Lines.Add(new PaymentVoucherLineInfo
                {
                    PostingID = Convert.ToInt64(row["PostingID"]),
                    AccountID = Convert.ToInt64(row["AccountID"]),
                    TransactionType = row["TransactionType"]?.ToString() ?? "",
                    DebitAmount = Convert.ToDecimal(row["DebitAmount"]),
                    CreditAmount = Convert.ToDecimal(row["CreditAmount"]),
                    BaseAmount = Convert.ToDecimal(row["BaseAmount"]),
                    TaxPercentage = row["TaxPercentage"] != DBNull.Value ? Convert.ToDecimal(row["TaxPercentage"]) : null,
                    LineTaxAmount = row["LineTaxAmount"] != DBNull.Value ? Convert.ToDecimal(row["LineTaxAmount"]) : null,
                    LineDescription = row["LineDescription"]?.ToString() ?? "",
                    CostCenter1ID = row["CostCenter1ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter1ID"]) : null,
                    CostCenter2ID = row["CostCenter2ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter2ID"]) : null,
                    CostCenter3ID = row["CostCenter3ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter3ID"]) : null,
                    CostCenter4ID = row["CostCenter4ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter4ID"]) : null,
                    CustomerID = row["CustomerID"] != DBNull.Value ? Convert.ToInt64(row["CustomerID"]) : null,
                    SupplierID = row["SupplierID"] != DBNull.Value ? Convert.ToInt64(row["SupplierID"]) : null,
                    BaseCurrencyAmount = Convert.ToDecimal(row["BaseCurrencyAmount"]),

                    // Related entity information
                    AccountCode = row["AccountCode"]?.ToString() ?? "",
                    AccountName = row["AccountName"]?.ToString() ?? "",
                    CostCenter1Name = row["CostCenter1Name"]?.ToString(),
                    CostCenter2Name = row["CostCenter2Name"]?.ToString(),
                    CostCenter3Name = row["CostCenter3Name"]?.ToString(),
                    CostCenter4Name = row["CostCenter4Name"]?.ToString(),
                    CustomerFullName = row["CustomerFullName"]?.ToString(),
                    SupplierName = row["SupplierName"]?.ToString()
                });
            }
        }

        private void ParseAttachmentsData(PaymentVoucherSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Attachments.Add(new PaymentVoucherAttachmentInfo
                {
                    PostingAttachmentID = Convert.ToInt64(row["PostingAttachmentID"]),
                    PostingID = Convert.ToInt64(row["PostingID"]),
                    DocTypeID = Convert.ToInt64(row["DocTypeID"]),
                    DocumentName = row["DocumentName"]?.ToString() ?? "",
                    FilePath = row["FilePath"]?.ToString() ?? "",
                    FileContentType = row["FileContentType"]?.ToString() ?? "",
                    FileSize = row["FileSize"] != DBNull.Value ? Convert.ToInt64(row["FileSize"]) : null,
                    DocumentDescription = row["DocumentDescription"]?.ToString() ?? "",
                    UploadedDate = Convert.ToDateTime(row["UploadedDate"]),
                    UploadedByUserID = Convert.ToInt64(row["UploadedByUserID"]),
                    IsRequired = Convert.ToBoolean(row["IsRequired"]),
                    DisplayOrder = row["DisplayOrder"] != DBNull.Value ? Convert.ToInt32(row["DisplayOrder"]) : null,

                    // Related entity information
                    DocTypeName = row["DocTypeName"]?.ToString() ?? "",
                    UploadedByUserName = row["UploadedByUserName"]?.ToString() ?? ""
                });
            }
        }
    }
}