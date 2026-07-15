using CampusOps.Core.Procurement;
namespace CampusOps.Core.Tests.Procurement;

public sealed class PurchaseRequestTests
{
    [Theory][InlineData(999, PurchaseRequestStatus.Approved)][InlineData(1001, PurchaseRequestStatus.PendingApproval)] public void Submit_UsesApprovalThreshold(decimal amount, PurchaseRequestStatus expected) { var quote = new VendorQuote(Guid.NewGuid(), Guid.NewGuid(), "Q1", amount, DateTimeOffset.UtcNow.AddDays(10)); var request = new PurchaseRequest(Guid.NewGuid(), Guid.NewGuid(), quote, "Replacement part"); request.Submit(1000); Assert.Equal(expected, request.Status); }
}
