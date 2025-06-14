// LeaseERP.Infrastructure/DependencyInjection.cs
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Core.Services.Reports.Components;
using LeaseERP.Core.Services.Reports.Contracts;
using LeaseERP.Core.Services.Reports.Invoices;
using LeaseERP.Core.Services.Reports.Termination;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;
using System.Data;

namespace LeaseERP.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            // Database connection
            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                connection.Open();
                return connection;
            });

            // Core services
            services.AddScoped<IDataService, DataService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Report framework services
            services.AddScoped<IReportEngine, ReportEngine>();
            services.AddScoped<IReportFactory, ReportFactory>();
            services.AddScoped<IPdfService, PdfService>();

            // Generic data provider factory (handles most reports automatically)
            services.AddScoped<GenericReportDataProviderFactory>();

            // Generic template for simple reports
            services.AddTransient<GenericReportTemplate>();

            // Custom templates (only for complex reports that need special formatting)
            services.AddScoped<ContractSlipTemplate>();
            services.AddScoped<ContractListTemplate>();
            services.AddScoped<TerminationSlipTemplate>();
            services.AddScoped<InvoiceSlipTemplate>();
            services.AddScoped<InvoiceListTemplate>();

            // Add more custom templates only when needed:
            // services.AddScoped<ComplexFinancialDashboardTemplate>();
            // services.AddScoped<DetailedContractAnalysisTemplate>();

            // Custom data providers (only for reports with complex business logic)
            // Most reports will use the GenericReportDataProvider automatically
            // Only add custom ones when you need special parameter handling or business logic:
            // services.AddScoped<ComplexContractAnalysisDataProvider>();
            // services.AddScoped<FinancialDashboardDataProvider>();

            // Report components (headers, footers, etc.)
            services.AddScoped<IReportComponent, StandardHeaderComponent>();
            services.AddScoped<IReportComponent, StandardFooterComponent>();
            services.AddScoped<StandardHeaderComponent>();
            services.AddScoped<StandardFooterComponent>();

            return services;
        }
    }
}