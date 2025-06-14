// LeaseERP.Core/Services/Reports/Invoices/InvoiceSlipTemplate.cs
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
    public class InvoiceSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "invoice-slip";

        public InvoiceSlipTemplate(
            ILogger<InvoiceSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "invoice-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "LEASE INVOICE",
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
                    HeaderColor = "#1e40af",
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var invoiceData = ParseInvoiceData(data.DataSet);
            if (invoiceData?.Invoice == null)
            {
                container.AlignCenter().Padding(20).Text("Invoice data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Invoice Header Information
                RenderInvoiceHeader(column, invoiceData, config);

                column.Item().PaddingVertical(10);

                // Customer and Contract Information
                RenderCustomerContractInfo(column, invoiceData, config);

                column.Item().PaddingVertical(10);

                // Property and Unit Information
                RenderPropertyUnitInfo(column, invoiceData, config);

                column.Item().PaddingVertical(10);

                // Invoice Period and Details
                RenderInvoiceDetails(column, invoiceData, config);

                column.Item().PaddingVertical(10);

                // Financial Summary
                RenderFinancialSummary(column, invoiceData, config);

                // Payment History
                if (invoiceData.Payments.Any())
                {
                    column.Item().PaddingVertical(10);
                    RenderPaymentHistory(column, invoiceData, config);
                }

                // Notes Section
                if (!string.IsNullOrEmpty(invoiceData.Invoice.Notes) || !string.IsNullOrEmpty(invoiceData.Invoice.InternalNotes))
                {
                    column.Item().PaddingVertical(10);
                    RenderNotesSection(column, invoiceData, config);
                }

                // Terms and Conditions
                column.Item().PaddingVertical(15);
                RenderTermsAndConditions(column, invoiceData, config);

                // Status and Signature Section
                column.Item().PaddingVertical(15);
                RenderStatusAndSignatures(column, invoiceData, config);
            });
        }

        private void RenderInvoiceHeader(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Background(GetStatusBackgroundColor(invoiceData.Invoice.InvoiceStatus))
                .Padding(12).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Invoice No: {invoiceData.Invoice.InvoiceNo}")
                            .SemiBold().FontSize(14).FontColor(GetStatusTextColor(invoiceData.Invoice.InvoiceStatus));
                        col.Item().Text($"Type: {invoiceData.Invoice.InvoiceType}").FontSize(10);
                        col.Item().Text($"Status: {invoiceData.Invoice.InvoiceStatus}")
                            .SemiBold().FontColor(GetStatusColor(invoiceData.Invoice.InvoiceStatus));
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Invoice Date: {invoiceData.Invoice.InvoiceDate:dd/MM/yyyy}").FontSize(10);
                        col.Item().Text($"Due Date: {invoiceData.Invoice.DueDate:dd/MM/yyyy}").FontSize(10);

                        if (invoiceData.Invoice.IsOverdue)
                        {
                            col.Item().Text($"⚠ OVERDUE: {invoiceData.Invoice.DaysOverdue} days")
                                .FontSize(9).FontColor(Colors.Red.Darken2).SemiBold();
                        }
                    });

                    row.RelativeItem().Column(col =>
                    {
                        col.Item().AlignRight().Text($"Currency: {invoiceData.Invoice.CurrencyCode}").FontSize(10);
                        if (invoiceData.Invoice.ExchangeRate != 1)
                        {
                            col.Item().AlignRight().Text($"Exchange Rate: {invoiceData.Invoice.ExchangeRate:N4}").FontSize(9);
                        }
                        if (invoiceData.Invoice.IsPosted)
                        {
                            col.Item().AlignRight().Text("✓ POSTED").FontColor(Colors.Green.Darken2).SemiBold().FontSize(9);
                        }
                    });
                });
        }

        private void RenderCustomerContractInfo(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Border(1).BorderColor(ParseColor(config.Styling.BorderColor))
                .Padding(12).Column(infoColumn =>
                {
                    infoColumn.Item().Text("CUSTOMER & CONTRACT INFORMATION")
                        .FontSize(12).SemiBold().FontColor(ParseColor(config.Styling.HeaderColor));

                    infoColumn.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Customer Information").FontSize(10).SemiBold();
                            col.Item().Text($"Name: {invoiceData.Invoice.CustomerFullName}").FontSize(9);
                            col.Item().Text($"Customer No: {invoiceData.Invoice.CustomerNo}").FontSize(9);
                            if (!string.IsNullOrEmpty(invoiceData.Invoice.CustomerTaxNo))
                            {
                                col.Item().Text($"Tax Reg No: {invoiceData.Invoice.CustomerTaxNo}").FontSize(9);
                            }
                        });

                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Contract Information").FontSize(10).SemiBold();
                            col.Item().Text($"Contract No: {invoiceData.Invoice.ContractNo}").FontSize(9);
                            col.Item().Text($"Contract Status: {invoiceData.Invoice.ContractStatus}").FontSize(9);
                            if (!string.IsNullOrEmpty(invoiceData.Invoice.SalesPersonName))
                            {
                                col.Item().Text($"Sales Person: {invoiceData.Invoice.SalesPersonName}").FontSize(9);
                            }
                        });
                    });
                });
        }

        private void RenderPropertyUnitInfo(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Property Details").FontSize(10).SemiBold();
                    col.Item().Text($"Property: {invoiceData.Invoice.PropertyName}").FontSize(9);
                    if (!string.IsNullOrEmpty(invoiceData.Invoice.PropertyNo))
                    {
                        col.Item().Text($"Property No: {invoiceData.Invoice.PropertyNo}").FontSize(9);
                    }
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Unit Details").FontSize(10).SemiBold();
                    col.Item().Text($"Unit No: {invoiceData.Invoice.UnitNo}").FontSize(9);
                    col.Item().Text($"Unit Status: {invoiceData.Invoice.UnitStatus}").FontSize(9);
                });
            });
        }

        private void RenderInvoiceDetails(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Column(detailColumn =>
            {
                detailColumn.Item().Text("INVOICE DETAILS").FontSize(12).SemiBold()
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
                            .Text("Billing Period From").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Billing Period To").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Payment Terms").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(8)
                            .Text("Tax Information").FontColor(Colors.White).FontSize(9).SemiBold();
                    });

                    table.Cell().Padding(8)
                        .Text(invoiceData.Invoice.PeriodFromDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(9);
                    table.Cell().Padding(8)
                        .Text(invoiceData.Invoice.PeriodToDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(9);
                    table.Cell().Padding(8)
                        .Text(invoiceData.Invoice.PaymentTermName ?? "Standard").FontSize(9);
                    table.Cell().Padding(8).Column(taxCol =>
                    {
                        if (!string.IsNullOrEmpty(invoiceData.Invoice.TaxName))
                        {
                            taxCol.Item().Text($"{invoiceData.Invoice.TaxName}").FontSize(9);
                            if (invoiceData.Invoice.TaxRate.HasValue)
                            {
                                taxCol.Item().Text($"Rate: {invoiceData.Invoice.TaxRate.Value:N2}%").FontSize(8);
                            }
                        }
                        else
                        {
                            taxCol.Item().Text("No Tax").FontSize(9);
                        }
                    });
                });

                // Recurring Information
                if (invoiceData.Invoice.IsRecurring)
                {
                    detailColumn.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text("Recurring Invoice").SemiBold().FontSize(9);
                        if (!string.IsNullOrEmpty(invoiceData.Invoice.RecurrencePattern))
                        {
                            row.RelativeItem().Text($"Pattern: {invoiceData.Invoice.RecurrencePattern}").FontSize(9);
                        }
                        if (invoiceData.Invoice.NextInvoiceDate.HasValue)
                        {
                            row.RelativeItem().AlignRight()
                                .Text($"Next Invoice: {invoiceData.Invoice.NextInvoiceDate.Value:dd/MM/yyyy}").FontSize(9);
                        }
                    });
                }
            });
        }

        private void RenderFinancialSummary(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
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
                                r.RelativeItem().Text("Subtotal:");
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{invoiceData.Invoice.CurrencyCode} {invoiceData.Invoice.SubTotal:N2}");
                            });

                            if (invoiceData.Invoice.TaxAmount > 0)
                            {
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Tax Amount:");
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"{invoiceData.Invoice.CurrencyCode} {invoiceData.Invoice.TaxAmount:N2}");
                                });
                            }

                            if (invoiceData.Invoice.DiscountAmount > 0)
                            {
                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("Discount:");
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"({invoiceData.Invoice.CurrencyCode} {invoiceData.Invoice.DiscountAmount:N2})")
                                        .FontColor(Colors.Red.Darken1);
                                });
                            }

                            col.Item().BorderTop(1).PaddingTop(8);

                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("TOTAL AMOUNT:").SemiBold().FontSize(12);
                                r.ConstantItem(120).AlignRight()
                                    .Text($"{invoiceData.Invoice.CurrencyCode} {invoiceData.Invoice.TotalAmount:N2}")
                                    .SemiBold().FontSize(12).FontColor(ParseColor(config.Styling.HeaderColor));
                            });

                            if (invoiceData.Invoice.PaidAmount > 0)
                            {
                                col.Item().PaddingTop(5).Row(r =>
                                {
                                    r.RelativeItem().Text("Paid Amount:");
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"{invoiceData.Invoice.CurrencyCode} {invoiceData.Invoice.PaidAmount:N2}")
                                        .FontColor(Colors.Green.Darken2);
                                });

                                col.Item().Row(r =>
                                {
                                    r.RelativeItem().Text("BALANCE DUE:").SemiBold();
                                    r.ConstantItem(120).AlignRight()
                                        .Text($"{invoiceData.Invoice.CurrencyCode} {invoiceData.Invoice.BalanceAmount:N2}")
                                        .SemiBold().FontColor(invoiceData.Invoice.BalanceAmount > 0 ? Colors.Red.Darken2 : Colors.Green.Darken2);
                                });
                            }
                        });
                    });
                });
        }

        private void RenderPaymentHistory(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Column(paymentColumn =>
            {
                paymentColumn.Item().Text("PAYMENT HISTORY").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                paymentColumn.Item().PaddingTop(5);

                var tableConfig = new TableConfiguration<InvoicePaymentInfo>
                {
                    Columns = new List<TableColumn<InvoicePaymentInfo>>
                    {
                        new() { Header = "Receipt No", PropertyName = "ReceiptNo", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.ReceiptNo).FontSize(9) },
                        new() { Header = "Date", PropertyName = "ReceiptDate", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.ReceiptDate.ToString("dd/MM/yyyy")).FontSize(9) },
                        new() { Header = "Amount", PropertyName = "ReceivedAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text($"{invoiceData.Invoice.CurrencyCode} {item.ReceivedAmount:N2}").FontSize(9) },
                        new() { Header = "Type", PropertyName = "PaymentType", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.PaymentType).FontSize(9) },
                        new() { Header = "Reference", PropertyName = "TransactionReference", Width = 2,
                                RenderCell = (c, item, cfg) => c.Text(item.TransactionReference ?? "").FontSize(9) },
                        new() { Header = "Status", PropertyName = "PaymentStatus", Width = 1,
                                RenderCell = (c, item, cfg) => c.Text(item.PaymentStatus).FontSize(9)
                                    .FontColor(item.PaymentStatus == "Cleared" ? Colors.Green.Darken2 : Colors.Orange.Darken1) }
                    },
                    ShowTotals = true,
                    TotalCalculators = new Dictionary<string, Func<IEnumerable<InvoicePaymentInfo>, string>>
                    {
                        { "ReceivedAmount", payments => $"{invoiceData.Invoice.CurrencyCode} {payments.Sum(p => p.ReceivedAmount):N2}" }
                    }
                };

                RenderTable(paymentColumn.Item(), invoiceData.Payments, tableConfig);
            });
        }

        private void RenderNotesSection(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Column(notesColumn =>
            {
                if (!string.IsNullOrEmpty(invoiceData.Invoice.Notes))
                {
                    notesColumn.Item().Text("NOTES").FontSize(12).SemiBold()
                        .FontColor(ParseColor(config.Styling.HeaderColor));
                    notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                        .Text(invoiceData.Invoice.Notes).FontSize(9);
                }

                if (!string.IsNullOrEmpty(invoiceData.Invoice.InternalNotes))
                {
                    notesColumn.Item().PaddingTop(8).Text("INTERNAL NOTES").FontSize(12).SemiBold()
                        .FontColor(Colors.Grey.Darken2);
                    notesColumn.Item().PaddingTop(5).Border(1).BorderColor(Colors.Grey.Medium).Padding(8)
                        .Text(invoiceData.Invoice.InternalNotes).FontSize(9).FontColor(Colors.Grey.Darken2);
                }
            });
        }

        private void RenderTermsAndConditions(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Column(termsColumn =>
            {
                termsColumn.Item().Text("TERMS & CONDITIONS").FontSize(10).SemiBold();

                var terms = new[]
                {
                    "1. Payment is due within the specified payment terms from the invoice date.",
                    "2. Late payment charges may apply for overdue amounts.",
                    "3. All payments should reference the invoice number.",
                    "4. Disputes must be raised within 7 days of invoice date.",
                    "5. This invoice is computer generated and does not require a signature."
                };

                foreach (var term in terms)
                {
                    termsColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"{term}").FontSize(8);
                    });
                }
            });
        }

        private void RenderStatusAndSignatures(ColumnDescriptor column, InvoiceSlipData invoiceData, ReportConfiguration config)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Generated by: {invoiceData.Invoice.CreatedBy}").FontSize(9);
                    col.Item().Text($"Generated on: {invoiceData.Invoice.CreatedOn:dd/MM/yyyy HH:mm}").FontSize(9);
                    if (!string.IsNullOrEmpty(invoiceData.Invoice.UpdatedBy))
                    {
                        col.Item().Text($"Last updated by: {invoiceData.Invoice.UpdatedBy}").FontSize(8);
                        col.Item().Text($"Updated on: {invoiceData.Invoice.UpdatedOn:dd/MM/yyyy HH:mm}").FontSize(8);
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

        private string GetStatusBackgroundColor(string status)
        {
            return status?.ToUpper() switch
            {
                "DRAFT" => Colors.Grey.Lighten4,
                "PENDING" => Colors.Orange.Lighten4,
                "APPROVED" => Colors.Blue.Lighten4,
                "ACTIVE" => Colors.Green.Lighten4,
                "PAID" => Colors.Green.Lighten4,
                "CANCELLED" => Colors.Red.Lighten4,
                "VOIDED" => Colors.Red.Lighten4,
                _ => Colors.Grey.Lighten5
            };
        }

        private string GetStatusTextColor(string status)
        {
            return status?.ToUpper() switch
            {
                "PAID" => Colors.Green.Darken2,
                "CANCELLED" => Colors.Red.Darken2,
                "VOIDED" => Colors.Red.Darken2,
                _ => Colors.Black
            };
        }

        private InvoiceSlipData ParseInvoiceData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 3) return null;

            var invoiceSlipData = new InvoiceSlipData();

            // Parse invoice master data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                invoiceSlipData.Invoice = new InvoiceMasterInfo
                {
                    LeaseInvoiceID = Convert.ToInt64(row["LeaseInvoiceID"]),
                    InvoiceNo = row["InvoiceNo"]?.ToString() ?? "",
                    InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
                    DueDate = Convert.ToDateTime(row["DueDate"]),
                    ContractID = Convert.ToInt64(row["ContractID"]),
                    ContractUnitID = Convert.ToInt64(row["ContractUnitID"]),
                    CustomerID = Convert.ToInt64(row["CustomerID"]),
                    CompanyID = Convert.ToInt64(row["CompanyID"]),
                    FiscalYearID = Convert.ToInt64(row["FiscalYearID"]),
                    InvoiceType = row["InvoiceType"]?.ToString() ?? "",
                    InvoiceStatus = row["InvoiceStatus"]?.ToString() ?? "",
                    PeriodFromDate = row["PeriodFromDate"] != DBNull.Value ? Convert.ToDateTime(row["PeriodFromDate"]) : null,
                    PeriodToDate = row["PeriodToDate"] != DBNull.Value ? Convert.ToDateTime(row["PeriodToDate"]) : null,
                    SubTotal = Convert.ToDecimal(row["SubTotal"]),
                    TaxAmount = Convert.ToDecimal(row["TaxAmount"]),
                    DiscountAmount = Convert.ToDecimal(row["DiscountAmount"]),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    PaidAmount = Convert.ToDecimal(row["PaidAmount"]),
                    BalanceAmount = Convert.ToDecimal(row["BalanceAmount"]),
                    CurrencyID = Convert.ToInt64(row["CurrencyID"]),
                    ExchangeRate = Convert.ToDecimal(row["ExchangeRate"]),
                    PaymentTermID = row["PaymentTermID"] != DBNull.Value ? Convert.ToInt64(row["PaymentTermID"]) : null,
                    SalesPersonID = row["SalesPersonID"] != DBNull.Value ? Convert.ToInt64(row["SalesPersonID"]) : null,
                    TaxID = row["TaxID"] != DBNull.Value ? Convert.ToInt64(row["TaxID"]) : null,
                    IsRecurring = Convert.ToBoolean(row["IsRecurring"]),
                    RecurrencePattern = row["RecurrencePattern"]?.ToString(),
                    NextInvoiceDate = row["NextInvoiceDate"] != DBNull.Value ? Convert.ToDateTime(row["NextInvoiceDate"]) : null,
                    Notes = row["Notes"]?.ToString(),
                    InternalNotes = row["InternalNotes"]?.ToString(),

                    // Related entity information
                    CustomerFullName = row["CustomerName"]?.ToString() ?? "",
                    CustomerNo = row["CustomerNo"]?.ToString() ?? "",
                    CustomerTaxNo = row["CustomerTaxNo"]?.ToString() ?? "",
                    ContractNo = row["ContractNo"]?.ToString() ?? "",
                    ContractStatus = row["ContractStatus"]?.ToString() ?? "",
                    UnitNo = row["UnitNo"]?.ToString() ?? "",
                    UnitStatus = row["UnitStatus"]?.ToString() ?? "",
                    PropertyName = row["PropertyName"]?.ToString() ?? "",
                    PropertyNo = row["PropertyNo"]?.ToString() ?? "",
                    CurrencyCode = row["CurrencyCode"]?.ToString() ?? "",
                    CurrencyName = row["CurrencyName"]?.ToString() ?? "",
                    PaymentTermName = row["PaymentTermName"]?.ToString(),
                    SalesPersonName = row["SalesPersonName"]?.ToString(),
                    TaxName = row["TaxName"]?.ToString(),
                    TaxRate = row["TaxRate"] != DBNull.Value ? Convert.ToDecimal(row["TaxRate"]) : null,

                    // Status information
                    IsOverdue = Convert.ToBoolean(row["IsOverdue"]),
                    DaysOverdue = Convert.ToInt32(row["DaysOverdue"]),
                    IsPosted = Convert.ToBoolean(row["IsPosted"]),

                    // Audit information
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString(),
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                };
            }

            // Parse payments data (Table 1)
            if (dataSet.Tables.Count > 1)
            {
                ParsePaymentsData(invoiceSlipData, dataSet.Tables[1]);
            }

            // Parse postings data (Table 2)
            if (dataSet.Tables.Count > 2)
            {
                ParsePostingsData(invoiceSlipData, dataSet.Tables[2]);
            }

            return invoiceSlipData;
        }

        private void ParsePaymentsData(InvoiceSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Payments.Add(new InvoicePaymentInfo
                {
                    LeaseReceiptID = Convert.ToInt64(row["LeaseReceiptID"]),
                    ReceiptNo = row["ReceiptNo"]?.ToString() ?? "",
                    ReceiptDate = Convert.ToDateTime(row["ReceiptDate"]),
                    ReceivedAmount = Convert.ToDecimal(row["ReceivedAmount"]),
                    PaymentType = row["PaymentType"]?.ToString() ?? "",
                    PaymentStatus = row["PaymentStatus"]?.ToString() ?? "",
                    BankAccountNo = row["BankAccountNo"]?.ToString(),
                    ChequeNo = row["ChequeNo"]?.ToString(),
                    ChequeDate = row["ChequeDate"] != DBNull.Value ? Convert.ToDateTime(row["ChequeDate"]) : null,
                    TransactionReference = row["TransactionReference"]?.ToString(),
                    Notes = row["Notes"]?.ToString(),
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"])
                });
            }
        }

        private void ParsePostingsData(InvoiceSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Postings.Add(new InvoicePostingInfo
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