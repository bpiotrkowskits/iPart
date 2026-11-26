using iPart.Core.Components;
using iPart.Core.Packages;

namespace iPart.Tests;

public sealed class MountingCategoriesTests
{
    [Theory]
    [InlineData("THT")]
    [InlineData("tht")]
    [InlineData("Press-Fit")]
    [InlineData("PressFit")]
    [InlineData("Mech")]
    [InlineData("MECH")]
    [InlineData(" THT ")]
    [InlineData("Plugs")]
    public void NonSmtMountingIsRecognisedInEverySpelling(string mounting) =>
        Assert.True(MountingCategories.IsNonSmt(mounting));

    [Theory]
    [InlineData("SMT")]
    [InlineData("Pin-In-Paste")]
    [InlineData("DNP")]
    [InlineData("")]
    [InlineData(null)]
    public void OtherMountingIsNotNonSmt(string? mounting) =>
        Assert.False(MountingCategories.IsNonSmt(mounting));

    [Fact]
    public void ComponentListItemExposesTheRuleForTheView()
    {
        Assert.True(new ComponentListItem("X", "Press-Fit", null, null, null).IsNonSmtMounting);
        Assert.False(new ComponentListItem("X", "SMT", null, null, null).IsNonSmtMounting);
    }
}
