using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Rouyan.Services;
using System.Windows;

namespace Rouyan.Pages.ViewModel
{
    public class PromptManagementViewModel : Screen
    {
        private readonly PromptManagementService _promptService;

        private string _currentLLMPrompt = string.Empty;
        private string _currentVLMPrompt = string.Empty;
        private PromptItem? _selectedLLMPrompt;
        private PromptItem? _selectedVLMPrompt;

        public PromptManagementViewModel(PromptManagementService promptService)
        {
            _promptService = promptService;           
        }

        public string CurrentLLMPrompt
        {
            get => _currentLLMPrompt;
            set => SetAndNotify(ref _currentLLMPrompt, value);
        }

        public string CurrentVLMPrompt
        {
            get => _currentVLMPrompt;
            set => SetAndNotify(ref _currentVLMPrompt, value);
        }

        public ObservableCollection<PromptItem> LLMPrompts => _promptService.LLMPrompts;
        public ObservableCollection<PromptItem> VLMPrompts => _promptService.VLMPrompts;

        public PromptItem? SelectedLLMPrompt
        {
            get => _selectedLLMPrompt;
            set
            {
                SetAndNotify(ref _selectedLLMPrompt, value);              
            }
        }

        public PromptItem? SelectedVLMPrompt
        {
            get => _selectedVLMPrompt;
            set
            {
                SetAndNotify(ref _selectedVLMPrompt, value);             
            }
        }
  
        protected override async void OnInitialActivate()
        {
            base.OnInitialActivate();
            await LoadPromptsAsync();
        }

        private async Task LoadPromptsAsync()
        {
            try
            {
                await _promptService.LoadPromptsAsync();

                CurrentLLMPrompt = _promptService.CurrentLLMPrompt;
                CurrentVLMPrompt = _promptService.CurrentVLMPrompt;

                if (LLMPrompts.Any())
                {
                    SelectedLLMPrompt = LLMPrompts.First();
                }

                if (VLMPrompts.Any())
                {
                    SelectedVLMPrompt = VLMPrompts.First();
                }
            }
            catch (Exception ex)
            {
                // Log error or show message to user
                Console.WriteLine($"Error loading prompts: {ex.Message}");
            }
        }

        public void SetCurrentLLMPrompt()
        {
            if (SelectedLLMPrompt != null)
            {
                _promptService.SetCurrentLLMPrompt(SelectedLLMPrompt);
                CurrentLLMPrompt = _promptService.CurrentLLMPrompt;
                MessageBox.Show("设置当前LLM提示词成功！！");
            }
        }

        public void SetCurrentVLMPrompt()
        {
            if (SelectedVLMPrompt != null)
            {
                _promptService.SetCurrentVLMPrompt(SelectedVLMPrompt);
                CurrentVLMPrompt = _promptService.CurrentVLMPrompt;
                MessageBox.Show("设置当前VLM提示词成功！！");
            }
        }    
    } 
}
