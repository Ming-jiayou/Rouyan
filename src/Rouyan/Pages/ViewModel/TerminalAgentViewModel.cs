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
                .CreateAIAgent(instructions: "你是一个乐于助人的助手，可以执行命令行脚本。请使用中文回答。", tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ExecuteCmd))]);

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
                    var scriptContent = functionApprovalRequest.FunctionCall.Arguments?["script"]?.ToString() ?? "未知脚本";
                    var functionName = functionApprovalRequest.FunctionCall.Name;

                    var dialogVm = new HumanApprovalDialogViewModel
                    {
                        Title = "命令执行审批",
                        Message = $"是否同意执行以下命令？\n\n函数名称: {functionName}\n脚本内容: {scriptContent}"
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
}