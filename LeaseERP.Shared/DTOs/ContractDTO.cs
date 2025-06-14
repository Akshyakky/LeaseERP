namespace LeaseERP.Shared.DTOs
{
    public class ContractSlipData
    {
        public ContractMasterInfo Contract { get; set; } = new();
        public List<ContractUnitInfo> Units { get; set; } = new();
        public List<ContractChargeInfo> AdditionalCharges { get; set; } = new();
        public List<ContractAttachmentInfo> Attachments { get; set; } = new();
        public CompanyInfo Company { get; set; } = new();
    }

    public class ContractMasterInfo
    {
        public long ContractID { get; set; }
        public string ContractNo { get; set; } = string.Empty;
        public string ContractStatus { get; set; } = string.Empty;
        public long CustomerID { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public long? JointCustomerID { get; set; }
        public string JointCustomerName { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AdditionalCharges { get; set; }
        public decimal GrandTotal { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
        public DateTime? UpdatedOn { get; set; }
    }

    public class ContractUnitInfo
    {
        public long ContractUnitID { get; set; }
        public long UnitID { get; set; }
        public string UnitNo { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string UnitTypeName { get; set; } = string.Empty;
        public string UnitCategoryName { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public int? BedRooms { get; set; }
        public int? BathRooms { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime? FitoutFromDate { get; set; }
        public DateTime? FitoutToDate { get; set; }
        public DateTime? CommencementDate { get; set; }
        public int? ContractDays { get; set; }
        public int? ContractMonths { get; set; }
        public int? ContractYears { get; set; }
        public decimal RentPerMonth { get; set; }
        public decimal RentPerYear { get; set; }
        public int? NoOfInstallments { get; set; }
        public DateTime? RentFreePeriodFrom { get; set; }
        public DateTime? RentFreePeriodTo { get; set; }
        public decimal? RentFreeAmount { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ContractChargeInfo
    {
        public long ContractAdditionalChargeID { get; set; }
        public long AdditionalChargesID { get; set; }
        public string ChargesName { get; set; } = string.Empty;
        public string ChargesCode { get; set; } = string.Empty;
        public string ChargesCategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ContractAttachmentInfo
    {
        public long ContractAttachmentID { get; set; }
        public string DocumentName { get; set; } = string.Empty;
        public string DocTypeName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public DateTime? DocIssueDate { get; set; }
        public DateTime? DocExpiryDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class CompanyInfo
    {
        public long CompanyID { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public string CompanyEmail { get; set; } = string.Empty;
        public string CompanyWebsite { get; set; } = string.Empty;
        public string CompanyLogo { get; set; } = string.Empty;
        public string TaxRegNo { get; set; } = string.Empty;
        public string CommercialRegNo { get; set; } = string.Empty;
    }
}
