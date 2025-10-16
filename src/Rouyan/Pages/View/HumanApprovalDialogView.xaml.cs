using System;
using System.Windows;
using System.Windows.Input;

namespace Rouyan.Pages.View
{
    public partial class HumanApprovalDialogView : Window
    {
        public HumanApprovalDialogView()
        {
            InitializeComponent();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.DialogResult = false;
                this.Close();
                return;
            }
            base.OnKeyDown(e);
        }
    }
}