using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLifeOS.Application.Ports;
using ServiceLifeOS.Application.Services;
using ServiceLifeOS.Dtos.Dashboard;

namespace ServiceLifeOS.Api.Controllers;

[ApiController]
[Authorize]
[Route("dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboard;
    private readonly ICurrentUser _currentUser;

    public DashboardController(DashboardService dashboard, ICurrentUser currentUser)
    {
        _dashboard = dashboard;
        _currentUser = currentUser;
    }

    [HttpGet]
    public Task<DashboardResponseDto> Get(CancellationToken cancellationToken) => _dashboard.GetAsync(_currentUser.UserId, cancellationToken);
}
