using DigitC2.Server.Models;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.Json;

namespace DigitC2.Server.Services;

public sealed class ResultCatalogService
{
    private const string ResultFileName = "Result.txt";
    private const string CombinedLogFileName = "COMBINED LOG FILE.txt";

    private static readonly HashSet<string> HiddenFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Config.txt"
    };

    private static readonly string[] HiddenNameFragments =
    [
        "_detail",
        "log",
        "gmm"
    ];

    private static readonly HashSet<string> DisplayableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".json",
        ".png",
        ".wav"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ProcessingWorkspaceOptions _options;
    private readonly FileExtensionContentTypeProvider _contentTypes = new();

    public ResultCatalogService(IWebHostEnvironment environment, Microsoft.Extensions.Options.IOptions<ProcessingWorkspaceOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public ResultManifest GetManifest(string jobId, HttpRequest request)
    {
        var jobRoot = GetJobRoot(jobId);
        var outputRoot = Path.Combine(jobRoot, "output");

        if (!Directory.Exists(outputRoot))
        {
            throw new DirectoryNotFoundException($"Result job was not found: {jobId}");
        }

        var metadata = ReadMetadata(jobRoot);
        var sessionFolder = Directory.Exists(metadata.OutputFolder)
            ? metadata.OutputFolder
            : Directory.GetDirectories(outputRoot).OrderBy(path => path).FirstOrDefault() ?? outputRoot;
        var winningBranch = ResolveWinningBranch(sessionFolder);
        var files = EnumerateDirectory(jobId, sessionFolder, sessionFolder, winningBranch, request);

        return new ResultManifest(
            jobId,
            metadata.SessionName,
            metadata.Status,
            metadata.CreatedAt,
            files,
            GetWinningConfigParams(winningBranch),
            metadata.Messages,
            metadata.Errors);
    }

    public (string Path, string ContentType, string DownloadName) GetFile(string jobId, string relativePath)
    {
        var manifestRoot = ResolveManifestRoot(jobId);
        var winningBranch = ResolveWinningBranch(manifestRoot);
        var fullPath = Path.GetFullPath(Path.Combine(manifestRoot, relativePath));
        var rootPath = Path.GetFullPath(manifestRoot);

        if (!fullPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase)
            || !File.Exists(fullPath)
            || !IsVisible(fullPath)
            || !IsInWinningBranchScope(fullPath, winningBranch))
        {
            throw new FileNotFoundException("Result file was not found.");
        }

        if (!_contentTypes.TryGetContentType(fullPath, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return (fullPath, contentType, Path.GetFileName(fullPath));
    }

    private IReadOnlyList<ResultFileNode> EnumerateDirectory(
        string jobId,
        string root,
        string directory,
        WinningBranch winningBranch,
        HttpRequest request)
    {
        var nodes = new List<ResultFileNode>();

        foreach (var childDirectory in Directory.GetDirectories(directory).OrderBy(path => path))
        {
            if (!IsDirectoryOnWinningBranchPath(childDirectory, winningBranch))
            {
                continue;
            }

            var children = EnumerateDirectory(jobId, root, childDirectory, winningBranch, request);
            if (children.Count > 0)
            {
                nodes.Add(new ResultFileNode(
                    Path.GetFileName(childDirectory),
                    Path.GetRelativePath(root, childDirectory).Replace('\\', '/'),
                    "folder",
                    null,
                    null,
                    null,
                    children));
            }
        }

        foreach (var file in Directory.GetFiles(directory).Where(IsVisible).Where(path => IsInWinningBranchScope(path, winningBranch)).OrderBy(path => path))
        {
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            _contentTypes.TryGetContentType(file, out var contentType);

            nodes.Add(new ResultFileNode(
                Path.GetFileName(file),
                relativePath,
                "file",
                new FileInfo(file).Length,
                contentType ?? "application/octet-stream",
                $"/api/jobs/{jobId}/files/{Uri.EscapeDataString(relativePath).Replace("%2F", "/")}",
                null));
        }

        return nodes;
    }

    private string ResolveManifestRoot(string jobId)
    {
        var jobRoot = GetJobRoot(jobId);
        var metadata = ReadMetadata(jobRoot);
        var outputRoot = Path.Combine(jobRoot, "output");

        return Directory.Exists(metadata.OutputFolder)
            ? metadata.OutputFolder
            : Directory.GetDirectories(outputRoot).OrderBy(path => path).FirstOrDefault() ?? outputRoot;
    }

    private string GetJobRoot(string jobId)
    {
        if (jobId.Any(ch => !char.IsLetterOrDigit(ch) && ch != '-'))
        {
            throw new DirectoryNotFoundException("Invalid job id.");
        }

        return Path.Combine(GetWorkspaceRoot(), "jobs", jobId);
    }

    private static JobMetadata ReadMetadata(string jobRoot)
    {
        var metadataPath = Path.Combine(jobRoot, "job.json");
        if (!File.Exists(metadataPath))
        {
            var fallbackOutputRoot = Path.Combine(jobRoot, "output");
            var fallbackOutput = Directory.GetDirectories(fallbackOutputRoot).OrderBy(path => path).FirstOrDefault() ?? fallbackOutputRoot;
            return new JobMetadata(
                Path.GetFileName(jobRoot),
                Path.GetFileName(fallbackOutput),
                "completed",
                fallbackOutput,
                Directory.GetCreationTimeUtc(jobRoot),
                [],
                []);
        }

        return JsonSerializer.Deserialize<JobMetadata>(File.ReadAllText(metadataPath))
            ?? throw new DirectoryNotFoundException("Job metadata could not be read.");
    }

    private string GetWorkspaceRoot()
    {
        var configured = _options.WorkspaceRoot;
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(_environment.ContentRootPath, "App_Data")
            : configured;
    }

    private static bool IsVisible(string path)
    {
        var name = Path.GetFileName(path);
        if (HiddenFileNames.Contains(name))
        {
            return false;
        }

        if (string.Equals(name, CombinedLogFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (HiddenNameFragments.Any(fragment => name.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        if (string.Equals(name, ResultFileName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return DisplayableExtensions.Contains(Path.GetExtension(path));
    }

    private static IReadOnlyList<ConfigParamDto> GetWinningConfigParams(WinningBranch winningBranch)
    {
        return ConfigDtoMapper.GetEditableParams(Path.Combine(winningBranch.FinalFolder, "Config.txt"));
    }

    private static WinningBranch ResolveWinningBranch(string sessionFolder)
    {
        var sessionRoot = Path.GetFullPath(sessionFolder);
        var candidates = Directory
            .EnumerateFiles(sessionRoot, ResultFileName, SearchOption.AllDirectories)
            .Select(path => new ResultCandidate(path, TryReadScore(path)))
            .ToList();

        var winner = candidates
            .Where(candidate => candidate.Score.HasValue)
            .OrderByDescending(candidate => candidate.Score!.Value)
            .ThenBy(candidate => candidate.ResultFilePath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault()
            ?? candidates
                .OrderBy(candidate => candidate.ResultFilePath, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();

        var finalFolder = winner != null
            ? Path.GetDirectoryName(winner.ResultFilePath)!
            : sessionRoot;

        return new WinningBranch(sessionRoot, Path.GetFullPath(finalFolder), winner?.Score);
    }

    private static double? TryReadScore(string resultFile)
    {
        var firstLine = File.ReadLines(resultFile).FirstOrDefault();
        if (firstLine == null || !firstLine.StartsWith("Score=", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rawScore = firstLine["Score=".Length..].Trim();
        return double.TryParse(rawScore, out var score)
            ? Math.Clamp(score, 0, 100)
            : null;
    }

    private static bool IsDirectoryOnWinningBranchPath(string directory, WinningBranch winningBranch)
    {
        var fullDirectory = Path.GetFullPath(directory);
        return IsSameOrAncestor(fullDirectory, winningBranch.FinalFolder)
            || IsSameOrAncestor(winningBranch.FinalFolder, fullDirectory);
    }

    private static bool IsInWinningBranchScope(string file, WinningBranch winningBranch)
    {
        var fileFolder = Path.GetFullPath(Path.GetDirectoryName(file)!);

        return string.Equals(fileFolder, winningBranch.SessionRoot, StringComparison.OrdinalIgnoreCase)
            || IsSameOrAncestor(fileFolder, winningBranch.FinalFolder)
            || IsSameOrAncestor(winningBranch.FinalFolder, fileFolder);
    }

    private static bool IsSameOrAncestor(string possibleAncestor, string path)
    {
        var ancestor = Path.TrimEndingDirectorySeparator(Path.GetFullPath(possibleAncestor));
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

        return string.Equals(ancestor, fullPath, StringComparison.OrdinalIgnoreCase)
            || fullPath.StartsWith(ancestor + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ResultCandidate(string ResultFilePath, double? Score);

    private sealed record WinningBranch(string SessionRoot, string FinalFolder, double? Score);
}
