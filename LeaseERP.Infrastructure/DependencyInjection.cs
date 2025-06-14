using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Services;
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
            // Configure QuestPDF license - Community license is free for open-source and personal projects
            QuestPDF.Settings.License = LicenseType.Community;

            services.AddScoped<IDbConnection>(sp =>
            {
                var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
                connection.Open();
                return connection;
            });

            services.AddScoped<IDataService, DataService>();
            services.AddSingleton<IEncryptionService, EncryptionService>();
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IPdfService, PdfService>();

            return services;
        }
    }
}
