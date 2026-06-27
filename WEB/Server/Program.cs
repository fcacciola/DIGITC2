using DigitC2.Server.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var uploadLimit = builder.Configuration.GetValue<long?>("DigitC2:MaxUploadBytes") ?? 100 * 1024 * 1024;
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = uploadLimit;
});
builder.Services.Configure<ProcessingWorkspaceOptions>(builder.Configuration.GetSection("DigitC2"));
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = uploadLimit;
});
builder.Services.AddSingleton<EngineJobService>();
builder.Services.AddSingleton<ResultCatalogService>();
builder.Services.AddHostedService<JobCleanupService>();

var app = builder.Build();
const string AuthCookieName = "digitc2_auth";

app.Use(async (context, next) =>
{
    var sharedPassword = app.Configuration["DigitC2:SharedPassword"];
    if (string.IsNullOrWhiteSpace(sharedPassword) || IsAuthExempt(context.Request))
    {
        await next();
        return;
    }

    var expectedToken = ComputeAuthToken(sharedPassword);
    if (context.Request.Cookies.TryGetValue(AuthCookieName, out var token)
        && CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(token), Encoding.UTF8.GetBytes(expectedToken)))
    {
        await next();
        return;
    }

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { error = "Password required." });
        return;
    }

    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(GetLoginPage());
});

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/login", () => Results.Content(GetLoginPage(), "text/html"));

app.MapPost("/login", async (HttpContext context) =>
{
    var sharedPassword = app.Configuration["DigitC2:SharedPassword"];
    if (string.IsNullOrWhiteSpace(sharedPassword))
    {
        return Results.Redirect("/");
    }

    var form = await context.Request.ReadFormAsync();
    var password = form["password"].ToString();
    if (!CryptographicOperations.FixedTimeEquals(
        Encoding.UTF8.GetBytes(password),
        Encoding.UTF8.GetBytes(sharedPassword)))
    {
        return Results.Content(GetLoginPage("Incorrect password."), "text/html", statusCode: StatusCodes.Status401Unauthorized);
    }

    context.Response.Cookies.Append(
        AuthCookieName,
        ComputeAuthToken(sharedPassword),
        new CookieOptions
        {
            HttpOnly = true,
            Secure = !app.Environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(30)
        });

    return Results.Redirect("/");
}).DisableAntiforgery();

app.MapPost("/logout", (HttpContext context) =>
{
    context.Response.Cookies.Delete(AuthCookieName);
    return Results.Redirect("/login");
});

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
            : JsonSerializer.Deserialize<IReadOnlyList<DigitC2.Server.Models.ConfigParamDto>>(config, jsonOptions);
        var result = await jobs.ProcessAsync(file, name, configParams, cancellationToken);
        return Results.Created(
            $"/api/jobs/{result.JobId}/result",
            new DigitC2.Server.Models.ProcessJobResponse(result.JobId, result.SessionName, "completed", $"/api/jobs/{result.JobId}/result"));
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (JsonException ex)
    {
        app.Logger.LogWarning(ex, "Invalid processing configuration JSON.");
        return Results.BadRequest(new { error = "Processing configuration could not be read." });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Processing failed unexpectedly.");
        return Results.Problem(
            title: "Processing failed.",
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError);
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

app.MapFallbackToFile("index.html");

app.Run();

static bool IsAuthExempt(HttpRequest request)
{
    return request.Path.StartsWithSegments("/login")
        || request.Path.StartsWithSegments("/health")
        || request.Path.StartsWithSegments("/favicon.ico");
}

static string ComputeAuthToken(string sharedPassword)
{
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"DigitC2:{sharedPassword}"));
    return Convert.ToHexString(hash);
}

static string GetLoginPage(string? error = null)
{
    var errorHtml = string.IsNullOrWhiteSpace(error)
        ? ""
        : $"""<div class="error">{System.Net.WebUtility.HtmlEncode(error)}</div>""";

    return $$"""
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Transgraphier 2.4.1</title>
  <style>
    :root { font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif; color: #1f2937; background: #f4f6f8; }
    body { margin: 0; min-height: 100vh; display: grid; place-items: center; }
    main { width: min(360px, calc(100vw - 32px)); border: 1px solid #d1d9e2; border-radius: 6px; background: #fff; padding: 18px; }
    h1 { margin: 0 0 4px; font-size: 20px; letter-spacing: 0; }
    p { margin: 0 0 16px; color: #64748b; font-size: 12px; }
    form { display: grid; gap: 10px; }
    input, button { min-height: 36px; border-radius: 6px; font: inherit; }
    input { border: 1px solid #b8c3cf; padding: 0 10px; }
    button { border: 1px solid #1f2937; background: #1f2937; color: #fff; cursor: pointer; }
    .error { margin-bottom: 10px; border: 1px solid #fca5a5; border-radius: 6px; background: #fff5f5; color: #991b1b; padding: 8px 10px; font-size: 13px; }
  </style>
</head>
<body>
  <main>
    <h1>Transgraphier 2.4.1</h1>
    <p>Digital Instrumental Trans-Communication (ITC) workbench</p>
    {{errorHtml}}
    <form method="post" action="/login">
      <input type="password" name="password" autocomplete="current-password" autofocus placeholder="Shared password">
      <button type="submit">Enter</button>
    </form>
  </main>
</body>
</html>
""";
}
