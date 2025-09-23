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
using Rouyan.Models;
using Stylet;

namespace Rouyan.Pages.ViewModel
{
    public class PromptManagementViewModel : Screen
    {
        private readonly PromptManagementService _promptService;

        private string _currentLLMPrompt1 = string.Empty;
        private string _currentLLMPrompt2 = string.Empty;
        private string _currentVLMPrompt1 = string.Empty;
        private string _currentVLMPrompt2 = string.Empty;
        private PromptItem? _selectedLLMPrompt;
        private PromptItem? _selectedVLMPrompt;


        public PromptManagementViewModel(PromptManagementService promptService)
        {
            _promptService = promptService;
        }

        public string CurrentLLMPrompt1
        {
            get => _currentLLMPrompt1;
            set => SetAndNotify(ref _currentLLMPrompt1, value);
        }

        public string CurrentLLMPrompt2
        {
            get => _currentLLMPrompt2;
            set => SetAndNotify(ref _currentLLMPrompt2, value);
        }

        public string CurrentVLMPrompt1
        {
            get => _currentVLMPrompt1;
            set => SetAndNotify(ref _currentVLMPrompt1, value);
        }

        public string CurrentVLMPrompt2
        {
            get => _currentVLMPrompt2;
            set => SetAndNotify(ref _currentVLMPrompt2, value);
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

                CurrentLLMPrompt1 = _promptService.CurrentLLMPrompt1;
                CurrentLLMPrompt2 = _promptService.CurrentLLMPrompt2;
                CurrentVLMPrompt1 = _promptService.CurrentVLMPrompt1;
                CurrentVLMPrompt2 = _promptService.CurrentVLMPrompt2;

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

        public async void SetCurrentLLMPrompt1()
        {
            if (SelectedLLMPrompt != null)
            {
                _promptService.SetCurrentLLMPrompt1(SelectedLLMPrompt);
                CurrentLLMPrompt1 = _promptService.CurrentLLMPrompt1;
                await _promptService.SaveConfigAsync();
                MessageBox.Show("设置LLM提示词1成功！");
            }
        }

        public async void SetCurrentLLMPrompt2()
        {
            if (SelectedLLMPrompt != null)
            {
                _promptService.SetCurrentLLMPrompt2(SelectedLLMPrompt);
                CurrentLLMPrompt2 = _promptService.CurrentLLMPrompt2;
                await _promptService.SaveConfigAsync();
                MessageBox.Show("设置LLM提示词2成功！");
            }
        }

        public async void SetCurrentVLMPrompt1()
        {
            if (SelectedVLMPrompt != null)
            {
                _promptService.SetCurrentVLMPrompt1(SelectedVLMPrompt);
                CurrentVLMPrompt1 = _promptService.CurrentVLMPrompt1;
                await _promptService.SaveConfigAsync();
                MessageBox.Show("设置VLM提示词1成功！");
            }
        }

        public async void SetCurrentVLMPrompt2()
        {
            if (SelectedVLMPrompt != null)
            {
                _promptService.SetCurrentVLMPrompt2(SelectedVLMPrompt);
                CurrentVLMPrompt2 = _promptService.CurrentVLMPrompt2;
                await _promptService.SaveConfigAsync();
                MessageBox.Show("设置VLM提示词2成功！");
            }
        }
    } 
}
