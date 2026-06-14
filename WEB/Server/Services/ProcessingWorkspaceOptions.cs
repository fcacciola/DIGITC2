namespace DigitC2.Server.Services;

public sealed class ProcessingWorkspaceOptions
{
    public string? WorkspaceRoot { get; set; }
    public string? SharedPassword { get; set; }
    public long MaxUploadBytes { get; set; } = 100 * 1024 * 1024;
    public int JobRetentionDays { get; set; } = 14;
    public int CleanupIntervalMinutes { get; set; } = 60;
}
