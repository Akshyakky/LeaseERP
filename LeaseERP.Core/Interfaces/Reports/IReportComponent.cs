// LeaseERP.Core/Interfaces/Reports/IReportComponent.cs
using QuestPDF.Infrastructure;

namespace LeaseERP.Core.Interfaces.Reports
{
    public interface IReportComponent
    {
        void Render(IContainer container, ReportData data, ReportConfiguration config);
        string ComponentName { get; }
        bool IsRequired { get; }
    }

    public interface IReportHeaderComponent : IReportComponent
    {
    }

    public interface IReportFooterComponent : IReportComponent
    {
    }

    public interface IReportContentComponent : IReportComponent
    {
    }
}