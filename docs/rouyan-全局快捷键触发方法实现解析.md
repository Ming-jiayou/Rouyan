# Rouyan WPF 全局快捷键触发方法实现解析

本文系统阐述 Rouyan 项目如何在 WPF 中“通过快捷键运行一个方法”。围绕全局键盘钩子、按键序列识别（Tab+字母）、UI 线程调度与 Stylet IoC 的协作，逐步拆解代码与设计。阅读后你将：

- 理解 Tab+字母 序列热键的识别流程与状态机实现
- 掌握热键触发到业务方法调用的完整链路
- 了解资源释放、线程切换及扩展（新增/修改快捷键）的实践要点

相关源码：

- `src/Rouyan/Services/HotkeyService.cs`
- `src/Rouyan/Services/KeySequenceService.cs`
- `src/Rouyan/Bootstrapper.cs`
- `src/Rouyan/Pages/ViewModel/HomeViewModel.cs`

---

## 背景与目标

桌面工具类应用往往需要“全局快捷键”来提升效率：不切换焦点即可触发某个功能。Rouyan 的需求更进一步——一次按下 Tab 作为“起始键”，随后在限定时间内按不同字母来选择不同功能（如调用大语言模型或视觉模型）。因此，项目实现了一套“全局键盘钩子 + 序列状态机”的方案，以最小耦合把键盘事件映射到 ViewModel 中的具体方法。

目标可以概括为：

- 在系统范围捕获键盘事件（无需窗口焦点）。
- 用 Tab 作为序列起点，2 秒内按下字母键（K/L/U/I/S/D/W/E）触发不同功能。
- 保证被触发的方法在 UI 线程上执行，以安全访问剪贴板、弹窗等。
- 通过 IoC 获取目标 ViewModel，避免钩子层直接耦合业务逻辑。
- 在退出或释放时卸载钩子，避免资源泄漏。

---

## 架构总览：服务 + 钩子 + 状态机 + IoC

Rouyan 的实现可以拆分为四个层面：

- `HotkeyService`：热键服务的对外入口。负责初始化 `KeySequenceService`，将各个组合键映射到对应的回调方法（最终调用 `HomeViewModel` 的业务方法）。
- `KeySequenceService`：核心捕获与判定。通过 Win32 低级键盘钩子（`WH_KEYBOARD_LL`）监听全局按键，并用简单状态机识别“Tab + 字母”的序列，命中后调用注入的 `Action`。
- `Bootstrapper`：应用启动时的装配与初始化。完成 IoC 绑定，并在主窗口就绪后启动热键服务监听。
- `HomeViewModel`：被触发的业务方法集合。例如 `RunLLMPrompt1` 从剪贴板读取文本并调用 LLM，把结果写入选中文件或弹窗显示。

这种分层让“输入层（键盘钩子）”与“业务层（ViewModel 方法）”通过回调与 IoC 解耦：热键服务不关心具体业务，只负责“识别并调用”。

---

## 启动与 IoC 绑定：Bootstrapper 的角色

应用启动由 `Bootstrapper` 驱动。在 `ConfigureIoC` 中，项目将导航控制器、环境配置服务、提示词管理服务以及 `HomeViewModel`、各页面 ViewModel 工厂等注册到 Stylet IoC；同时把 `HotkeyService` 注册为单例，保证全局监听唯一性与生命周期统一。

在 `OnLaunch` 中，导航到主页面之后，取出 `HotkeyService` 并调用 `Initialize(Application.Current.MainWindow)`，完成热键钩子的安装与序列服务的启动。关键片段见 `src/Rouyan/Bootstrapper.cs:28`：

```csharp
// 绑定全局快捷键服务为单例
builder.Bind<HotkeyService>().To<HotkeyService>().InSingletonScope();
...
var _hotkeyService = this.Container.Get<HotkeyService>();
if (Application.Current?.MainWindow != null)
{
    _hotkeyService.Initialize(Application.Current.MainWindow);
}
```

这一步确保主窗口存在（某些钩子或调度需要上下文），随后才开始监听键盘事件。

---

## 热键服务：把组合键映射到业务方法

