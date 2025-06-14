// LeaseERP.Core/Services/Reports/Contracts/ContractListTemplate.cs
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
    public class ContractListTemplate : BaseReportTemplate
    {
        public override string ReportType => "contract-list";

        public ContractListTemplate(
            ILogger<ContractListTemplate> logger,
            IConfiguration configuration,
            IEnumerable<IReportComponent> components)
            : base(logger, configuration, components)
        {
        }

        public override bool CanHandle(string reportType)
        {
            return string.Equals(reportType, "contract-list", StringComparison.OrdinalIgnoreCase);
        }

        public override ReportConfiguration GetDefaultConfiguration()
        {
            return new ReportConfiguration
            {
                Title = "CONTRACT LIST REPORT",
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
            var contractListData = ParseContractListData(data.DataSet);
            if (contractListData?.Contracts == null || !contractListData.Contracts.Any())
            {
                container.AlignCenter().Padding(20).Text("No contracts found matching the specified criteria").FontSize(12);
                return;
            }

            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                // Filters Section
                if (config.Header.ShowFilters)
                {
                    RenderFiltersSection(column, contractListData, config);
                    column.Item().PaddingVertical(10);
                }

                // Contracts Table
                RenderContractsTable(column, contractListData, config);

                // Summary Section
                column.Item().PaddingVertical(15);
                RenderSummarySection(column, contractListData, config);
            });
        }

        private void RenderFiltersSection(ColumnDescriptor column, ContractListData contractListData, ReportConfiguration config)
        {
            if (contractListData.AppliedFilters == null) return;

            var filters = contractListData.AppliedFilters;
            var hasFilters = !string.IsNullOrEmpty(filters.SearchText) ||
                           !string.IsNullOrEmpty(filters.CustomerName) ||
                           !string.IsNullOrEmpty(filters.ContractStatus) ||
                           filters.FromDate.HasValue ||
                           filters.ToDate.HasValue ||
                           !string.IsNullOrEmpty(filters.UnitNo) ||
                           !string.IsNullOrEmpty(filters.PropertyName);

            if (!hasFilters) return;

            column.Item().Background(Colors.Grey.Lighten4).Padding(8).Column(filterColumn =>
            {
                filterColumn.Item().Text("Applied Filters:").FontSize(10).SemiBold();

                var filterItems = new List<string>();

                if (!string.IsNullOrEmpty(filters.SearchText))
                    filterItems.Add($"Search: {filters.SearchText}");

                if (!string.IsNullOrEmpty(filters.CustomerName))
                    filterItems.Add($"Customer: {filters.CustomerName}");

                if (!string.IsNullOrEmpty(filters.ContractStatus))
                    filterItems.Add($"Status: {filters.ContractStatus}");

                if (filters.FromDate.HasValue)
                    filterItems.Add($"From: {filters.FromDate.Value:dd/MM/yyyy}");

                if (filters.ToDate.HasValue)
                    filterItems.Add($"To: {filters.ToDate.Value:dd/MM/yyyy}");

                if (!string.IsNullOrEmpty(filters.UnitNo))
                    filterItems.Add($"Unit: {filters.UnitNo}");

                if (!string.IsNullOrEmpty(filters.PropertyName))
                    filterItems.Add($"Property: {filters.PropertyName}");

                filterColumn.Item().Text(string.Join(" | ", filterItems)).FontSize(9);
            });
        }

        private void RenderContractsTable(ColumnDescriptor column, ContractListData contractListData, ReportConfiguration config)
        {
            var tableConfig = new TableConfiguration<ContractSummaryInfo>
            {
                Columns = new List<TableColumn<ContractSummaryInfo>>
                {
                    new() { Header = "Contract No", PropertyName = "ContractNo", Width = 3, RenderCell = (c, item, cfg) => c.Text(item.ContractNo).FontSize(8) },
                    new() { Header = "Customer", PropertyName = "CustomerName", Width = 4, RenderCell = (c, item, cfg) => c.Text(item.CustomerName).FontSize(8) },
                    new() { Header = "Status", PropertyName = "ContractStatus", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.ContractStatus).FontSize(8) },
                    new() { Header = "Date", PropertyName = "TransactionDate", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.TransactionDate.ToString("dd/MM/yyyy")).FontSize(8) },
                    new() { Header = "Units", PropertyName = "UnitCount", Width = 1, Alignment = TextAlignment.Center, RenderCell = (c, item, cfg) => c.AlignCenter().Text(item.UnitCount.ToString()).FontSize(8) },
                    new() { Header = "Total Amt", PropertyName = "TotalAmount", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.TotalAmount.ToString("N2")).FontSize(8) },
                    new() { Header = "Add'l Charges", PropertyName = "AdditionalCharges", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.AdditionalCharges.ToString("N2")).FontSize(8) },
                    new() { Header = "Grand Total", PropertyName = "GrandTotal", Width = 2, Alignment = TextAlignment.Right, RenderCell = (c, item, cfg) => c.AlignRight().Text(item.GrandTotal.ToString("N2")).FontSize(8) },
                    new() { Header = "Created By", PropertyName = "CreatedBy", Width = 2, RenderCell = (c, item, cfg) => c.Text(item.CreatedBy).FontSize(8) }
                },
                ShowTotals = true,
                TotalCalculators = new Dictionary<string, Func<IEnumerable<ContractSummaryInfo>, string>>
                {
                    { "TotalAmount", contracts => contracts.Sum(c => c.TotalAmount).ToString("N2") },
                    { "AdditionalCharges", contracts => contracts.Sum(c => c.AdditionalCharges).ToString("N2") },
                    { "GrandTotal", contracts => contracts.Sum(c => c.GrandTotal).ToString("N2") }
                }
            };

            RenderTable(column.Item(), contractListData.Contracts, tableConfig);
        }

        private void RenderSummarySection(ColumnDescriptor column, ContractListData contractListData, ReportConfiguration config)
        {
            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
            {
                // Summary statistics
                row.RelativeItem().Column(summaryCol =>
                {
                    summaryCol.Item().Text("SUMMARY").FontSize(12).SemiBold();
                    summaryCol.Item().PaddingTop(5).Text($"Total Contracts: {contractListData.Summary.TotalContracts:N0}").FontSize(9);
                    summaryCol.Item().Text($"Total Units: {contractListData.Summary.TotalUnits:N0}").FontSize(9);
                    summaryCol.Item().Text($"Total Additional Charges: {contractListData.Summary.TotalCharges:N0}").FontSize(9);
                    summaryCol.Item().Text($"Total Attachments: {contractListData.Summary.TotalAttachments:N0}").FontSize(9);
                });

                // Financial summary
                row.RelativeItem().Column(financialCol =>
                {
                    financialCol.Item().Text("FINANCIAL SUMMARY").FontSize(12).SemiBold();
                    financialCol.Item().PaddingTop(5).Text($"Total Contract Value: {contractListData.Summary.TotalContractValue:N2}").FontSize(9);
                    financialCol.Item().Text($"Total Additional Charges: {contractListData.Summary.TotalAdditionalCharges:N2}").FontSize(9);
                    financialCol.Item().Text($"Grand Total Value: {contractListData.Summary.GrandTotalValue:N2}").FontSize(9).SemiBold();
                });

                // Status breakdown
                if (contractListData.Summary.StatusBreakdown.Any())
                {
                    row.RelativeItem().Column(statusCol =>
                    {
                        statusCol.Item().Text("STATUS BREAKDOWN").FontSize(12).SemiBold();
                        statusCol.Item().PaddingTop(5);

                        foreach (var status in contractListData.Summary.StatusBreakdown)
                        {
                            statusCol.Item().Text($"{status.Key}: {status.Value:N0}").FontSize(9);
                        }
                    });
                }
            });
        }

        private ContractListData ParseContractListData(DataSet dataSet)
        {
            var contractListData = new ContractListData();

            if (dataSet?.Tables?.Count == 0) return contractListData;

            // Parse contracts from the first table
            if (dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                foreach (DataRow row in dataSet.Tables[0].Rows)
                {
                    contractListData.Contracts.Add(new ContractSummaryInfo
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
                        UnitCount = row.Table.Columns.Contains("UnitCount") ? Convert.ToInt32(row["UnitCount"]) : 0,
                        ChargeCount = row.Table.Columns.Contains("ChargeCount") ? Convert.ToInt32(row["ChargeCount"]) : 0,
                        AttachmentCount = row.Table.Columns.Contains("AttachmentCount") ? Convert.ToInt32(row["AttachmentCount"]) : 0,
                        CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                        CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                        UpdatedBy = row["UpdatedBy"]?.ToString() ?? "",
                        UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                    });
                }

                // Calculate summary
                contractListData.Summary = new ContractListSummary
                {
                    TotalContracts = contractListData.Contracts.Count,
                    TotalContractValue = contractListData.Contracts.Sum(c => c.TotalAmount),
                    TotalAdditionalCharges = contractListData.Contracts.Sum(c => c.AdditionalCharges),
                    GrandTotalValue = contractListData.Contracts.Sum(c => c.GrandTotal),
                    TotalUnits = contractListData.Contracts.Sum(c => c.UnitCount),
                    TotalCharges = contractListData.Contracts.Sum(c => c.ChargeCount),
                    TotalAttachments = contractListData.Contracts.Sum(c => c.AttachmentCount),
                    StatusBreakdown = contractListData.Contracts
                        .GroupBy(c => c.ContractStatus)
                        .ToDictionary(g => g.Key, g => g.Count())
                };
            }

            return contractListData;
        }
    }
}