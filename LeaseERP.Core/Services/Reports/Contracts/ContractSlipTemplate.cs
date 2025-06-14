// LeaseERP.Core/Services/Reports/Contracts/ContractSlipTemplate.cs
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services.Reports.Contracts
{
    public class ContractSlipTemplate : BaseReportTemplate
    {
        public override string ReportType => "contract-slip";

        public ContractSlipTemplate(
            ILogger<ContractSlipTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "contract-slip", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "CONTRACT SLIP",
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
            var contractData = ParseContractData(data.DataSet);
            if (contractData?.Contract == null)
            {
                container.AlignCenter().Padding(20).Text("Contract data not found").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Contract Header Information
                RenderContractHeader(column, contractData, config);

                column.Item().PaddingVertical(10);

                // Units Section
                if (contractData.Units.Any())
                {
                    RenderUnitsSection(column, contractData, config);
                    column.Item().PaddingVertical(10);
                }

                // Additional Charges Section
                if (contractData.AdditionalCharges.Any())
                {
                    RenderChargesSection(column, contractData, config);
                    column.Item().PaddingVertical(10);
                }

                // Summary Section
                RenderSummarySection(column, contractData, config);

                // Remarks Section
                if (!string.IsNullOrEmpty(contractData.Contract.Remarks))
                {
                    RenderRemarksSection(column, contractData, config);
                }

                // Attachments Section
                if (contractData.Attachments.Any())
                {
                    RenderAttachmentsSection(column, contractData, config);
                }
            });
        }

        private void RenderContractHeader(ColumnDescriptor column, ContractSlipData contractData, ReportConfiguration config)
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Contract No: {contractData.Contract.ContractNo}").SemiBold();
                    col.Item().Text($"Status: {contractData.Contract.ContractStatus}");
                    col.Item().Text($"Transaction Date: {contractData.Contract.TransactionDate:dd/MM/yyyy}");
                });

                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Customer: {contractData.Contract.CustomerName}").SemiBold();
                    if (!string.IsNullOrEmpty(contractData.Contract.JointCustomerName))
                        col.Item().Text($"Joint Customer: {contractData.Contract.JointCustomerName}");
                    col.Item().Text($"Created By: {contractData.Contract.CreatedBy}");
                    col.Item().Text($"Created On: {contractData.Contract.CreatedOn:dd/MM/yyyy}");
                });
            });
        }

        private void RenderUnitsSection(ColumnDescriptor column, ContractSlipData contractData, ReportConfiguration config)
        {
            column.Item().Text("LEASED UNITS").FontSize(14).SemiBold();
            column.Item().PaddingVertical(5);

            var tableConfig = new TableConfiguration<ContractUnitInfo>
            {
                Columns = new List<TableColumn<ContractUnitInfo>>
                {
                    new() { Header = "Unit No", PropertyName = "UnitNo", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.UnitNo) },
                    new() { Header = "Property", PropertyName = "PropertyName", Width = 3, RenderCell = (c, item, cfg) => c.Text(item.PropertyName) },
                    new() { Header = "Type", PropertyName = "UnitTypeName", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.UnitTypeName) },
                    new() { Header = "From Date", PropertyName = "FromDate", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.FromDate.ToString("dd/MM/yyyy")) },
                    new() { Header = "To Date", PropertyName = "ToDate", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.ToDate.ToString("dd/MM/yyyy")) },
                    new() { Header = "Rent/Month", PropertyName = "RentPerMonth", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.RentPerMonth.ToString("N2")) },
                    new() { Header = "Total", PropertyName = "TotalAmount", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TotalAmount.ToString("N2")) }
                },
                ShowTotals = true,
                TotalCalculators = new Dictionary<string, Func<IEnumerable<ContractUnitInfo>, string>>
                {
                    { "TotalAmount", units => units.Sum(u => u.TotalAmount).ToString("N2") }
                }
            };

            RenderTable(column.Item(), contractData.Units, tableConfig);
        }

        private void RenderChargesSection(ColumnDescriptor column, ContractSlipData contractData, ReportConfiguration config)
        {
            column.Item().Text("ADDITIONAL CHARGES").FontSize(14).SemiBold();
            column.Item().PaddingVertical(5);

            var tableConfig = new TableConfiguration<ContractChargeInfo>
            {
                Columns = new List<TableColumn<ContractChargeInfo>>
                {
                    new() { Header = "Charge", PropertyName = "ChargesName", Width = 3, RenderCell = (c, item, cfg) => c.Text(item.ChargesName) },
                    new() { Header = "Category", PropertyName = "ChargesCategoryName", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.ChargesCategoryName) },
                    new() { Header = "Amount", PropertyName = "Amount", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.Amount.ToString("N2")) },
                    new() { Header = "Tax %", PropertyName = "TaxPercentage", Width = 1, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TaxPercentage?.ToString("N1") ?? "0.0") },
                    new() { Header = "Tax Amt", PropertyName = "TaxAmount", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TaxAmount?.ToString("N2") ?? "0.00") },
                    new() { Header = "Total", PropertyName = "TotalAmount", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TotalAmount.ToString("N2")) }
                },
                ShowTotals = true,
                TotalCalculators = new Dictionary<string, Func<IEnumerable<ContractChargeInfo>, string>>
                {
                    { "TotalAmount", charges => charges.Sum(c => c.TotalAmount).ToString("N2") }
                }
            };

            RenderTable(column.Item(), contractData.AdditionalCharges, tableConfig);
        }

        private void RenderSummarySection(ColumnDescriptor column, ContractSlipData contractData, ReportConfiguration config)
        {
            column.Item().AlignRight().Column(summaryColumn =>
            {
                summaryColumn.Item().BorderTop(1).PaddingTop(10).Row(row =>
                {
                    row.RelativeItem(3);
                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Total Amount:");
                            r.ConstantItem(80).AlignRight().Text(contractData.Contract.TotalAmount.ToString("N2"));
                        });
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text("Additional Charges:");
                            r.ConstantItem(80).AlignRight().Text(contractData.Contract.AdditionalCharges.ToString("N2"));
                        });
                        col.Item().BorderTop(1).PaddingTop(5).Row(r =>
                        {
                            r.RelativeItem().Text("Grand Total:").SemiBold();
                            r.ConstantItem(80).AlignRight().Text(contractData.Contract.GrandTotal.ToString("N2")).SemiBold();
                        });
                    });
                });
            });
        }

        private void RenderRemarksSection(ColumnDescriptor column, ContractSlipData contractData, ReportConfiguration config)
        {
            column.Item().PaddingTop(20).Column(remarksColumn =>
            {
                remarksColumn.Item().Text("REMARKS").FontSize(12).SemiBold();
                remarksColumn.Item().PaddingTop(5).Text(contractData.Contract.Remarks);
            });
        }

        private void RenderAttachmentsSection(ColumnDescriptor column, ContractSlipData contractData, ReportConfiguration config)
        {
            column.Item().PaddingTop(20).Column(attachColumn =>
            {
                attachColumn.Item().Text("ATTACHMENTS").FontSize(12).SemiBold();
                attachColumn.Item().PaddingTop(5);

                foreach (var attachment in contractData.Attachments)
                {
                    attachColumn.Item().Text($"• {attachment.DocumentName} ({attachment.DocTypeName})").FontSize(9);
                }
            });
        }

        private ContractSlipData ParseContractData(DataSet dataSet)
        {
            if (dataSet?.Tables?.Count < 4) return null;

            var contractSlipData = new ContractSlipData();

            // Parse contract master data (Table 0)
            if (dataSet.Tables[0].Rows.Count > 0)
            {
                var row = dataSet.Tables[0].Rows[0];
                contractSlipData.Contract = new ContractMasterInfo
                {
                    ContractID = Convert.ToInt64(row["ContractID"]),
                    ContractNo = row["ContractNo"]?.ToString() ?? "",
                    ContractStatus = row["ContractStatus"]?.ToString() ?? "",
                    CustomerID = Convert.ToInt64(row["CustomerID"]),
                    CustomerName = row["CustomerName"]?.ToString() ?? "",
                    JointCustomerID = row["JointCustomerID"] != DBNull.Value ? Convert.ToInt64(row["JointCustomerID"]) : null,
                    JointCustomerName = row["JointCustomerName"]?.ToString() ?? "",
                    TransactionDate = Convert.ToDateTime(row["TransactionDate"]),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"]),
                    AdditionalCharges = Convert.ToDecimal(row["AdditionalCharges"]),
                    GrandTotal = Convert.ToDecimal(row["GrandTotal"]),
                    Remarks = row["Remarks"]?.ToString() ?? "",
                    CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    UpdatedBy = row["UpdatedBy"]?.ToString() ?? "",
                    UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                };
            }

            // Parse units, charges, and attachments from other tables
            ParseUnitsData(contractSlipData, dataSet.Tables[1]);
            ParseChargesData(contractSlipData, dataSet.Tables[2]);
            ParseAttachmentsData(contractSlipData, dataSet.Tables[3]);

            return contractSlipData;
        }

        private void ParseUnitsData(ContractSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Units.Add(new ContractUnitInfo
                {
                    ContractUnitID = Convert.ToInt64(row["ContractUnitID"]),
                    UnitID = Convert.ToInt64(row["UnitID"]),
                    UnitNo = row["UnitNo"]?.ToString() ?? "",
                    PropertyName = row["PropertyName"]?.ToString() ?? "",
                    UnitTypeName = row["UnitTypeName"]?.ToString() ?? "",
                    UnitCategoryName = row["UnitCategoryName"]?.ToString() ?? "",
                    FloorName = row["FloorName"]?.ToString() ?? "",
                    FromDate = Convert.ToDateTime(row["FromDate"]),
                    ToDate = Convert.ToDateTime(row["ToDate"]),
                    RentPerMonth = Convert.ToDecimal(row["RentPerMonth"]),
                    RentPerYear = Convert.ToDecimal(row["RentPerYear"]),
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                });
            }
        }

        private void ParseChargesData(ContractSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.AdditionalCharges.Add(new ContractChargeInfo
                {
                    ContractAdditionalChargeID = Convert.ToInt64(row["ContractAdditionalChargeID"]),
                    AdditionalChargesID = Convert.ToInt64(row["AdditionalChargesID"]),
                    ChargesName = row["ChargesName"]?.ToString() ?? "",
                    ChargesCategoryName = row["ChargesCategoryName"]?.ToString() ?? "",
                    Amount = Convert.ToDecimal(row["Amount"]),
                    TaxPercentage = row["TaxPercentage"] != DBNull.Value ? Convert.ToDecimal(row["TaxPercentage"]) : null,
                    TaxAmount = row["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(row["TaxAmount"]) : null,
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                });
            }
        }

        private void ParseAttachmentsData(ContractSlipData data, DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                data.Attachments.Add(new ContractAttachmentInfo
                {
                    ContractAttachmentID = Convert.ToInt64(row["ContractAttachmentID"]),
                    DocumentName = row["DocumentName"]?.ToString() ?? "",
                    DocTypeName = row["DocTypeName"]?.ToString() ?? "",
                    FilePath = row["FilePath"]?.ToString() ?? "",
                    FileSize = row["FileSize"] != DBNull.Value ? Convert.ToInt64(row["FileSize"]) : null,
                    DocIssueDate = row["DocIssueDate"] != DBNull.Value ? Convert.ToDateTime(row["DocIssueDate"]) : null,
                    DocExpiryDate = row["DocExpiryDate"] != DBNull.Value ? Convert.ToDateTime(row["DocExpiryDate"]) : null,
                    Remarks = row["Remarks"]?.ToString() ?? ""
                });
            }
        }
    }
}
