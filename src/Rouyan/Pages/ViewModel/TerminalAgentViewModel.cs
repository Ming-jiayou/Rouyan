using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using Stylet;
using System;
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Rouyan.Pages.ViewModel;

public class TerminalAgentViewModel : Screen
{
    private readonly IWindowManager _windowManager;

    public TerminalAgentViewModel(IWindowManager windowManager)
    {
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
        OutputText = string.Empty;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("未设置环境变量：OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASEURL") ?? throw new InvalidOperationException("未设置环境变量：OPENAI_BASEURL");

        ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);

        OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
        openAIClientOptions.Endpoint = new Uri(baseUrl);

        AIAgent agent = new OpenAIClient(apiKeyCredential, openAIClientOptions)
            .GetChatClient(model)
            .CreateAIAgent(instructions: "你是一个乐于助人的助手，可以执行命令行脚本。请使用中文回答。", tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ExecuteCmd))]);

        // Call the agent and check if there are any user input requests to handle.
        AgentThread thread = agent.GetNewThread();

        var response = await agent.RunAsync(InputText, thread);
        OutputText += response.Text;
        var userInputRequests = response.UserInputRequests.ToList();

        // For streaming use:
        // var updates = await agent.RunStreamingAsync(InputText, thread).ToListAsync();

        while (userInputRequests.Count > 0)
        {
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

        // Invoke the agent with streaming support.
        await foreach (var update in agent.RunStreamingAsync("输出最终回答",thread))
        {
            OutputText += update.Text;
        }
    }
}