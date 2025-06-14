// LeaseERP.Core/Services/Reports/Termination/TerminationSlipTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.Termination
{
    public class TerminationSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "termination-slip";

        public TerminationSlipTemplate(
            ILogger<TerminationSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "termination-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "CONTRACT TERMINATION SLIP",
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
                    HeaderColor = "#dc2626", // Red color for termination
                    BorderColor = "#e5e7eb"
                }
            };
        }

        protected override void RenderContent(IContainer container, ReportData data, ReportConfiguration config)
        {
            var terminationData = ParseTerminationData(data.DataSet);
            if (terminationData?.Termination == null)
            {
                container.AlignCenter().Padding(20).Text("Termination data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Termination Header Information
                RenderTerminationHeader(column, terminationData, config);

                column.Item().PaddingVertical(10);

                // Contract and Customer Information
                RenderContractCustomerInfo(column, terminationData, config);

                column.Item().PaddingVertical(10);

                // Termination Timeline Section
                RenderTerminationTimeline(column, terminationData, config);

                column.Item().PaddingVertical(10);

                // Deductions Section
                if (terminationData.Deductions.Any())
                {
                    RenderDeductionsSection(column, terminationData, config);
                    column.Item().PaddingVertical(10);
                }

                // Financial Summary Section
                RenderFinancialSummary(column, terminationData, config);

                // Termination Reason and Notes
                if (!string.IsNullOrEmpty(terminationData.Termination.TerminationReason) ||
                    !string.IsNullOrEmpty(terminationData.Termination.Notes))
                {
                    column.Item().PaddingVertical(10);
                    RenderNotesSection(column, terminationData, config);
                }

                // Attachments Section
                if (terminationData.Attachments.Any())
                {
                    column.Item().PaddingVertical(10);
                    RenderAttachmentsSection(column, terminationData, config);
                }

                // Signatures Section
                column.Item().PaddingVertical(20);
                RenderSignaturesSection(column, terminationData, config);
            });
        }

        private void RenderTerminationHeader(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Red.Lighten4).Padding(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Termination No: {terminationData.Termination.TerminationNo}").SemiBold().FontSize(12);
                    col.Item().Text($"Status: {terminationData.Termination.TerminationStatus}")
                        .FontColor(GetStatusColor(terminationData.Termination.TerminationStatus));
                    col.Item().Text($"Termination Date: {terminationData.Termination.TerminationDate:dd/MM/yyyy}");
                });

                row.RelativeItem().Column(col =>
                {
                    if (terminationData.Termination.EffectiveDate.HasValue)
                        col.Item().Text($"Effective Date: {terminationData.Termination.EffectiveDate.Value:dd/MM/yyyy}");

                    col.Item().Text($"Created By: {terminationData.Termination.CreatedBy}");
                    col.Item().Text($"Created On: {terminationData.Termination.CreatedOn:dd/MM/yyyy}");
                });
            });
        }

        private void RenderContractCustomerInfo(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Border(1).Padding(10).Column(contractColumn =>
            {
                contractColumn.Item().Text("CONTRACT & CUSTOMER INFORMATION").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                contractColumn.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Contract No: {terminationData.Termination.ContractNo}").SemiBold();
                        col.Item().Text($"Customer: {terminationData.Termination.CustomerFullName}").SemiBold();
                        if (!string.IsNullOrEmpty(terminationData.Termination.PropertyName))
                            col.Item().Text($"Property: {terminationData.Termination.PropertyName}");
                    });

                    row.RelativeItem().Column(col =>
                    {
                        if (!string.IsNullOrEmpty(terminationData.Termination.UnitNumbers))
                            col.Item().Text($"Unit(s): {terminationData.Termination.UnitNumbers}");

                        if (terminationData.Termination.SecurityDepositAmount.HasValue)
                            col.Item().Text($"Security Deposit: {terminationData.Termination.SecurityDepositAmount.Value:N2}").SemiBold();
                    });
                });
            });
        }

        private void RenderTerminationTimeline(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Column(timelineColumn =>
            {
                timelineColumn.Item().Text("TERMINATION TIMELINE").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                timelineColumn.Item().PaddingTop(8).Table(table =>
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
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(5)
                            .Text("Notice Date").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(5)
                            .Text("Vacating Date").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(5)
                            .Text("Move Out Date").FontColor(Colors.White).FontSize(9).SemiBold();
                        header.Cell().Background(ParseColor(config.Styling.HeaderColor)).Padding(5)
                            .Text("Key Return Date").FontColor(Colors.White).FontSize(9).SemiBold();
                    });

                    table.Cell().Padding(5)
                        .Text(terminationData.Termination.NoticeDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(9);
                    table.Cell().Padding(5)
                        .Text(terminationData.Termination.VacatingDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(9);
                    table.Cell().Padding(5)
                        .Text(terminationData.Termination.MoveOutDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(9);
                    table.Cell().Padding(5)
                        .Text(terminationData.Termination.KeyReturnDate?.ToString("dd/MM/yyyy") ?? "N/A").FontSize(9);
                });

                // Stay Period Information
                if (terminationData.Termination.StayPeriodDays.HasValue || terminationData.Termination.StayPeriodAmount.HasValue)
                {
                    timelineColumn.Item().PaddingTop(8).Row(row =>
                    {
                        if (terminationData.Termination.StayPeriodDays.HasValue)
                            row.RelativeItem().Text($"Stay Period: {terminationData.Termination.StayPeriodDays.Value} days").FontSize(9);

                        if (terminationData.Termination.StayPeriodAmount.HasValue)
                            row.RelativeItem().AlignRight().Text($"Stay Period Amount: {terminationData.Termination.StayPeriodAmount.Value:N2}").FontSize(9);
                    });
                }
            });
        }

        private void RenderDeductionsSection(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Column(deductionColumn =>
            {
                deductionColumn.Item().Text("DEDUCTIONS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                deductionColumn.Item().PaddingTop(5);

                var tableConfig = new TableConfiguration<TerminationDeductionInfo>
                {
                    Columns = new List<TableColumn<TerminationDeductionInfo>>
                    {
                        new() { Header = "Deduction", PropertyName = "DeductionName", Width = 3,
                                RenderCell = (c, item, cfg) => c.Text(item.DeductionName).FontSize(9) },
                        new() { Header = "Description", PropertyName = "DeductionDescription", Width = 3,
                                RenderCell = (c, item, cfg) => c.Text(item.DeductionDescription ?? "").FontSize(9) },
                        new() { Header = "Amount", PropertyName = "DeductionAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text(item.DeductionAmount.ToString("N2")).FontSize(9) },
                        new() { Header = "Tax %", PropertyName = "TaxPercentage", Width = 1, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TaxPercentage?.ToString("N1") ?? "0.0").FontSize(9) },
                        new() { Header = "Tax Amt", PropertyName = "TaxAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TaxAmount?.ToString("N2") ?? "0.00").FontSize(9) },
                        new() { Header = "Total", PropertyName = "TotalAmount", Width = 2, Alignment = TextAlignment.Right,
                                RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TotalAmount.ToString("N2")).FontSize(9).SemiBold() }
                    },
                    ShowTotals = true,
                    TotalCalculators = new Dictionary<string, Func<IEnumerable<TerminationDeductionInfo>, string>>
                    {
                        { "TotalAmount", deductions => deductions.Sum(d => d.TotalAmount).ToString("N2") }
                    }
                };

                RenderTable(deductionColumn.Item(), terminationData.Deductions, tableConfig);
            });
        }

        private void RenderFinancialSummary(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Border(2).BorderColor(ParseColor(config.Styling.HeaderColor)).Padding(15).Column(summaryColumn =>
            {
                summaryColumn.Item().Text("FINANCIAL SUMMARY").FontSize(14).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor)).AlignCenter();

                summaryColumn.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem(3);
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Security Deposit:");
                            r.ConstantItem(100).AlignRight()
                                .Text((terminationData.Termination.SecurityDepositAmount ?? 0).ToString("N2"));
                        });

                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Deductions:");
                            r.ConstantItem(100).AlignRight()
                                .Text((terminationData.Termination.TotalDeductions ?? 0).ToString("N2"));
                        });

                        if (terminationData.Termination.AdjustAmount.HasValue && terminationData.Termination.AdjustAmount != 0)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Adjustment Amount:");
                                r.ConstantItem(100).AlignRight()
                                    .Text(terminationData.Termination.AdjustAmount.Value.ToString("N2"));
                            });
                        }

                        col.Item().BorderTop(1).PaddingTop(5);

                        if (terminationData.Termination.RefundAmount > 0)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("REFUND AMOUNT:").SemiBold().FontColor(Colors.Green.Darken2);
                                r.ConstantItem(100).AlignRight()
                                    .Text(terminationData.Termination.RefundAmount?.ToString("N2") ?? "0.00")
                                    .SemiBold().FontColor(Colors.Green.Darken2).FontSize(12);
                            });

                            if (terminationData.Termination.IsRefundProcessed)
                            {
                                col.Item().PaddingTop(3).Text("✓ Refund Processed").FontColor(Colors.Green.Darken1).FontSize(9);
                                if (terminationData.Termination.RefundDate.HasValue)
                                {
                                    col.Item().Text($"Date: {terminationData.Termination.RefundDate.Value:dd/MM/yyyy}").FontSize(8);
                                }
                                if (!string.IsNullOrEmpty(terminationData.Termination.RefundReference))
                                {
                                    col.Item().Text($"Ref: {terminationData.Termination.RefundReference}").FontSize(8);
                                }
                            }
                            else
                            {
                                col.Item().PaddingTop(3).Text("⚠ Refund Pending").FontColor(Colors.Orange.Darken1).FontSize(9);
                            }
                        }

                        if (terminationData.Termination.CreditNoteAmount > 0)
                        {
                            col.Item().Row(r =>
                            {
                                r.RelativeItem().Text("CREDIT NOTE AMOUNT:").SemiBold().FontColor(Colors.Red.Darken2);
                                r.ConstantItem(100).AlignRight()
                                    .Text(terminationData.Termination.CreditNoteAmount?.ToString("N2") ?? "0.00")
                                    .SemiBold().FontColor(Colors.Red.Darken2).FontSize(12);
                            });
                        }
                    });
                });
            });
        }

        private void RenderNotesSection(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Column(notesColumn =>
            {
                if (!string.IsNullOrEmpty(terminationData.Termination.TerminationReason))
                {
                    notesColumn.Item().Text("TERMINATION REASON").FontSize(12).SemiBold()
                        .FontColor(ParseColor(config.Styling.HeaderColor));
                    notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                        .Text(terminationData.Termination.TerminationReason).FontSize(9);
                    notesColumn.Item().PaddingVertical(8);
                }

                if (!string.IsNullOrEmpty(terminationData.Termination.Notes))
                {
                    notesColumn.Item().Text("ADDITIONAL NOTES").FontSize(12).SemiBold()
                        .FontColor(ParseColor(config.Styling.HeaderColor));
                    notesColumn.Item().PaddingTop(5).Border(1).Padding(8)
                        .Text(terminationData.Termination.Notes).FontSize(9);
                }
            });
        }

        private void RenderAttachmentsSection(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().Column(attachColumn =>
            {
                attachColumn.Item().Text("ATTACHMENTS").FontSize(12).SemiBold()
                    .FontColor(ParseColor(config.Styling.HeaderColor));

                attachColumn.Item().PaddingTop(5);

                foreach (var attachment in terminationData.Attachments)
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
                    });
                }
            });
        }

        private void RenderSignaturesSection(ColumnDescriptor column, TerminationSlipData terminationData, ReportConfiguration config)
        {
            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                        .Text("Tenant Signature").FontSize(10);
                    col.Item().PaddingTop(5).AlignCenter()
                        .Text("Date: ____________________").FontSize(9);
                });

                row.RelativeItem(2);

                row.RelativeItem().Column(col =>
                {
                    col.Item().BorderTop(1).PaddingTop(5).AlignCenter()
                        .Text("Landlord/Agent Signature").FontSize(10);
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
                "COMPLETED" => Colors.Green.Darken1,
                "CANCELLED" => Colors.Red.Darken1,
                _ => Colors.Black
            };
        }

        private TerminationSlipData ParseTerminationData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 3) return null;

            var terminationSlipData = new TerminationSlipData();

            // Parse termination master data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                terminationSlipData.Termination = new TerminationMasterInfo
                {
                    TerminationID = Convert.ToInt64(row["TerminationID"]),
                    TerminationNo = row["TerminationNo"]?.ToString() ?? "",
                    ContractID = Convert.ToInt64(row["ContractID"]),
                    ContractNo = row["ContractNo"]?.ToString() ?? "",
                    TerminationDate = Convert.ToDateTime(row["TerminationDate"]),
                    NoticeDate = row["NoticeDate"] != DBNull.Value ? Convert.ToDateTime(row["NoticeDate"]) : null,
                    EffectiveDate = row["EffectiveDate"] != DBNull.Value ? Convert.ToDateTime(row["EffectiveDate"]) : null,
                    VacatingDate = row["VacatingDate"] != DBNull.Value ? Convert.ToDateTime(row["VacatingDate"]) : null,
                    MoveOutDate = row["MoveOutDate"] != DBNull.Value ? Convert.ToDateTime(row["MoveOutDate"]) : null,
                    KeyReturnDate = row["KeyReturnDate"] != DBNull.Value ? Convert.ToDateTime(row["KeyReturnDate"]) : null,
                    StayPeriodDays = row["StayPeriodDays"] != DBNull.Value ? Convert.ToInt32(row["StayPeriodDays"]) : null,
                    StayPeriodAmount = row["StayPeriodAmount"] != DBNull.Value ? Convert.ToDecimal(row["StayPeriodAmount"]) : null,
                    TerminationReason = row["TerminationReason"]?.ToString() ?? "",
                    TerminationStatus = row["TerminationStatus"]?.ToString() ?? "",
                    TotalDeductions = row["TotalDeductions"] != DBNull.Value ? Convert.ToDecimal(row["TotalDeductions"]) : null,
                    SecurityDepositAmount = row["SecurityDepositAmount"] != DBNull.Value ? Convert.ToDecimal(row["SecurityDepositAmount"]) : null,
                    AdjustAmount = row["AdjustAmount"] != DBNull.Value ? Convert.ToDecimal(row["AdjustAmount"]) : null,
                    TotalInvoiced = row["TotalInvoiced"] != DBNull.Value ? Convert.ToDecimal(row["TotalInvoiced"]) : null,
                    TotalReceived = row["TotalReceived"] != DBNull.Value ? Convert.ToDecimal(row["TotalReceived"]) : null,
                    CreditNoteAmount = row["CreditNoteAmount"] != DBNull.Value ? Convert.ToDecimal(row["CreditNoteAmount"]) : null,
                    RefundAmount = row["RefundAmount"] != DBNull.Value ? Convert.ToDecimal(row["RefundAmount"]) : null,
                    IsRefundProcessed = Convert.ToBoolean(row["IsRefundProcessed"]),
                    RefundDate = row["RefundDate"] != DBNull.Value ? Convert.ToDateTime(row["RefundDate"]) : null,
                    RefundReference = row["RefundReference"]?.ToString() ?? "",
                    Notes = row["Notes"]?.ToString() ?? "",
                    CustomerFullName = row["CustomerFullName"]?.ToString() ?? "",
                    CustomerID = Convert.ToInt64(row["CustomerID"]),
                    PropertyID = row["PropertyID"] != DBNull.Value ? Convert.ToInt64(row["PropertyID"]) : null,
                    PropertyName = row["PropertyName"]?.ToString() ?? "",
                    UnitNumbers = row["UnitNumbers"]?.ToString() ?? "",
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString() ?? "",
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                };
            }

            // Parse deductions data (Table 1)
            if (dataSet.Tables.Count > 1)
            {
                ParseDeductionsData(terminationSlipData, dataSet.Tables[1]);
            }

            // Parse attachments data (Table 2)
            if (dataSet.Tables.Count > 2)
            {
                ParseAttachmentsData(terminationSlipData, dataSet.Tables[2]);
            }

            return terminationSlipData;
        }

        private void ParseDeductionsData(TerminationSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Deductions.Add(new TerminationDeductionInfo
                {
                    TerminationDeductionID = Convert.ToInt64(row["TerminationDeductionID"]),
                    TerminationID = Convert.ToInt64(row["TerminationID"]),
                    DeductionID = row["DeductionID"] != DBNull.Value ? Convert.ToInt64(row["DeductionID"]) : null,
                    DeductionName = row["DeductionName"]?.ToString() ?? "",
                    DeductionDescription = row["DeductionDescription"]?.ToString() ?? "",
                    DeductionAmount = Convert.ToDecimal(row["DeductionAmount"]),
                    TaxPercentage = row["TaxPercentage"] != DBNull.Value ? Convert.ToDecimal(row["TaxPercentage"]) : null,
                    TaxAmount = row["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(row["TaxAmount"]) : null,
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    DeductionCode = row["DeductionCode"]?.ToString() ?? "",
                    DeductionType = row["DeductionType"]?.ToString() ?? "",
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString() ?? "",
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                });
            }
        }

        private void ParseAttachmentsData(TerminationSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Attachments.Add(new TerminationAttachmentInfo
                {
                    TerminationAttachmentID = Convert.ToInt64(row["TerminationAttachmentID"]),
                    TerminationID = Convert.ToInt64(row["TerminationID"]),
                    DocTypeID = Convert.ToInt64(row["DocTypeID"]),
                    DocumentName = row["DocumentName"]?.ToString() ?? "",
                    FilePath = row["FilePath"]?.ToString() ?? "",
                    FileContentType = row["FileContentType"]?.ToString() ?? "",
                    FileSize = row["FileSize"] != DBNull.Value ? Convert.ToInt64(row["FileSize"]) : null,
                    DocIssueDate = row["DocIssueDate"] != DBNull.Value ? Convert.ToDateTime(row["DocIssueDate"]) : null,
                    DocExpiryDate = row["DocExpiryDate"] != DBNull.Value ? Convert.ToDateTime(row["DocExpiryDate"]) : null,
                    Remarks = row["Remarks"]?.ToString() ?? "",
                    DocTypeName = row["DocTypeName"]?.ToString() ?? "",
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString() ?? "",
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                });
            }
        }
    }
}