using Kasio.Domain.Roles;
using Xunit;

namespace Kasio.UnitTests;

public sealed class RolePolicyTests
{
    [Theory]
    [InlineData(RoleNames.Admin, RoleNames.Owner, true)]
    [InlineData(RoleNames.Admin, RoleNames.Manager, false)]
    [InlineData(RoleNames.Admin, RoleNames.Renter, false)]
    [InlineData(RoleNames.Admin, RoleNames.Admin, false)]
    [InlineData(RoleNames.Owner, RoleNames.Manager, true)]
    [InlineData(RoleNames.Owner, RoleNames.Renter, true)]
    [InlineData(RoleNames.Owner, "custom_role", true)]
    [InlineData(RoleNames.Owner, RoleNames.Owner, false)]
    [InlineData(RoleNames.Owner, RoleNames.Admin, false)]
    [InlineData(RoleNames.Manager, RoleNames.Renter, false)]
    [InlineData(RoleNames.Renter, RoleNames.Renter, false)]
    public void CanCreateAccount_follows_hierarchy(string actorRole, string targetRole, bool expected) =>
        Assert.Equal(expected, RolePolicy.CanCreateAccount(actorRole, targetRole));

    [Theory]
    [InlineData(RoleNames.Owner, RoleNames.Manager, true)]
    [InlineData(RoleNames.Owner, RoleNames.Renter, true)]
    [InlineData(RoleNames.Owner, "custom_role", true)]
    [InlineData(RoleNames.Owner, RoleNames.Owner, false)]
    [InlineData(RoleNames.Owner, RoleNames.Admin, false)]
    [InlineData(RoleNames.Admin, RoleNames.Owner, true)]
    [InlineData(RoleNames.Admin, RoleNames.Admin, false)]
    [InlineData(RoleNames.Manager, RoleNames.Renter, false)]
    public void CanManageAccount_follows_hierarchy(string actorRole, string targetRole, bool expected) =>
        Assert.Equal(expected, RolePolicy.CanManageAccount(actorRole, targetRole));
}
