namespace LeaseERP.Shared.DTOs
{
    public class TerminationSlipData
    {
        public TerminationMasterInfo Termination { get; set; } = new();
        public List<TerminationDeductionInfo> Deductions { get; set; } = new();
        public List<TerminationAttachmentInfo> Attachments { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
    }

    public class TerminationMasterInfo
    {
        public long TerminationID { get; set; }
        public string TerminationNo { get; set; } = string.Empty;
        public long ContractID { get; set; }
        public string ContractNo { get; set; } = string.Empty;
        public DateTime TerminationDate { get; set; }
        public DateTime? NoticeDate { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? VacatingDate { get; set; }
        public DateTime? MoveOutDate { get; set; }
        public DateTime? KeyReturnDate { get; set; }
        public int? StayPeriodDays { get; set; }
        public decimal? StayPeriodAmount { get; set; }
        public string TerminationReason { get; set; } = string.Empty;
        public string TerminationStatus { get; set; } = string.Empty;
        public decimal? TotalDeductions { get; set; }
        public decimal? SecurityDepositAmount { get; set; }
        public decimal? AdjustAmount { get; set; }
        public decimal? TotalInvoiced { get; set; }
        public decimal? TotalReceived { get; set; }
        public decimal? CreditNoteAmount { get; set; }
        public decimal? RefundAmount { get; set; }
        public bool IsRefundProcessed { get; set; }
        public DateTime? RefundDate { get; set; }
        public string RefundReference { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        // Contract and Customer Information
        public string CustomerFullName { get; set; } = string.Empty;
        public long CustomerID { get; set; }
        public long? PropertyID { get; set; }
        public string PropertyName { get; set; } = string.Empty;
        public string UnitNumbers { get; set; } = string.Empty;

        // Audit Information
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }
    }

    public class TerminationDeductionInfo
    {
        public long TerminationDeductionID { get; set; }
        public long TerminationID { get; set; }
        public long? DeductionID { get; set; }
        public string DeductionName { get; set; } = string.Empty;
        public string DeductionDescription { get; set; } = string.Empty;
        public decimal DeductionAmount { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string DeductionCode { get; set; } = string.Empty;
        public string DeductionType { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }
    }

    public class TerminationAttachmentInfo
    {
        public long TerminationAttachmentID { get; set; }
        public long TerminationID { get; set; }
        public long DocTypeID { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public byte[]? FileContent { get; set; }
        public string FileContentType { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime? DocIssueDate { get; set; }
        public DateTime? DocExpiryDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }
    }

    public class TerminationSummaryInfo
    {
        public decimal SecurityDeposit { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal AdjustmentAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal CreditNoteAmount { get; set; }
        public bool IsRefundDue => RefundAmount > 0;
        public bool IsCreditNoteDue => CreditNoteAmount > 0;
    }
}