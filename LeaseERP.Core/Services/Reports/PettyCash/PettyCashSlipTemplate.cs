// LeaseERP.Core/Services/Reports/PettyCash/PettyCashSlipTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.PettyCash
{
    public class PettyCashSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "petty-cash-slip";

        public PettyCashSlipTemplate(
            ILogger<PettyCashSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "petty-cash-slip", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(reportType, "pettycash-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "PETTY CASH VOUCHER",
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
                    HeaderColor = "#7c3aed", // Purple color for petty cash
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var pettyCashData = ParsePettyCashData(data.DataSet);
            if (pettyCashData?.Voucher == null)
            {
                container.AlignCenter().Padding(20).Text("Petty cash voucher data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Voucher Header Information
                RenderVoucherHeader(column, pettyCashData, config);

                column.Item().PaddingVertical(10);

                // Voucher Details Section
                RenderVoucherDetails(column, pettyCashData, config);

                column.Item().PaddingVertical(10);

                // Voucher Lines Section
                RenderVoucherLines(column, pettyCashData, config);

                column.Item().PaddingVertical(10);

                // Financial Summary Section
                RenderFinancialSummary(column, pettyCashData, config);

                // Attachments Section
                if (pettyCashData.Attachments.Any())
                {
                    column.Item().PaddingVertical(10);
                    RenderAttachmentsSection(column, pettyCashData, config);
                }

                // Notes Section
                if (!string.IsNullOrEmpty(pettyCashData.Voucher.Narration))
                {
                    column.Item().PaddingVertical(10);
                    RenderNotesSection(column, pettyCashData, config);
                }

                // Approval and Signature Section
                column.Item().PaddingVertical(20);
                RenderApprovalSection(column, pettyCashData, config);
            });
        }

        private void RenderVoucherHeader(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
        {
            column.Item().Background(GetStatusBackgroundColor(pettyCashData.Voucher.PostingStatus))
                .Padding(12).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Voucher No: {pettyCashData.Voucher.VoucherNo}")
                            .SemiBold().FontSize(14).FontColor(GetStatusTextColor(pettyCashData.Voucher.PostingStatus));
                        col.Item().Text($"Voucher Type: {pettyCashData.Voucher.VoucherType}").FontSize(10);
                        col.Item().Text($"Status: {pettyCashData.Voucher.PostingStatus}")
                            .SemiBold().FontColor(GetStatusColor(pettyCashData.Voucher.PostingStatus));
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Transaction Date: {pettyCashData.Voucher.TransactionDate:dd/MM/yyyy}").FontSize(10);
                        col.Item().Text($"Posting Date: {pettyCashData.Voucher.PostingDate:dd/MM/yyyy}").FontSize(10);
                        if (!string.IsNullOrEmpty(pettyCashData.Voucher.PaidTo))
                        {
                            col.Item().Text($"Paid To: {pettyCashData.Voucher.PaidTo}").FontSize(10);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Currency: {pettyCashData.Voucher.CurrencyName}").FontSize(10);
                        if (pettyCashData.Voucher.ExchangeRate != 1)
                        {
                            col.Item().AlignRight().Text($"Exchange Rate: {pettyCashData.Voucher.ExchangeRate:N4}").FontSize(9);
                        }
                        col.Item().AlignRight().Text($"FY: {pettyCashData.Voucher.FYDescription}").FontSize(9);
                    });
                });
        }

        private void RenderVoucherDetails(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
        {
            column.Item().Border(1).BorderColor(ParseColor(config.Styling.BorderColor))
                .Padding(12).Column(detailColumn =>
                {
                    detailColumn.Item().Text("VOUCHER DETAILS")
                        .FontSize(12).SemiBold().FontColor(ParseColor(config.Styling.HeaderColor));

                    detailColumn.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Basic Information").FontSize(10).SemiBold();
                            col.Item().Text($"Company: {pettyCashData.Voucher.CompanyName}").FontSize(9);
                            col.Item().Text($"Description: {pettyCashData.Voucher.Description}").FontSize(9);
                            if (!string.IsNullOrEmpty(pettyCashData.Voucher.InvoiceNo))
                            {
                                col.Item().Text($"Invoice No: {pettyCashData.Voucher.InvoiceNo}").FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(pettyCashData.Voucher.RefNo))
                            {
                                col.Item().Text($"Reference No: {pettyCashData.Voucher.RefNo}").FontSize(9);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Payment Information").FontSize(10).SemiBold();
                            if (!string.IsNullOrEmpty(pettyCashData.Voucher.ChequeNo))
                            {
                                col.Item().Text($"Cheque No: {pettyCashData.Voucher.ChequeNo}").FontSize(9);
                                if (pettyCashData.Voucher.ChequeDate.HasValue)
                                {
                                    col.Item().Text($"Cheque Date: {pettyCashData.Voucher.ChequeDate.Value:dd/MM/yyyy}").FontSize(9);
                                }
                            }
                            if (!string.IsNullOrEmpty(pettyCashData.Voucher.BankName))
                            {
                                col.Item().Text($"Bank: {pettyCashData.Voucher.BankName}").FontSize(9);
                            }
                        });
                    });
                });
        }

        private void RenderVoucherLines(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
        {
            column.Item().Column(linesColumn =>
            {
                linesColumn.Item().Text("VOUCHER ENTRIES").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                linesColumn.Item().PaddingTop(5);

                var tableConfig = new TableConfiguration<PettyCashLineInfo>
                {
                    Columns = new List<TableColumn<PettyCashLineInfo>>
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
                                    .Text(item.DebitAmount > 0 ? item.DebitAmount.ToString("N2") : "").FontSize(9) },
                        new() { Header = "Credit", PropertyName = "CreditAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight()
                                    .Text(item.CreditAmount > 0 ? item.CreditAmount.ToString("N2") : "").FontSize(9) }
                    },
                    ShowTotals = true,
                    TotalCalculators = new Dictionary<string, Func<IEnumerable<PettyCashLineInfo>, string>>
                    {
                        { "DebitAmount", lines => lines.Sum(l => l.DebitAmount).ToString("N2") },
                        { "CreditAmount", lines => lines.Sum(l => l.CreditAmount).ToString("N2") }
                    }
                };

                RenderTable(linesColumn.Item(), pettyCashData.Lines, tableConfig);
            });
        }

        private void RenderFinancialSummary(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
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
                                    .Text($"{pettyCashData.Voucher.CurrencyName} {pettyCashData.Voucher.TotalDebitAmount:N2}");
                            });

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total Credits:");
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{pettyCashData.Voucher.CurrencyName} {pettyCashData.Voucher.TotalCreditAmount:N2}");
                            });

                            col.Item().BorderTop(1).PaddingTop(5);

                            var balanceDifference = Math.Abs(pettyCashData.Voucher.TotalDebitAmount - pettyCashData.Voucher.TotalCreditAmount);
                            var isBalanced = balanceDifference < 0.01m;

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("BALANCE CHECK:").SemiBold();
                                r.ConstantItem(120).AlignRight()
                                    .Text(isBalanced ? "BALANCED" : $"DIFF: {balanceDifference:N2}")
                                    .SemiBold().FontColor(isBalanced ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            });

                            // Exchange rate information
                            if (pettyCashData.Voucher.ExchangeRate != 1)
                            {
                                col.Item().PaddingTop(8).Text("Exchange Rate Information").FontSize(10).SemiBold();
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Rate:");
                                    r.ConstantItem(120).AlignRight().Text($"1 = {pettyCashData.Voucher.ExchangeRate:N4}").FontSize(9);
                                });
                            }
                        });
                    });
                });
        }

        private void RenderAttachmentsSection(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
        {
            column.Item().Column(attachColumn =>
            {
                attachColumn.Item().Text("ATTACHMENTS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                attachColumn.Item().PaddingTop(5);

                foreach (var attachment in pettyCashData.Attachments.OrderBy(a => a.DisplayOrder))
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

                if (pettyCashData.Attachments.Any(a => a.IsRequired))
                {
                    attachColumn.Item().PaddingTop(5).Text("* Required documents").FontSize(8).FontColor(Colors.Red.Darken2);
                }
            });
        }

        private void RenderNotesSection(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
        {
            column.Item().Column(notesColumn =>
            {
                notesColumn.Item().Text("NARRATION / NOTES").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));
                notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                    .Text(pettyCashData.Voucher.Narration).FontSize(9);
            });
        }

        private void RenderApprovalSection(ColumnDescriptor column, PettyCashSlipData pettyCashData, ReportConfiguration config)
        {
            column.Item().Column(approvalColumn =>
            {
                // Status information
                approvalColumn.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Prepared by: {pettyCashData.Voucher.CreatedBy}").FontSize(9);
                        col.Item().Text($"Prepared on: {pettyCashData.Voucher.CreatedOn:dd/MM/yyyy HH:mm}").FontSize(9);
                        if (!string.IsNullOrEmpty(pettyCashData.Voucher.UpdatedBy))
                        {
                            col.Item().Text($"Last updated by: {pettyCashData.Voucher.UpdatedBy}").FontSize(8);
                            col.Item().Text($"Updated on: {pettyCashData.Voucher.UpdatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Status: {pettyCashData.Voucher.PostingStatus}")
                            .SemiBold().FontColor(GetStatusColor(pettyCashData.Voucher.PostingStatus));

                        if (pettyCashData.Voucher.PostingStatus == "Posted")
                        {
                            col.Item().AlignRight().Text("✓ APPROVED & POSTED").FontColor(Colors.Green.Darken2).FontSize(9);
                        }
                        else if (pettyCashData.Voucher.PostingStatus == "Pending")
                        {
                            col.Item().AlignRight().Text("⏳ PENDING APPROVAL").FontColor(Colors.Orange.Darken2).FontSize(9);
                        }
                    });
                });

                // Signature section
                approvalColumn.Item().PaddingTop(30).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                            .Text("Prepared By").FontSize(10);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text($"({pettyCashData.Voucher.CreatedBy})").FontSize(9);
                        col.Item().AlignCenter().Text("Date: ____________________").FontSize(9);
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                            .Text("Checked By").FontSize(10);
                        col.Item().PaddingTop(5).AlignCenter()
                            .Text("____________________").FontSize(9);
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
                });

                // Terms and conditions
                approvalColumn.Item().PaddingTop(20).Column(termsColumn =>
                {
                    termsColumn.Item().Text("TERMS & CONDITIONS").FontSize(10).SemiBold();

                    var terms = new[]
                    {
                        "1. All supporting documents must be attached to this voucher.",
                        "2. This voucher is valid only when properly approved and posted.",
                        "3. Any alterations must be initialed by the preparer and approver.",
                        "4. This is a computer generated voucher."
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
                "POSTED" => Colors.Green.Darken2,
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
                "POSTED" => Colors.Green.Lighten4,
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
                "POSTED" => Colors.Green.Darken2,
                "REJECTED" => Colors.Red.Darken2,
                "CANCELLED" => Colors.Red.Darken2,
                _ => Colors.Black
            };
        }

        private PettyCashSlipData ParsePettyCashData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 3) return null;

            var pettyCashSlipData = new PettyCashSlipData();

            // Parse voucher header data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                pettyCashSlipData.Voucher = new PettyCashVoucherInfo
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
                    //PaidTo = row["PaidTo"]?.ToString(),
                    //InvoiceNo = row["InvoiceNo"]?.ToString(),
                    //RefNo = row["RefNo"]?.ToString(),
                    ChequeNo = row["ChequeNo"]?.ToString(),
                    ChequeDate = row["ChequeDate"] != DBNull.Value ? Convert.ToDateTime(row["ChequeDate"]) : null,
                    BankID = row["BankID"] != DBNull.Value ? Convert.ToInt64(row["BankID"]) : null,
                    PostingStatus = row["PostingStatus"]?.ToString() ?? "",

                    // Related entity information
                    CompanyName = row["CompanyName"]?.ToString() ?? "",
                    FYDescription = row["FYDescription"]?.ToString() ?? "",
                    CurrencyName = row["CurrencyName"]?.ToString() ?? "",
                    BankName = row["BankName"]?.ToString(),

                    // Audit information
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString(),
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                };
            }

            // Parse voucher lines data (Table 1)
            if (dataSet.Tables.Count > 1)
            {
                ParseVoucherLinesData(pettyCashSlipData, dataSet.Tables[1]);
            }

            // Parse attachments data (Table 2)
            if (dataSet.Tables.Count > 2)
            {
                ParseAttachmentsData(pettyCashSlipData, dataSet.Tables[2]);
            }

            // Calculate totals
            if (pettyCashSlipData.Lines.Any())
            {
                pettyCashSlipData.Voucher.TotalDebitAmount = pettyCashSlipData.Lines.Sum(l => l.DebitAmount);
                pettyCashSlipData.Voucher.TotalCreditAmount = pettyCashSlipData.Lines.Sum(l => l.CreditAmount);
            }

            return pettyCashSlipData;
        }

        private void ParseVoucherLinesData(PettyCashSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Lines.Add(new PettyCashLineInfo
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

        private void ParseAttachmentsData(PettyCashSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Attachments.Add(new PettyCashAttachmentInfo
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