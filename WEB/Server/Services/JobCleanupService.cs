using Microsoft.Extensions.Options;

namespace DigitC2.Server.Services;

public sealed class JobCleanupService : BackgroundService
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<JobCleanupService> _logger;
    private readonly ProcessingWorkspaceOptions _options;

    public JobCleanupService(
        IWebHostEnvironment environment,
        IOptions<ProcessingWorkspaceOptions> options,
        ILogger<JobCleanupService> logger)
    {
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupOnce(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(5, _options.CleanupIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CleanupOnce(stoppingToken);
        }
    }

    private Task CleanupOnce(CancellationToken cancellationToken)
    {
        if (_options.JobRetentionDays <= 0)
        {
            return Task.CompletedTask;
        }

        var jobsRoot = Path.Combine(GetWorkspaceRoot(), "jobs");
        if (!Directory.Exists(jobsRoot))
        {
            return Task.CompletedTask;
        }

        var cutoff = DateTime.UtcNow.AddDays(-_options.JobRetentionDays);
        foreach (var jobFolder in Directory.GetDirectories(jobsRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var created = Directory.GetCreationTimeUtc(jobFolder);
                if (created < cutoff)
                {
                    Directory.Delete(jobFolder, recursive: true);
                    _logger.LogInformation("Deleted expired job folder {JobFolder}.", jobFolder);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not delete expired job folder {JobFolder}.", jobFolder);
            }
        }

        return Task.CompletedTask;
    }

    private string GetWorkspaceRoot()
    {
        return string.IsNullOrWhiteSpace(_options.WorkspaceRoot)
            ? Path.Combine(_environment.ContentRootPath, "App_Data")
            : _options.WorkspaceRoot;
    }
}
