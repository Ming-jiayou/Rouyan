using System.Windows.Controls;
using System.Windows.Input;
using System.Threading.Tasks;
using Rouyan.Pages.ViewModel;

namespace Rouyan.Pages.View
{
    public partial class TerminalAgentView : UserControl
    {
        public TerminalAgentView()
        {
            InitializeComponent();
        }

        private async void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                if (DataContext is TerminalAgentViewModel vm)
                {
                    e.Handled = true;
                    await vm.Run();
                }
            }
        }
    }
}