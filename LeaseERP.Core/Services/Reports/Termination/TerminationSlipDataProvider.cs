// LeaseERP.Core/Services/Reports/Termination/TerminationSlipDataProvider.cs
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
            parameters.Add("@Mode", (int)OperationType.FetchById);

            if (request.Parameters.TryGetValue("TerminationId", out var terminationId))
            {
                try
                {
                    var terminationIdValue = terminationId is JsonElement jsonElement
                        ? jsonElement.GetInt64()
                        : Convert.ToInt64(terminationId);

                    parameters.Add("@TerminationID", terminationIdValue);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert TerminationId parameter to Int64");
                    throw new ArgumentException("Invalid TerminationId parameter format", nameof(request));
                }
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

            reportData.Metadata["ReportTitle"] = "Contract Termination Slip";
            reportData.Metadata["ReportType"] = "termination-slip";

            if (request.Parameters.TryGetValue("TerminationId", out var terminationId))
            {
                try
                {
                    var terminationIdValue = terminationId is JsonElement jsonElement
                        ? jsonElement.GetInt64()
                        : Convert.ToInt64(terminationId);

                    reportData.Metadata["TerminationID"] = terminationIdValue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to convert TerminationId for metadata");
                    // Continue processing without adding the TerminationID to metadata
                }
            }
        }
    }
}