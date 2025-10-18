# Rouyan 中取消运行任务的方式（技术说明 + 代码示例）

本文基于终端代理页面，说明如何通过 CancellationToken 机制实现任务取消功能，包含 UI 状态管理、取消令牌传递、异常处理与资源清理的完整流程。

适用文件：
- 视图模型：[TerminalAgentViewModel](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:19)；取消方法：[Cancel()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:61)、运行方法：[Run()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:103)
- 视图（XAML）：[TerminalAgentView.xaml](src/Rouyan/Pages/View/TerminalAgentView.xaml:1)；取消按钮：[Cancel 按钮](src/Rouyan/Pages/View/TerminalAgentView.xaml:35)

一、取消机制设计目标
- 立即响应：用户点击"取消"按钮后能立即中断正在运行的异步操作
- 状态同步：取消后正确更新 UI 状态，重新启用"运行"按钮，禁用"取消"按钮
- 资源清理：确保 CancellationTokenSource、等待窗口等资源得到正确释放
- 用户体验：在输出区域显示"已取消运行"的明确反馈信息

二、核心实现机制

1) CancellationTokenSource 的生命周期管理：

```csharp
// TerminalAgentViewModel.cs
private CancellationTokenSource? _cts;

public async Task Run()
{
    if (IsRunning) return;

    // 创建新的取消令牌源
    _cts = new CancellationTokenSource();
    IsRunning = true;

    try
    {
        // 异步操作中使用取消令牌
        await foreach (var update in agent.RunStreamingAsync("输出最终答案", thread)
            .WithCancellation(_cts!.Token))
        {
            OutputText += update.Text;
        }
    }
    finally
    {
        // 确保资源清理
        IsRunning = false;
        _cts?.Dispose();
        _cts = null;
    }
}
```

- 令牌创建：[_cts = new CancellationTokenSource()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:108) 在每次运行开始时创建
- 令牌传递：[.WithCancellation(_cts!.Token)](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:180) 将令牌传递给异步流
- 资源释放：[_cts?.Dispose()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:193) 在 finally 块中确保释放

2) 取消操作的触发与处理：

```csharp
// TerminalAgentViewModel.cs
public void Cancel()
{
    _cts?.Cancel();
}
```

- 取消触发：[_cts?.Cancel()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:63) 调用令牌源的取消方法
- UI 绑定：[Command="{s:Action Cancel}"](src/Rouyan/Pages/View/TerminalAgentView.xaml:36) 将按钮点击绑定到取消方法

3) 运行状态与按钮可用性控制：

```csharp
// TerminalAgentViewModel.cs
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
```

- 状态控制：[IsRunning](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:45) 属性变更时自动通知按钮可用性
- 按钮状态：[CanRun](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:58) 和 [CanCancel](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:59) 确保互斥状态

三、取消检查点与异常处理

1) 多点取消检查：

```csharp
// 审批循环中的取消检查
while (userInputRequests.Count > 0)
{
    if (_cts?.IsCancellationRequested == true) break;
    // ... 处理用户输入请求
}

// 取消后的状态处理
if (_cts?.IsCancellationRequested == true)
{
    OutputText += "\n已取消运行。";
    return;
}
```

- 循环检查：[_cts?.IsCancellationRequested](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:147) 在关键循环中检查取消状态
- 用户反馈：[OutputText += "\n已取消运行。"](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:175) 向用户显示取消信息

2) OperationCanceledException 处理：

```csharp
try
{
    // 异步操作
}
catch (OperationCanceledException)
{
    OutputText += "\n已取消运行。";
}
finally
{
    // 资源清理
    IsRunning = false;
    _cts?.Dispose();
    _cts = null;
}
```

- 异常捕获：[catch (OperationCanceledException)](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:185) 处理取消引发的异常
- 统一清理：[finally 块](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:189) 确保无论何种退出都能正确清理

四、等待窗口的取消处理

```csharp
WaitingViewModel? waitingVm = null;

try
{
    // 显示等待窗体
    waitingVm = new WaitingViewModel { Text = "正在分析请求，请稍候..." };
    _windowManager.ShowWindow(waitingVm);

    // ... 异步操作

    // 关闭等待窗体
    waitingVm?.RequestClose();
}
finally
{
    // 确保等待窗口关闭
    waitingVm?.RequestClose();
}
```

- 窗口管理：[waitingVm?.RequestClose()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:143) 在操作完成或取消时关闭等待窗口
- 双重保险：[finally 块中的 RequestClose()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:191) 确保窗口一定会被关闭

五、用户交互流程

1) 正常运行状态：
- 用户点击"运行" → [IsRunning = true](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:109) → "运行"按钮禁用，"取消"按钮启用

2) 取消操作流程：
- 用户点击"取消" → [Cancel()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:61) → [_cts?.Cancel()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:63) → 异步操作检测到取消 → 输出"已取消运行" → [IsRunning = false](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:192) → 按钮状态恢复

六、最佳实践与注意事项

- **及时检查**：在长时间运行的循环和异步操作中定期检查 [IsCancellationRequested](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:147)
- **资源管理**：使用 [using 语句或 finally 块](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:189) 确保 CancellationTokenSource 得到释放
- **用户反馈**：取消后在 [OutputText](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:175) 中显示明确的取消信息
- **状态一致性**：确保 [IsRunning 状态](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:192) 在所有退出路径中都能正确重置
- **异常安全**：使用 [try-catch-finally](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:113) 模式处理取消异常和资源清理

七、扩展示例

对于其他长时间运行的操作，可以参考相同的模式：

```csharp
private CancellationTokenSource? _operationCts;

public async Task LongRunningOperation()
{
    _operationCts = new CancellationTokenSource();

    try
    {
        // 使用取消令牌的异步操作
        await SomeAsyncMethod(_operationCts.Token);

        // 定期检查取消
        for (int i = 0; i < 1000; i++)
        {
            if (_operationCts.IsCancellationRequested) break;
            // 处理逻辑
        }
    }
    catch (OperationCanceledException)
    {
        // 取消处理
    }
    finally
    {
        _operationCts?.Dispose();
        _operationCts = null;
    }
}

public void CancelOperation()
{
    _operationCts?.Cancel();
}
```

八、文件定位速查
- 取消方法：[TerminalAgentViewModel.Cancel()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:61)
- 运行方法：[TerminalAgentViewModel.Run()](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:103)
- 状态属性：[IsRunning](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:45)、[CanRun](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:58)、[CanCancel](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:59)
- UI 绑定：[取消按钮](src/Rouyan/Pages/View/TerminalAgentView.xaml:35)、[运行按钮](src/Rouyan/Pages/View/TerminalAgentView.xaml:29)
- 令牌管理：[CancellationTokenSource](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:43)、[WithCancellation](src/Rouyan/Pages/ViewModel/TerminalAgentViewModel.cs:180)