`HotkeyService` 是将“组合键”与“业务方法”连接起来的桥梁。它在 `Initialize` 中创建 `KeySequenceService`，注入 8 个 `Action` 回调，分别对应 LLM/VLM 的两类方法及其 Streaming 变体：

- Tab+K → `RunLLMPrompt1`
- Tab+L → `RunLLMPrompt1Streaming`
- Tab+U → `RunLLMPrompt2`
- Tab+I → `RunLLMPrompt2Streaming`
- Tab+S → `RunVLMPrompt1`
- Tab+D → `RunVLMPrompt1Streaming`
- Tab+W → `RunVLMPrompt2`
- Tab+E → `RunVLMPrompt2Streaming`

初始化参考 `src/Rouyan/Services/HotkeyService.cs`：

```csharp
_keySequenceService = new KeySequenceService(
    ExecuteRunLLMPrompt1,
    ExecuteRunLLMPrompt1Streaming,
    ExecuteRunLLMPrompt2,
    ExecuteRunLLMPrompt2Streaming,
    ExecuteRunVLMPrompt1,
    ExecuteRunVLMPrompt1Streaming,
    ExecuteRunVLMPrompt2,
    ExecuteRunVLMPrompt2Streaming);
_keySequenceService.RegisterHotKeys();
```

每个 `Execute*` 方法内部逻辑类似：从 IoC 容器获取 `HomeViewModel` 实例，然后 `await` 调用对应的异步方法。如果未能获取实例或调用失败，会在控制台输出警告或异常信息，提升可观测性。

示例（`Tab+K` → `RunLLMPrompt1`）：

```csharp
private async void ExecuteRunLLMPrompt1()
{
    try
    {
        var homeViewModel = _container.Get<HomeViewModel>();
        if (homeViewModel != null)
        {
            await homeViewModel.RunLLMPrompt1();
        }
        else
        {
            Console.WriteLine("警告: 无法获取HomeViewModel实例");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"执行Tab+K热键操作失败: {ex.Message}");
    }
}
```

这让“热键层”保持轻量：仅负责把命中事件转发到业务层，而不嵌入任何领域逻辑。

---

## 全局键盘钩子：Win32 WH_KEYBOARD_LL

要实现“无焦点也能响应”的全局快捷键，需要使用 Win32 低级键盘钩子 `WH_KEYBOARD_LL`。`KeySequenceService` 通过 `SetWindowsHookEx` 安装钩子，并在回调 `HookCallback` 中处理 `WM_KEYDOWN` 消息：

- `SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(moduleName), 0)` 安装钩子。
- `HookCallback` 在按键事件到来时读取虚拟键码（VK），交由状态机处理。
- `CallNextHookEx` 保持钩子链的正常传递。
- `UnhookWindowsHookEx` 在释放时卸载钩子，避免句柄泄漏。

钩子安装参考 `src/Rouyan/Services/KeySequenceService.cs`：

```csharp
_hookID = SetHook(_proc);
...
return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
```

回调核心：

```csharp
private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
{
    if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
    {
        int vkCode = Marshal.ReadInt32(lParam);
        HandleKeyDown(vkCode);
    }
    return CallNextHookEx(_hookID, nCode, wParam, lParam);
}
```

该钩子是系统级的，不依赖窗口焦点，因此在应用最小化或不激活时仍可截获按键事件。

---

## 序列识别：Tab + 字母 的状态机

Rouyan 并非用传统的“修饰键（Ctrl/Alt/Shift）+字母”的固定组合，而是采用“序列热键”：先按 Tab，随后在限定时间（2 秒）内按某个字母。这样可以把多个功能聚合到同一个“起始键”后，提升多功能选择的效率。

状态机定义与逻辑：

- 状态枚举：`None`（未进入序列）、`WaitingAfterTab`（已按下 Tab，等待后续字母）。
- 进入等待：在 `None` 状态下捕获到 `VK_TAB` → 切换到 `WaitingAfterTab`，同时记录序列开始时间。
- 判定命中：在 `WaitingAfterTab` 且未超时的情况下，若按下 K/L/U/I/S/D/W/E 中任一字母，则调用对应的回调 `Action`。
- 超时与重置：超过 `SEQUENCE_TIMEOUT_MS = 2000` 毫秒则视为超时；命中或超时后都会调用 `ResetState()` 回到初始状态。

