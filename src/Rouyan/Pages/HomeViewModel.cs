using System;
using System.Windows;
using Stylet;

namespace Rouyan.Pages;

public class HomeViewModel : Screen
{
    private readonly INavigationController navigationController;
    private string _clipboardText = string.Empty;

    public string ClipboardText
    {
        get => _clipboardText;
        set
        {
            if (_clipboardText != value)
            {
                _clipboardText = value;
                NotifyOfPropertyChange();
            }
        }
    }

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
                ClipboardText = clipboardText;
                MessageBox.Show($"剪贴板内容已获取", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Clipboard.ContainsImage())
            {
                ClipboardText = "[剪贴板中包含图片内容]";
                MessageBox.Show("剪贴板中包含图片内容（无法以文本方式显示）", "剪贴板内容", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                string fileList = string.Join("\n", files.Cast<string>());
                ClipboardText = $"[文件列表]\n{fileList}";
            }
            else
            {
                ClipboardText = "[剪贴板为空或无支持的格式]";
                MessageBox.Show("剪贴板为空或不包含可识别的内容类型", "剪贴板内容", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            ClipboardText = $"[获取剪贴板内容失败: {ex.Message}]";
            MessageBox.Show($"获取剪贴板内容失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}