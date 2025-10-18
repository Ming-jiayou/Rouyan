# 使用Microsoft Agent Framework框架创建一个带有审批功能的终端Agent

## 前言

在AI辅助开发领域，<span style="color: dodgerblue;">Microsoft Agent Framework</span>为我们提供了强大的工具来构建智能Agent。对于需要执行敏感操作（如系统命令）的场景，人工审批机制显得尤为重要。本文以Rouyan为例，说明如何使用Microsoft Agent Framework创建一个能够执行终端命令并具备人工审批功能的WPF应用。

## Microsoft Agent Framework简介

<span style="color: dodgerblue;">Microsoft Agent Framework</span>是微软推出的AI Agent开发框架，它提供了：

*1、AIAgent：核心Agent类，支持工具调用和流式响应*

*2、AgentThread：管理对话上下文和状态*

*3、Function调用：支持自定义函数和工具集成*

*4、审批机制：内置的人工审批流程支持*

Microsoft Agent Framework项目地址：*https://github.com/microsoft/agents*

## 核心架构设计

Rouyan中的终端Agent采用了以下架构：

```
用户输入 → AI分析 → 生成命令 → 人工审批 → 执行命令 → 返回结果
```

关键组件包括：

*1、**TerminalAgentViewModel**：主要的业务逻辑控制器*

*2、**HumanApprovalDialogViewModel**：审批对话框的数据模型*

*3、**ExecuteCmd函数**：实际执行命令的工具函数*

*4、**WaitingViewModel**：等待状态的显示*

## 具体实现

### 第一步：创建AI Agent工具函数

首先定义一个可以执行Windows命令的函数，并使用Microsoft Agent Framework的特性进行标注：

```csharp
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
```

关键点：

- `[Description]`特性：为AI Agent提供函数说明
- 参数描述：帮助AI理解如何使用这个函数
- 错误处理：确保命令执行的安全性

### 第二步：配置AI Agent

在`TerminalAgentViewModel.cs:132`中配置AI Agent：

```csharp
AIAgent agent = new OpenAIClient(apiKeyCredential, openAIClientOptions)
    .GetChatClient(model)
    .CreateAIAgent(
        instructions: "你是一个乐于助人的助手，可以执行命令行脚本。请使用中文回答。",
        tools: [new ApprovalRequiredAIFunction(AIFunctionFactory.Create(ExecuteCmd))]
    );
```

核心要点：

- **ApprovalRequiredAIFunction**：将普通函数包装为需要审批的函数
- **AIFunctionFactory.Create**：将静态方法转换为AI可调用的函数
- **instructions**：为Agent设置行为指令

### 第三步：实现人工审批流程

处理AI Agent返回的用户输入请求：

```csharp
var response = await agent.RunAsync(InputText, thread);
var userInputRequests = response.UserInputRequests.ToList();

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

    // 将用户审批结果传回Agent
    response = await agent.RunAsync(userInputResponses, thread);
    userInputRequests = response.UserInputRequests.ToList();
}
```

审批流程说明：

1. **检测审批请求**：通过`UserInputRequests`获取需要审批的操作
2. **显示审批对话框**：向用户展示即将执行的命令详情
3. **收集审批结果**：用户同意或拒绝执行
4. **反馈给Agent**：通过`CreateResponse`将审批结果传回

### 第四步：创建审批对话框

**HumanApprovalDialogViewModel**实现：

```csharp
public class HumanApprovalDialogViewModel : Screen
{
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetAndNotify(ref _title, value);
    }

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => SetAndNotify(ref _message, value);
    }

    // 同意操作
    public void Approve()
    {
        RequestClose(true);
    }

    // 拒绝操作
    public void Reject()
    {
        RequestClose(false);
    }
}
```

**审批对话框界面**（HumanApprovalDialogView.xaml）：

