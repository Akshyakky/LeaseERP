// LeaseERP.Shared/DTOs/ReceiptDTO.cs
namespace LeaseERP.Shared.DTOs
{
    public class ReceiptSlipData
    {
        public ReceiptMasterInfo Receipt { get; set; } = new();
        public List<ReceiptPostingInfo> Postings { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
    }

    public class ReceiptMasterInfo
    {
        public long LeaseReceiptID { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public long? LeaseInvoiceID { get; set; }
        public long CustomerID { get; set; }
        public long CompanyID { get; set; }
        public long FiscalYearID { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal ReceivedAmount { get; set; }
        public long CurrencyID { get; set; }
        public decimal ExchangeRate { get; set; }
        public long? BankID { get; set; }
        public string? BankAccountNo { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? TransactionReference { get; set; }
        public long? DepositedBankID { get; set; }
        public DateTime? DepositDate { get; set; }
        public DateTime? ClearanceDate { get; set; }
        public bool IsAdvancePayment { get; set; }
        public decimal? SecurityDepositAmount { get; set; }
        public decimal? PenaltyAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public long? ReceivedByUserID { get; set; }
        public long? AccountID { get; set; }
        public bool IsPosted { get; set; }
        public long? PostingID { get; set; }
        public string? Notes { get; set; }

        // Related Entity Information
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerNo { get; set; } = string.Empty;
        public string CustomerTaxNo { get; set; } = string.Empty;
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public decimal? InvoiceAmount { get; set; }
        public decimal? InvoiceBalance { get; set; }
        public string? ContractNo { get; set; }
        public string? UnitNo { get; set; }
        public string? PropertyName { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? SwiftCode { get; set; }
        public string? DepositBankName { get; set; }
        public string? ReceivedByUser { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string FiscalYear { get; set; } = string.Empty;

        // Audit Information
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class ReceiptPostingInfo
    {
        public long PostingID { get; set; }
        public string VoucherNo { get; set; } = string.Empty;
        public DateTime PostingDate { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Narration { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public bool IsReversed { get; set; }
        public string? ReversalReason { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }

    public class ReceiptListRequest
    {
        public string? SearchText { get; set; }
        public string? FilterPaymentStatus { get; set; }
        public string? FilterPaymentType { get; set; }
        public long? FilterPropertyID { get; set; }
        public long? FilterUnitID { get; set; }
        public long? FilterCustomerID { get; set; }
        public long? FilterContractID { get; set; }
        public DateTime? FilterFromDate { get; set; }
        public DateTime? FilterToDate { get; set; }
        public DateTime? FilterDepositFromDate { get; set; }
        public DateTime? FilterDepositToDate { get; set; }
        public bool? FilterPostedOnly { get; set; }
        public bool? FilterUnpostedOnly { get; set; }
        public bool? FilterAdvanceOnly { get; set; }
        public long? FilterBankID { get; set; }
        public long? FilterReceivedByUserID { get; set; }
        public decimal? FilterAmountFrom { get; set; }
        public decimal? FilterAmountTo { get; set; }
        public string ReportTitle { get; set; } = "Receipt List Report";
    }

    public class ReceiptListData
    {
        public List<ReceiptSummaryInfo> Receipts { get; set; } = new();
        public ReceiptListSummary Summary { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
        public string ReportTitle { get; set; } = "Receipt List Report";
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public ReceiptListFilters AppliedFilters { get; set; } = new();
    }

    public class ReceiptSummaryInfo
    {
        public long LeaseReceiptID { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal ReceivedAmount { get; set; }
        public decimal? SecurityDepositAmount { get; set; }
        public decimal? PenaltyAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public bool IsAdvancePayment { get; set; }
        public string? TransactionReference { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? DepositDate { get; set; }
        public bool IsPosted { get; set; }
        public string? Notes { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string? InvoiceNo { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string? PropertyName { get; set; }
        public string? UnitNo { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string? BankName { get; set; }
        public string? ReceivedByUser { get; set; }
        public int DaysToDeposit { get; set; }
        public bool RequiresDeposit { get; set; }
    }

    public class ReceiptListSummary
    {
        public int TotalReceipts { get; set; }
        public decimal TotalReceiptValue { get; set; }
        public decimal TotalAdvanceAmount { get; set; }
        public decimal TotalSecurityDeposit { get; set; }
        public decimal TotalPenaltyAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public Dictionary<string, decimal> StatusAmountBreakdown { get; set; } = new();
        public Dictionary<string, int> PaymentTypeBreakdown { get; set; } = new();
        public Dictionary<string, decimal> PaymentTypeAmountBreakdown { get; set; } = new();
        public int PostedCount { get; set; }
        public int UnpostedCount { get; set; }
        public decimal PostedAmount { get; set; }
        public decimal UnpostedAmount { get; set; }
        public int PendingDepositCount { get; set; }
        public decimal PendingDepositAmount { get; set; }
        public int AdvancePaymentCount { get; set; }
    }

    public class ReceiptListFilters
    {
        public string? SearchText { get; set; }
        public string? PaymentStatus { get; set; }
        public string? PaymentType { get; set; }
        public string? CustomerName { get; set; }
        public string? PropertyName { get; set; }
        public string? UnitNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? DepositFromDate { get; set; }
        public DateTime? DepositToDate { get; set; }
        public bool? PostedOnly { get; set; }
        public bool? UnpostedOnly { get; set; }
        public bool? AdvanceOnly { get; set; }
        public string? BankName { get; set; }
        public string? ReceivedByUser { get; set; }
        public decimal? AmountFrom { get; set; }
        public decimal? AmountTo { get; set; }
    }
}