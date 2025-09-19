using Stylet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Rouyan.Services;

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

            SetCurrentLLMPromptCommand = new ActionCommand(SetCurrentLLMPrompt, CanSetCurrentLLMPrompt);
            SetCurrentVLMPromptCommand = new ActionCommand(SetCurrentVLMPrompt, CanSetCurrentVLMPrompt);
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
                (SetCurrentLLMPromptCommand as ActionCommand)?.RaiseCanExecuteChanged();
            }
        }

        public PromptItem? SelectedVLMPrompt
        {
            get => _selectedVLMPrompt;
            set
            {
                SetAndNotify(ref _selectedVLMPrompt, value);
                (SetCurrentVLMPromptCommand as ActionCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand SetCurrentLLMPromptCommand { get; }
        public ICommand SetCurrentVLMPromptCommand { get; }

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

        private void SetCurrentLLMPrompt()
        {
            if (SelectedLLMPrompt != null)
            {
                _promptService.SetCurrentLLMPrompt(SelectedLLMPrompt);
                CurrentLLMPrompt = _promptService.CurrentLLMPrompt;
            }
        }

        private bool CanSetCurrentLLMPrompt()
        {
            return SelectedLLMPrompt != null;
        }

        private void SetCurrentVLMPrompt()
        {
            if (SelectedVLMPrompt != null)
            {
                _promptService.SetCurrentVLMPrompt(SelectedVLMPrompt);
                CurrentVLMPrompt = _promptService.CurrentVLMPrompt;
            }
        }

        private bool CanSetCurrentVLMPrompt()
        {
            return SelectedVLMPrompt != null;
        }
    }

    public class ActionCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public ActionCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute.Invoke();
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
