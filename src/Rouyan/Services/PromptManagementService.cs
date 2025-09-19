using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rouyan.Services
{
    public class PromptManagementService
    {
        private readonly string _baseDirectory;
        private readonly string _llmPromptsDirectory;
        private readonly string _vlmPromptsDirectory;

        public PromptManagementService()
        {
            _baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Prompts");
            _llmPromptsDirectory = Path.Combine(_baseDirectory, "LLMPrompts");
            _vlmPromptsDirectory = Path.Combine(_baseDirectory, "VLMPrompts");
        }

        public string CurrentLLMPrompt { get; set; } = string.Empty;
        public string CurrentVLMPrompt { get; set; } = string.Empty;
        public ObservableCollection<PromptItem> LLMPrompts { get; set; } = new ObservableCollection<PromptItem>();
        public ObservableCollection<PromptItem> VLMPrompts { get; set; } = new ObservableCollection<PromptItem>();

        public async Task LoadPromptsAsync()
        {
            await LoadLLMPromptsAsync();
            await LoadVLMPromptsAsync();
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

            if (LLMPrompts.Any() && string.IsNullOrEmpty(CurrentLLMPrompt))
            {
                CurrentLLMPrompt = LLMPrompts.First().Content;
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

            if (VLMPrompts.Any() && string.IsNullOrEmpty(CurrentVLMPrompt))
            {
                CurrentVLMPrompt = VLMPrompts.First().Content;
            }
        }

        public void SetCurrentLLMPrompt(PromptItem prompt)
        {
            if (prompt != null)
            {
                CurrentLLMPrompt = prompt.Content;
            }
        }

        public void SetCurrentVLMPrompt(PromptItem prompt)
        {
            if (prompt != null)
            {
                CurrentVLMPrompt = prompt.Content;
            }
        }
    }

    public class PromptItem
    {
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
