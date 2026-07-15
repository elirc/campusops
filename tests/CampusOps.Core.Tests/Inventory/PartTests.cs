using CampusOps.Core.Common;
using CampusOps.Core.Inventory;
namespace CampusOps.Core.Tests.Inventory;

public sealed class PartTests
{
    [Fact] public void ReserveAndConsume_KeepQuantitiesConsistent() { var part = new Part(Guid.NewGuid(), "FLT-1", "Filter", 10); part.Reserve(4); part.Consume(3); Assert.Equal(7, part.OnHand); Assert.Equal(1, part.Reserved); Assert.Equal(6, part.Available); }
    [Fact] public void Reserve_AboveAvailable_IsRejected() { var part = new Part(Guid.NewGuid(), "FLT-1", "Filter", 2); Assert.Equal("part.stock_insufficient", Assert.Throws<DomainException>(() => part.Reserve(3)).Code); }
}
