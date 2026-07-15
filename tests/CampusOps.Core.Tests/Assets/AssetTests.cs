using CampusOps.Core.Assets;
using CampusOps.Core.Common;
namespace CampusOps.Core.Tests.Assets;

public sealed class AssetTests
{
    [Fact] public void Lifecycle_AllowsServiceRoundTrip() { var asset = NewAsset(); asset.MarkOutOfService(); asset.ReturnToService(); Assert.Equal(AssetStatus.Active, asset.Status); }
    [Fact] public void RetiredAsset_CannotMove() { var asset = NewAsset(); asset.Retire(); Assert.Equal("asset.retired", Assert.Throws<DomainException>(() => asset.MoveTo(Guid.NewGuid())).Code); }
    private static Asset NewAsset() => new(Guid.NewGuid(), "hvac-1", "Air handler", "HVAC", Guid.NewGuid());
}
