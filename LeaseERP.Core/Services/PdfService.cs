using LeaseERP.Core.Interfaces;
using LeaseERP.Shared.DTOs;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Core.Services
{
    public class PdfService : IPdfService
    {
        private readonly IDataService _dataService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PdfService> _logger;

        public PdfService(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<PdfService> logger)
        {
            _dataService = dataService;
            _configuration = configuration;
            _logger = logger;

            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public async Task<byte[]> GenerateContractSlipAsync(long contractId, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating contract slip for ContractID: {ContractId}", contractId);

                // Fetch contract data
                var contractData = await GetContractDataAsync(contractId, actionBy);

                if (contractData?.Contract == null)
                {
                    throw new Exception("Contract data not found");
                }

                // Generate PDF
                var document = CreateContractSlipDocument(contractData);
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract slip for ContractID: {ContractId}", contractId);
                throw;
            }
        }

        public async Task<byte[]> GenerateContractListAsync(ContractListRequest request, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating contract list PDF by user: {ActionBy}", actionBy);

                // Fetch contract list data
                var contractListData = await GetContractListDataAsync(request, actionBy);

                if (contractListData?.Contracts == null && contractListData?.Contracts.Count == 0)
                {
                    throw new Exception("Contract list data not found");
                }

                // Generate PDF
                var document = CreateContractListDocument(contractListData);
                return document.GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract list PDF");
                throw;
            }
        }

        public async Task<byte[]> GenerateInvoiceAsync(long invoiceId, string actionBy)
        {
            // Implementation for invoice PDF generation
            throw new NotImplementedException("Invoice PDF generation will be implemented next");
        }

        public async Task<byte[]> GenerateReceiptAsync(long receiptId, string actionBy)
        {
            // Implementation for receipt PDF generation
            throw new NotImplementedException("Receipt PDF generation will be implemented next");
        }

        public async Task<byte[]> GenerateCustomReportAsync(string reportType, Dictionary<string, object> parameters)
        {
            // Implementation for custom reports
            throw new NotImplementedException("Custom report generation will be implemented based on requirements");
        }

        private async Task<ContractListData> GetContractListDataAsync(ContractListRequest request, string actionBy)
        {
            var spName = _configuration["StoredProcedures:contractmanagement"];
            if (string.IsNullOrEmpty(spName))
            {
                throw new Exception("Contract management stored procedure not configured");
            }

            // Use search mode if any filters are applied, otherwise fetch all
            var mode = HasFilters(request) ? (int)OperationType.Search : (int)OperationType.FetchAll;

            var parameters = new Dictionary<string, object>
            {
                { "@Mode", mode },
                { "@CurrentUserName", actionBy }
            };

            // Add filter parameters if they exist
            if (!string.IsNullOrEmpty(request.SearchText))
                parameters.Add("@SearchText", request.SearchText);

            if (request.FilterCustomerID.HasValue)
                parameters.Add("@FilterCustomerID", request.FilterCustomerID.Value);

            if (!string.IsNullOrEmpty(request.FilterContractStatus))
                parameters.Add("@FilterContractStatus", request.FilterContractStatus);

            if (request.FilterFromDate.HasValue)
                parameters.Add("@FilterFromDate", request.FilterFromDate.Value);

            if (request.FilterToDate.HasValue)
                parameters.Add("@FilterToDate", request.FilterToDate.Value);

            if (request.FilterUnitID.HasValue)
                parameters.Add("@FilterUnitID", request.FilterUnitID.Value);

            if (request.FilterPropertyID.HasValue)
                parameters.Add("@FilterPropertyID", request.FilterPropertyID.Value);

            var result = await _dataService.ExecuteStoredProcedureAsync(spName, parameters);

            if (result?.Tables?.Count < 1)
            {
                throw new Exception("Invalid data structure returned from stored procedure");
            }

            var contractListData = new ContractListData
            {
                ReportTitle = request.ReportTitle,
                GeneratedBy = actionBy,
                GeneratedOn = DateTime.Now
            };

            // Parse contract list data (Table 0)
            foreach (DataRow row in result.Tables[0].Rows)
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
                    UnitCount = row["UnitCount"] != DBNull.Value ? Convert.ToInt32(row["UnitCount"]) : 0,
                    //ChargeCount = row["ChargeCount"] != DBNull.Value ? Convert.ToInt32(row["ChargeCount"]) : 0,
                    //AttachmentCount = row["AttachmentCount"] != DBNull.Value ? Convert.ToInt32(row["AttachmentCount"]) : 0,
                    //CreatedBy = row["CreatedBy"]?.ToString() ?? "",
                    //CreatedOn = Convert.ToDateTime(row["CreatedOn"]),
                    //UpdatedBy = row["UpdatedBy"]?.ToString() ?? "",
                    //UpdatedOn = row["UpdatedOn"] != DBNull.Value ? Convert.ToDateTime(row["UpdatedOn"]) : null
                });
            }

            // Calculate summary
            contractListData.Summary = CalculateSummary(contractListData.Contracts);

            // Set applied filters
            contractListData.AppliedFilters = new ContractListFilters
            {
                SearchText = request.SearchText,
                ContractStatus = request.FilterContractStatus,
                FromDate = request.FilterFromDate,
                ToDate = request.FilterToDate
            };

            // Get company information
            contractListData.Company = await GetCompanyInfoAsync();

            return contractListData;
        }

        private bool HasFilters(ContractListRequest request)
        {
            return !string.IsNullOrEmpty(request.SearchText) ||
                   request.FilterCustomerID.HasValue ||
                   !string.IsNullOrEmpty(request.FilterContractStatus) ||
                   request.FilterFromDate.HasValue ||
                   request.FilterToDate.HasValue ||
                   request.FilterUnitID.HasValue ||
                   request.FilterPropertyID.HasValue;
        }

        private ContractListSummary CalculateSummary(List<ContractSummaryInfo> contracts)
        {
            var summary = new ContractListSummary
            {
                TotalContracts = contracts.Count,
                TotalContractValue = contracts.Sum(c => c.TotalAmount),
                TotalAdditionalCharges = contracts.Sum(c => c.AdditionalCharges),
                GrandTotalValue = contracts.Sum(c => c.GrandTotal),
                TotalUnits = contracts.Sum(c => c.UnitCount),
                TotalCharges = contracts.Sum(c => c.ChargeCount),
                TotalAttachments = contracts.Sum(c => c.AttachmentCount)
            };

            // Calculate status breakdown
            summary.StatusBreakdown = contracts
                .GroupBy(c => c.ContractStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            return summary;
        }

        private IDocument CreateContractListDocument(ContractListData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); // Use landscape for better table layout
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    page.Header()
                        .Height(120) // Reduced from 140 to 120
                        .Background(Colors.Grey.Lighten3)
                        .Padding(10) // Reduced from 15 to 10
                        .Column(column =>
                        {
                            // Company Header Row
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text(data.Company.CompanyName)
                                        .FontSize(16) // Reduced from 18
                                        .SemiBold()
                                        .FontColor(Colors.Blue.Darken2);

                                    col.Item().Text(data.Company.CompanyAddress).FontSize(7); // Reduced from 8
                                    col.Item().Text($"Phone: {data.Company.CompanyPhone} | Email: {data.Company.CompanyEmail}").FontSize(7); // Reduced from 8
                                });

                                // Company Logo
                                row.ConstantItem(70).AlignCenter().AlignMiddle().Container().Row(logoRow => // Reduced from 80 to 70
                                {
                                    if (LogoFileExists())
                                    {
                                        try
                                        {
                                            var logoPath = GetAbsoluteLogoPath();
                                            logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                                .MaxHeight(50) // Reduced from 60
                                                .MaxWidth(65) // Reduced from 75
                                                .Image(logoPath)
                                                .FitArea();
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogWarning(ex, "Failed to load company logo");
                                            logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                                .Border(1).BorderColor(Colors.Grey.Medium)
                                                .Background(Colors.Grey.Lighten4)
                                                .Padding(4).Text("LOGO").FontSize(7); // Reduced padding and font
                                        }
                                    }
                                    else
                                    {
                                        logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                            .Border(1).BorderColor(Colors.Grey.Medium)
                                            .Background(Colors.Grey.Lighten4)
                                            .Padding(4).Text("LOGO").FontSize(7); // Reduced padding and font
                                    }
                                });
                            });

                            column.Item().PaddingVertical(3); // Reduced from 5

                            // Report Title
                            column.Item().AlignCenter().Text(data.ReportTitle)
                                .FontSize(14) // Reduced from 16
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            // Generation Info
                            column.Item().PaddingTop(3).Row(row => // Reduced from 5
                            {
                                row.RelativeItem().Text($"Generated by: {data.GeneratedBy}").FontSize(7); // Reduced from 8
                                row.RelativeItem().AlignRight().Text($"Generated on: {data.GeneratedOn:dd/MM/yyyy HH:mm}").FontSize(7); // Reduced from 8
                            });
                        });

                    page.Content()
                        .PaddingVertical(5) // Reduced from 10
                        .Column(column =>
                        {
                            // Applied Filters - Moved to content area to avoid header space constraints
                            if (HasAppliedFilters(data.AppliedFilters))
                            {
                                column.Item().Background(Colors.Blue.Lighten4).Padding(8).Column(filterCol =>
                                {
                                    filterCol.Item().Text("Applied Filters:").FontSize(9).SemiBold();

                                    var filterTexts = new List<string>();

                                    if (!string.IsNullOrEmpty(data.AppliedFilters.SearchText))
                                        filterTexts.Add($"Search: {data.AppliedFilters.SearchText}");

                                    if (!string.IsNullOrEmpty(data.AppliedFilters.ContractStatus))
                                        filterTexts.Add($"Status: {data.AppliedFilters.ContractStatus}");

                                    if (data.AppliedFilters.FromDate.HasValue)
                                        filterTexts.Add($"From: {data.AppliedFilters.FromDate:dd/MM/yyyy}");

                                    if (data.AppliedFilters.ToDate.HasValue)
                                        filterTexts.Add($"To: {data.AppliedFilters.ToDate:dd/MM/yyyy}");

                                    if (filterTexts.Any())
                                    {
                                        filterCol.Item().Text(string.Join(" | ", filterTexts)).FontSize(8);
                                    }
                                });

                                column.Item().PaddingVertical(5); // Add spacing after filters
                            }

                            // Summary Section
                            column.Item().Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("SUMMARY").FontSize(11).SemiBold();
                                    col.Item().Row(r =>
                                    {
                                        r.RelativeItem().Text($"Total Contracts: {data.Summary.TotalContracts}");
                                        r.RelativeItem().Text($"Total Units: {data.Summary.TotalUnits}");
                                        r.RelativeItem().Text($"Total Value: {data.Summary.GrandTotalValue:N2}");
                                    });
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text("STATUS BREAKDOWN").FontSize(11).SemiBold();
                                    foreach (var status in data.Summary.StatusBreakdown)
                                    {
                                        col.Item().Text($"{status.Key}: {status.Value}").FontSize(9);
                                    }
                                });
                            });

                            column.Item().PaddingVertical(10);

                            // Contracts Table
                            if (data.Contracts.Any())
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(80);  // Contract No
                                        columns.RelativeColumn(2);  // Customer Name
                                        columns.ConstantColumn(60);  // Status
                                        columns.ConstantColumn(70);  // Date
                                        columns.ConstantColumn(70);  // Total Amount
                                        columns.ConstantColumn(70);  // Additional Charges
                                        columns.ConstantColumn(70);  // Grand Total
                                        columns.ConstantColumn(35);  // Units
                                        columns.ConstantColumn(35);  // Charges
                                        columns.RelativeColumn(1);  // Created By
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Contract No").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Customer Name").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Status").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Date").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Total Amt").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Add. Charges").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Grand Total").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Units").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Charges").FontColor(Colors.White).SemiBold().FontSize(8);
                                        header.Cell().Background(Colors.Blue.Darken2).Padding(3).Text("Created By").FontColor(Colors.White).SemiBold().FontSize(8);
                                    });

                                    // Data rows
                                    foreach (var contract in data.Contracts)
                                    {
                                        table.Cell().BorderBottom(0.5f).Padding(3).Text(contract.ContractNo).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).Text(contract.CustomerName).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).Text(contract.ContractStatus).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).Text(contract.TransactionDate.ToString("dd/MM/yyyy")).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).AlignRight().Text(contract.TotalAmount.ToString("N0")).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).AlignRight().Text(contract.AdditionalCharges.ToString("N0")).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).AlignRight().Text(contract.GrandTotal.ToString("N0")).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).AlignCenter().Text(contract.UnitCount.ToString()).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).AlignCenter().Text(contract.ChargeCount.ToString()).FontSize(8);
                                        table.Cell().BorderBottom(0.5f).Padding(3).Text(contract.CreatedBy).FontSize(8);
                                    }

                                    // Total row
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text("TOTALS").SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).Text($"{data.Summary.TotalContracts} Contracts").SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(data.Summary.TotalContractValue.ToString("N0")).SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(data.Summary.TotalAdditionalCharges.ToString("N0")).SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignRight().Text(data.Summary.GrandTotalValue.ToString("N0")).SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(data.Summary.TotalUnits.ToString()).SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3).AlignCenter().Text(data.Summary.TotalCharges.ToString()).SemiBold().FontSize(8);
                                    table.Cell().Background(Colors.Grey.Lighten2).Padding(3);
                                });
                            }
                            else
                            {
                                column.Item().AlignCenter().Padding(20).Text("No contracts found matching the specified criteria.")
                                    .FontSize(12).FontColor(Colors.Grey.Darken1);
                            }
                        });

                    page.Footer()
                        .Height(30)
                        .Background(Colors.Grey.Lighten4)
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                            x.Span($" | {data.ReportTitle} | Generated: {data.GeneratedOn:dd/MM/yyyy HH:mm}");
                        });
                });
            });
        }

        private bool HasAppliedFilters(ContractListFilters filters)
        {
            return !string.IsNullOrEmpty(filters.SearchText) ||
                   !string.IsNullOrEmpty(filters.ContractStatus) ||
                   filters.FromDate.HasValue ||
                   filters.ToDate.HasValue;
        }

        private async Task<ContractSlipData> GetContractDataAsync(long contractId, string actionBy)
        {
            var spName = _configuration["StoredProcedures:contractmanagement"];
            if (string.IsNullOrEmpty(spName))
            {
                throw new Exception("Contract management stored procedure not configured");
            }

            var parameters = new Dictionary<string, object>
            {
                { "@Mode", (int)OperationType.FetchById },
                { "@ContractID", contractId },
                { "@CurrentUserName", actionBy }
            };

            var result = await _dataService.ExecuteStoredProcedureAsync(spName, parameters);

            if (result?.Tables?.Count < 4)
            {
                throw new Exception("Invalid data structure returned from stored procedure");
            }

            var contractSlipData = new ContractSlipData();

            // Parse contract master data (Table 0)
            if (result.Tables[0].Rows.Count > 0)
            {
                var row = result.Tables[0].Rows[0];
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

            // Parse units data (Table 1)
            foreach (DataRow row in result.Tables[1].Rows)
            {
                contractSlipData.Units.Add(new ContractUnitInfo
                {
                    ContractUnitID = Convert.ToInt64(row["ContractUnitID"]),
                    UnitID = Convert.ToInt64(row["UnitID"]),
                    UnitNo = row["UnitNo"]?.ToString() ?? "",
                    PropertyName = row["PropertyName"]?.ToString() ?? "",
                    UnitTypeName = row["UnitTypeName"]?.ToString() ?? "",
                    UnitCategoryName = row["UnitCategoryName"]?.ToString() ?? "",
                    FloorName = row["FloorName"]?.ToString() ?? "",
                    BedRooms = row["BedRooms"] != DBNull.Value ? Convert.ToInt32(row["BedRooms"]) : null,
                    BathRooms = row["BathRooms"] != DBNull.Value ? Convert.ToInt32(row["BathRooms"]) : null,
                    FromDate = Convert.ToDateTime(row["FromDate"]),
                    ToDate = Convert.ToDateTime(row["ToDate"]),
                    FitoutFromDate = row["FitoutFromDate"] != DBNull.Value ? Convert.ToDateTime(row["FitoutFromDate"]) : null,
                    FitoutToDate = row["FitoutToDate"] != DBNull.Value ? Convert.ToDateTime(row["FitoutToDate"]) : null,
                    CommencementDate = row["CommencementDate"] != DBNull.Value ? Convert.ToDateTime(row["CommencementDate"]) : null,
                    ContractDays = row["ContractDays"] != DBNull.Value ? Convert.ToInt32(row["ContractDays"]) : null,
                    ContractMonths = row["ContractMonths"] != DBNull.Value ? Convert.ToInt32(row["ContractMonths"]) : null,
                    ContractYears = row["ContractYears"] != DBNull.Value ? Convert.ToInt32(row["ContractYears"]) : null,
                    RentPerMonth = Convert.ToDecimal(row["RentPerMonth"]),
                    RentPerYear = Convert.ToDecimal(row["RentPerYear"]),
                    NoOfInstallments = row["NoOfInstallments"] != DBNull.Value ? Convert.ToInt32(row["NoOfInstallments"]) : null,
                    RentFreePeriodFrom = row["RentFreePeriodFrom"] != DBNull.Value ? Convert.ToDateTime(row["RentFreePeriodFrom"]) : null,
                    RentFreePeriodTo = row["RentFreePeriodTo"] != DBNull.Value ? Convert.ToDateTime(row["RentFreePeriodTo"]) : null,
                    RentFreeAmount = row["RentFreeAmount"] != DBNull.Value ? Convert.ToDecimal(row["RentFreeAmount"]) : null,
                    TaxPercentage = row["TaxPercentage"] != DBNull.Value ? Convert.ToDecimal(row["TaxPercentage"]) : null,
                    TaxAmount = row["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(row["TaxAmount"]) : null,
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                });
            }

            // Parse additional charges data (Table 2)
            foreach (DataRow row in result.Tables[2].Rows)
            {
                contractSlipData.AdditionalCharges.Add(new ContractChargeInfo
                {
                    ContractAdditionalChargeID = Convert.ToInt64(row["ContractAdditionalChargeID"]),
                    AdditionalChargesID = Convert.ToInt64(row["AdditionalChargesID"]),
                    ChargesName = row["ChargesName"]?.ToString() ?? "",
                    ChargesCode = row["ChargesCode"]?.ToString() ?? "",
                    ChargesCategoryName = row["ChargesCategoryName"]?.ToString() ?? "",
                    Amount = Convert.ToDecimal(row["Amount"]),
                    TaxPercentage = row["TaxPercentage"] != DBNull.Value ? Convert.ToDecimal(row["TaxPercentage"]) : null,
                    TaxAmount = row["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(row["TaxAmount"]) : null,
                    TotalAmount = Convert.ToDecimal(row["TotalAmount"])
                });
            }

            // Parse attachments data (Table 3)
            foreach (DataRow row in result.Tables[3].Rows)
            {
                contractSlipData.Attachments.Add(new ContractAttachmentInfo
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

            // Get company information - this would typically come from a separate call or be included in the SP
            contractSlipData.Company = await GetCompanyInfoAsync();

            return contractSlipData;
        }

        private async Task<CompanyInfo> GetCompanyInfoAsync()
        {
            // This would typically fetch company data from the database
            // For now, return a default company info with logo path from configuration
            var logoPath = _configuration["PdfSettings:CompanyLogo"];

            return new CompanyInfo
            {
                CompanyID = 1,
                CompanyName = "LeaseERP Solutions",
                CompanyAddress = "123 Business Street, City, Country",
                CompanyPhone = "+1-234-567-8900",
                CompanyEmail = "info@leaseerp.com",
                CompanyWebsite = "www.leaseerp.com",
                TaxRegNo = "TAX123456789",
                CommercialRegNo = "CR123456789",
                CompanyLogo = logoPath ?? ""
            };
        }

        private string GetAbsoluteLogoPath()
        {
            var logoPath = _configuration["PdfSettings:CompanyLogo"];
            if (string.IsNullOrEmpty(logoPath))
                return string.Empty;

            // Handle tilde path resolution
            if (logoPath.StartsWith("~"))
            {
                // Get the content root path
                var contentRoot = Directory.GetCurrentDirectory();
                logoPath = logoPath.Replace("~", contentRoot);
            }

            // Normalize path separators for current OS
            logoPath = logoPath.Replace("\\", Path.DirectorySeparatorChar.ToString());
            logoPath = logoPath.Replace("/", Path.DirectorySeparatorChar.ToString());

            return logoPath;
        }

        private bool LogoFileExists()
        {
            var logoPath = GetAbsoluteLogoPath();
            if (string.IsNullOrEmpty(logoPath))
                return false;

            var exists = File.Exists(logoPath);
            if (!exists)
            {
                _logger.LogWarning("Company logo file not found at path: {LogoPath}", logoPath);
            }
            return exists;
        }

        private IDocument CreateContractSlipDocument(ContractSlipData data)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header()
                        .Height(120)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(15)
                        .Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text(data.Company.CompanyName)
                                    .FontSize(20)
                                    .SemiBold()
                                    .FontColor(Colors.Blue.Darken2);

                                column.Item().Text(data.Company.CompanyAddress).FontSize(9);
                                column.Item().Text($"Phone: {data.Company.CompanyPhone} | Email: {data.Company.CompanyEmail}").FontSize(9);
                                if (!string.IsNullOrEmpty(data.Company.TaxRegNo))
                                    column.Item().Text($"Tax Reg No: {data.Company.TaxRegNo}").FontSize(9);
                            });

                            // Company Logo Section
                            row.ConstantItem(90).AlignCenter().AlignMiddle().Container().Row(logoRow =>
                            {
                                if (LogoFileExists())
                                {
                                    try
                                    {
                                        var logoPath = GetAbsoluteLogoPath();
                                        logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                            .MaxHeight(70)
                                            .MaxWidth(85)
                                            .Image(logoPath)
                                            .FitArea();
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Failed to load company logo from path: {LogoPath}", GetAbsoluteLogoPath());
                                        // Fallback to text placeholder
                                        logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                            .Border(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .Background(Colors.Grey.Lighten4)
                                            .Padding(8)
                                            .Text("LOGO")
                                            .FontSize(10)
                                            .FontColor(Colors.Grey.Darken1);
                                    }
                                }
                                else
                                {
                                    // Placeholder when logo file doesn't exist
                                    logoRow.RelativeItem().AlignCenter().AlignMiddle()
                                        .Border(1)
                                        .BorderColor(Colors.Grey.Medium)
                                        .Background(Colors.Grey.Lighten4)
                                        .Padding(8)
                                        .Text("LOGO")
                                        .FontSize(10)
                                        .FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            // Title
                            column.Item().AlignCenter().Text("CONTRACT SLIP")
                                .FontSize(18)
                                .SemiBold()
                                .FontColor(Colors.Blue.Darken2);

                            column.Item().PaddingVertical(10);

                            // Contract Header Information
                            column.Item().Row(row =>
                            {
                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text($"Contract No: {data.Contract.ContractNo}").SemiBold();
                                    col.Item().Text($"Status: {data.Contract.ContractStatus}");
                                    col.Item().Text($"Transaction Date: {data.Contract.TransactionDate:dd/MM/yyyy}");
                                });

                                row.RelativeItem().Column(col =>
                                {
                                    col.Item().Text($"Customer: {data.Contract.CustomerName}").SemiBold();
                                    if (!string.IsNullOrEmpty(data.Contract.JointCustomerName))
                                        col.Item().Text($"Joint Customer: {data.Contract.JointCustomerName}");
                                    col.Item().Text($"Created By: {data.Contract.CreatedBy}");
                                    col.Item().Text($"Created On: {data.Contract.CreatedOn:dd/MM/yyyy}");
                                });
                            });

                            column.Item().PaddingVertical(10);

                            // Units Section
                            if (data.Units.Any())
                            {
                                column.Item().Text("LEASED UNITS").FontSize(14).SemiBold();
                                column.Item().PaddingVertical(5);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2); // Unit No
                                        columns.RelativeColumn(3); // Property
                                        columns.RelativeColumn(2); // Type
                                        columns.RelativeColumn(2); // From Date
                                        columns.RelativeColumn(2); // To Date
                                        columns.RelativeColumn(2); // Rent/Month
                                        columns.RelativeColumn(2); // Total
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Unit No").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Property").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Type").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("From Date").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("To Date").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Rent/Month").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Total").SemiBold();
                                    });

                                    // Data rows
                                    foreach (var unit in data.Units)
                                    {
                                        table.Cell().BorderBottom(1).Padding(5).Text(unit.UnitNo);
                                        table.Cell().BorderBottom(1).Padding(5).Text(unit.PropertyName);
                                        table.Cell().BorderBottom(1).Padding(5).Text(unit.UnitTypeName);
                                        table.Cell().BorderBottom(1).Padding(5).Text(unit.FromDate.ToString("dd/MM/yyyy"));
                                        table.Cell().BorderBottom(1).Padding(5).Text(unit.ToDate.ToString("dd/MM/yyyy"));
                                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(unit.RentPerMonth.ToString("N2"));
                                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(unit.TotalAmount.ToString("N2"));
                                    }
                                });

                                column.Item().PaddingVertical(10);
                            }

                            // Additional Charges Section
                            if (data.AdditionalCharges.Any())
                            {
                                column.Item().Text("ADDITIONAL CHARGES").FontSize(14).SemiBold();
                                column.Item().PaddingVertical(5);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(3); // Charge Name
                                        columns.RelativeColumn(2); // Category
                                        columns.RelativeColumn(2); // Amount
                                        columns.RelativeColumn(1); // Tax %
                                        columns.RelativeColumn(2); // Tax Amount
                                        columns.RelativeColumn(2); // Total
                                    });

                                    // Header
                                    table.Header(header =>
                                    {
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Charge").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Category").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Amount").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tax %").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Tax Amt").SemiBold();
                                        header.Cell().Background(Colors.Grey.Lighten2).Padding(5).Text("Total").SemiBold();
                                    });

                                    // Data rows
                                    foreach (var charge in data.AdditionalCharges)
                                    {
                                        table.Cell().BorderBottom(1).Padding(5).Text(charge.ChargesName);
                                        table.Cell().BorderBottom(1).Padding(5).Text(charge.ChargesCategoryName);
                                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(charge.Amount.ToString("N2"));
                                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(charge.TaxPercentage?.ToString("N1") ?? "0.0");
                                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(charge.TaxAmount?.ToString("N2") ?? "0.00");
                                        table.Cell().BorderBottom(1).Padding(5).AlignRight().Text(charge.TotalAmount.ToString("N2"));
                                    }
                                });

                                column.Item().PaddingVertical(10);
                            }

                            // Summary Section
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
                                            r.ConstantItem(80).AlignRight().Text(data.Contract.TotalAmount.ToString("N2"));
                                        });
                                        col.Item().Row(r =>
                                        {
                                            r.RelativeItem().Text("Additional Charges:");
                                            r.ConstantItem(80).AlignRight().Text(data.Contract.AdditionalCharges.ToString("N2"));
                                        });
                                        col.Item().BorderTop(1).PaddingTop(5).Row(r =>
                                        {
                                            r.RelativeItem().Text("Grand Total:").SemiBold();
                                            r.ConstantItem(80).AlignRight().Text(data.Contract.GrandTotal.ToString("N2")).SemiBold();
                                        });
                                    });
                                });
                            });

                            // Remarks Section
                            if (!string.IsNullOrEmpty(data.Contract.Remarks))
                            {
                                column.Item().PaddingTop(20).Column(remarksColumn =>
                                {
                                    remarksColumn.Item().Text("REMARKS").FontSize(12).SemiBold();
                                    remarksColumn.Item().PaddingTop(5).Text(data.Contract.Remarks);
                                });
                            }

                            // Attachments Section
                            if (data.Attachments.Any())
                            {
                                column.Item().PaddingTop(20).Column(attachColumn =>
                                {
                                    attachColumn.Item().Text("ATTACHMENTS").FontSize(12).SemiBold();
                                    attachColumn.Item().PaddingTop(5);

                                    foreach (var attachment in data.Attachments)
                                    {
                                        attachColumn.Item().Text($"• {attachment.DocumentName} ({attachment.DocTypeName})").FontSize(9);
                                    }
                                });
                            }
                        });

                    page.Footer()
                        .Height(50)
                        .Background(Colors.Grey.Lighten4)
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                            x.Span($" | Generated on {DateTime.Now:dd/MM/yyyy HH:mm}");
                        });
                });
            });
        }
    }
}