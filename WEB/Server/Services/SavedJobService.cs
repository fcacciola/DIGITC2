using DigitC2.Server.Models;
using Microsoft.Extensions.Options;
using System.IO.Compression;
using System.Text.Json;

namespace DigitC2.Server.Services;

public sealed class SavedJobService
{
  private const int ArchiveFormatVersion = 1;
  private const int MaxArchiveEntries = 10_000;

  private static readonly JsonSerializerOptions MetadataJsonOptions = new()
  {
    PropertyNameCaseInsensitive = true,
    WriteIndented = true
  };

  private static readonly JsonSerializerOptions ArchiveJsonOptions = new(JsonSerializerDefaults.Web)
  {
    WriteIndented = true
  };

  private readonly SemaphoreSlim _gate = new(1, 1);
  private readonly IWebHostEnvironment _environment;
  private readonly ILogger<SavedJobService> _logger;
  private readonly ProcessingWorkspaceOptions _options;

  public SavedJobService(
    IWebHostEnvironment environment,
    IOptions<ProcessingWorkspaceOptions> options,
    ILogger<SavedJobService> logger)
  {
    _environment = environment;
    _options = options.Value;
    _logger = logger;
  }

  public string JobsRoot => Path.Combine(GetWorkspaceRoot(), "jobs");

