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
}