核心处理参考 `src/Rouyan/Services/KeySequenceService.cs`：

```csharp
case HotkeyMode.None:
    if (vkCode == VK_TAB)
    {
        _currentMode = HotkeyMode.WaitingAfterTab;
        _sequenceStartTime = DateTime.Now;
    }
    break;

case HotkeyMode.WaitingAfterTab:
    if (IsTimeout())
    {
        Console.WriteLine("按键序列超时");
    }
    else
    {
        switch (vkCode)
        {
            case VK_K: ExecuteAction(_runLLMPrompt1); break;
            case VK_L: ExecuteAction(_runLLMPrompt1Streaming); break;
            case VK_U: ExecuteAction(_runLLMPrompt2); break;
            case VK_I: ExecuteAction(_runLLMPrompt2Streaming); break;
            case VK_S: ExecuteAction(_runVLMPrompt1); break;
            case VK_D: ExecuteAction(_runVLMPrompt1Streaming); break;
            case VK_W: ExecuteAction(_runVLMPrompt2); break;
            case VK_E: ExecuteAction(_runVLMPrompt2Streaming); break;
            default: Console.WriteLine($"检测到 Tab 后的无效按键: {vkCode}"); break;
        }
    }
    ResetState();
    break;
```

相比固定组合，序列模式具有更好的扩展性与记忆性：用户先按 Tab，接着按不同字母即可选择不同功能；新增功能只需在等待状态增加一个字母分支。

---

## UI 线程调度：确保在正确线程上执行

键盘钩子回调不在 WPF UI 线程上，直接操作剪贴板、弹窗或绑定属性会引发异常。因此，Rouyan 使用 `Application.Current.Dispatcher.BeginInvoke` 将方法调用切回 UI 线程：

```csharp
Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
{
    try { action?.Invoke(); }
    catch (Exception ex) { Console.WriteLine($"执行热键操作时出错: {ex.Message}"); }
}), DispatcherPriority.Normal);
```

这点尤为重要。以剪贴板访问为例，`HomeViewModel` 在执行时会通过 `Dispatcher.InvokeAsync` 读取文本或图片，避免跨线程访问 UI 对象：

```csharp
string clipboardText = string.Empty;
await Application.Current.Dispatcher.InvokeAsync(() =>
{
    if (Clipboard.ContainsText())
        clipboardText = Clipboard.GetText();
});
```

---

## 业务方法示例：从剪贴板到模型推理

以 `RunLLMPrompt1` 为例（由 `Tab+K` 触发）：

- 确保提示词加载完成（`EnsurePromptsLoadedAsync`）。
- 用 `IWindowManager` 打开等待窗口，提升用户反馈。
- 在 UI 线程读取剪贴板文本，若为空则弹窗提示。
- 读取 `.env` 配置，构造 `OpenAI.Chat.ChatClient`（通过 `Microsoft.Extensions.AI` 包装）。
- 发送系统消息 + 用户文本，获取模型响应。
- 若用户选择了输出文件，追加写入；否则弹窗提示选择文件。
- 关闭等待窗口。

代码片段见 `src/Rouyan/Pages/ViewModel/HomeViewModel.cs`，整个流程以异步方式执行，并在必要处切换到 UI 线程，保证体验与安全。

Streaming 变体（如 `RunLLMPrompt1Streaming`）会以流式增量更新界面（例如将逐步生成的文本追加到窗口的绑定属性），视觉模型变体（`RunVLMPrompt*`）则会从剪贴板读取图片并编码为 JPEG 字节数据传给模型。

---

## 资源释放与生命周期管理

全局钩子必须在不需要时卸载，否则会造成系统资源泄漏或影响其他软件。Rouyan 在 `KeySequenceService.Dispose()` 中调用 `UnhookWindowsHookEx`；`HotkeyService.Dispose()` 则统一释放其内部的序列服务：

