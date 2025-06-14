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
            // For now, return a default company info
            return new CompanyInfo
            {
                CompanyID = 1,
                CompanyName = "LeaseERP Solutions",
                CompanyAddress = "123 Business Street, City, Country",
                CompanyPhone = "+1-234-567-8900",
                CompanyEmail = "info@leaseerp.com",
                CompanyWebsite = "www.leaseerp.com",
                TaxRegNo = "TAX123456789",
                CommercialRegNo = "CR123456789"
            };
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
                        .Height(100)
                        .Background(Colors.Grey.Lighten3)
                        .Padding(20)
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

                            row.ConstantItem(100).Height(50).Placeholder();
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