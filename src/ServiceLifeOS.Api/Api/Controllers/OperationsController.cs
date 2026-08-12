using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Operations;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("operations")]
public sealed class OperationsController : ControllerBase
{
    private readonly OperationsService _operationsService;
    private readonly ICurrentUser _currentUser;

    public OperationsController(OperationsService operationsService, ICurrentUser currentUser)
    {
        _operationsService = operationsService;
        _currentUser = currentUser;
    }

    [HttpGet("audit-logs")]
    public async Task<ActionResult<PagedAuditLogResponseDto>> GetAuditLogs(
        [FromQuery] AuditLogQueryDto query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _operationsService.GetAuditLogsAsync(
                _currentUser.UserId,
                query,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
