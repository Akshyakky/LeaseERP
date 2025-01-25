using LeaseERP.Core.Interfaces;
using LeaseERP.Shared.DTOs;
using LeaseERP.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LeaseERP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MasterController : ControllerBase
    {
        private readonly IDataService _dataService;
        private readonly ILogger<MasterController> _logger;
        private readonly IReadOnlyDictionary<string, string> _procedureMap;
        private readonly IConfiguration _configuration;

        public MasterController(IDataService dataService, ILogger<MasterController> logger, IConfiguration configuration)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _procedureMap = LoadProcedureMap();
        }

        /// <summary>
        /// Executes a stored procedure operation for the specified entity.
        /// </summary>
        /// <param name="entity">The entity type to perform the operation on</param>
        /// <param name="request">The operation request details</param>
        /// <returns>Operation result</returns>
        /// <response code="200">Operation completed successfully</response>
        /// <response code="400">Invalid request or validation error</response>
        /// <response code="404">Entity not found</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("{entity}")]
        [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<dynamic>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExecuteOperation([Required][RegularExpression("^[a-zA-Z0-9]+$")] string entity, [FromBody] BaseRequest request)
        {
            try
            {
                _logger.LogInformation("Executing operation for entity: {Entity}", entity);

                if (request == null)
                {
                    return BadRequest(new ApiResponse<dynamic>
                    {
                        Success = false,
                        Message = "Request body cannot be null"
                    });
                }

                var validationResult = ValidateRequest(request);
                if (!validationResult.Success)
                {
                    return BadRequest(validationResult);
                }

                if (!_procedureMap.TryGetValue(entity.ToLower(), out string procedureName))
                {
                    _logger.LogWarning("Unknown entity requested: {Entity}", entity);
                    return NotFound(new ApiResponse<dynamic>
                    {
                        Success = false,
                        Message = $"Entity '{entity}' not found"
                    });
                }

                var result = await _dataService.ExecuteStoredProcedureAsync(procedureName, request);

                if (!result.Success)
                {
                    _logger.LogWarning("Operation failed for entity {Entity}: {Message}",
                        entity, result.Message);
                }

                return result.Success ? Ok(result) : BadRequest(result);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request cancelled for entity: {Entity}", entity);
                return StatusCode(StatusCodes.Status408RequestTimeout, new ApiResponse<dynamic>
                {
                    Success = false,
                    Message = "The request was cancelled"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request for entity {Entity}", entity);
                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse<dynamic>
                {
                    Success = false,
                    Message = "An internal server error occurred",
                    Errors = new List<string>
                    {
                        _configuration.GetValue<bool>("AppSettings:ShowDetailedErrors")
                            ? ex.Message
                            : "Please contact support if the problem persists"
                    }
                });
            }
        }

        private IReadOnlyDictionary<string, string> LoadProcedureMap()
        {
            try
            {
                var procedureMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                _configuration.GetSection("StoredProcedures").Bind(procedureMap);

                if (procedureMap.Count == 0)
                {
                    _logger.LogWarning("No stored procedure mappings found in configuration");
                    throw new InvalidOperationException("Stored procedure mappings not configured");
                }

                return procedureMap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading procedure mappings");
                throw new InvalidOperationException("Failed to load stored procedure mappings", ex);
            }
        }

        private ApiResponse<dynamic> ValidateRequest(BaseRequest request)
        {
            var errors = new List<string>();

            if (request.Mode == OperationType.None)
            {
                errors.Add("Invalid operation type");
            }

            // Parameters validation based on Mode
            if (request.Parameters == null)
            {
                errors.Add("Parameters cannot be null");
            }
            else
            {
                switch (request.Mode)
                {
                    case OperationType.Insert:
                        if (!request.Parameters.Any()) errors.Add("Parameters are required for Insert operation");
                        if (string.IsNullOrWhiteSpace(request.ActionBy)) errors.Add("ActionBy is required for Insert operation");
                        break;

                    case OperationType.Update:
                        if (!request.Parameters.Any()) errors.Add("Parameters are required for Update operation");
                        if (string.IsNullOrWhiteSpace(request.ActionBy)) errors.Add("ActionBy is required for Update operation");
                        break;

                    case OperationType.Delete:
                        if (!request.Parameters.ContainsKey("UserID")) errors.Add("UserID is required for Delete operation");
                        if (string.IsNullOrWhiteSpace(request.ActionBy)) errors.Add("ActionBy is required for Delete operation");
                        break;

                    case OperationType.FetchById:
                        if (!request.Parameters.ContainsKey("UserID")) errors.Add("UserID is required for FetchById operation");
                        break;

                    case OperationType.Search:
                        // Search can have empty parameters for fetching all records
                        break;

                    case OperationType.FetchAll:
                        // FetchAll doesn't require parameters
                        break;
                }
            }

            if (request.Parameters?.Any() == true && !request.Parameters.All(p => !string.IsNullOrWhiteSpace(p.Key)))
            {
                errors.Add("Parameter keys cannot be null or empty");
            }

            return errors.Any()
                ? new ApiResponse<dynamic>
                {
                    Success = false,
                    Message = "Validation failed",
                    Errors = errors
                }
                : new ApiResponse<dynamic> { Success = true };
        }
    }
}