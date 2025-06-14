// LeaseERP.API/Controllers/PdfController.cs (Updated to use generic framework)
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LeaseERP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PdfController : ControllerBase
    {
        private readonly IPdfService _pdfService;
        private readonly IReportEngine _reportEngine;
        private readonly ILogger<PdfController> _logger;

        public PdfController(
            IPdfService pdfService,
            IReportEngine reportEngine,
            ILogger<PdfController> logger)
        {
            _pdfService = pdfService;
            _reportEngine = reportEngine;
            _logger = logger;
        }

        // Existing endpoints remain the same for backward compatibility
        [HttpGet("contract-slip/{contractId}")]
        public async Task<IActionResult> GenerateContractSlip(long contractId)
        {
            try
            {
                var userName = GetCurrentUserName();
                _logger.LogInformation("Generating contract slip PDF for ContractID: {ContractId} by user: {UserName}", contractId, userName);

                var pdfBytes = await _pdfService.GenerateContractSlipAsync(contractId, userName);
                var fileName = $"ContractSlip_{contractId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract slip PDF for ContractID: {ContractId}", contractId);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GenerateInvoice(long invoiceId)
        {
            try
            {
                var userName = GetCurrentUserName();
                _logger.LogInformation("Generating invoice PDF for InvoiceID: {InvoiceId} by user: {UserName}", invoiceId, userName);

                var pdfBytes = await _pdfService.GenerateInvoiceAsync(invoiceId, userName);
                var fileName = $"Invoice_{invoiceId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice PDF for InvoiceID: {InvoiceId}", invoiceId);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        [HttpGet("receipt/{receiptId}")]
        public async Task<IActionResult> GenerateReceipt(long receiptId)
        {
            try
            {
                var userName = GetCurrentUserName();
                _logger.LogInformation("Generating receipt PDF for ReceiptID: {ReceiptId} by user: {UserName}", receiptId, userName);

                var pdfBytes = await _pdfService.GenerateReceiptAsync(receiptId, userName);
                var fileName = $"Receipt_{receiptId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt PDF for ReceiptID: {ReceiptId}", receiptId);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        // New endpoint for termination slips
        [HttpGet("termination-slip/{terminationId}")]
        public async Task<IActionResult> GenerateTerminationSlip(long terminationId)
        {
            try
            {
                var userName = GetCurrentUserName();
                _logger.LogInformation("Generating termination slip PDF for TerminationID: {TerminationId} by user: {UserName}", terminationId, userName);

                var pdfBytes = await _pdfService.GenerateTerminationSlipAsync(terminationId, userName);
                var fileName = $"TerminationSlip_{terminationId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating termination slip PDF for TerminationID: {TerminationId}", terminationId);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        // Generic endpoint for any report type
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                var userName = GetCurrentUserName();
                _logger.LogInformation("Generating report: {ReportType} by user: {UserName}", request.ReportType, userName);

                if (string.IsNullOrEmpty(request.ReportType))
                {
                    return BadRequest(new { Success = false, Message = "Report type is required." });
                }

                var pdfBytes = await _pdfService.GenerateReportAsync(request.ReportType, request.Parameters ?? new Dictionary<string, object>(), userName);
                var fileName = $"{request.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report: {ReportType}", request.ReportType);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        // Get available report types
        [HttpGet("available-reports")]
        public IActionResult GetAvailableReports()
        {
            try
            {
                var reports = _reportEngine.GetAvailableReports();
                return Ok(new { Success = true, Data = reports });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available reports");
                return StatusCode(500, new { Success = false, Message = "Error retrieving available reports." });
            }
        }

        // Get report configuration
        [HttpGet("configuration/{reportType}")]
        public async Task<IActionResult> GetReportConfiguration(string reportType)
        {
            try
            {
                var config = await _reportEngine.GetReportConfigurationAsync(reportType);
                return Ok(new { Success = true, Data = config });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report configuration for: {ReportType}", reportType);
                return StatusCode(500, new { Success = false, Message = "Error retrieving report configuration." });
            }
        }

        // Preview endpoint for any report type
        [HttpPost("preview")]
        public async Task<IActionResult> PreviewReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                var userName = GetCurrentUserName();
                _logger.LogInformation("Generating report preview: {ReportType} by user: {UserName}", request.ReportType, userName);

                if (string.IsNullOrEmpty(request.ReportType))
                {
                    return BadRequest(new { Success = false, Message = "Report type is required." });
                }

                var pdfBytes = await _pdfService.GenerateReportAsync(request.ReportType, request.Parameters ?? new Dictionary<string, object>(), userName);

                // Return PDF for inline viewing in browser
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report preview: {ReportType}", request.ReportType);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF preview. Please try again." });
            }
        }

        private string GetCurrentUserName()
        {
            return User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
        }
    }

    public class GenerateReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
        public string? Orientation { get; set; } // "Portrait" or "Landscape"
        public string? Format { get; set; } = "PDF"; // Future: PDF, Excel, CSV
    }
}