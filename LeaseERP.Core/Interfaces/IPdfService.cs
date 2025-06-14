using LeaseERP.Shared.DTOs;

namespace LeaseERP.Core.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GenerateContractSlipAsync(long contractId, string actionBy);
        Task<byte[]> GenerateInvoiceAsync(long invoiceId, string actionBy);
        Task<byte[]> GenerateReceiptAsync(long receiptId, string actionBy);
        Task<byte[]> GenerateTerminationSlipAsync(long terminationId, string actionBy);
        Task<byte[]> GenerateCustomReportAsync(string reportType, Dictionary<string, object> parameters);
        Task<byte[]> GenerateContractListAsync(ContractListRequest request, string actionBy);
        Task<byte[]> GenerateReportAsync(string reportType, Dictionary<string, object> parameters, string actionBy);
    }
}