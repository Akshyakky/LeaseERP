// LeaseERP.Infrastructure/DependencyInjection.cs (Updated)
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Core.Services.Reports.Contracts;
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

            // Report templates
            services.AddScoped<ContractSlipTemplate>();
            services.AddScoped<ContractListTemplate>();
            // Add more templates as needed:
            // services.AddScoped<TerminationSlipTemplate>();
            // services.AddScoped<InvoiceSlipTemplate>();
            // services.AddScoped<ReceiptSlipTemplate>();

            // Report data providers
            services.AddScoped<ContractSlipDataProvider>();
            services.AddScoped<ContractListDataProvider>();
            // Add more data providers as needed:
            // services.AddScoped<TerminationSlipDataProvider>();
            // services.AddScoped<InvoiceSlipDataProvider>();
            // services.AddScoped<ReceiptSlipDataProvider>();

            // Report components (headers, footers, etc.)
            services.AddScoped<IReportComponent, StandardHeaderComponent>();
            services.AddScoped<IReportComponent, StandardFooterComponent>();

            return services;
        }
    }
}