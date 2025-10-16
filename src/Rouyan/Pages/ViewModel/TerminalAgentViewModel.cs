using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using Stylet;
using System;
using System.ClientModel;
using System.ComponentModel;
using System.Diagnostics;

namespace Rouyan.Pages.ViewModel;

public class TerminalAgentViewModel : Screen
{
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
                    return $"����: {error.Trim()}";
                }

                return output.Trim();
            }
        }
        catch (Exception ex)
        {
            return $"ִ��ʧ��: {ex.Message}";
        }
    }

    public async Task Run()
    {
        OutputText = string.Empty;
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("δ���û���������OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o-mini";
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASEURL") ?? throw new InvalidOperationException("δ���û���������OPENAI_BASEURL");

        ApiKeyCredential apiKeyCredential = new ApiKeyCredential(apiKey);

        OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
        openAIClientOptions.Endpoint = new Uri(baseUrl);

        AIAgent agent = new OpenAIClient(apiKeyCredential, openAIClientOptions)
            .GetChatClient(model)
            .CreateAIAgent(instructions: "����һ���������˵����֣���ʹ�����Ļش�", tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ExecuteCmd))]);

        // Call the agent and check if there are any user input requests to handle.
        AgentThread thread = agent.GetNewThread();

        var response = await agent.RunAsync(InputText, thread);
        OutputText += response.Text;
        var userInputRequests = response.UserInputRequests.ToList();

        // For streaming use:
        // var updates = await agent.RunStreamingAsync(InputText, thread).ToListAsync();

        while (userInputRequests.Count > 0)
        {
            // ���ڶԻ������Ϊ������
            var userInputResponses = new List<ChatMessage>();

            foreach (var functionApprovalRequest in userInputRequests.OfType<FunctionApprovalRequestContent>())
            {
                //var result = await ShowDialogAsync($"�Ƿ�ͬ����ú�����\n" +
                //                                   $"{functionApprovalRequest.FunctionCall.Name}��\n" +
                //                                   $"�ű����ݣ�" +
                //                                   $"{functionApprovalRequest.FunctionCall.Arguments?["script"]}��");
                bool approved = true;

                Debug.WriteLine($"�û��������: {(approved ? "ͬ��" : "�ܾ�")}");

                userInputResponses.Add(new ChatMessage(ChatRole.User, [functionApprovalRequest.CreateResponse(approved)]));
            }

            // Pass the user input responses back to the agent for further processing.
            response = await agent.RunAsync(userInputResponses, thread);

            userInputRequests = response.UserInputRequests.ToList();

            // For streaming use:
            // updates = await agent.RunStreamingAsync(userInputResponses, thread).ToListAsync();
            // userInputRequests = updates.SelectMany(x => x.UserInputRequests).ToList();
        }

        OutputText = response.ToString();

        // For streaming use:
        // Console.WriteLine($"\nAgent: {updates.ToAgentRunResponse()}");
    }
}