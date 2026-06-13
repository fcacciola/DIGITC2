using DigitC2.Server.Models;
using ENGINE;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace DigitC2.Server.Services;

public sealed class EngineJobService
{
    private static readonly HashSet<string> AllowedInputExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".wav",
        ".txt"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ProcessingWorkspaceOptions _options;

    public EngineJobService(IWebHostEnvironment environment, IOptions<ProcessingWorkspaceOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<EngineRunResult> ProcessAsync(IFormFile inputFile, string? requestedName, CancellationToken cancellationToken)
        => await ProcessAsync(inputFile, requestedName, null, cancellationToken);

    public async Task<EngineRunResult> ProcessAsync(
        IFormFile inputFile,
        string? requestedName,
        IReadOnlyList<ConfigParamDto>? configParams,
        CancellationToken cancellationToken)
    {
        if (inputFile.Length == 0)
        {
            throw new InvalidOperationException("Input file is empty.");
        }

        var extension = Path.GetExtension(inputFile.FileName);
        if (!AllowedInputExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only .wav and .txt input files are supported.");
        }

        var jobId = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N")[..8];
        var sessionName = MakeSafeName(string.IsNullOrWhiteSpace(requestedName)
            ? Path.GetFileNameWithoutExtension(inputFile.FileName)
            : requestedName);

        var jobRoot = Path.Combine(GetWorkspaceRoot(), "jobs", jobId);
        var uploadFolder = Path.Combine(jobRoot, "input");
        var outputRoot = Path.Combine(jobRoot, "output");
        Directory.CreateDirectory(uploadFolder);
        Directory.CreateDirectory(outputRoot);

        var inputPath = Path.Combine(uploadFolder, MakeSafeName(Path.GetFileNameWithoutExtension(inputFile.FileName)) + extension.ToLowerInvariant());
        await using (var stream = File.Create(inputPath))
        {
            await inputFile.CopyToAsync(stream, cancellationToken);
        }

        var defaultsRoot = GetEngineDefaultsRoot();
        var configPath = Path.Combine(defaultsRoot, "Config.txt");
        var config = Config.FromFile(configPath) ?? throw new InvalidOperationException($"Engine config file was not found: {configPath}");
        ConfigDtoMapper.ApplyOverrides(config, configParams);
        var settings = CreateSettings(defaultsRoot, uploadFolder, outputRoot);
        var gui = new CapturingGui();
        var session = new Session(inputPath, sessionName, settings, gui, config);

        try
        {
            var startSignal = CreateStartSignal(inputPath, extension);
            var pipeline = string.Equals(extension, ".wav", StringComparison.OrdinalIgnoreCase)
                ? PipelineFactory.FromAudioToTapCode().Then(PipelineFactory.FromTapCode())
                : PipelineFactory.FromTapCode();

            var result = Processor.Process(session, settings, session.Name, pipeline, startSignal);
            CopyInputToSessionFolder(inputPath, session);
            result.Save();

            var runResult = new EngineRunResult(
                jobId,
                sessionName,
                session.CurrentOutputFolder,
                DateTimeOffset.UtcNow,
                gui.Messages,
                gui.Errors);

            WriteMetadata(jobRoot, runResult);

            return runResult;
        }
        finally
        {
            session.Shutdown();
        }
    }

    private static Signal CreateStartSignal(string inputPath, string extension)
    {
        if (string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase))
        {
            return new FileSignal(inputPath);
        }

        var signal = WaveFileSource.Load(inputPath)
            ?? throw new InvalidOperationException("The WAV file could not be loaded.");

        return new WaveSignal(signal);
    }

    private static Settings CreateSettings(string defaultsRoot, string uploadFolder, string outputRoot)
    {
        var settingsPath = Path.Combine(defaultsRoot, "Settings.txt");
        var settings = File.Exists(settingsPath)
            ? Settings.FromFile(settingsPath)
            : new Settings();

        settings.Set("InputFolder", defaultsRoot, null);
        settings.Set("OutputFolder", outputRoot, null);
        settings.Set("SamplesFolder", uploadFolder, null);
        EnsureSetting(settings, "OutputDetails", "false");
        EnsureSetting(settings, "DisableBranching", "false");
        EnsureSetting(settings, "MaxGoodBranches", "4");
        EnsureSetting(settings, "MaxTotalBranches", "16");
        return settings;
    }

    private static void EnsureSetting(Settings settings, string key, string value)
    {
        if (settings.GetValue(key) == null)
        {
            settings.Set(key, value, null);
        }
    }

    private static void CopyInputToSessionFolder(string inputPath, Session session)
    {
        var copyPath = Path.Combine(session.CurrentOutputFolder, Path.GetFileName(inputPath));
        File.Copy(inputPath, copyPath, true);
    }

    private string GetWorkspaceRoot()
    {
        var configured = _options.WorkspaceRoot;
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(_environment.ContentRootPath, "App_Data")
            : configured;
    }

    private string GetEngineDefaultsRoot()
    {
        var runtimeDefaults = Path.Combine(AppContext.BaseDirectory, "EngineDefaults");
        if (Directory.Exists(runtimeDefaults))
        {
            return runtimeDefaults;
        }

        return Path.Combine(_environment.ContentRootPath, "EngineDefaults");
    }

    public IReadOnlyList<ConfigParamDto> GetDefaultConfigParams()
    {
        var defaultsRoot = GetEngineDefaultsRoot();
        return ConfigDtoMapper.GetEditableParams(Path.Combine(defaultsRoot, "Config.txt"));
    }

    private static void WriteMetadata(string jobRoot, EngineRunResult result)
    {
        var metadata = new JobMetadata(
            result.JobId,
            result.SessionName,
            "completed",
            result.OutputFolder,
            result.CreatedAt,
            result.Messages,
            result.Errors);

        File.WriteAllText(
            Path.Combine(jobRoot, "job.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string MakeSafeName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(value.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "session" : safe;
    }
}
