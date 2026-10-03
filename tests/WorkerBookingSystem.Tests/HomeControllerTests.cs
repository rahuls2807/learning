using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WorkerBookingSystem.Controllers;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class HomeControllerTests
{
    [Fact]
    public void Home_index_renders_home_instead_of_redirecting_to_a_role_workspace()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance);

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Theory]
    [InlineData("Worker")]
    [InlineData("Client")]
    [InlineData("Admin")]
    public void Authenticated_roles_can_open_home(string role)
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.Role, role) }, "test"))
                }
            }
        };

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }
}
