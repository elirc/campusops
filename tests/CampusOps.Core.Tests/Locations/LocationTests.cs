using CampusOps.Core.Common;
using CampusOps.Core.Locations;

namespace CampusOps.Core.Tests.Locations;

public sealed class LocationTests
{
    [Fact]
    public void Create_NormalizesRoomAndBuildsDisplayName()
    {
        var location = new Location(Guid.NewGuid(), "North", "Science", " b-12 ");
        Assert.Equal("B-12", location.Room);
        Assert.Equal("North / Science / B-12", location.DisplayName);
    }

    [Fact]
    public void Create_MissingDetail_RejectsRule() =>
        Assert.Equal("location.details_required", Assert.Throws<DomainException>(() =>
            new Location(Guid.NewGuid(), "North", "", "B-12")).Code);
}
