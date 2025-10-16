using Stylet;
using System;

namespace Rouyan.Pages.ViewModel
{
    public class HumanApprovalDialogViewModel : Screen
    {
        public HumanApprovalDialogViewModel()
        {
            Title = "执行审批";
        }

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set => SetAndNotify(ref _title, value);
        }

        private string _message = string.Empty;
        public string Message
        {
            get => _message;
            set => SetAndNotify(ref _message, value);
        }

        // Approve the action
        public void Approve()
        {
            RequestClose(true);
        }

        // Reject the action
        public void Reject()
        {
            RequestClose(false);
        }
    }
}