// LeaseERP.Core/Services/Reports/JournalVouchers/JournalVoucherSlipTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.JournalVouchers
{
    public class JournalVoucherSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "journal-voucher-slip";

        public JournalVoucherSlipTemplate(
            ILogger<JournalVoucherSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "journal-voucher-slip", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(reportType, "journalvoucher-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "JOURNAL VOUCHER",
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
                    HeaderColor = "#6366f1", // Indigo color for journal entries
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var journalData = ParseJournalVoucherData(data.DataSet);
            if (journalData?.Voucher == null)
            {
                container.AlignCenter().Padding(20).Text("Journal voucher data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Voucher Header Information
                RenderVoucherHeader(column, journalData, config);

                column.Item().PaddingVertical(10);

                // Voucher Details Section
                RenderVoucherDetails(column, journalData, config);

                column.Item().PaddingVertical(10);

                // Journal Entries Section
                RenderJournalEntries(column, journalData, config);

                column.Item().PaddingVertical(10);

                // Financial Summary Section
                RenderFinancialSummary(column, journalData, config);

                // Attachments Section
                if (journalData.Attachments.Any())
                {
                    column.Item().PaddingVertical(10);
                    RenderAttachmentsSection(column, journalData, config);
                }

                // Notes Section
                if (!string.IsNullOrEmpty(journalData.Voucher.Narration))
                {
                    column.Item().PaddingVertical(10);
                    RenderNotesSection(column, journalData, config);
                }

                // Approval and Signature Section
                column.Item().PaddingVertical(20);
                RenderApprovalSection(column, journalData, config);
            });
        }

        private void RenderVoucherHeader(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
        {
            column.Item().Background(GetStatusBackgroundColor(journalData.Voucher.PostingStatus))
                .Padding(12).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Voucher No: {journalData.Voucher.VoucherNo}")
                            .SemiBold().FontSize(14).FontColor(GetStatusTextColor(journalData.Voucher.PostingStatus));
                        col.Item().Text($"Voucher Type: {journalData.Voucher.VoucherType}").FontSize(10);
                        col.Item().Text($"Status: {journalData.Voucher.PostingStatus}")
                            .SemiBold().FontColor(GetStatusColor(journalData.Voucher.PostingStatus));
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Transaction Date: {journalData.Voucher.TransactionDate:dd/MM/yyyy}").FontSize(10);
                        col.Item().Text($"Posting Date: {journalData.Voucher.PostingDate:dd/MM/yyyy}").FontSize(10);
                        if (!string.IsNullOrEmpty(journalData.Voucher.JournalType))
                        {
                            col.Item().Text($"Journal Type: {journalData.Voucher.JournalType}").FontSize(10);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Currency: {journalData.Voucher.CurrencyName}").FontSize(10);
                        if (journalData.Voucher.ExchangeRate != 1)
                        {
                            col.Item().AlignRight().Text($"Exchange Rate: {journalData.Voucher.ExchangeRate:N4}").FontSize(9);
                        }
                        col.Item().AlignRight().Text($"FY: {journalData.Voucher.FYDescription}").FontSize(9);
                        if (journalData.Voucher.IsReversed)
                        {
                            col.Item().AlignRight().Text("⚠ REVERSED").FontColor(Colors.Red.Darken2).SemiBold().FontSize(9);
                        }
                    });
                });
        }

        private void RenderVoucherDetails(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
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
                            col.Item().Text($"Company: {journalData.Voucher.CompanyName}").FontSize(9);
                            if (!string.IsNullOrEmpty(journalData.Voucher.Description))
                            {
                                col.Item().Text($"Description: {journalData.Voucher.Description}").FontSize(9);
                            }
                            if (!string.IsNullOrEmpty(journalData.Voucher.RefNo))
                            {
                                col.Item().Text($"Reference No: {journalData.Voucher.RefNo}").FontSize(9);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Reference Information").FontSize(10).SemiBold();
                            if (!string.IsNullOrEmpty(journalData.Voucher.InvoiceNo))
                            {
                                col.Item().Text($"Invoice No: {journalData.Voucher.InvoiceNo}").FontSize(9);
                                if (journalData.Voucher.InvoiceDate.HasValue)
                                {
                                    col.Item().Text($"Invoice Date: {journalData.Voucher.InvoiceDate.Value:dd/MM/yyyy}").FontSize(9);
                                }
                            }
                            if (!string.IsNullOrEmpty(journalData.Voucher.ChequeNo))
                            {
                                col.Item().Text($"Cheque No: {journalData.Voucher.ChequeNo}").FontSize(9);
                                if (journalData.Voucher.ChequeDate.HasValue)
                                {
                                    col.Item().Text($"Cheque Date: {journalData.Voucher.ChequeDate.Value:dd/MM/yyyy}").FontSize(9);
                                }
                            }
                        });

                        if (!string.IsNullOrEmpty(journalData.Voucher.SupplierName) || !string.IsNullOrEmpty(journalData.Voucher.PaidTo))
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Party Information").FontSize(10).SemiBold();
                                if (!string.IsNullOrEmpty(journalData.Voucher.SupplierName))
                                {
                                    col.Item().Text($"Supplier: {journalData.Voucher.SupplierName}").FontSize(9);
                                }
                                if (!string.IsNullOrEmpty(journalData.Voucher.PaidTo))
                                {
                                    col.Item().Text($"Paid To: {journalData.Voucher.PaidTo}").FontSize(9);
                                }
                                if (!string.IsNullOrEmpty(journalData.Voucher.TaxRegNo))
                                {
                                    col.Item().Text($"Tax Reg No: {journalData.Voucher.TaxRegNo}").FontSize(9);
                                }
                                if (!string.IsNullOrEmpty(journalData.Voucher.City))
                                {
                                    col.Item().Text($"City: {journalData.Voucher.City}").FontSize(9);
                                }
                            });
                        }
                    });
                });
        }

        private void RenderJournalEntries(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
        {
            column.Item().Column(entriesColumn =>
            {
                entriesColumn.Item().Text("JOURNAL ENTRIES").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                entriesColumn.Item().PaddingTop(5);

                var tableConfig = new TableConfiguration<JournalVoucherLineInfo>
                {
                    Columns = new List<TableColumn<JournalVoucherLineInfo>>
                    {
                        new() { Header = "Account", PropertyName = "AccountName", Width = 3,
                                RenderCell = (c, item, cfg) => c.Column(col =>
                                {
                                    col.Item().Text(item.AccountName).FontSize(9);
                                    col.Item().Text($"({item.AccountCode})").FontSize(8).FontColor(Colors.Grey.Darken1);
                                }) },
                        new() { Header = "Description", PropertyName = "LineDescription", Width = 3,
                                RenderCell = (c, item, cfg) => c.Text(item.LineDescription).FontSize(9) },
                        new() { Header = "Cost Center", PropertyName = "LineCostCenter1Name", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.LineCostCenter1Name ?? "").FontSize(8) },
                        new() { Header = "Party", PropertyName = "CustomerFullName", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.CustomerFullName ?? item.LineSupplierName ?? "").FontSize(8) },
                        new() { Header = "Invoice", PropertyName = "LineInvoiceNo", Width = 2,
                                RenderCell = (c, item, cfg) => c.Column(col =>
                                {
                                    if (!string.IsNullOrEmpty(item.LineInvoiceNo))
                                    {
                                        col.Item().Text(item.LineInvoiceNo).FontSize(8);
                                        if (item.LineInvoiceDate.HasValue)
                                        {
                                            col.Item().Text(item.LineInvoiceDate.Value.ToString("dd/MM/yyyy")).FontSize(7);
                                        }
                                    }
                                }) },
                        new() { Header = "Debit", PropertyName = "DebitAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight()
                                    .Text(item.DebitAmount > 0 ? item.DebitAmount.ToString("N2") : "").FontSize(9) },
                        new() { Header = "Credit", PropertyName = "CreditAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight()
                                    .Text(item.CreditAmount > 0 ? item.CreditAmount.ToString("N2") : "").FontSize(9) }
                    },
                    ShowTotals = true,
                    TotalCalculators = new Dictionary<string, Func<IEnumerable<JournalVoucherLineInfo>, string>>
                    {
                        { "DebitAmount", lines => lines.Sum(l => l.DebitAmount).ToString("N2") },
                        { "CreditAmount", lines => lines.Sum(l => l.CreditAmount).ToString("N2") }
                    }
                };

                RenderTable(entriesColumn.Item(), journalData.Lines, tableConfig);
            });
        }

        private void RenderFinancialSummary(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
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
                                    .Text($"{journalData.Voucher.CurrencyName} {journalData.Voucher.TotalDebitAmount:N2}");
                            });

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Total Credits:");
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{journalData.Voucher.CurrencyName} {journalData.Voucher.TotalCreditAmount:N2}");
                            });

                            col.Item().BorderTop(1).PaddingTop(5);

                            var balanceDifference = Math.Abs(journalData.Voucher.TotalDebitAmount - journalData.Voucher.TotalCreditAmount);
                            var isBalanced = balanceDifference < 0.01m;

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("BALANCE CHECK:").SemiBold();
                                r.ConstantItem(120).AlignRight()
                                    .Text(isBalanced ? "BALANCED" : $"DIFF: {balanceDifference:N2}")
                                    .SemiBold().FontColor(isBalanced ? Colors.Green.Darken2 : Colors.Red.Darken2);
                            });

                            // Exchange rate information
                            if (journalData.Voucher.ExchangeRate != 1)
                            {
                                col.Item().PaddingTop(8).Text("Exchange Rate Information").FontSize(10).SemiBold();
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Rate:");
                                    r.ConstantItem(120).AlignRight().Text($"1 = {journalData.Voucher.ExchangeRate:N4}").FontSize(9);
                                });
                            }

                            // Reversal information
                            if (journalData.Voucher.IsReversed)
                            {
                                col.Item().PaddingTop(8).Background(Colors.Red.Lighten4).Padding(5).Column(reversalCol =>
                                {
                                    reversalCol.Item().Text("REVERSAL INFORMATION").FontSize(10).SemiBold().FontColor(Colors.Red.Darken2);
                                    if (!string.IsNullOrEmpty(journalData.Voucher.ReversalReason))
                                    {
                                        reversalCol.Item().Text($"Reason: {journalData.Voucher.ReversalReason}").FontSize(9);
                                    }
                                    if (journalData.Voucher.ReversedOn.HasValue)
                                    {
                                        reversalCol.Item().Text($"Reversed On: {journalData.Voucher.ReversedOn.Value:dd/MM/yyyy HH:mm}").FontSize(9);
                                        if (!string.IsNullOrEmpty(journalData.Voucher.ReversedBy))
                                        {
                                            reversalCol.Item().Text($"Reversed By: {journalData.Voucher.ReversedBy}").FontSize(9);
                                        }
                                    }
                                });
                            }
                        });
                    });
                });
        }

        private void RenderAttachmentsSection(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
        {
            column.Item().Column(attachColumn =>
            {
                attachColumn.Item().Text("SUPPORTING DOCUMENTS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                attachColumn.Item().PaddingTop(5);

                foreach (var attachment in journalData.Attachments.OrderBy(a => a.DisplayOrder))
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

                if (journalData.Attachments.Any(a => a.IsRequired))
                {
                    attachColumn.Item().PaddingTop(5).Text("* Required documents").FontSize(8).FontColor(Colors.Red.Darken2);
                }
            });
        }

        private void RenderNotesSection(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
        {
            column.Item().Column(notesColumn =>
            {
                notesColumn.Item().Text("NARRATION / NOTES").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));
                notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                    .Text(journalData.Voucher.Narration).FontSize(9);
            });
        }

        private void RenderApprovalSection(ColumnDescriptor column, JournalVoucherSlipData journalData, ReportConfiguration config)
        {
            column.Item().Column(approvalColumn =>
            {
                // Status information
                approvalColumn.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Prepared by: {journalData.Voucher.CreatedBy}").FontSize(9);
                        col.Item().Text($"Prepared on: {journalData.Voucher.CreatedOn:dd/MM/yyyy HH:mm}").FontSize(9);
                        if (!string.IsNullOrEmpty(journalData.Voucher.UpdatedBy))
                        {
                            col.Item().Text($"Last updated by: {journalData.Voucher.UpdatedBy}").FontSize(8);
                            col.Item().Text($"Updated on: {journalData.Voucher.UpdatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
                        }
                        if (!string.IsNullOrEmpty(journalData.Voucher.ApprovedBy))
                        {
                            col.Item().Text($"Approved by: {journalData.Voucher.ApprovedBy}").FontSize(8);
                            col.Item().Text($"Approved on: {journalData.Voucher.ApprovedOn:dd/MM/yyyy HH:mm}").FontSize(8);
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Status: {journalData.Voucher.PostingStatus}")
                            .SemiBold().FontColor(GetStatusColor(journalData.Voucher.PostingStatus));

                        if (journalData.Voucher.PostingStatus == "Posted")
                        {
                            col.Item().AlignRight().Text("✓ POSTED").FontColor(Colors.Green.Darken2).FontSize(9);
                        }
                        else if (journalData.Voucher.PostingStatus == "Pending")
                        {
                            col.Item().AlignRight().Text("⏳ PENDING APPROVAL").FontColor(Colors.Orange.Darken2).FontSize(9);
                        }
                        else if (journalData.Voucher.PostingStatus == "Draft")
                        {
                            col.Item().AlignRight().Text("📝 DRAFT STATUS").FontColor(Colors.Grey.Darken2).FontSize(9);
                        }
                        else if (journalData.Voucher.PostingStatus == "Rejected")
                        {
                            col.Item().AlignRight().Text("❌ REJECTED").FontColor(Colors.Red.Darken2).FontSize(9);
                        }

                        if (journalData.Voucher.IsReversed)
                        {
                            col.Item().AlignRight().Text("⚠ ENTRY REVERSED").FontColor(Colors.Red.Darken2).FontSize(9);
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
                            .Text($"({journalData.Voucher.CreatedBy})").FontSize(9);
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
                        "4. Journal entries must be balanced (Debits = Credits).",
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

        private JournalVoucherSlipData ParseJournalVoucherData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 3) return null;

            var journalVoucherSlipData = new JournalVoucherSlipData();

            // Parse voucher header data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                journalVoucherSlipData.Voucher = new JournalVoucherInfo
                {
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
                    ChequeNo = row["ChequeNo"]?.ToString(),
                    ChequeDate = row["ChequeDate"] != DBNull.Value ? Convert.ToDateTime(row["ChequeDate"]) : null,
                    //InvoiceNo = row["InvoiceNo"]?.ToString(),
                    //InvoiceDate = row["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(row["InvoiceDate"]) : null,
                    //PaidTo = row["PaidTo"]?.ToString(),
                    //RefNo = row["RefNo"]?.ToString(),
                    //SupplierID = row["SupplierID"] != DBNull.Value ? Convert.ToInt64(row["SupplierID"]) : null,
                    //TaxRegNo = row["TaxRegNo"]?.ToString(),
                    //City = row["City"]?.ToString(),
                    //PostingStatus = row["PostingStatus"]?.ToString() ?? "",
                    //CostCenter1ID = row["CostCenter1ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter1ID"]) : null,
                    //CostCenter2ID = row["CostCenter2ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter2ID"]) : null,
                    //CostCenter3ID = row["CostCenter3ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter3ID"]) : null,
                    //CostCenter4ID = row["CostCenter4ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter4ID"]) : null,
                    //TaxID = row["TaxID"] != DBNull.Value ? Convert.ToInt64(row["TaxID"]) : null,
                    //IsTaxInclusive = Convert.ToBoolean(row["IsTaxInclusive"]),
                    ReferenceType = row["ReferenceType"]?.ToString(),
                    ReferenceID = row["ReferenceID"] != DBNull.Value ? Convert.ToInt64(row["ReferenceID"]) : null,
                    ReferenceNo = row["ReferenceNo"]?.ToString(),
                    //IsReversed = Convert.ToBoolean(row["IsReversed"]),
                    //ReversalReason = row["ReversalReason"]?.ToString(),
                    //ReversedOn = row["ReversedOn"] != DBNull.Value ? Convert.ToDateTime(row["ReversedOn"]) : null,
                    //ReversedBy = row["ReversedBy"]?.ToString(),

                    // Related entity information
                    CompanyName = row["CompanyName"]?.ToString() ?? "",
                    FYDescription = row["FYDescription"]?.ToString() ?? "",
                    CurrencyName = row["CurrencyName"]?.ToString() ?? "",
                    //SupplierName = row["SupplierName"]?.ToString(),
                    //CostCenter1Name = row["CostCenter1Name"]?.ToString(),
                    //CostCenter2Name = row["CostCenter2Name"]?.ToString(),
                    //CostCenter3Name = row["CostCenter3Name"]?.ToString(),
                    //CostCenter4Name = row["CostCenter4Name"]?.ToString(),
                    //TaxName = row["TaxName"]?.ToString(),
                    //TaxRate = row["TaxRate"] != DBNull.Value ? Convert.ToDecimal(row["TaxRate"]) : null,

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
                ParseVoucherLinesData(journalVoucherSlipData, dataSet.Tables[1]);
            }

            // Parse attachments data (Table 2)
            if (dataSet.Tables.Count > 2)
            {
                ParseAttachmentsData(journalVoucherSlipData, dataSet.Tables[2]);
            }

            // Calculate totals
            if (journalVoucherSlipData.Lines.Any())
            {
                journalVoucherSlipData.Voucher.TotalDebitAmount = journalVoucherSlipData.Lines.Sum(l => l.DebitAmount);
                journalVoucherSlipData.Voucher.TotalCreditAmount = journalVoucherSlipData.Lines.Sum(l => l.CreditAmount);
            }

            return journalVoucherSlipData;
        }

        private void ParseVoucherLinesData(JournalVoucherSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Lines.Add(new JournalVoucherLineInfo
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
                    //LineInvoiceNo = row["LineInvoiceNo"]?.ToString(),
                    //LineInvoiceDate = row["LineInvoiceDate"] != DBNull.Value ? Convert.ToDateTime(row["LineInvoiceDate"]) : null,
                    //LineSupplierID = row["LineSupplierID"] != DBNull.Value ? Convert.ToInt64(row["LineSupplierID"]) : null,
                    //LineTRN = row["LineTRN"]?.ToString(),
                    //LineCity = row["LineCity"]?.ToString(),
                    LineCostCenter1ID = row["CostCenter1ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter1ID"]) : null,
                    LineCostCenter2ID = row["CostCenter2ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter2ID"]) : null,
                    LineCostCenter3ID = row["CostCenter3ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter3ID"]) : null,
                    LineCostCenter4ID = row["CostCenter4ID"] != DBNull.Value ? Convert.ToInt64(row["CostCenter4ID"]) : null,
                    CustomerID = row["CustomerID"] != DBNull.Value ? Convert.ToInt64(row["CustomerID"]) : null,
                    BaseCurrencyAmount = Convert.ToDecimal(row["BaseCurrencyAmount"]),

                    // Related entity information
                    AccountCode = row["AccountCode"]?.ToString() ?? "",
                    AccountName = row["AccountName"]?.ToString() ?? "",
                    LineCostCenter1Name = row["CostCenter1Name"]?.ToString(),
                    LineCostCenter2Name = row["CostCenter2Name"]?.ToString(),
                    LineCostCenter3Name = row["CostCenter3Name"]?.ToString(),
                    LineCostCenter4Name = row["CostCenter4Name"]?.ToString(),
                    CustomerFullName = row["CustomerFullName"]?.ToString(),
                    LineSupplierName = row["SupplierName"]?.ToString()
                });
            }
        }

        private void ParseAttachmentsData(JournalVoucherSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Attachments.Add(new JournalVoucherAttachmentInfo
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