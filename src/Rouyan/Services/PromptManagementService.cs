using Rouyan.Interfaces;
using Rouyan.Models;
using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rouyan.Services
{
    public class PromptManagementService : IPromptManagementService
    {
        private readonly string _baseDirectory;
        private readonly string _llmPromptsDirectory;
        private readonly string _vlmPromptsDirectory;
        private readonly string _configFilePath;

        public PromptManagementService(IEventAggregator eventAggregator)
        {
            _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts");
            _llmPromptsDirectory = Path.Combine(_baseDirectory, "LLMPrompts");
            _vlmPromptsDirectory = Path.Combine(_baseDirectory, "VLMPrompts");
            _configFilePath = Path.Combine(_baseDirectory, "PromptConfig.txt");
        }

        public string CurrentLLMPrompt1 { get; set; } = string.Empty;
        public string CurrentLLMPrompt2 { get; set; } = string.Empty;
        public string CurrentVLMPrompt1 { get; set; } = string.Empty;
        public string CurrentVLMPrompt2 { get; set; } = string.Empty;
        public ObservableCollection<PromptItem> LLMPrompts { get; set; } = new ObservableCollection<PromptItem>();
        public ObservableCollection<PromptItem> VLMPrompts { get; set; } = new ObservableCollection<PromptItem>();

        public async Task LoadPromptsAsync()
        {
            await LoadLLMPromptsAsync();
            await LoadVLMPromptsAsync();
            await LoadConfigAsync();
        }

        public async Task LoadConfigAsync()
        {
            try
            {
                // 获取源文件路径
                var sourceConfigPath = GetSourceConfigPath();
                
                if (File.Exists(sourceConfigPath))
                {
                    var configLines = await File.ReadAllLinesAsync(sourceConfigPath);
                    var config = new Dictionary<string, string>();

                    foreach (var line in configLines)
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains('='))
                        {
                            var parts = line.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                config[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }

                    // 根据配置文件设置当前提示词
                    CurrentLLMPrompt1 = GetPromptContent(config, "LLMPrompt1", LLMPrompts, 0);
                    CurrentLLMPrompt2 = GetPromptContent(config, "LLMPrompt2", LLMPrompts, 1);
                    CurrentVLMPrompt1 = GetPromptContent(config, "VLMPrompt1", VLMPrompts, 0);
                    CurrentVLMPrompt2 = GetPromptContent(config, "VLMPrompt2", VLMPrompts, 1);
                }
                else
                {
                    // 如果配置文件不存在，使用默认设置
                    if (LLMPrompts.Any())
                    {
                        CurrentLLMPrompt1 = LLMPrompts.First().Content;
                        CurrentLLMPrompt2 = LLMPrompts.Count > 1 ? LLMPrompts.ElementAt(1).Content : LLMPrompts.First().Content;
                    }
                    if (VLMPrompts.Any())
                    {
                        CurrentVLMPrompt1 = VLMPrompts.First().Content;
                        CurrentVLMPrompt2 = VLMPrompts.Count > 1 ? VLMPrompts.ElementAt(1).Content : VLMPrompts.First().Content;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                // 如果配置文件加载失败，使用默认设置
                if (LLMPrompts.Any())
                {
                    CurrentLLMPrompt1 = LLMPrompts.First().Content;
                    CurrentLLMPrompt2 = LLMPrompts.Count > 1 ? LLMPrompts.ElementAt(1).Content : LLMPrompts.First().Content;
                }
                if (VLMPrompts.Any())
                {
                    CurrentVLMPrompt1 = VLMPrompts.First().Content;
                    CurrentVLMPrompt2 = VLMPrompts.Count > 1 ? VLMPrompts.ElementAt(1).Content : VLMPrompts.First().Content;
                }
            }
        }

        private async Task LoadLLMPromptsAsync()
        {
            LLMPrompts.Clear();

            if (!Directory.Exists(_llmPromptsDirectory))
                return;

            var files = Directory.GetFiles(_llmPromptsDirectory, "*.txt")
                                .OrderBy(f => f)
                                .ToList();

            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    LLMPrompts.Add(new PromptItem
                    {
                        Name = fileName,
                        Content = content,
                        FilePath = file
                    });
                }
                catch (Exception ex)
                {
                    // Log error if needed
                    Console.WriteLine($"Error loading LLM prompt {file}: {ex.Message}");
                }
            }

        }

        private async Task LoadVLMPromptsAsync()
        {
            VLMPrompts.Clear();

            if (!Directory.Exists(_vlmPromptsDirectory))
                return;

            var files = Directory.GetFiles(_vlmPromptsDirectory, "*.txt")
                                .OrderBy(f => f)
                                .ToList();

            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var fileName = Path.GetFileNameWithoutExtension(file);

                    VLMPrompts.Add(new PromptItem
                    {
                        Name = fileName,
                        Content = content,
                        FilePath = file
                    });
                }
                catch (Exception ex)
                {
                    // Log error if needed
                    Console.WriteLine($"Error loading VLM prompt {file}: {ex.Message}");
                }
            }

        }

        public void SetCurrentLLMPrompt1(PromptItem prompt)
        {
            if (prompt != null)
            {
                CurrentLLMPrompt1 = prompt.Content;
            }
        }

        public void SetCurrentLLMPrompt2(PromptItem prompt)
        {
            if (prompt != null)
            {
                CurrentLLMPrompt2 = prompt.Content;
            }
        }

        public void SetCurrentVLMPrompt1(PromptItem prompt)
        {
            if (prompt != null)
            {
                CurrentVLMPrompt1 = prompt.Content;
            }
        }

        public void SetCurrentVLMPrompt2(PromptItem prompt)
        {
            if (prompt != null)
            {
                CurrentVLMPrompt2 = prompt.Content;
            }
        }
     
        public async Task SaveConfigAsync()
        {
            try
            {
                var config = new List<string>();
                
                // 找到当前提示词对应的文件名
                var llmPrompt1File = LLMPrompts.FirstOrDefault(p => p.Content == CurrentLLMPrompt1)?.Name;
                var llmPrompt2File = LLMPrompts.FirstOrDefault(p => p.Content == CurrentLLMPrompt2)?.Name;
                var vlmPrompt1File = VLMPrompts.FirstOrDefault(p => p.Content == CurrentVLMPrompt1)?.Name;
                var vlmPrompt2File = VLMPrompts.FirstOrDefault(p => p.Content == CurrentVLMPrompt2)?.Name;

                if (!string.IsNullOrEmpty(llmPrompt1File))
                    config.Add($"LLMPrompt1={llmPrompt1File}.txt");
                if (!string.IsNullOrEmpty(llmPrompt2File))
                    config.Add($"LLMPrompt2={llmPrompt2File}.txt");
                if (!string.IsNullOrEmpty(vlmPrompt1File))
                    config.Add($"VLMPrompt1={vlmPrompt1File}.txt");
                if (!string.IsNullOrEmpty(vlmPrompt2File))
                    config.Add($"VLMPrompt2={vlmPrompt2File}.txt");

                // 获取源文件路径，而不是输出目录路径
                var sourceConfigPath = GetSourceConfigPath();
                await File.WriteAllLinesAsync(sourceConfigPath, config);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving config: {ex.Message}");
            }
        }

        private string GetSourceConfigPath()
        {
            // 获取当前执行程序的路径
            var currentPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directory = Path.GetDirectoryName(currentPath);
            
            // 向上导航到项目根目录，然后找到源文件
            var projectRoot = directory;
            
            // 如果我们在 bin/Debug 或 bin/Release 目录中，向上导航到项目根目录
            while (projectRoot != null &&
                   (projectRoot.Contains("bin") || projectRoot.Contains("Debug") || projectRoot.Contains("Release")))
            {
                projectRoot = Path.GetDirectoryName(projectRoot);
            }
            
            // 构建源文件路径
            var sourcePath = Path.Combine(projectRoot ?? directory, "Prompts", "PromptConfig.txt");
            
            // 如果源文件存在，使用它；否则回退到原来的路径
            return File.Exists(sourcePath) ? sourcePath : _configFilePath;
        }

        private string GetPromptContent(Dictionary<string, string> config, string configKey, ObservableCollection<PromptItem> prompts, int defaultIndex)
        {
            if (config.ContainsKey(configKey) && prompts.Any(p => p.Name + ".txt" == config[configKey]))
            {
                return prompts.First(p => p.Name + ".txt" == config[configKey]).Content;
            }
            else if (prompts.Count > defaultIndex)
            {
                return prompts.ElementAt(defaultIndex).Content;
            }
            else if (prompts.Any())
            {
                return prompts.First().Content;
            }
            return string.Empty;
        }
    }
}
