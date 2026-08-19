namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record JamiyatJamaatResponse(
    List<JamiyatItem> Jamiyats,
    List<JamaatItem> Jamaats
);

public sealed record JamiyatItem(
    int Id,
    string Name,
    int Count
);

public sealed record JamaatItem(
    int Id,
    string Name,
    int Count,
    int JamiyatId
);

