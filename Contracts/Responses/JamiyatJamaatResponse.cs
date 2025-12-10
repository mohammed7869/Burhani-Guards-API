namespace BurhaniGuards.Api.Contracts.Responses;

public sealed record JamiyatJamaatResponse(
    List<JamiyatItem> Jamiyats,
    List<JamaatItem> Jamaats
);

public sealed record JamiyatItem(
    string Name,
    int Count
);

public sealed record JamaatItem(
    string Name,
    int Count
);

