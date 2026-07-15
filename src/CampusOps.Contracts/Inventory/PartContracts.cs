namespace CampusOps.Contracts.Inventory;

public sealed record CreatePartRequest(string Sku, string Name, int OnHand);
public sealed record ReservePartRequest(Guid PartId, int Quantity);
public sealed record PartResponse(Guid Id, string Sku, string Name, int OnHand, int Reserved, int Available);
