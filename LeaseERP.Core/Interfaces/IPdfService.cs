namespace LeaseERP.Core.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GenerateContractSlipAsync(long contractId, string actionBy);
        Task<byte[]> GenerateInvoiceAsync(long invoiceId, string actionBy);
        Task<byte[]> GenerateReceiptAsync(long receiptId, string actionBy);
        Task<byte[]> GenerateCustomReportAsync(string reportType, Dictionary<string, object> parameters);
    }
}
