// LeaseERP.Shared/DTOs/PaymentVoucherDTO.cs
namespace LeaseERP.Shared.DTOs
{
    public class PaymentVoucherSlipData
    {
        public PaymentVoucherInfo Voucher { get; set; } = new();
        public List<PaymentVoucherLineInfo> Lines { get; set; } = new();
        public List<PaymentVoucherAttachmentInfo> Attachments { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
    }

    public class PaymentVoucherInfo
    {
        public long PostingID { get; set; }
        public string VoucherNo { get; set; } = string.Empty;
        public string VoucherType { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime PostingDate { get; set; }
        public long CompanyID { get; set; }
        public long FiscalYearID { get; set; }
        public long CurrencyID { get; set; }
        public decimal ExchangeRate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Narration { get; set; } = string.Empty;

        // Payment specific fields
        public string PaymentType { get; set; } = string.Empty; // Cheque, Cash, Bank Transfer, Online
        public long PaymentAccountID { get; set; }
        public long? SupplierID { get; set; }
        public string? PaidTo { get; set; }
        public string? RefNo { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public long? BankID { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = string.Empty; // Draft, Pending, Paid, Cancelled

        // Reference information
        public string? ReferenceType { get; set; }
        public long? ReferenceID { get; set; }
        public string? ReferenceNo { get; set; }

        // Related Entity Information
        public string CompanyName { get; set; } = string.Empty;
        public string FYDescription { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public string PaymentAccountName { get; set; } = string.Empty;
        public string PaymentAccountCode { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? BankName { get; set; }

        // Financial Summary
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }

        // Audit Information
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovedOn { get; set; }
    }

    public class PaymentVoucherLineInfo
    {
        public long PostingID { get; set; }
        public long AccountID { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal BaseAmount { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? LineTaxAmount { get; set; }
        public string LineDescription { get; set; } = string.Empty;
        public long? CostCenter1ID { get; set; }
        public long? CostCenter2ID { get; set; }
        public long? CostCenter3ID { get; set; }
        public long? CostCenter4ID { get; set; }
        public long? CustomerID { get; set; }
        public long? SupplierID { get; set; }
        public decimal BaseCurrencyAmount { get; set; }

        // Related Entity Information
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? CostCenter1Name { get; set; }
        public string? CostCenter2Name { get; set; }
        public string? CostCenter3Name { get; set; }
        public string? CostCenter4Name { get; set; }
        public string? CustomerFullName { get; set; }
        public string? SupplierName { get; set; }
    }

    public class PaymentVoucherAttachmentInfo
    {
        public long PostingAttachmentID { get; set; }
        public long PostingID { get; set; }
        public long DocTypeID { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public byte[]? FileContent { get; set; }
        public string FileContentType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string DocumentDescription { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public long UploadedByUserID { get; set; }
        public bool IsRequired { get; set; }
        public int? DisplayOrder { get; set; }

        // Related Entity Information
        public string DocTypeName { get; set; } = string.Empty;
        public string UploadedByUserName { get; set; } = string.Empty;
    }

    public class PaymentVoucherListRequest
    {
        public string? SearchText { get; set; }
        public DateTime? FilterDateFrom { get; set; }
        public DateTime? FilterDateTo { get; set; }
        public long? FilterCompanyID { get; set; }
        public long? FilterFiscalYearID { get; set; }
        public string? FilterStatus { get; set; }
        public long? FilterSupplierID { get; set; }
        public string? FilterPaymentType { get; set; }
        public long? FilterAccountID { get; set; }
        public decimal? FilterAmountFrom { get; set; }
        public decimal? FilterAmountTo { get; set; }
        public string ReportTitle { get; set; } = "Payment Voucher List Report";
    }

    public class PaymentVoucherListData
    {
        public List<PaymentVoucherSummaryInfo> Vouchers { get; set; } = new();
        public PaymentVoucherListSummary Summary { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
        public string ReportTitle { get; set; } = "Payment Voucher List Report";
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public PaymentVoucherListFilters AppliedFilters { get; set; } = new();
    }

    public class PaymentVoucherSummaryInfo
    {
        public string VoucherNo { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime PostingDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string FYDescription { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public string PaymentAccountName { get; set; } = string.Empty;
        public string? SupplierName { get; set; }
        public string? PaidTo { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? BankName { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class PaymentVoucherListSummary
    {
        public int TotalVouchers { get; set; }
        public decimal TotalPaymentAmount { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public Dictionary<string, decimal> StatusAmountBreakdown { get; set; } = new();
        public Dictionary<string, int> PaymentTypeBreakdown { get; set; } = new();
        public Dictionary<string, decimal> PaymentTypeAmountBreakdown { get; set; } = new();
        public int DraftCount { get; set; }
        public int PendingCount { get; set; }
        public int PaidCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal DraftAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal CancelledAmount { get; set; }
    }

    public class PaymentVoucherListFilters
    {
        public string? SearchText { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CompanyName { get; set; }
        public string? FiscalYear { get; set; }
        public string? Status { get; set; }
        public string? SupplierName { get; set; }
        public string? PaymentType { get; set; }
        public string? AccountName { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
    }
}