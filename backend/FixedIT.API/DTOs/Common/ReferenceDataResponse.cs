namespace FixedIT.API.DTOs.Common;

public sealed record ReferenceOptionResponse(int Id, string Name);

public sealed record ReferenceDataResponse(
    IReadOnlyCollection<ReferenceOptionResponse> Cities,
    IReadOnlyCollection<ReferenceOptionResponse> Categories);
