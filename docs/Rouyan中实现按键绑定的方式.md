# Rouyan 中实现按键绑定的方式（技术说明 + 代码示例）

本文基于人机审批对话框，说明如何通过窗口级输入绑定、按钮默认/取消语义、视图模型动作与代码隐藏兜底实现一致的键盘交互。

适用文件：
- 视图模型：[HumanApprovalDialogViewModel](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:6)；动作方法：[Approve()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:28)、[Reject()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:34)
- 视图（XAML）：[HumanApprovalDialogView.xaml](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:1)；窗口级输入绑定：[Window.InputBindings](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:14)
- 代码隐藏（C#）：[HumanApprovalDialogView.OnKeyDown()](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml.cs:14)

一、交互目标
- 按 Y：同意 → [Approve()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:28) → [Screen.RequestClose(true)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:30)
- 按 N：拒绝 → [Reject()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:34) → [Screen.RequestClose(false)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:36)
- 按 Enter：触发默认按钮 [IsDefault](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:43) → 等价 Approve
- 按 Esc：触发取消按钮 [IsCancel](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:51) 或兜底 [OnKeyDown](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml.cs:14) → 等价 Reject

二、实现步骤
1) 在窗口级 InputBindings 绑定快捷键到 ViewModel 动作（Stylet s:Action）：

```xml
<!-- HumanApprovalDialogView.xaml -->
<Window.InputBindings>
  <KeyBinding Command="{s:Action Approve}" Key="Y"/>
  <KeyBinding Command="{s:Action Reject}"  Key="N"/>
</Window.InputBindings>
```

- 绑定位置： [Window.InputBindings](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:14)
- 命令解析：Stylet 的 [s:Action](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:15) 会将命令路由到 DataContext（ViewModel）上的同名方法。

2) 使用按钮默认/取消语义支持 Enter/Esc：

```xml
<!-- HumanApprovalDialogView.xaml（底部按钮） -->
<Button Content="同意(Y)" Command="{s:Action Approve}" IsDefault="True"/>
<Button Content="拒绝(N)" Command="{s:Action Reject}"  IsCancel="True"/>
```

- 默认键： [IsDefault="True"](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:43) → Enter 触发同意
- 取消键： [IsCancel="True"](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:51) → Esc 触发拒绝

3) 在 ViewModel 中实现动作方法并发出关闭请求：

```csharp
// HumanApprovalDialogViewModel.cs
public void Approve() => RequestClose(true);
public void Reject()  => RequestClose(false);
```

- 关闭机制： [Screen.RequestClose(boolean)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:30) 将结果传回调用方并关闭对话框。

4) 在代码隐藏中对 Esc 做兜底处理（确保极端焦点状态也能退出）：

```csharp
// HumanApprovalDialogView.xaml.cs
protected override void OnKeyDown(KeyEventArgs e)
{
    if (e.Key == Key.Escape)
    {
        this.DialogResult = false;
        this.Close();
        return;
    }
    base.OnKeyDown(e);
}
```

三、事件链（文字版）
- 按 Y：Window 捕获 → s:Action → [Approve()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:28) → [RequestClose(true)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:30)
- 按 N：Window 捕获 → s:Action → [Reject()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:34) → [RequestClose(false)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:36)
- 按 Enter：默认按钮（[IsDefault](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:43)）触发 → 等价 Approve
- 按 Esc：取消按钮（[IsCancel](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:51)）或兜底（[OnKeyDown](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml.cs:14)）触发 → 等价 Reject

四、扩展示例
- 组合键：在 [Window.InputBindings](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:14) 添加 `Modifiers="Control"` 等属性，实现 Ctrl+Y/Ctrl+N。

```xml
<Window.InputBindings>
  <KeyBinding Command="{s:Action Approve}" Key="Y" Modifiers="Control"/>
  <KeyBinding Command="{s:Action Reject}"  Key="N" Modifiers="Control"/>
</Window.InputBindings>
```

- 带参数动作：s:Action 支持向方法传参（如区分来源），例如：

```xml
<KeyBinding Command="{s:Action Approve('hotkey')}" Key="Y"/>
```

五、注意事项与最佳实践
- DataContext：确保窗口绑定到 [HumanApprovalDialogViewModel](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:6)，否则 s:Action 无法解析目标方法。
- 焦点覆盖：窗口级绑定不受子控件焦点影响，更稳定。
- 行为一致性：Esc 的行为在 [IsCancel](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:51) 与 [OnKeyDown](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml.cs:14) 中统一为“拒绝并关闭”。
- 模态/非模态：ShowDialog 模态时返回值与 [RequestClose(bool)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:30) 保持一致契约。

六、文件定位速查
- 视图： [HumanApprovalDialogView.xaml](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:1)；输入绑定 [Window.InputBindings](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:14)；默认/取消 [IsDefault](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:43)、[IsCancel](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml:51)
- 代码隐藏： [HumanApprovalDialogView.OnKeyDown()](src/Rouyan/Pages/View/HumanApprovalDialogView.xaml.cs:14)
- 视图模型： [HumanApprovalDialogViewModel.Approve()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:28)、[HumanApprovalDialogViewModel.Reject()](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:34)、[Screen.RequestClose(boolean)](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:30)