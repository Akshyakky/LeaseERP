// LeaseERP.Core/Services/Reports/Termination/TerminationSlipDataProvider.cs
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeaseERP.Core.Services.Reports.Termination
{
    public class TerminationSlipDataProvider : BaseReportDataProvider
    {
        public TerminationSlipDataProvider(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<TerminationSlipDataProvider> logger)
            : base(dataService, configuration, logger)
        {
        }

        public override string GetStoredProcedureName()
        {
            return _configuration["StoredProcedures:contracttermination"];
        }

        public override Dictionary<string, object> BuildParameters(ReportRequest request)
        {
            var parameters = GetBaseParameters(request);

            // Use FetchById mode to get termination details with related data
            parameters.Add("@Mode", (int)OperationType.FetchById);

            if (request.Parameters.TryGetValue("TerminationId", out var terminationId))
            {
                parameters.Add("@TerminationID", Convert.ToInt64(terminationId));
            }
            else
            {
                throw new ArgumentException("TerminationId parameter is required for termination slip generation");
            }

            return parameters;
        }

        protected override async Task EnrichWithMetadata(ReportData reportData, ReportRequest request)
        {
            await base.EnrichWithMetadata(reportData, request);

            // Add termination slip specific metadata
            reportData.Metadata["ReportTitle"] = "Contract Termination Slip";
            reportData.Metadata["ReportType"] = "termination-slip";

            // Add termination ID to metadata
            if (request.Parameters.TryGetValue("TerminationId", out var terminationId))
            {
                reportData.Metadata["TerminationID"] = terminationId;
            }
        }
    }
}