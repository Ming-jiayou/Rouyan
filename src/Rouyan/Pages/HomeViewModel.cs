using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Stylet;
using System.IO;
using System.Windows.Interop;
using System.Drawing;
using System.Drawing.Imaging;

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
    
    public void TestMethod()
    {
      
    }

    public void SelectFile()
    {
        try
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择文件",
                Filter = "所有文件 (*.*)|*.*",
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
            }
        }
        catch (Exception ex)
        {           
            MessageBox.Show($"选择文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string _selectedFilePath = string.Empty;
    public string SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            if (_selectedFilePath != value)
            {
                _selectedFilePath = value;
                NotifyOfPropertyChange();
            }
        }
    }
    
    private BitmapSource? _clipboardImage;
    public BitmapSource? ClipboardImage
    {
        get => _clipboardImage;
        set
        {
            _clipboardImage = value;
            NotifyOfPropertyChange();
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
            // 清除之前的内容
            ClipboardText = string.Empty;
            ClipboardImage = null;

            if (Clipboard.ContainsText())
            {
                string clipboardText = Clipboard.GetText();
                ClipboardText = clipboardText;
                MessageBox.Show("剪贴板文本内容已获取", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Clipboard.ContainsImage())
            {
                var image = Clipboard.GetImage();
                ClipboardImage = image;
                ClipboardText = "[剪贴板中包含图片内容]";
                MessageBox.Show("剪贴板图片内容已获取", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (Clipboard.ContainsFileDropList())
            {
                var files = Clipboard.GetFileDropList();
                string fileList = string.Join("\n", files.Cast<string>());
                ClipboardText = $"[文件列表]\n{fileList}";
                // 如果第一个文件是图片，尝试显示缩略图
                if (files.Count > 0 && IsImageFile(files[0]))
                {
                    try
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.UriSource = new Uri(files[0]);
                        bitmapImage.DecodePixelWidth = 280;
                        bitmapImage.EndInit();
                        ClipboardImage = bitmapImage;
                    }
                    catch
                    {
                        ClipboardImage = null;
                    }
                }
            }
            else
            {
                ClipboardText = "[剪贴板为空或无支持的格式]";
            }
        }
        catch (Exception ex)
        {
            ClipboardText = $"[获取剪贴板内容失败: {ex.Message}]";
            ClipboardImage = null;
            MessageBox.Show($"获取剪贴板内容失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool IsImageFile(string filePath)
    {
        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || 
               extension == ".bmp" || extension == ".gif" || extension == ".tiff" || 
               extension == ".ico";
    }
}