using Microsoft.Extensions.Options;

namespace DigitC2.Server.Services;

public sealed class JobCleanupService : BackgroundService
{
  private readonly ILogger<JobCleanupService> _logger;
  private readonly ProcessingWorkspaceOptions _options;
  private readonly SavedJobService _savedJobs;

  public JobCleanupService(
    IOptions<ProcessingWorkspaceOptions> options,
    ILogger<JobCleanupService> logger,
    SavedJobService savedJobs)
  {
    _options = options.Value;
    _logger = logger;
    _savedJobs = savedJobs;
  }

  protected override async Task ExecuteAsync(CancellationToken stoppingToken)
  {
    await CleanupOnce(stoppingToken);

    using var timer = new PeriodicTimer(
      TimeSpan.FromMinutes(Math.Max(5, _options.CleanupIntervalMinutes)));

    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
      await CleanupOnce(stoppingToken);
    }
  }

  private async Task CleanupOnce(CancellationToken cancellationToken)
  {
    if (_options.JobRetentionDays <= 0 || !Directory.Exists(_savedJobs.JobsRoot))
    {
      return;
    }

    var cutoff = DateTime.UtcNow.AddDays(-_options.JobRetentionDays);
    foreach (var jobFolder in Directory.GetDirectories(_savedJobs.JobsRoot))
    {
      cancellationToken.ThrowIfCancellationRequested();

      try
      {
        if (await _savedJobs.TryDeleteExpiredUnsavedJobAsync(
            jobFolder,
            cutoff,
            cancellationToken))
        {
          _logger.LogInformation("Deleted expired job folder {JobFolder}.", jobFolder);
        }
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "Could not delete expired job folder {JobFolder}.", jobFolder);
      }
    }
  }
}
