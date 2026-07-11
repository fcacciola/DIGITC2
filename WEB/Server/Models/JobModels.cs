namespace DigitC2.Server.Models;

public sealed record ProcessJobResponse(
    string JobId,
    string SessionName,
    string Status,
    string ResultUrl);

public sealed record ResultManifest(
    string JobId,
    string SessionName,
    string Status,
    DateTimeOffset CreatedAt,
    string WinningBranchName,
    int BranchCount,
    IReadOnlyList<ResultFileNode> Files,
    IReadOnlyList<ConfigParamDto> ConfigParams,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Errors);

public sealed record ResultFileNode(
    string Name,
    string RelativePath,
    string Kind,
    long? Size,
    string? ContentType,
    string? Url,
    IReadOnlyList<ResultFileNode>? Children);

public sealed record EngineRunResult(
    string JobId,
    string SessionName,
    string OutputFolder,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Errors);

public sealed record JobMetadata(
    string JobId,
    string SessionName,
    string Status,
    string OutputFolder,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Errors);

public sealed record ConfigParamDto(
    string Section,
    string Key,
    string Name,
    string Value,
    string Label);
