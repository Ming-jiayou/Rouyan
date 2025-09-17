using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Rouyan.Services;

public interface IEnvConfigService
{
    Task<EnvConfig> LoadConfigAsync();
    Task SaveConfigAsync(EnvConfig config);
}

public class EnvConfig
{
    public string ChatApiKey { get; set; } = "";
    public string ChatBaseUrl { get; set; } = "";
    public string ChatModel { get; set; } = "";

    public string VisionApiKey { get; set; } = "";
    public string VisionBaseUrl { get; set; } = "";
    public string VisionModel { get; set; } = "";
}

public class EnvConfigService : IEnvConfigService
{
    private readonly string _envFilePath;

    public EnvConfigService()
    {
        _envFilePath = Path.Combine(AppContext.BaseDirectory, ".env");
    }

    public async Task<EnvConfig> LoadConfigAsync()
    {
        var config = new EnvConfig();

        if (!File.Exists(_envFilePath))
        {
            return config;
        }

        var lines = await File.ReadAllLinesAsync(_envFilePath);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                continue;

            var parts = line.Split('=', 2);
            if (parts.Length != 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim().Trim('"');

            switch (key)
            {
                case "OPENAI_API_KEY":
                    config.ChatApiKey = value;
                    break;
                case "OPENAI_BASE_URL":
                    config.ChatBaseUrl = value;
                    break;
                case "OPENAI_CHAT_MODEL":
                    config.ChatModel = value;
                    break;
                case "OPENAI_VISION_API_KEY":
                    config.VisionApiKey = value;
                    break;
                case "OPENAI_VISION_BASE_URL":
                    config.VisionBaseUrl = value;
                    break;
                case "OPENAI_VISION_MODEL":
                    config.VisionModel = value;
                    break;
            }
        }

        return config;
    }

    public async Task SaveConfigAsync(EnvConfig config)
    {
        var lines = new List<string>();

        if (File.Exists(_envFilePath))
        {
            var existingLines = await File.ReadAllLinesAsync(_envFilePath);
            var processedKeys = new HashSet<string>();

            foreach (var line in existingLines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                {
                    lines.Add(line);
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts.Length != 2)
                {
                    lines.Add(line);
                    continue;
                }

                var key = parts[0].Trim();

                switch (key)
                {
                    case "OPENAI_API_KEY":
                        lines.Add($"OPENAI_API_KEY=\"{config.ChatApiKey}\"");
                        processedKeys.Add(key);
                        break;
                    case "OPENAI_BASE_URL":
                        lines.Add($"OPENAI_BASE_URL=\"{config.ChatBaseUrl}\"");
                        processedKeys.Add(key);
                        break;
                    case "OPENAI_CHAT_MODEL":
                        lines.Add($"OPENAI_CHAT_MODEL=\"{config.ChatModel}\"");
                        processedKeys.Add(key);
                        break;
                    case "OPENAI_VISION_API_KEY":
                        lines.Add($"OPENAI_VISION_API_KEY=\"{config.VisionApiKey}\"");
                        processedKeys.Add(key);
                        break;
                    case "OPENAI_VISION_BASE_URL":
                        lines.Add($"OPENAI_VISION_BASE_URL=\"{config.VisionBaseUrl}\"");
                        processedKeys.Add(key);
                        break;
                    case "OPENAI_VISION_MODEL":
                        lines.Add($"OPENAI_VISION_MODEL=\"{config.VisionModel}\"");
                        processedKeys.Add(key);
                        break;
                    default:
                        lines.Add(line);
                        break;
                }
            }

            if (!processedKeys.Contains("OPENAI_API_KEY"))
                lines.Add($"OPENAI_API_KEY=\"{config.ChatApiKey}\"");
            if (!processedKeys.Contains("OPENAI_BASE_URL"))
                lines.Add($"OPENAI_BASE_URL=\"{config.ChatBaseUrl}\"");
            if (!processedKeys.Contains("OPENAI_CHAT_MODEL"))
                lines.Add($"OPENAI_CHAT_MODEL=\"{config.ChatModel}\"");
            if (!processedKeys.Contains("OPENAI_VISION_API_KEY"))
                lines.Add($"OPENAI_VISION_API_KEY=\"{config.VisionApiKey}\"");
            if (!processedKeys.Contains("OPENAI_VISION_BASE_URL"))
                lines.Add($"OPENAI_VISION_BASE_URL=\"{config.VisionBaseUrl}\"");
            if (!processedKeys.Contains("OPENAI_VISION_MODEL"))
                lines.Add($"OPENAI_VISION_MODEL=\"{config.VisionModel}\"");
        }
        else
        {
            lines.Add($"OPENAI_API_KEY=\"{config.ChatApiKey}\"");
            lines.Add($"OPENAI_BASE_URL=\"{config.ChatBaseUrl}\"");
            lines.Add($"OPENAI_CHAT_MODEL=\"{config.ChatModel}\"");
            lines.Add("");
            lines.Add($"OPENAI_VISION_MODEL=\"{config.VisionModel}\"");
            lines.Add($"OPENAI_VISION_BASE_URL=\"{config.VisionBaseUrl}\"");
            lines.Add($"OPENAI_VISION_API_KEY=\"{config.VisionApiKey}\"");
        }

        await File.WriteAllLinesAsync(_envFilePath, lines);
    }
}