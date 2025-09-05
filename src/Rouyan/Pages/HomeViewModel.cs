using System;
using System.Windows;
using Stylet;

namespace Rouyan.Pages;

public class HomeViewModel : Screen
{
    private readonly INavigationController navigationController;
    
    public HomeViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }
    
    public void NavigateToAbout()
    {
        this.navigationController.NavigateToAbout();
    }
    
    public void NavigateToSettings()
    {
        this.navigationController.NavigateToSettings();
    }
    
    public void GetClipboardContent()
    {
        try
        {
            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                MessageBox.Show($"剪贴板内容：\n{clipboardText}", "剪贴板内容", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Clipboard.ContainsImage())
            {
                MessageBox.Show("剪贴板中包含图片内容（非文本）", "剪贴板内容", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                string fileList = string.Join("\n", files.Cast<string>());
                MessageBox.Show($"剪贴板包含文件：\n{fileList}", "剪贴板内容", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("剪贴板为空或不包含可识别的内容类型", "剪贴板内容", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"获取剪贴板内容失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}