// LeaseERP.Shared/DTOs/InvoiceDTO.cs
namespace LeaseERP.Shared.DTOs
{
    public class InvoiceSlipData
    {
        public InvoiceMasterInfo Invoice { get; set; } = new();
        public List<InvoicePaymentInfo> Payments { get; set; } = new();
        public List<InvoicePostingInfo> Postings { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
    }

    public class InvoiceMasterInfo
    {
        public long LeaseInvoiceID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public long ContractID { get; set; }
        public long ContractUnitID { get; set; }
        public long CustomerID { get; set; }
        public long CompanyID { get; set; }
        public long FiscalYearID { get; set; }
        public string InvoiceType { get; set; } = string.Empty;
        public string InvoiceStatus { get; set; } = string.Empty;
        public DateTime? PeriodFromDate { get; set; }
        public DateTime? PeriodToDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public long CurrencyID { get; set; }
        public decimal ExchangeRate { get; set; }
        public long? PaymentTermID { get; set; }
        public long? SalesPersonID { get; set; }
        public long? TaxID { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public DateTime? NextInvoiceDate { get; set; }
        public string? Notes { get; set; }
        public string? InternalNotes { get; set; }

        // Related Entity Information
        public string CustomerFullName { get; set; } = string.Empty;
        public string CustomerNo { get; set; } = string.Empty;
        public string CustomerTaxNo { get; set; } = string.Empty;
        public string ContractNo { get; set; } = string.Empty;
        public string ContractStatus { get; set; } = string.Empty;
        public string UnitNo { get; set; } = string.Empty;
        public string UnitStatus { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string PropertyNo { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public string? PaymentTermName { get; set; }
        public string? SalesPersonName { get; set; }
        public string? TaxName { get; set; }
        public decimal? TaxRate { get; set; }

        // Status Information
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public bool IsPosted { get; set; }

        // Audit Information
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class InvoicePaymentInfo
    {
        public long LeaseReceiptID { get; set; }
        public string ReceiptNo { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public decimal ReceivedAmount { get; set; }
        public string PaymentType { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public string? BankAccountNo { get; set; }
        public string? ChequeNo { get; set; }
        public DateTime? ChequeDate { get; set; }
        public string? TransactionReference { get; set; }
        public string? Notes { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
    }

    public class InvoicePostingInfo
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

    public class InvoiceListRequest
    {
        public string? SearchText { get; set; }
        public string? FilterInvoiceStatus { get; set; }
        public string? FilterInvoiceType { get; set; }
        public long? FilterPropertyID { get; set; }
        public long? FilterUnitID { get; set; }
        public long? FilterCustomerID { get; set; }
        public long? FilterContractID { get; set; }
        public DateTime? FilterFromDate { get; set; }
        public DateTime? FilterToDate { get; set; }
        public DateTime? FilterDueDateFrom { get; set; }
        public DateTime? FilterDueDateTo { get; set; }
        public bool? FilterPostedOnly { get; set; }
        public bool? FilterUnpostedOnly { get; set; }
        public bool? FilterOverdueOnly { get; set; }
        public string ReportTitle { get; set; } = "Invoice List Report";
    }

    public class InvoiceListData
    {
        public List<InvoiceSummaryInfo> Invoices { get; set; } = new();
        public InvoiceListSummary Summary { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
        public string ReportTitle { get; set; } = "Invoice List Report";
        public DateTime GeneratedOn { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; } = string.Empty;
        public InvoiceListFilters AppliedFilters { get; set; } = new();
    }

    public class InvoiceSummaryInfo
    {
        public long LeaseInvoiceID { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string InvoiceType { get; set; } = string.Empty;
        public string InvoiceStatus { get; set; } = string.Empty;
        public DateTime? PeriodFromDate { get; set; }
        public DateTime? PeriodToDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string ContractNo { get; set; } = string.Empty;
        public string UnitNo { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public bool IsPosted { get; set; }
    }

    public class InvoiceListSummary
    {
        public int TotalInvoices { get; set; }
        public decimal TotalInvoiceValue { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public decimal TotalBalanceAmount { get; set; }
        public Dictionary<string, int> StatusBreakdown { get; set; } = new();
        public Dictionary<string, decimal> StatusAmountBreakdown { get; set; } = new();
        public int OverdueCount { get; set; }
        public decimal OverdueAmount { get; set; }
        public int PostedCount { get; set; }
        public int UnpostedCount { get; set; }
    }

    public class InvoiceListFilters
    {
        public string? SearchText { get; set; }
        public string? InvoiceStatus { get; set; }
        public string? InvoiceType { get; set; }
        public string? CustomerName { get; set; }
        public string? ContractNo { get; set; }
        public string? PropertyName { get; set; }
        public string? UnitNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? DueDateFrom { get; set; }
        public DateTime? DueDateTo { get; set; }
        public bool? PostedOnly { get; set; }
        public bool? UnpostedOnly { get; set; }
        public bool? OverdueOnly { get; set; }
    }
}