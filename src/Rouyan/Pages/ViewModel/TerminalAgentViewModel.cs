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
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
    private string _inputText = "获取当前时间";
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

    public void Cancel()
    {
        _cts?.Cancel();
    }

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
                .CreateAIAgent(instructions: "你是一个乐于助人的助手，可以执行命令行脚本也可以获取网页内容。请使用中文回答。",
                tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ExecuteCmd)),
                        new ApprovalRequiredAIFunction(AIFunctionFactory.Create(GetWebPageContent))]);

            // Call the agent and check if there are any user input requests to handle.
            AgentThread thread = agent.GetNewThread();

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