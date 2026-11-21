using GentleHealersApi.DTOs;
using GentleHealersApi.Extensions;
using GentleHealersApi.Services;
using GentleHealersApi.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace GentleHealersApi.Controllers;

[ApiController]
[Route("api/treatment-codes")]
[Authorize]
public sealed class TreatmentCodesController(
    ITreatmentCodeImportService importService,
    ITreatmentCodeRepository repository,
    ILogger<TreatmentCodesController>? logger = null) : ControllerBase
{
    [HttpPost("/api/admin/treatment-codes/import")]
    [Authorize(Roles = "SystemAdministrator")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(TreatmentCodeImportService.MaximumFileSize + 1_048_576)]
    public async Task<ActionResult<TreatmentCodeImportResult>> Import([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        try
        {
            var result = await importService.ImportAsync(file, User.GetUserIdFromClaims(), cancellationToken);
            return Ok(result);
        }
        catch (TreatmentCodeImportException exception)
        {
            return BadRequest(new TreatmentCodeImportError(exception.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            logger?.LogError(exception, "Treatment-code import could not be stored.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new TreatmentCodeImportError("Het bestand kon niet worden opgeslagen. Probeer het later opnieuw."));
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TreatmentCodeDto>>> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await repository.GetActiveAsync(User.GetUserIdFromClaims(), cancellationToken));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception exception) when (exception is SqlException or InvalidOperationException)
        {
            logger?.LogError(exception, "Active treatment codes could not be read.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new TreatmentCodeImportError("De behandelcodes kunnen niet worden opgehaald. Probeer het later opnieuw."));
        }
    }
}
