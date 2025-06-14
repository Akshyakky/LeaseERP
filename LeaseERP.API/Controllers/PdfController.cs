using LeaseERP.Core.Interfaces;
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
        private readonly ILogger<PdfController> _logger;

        public PdfController(
            IPdfService pdfService,
            ILogger<PdfController> logger)
        {
            _pdfService = pdfService;
            _logger = logger;
        }

        [HttpGet("contract-slip/{contractId}")]
        public async Task<IActionResult> GenerateContractSlip(long contractId)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
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

        [HttpGet("contract-slip/{contractId}/preview")]
        public async Task<IActionResult> PreviewContractSlip(long contractId)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating contract slip PDF preview for ContractID: {ContractId} by user: {UserName}", contractId, userName);

                var pdfBytes = await _pdfService.GenerateContractSlipAsync(contractId, userName);

                // Return PDF for inline viewing in browser
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract slip PDF preview for ContractID: {ContractId}", contractId);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF preview. Please try again." });
            }
        }

        [HttpPost("contract-slip/batch")]
        public async Task<IActionResult> GenerateBatchContractSlips([FromBody] BatchPdfRequest request)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating batch contract slips for {Count} contracts by user: {UserName}", request.ContractIds.Count, userName);

                if (request.ContractIds == null || !request.ContractIds.Any())
                {
                    return BadRequest(new { Success = false, Message = "Contract IDs are required." });
                }

                if (request.ContractIds.Count > 50) // Limit batch size
                {
                    return BadRequest(new { Success = false, Message = "Maximum 50 contracts can be processed in a single batch." });
                }

                // For batch processing, we'll create a ZIP file containing all PDFs
                using var memoryStream = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var contractId in request.ContractIds)
                    {
                        try
                        {
                            var pdfBytes = await _pdfService.GenerateContractSlipAsync(contractId, userName);
                            var entry = archive.CreateEntry($"ContractSlip_{contractId}.pdf");

                            using var entryStream = entry.Open();
                            await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate PDF for ContractID: {ContractId} in batch", contractId);
                            // Continue with other contracts, but log the failure
                        }
                    }
                }

                var zipBytes = memoryStream.ToArray();
                var fileName = $"ContractSlips_Batch_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                return File(zipBytes, "application/zip", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating batch contract slips");
                return StatusCode(500, new { Success = false, Message = "Error generating batch PDFs. Please try again." });
            }
        }

        [HttpPost("contract-list")]
        public async Task<IActionResult> GenerateContractList([FromBody] ContractListRequest request)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating contract list PDF by user: {UserName}", userName);

                if (request == null)
                {
                    request = new ContractListRequest();
                }

                var pdfBytes = await _pdfService.GenerateContractListAsync(request, userName);

                var fileName = $"ContractList_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract list PDF");
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        [HttpPost("contract-list/preview")]
        public async Task<IActionResult> PreviewContractList([FromBody] ContractListRequest request)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating contract list PDF preview by user: {UserName}", userName);

                if (request == null)
                {
                    request = new ContractListRequest();
                }

                var pdfBytes = await _pdfService.GenerateContractListAsync(request, userName);

                // Return PDF for inline viewing in browser
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract list PDF preview");
                return StatusCode(500, new { Success = false, Message = "Error generating PDF preview. Please try again." });
            }
        }

        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GenerateInvoice(long invoiceId)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating invoice PDF for InvoiceID: {InvoiceId} by user: {UserName}", invoiceId, userName);

                var pdfBytes = await _pdfService.GenerateInvoiceAsync(invoiceId, userName);

                var fileName = $"Invoice_{invoiceId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { Success = false, Message = "Invoice PDF generation is not yet implemented." });
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
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating receipt PDF for ReceiptID: {ReceiptId} by user: {UserName}", receiptId, userName);

                var pdfBytes = await _pdfService.GenerateReceiptAsync(receiptId, userName);

                var fileName = $"Receipt_{receiptId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { Success = false, Message = "Receipt PDF generation is not yet implemented." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt PDF for ReceiptID: {ReceiptId}", receiptId);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }

        [HttpPost("custom-report")]
        public async Task<IActionResult> GenerateCustomReport([FromBody] CustomReportRequest request)
        {
            try
            {
                var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "System";
                _logger.LogInformation("Generating custom report: {ReportType} by user: {UserName}", request.ReportType, userName);

                if (string.IsNullOrEmpty(request.ReportType))
                {
                    return BadRequest(new { Success = false, Message = "Report type is required." });
                }

                var pdfBytes = await _pdfService.GenerateCustomReportAsync(request.ReportType, request.Parameters ?? new Dictionary<string, object>());

                var fileName = $"{request.ReportType}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (NotImplementedException)
            {
                return StatusCode(501, new { Success = false, Message = $"Custom report '{request.ReportType}' is not yet implemented." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating custom report: {ReportType}", request.ReportType);
                return StatusCode(500, new { Success = false, Message = "Error generating PDF. Please try again." });
            }
        }
    }

    public class BatchPdfRequest
    {
        public List<long> ContractIds { get; set; } = new List<long>();
    }

    public class CustomReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object>? Parameters { get; set; }
    }
}
