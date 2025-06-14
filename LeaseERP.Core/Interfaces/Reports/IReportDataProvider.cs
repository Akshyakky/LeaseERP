// LeaseERP.Core/Interfaces/Reports/IReportDataProvider.cs
using System.Data;

namespace LeaseERP.Core.Interfaces.Reports
{
    public interface IReportDataProvider
    {
        Task<ReportData> GetDataAsync(ReportRequest request);
        string GetStoredProcedureName();
        Dictionary<string, object> BuildParameters(ReportRequest request);
    }

    public class ReportData
    {
        public DataSet DataSet { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }

    public class ReportRequest
    {
        public string ReportType { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new();
        public string ActionBy { get; set; } = string.Empty;
        public ReportFormat Format { get; set; } = ReportFormat.PDF;
        public ReportOrientation Orientation { get; set; } = ReportOrientation.Portrait;
    }

    public enum ReportFormat
    {
        PDF,
        Excel,
        CSV
    }

    public enum ReportOrientation
    {
        Portrait,
        Landscape
    }
}