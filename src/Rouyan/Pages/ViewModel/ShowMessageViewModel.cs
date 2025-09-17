using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rouyan.Pages.ViewModel
{
    public class ShowMessageViewModel : Screen
    {
        public ShowMessageViewModel()
        {
            Title = "翻译窗口";
        }

        private string _titile = string.Empty;
        public string Title
        {
            get => _titile;
            set => SetAndNotify(ref _titile, value);
        }

        private string _text = string.Empty;
        public string Text
        {
            get => _text;
            set => SetAndNotify(ref _text, value);
        }
    }
}
