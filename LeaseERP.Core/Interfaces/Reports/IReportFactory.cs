// LeaseERP.Core/Interfaces/Reports/IReportFactory.cs
namespace LeaseERP.Core.Interfaces.Reports
{
    public interface IReportFactory
    {
        IReportTemplate CreateTemplate(string reportType);
        IReportDataProvider CreateDataProvider(string reportType);
        IEnumerable<IReportComponent> CreateComponents(string reportType);
    }
}