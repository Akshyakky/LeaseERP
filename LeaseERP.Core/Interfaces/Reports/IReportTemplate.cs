// LeaseERP.Core/Interfaces/Reports/IReportTemplate.cs
using QuestPDF.Infrastructure;

namespace LeaseERP.Core.Interfaces.Reports
{
    public interface IReportTemplate
    {
        string ReportType { get; }
        IDocument CreateDocument(ReportData data, ReportConfiguration config);
        ReportConfiguration GetDefaultConfiguration();
        bool CanHandle(string reportType);
    }

    public class ReportConfiguration
    {
        public string Title { get; set; } = string.Empty;
        public string SubTitle { get; set; } = string.Empty;
        public ReportOrientation Orientation { get; set; } = ReportOrientation.Portrait;
        public ReportHeaderConfig Header { get; set; } = new();
        public ReportFooterConfig Footer { get; set; } = new();
        public ReportStylingConfig Styling { get; set; } = new();
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    public class ReportHeaderConfig
    {
        public bool ShowCompanyInfo { get; set; } = true;
        public bool ShowLogo { get; set; } = true;
        public bool ShowReportTitle { get; set; } = true;
        public bool ShowGenerationInfo { get; set; } = true;
        public bool ShowFilters { get; set; } = true;
        public int Height { get; set; } = 120;
    }

    public class ReportFooterConfig
    {
        public bool ShowPageNumbers { get; set; } = true;
        public bool ShowGenerationInfo { get; set; } = true;
        public bool ShowReportTitle { get; set; } = true;
        public int Height { get; set; } = 30;
    }

    public class ReportStylingConfig
    {
        public int DefaultFontSize { get; set; } = 10;
        public string FontFamily { get; set; } = "Arial";
        public string HeaderColor { get; set; } = "#1e40af";
        public string BorderColor { get; set; } = "#e5e7eb";
        public int TableBorderWidth { get; set; } = 1;
        public bool AlternateRowColors { get; set; } = true;
    }
}
