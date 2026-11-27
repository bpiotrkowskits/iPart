using iPart.Core.Shared;

namespace iPart.Tests;

public sealed class ReferenceCopiesTests
{
    [Fact]
    public void UnknownDepartmentIsShownByNumber()
    {
        var dictionary = new Dictionary<int, Department> { [100102] = new() { Id = 100102, Code = "THT", Name = "THT", IsActive = true } };
        Assert.Equal("THT", Department.Describe(100102, dictionary));
        Assert.Equal("dział 100110 (nieznany)", Department.Describe(100110, dictionary));
        Assert.Equal("bez działu", Department.Describe(null, dictionary));
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    public void OnlyActiveAndNotDeletedAccountsAreSelectable(bool active, bool deleted, bool selectable) =>
        Assert.Equal(selectable, new ProduManAccount { IsActive = active, Deleted = deleted }.Selectable);
}
