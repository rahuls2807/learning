using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkerBookingSystem.Data;
using WorkerBookingSystem.Models;

namespace WorkerBookingSystem.Services;

public sealed class ProfileDisplayNameClaimsPrincipalFactory
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
{
    private readonly WorkerBookingContext _context;

    public ProfileDisplayNameClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IOptions<IdentityOptions> options,
        WorkerBookingContext context)
        : base(userManager, roleManager, options)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        var workerName = await _context.Workers.AsNoTracking()
            .Where(worker => worker.UserId == user.Id)
            .Select(worker => (worker.FirstName + " " + worker.LastName).Trim())
            .FirstOrDefaultAsync();
        var clientName = workerName == null
            ? await _context.Clients.AsNoTracking()
                .Where(client => client.UserId == user.Id)
                .Select(client => (client.FirstName + " " + client.LastName).Trim())
                .FirstOrDefaultAsync()
            : null;

        var roleName = workerName ?? clientName;
        var displayName = !string.IsNullOrWhiteSpace(roleName)
            ? roleName
            : !string.IsNullOrWhiteSpace(user.BioDescription)
                ? user.BioDescription.Trim()
                : CommunityProfilePolicy.GetDisplayName(user, workerName, clientName);
        identity.AddClaim(new Claim("display_name", displayName));
        return identity;
    }
}
