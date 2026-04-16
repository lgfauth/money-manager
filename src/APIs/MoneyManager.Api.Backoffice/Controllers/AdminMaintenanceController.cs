using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyManager.Api.Administration.Services;

namespace MoneyManager.Api.Administration.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
public sealed class AdminMaintenanceController : ControllerBase
{
    public AdminMaintenanceController(AdminAuditService auditService)
    {
    }
}
