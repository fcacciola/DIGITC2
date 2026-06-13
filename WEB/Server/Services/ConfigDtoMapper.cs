using DigitC2.Server.Models;
using ENGINE;

namespace DigitC2.Server.Services;

public static class ConfigDtoMapper
{
    public static IReadOnlyList<ConfigParamDto> GetEditableParams(string configPath)
    {
        if (!File.Exists(configPath))
        {
            return [];
        }

        var result = new List<ConfigParamDto>();
        foreach (var line in File.ReadLines(configPath))
        {
            if (!ConfigHelper.IsValidLine(line))
            {
                continue;
            }

            var equalsIndex = line.IndexOf('=');
            if (equalsIndex < 0)
            {
                continue;
            }

            var name = line[..equalsIndex].Trim();
            var rawValue = line[(equalsIndex + 1)..].Trim();
            var (value, label) = ConfigHelper.SplitSectionValue(rawValue);
            if (label == null)
            {
                continue;
            }

            var (section, key) = ConfigHelper.SplitSectionKey(name);
            result.Add(new ConfigParamDto(section, key, name, value, label));
        }

        return result;
    }

    public static void ApplyOverrides(Config config, IEnumerable<ConfigParamDto>? overrides)
    {
        if (overrides == null)
        {
            return;
        }

        foreach (var param in overrides)
        {
            if (string.IsNullOrWhiteSpace(param.Section) || string.IsNullOrWhiteSpace(param.Key))
            {
                continue;
            }

            config.GetSection(param.Section).Set(param.Key, param.Value ?? "", string.IsNullOrWhiteSpace(param.Label) ? null : param.Label);
        }
    }
}
