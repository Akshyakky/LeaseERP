// LeaseERP.Core/Interfaces/Reports/IReportEngine.cs
namespace LeaseERP.Core.Interfaces.Reports
{
    public interface IReportEngine
    {
        Task<byte[]> GenerateReportAsync(ReportRequest request);
        Task<ReportConfiguration> GetReportConfigurationAsync(string reportType);
        IEnumerable<string> GetAvailableReports();
        Task<ReportData> GetReportDataAsync(ReportRequest request);
    }
}