  public async Task<SavedJobResponse> SaveAsync(string jobId, CancellationToken cancellationToken)
  {
    await _gate.WaitAsync(cancellationToken);
    try
    {
      var jobRoot = GetExistingJobRoot(jobId);
      var metadata = ReadMetadata(jobRoot);
      if (!string.Equals(metadata.Status, "completed", StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("Only completed jobs can be saved.");
      }

      var sessionName = GetUniqueSavedSessionName(metadata.SessionName, jobId);
      metadata = metadata with
      {
        SessionName = sessionName,
        Saved = true
      };

      WriteMetadata(jobRoot, metadata);
      return CreateSavedJobResponse(metadata);
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task<IReadOnlyList<SavedJobSummary>> GetSavedJobsAsync(CancellationToken cancellationToken)
  {
    await _gate.WaitAsync(cancellationToken);
    try
    {
      if (!Directory.Exists(JobsRoot))
      {
        return [];
      }

      var savedJobs = new List<SavedJobSummary>();
      foreach (var jobRoot in Directory.GetDirectories(JobsRoot))
      {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
          var metadata = ReadMetadata(jobRoot);
          if (metadata.Saved)
          {
            savedJobs.Add(new SavedJobSummary(metadata.JobId, metadata.SessionName));
          }
        }
        catch (Exception ex)
        {
          _logger.LogWarning(ex, "Could not read job metadata from {JobRoot}.", jobRoot);
        }
      }

      return savedJobs
        .OrderBy(job => job.SessionName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(job => job.JobId, StringComparer.OrdinalIgnoreCase)
        .ToList();
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task EnsureSavedAsync(string jobId, CancellationToken cancellationToken)
  {
    await _gate.WaitAsync(cancellationToken);
    try
    {
      var metadata = ReadMetadata(GetExistingJobRoot(jobId));
      if (!metadata.Saved)
      {
        throw new InvalidOperationException("The requested job is not saved.");
      }
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task DeleteAsync(string jobId, CancellationToken cancellationToken)
  {
    await _gate.WaitAsync(cancellationToken);
    try
    {
      var jobRoot = GetExistingJobRoot(jobId);
      var metadata = ReadMetadata(jobRoot);
      if (!metadata.Saved)
      {
        throw new InvalidOperationException("Only saved jobs can be deleted through this endpoint.");
      }

      Directory.Delete(jobRoot, recursive: true);
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task<(string Path, string DownloadName)> CreateExportArchiveAsync(
    string jobId,
    CancellationToken cancellationToken)
  {
    await _gate.WaitAsync(cancellationToken);
    string? archivePath = null;

    try
    {
      var jobRoot = GetExistingJobRoot(jobId);
      var metadata = ReadMetadata(jobRoot);
      if (!string.Equals(metadata.Status, "completed", StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("Only completed jobs can be exported.");
      }

      var exportsRoot = Path.Combine(GetWorkspaceRoot(), "temp", "exports");
      Directory.CreateDirectory(exportsRoot);
      archivePath = Path.Combine(exportsRoot, $"{Guid.NewGuid():N}.digitc2.zip");

      await using var archiveStream = File.Create(archivePath);
      using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create);

      var sharedMetadata = new SharedJobArchive(
        ArchiveFormatVersion,
        metadata.SessionName,
        metadata.CreatedAt,
        metadata.Messages,
        metadata.Errors);

      var metadataEntry = archive.CreateEntry("share.json", CompressionLevel.Optimal);
      await using (var metadataStream = metadataEntry.Open())
      {
        await JsonSerializer.SerializeAsync(
          metadataStream,
          sharedMetadata,
          ArchiveJsonOptions,
          cancellationToken);
      }

      AddDirectoryToArchive(
        archive,
        Path.Combine(jobRoot, "input"),
        "input",
        cancellationToken);
      AddDirectoryToArchive(
        archive,
        Path.Combine(jobRoot, "output"),
        "output",
        cancellationToken);

      return (archivePath, $"{MakeSafeName(metadata.SessionName)}.digitc2.zip");
    }
    catch
    {
      if (archivePath != null)
      {
        DeleteExportArchive(archivePath);
      }

      throw;
    }
    finally
    {
      _gate.Release();
    }
  }

  public async Task<SavedJobResponse> ImportAsync(
    IFormFile archiveFile,
    CancellationToken cancellationToken)
  {
    if (archiveFile.Length == 0)
    {
      throw new InvalidOperationException("The imported archive is empty.");
    }

    if (_options.MaxUploadBytes > 0 && archiveFile.Length > _options.MaxUploadBytes)
    {
      throw new InvalidOperationException("The imported archive exceeds the upload limit.");
    }

    await _gate.WaitAsync(cancellationToken);

    string? importRoot = null;
    string? jobRoot = null;
    try
    {
      var tempRoot = Path.Combine(GetWorkspaceRoot(), "temp", "imports");
      Directory.CreateDirectory(tempRoot);
      importRoot = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(importRoot);

      await ExtractArchiveAsync(archiveFile, importRoot, cancellationToken);

      var sharedMetadataPath = Path.Combine(importRoot, "share.json");
      if (!File.Exists(sharedMetadataPath))
      {
        throw new InvalidDataException("The archive does not contain share.json.");
      }

      var sharedMetadata = JsonSerializer.Deserialize<SharedJobArchive>(
        await File.ReadAllTextAsync(sharedMetadataPath, cancellationToken),
        ArchiveJsonOptions)
        ?? throw new InvalidDataException("The shared-job metadata could not be read.");

      if (sharedMetadata.FormatVersion != ArchiveFormatVersion)
      {
        throw new InvalidDataException($"Unsupported shared-job format version: {sharedMetadata.FormatVersion}.");
      }

      var importedOutputRoot = Path.Combine(importRoot, "output");
      if (!Directory.Exists(importedOutputRoot))
      {
        throw new InvalidDataException("The archive does not contain job output.");
      }

      Directory.CreateDirectory(JobsRoot);
      var jobId = CreateJobId();
      while (Directory.Exists(Path.Combine(JobsRoot, jobId)))
      {
        jobId = CreateJobId();
      }

      jobRoot = Path.Combine(JobsRoot, jobId);
      Directory.CreateDirectory(jobRoot);

      var importedInputRoot = Path.Combine(importRoot, "input");
      var jobInputRoot = Path.Combine(jobRoot, "input");
      if (Directory.Exists(importedInputRoot))
      {
        Directory.Move(importedInputRoot, jobInputRoot);
      }
      else
      {
        Directory.CreateDirectory(jobInputRoot);
      }

      var jobOutputRoot = Path.Combine(jobRoot, "output");
      Directory.Move(importedOutputRoot, jobOutputRoot);

      var outputFolder = Directory
        .GetDirectories(jobOutputRoot)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault()
        ?? jobOutputRoot;
      var sessionName = GetUniqueSavedSessionName(sharedMetadata.SessionName);
      var metadata = new JobMetadata(
        jobId,
        sessionName,
        "completed",
        outputFolder,
        sharedMetadata.CreatedAt,
        sharedMetadata.Messages ?? [],
        sharedMetadata.Errors ?? [],
        true);

      WriteMetadata(jobRoot, metadata);
      return CreateSavedJobResponse(metadata);
    }
    catch
    {
      if (jobRoot != null && Directory.Exists(jobRoot))
      {
        Directory.Delete(jobRoot, recursive: true);
      }

      throw;
    }
    finally
    {
      if (importRoot != null && Directory.Exists(importRoot))
      {
        Directory.Delete(importRoot, recursive: true);
      }

      _gate.Release();
    }
  }

  public async Task<bool> TryDeleteExpiredUnsavedJobAsync(
    string jobRoot,
    DateTime cutoff,
    CancellationToken cancellationToken)
  {
    await _gate.WaitAsync(cancellationToken);
    try
    {
      var fullJobsRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(JobsRoot));
      var fullJobRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(jobRoot));
      if (!string.Equals(
          Path.GetDirectoryName(fullJobRoot),
          fullJobsRoot,
          StringComparison.OrdinalIgnoreCase))
      {
        throw new InvalidOperationException("The cleanup target is outside the jobs directory.");
      }

      if (!Directory.Exists(fullJobRoot) || Directory.GetCreationTimeUtc(fullJobRoot) >= cutoff)
      {
        return false;
      }

      var metadataPath = Path.Combine(fullJobRoot, "job.json");
      if (File.Exists(metadataPath))
      {
        JobMetadata metadata;
        try
        {
          metadata = ReadMetadata(fullJobRoot);
        }
        catch (Exception ex)
        {
          _logger.LogWarning(
            ex,
            "Skipping cleanup for {JobRoot} because its metadata could not be read.",
            fullJobRoot);
          return false;
        }

        if (metadata.Saved)
        {
          return false;
        }
      }

      Directory.Delete(fullJobRoot, recursive: true);
      return true;
    }
    finally
    {
      _gate.Release();
    }
  }

  public static void DeleteExportArchive(string archivePath)
  {
    try
    {
      if (File.Exists(archivePath))
      {
        File.Delete(archivePath);
      }
    }
    catch
    {
      // Export cleanup is best-effort; the server's temp folder can be cleared manually.
    }
  }

  private async Task ExtractArchiveAsync(
    IFormFile archiveFile,
    string importRoot,
    CancellationToken cancellationToken)
  {
    await using var input = archiveFile.OpenReadStream();
    using var archive = new ZipArchive(input, ZipArchiveMode.Read);
    if (archive.Entries.Count > MaxArchiveEntries)
    {
      throw new InvalidDataException("The imported archive contains too many files.");
    }

    var importRootPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(importRoot));
    var importRootPrefix = importRootPath + Path.DirectorySeparatorChar;
    var extractedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    long extractedBytes = 0;
    var maxExtractedBytes = GetMaxExtractedBytes();

    foreach (var entry in archive.Entries)
    {
      cancellationToken.ThrowIfCancellationRequested();

      var relativePath = entry.FullName.Replace('\\', '/');
      if (!IsAllowedArchivePath(relativePath))
      {
        throw new InvalidDataException($"The archive contains an unsupported path: {entry.FullName}");
      }

      if (relativePath.EndsWith('/'))
      {
        continue;
      }

      extractedBytes = checked(extractedBytes + entry.Length);
      if (extractedBytes > maxExtractedBytes)
      {
        throw new InvalidDataException("The extracted archive exceeds the allowed size.");
      }

      var platformPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
      var destinationPath = Path.GetFullPath(Path.Combine(importRootPath, platformPath));
      if (!destinationPath.StartsWith(importRootPrefix, StringComparison.OrdinalIgnoreCase)
          || !extractedPaths.Add(destinationPath))
      {
        throw new InvalidDataException($"The archive contains an invalid path: {entry.FullName}");
      }

      Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
      await using var output = File.Create(destinationPath);
      await using var entryStream = entry.Open();
      await entryStream.CopyToAsync(output, cancellationToken);
    }
  }

  private static bool IsAllowedArchivePath(string relativePath)
  {
    return string.Equals(relativePath, "share.json", StringComparison.OrdinalIgnoreCase)
        || relativePath.StartsWith("input/", StringComparison.OrdinalIgnoreCase)
        || relativePath.StartsWith("output/", StringComparison.OrdinalIgnoreCase);
  }

  private long GetMaxExtractedBytes()
  {
    var uploadLimit = _options.MaxUploadBytes > 0
      ? _options.MaxUploadBytes
      : 100L * 1024 * 1024;

    return uploadLimit > long.MaxValue / 10
      ? long.MaxValue
      : uploadLimit * 10;
  }

  private static void AddDirectoryToArchive(
    ZipArchive archive,
    string sourceRoot,
    string archiveRoot,
    CancellationToken cancellationToken)
  {
    if (!Directory.Exists(sourceRoot))
    {
      return;
    }

    foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
    {
      cancellationToken.ThrowIfCancellationRequested();
      var relativePath = Path.GetRelativePath(sourceRoot, file).Replace('\\', '/');
      archive.CreateEntryFromFile(
        file,
        $"{archiveRoot}/{relativePath}",
        CompressionLevel.Optimal);
    }
  }

  private string GetUniqueSavedSessionName(string requestedName, string? excludedJobId = null)
  {
    var baseName = MakeSafeName(requestedName);
    var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    if (Directory.Exists(JobsRoot))
    {
      foreach (var jobRoot in Directory.GetDirectories(JobsRoot))
      {
        try
        {
          var metadata = ReadMetadata(jobRoot);
          if (metadata.Saved
              && !string.Equals(metadata.JobId, excludedJobId, StringComparison.OrdinalIgnoreCase))
          {
            usedNames.Add(metadata.SessionName);
          }
        }
        catch
        {
          // Unreadable and unsaved jobs do not participate in display-name uniqueness.
        }
      }
    }

    if (!usedNames.Contains(baseName))
    {
      return baseName;
    }

    for (var suffix = 2; ; suffix++)
    {
      var candidate = $"{baseName} ({suffix})";
      if (!usedNames.Contains(candidate))
      {
        return candidate;
      }
    }
  }

  private static SavedJobResponse CreateSavedJobResponse(JobMetadata metadata)
  {
    return new SavedJobResponse(
      metadata.JobId,
      metadata.SessionName,
      $"/api/saved-jobs/{metadata.JobId}/result");
  }

  private JobMetadata ReadMetadata(string jobRoot)
  {
    var metadataPath = Path.Combine(jobRoot, "job.json");
    if (!File.Exists(metadataPath))
    {
      throw new DirectoryNotFoundException("Job metadata was not found.");
    }

    return JsonSerializer.Deserialize<JobMetadata>(
      File.ReadAllText(metadataPath),
      MetadataJsonOptions)
      ?? throw new DirectoryNotFoundException("Job metadata could not be read.");
  }

  private static void WriteMetadata(string jobRoot, JobMetadata metadata)
  {
    var metadataPath = Path.Combine(jobRoot, "job.json");
    var temporaryPath = $"{metadataPath}.tmp-{Guid.NewGuid():N}";

    try
    {
      File.WriteAllText(
        temporaryPath,
        JsonSerializer.Serialize(metadata, MetadataJsonOptions));
      File.Move(temporaryPath, metadataPath, overwrite: true);
    }
    finally
    {
      if (File.Exists(temporaryPath))
      {
        File.Delete(temporaryPath);
      }
    }
  }

  private string GetExistingJobRoot(string jobId)
  {
    var jobRoot = GetJobRoot(jobId);
    if (!Directory.Exists(jobRoot))
    {
      throw new DirectoryNotFoundException("Result job was not found.");
    }

    return jobRoot;
  }

  private string GetJobRoot(string jobId)
  {
    if (string.IsNullOrWhiteSpace(jobId)
        || jobId.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-'))
    {
      throw new DirectoryNotFoundException("Invalid job id.");
    }

    return Path.Combine(JobsRoot, jobId);
  }

  private static string CreateJobId()
  {
    return DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff")
        + "-"
        + Guid.NewGuid().ToString("N")[..8];
  }

  private static string MakeSafeName(string? value)
  {
    var invalid = Path.GetInvalidFileNameChars();
    var safe = new string((value ?? "").Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
    return string.IsNullOrWhiteSpace(safe) ? "session" : safe;
  }

  private string GetWorkspaceRoot()
  {
    return string.IsNullOrWhiteSpace(_options.WorkspaceRoot)
      ? Path.Combine(_environment.ContentRootPath, "App_Data")
      : _options.WorkspaceRoot;
  }
}
