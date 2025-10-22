using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Stylet;
using StyletIoC;
using System;
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;
using IContainer = StyletIoC.IContainer;

namespace Rouyan.Pages.ViewModel;

public class TerminalAgentViewModel : Screen
{
    private readonly IContainer _container;
    private readonly IWindowManager _windowManager;

    public TerminalAgentViewModel(IContainer container,IWindowManager windowManager)
    {
        _container = container;
        _windowManager = windowManager;
    }

#pragma warning disable MEAI001
    private string _inputText = "获取https://learn.microsoft.com/en-us/agent-framework/tutorials/workflows/simple-sequential-workflow?pivots=programming-language-csharp的内容 将其主体部分翻译为中文 并形成一份md文档 保存至目录C:\\Users\\Maxwell\\Desktop\\2025.10";
    public string InputText
    {
        get => _inputText;
        set => SetAndNotify(ref _inputText, value);
    }

    private string _outputText = string.Empty;
    public string OutputText
    {
        get => _outputText;
        set => SetAndNotify(ref _outputText, value);
    }

    private CancellationTokenSource? _cts;
    private AgentThread thread;
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetAndNotify(ref _isRunning, value))
            {
                NotifyOfPropertyChange(nameof(CanRun));
                NotifyOfPropertyChange(nameof(CanCancel));
            }
        }
    }

    public bool CanRun => !_isRunning;
    public bool CanCancel => _isRunning;

    #region AI Function
    [Description("Execute a Windows cmd.exe script and return its output.")]
    static string ExecuteCmd([Description("The script content to run via 'cmd.exe /c'.")] string script)
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c " + script)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = psi;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    return $"错误: {error.Trim()}";
                }

                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            return $"执行失败: {ex.Message}";
        }
    }

    [Description("获取指定网页的文本内容")]
    static async Task<string> GetWebPageContent([Description("要获取内容的网页URL")] string url)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                // 设置请求头模拟浏览器访问
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

                // 获取网页内容
                var htmlContent = await httpClient.GetStringAsync(url);

                // 提取文本内容（简单的HTML标签清理）
                var textContent = ExtractTextFromHtml(htmlContent);

                return textContent;
            }
        }
        catch (Exception ex)
        {
            return $"获取网页内容失败: {ex.Message}";
        }
    }

    [Description("创建一个新文件")]
    static string CreateFile([Description("要创建的文件路径")] string filePath)
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 创建文件
            using (var fileStream = File.Create(filePath))
            {
                // 文件创建后立即关闭，创建空文件
            }

            return $"文件创建成功: {filePath}";
        }
        catch (Exception ex)
        {
            return $"创建文件失败: {ex.Message}";
        }
    }

    [Description("删除指定文件")]
    static string DeleteFile([Description("要删除的文件路径")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return $"文件不存在: {filePath}";
            }

            File.Delete(filePath);
            return $"文件删除成功: {filePath}";
        }
        catch (Exception ex)
        {
            return $"删除文件失败: {ex.Message}";
        }
    }

    [Description("向指定文件写入内容")]
    static string WriteToFile([Description("要写入的文件路径")] string filePath,
                              [Description("要写入的内容")] string content,
                              [Description("写入模式：'overwrite'覆盖文件，'append'追加到文件末尾，默认为overwrite")] string mode = "overwrite")
    {
        try
        {
            // 确保目录存在
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 确定写入模式
            var isAppend = mode.ToLower() == "append";

            // 写入文件
            using (var writer = new StreamWriter(filePath, isAppend, Encoding.UTF8))
            {
                writer.Write(content);
            }

            return $"内容写入成功: {filePath} (模式: {mode})";
        }
        catch (Exception ex)
        {
            return $"写入文件失败: {ex.Message}";
        }
    }

    [Description("读取指定文件的内容")]
    static string ReadFile([Description("要读取的文件路径")] string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return $"文件不存在: {filePath}";
            }

            string content = File.ReadAllText(filePath, Encoding.UTF8);
            return content;
        }
        catch (Exception ex)
        {
            return $"读取文件失败: {ex.Message}";
        }
    }

    [Description("检查文件或目录是否存在")]
    static string CheckPathExists([Description("要检查的文件或目录路径")] string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return $"文件存在: {path}";
            }
            else if (Directory.Exists(path))
            {
                return $"目录存在: {path}";
            }
            else
            {
                return $"路径不存在: {path}";
            }
        }
        catch (Exception ex)
        {
            return $"检查路径失败: {ex.Message}";
        }
    }

    [Description("创建目录")]
    static string CreateDirectory([Description("要创建的目录路径")] string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                return $"目录已存在: {directoryPath}";
            }

            Directory.CreateDirectory(directoryPath);
            return $"目录创建成功: {directoryPath}";
        }
        catch (Exception ex)
        {
            return $"创建目录失败: {ex.Message}";
        }
    }
    #endregion

    public async Task Run()
    {
        if (IsRunning) return;

        OutputText = string.Empty;
        _cts = new CancellationTokenSource();
        IsRunning = true;

        WaitingViewModel? waitingVm = null;

        try
        {
            // 显示等待窗体          
            waitingVm = _container.Get<WaitingViewModel>();
            waitingVm.Text = "正在分析请求，请稍候...";
            _windowManager.ShowWindow(waitingVm);

            // 配置AI Agent
            DotEnv.Load();
            var envVars = DotEnv.Read();

            var apiKey = envVars["OPENAI_API_KEY"];
            var model = envVars["OPENAI_CHAT_MODEL"];
            var baseUrl = new Uri(envVars["OPENAI_BASE_URL"]);

            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);

            OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
            openAIClientOptions.Endpoint = baseUrl;

            AIAgent agent = new OpenAIClient(apiKeyCredential, openAIClientOptions)
                .GetChatClient(model)
                .CreateAIAgent(instructions: "你是一个乐于助人的助手，可以执行命令行脚本、获取网页内容，以及进行文件操作（创建、删除、读写文件等）。请使用中文回答。",
                tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ExecuteCmd)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(GetWebPageContent)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CreateFile)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(DeleteFile)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(WriteToFile)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ReadFile)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CheckPathExists)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(CreateDirectory))
                        ]);
          
            if (thread == null)
            {
                thread = agent.GetNewThread();
            }

            var response = await agent.RunAsync(InputText, thread);
            var userInputRequests = response.UserInputRequests.ToList();

            // 关闭等待窗体
            waitingVm?.RequestClose();

            while (userInputRequests.Count > 0)
            {
                if (_cts?.IsCancellationRequested == true) break;

                var userInputResponses = new List<ChatMessage>();

                foreach (var functionApprovalRequest in userInputRequests.OfType<FunctionApprovalRequestContent>())
                {
                    var functionName = functionApprovalRequest.FunctionCall.Name;
                    var args = functionApprovalRequest.FunctionCall.Arguments;

                    string GetArg(string key, string fallback = "")
                    {
                        if (args != null && args.TryGetValue(key, out var value) && value != null)
                            return value.ToString();
                        return fallback;
                    }

                    string functionInfo;
                    switch (functionName)
                    {
                        case "ExecuteCmd":
                        {
                            var script = GetArg("script", string.Empty);
                            functionInfo = $"执行命令脚本：\n{script}";
                            break;
                        }
                        case "GetWebPageContent":
                        {
                            var webUrl = GetArg("url", string.Empty);
                            functionInfo = $"获取网页内容：\n{webUrl}";
                            break;
                        }
                        case "CreateFile":
                        {
                            var filePath = GetArg("filePath", string.Empty);
                            functionInfo = $"创建文件：\n{filePath}";
                            break;
                        }
                        case "DeleteFile":
                        {
                            var filePath = GetArg("filePath", string.Empty);
                            functionInfo = $"删除文件：\n{filePath}";
                            break;
                        }
                        case "WriteToFile":
                        {
                            var filePath = GetArg("filePath", string.Empty);
                            var content = GetArg("content", string.Empty);
                            var mode = GetArg("mode", "overwrite");
                            functionInfo = $"写入文件：\n路径: {filePath}\n模式: {mode}\n内容预览: {(content.Length > 100 ? content.Substring(0, 100) + "..." : content)}";
                            break;
                        }
                        case "ReadFile":
                        {
                            var filePath = GetArg("filePath", string.Empty);
                            functionInfo = $"读取文件：\n{filePath}";
                            break;
                        }
                        case "CheckPathExists":
                        {
                            var path = GetArg("path", string.Empty);
                            functionInfo = $"检查路径是否存在：\n{path}";
                            break;
                        }
                        case "CreateDirectory":
                        {
                            var directoryPath = GetArg("directoryPath", string.Empty);
                            functionInfo = $"创建目录：\n{directoryPath}";
                            break;
                        }
                        default:
                        {
                            functionInfo = $"函数：{functionName}";
                            break;
                        }
                    }

                    var dialogVm = new HumanApprovalDialogViewModel
                    {
                        Title = "函数调用审批",
                        Message = $"是否同意以下操作？\n\n{functionInfo}"
                    };

                    bool? result = _windowManager.ShowDialog(dialogVm);
                    bool approved = result == true;
                    userInputResponses.Add(new ChatMessage(ChatRole.User, [functionApprovalRequest.CreateResponse(approved)]));
                }

                // Pass the user input responses back to the agent for further processing.
                response = await agent.RunAsync(userInputResponses, thread);
                userInputRequests = response.UserInputRequests.ToList();
            }

            if (_cts?.IsCancellationRequested == true)
            {
                OutputText += "\n已取消运行。";
                return;
            }

            // Invoke the agent with streaming support, honoring cancellation.
            await foreach (var update in agent.RunStreamingAsync("输出最终答案", thread).WithCancellation(_cts!.Token))
            {
                OutputText += update.Text;
            }
        }
        catch (OperationCanceledException)
        {
            OutputText += "\n已取消运行。";
        }
        finally
        {
            waitingVm?.RequestClose();
            IsRunning = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
    }

    public void ClearContext()
    {
        OutputText = string.Empty;
        thread = null;
    }

    public async Task Test()
    {
        //var dialogVm = new HumanApprovalDialogViewModel
        //{
        //    Title = "函数调用审批",
        //    Message = $"是否同意这个操作？"
        //};

        //bool? result = _windowManager.ShowDialog(dialogVm);

        //// 显示等待窗体          
        //var waitingVm = _container.Get<WaitingViewModel>();
        //waitingVm.Text = "正在分析请求，请稍候...";
        //_windowManager.ShowWindow(waitingVm);

        //await Task.Delay(5000);

        //waitingVm.RequestClose();

        // 1. 基本用法 - 只显示消息
        _windowManager.ShowMessageBox("你好");

        // 2. 带标题的消息框
        _windowManager.ShowMessageBox("操作完成", "提示");

        // 3. 带确认和取消按钮的消息框
        var result1 = _windowManager.ShowMessageBox("确定要删除这个文件吗？", "确认删除",
            MessageBoxButton.OKCancel, MessageBoxImage.Question);

        // 4. 带是/否/取消按钮和警告图标的消息框
        var result2 = _windowManager.ShowMessageBox("文件已修改，是否保存？", "保存确认",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);

        // 5. 带自定义按钮标签的消息框 (使用YesNoCancel按钮以展示更多选项)
        var customButtons = new Dictionary<MessageBoxResult, string>
        {
            { MessageBoxResult.Yes, "继续" },
            { MessageBoxResult.No, "停止" },
            { MessageBoxResult.Cancel, "取消" }
        };
        var result3 = _windowManager.ShowMessageBox("检测到潜在风险，是否继续操作？", "安全警告",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation, MessageBoxResult.No, MessageBoxResult.Cancel, customButtons);

        // 6. 带文本对齐和流方向的消息框
        _windowManager.ShowMessageBox("这是一个从右到左显示的消息框文本", "RTL示例",
            MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None, MessageBoxResult.None,
            null, FlowDirection.RightToLeft, TextAlignment.Center);

        // 7. 完整参数示例
        var fullResult = _windowManager.ShowMessageBox(
            "这是一个完整的消息框示例，包含了所有参数的使用",
            "完整示例",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information,
            MessageBoxResult.OK,
            MessageBoxResult.Cancel,
            null,
            FlowDirection.LeftToRight,
            TextAlignment.Left);
    }



    private static string ExtractTextFromHtml(string html)
    {
        // 移除脚本和样式标签
        html = Regex.Replace(html, "<script[^>]*>.*?</script>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        html = Regex.Replace(html, "<style[^>]*>.*?</style>", "", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        // 移除HTML标签
        html = Regex.Replace(html, "<[^>]+>", " ");

        // 清理多余的空白字符
        html = Regex.Replace(html, @"\s+", " ");
        html = html.Trim();

        return html;
    }
}