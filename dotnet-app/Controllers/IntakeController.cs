using AccordIntakeApi.Dtos;
using AccordIntakeApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccordIntakeApi.Controllers;

[ApiController]
public sealed class IntakeController(AcordXmlIntakeService intakeService) : ControllerBase
{
    // Dependency Injection also works with this commented code

    // private readonly AcordXmlIntakeService _intakeService;
    // private readonly PostgresIntakeRepository _repository;

    // public IntakeController(AcordXmlIntakeService intakeService, PostgresIntakeRepository repository)
    // {
    //     _intakeService = intakeService;
    //     _repository = repository;
    // }

    [HttpGet("/health")]
    public IActionResult Health()
    {
        return Ok(new { status = "OK! App started successfully!" });
    }

    [HttpPost("/intake/xml")]
    [Consumes("application/xml")]
    public async Task<IActionResult> ProcessXml(CancellationToken cancellationToken)
    {
        if (Request.ContentType is null || !Request.ContentType.Contains("xml", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Send the request with Content-Type: application/xml." });

        try
        {
            var result = await intakeService.ProcessAsync(Request.Body, cancellationToken);
            return Ok(result);
        }
        catch (FormatException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("/intake/records")]
    public IActionResult GetRecords()
    {
        return Ok(intakeService.GetAllRecords());
    }
    
    [HttpPut("/intake/records/{id:long}")]
    public async Task<IActionResult> UpdateRecord(
        long id,
        ApsIncomingEntity entity,
        CancellationToken cancellationToken)
    {
        var updated = await intakeService.UpdateRecordAsync(id, entity, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("/intake/records/{id:long}")]
    public async Task<IActionResult> DeleteRecord(long id, CancellationToken cancellationToken)
    {
        var deleted = await intakeService.DeleteRecordAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
