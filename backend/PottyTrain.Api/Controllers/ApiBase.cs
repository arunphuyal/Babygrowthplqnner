using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace PottyTrain.Api.Controllers;

[ApiController]
public abstract class ApiBase : ControllerBase
{
    protected Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