```xaml
<Window x:Class="Rouyan.Pages.View.HumanApprovalDialogView"
        Title="{Binding Title}"
        Height="200" Width="420"
        WindowStartupLocation="CenterOwner"
        ResizeMode="NoResize"
        Topmost="True">
    <Window.InputBindings>
        <KeyBinding Command="{s:Action Approve}" Key="Y"/>
        <KeyBinding Command="{s:Action Reject}" Key="N"/>
    </Window.InputBindings>

    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 消息显示区域 -->
        <ScrollViewer Grid.Row="0" VerticalScrollBarVisibility="Auto">
            <TextBlock Text="{Binding Message}"
                       TextWrapping="Wrap"
                       FontSize="14"/>
        </ScrollViewer>

        <!-- 按钮区域 -->
        <Grid Grid.Row="1" Margin="0,16,0,0">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="*"/>
            </Grid.ColumnDefinitions>

            <Button Grid.Column="0"
                    Content="同意(Y)"
                    Command="{s:Action Approve}"
                    IsDefault="True"/>

            <Button Grid.Column="1"
                    Content="拒绝(N)"
                    Command="{s:Action Reject}"
                    IsCancel="True"/>
        </Grid>
    </Grid>
</Window>
```

界面特点：

- **快捷键支持**：Y键同意，N键拒绝
- **模态对话框**：`Topmost="True"`确保始终在最前
- **Stylet命令绑定**：使用`{s:Action}`绑定ViewModel方法

### 第五步：实现流式响应

获取最终结果并流式显示：

```csharp
// 流式获取最终答案
await foreach (var update in agent.RunStreamingAsync("输出最终答案", thread).WithCancellation(_cts!.Token))
{
    OutputText += update.Text;
}
```

流式响应的优势：

- **实时反馈**：用户可以看到实时的输出过程
- **可取消**：支持`CancellationToken`中断操作
- **更好的用户体验**：避免长时间等待的空白期

## 主界面设计

**TerminalAgentView.xaml**提供了简洁的用户界面：

```xaml
<Grid Margin="16">
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto"/>    <!-- 输入区域 -->
        <RowDefinition Height="Auto"/>    <!-- 按钮区域 -->
        <RowDefinition Height="*"/>       <!-- 输出区域 -->
    </Grid.RowDefinitions>

    <!-- 输入文本框 -->
    <TextBox Grid.Row="0"
             Text="{Binding InputText, UpdateSourceTrigger=PropertyChanged}"
             AcceptsReturn="True"
             Height="60"/>

    <!-- 控制按钮 -->
    <StackPanel Grid.Row="1" Orientation="Horizontal">
        <Button Content="运行" Command="{s:Action Run}"/>
        <Button Content="取消" Command="{s:Action Cancel}"/>
    </StackPanel>

    <!-- 输出显示 -->
    <TextBox Grid.Row="2"
             Text="{Binding OutputText, UpdateSourceTrigger=PropertyChanged}"
             IsReadOnly="True"/>
</Grid>
```

## 安全性考虑

在实现过程中，Rouyan采用了多层安全措施：

**1、人工审批**

所有命令执行都需要用户明确同意，防止恶意操作。

**2、命令显示**

在审批对话框中完整显示即将执行的命令内容。

**3、错误处理**

完善的异常捕获和错误信息反馈。

**4、取消机制**

支持在任何阶段取消操作，避免意外执行。

## 实际使用效果

用户在输入框中输入请求，例如"获取当前时间"：

1. **显示等待窗体**：提示正在分析请求
2. **AI分析**：Agent理解请求并生成相应的命令
3. **审批对话框**：显示具体要执行的命令（如`date /t`）
4. **执行命令**：用户同意后执行并获取结果
5. **流式显示**：实时显示执行结果

## 扩展可能性

基于这个架构，可以进一步扩展：

*1、**多种工具支持**：添加文件操作、网络请求等工具*

*2、**审批级别**：根据命令危险程度设置不同审批级别*

*3、**历史记录**：保存执行历史和审批记录*

*4、**权限管理**：不同用户拥有不同的执行权限*

## 总结

通过Microsoft Agent Framework，我们成功创建了一个安全可靠的终端Agent。关键成功因素包括：

- **合理的架构设计**：清晰的组件分离和职责划分
- **完善的审批机制**：确保所有敏感操作都经过人工确认
- **良好的用户体验**：流式响应和实时反馈
- **充分的安全考虑**：多层防护措施

这个实现为构建更复杂的AI Agent应用提供了良好的基础和参考。

项目地址：https://github.com/Ming-jiayou/Rouyan