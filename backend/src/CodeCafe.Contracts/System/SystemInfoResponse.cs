namespace CodeCafe.Contracts.System;

public sealed record SystemInfoResponse(
    string Name,
    string Environment,
    DateTimeOffset ServerTimeUtc);
