using WorkerBookingSystem.Models;
using WorkerBookingSystem.Services;
using Xunit;

namespace WorkerBookingSystem.Tests;

public class CommunityProfilePolicyTests
{
    [Theory]
    [InlineData("Asha Devi", "AD")]
    [InlineData("worker", "WO")]
    [InlineData("", "CM")]
    public void Initials_are_bounded_and_handle_empty_names(string name, string expected)
    {
        Assert.Equal(expected, CommunityProfilePolicy.GetInitials(name));
    }

    [Fact]
    public void Display_name_prefers_profile_then_account_name_without_exposing_email_domain()
    {
        var user = new ApplicationUser { Email = "local.worker@example.test" };

        Assert.Equal("Asha Devi", CommunityProfilePolicy.GetDisplayName(user, "Asha Devi"));
        Assert.Equal("local.worker", CommunityProfilePolicy.GetDisplayName(user));
        Assert.Equal("Community member", CommunityProfilePolicy.GetDisplayName(null));
    }

    [Fact]
    public void Owners_and_admins_can_moderate_but_other_members_cannot()
    {
        var post = new CommunityPost { AuthorUserId = "author" };
        var comment = new CommunityComment { AuthorUserId = "commenter" };

        Assert.True(CommunityProfilePolicy.CanModerate(post, "author", false));
        Assert.True(CommunityProfilePolicy.CanModerate(post, "admin", true));
        Assert.False(CommunityProfilePolicy.CanModerate(post, "other", false));
        Assert.True(CommunityProfilePolicy.CanModerate(comment, "commenter", false));
        Assert.False(CommunityProfilePolicy.CanModerate(comment, "other", false));
    }
}
