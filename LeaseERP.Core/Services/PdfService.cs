// LeaseERP.Core/Services/PdfService.cs 
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.Extensions.Logging;

namespace LeaseERP.Core.Services
{
    public class PdfService : IPdfService
    {
        private readonly IReportEngine _reportEngine;
        private readonly ILogger<PdfService> _logger;

        public PdfService(IReportEngine reportEngine, ILogger<PdfService> logger)
        {
            _reportEngine = reportEngine;
            _logger = logger;
        }

        public async Task<byte[]> GenerateContractSlipAsync(long contractId, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating contract slip for ContractID: {ContractId}", contractId);

                var request = new ReportRequest
                {
                    ReportType = "contract-slip",
                    ActionBy = actionBy,
                    Parameters = new Dictionary<string, object>
                    {
                        { "ContractId", contractId }
                    }
                };

                return await _reportEngine.GenerateReportAsync(request);
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

                var reportRequest = new ReportRequest
                {
                    ReportType = "contract-list",
                    ActionBy = actionBy,
                    Orientation = ReportOrientation.Landscape,
                    Parameters = new Dictionary<string, object>
                    {
                        { "SearchText", request.SearchText ?? "" },
                        { "FilterCustomerID", request.FilterCustomerID },
                        { "FilterContractStatus", request.FilterContractStatus ?? "" },
                        { "FilterFromDate", request.FilterFromDate },
                        { "FilterToDate", request.FilterToDate },
                        { "FilterUnitID", request.FilterUnitID },
                        { "FilterPropertyID", request.FilterPropertyID },
                        { "ReportTitle", request.ReportTitle }
                    }
                };

                return await _reportEngine.GenerateReportAsync(reportRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract list PDF");
                throw;
            }
        }

        public async Task<byte[]> GenerateInvoiceAsync(long invoiceId, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating invoice PDF for InvoiceID: {InvoiceId}", invoiceId);

                var request = new ReportRequest
                {
                    ReportType = "invoice-slip",
                    ActionBy = actionBy,
                    Parameters = new Dictionary<string, object>
                    {
                        { "InvoiceId", invoiceId }
                    }
                };

                return await _reportEngine.GenerateReportAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF for InvoiceID: {InvoiceId}", invoiceId);
                throw;
            }
        }

        public async Task<byte[]> GenerateReceiptAsync(long receiptId, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating receipt PDF for ReceiptID: {ReceiptId}", receiptId);

                var request = new ReportRequest
                {
                    ReportType = "receipt-slip",
                    ActionBy = actionBy,
                    Parameters = new Dictionary<string, object>
                    {
                        { "ReceiptId", receiptId }
                    }
                };

                return await _reportEngine.GenerateReportAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt PDF for ReceiptID: {ReceiptId}", receiptId);
                throw;
            }
        }

        public async Task<byte[]> GenerateCustomReportAsync(string reportType, Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation("Generating custom report: {ReportType}", reportType);

                var request = new ReportRequest
                {
                    ReportType = reportType,
                    ActionBy = "System",
                    Parameters = parameters
                };

                return await _reportEngine.GenerateReportAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom report: {ReportType}", reportType);
                throw;
            }
        }

        // New method for generating termination slips
        public async Task<byte[]> GenerateTerminationSlipAsync(long terminationId, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating termination slip for TerminationID: {TerminationId}", terminationId);

                var request = new ReportRequest
                {
                    ReportType = "termination-slip",
                    ActionBy = actionBy,
                    Parameters = new Dictionary<string, object>
                    {
                        { "TerminationId", terminationId }
                    }
                };

                return await _reportEngine.GenerateReportAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating termination slip for TerminationID: {TerminationId}", terminationId);
                throw;
            }
        }

        // Generic method for any report type
        public async Task<byte[]> GenerateReportAsync(string reportType, Dictionary<string, object> parameters, string actionBy)
        {
            try
            {
                _logger.LogInformation("Generating report: {ReportType} by {ActionBy}", reportType, actionBy);

                var request = new ReportRequest
                {
                    ReportType = reportType,
                    ActionBy = actionBy,
                    Parameters = parameters
                };

                return await _reportEngine.GenerateReportAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {ReportType}", reportType);
                throw;
            }
        }
    }
}