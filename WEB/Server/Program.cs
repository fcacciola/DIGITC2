using DigitC2.Server.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ProcessingWorkspaceOptions>(builder.Configuration.GetSection("DigitC2"));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100 * 1024 * 1024;
});
builder.Services.AddSingleton<EngineJobService>();
builder.Services.AddSingleton<ResultCatalogService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/config", (EngineJobService jobs) => Results.Ok(jobs.GetDefaultConfigParams()));

app.MapPost("/api/jobs", async (
    [FromForm] IFormFile file,
    [FromForm] string? name,
    [FromForm] string? config,
    EngineJobService jobs,
    CancellationToken cancellationToken) =>
{
    try
    {
        var configParams = string.IsNullOrWhiteSpace(config)
            ? null
            : JsonSerializer.Deserialize<IReadOnlyList<DigitC2.Server.Models.ConfigParamDto>>(config);
        var result = await jobs.ProcessAsync(file, name, configParams, cancellationToken);
        return Results.Created(
            $"/api/jobs/{result.JobId}/result",
            new DigitC2.Server.Models.ProcessJobResponse(result.JobId, result.SessionName, "completed", $"/api/jobs/{result.JobId}/result"));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
}).DisableAntiforgery();

app.MapGet("/api/jobs/{jobId}/result", (string jobId, ResultCatalogService catalog, HttpRequest request) =>
{
    try
    {
        return Results.Ok(catalog.GetManifest(jobId, request));
    }
    catch (DirectoryNotFoundException)
    {
        return Results.NotFound(new { error = "Result job was not found." });
    }
});

app.MapGet("/api/jobs/{jobId}/files/{**relativePath}", (string jobId, string relativePath, ResultCatalogService catalog) =>
{
    try
    {
        var file = catalog.GetFile(jobId, relativePath);
        return Results.File(file.Path, file.ContentType, file.DownloadName);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound(new { error = "Result file was not found." });
    }
    catch (DirectoryNotFoundException)
    {
        return Results.NotFound(new { error = "Result job was not found." });
    }
});

app.Run();