```csharp
public void Dispose()
{
    try
    {
        if (_hookID != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookID);
            _hookID = IntPtr.Zero;
            Console.WriteLine("全局热键已卸载");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"清理热键资源时出错: {ex.Message}");
    }
}
```

这确保应用退出或服务终止时不会留下悬挂钩子。

---

## 扩展与定制：新增/修改快捷键的步骤

要增加新的序列快捷键（例如 `Tab+A`）：

1. 在 `KeySequenceService` 中增加虚拟键常量：`const int VK_A = 0x41;`。
2. 在 `HandleKeyDown` 的 `WaitingAfterTab` 分支中添加 `case VK_A`，调用新的回调 `Action`。
3. 扩展 `KeySequenceService` 构造函数的参数列表，接收新的 `Action` 并存为字段。
4. 在 `HotkeyService.Initialize` 中传入新的 `Action`，并实现对应的 `Execute*` 方法：从 `_container.Get<HomeViewModel>()` 获取 VM 后调用新的业务方法。
5. 在 `RegisterHotKeys` 的日志输出中添加新组合键，方便调试与自检。

如果希望改回传统系统热键（例如 `Ctrl+Shift+X`），可考虑使用 `RegisterHotKey` API 配合修饰键；该方式无需状态机，但扩展性较弱，且可能与其他软件冲突更大。Rouyan 选用“序列热键”以平衡多功能选择与易用性。

---

## 注意事项与排错建议

- 未触发方法：确认是否先按了 `Tab`，并在 2 秒内按下目标字母；查看控制台是否有“全局热键已注册”与序列日志。
- 钩子安装失败：检查权限与进程环境（位数、桌面会话），验证 `SetWindowsHookEx` 返回值；在 `RegisterHotKeys` 的异常输出中获取线索。
- UI 异常或卡顿：保证所有 UI 相关操作在 Dispatcher 上执行，避免在钩子回调线程直接访问剪贴板或窗口对象。
- 与其他键盘管理工具冲突：全局钩子可能与第三方热键/输入法/键鼠增强工具冲突；测试时尽量排除其他钩子或临时关闭相关工具。
- 日志与可观测性：合理保留必要的 `Console.WriteLine` 输出，便于定位序列超时、无效按键等问题；生产环境可接入更完善的日志框架。

---

## 实战链路回顾：从启动到触发

1. 应用启动：`Bootstrapper` 完成 IoC 绑定与导航，初始化 `HotkeyService`。
2. 注册钩子：`KeySequenceService.RegisterHotKeys()` 安装 `WH_KEYBOARD_LL`，输出支持的组合键列表。
3. 用户输入：按下 `Tab` → 状态机进入等待；随后按 K/L/U/I/S/D/W/E → 判定命中或超时。
4. 线程切换：`ExecuteAction` 将回调调度到 UI 线程执行。
5. 调用业务：`HotkeyService.Execute*` 通过 IoC 获取 `HomeViewModel` 并调用方法（如 `RunLLMPrompt1`）。
6. 释放资源：应用退出或服务销毁时卸载钩子，清理资源。

---

## 设计小结与启发

- 全局钩子 + 序列状态机的组合，让一个“起始键”承载多功能选择，既灵活又高效。
- Dispatcher 切换线程是 WPF 中的硬性要求，所有 UI 相关操作都应在 UI 线程进行。
- 通过 Stylet IoC 获取 ViewModel，保持热键层的纯净与可测试性，便于扩展与维护。
- 统一的资源释放路径，避免系统级副作用，是任何钩子型设计的必要条件。

如果你在自己的 WPF 项目中需要“通过快捷键运行一个方法”，可以借鉴 Rouyan 的模式：用钩子获取事件、用状态机做序列判定、用 Dispatcher 保证线程安全、用 IoC 连接到业务层。这样既能保持代码整洁，又能快速迭代功能与快捷键布局。

---

## 参考文件

- `src/Rouyan/Services/HotkeyService.cs`
- `src/Rouyan/Services/KeySequenceService.cs`
- `src/Rouyan/Bootstrapper.cs`
- `src/Rouyan/Pages/ViewModel/HomeViewModel.cs`

