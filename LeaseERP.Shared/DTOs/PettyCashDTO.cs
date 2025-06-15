// LeaseERP.Shared/DTOs/PettyCashDTO.cs
namespace LeaseERP.Shared.DTOs
{
    public class PettyCashSlipData
    {
        public PettyCashVoucherInfo Voucher { get; set; } = new();
        public List<PettyCashLineInfo> Lines { get; set; } = new();
        public List<PettyCashAttachmentInfo> Attachments { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
    }

    public class PettyCashVoucherInfo
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
        public string? PaidTo { get; set; }
        public string? InvoiceNo { get; set; }
        public string? RefNo { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public long? BankID { get; set; }
        public string PostingStatus { get; set; } = string.Empty;

        // Related Entity Information
        public string CompanyName { get; set; } = string.Empty;
        public string FYDescription { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public string? BankName { get; set; }

        // Financial Summary
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }

        // Audit Information
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class PettyCashLineInfo
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

    public class PettyCashAttachmentInfo
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

    public class PettyCashListRequest
    {
        public string? SearchText { get; set; }
        public DateTime? FilterDateFrom { get; set; }
        public DateTime? FilterDateTo { get; set; }
        public long? FilterCompanyID { get; set; }
        public long? FilterFiscalYearID { get; set; }
        public string? FilterStatus { get; set; }
        public long? FilterAccountID { get; set; }
        public string? FilterPaidTo { get; set; }
        public decimal? FilterAmountFrom { get; set; }
        public decimal? FilterAmountTo { get; set; }
        public string ReportTitle { get; set; } = "Petty Cash Voucher List Report";
    }

    public class PettyCashListData
    {
        public List<PettyCashSummaryInfo> Vouchers { get; set; } = new();
        public PettyCashListSummary Summary { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
        public string ReportTitle { get; set; } = "Petty Cash Voucher List Report";
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public PettyCashListFilters AppliedFilters { get; set; } = new();
    }

    public class PettyCashSummaryInfo
    {
        public string VoucherNo { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public DateTime PostingDate { get; set; }
        public string PostingStatus { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string FYDescription { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public string? PaidTo { get; set; }
        public string? Description { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class PettyCashListSummary
    {
        public int TotalVouchers { get; set; }
        public decimal TotalDebitAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public Dictionary<string, decimal> StatusAmountBreakdown { get; set; } = new();
        public int DraftCount { get; set; }
        public int PendingCount { get; set; }
        public int PostedCount { get; set; }
        public decimal DraftAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal PostedAmount { get; set; }
    }

    public class PettyCashListFilters
    {
        public string? SearchText { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? CompanyName { get; set; }
        public string? FiscalYear { get; set; }
        public string? Status { get; set; }
        public string? AccountName { get; set; }
        public string? PaidTo { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
    }
}