using Microsoft.EntityFrameworkCore;
using PulseBoard.Application.Common.Exceptions;
using PulseBoard.Domain.Entities;
using PulseBoard.Domain.Enums;
using PulseBoard.Infrastructure.Auth;
using PulseBoard.Infrastructure.Data;

namespace PulseBoard.Tests;

public class DashboardAuthorizationTests
{
    private static PulseBoardDbContext NewDb() =>
        new(new DbContextOptionsBuilder<PulseBoardDbContext>()
            .UseInMemoryDatabase($"auth-{Guid.NewGuid()}")
            .Options);

    private static async Task<(PulseBoardDbContext db, User admin, User editor, User outsider, Dashboard dash)> SeedAsync()
    {
        var db = NewDb();
        var admin = new User { Email = "admin@x.io", DisplayName = "A", Role = AppRole.Admin };
        var editor = new User { Email = "editor@x.io", DisplayName = "E", Role = AppRole.Member };
        var outsider = new User { Email = "out@x.io", DisplayName = "O", Role = AppRole.Member };
        var owner = new User { Email = "owner@x.io", DisplayName = "Ow", Role = AppRole.Member };
        var dataset = new Dataset { Name = "d", Slug = "d", OwnerId = owner.Id };
        var dash = new Dashboard { Name = "dash", Slug = "dash", DatasetId = dataset.Id, OwnerId = owner.Id };
        db.AddRange(admin, editor, outsider, owner, dataset, dash);
        db.DashboardMembers.Add(new DashboardMember { DashboardId = dash.Id, UserId = editor.Id, Role = DashboardRole.Editor });
        await db.SaveChangesAsync();
        return (db, admin, editor, outsider, dash);
    }

    [Fact]
    public async Task AppAdmin_IsTreatedAsOwner_OfAnyDashboard()
    {
        var (db, admin, _, _, dash) = await SeedAsync();
        var sut = new DashboardAuthorizationService(db);

        Assert.Equal(DashboardRole.Owner, await sut.GetEffectiveRoleAsync(dash.Id, admin.Id));
    }

    [Fact]
    public async Task Member_GetsRoleFromMembership()
    {
        var (db, _, editor, _, dash) = await SeedAsync();
        var sut = new DashboardAuthorizationService(db);

        Assert.Equal(DashboardRole.Editor, await sut.GetEffectiveRoleAsync(dash.Id, editor.Id));
    }

    [Fact]
    public async Task NonMember_HasNoEffectiveRole()
    {
        var (db, _, _, outsider, dash) = await SeedAsync();
        var sut = new DashboardAuthorizationService(db);

        Assert.Null(await sut.GetEffectiveRoleAsync(dash.Id, outsider.Id));
    }

    [Fact]
    public async Task Authorize_AllowsWhenRoleMeetsMinimum()
    {
        var (db, _, editor, _, dash) = await SeedAsync();
        var sut = new DashboardAuthorizationService(db);

        await sut.AuthorizeAsync(dash.Id, editor.Id, DashboardRole.Viewer); // Editor >= Viewer
        await sut.AuthorizeAsync(dash.Id, editor.Id, DashboardRole.Editor);
    }

    [Fact]
    public async Task Authorize_ThrowsForbidden_WhenRoleBelowMinimum()
    {
        var (db, _, editor, _, dash) = await SeedAsync();
        var sut = new DashboardAuthorizationService(db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.AuthorizeAsync(dash.Id, editor.Id, DashboardRole.Owner)); // Editor < Owner
    }

    [Fact]
    public async Task Authorize_ThrowsForbidden_ForNonMember()
    {
        var (db, _, _, outsider, dash) = await SeedAsync();
        var sut = new DashboardAuthorizationService(db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => sut.AuthorizeAsync(dash.Id, outsider.Id, DashboardRole.Viewer));
    }
}
