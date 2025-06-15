// LeaseERP.Core/Services/Reports/Receipts/ReceiptSlipTemplate.cs
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
    public class ReceiptSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "receipt-slip";

        public ReceiptSlipTemplate(
            ILogger<ReceiptSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "receipt-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "LEASE RECEIPT",
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
                    HeaderColor = "#059669", // Green color for receipts (money received)
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var receiptData = ParseReceiptData(data.DataSet);
            if (receiptData?.Receipt == null)
            {
                container.AlignCenter().Padding(20).Text("Receipt data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Receipt Header Information
                RenderReceiptHeader(column, receiptData, config);

                column.Item().PaddingVertical(10);

                // Customer and Payment Information
                RenderCustomerPaymentInfo(column, receiptData, config);

                column.Item().PaddingVertical(10);

                // Invoice Information (if applicable)
                if (!string.IsNullOrEmpty(receiptData.Receipt.InvoiceNo))
                {
                    RenderInvoiceInfo(column, receiptData, config);
                    column.Item().PaddingVertical(10);
                }

                // Payment Details Section
                RenderPaymentDetails(column, receiptData, config);

                column.Item().PaddingVertical(10);

                // Amount Breakdown Section
                RenderAmountBreakdown(column, receiptData, config);

                // Posting Information (if posted)
                if (receiptData.Receipt.IsPosted && receiptData.Postings.Any())
                {
                    column.Item().PaddingVertical(10);
                    RenderPostingInfo(column, receiptData, config);
                }

                // Notes Section
                if (!string.IsNullOrEmpty(receiptData.Receipt.Notes))
                {
                    column.Item().PaddingVertical(10);
                    RenderNotesSection(column, receiptData, config);
                }

                // Acknowledgment and Signature Section
                column.Item().PaddingVertical(20);
                RenderAcknowledgmentSection(column, receiptData, config);
            });
        }

        private void RenderReceiptHeader(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Background(GetStatusBackgroundColor(receiptData.Receipt.PaymentStatus))
                .Padding(12).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Receipt No: {receiptData.Receipt.ReceiptNo}")
                            .SemiBold().FontSize(14).FontColor(GetStatusTextColor(receiptData.Receipt.PaymentStatus));
                        col.Item().Text($"Payment Type: {receiptData.Receipt.PaymentType}").FontSize(10);
                        col.Item().Text($"Status: {receiptData.Receipt.PaymentStatus}")
                            .SemiBold().FontColor(GetStatusColor(receiptData.Receipt.PaymentStatus));
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Receipt Date: {receiptData.Receipt.ReceiptDate:dd/MM/yyyy}").FontSize(10);
                        if (receiptData.Receipt.DepositDate.HasValue)
                        {
                            col.Item().Text($"Deposit Date: {receiptData.Receipt.DepositDate.Value:dd/MM/yyyy}").FontSize(10);
                        }
                        if (receiptData.Receipt.ClearanceDate.HasValue)
                        {
                            col.Item().Text($"Clearance Date: {receiptData.Receipt.ClearanceDate.Value:dd/MM/yyyy}").FontSize(10);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Currency: {receiptData.Receipt.CurrencyCode}").FontSize(10);
                        if (receiptData.Receipt.ExchangeRate != 1)
                        {
                            col.Item().AlignRight().Text($"Exchange Rate: {receiptData.Receipt.ExchangeRate:N4}").FontSize(9);
                        }
                        if (receiptData.Receipt.IsPosted)
                        {
                            col.Item().AlignRight().Text("✓ POSTED").FontColor(Colors.Green.Darken2).SemiBold().FontSize(9);
                        }
                        if (receiptData.Receipt.IsAdvancePayment)
                        {
                            col.Item().AlignRight().Text("ADVANCE PAYMENT").FontColor(Colors.Blue.Darken2).SemiBold().FontSize(9);
                        }
                    });
                });
        }

        private void RenderCustomerPaymentInfo(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Border(1).BorderColor(ParseColor(config.Styling.BorderColor))
                .Padding(12).Column(infoColumn =>
                {
                    infoColumn.Item().Text("CUSTOMER & PAYMENT INFORMATION")
                        .FontSize(12).SemiBold().FontColor(ParseColor(config.Styling.HeaderColor));

                    infoColumn.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Customer Information").FontSize(10).SemiBold();
                            col.Item().Text($"Name: {receiptData.Receipt.CustomerFullName}").FontSize(9);
                            col.Item().Text($"Customer No: {receiptData.Receipt.CustomerNo}").FontSize(9);
                            if (!string.IsNullOrEmpty(receiptData.Receipt.CustomerTaxNo))
                            {
                                col.Item().Text($"Tax Reg No: {receiptData.Receipt.CustomerTaxNo}").FontSize(9);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Payment Information").FontSize(10).SemiBold();
                            col.Item().Text($"Payment Method: {receiptData.Receipt.PaymentType}").FontSize(9);
                            if (!string.IsNullOrEmpty(receiptData.Receipt.ReceivedByUser))
                            {
                                col.Item().Text($"Received By: {receiptData.Receipt.ReceivedByUser}").FontSize(9);
                            }
                            col.Item().Text($"Fiscal Year: {receiptData.Receipt.FiscalYear}").FontSize(9);
                        });
                    });
                });
        }

        private void RenderInvoiceInfo(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Invoice Details").FontSize(10).SemiBold();
                    col.Item().Text($"Invoice No: {receiptData.Receipt.InvoiceNo}").FontSize(9);
                    if (receiptData.Receipt.InvoiceDate.HasValue)
                    {
                        col.Item().Text($"Invoice Date: {receiptData.Receipt.InvoiceDate.Value:dd/MM/yyyy}").FontSize(9);
                    }
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Invoice Amount Details").FontSize(10).SemiBold();
                    if (receiptData.Receipt.InvoiceAmount.HasValue)
                    {
                        col.Item().Text($"Invoice Amount: {receiptData.Receipt.CurrencyCode} {receiptData.Receipt.InvoiceAmount.Value:N2}").FontSize(9);
                    }
                    if (receiptData.Receipt.InvoiceBalance.HasValue)
                    {
                        col.Item().Text($"Invoice Balance: {receiptData.Receipt.CurrencyCode} {receiptData.Receipt.InvoiceBalance.Value:N2}")
                            .FontSize(9).FontColor(receiptData.Receipt.InvoiceBalance.Value > 0 ? Colors.Red.Darken1 : Colors.Green.Darken2);
                    }
                });

                if (!string.IsNullOrEmpty(receiptData.Receipt.ContractNo) || !string.IsNullOrEmpty(receiptData.Receipt.UnitNo))
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text("Property Details").FontSize(10).SemiBold();
                        if (!string.IsNullOrEmpty(receiptData.Receipt.ContractNo))
                        {
                            col.Item().Text($"Contract: {receiptData.Receipt.ContractNo}").FontSize(9);
                        }
                        if (!string.IsNullOrEmpty(receiptData.Receipt.PropertyName))
                        {
                            col.Item().Text($"Property: {receiptData.Receipt.PropertyName}").FontSize(9);
                        }
                        if (!string.IsNullOrEmpty(receiptData.Receipt.UnitNo))
                        {
                            col.Item().Text($"Unit: {receiptData.Receipt.UnitNo}").FontSize(9);
                        }
                    });
                }
            });
        }

        private void RenderPaymentDetails(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Column(detailColumn =>
            {
                detailColumn.Item().Text("PAYMENT DETAILS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                detailColumn.Item().PaddingTop(8).Table(table =>
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
                            .Text("Reference/Check Details").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Bank Information").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Status & Dates").FontColor(Colors.White).FontSize(9).SemiBold();
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        col.Item().Text(receiptData.Receipt.PaymentType).FontSize(9).SemiBold();
                        if (!string.IsNullOrEmpty(receiptData.Receipt.BankAccountNo))
                        {
                            col.Item().Text($"A/C: {receiptData.Receipt.BankAccountNo}").FontSize(8);
                        }
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        if (!string.IsNullOrEmpty(receiptData.Receipt.ChequeNo))
                        {
                            col.Item().Text($"Cheque No: {receiptData.Receipt.ChequeNo}").FontSize(9);
                            if (receiptData.Receipt.ChequeDate.HasValue)
                            {
                                col.Item().Text($"Date: {receiptData.Receipt.ChequeDate.Value:dd/MM/yyyy}").FontSize(8);
                            }
                        }
                        if (!string.IsNullOrEmpty(receiptData.Receipt.TransactionReference))
                        {
                            col.Item().Text($"Ref: {receiptData.Receipt.TransactionReference}").FontSize(8);
                        }
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        if (!string.IsNullOrEmpty(receiptData.Receipt.BankName))
                        {
                            col.Item().Text(receiptData.Receipt.BankName).FontSize(9);
                        }
                        if (!string.IsNullOrEmpty(receiptData.Receipt.SwiftCode))
                        {
                            col.Item().Text($"SWIFT: {receiptData.Receipt.SwiftCode}").FontSize(8);
                        }
                        if (!string.IsNullOrEmpty(receiptData.Receipt.DepositBankName))
                        {
                            col.Item().Text($"Deposited: {receiptData.Receipt.DepositBankName}").FontSize(8);
                        }
                    });

                    table.Cell().Padding(8).Column(col =>
                    {
                        col.Item().Text(receiptData.Receipt.PaymentStatus).FontSize(9)
                            .FontColor(GetStatusColor(receiptData.Receipt.PaymentStatus));
                        if (receiptData.Receipt.DepositDate.HasValue)
                        {
                            col.Item().Text($"Deposited: {receiptData.Receipt.DepositDate.Value:dd/MM/yyyy}").FontSize(8);
                        }
                        if (receiptData.Receipt.ClearanceDate.HasValue)
                        {
                            col.Item().Text($"Cleared: {receiptData.Receipt.ClearanceDate.Value:dd/MM/yyyy}").FontSize(8);
                        }
                    });
                });
            });
        }

        private void RenderAmountBreakdown(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Border(2).BorderColor(ParseColor(config.Styling.HeaderColor))
                .Padding(15).Column(amountColumn =>
                {
                    amountColumn.Item().Text("AMOUNT BREAKDOWN").FontSize(14).SemiBold()
                        .FontColor(ParseColor(config.Styling.HeaderColor)).AlignCenter();

                    amountColumn.Item().PaddingTop(12).Row(row =>
                    {
                        row.RelativeItem(3);
                        row.RelativeItem(2).Column(col =>
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Received Amount:");
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{receiptData.Receipt.CurrencyCode} {receiptData.Receipt.ReceivedAmount:N2}")
                                    .SemiBold().FontSize(12).FontColor(ParseColor(config.Styling.HeaderColor));
                            });

                            if (receiptData.Receipt.SecurityDepositAmount.HasValue && receiptData.Receipt.SecurityDepositAmount > 0)
                            {
                                col.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Security Deposit:");
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"{receiptData.Receipt.CurrencyCode} {receiptData.Receipt.SecurityDepositAmount.Value:N2}")
                                        .FontColor(Colors.Blue.Darken2);
                                });
                            }

                            if (receiptData.Receipt.PenaltyAmount.HasValue && receiptData.Receipt.PenaltyAmount > 0)
                            {
                                col.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Penalty Amount:");
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"{receiptData.Receipt.CurrencyCode} {receiptData.Receipt.PenaltyAmount.Value:N2}")
                                        .FontColor(Colors.Red.Darken2);
                                });
                            }

                            if (receiptData.Receipt.DiscountAmount.HasValue && receiptData.Receipt.DiscountAmount > 0)
                            {
                                col.Item().PaddingTop(3).Row(r =>
                                {
                                    r.RelativeItem().Text("Discount Applied:");
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"({receiptData.Receipt.CurrencyCode} {receiptData.Receipt.DiscountAmount.Value:N2})")
                                        .FontColor(Colors.Green.Darken2);
                                });
                            }

                            col.Item().PaddingTop(8).BorderTop(1);

                            var totalComponents = receiptData.Receipt.ReceivedAmount +
                                                (receiptData.Receipt.SecurityDepositAmount ?? 0) +
                                                (receiptData.Receipt.PenaltyAmount ?? 0) -
                                                (receiptData.Receipt.DiscountAmount ?? 0);

                            col.Item().PaddingTop(5).Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL AMOUNT:").SemiBold().FontSize(12);
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{receiptData.Receipt.CurrencyCode} {totalComponents:N2}")
                                    .SemiBold().FontSize(14).FontColor(ParseColor(config.Styling.HeaderColor));
                            });

                            // Payment status indicator
                            col.Item().PaddingTop(10).AlignCenter()
                                .Text($"Payment Status: {receiptData.Receipt.PaymentStatus}")
                                .FontColor(GetStatusColor(receiptData.Receipt.PaymentStatus))
                                .SemiBold().FontSize(11);
                        });
                    });
                });
        }

        private void RenderPostingInfo(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Column(postingColumn =>
            {
                postingColumn.Item().Text("ACCOUNTING ENTRIES").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                postingColumn.Item().PaddingTop(5);

                var tableConfig = new TableConfiguration<ReceiptPostingInfo>
                {
                    Columns = new List<TableColumn<ReceiptPostingInfo>>
                    {
                        new() { Header = "Account", PropertyName = "AccountName", Width = 3,
                                RenderCell = (c, item, cfg) => c.Column(col =>
                                {
                                    col.Item().Text(item.AccountName).FontSize(9);
                                    col.Item().Text($"({item.AccountCode})").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }) },
                        new() { Header = "Description", PropertyName = "Description", Width = 3,
                                RenderCell = (c, item, cfg) => c.Text(item.Description).FontSize(9) },
                        new() { Header = "Debit", PropertyName = "DebitAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text(item.DebitAmount > 0 ? $"{receiptData.Receipt.CurrencyCode} {item.DebitAmount:N2}" : "").FontSize(9) },
                        new() { Header = "Credit", PropertyName = "CreditAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text(item.CreditAmount > 0 ? $"{receiptData.Receipt.CurrencyCode} {item.CreditAmount:N2}" : "").FontSize(9) },
                        new() { Header = "Date", PropertyName = "PostingDate", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.PostingDate.ToString("dd/MM/yyyy")).FontSize(9) }
                    },
                    ShowTotals = true,
                    TotalCalculators = new Dictionary<string, Func<IEnumerable<ReceiptPostingInfo>, string>>
                    {
                        { "DebitAmount", postings => $"{receiptData.Receipt.CurrencyCode} {postings.Sum(p => p.DebitAmount):N2}" },
                        { "CreditAmount", postings => $"{receiptData.Receipt.CurrencyCode} {postings.Sum(p => p.CreditAmount):N2}" }
                    }
                };

                RenderTable(postingColumn.Item(), receiptData.Postings, tableConfig);
            });
        }

        private void RenderNotesSection(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Column(notesColumn =>
            {
                notesColumn.Item().Text("NOTES").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));
                notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                    .Text(receiptData.Receipt.Notes).FontSize(9);
            });
        }

        private void RenderAcknowledgmentSection(ColumnDescriptor column, ReceiptSlipData receiptData, ReportConfiguration config)
        {
            column.Item().Column(ackColumn =>
            {
                // Receipt acknowledgment text
                ackColumn.Item().Background(Colors.Grey.Lighten4).Padding(10)
                    .Text("This receipt acknowledges the payment received from the above customer for the amount stated. " +
                          "Please retain this receipt for your records.")
                    .FontSize(10).AlignCenter();

                ackColumn.Item().PaddingTop(20);

                // Generated information and signature
                ackColumn.Item().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Generated by: {receiptData.Receipt.CreatedBy}").FontSize(9);
                        col.Item().Text($"Generated on: {receiptData.Receipt.CreatedOn:dd/MM/yyyy HH:mm}").FontSize(9);
                        if (!string.IsNullOrEmpty(receiptData.Receipt.UpdatedBy))
                        {
                            col.Item().Text($"Last updated by: {receiptData.Receipt.UpdatedBy}").FontSize(8);
                            col.Item().Text($"Updated on: {receiptData.Receipt.UpdatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
                        }
                    });

                    row.RelativeItem(2);

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                            .Text("Authorized Signature").FontSize(10);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text("Date: ____________________").FontSize(9);
                    });
                });

                // Terms and conditions
                ackColumn.Item().PaddingTop(15).Column(termsColumn =>
                {
                    termsColumn.Item().Text("TERMS & CONDITIONS").FontSize(10).SemiBold();

                    var terms = new[]
                    {
                        "1. This receipt is valid only when payment is successfully processed and cleared.",
                        "2. In case of cheque payments, this receipt is subject to clearance of the cheque.",
                        "3. For any discrepancies, please contact the office within 7 days of receipt date.",
                        "4. This is a computer generated receipt and does not require a signature."
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
                "RECEIVED" => Colors.Orange.Darken1,
                "DEPOSITED" => Colors.Blue.Darken1,
                "CLEARED" => Colors.Green.Darken2,
                "BOUNCED" => Colors.Red.Darken2,
                "CANCELLED" => Colors.Red.Darken1,
                "REVERSED" => Colors.Purple.Darken1,
                _ => Colors.Black
            };
        }

        private string GetStatusBackgroundColor(string status)
        {
            return status?.ToUpper() switch
            {
                "RECEIVED" => Colors.Orange.Lighten4,
                "DEPOSITED" => Colors.Blue.Lighten4,
                "CLEARED" => Colors.Green.Lighten4,
                "BOUNCED" => Colors.Red.Lighten4,
                "CANCELLED" => Colors.Red.Lighten4,
                "REVERSED" => Colors.Purple.Lighten4,
                _ => Colors.Grey.Lighten5
            };
        }

        private string GetStatusTextColor(string status)
        {
            return status?.ToUpper() switch
            {
                "CLEARED" => Colors.Green.Darken2,
                "BOUNCED" => Colors.Red.Darken2,
                "CANCELLED" => Colors.Red.Darken2,
                "REVERSED" => Colors.Purple.Darken2,
                _ => Colors.Black
            };
        }

        private ReceiptSlipData ParseReceiptData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 2) return null;

            var receiptSlipData = new ReceiptSlipData();

            // Parse receipt master data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                receiptSlipData.Receipt = new ReceiptMasterInfo
                {
                    LeaseReceiptID = Convert.ToInt64(row["LeaseReceiptID"]),
                    ReceiptNo = row["ReceiptNo"]?.ToString() ?? "",
                    ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]),
                    LeaseInvoiceID = row["LeaseInvoiceID"] != DBNull.Value ? Convert.ToInt64(row["LeaseInvoiceID"]) : null,
                    CustomerID = Convert.ToInt64(row["CustomerID"]),
                    CompanyID = Convert.ToInt64(row["CompanyID"]),
                    FiscalYearID = Convert.ToInt64(row["FiscalYearID"]),
                    PaymentType = row["PaymentType"]?.ToString() ?? "",
                    PaymentStatus = row["PaymentStatus"]?.ToString() ?? "",
                    ReceivedAmount = Convert.ToDecimal(row["ReceivedAmount"]),
                    CurrencyID = Convert.ToInt64(row["CurrencyID"]),
                    ExchangeRate = Convert.ToDecimal(row["ExchangeRate"]),
                    BankID = row["BankID"] != DBNull.Value ? Convert.ToInt64(row["BankID"]) : null,
                    BankAccountNo = row["BankAccountNo"]?.ToString(),
                    ChequeNo = row["ChequeNo"]?.ToString(),
                    ChequeDate = row["ChequeDate"] != DBNull.Value ? Convert.ToDateTime(row["ChequeDate"]) : null,
                    TransactionReference = row["TransactionReference"]?.ToString(),
                    DepositedBankID = row["DepositedBankID"] != DBNull.Value ? Convert.ToInt64(row["DepositedBankID"]) : null,
                    DepositDate = row["DepositDate"] != DBNull.Value ? Convert.ToDateTime(row["DepositDate"]) : null,
                    ClearanceDate = row["ClearanceDate"] != DBNull.Value ? Convert.ToDateTime(row["ClearanceDate"]) : null,
                    IsAdvancePayment = Convert.ToBoolean(row["IsAdvancePayment"]),
                    SecurityDepositAmount = row["SecurityDepositAmount"] != DBNull.Value ? Convert.ToDecimal(row["SecurityDepositAmount"]) : null,
                    PenaltyAmount = row["PenaltyAmount"] != DBNull.Value ? Convert.ToDecimal(row["PenaltyAmount"]) : null,
                    DiscountAmount = row["DiscountAmount"] != DBNull.Value ? Convert.ToDecimal(row["DiscountAmount"]) : null,
                    ReceivedByUserID = row["ReceivedByUserID"] != DBNull.Value ? Convert.ToInt64(row["ReceivedByUserID"]) : null,
                    AccountID = row["AccountID"] != DBNull.Value ? Convert.ToInt64(row["AccountID"]) : null,
                    IsPosted = Convert.ToBoolean(row["IsPosted"]),
                    PostingID = row["PostingID"] != DBNull.Value ? Convert.ToInt64(row["PostingID"]) : null,
                    Notes = row["Notes"]?.ToString(),

                    // Related entity information
                    CustomerFullName = row["CustomerName"]?.ToString() ?? "",
                    CustomerNo = row["CustomerNo"]?.ToString() ?? "",
                    CustomerTaxNo = row["CustomerTaxNo"]?.ToString() ?? "",
                    InvoiceNo = row["InvoiceNo"]?.ToString(),
                    InvoiceDate = row["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(row["InvoiceDate"]) : null,
                    InvoiceAmount = row["InvoiceAmount"] != DBNull.Value ? Convert.ToDecimal(row["InvoiceAmount"]) : null,
                    InvoiceBalance = row["InvoiceBalance"] != DBNull.Value ? Convert.ToDecimal(row["InvoiceBalance"]) : null,
                    ContractNo = row["ContractNo"]?.ToString(),
                    UnitNo = row["UnitNo"]?.ToString(),
                    PropertyName = row["PropertyName"]?.ToString(),
                    CurrencyCode = row["CurrencyCode"]?.ToString() ?? "",
                    CurrencyName = row["CurrencyName"]?.ToString() ?? "",
                    BankName = row["BankName"]?.ToString(),
                    SwiftCode = row["SwiftCode"]?.ToString(),
                    DepositBankName = row["DepositBankName"]?.ToString(),
                    ReceivedByUser = row["ReceivedByUser"]?.ToString(),
                    CompanyName = row["CompanyName"]?.ToString() ?? "",
                    FiscalYear = row["FiscalYear"]?.ToString() ?? "",

                    // Audit information
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString(),
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                };
            }

            // Parse postings data (Table 1)
            if (dataSet.Tables.Count > 1)
            {
                ParsePostingsData(receiptSlipData, dataSet.Tables[1]);
            }

            return receiptSlipData;
        }

        private void ParsePostingsData(ReceiptSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Postings.Add(new ReceiptPostingInfo
                {
                    PostingID = Convert.ToInt64(row["PostingID"]),
                    VoucherNo = row["VoucherNo"]?.ToString() ?? "",
                    PostingDate = Convert.ToDateTime(row["PostingDate"]),
                    TransactionType = row["TransactionType"]?.ToString() ?? "",
                    DebitAmount = Convert.ToDecimal(row["DebitAmount"]),
                    CreditAmount = Convert.ToDecimal(row["CreditAmount"]),
                    Description = row["Description"]?.ToString() ?? "",
                    Narration = row["Narration"]?.ToString() ?? "",
                    AccountCode = row["AccountCode"]?.ToString() ?? "",
                    AccountName = row["AccountName"]?.ToString() ?? "",
                    IsReversed = Convert.ToBoolean(row["IsReversed"]),
                    ReversalReason = row["ReversalReason"]?.ToString(),
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                });
            }
        }
    }
}