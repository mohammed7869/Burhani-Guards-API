using Asp.Versioning;
using BurhaniGuards.Api.Services;
using BurhaniGuards.Api.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BurhaniGuards.Api.Controllers;

[Route("api/{v:apiVersion}/users")]
[ApiController]
[ApiVersion("1.0")]
[Authorize]
public class UserController : BaseController
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAll();
        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetById(id);
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] UserCreateViewModel viewmodel)
    {
        var id = await _userService.Add(viewmodel);
        return Ok(new { id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Edit(int id, [FromBody] UserEditViewModel viewmodel)
    {
        viewmodel.id = id;
        await _userService.Edit(viewmodel);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _userService.Delete(id);
        return Ok();
    }
}

