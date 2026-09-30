using CampusOps.Core.Common;
using CampusOps.Core.Inventory;

namespace CampusOps.Core.Tests.Inventory;

public sealed class StockCapacityTests
{
    [Fact]
    public void Receive_OverflowIsRejectedWithoutMutation()
    {
        var part = new Part(Guid.NewGuid(), "MAX", "Capacity example", int.MaxValue);
        var error = Assert.Throws<DomainException>(() => part.Receive(1));
        Assert.Equal("part.quantity_overflow", error.Code);
        Assert.Equal(int.MaxValue, part.OnHand);
        Assert.Equal(0, part.Reserved);
    }
}
