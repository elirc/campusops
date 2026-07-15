namespace CampusOps.Contracts.Locations;

public sealed record CreateLocationRequest(string Campus, string Building, string Room);
public sealed record LocationResponse(Guid Id, string Campus, string Building, string Room, string DisplayName);
