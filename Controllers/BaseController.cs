using BurhaniGuards.Api.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

public class BaseController : ControllerBase
{
    public CurrentUserViewModel? CurrentUser => HttpContext.Items["User"] as CurrentUserViewModel;
}

