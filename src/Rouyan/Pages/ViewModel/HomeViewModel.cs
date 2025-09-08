using dotenv.net;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Images;
using Stylet;
using System;
using System.ClientModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

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
    
    public async Task TranslateToChinese()
    {
        try
        {
            // 获取剪切板文本 - 确保在UI线程上执行
            string clipboardText = string.Empty;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ClipboardText = string.Empty;
                if (Clipboard.ContainsText())
                {
                    clipboardText = Clipboard.GetText();
                    ClipboardText = clipboardText;
                }
            });

            if (string.IsNullOrEmpty(clipboardText))
            {
                MessageBox.Show("剪贴板中没有文本内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 使用大语言模型翻译文本
            DotEnv.Load();
            var envVars = DotEnv.Read();
            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(envVars["OPENAI_API_KEY"]);

            OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
            openAIClientOptions.Endpoint = new Uri(envVars["OPENAI_BASE_URL"]);

            IChatClient client =
                           new OpenAI.Chat.ChatClient(envVars["OPENAI_CHAT_MODEL"], apiKeyCredential, openAIClientOptions)
                           .AsIChatClient();

            // Note: To use the ChatClientBuilder you need to install the Microsoft.Extensions.AI package
            var ChatClient = new ChatClientBuilder(client)
                 .UseFunctionInvocation()
                 .Build();
            IList<Microsoft.Extensions.AI.ChatMessage> Messages =
               [
                   // Add a system message
                   new(ChatRole.System, """
                   你是一个中文翻译助手，你可以将用户输入的中文翻译为英文。
                   输入：你今天怎么样
                   输出：How are you today?
                   """),
                ];


            Messages.Add(new(ChatRole.User, ClipboardText));


            var response = await ChatClient.GetResponseAsync(Messages);

            // 添加到选择的文件中
            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                try
                {                
                    // 追加写入文件
                    await File.AppendAllTextAsync(SelectedFilePath, response.Text + Environment.NewLine);
                    
                    // 在UI线程上显示消息
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"翻译结果已添加到文件：{SelectedFilePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"写入文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("请先选择要写入的文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"执行操作时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    public async Task TranslateToMarkDownTable()
    {
        try
        {
            // 获取剪切板图片 - 确保在UI线程上执行
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ClipboardImage = null;
                if (Clipboard.ContainsImage())
                {
                    var image = Clipboard.GetImage();
                    ClipboardImage = image;                 
                }
            });
            if (ClipboardImage == null)
            {
                MessageBox.Show("剪贴板中没有图片内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 使用大语言模型翻译文本
            DotEnv.Load();
            var envVars = DotEnv.Read();
            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(envVars["OPENAI_API_KEY"]);

            OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
            openAIClientOptions.Endpoint = new Uri(envVars["OPENAI_BASE_URL"]);

            IChatClient client =
                           new OpenAI.Chat.ChatClient(envVars["OPENAI_VISION_MODEL"], apiKeyCredential, openAIClientOptions)
                           .AsIChatClient();

            // Note: To use the ChatClientBuilder you need to install the Microsoft.Extensions.AI package
            var ChatClient = new ChatClientBuilder(client)
                 .UseFunctionInvocation()
                 .Build();
            IList<Microsoft.Extensions.AI.ChatMessage> Messages =
               [
                   // Add a system message
                   new(ChatRole.System, 
                   """
                   你是一个md表格翻译助手，你可以将用户输入的英文图表，翻译之后，形成md表格。
                   一个md表格例子：
                   | 列1    | 列2    | 列3    |
                   |--------|--------|--------|
                   | 数据1  | 数据2  | 数据3  |
                   | 数据4  | 数据5  | 数据6  |
                   | 数据7  | 数据8  | 数据9  |
                   """),
                ];



            var message = new ChatMessage(ChatRole.User,
             """
             根据我输入的英文图表，翻译之后，形成md表格。         
             """);

            // 在UI线程中将剪贴板图片转换为byte[]
            byte[] data = await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                return BitmapSourceToByteArray(ClipboardImage);
            });
            message.Contents.Add(new DataContent(data, "image/jpeg"));
            Messages.Add(message);

            var response = await ChatClient.GetResponseAsync(Messages);

            // 添加到选择的文件中
            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                try
                {
                    // 追加写入文件
                    await File.AppendAllTextAsync(SelectedFilePath, response.Text + Environment.NewLine);

                    // 在UI线程上显示消息
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"翻译结果已添加到文件：{SelectedFilePath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
                catch (Exception ex)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"写入文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show("请先选择要写入的文件", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }
        catch (Exception ex)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show($"执行操作时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    public void ExecuteTranslation()
    {
        MessageBox.Show("翻译功能正在开发中...", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private byte[] BitmapSourceToByteArray(BitmapSource bitmapSource)
    {
        if (bitmapSource == null)
            return Array.Empty<byte>();

        // 编码BitmapSource为JPEG格式
        JpegBitmapEncoder encoder = new JpegBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
        
        using (MemoryStream ms = new MemoryStream())
        {
            encoder.Save(ms);
            return ms.ToArray();
        }
    }
}