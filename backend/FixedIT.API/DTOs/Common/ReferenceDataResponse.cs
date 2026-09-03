namespace FixedIT.API.DTOs.Common;

public sealed record ReferenceOptionResponse(int Id, string Name);

public sealed record CityOptionResponse(int Id, string Name, int CountryId);

public sealed record CountryOptionResponse(int Id, string Name, string Code);

public sealed record ReservationStatusOptionResponse(int Id, string Name, string Description);

public sealed record ReferenceDataResponse(
    IReadOnlyCollection<CityOptionResponse> Cities,
    IReadOnlyCollection<ReferenceOptionResponse> Categories,
    IReadOnlyCollection<CountryOptionResponse> Countries,
    IReadOnlyCollection<ReservationStatusOptionResponse> ReservationStatuses);
