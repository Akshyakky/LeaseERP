// LeaseERP.Core/Services/Reports/Contracts/ContractSlipDataProvider.cs
using LeaseERP.Core.Interfaces;
using LeaseERP.Core.Interfaces.Reports;
using LeaseERP.Core.Services.Reports;
using LeaseERP.Shared.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeaseERP.Core.Services.Reports.Contracts
{
    public class ContractSlipDataProvider : BaseReportDataProvider
    {
        public ContractSlipDataProvider(
            IDataService dataService,
            IConfiguration configuration,
            ILogger<ContractSlipDataProvider> logger)
            : base(dataService, configuration, logger)
        {
        }

        public override string GetStoredProcedureName()
        {
            return _configuration["StoredProcedures:contractmanagement"];
        }

        public override Dictionary<string, object> BuildParameters(ReportRequest request)
        {
            var parameters = GetBaseParameters(request);

            parameters.Add("@Mode", (int)OperationType.FetchById);

            if (request.Parameters.TryGetValue("ContractId", out var contractId))
            {
                parameters.Add("@ContractID", Convert.ToInt64(contractId));
            }

            return parameters;
        }
    }
}