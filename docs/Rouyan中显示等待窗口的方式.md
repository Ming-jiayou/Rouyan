# Rouyan 中如何显示等待窗口

本文说明项目中“等待窗口（Waiting）”的定义、依赖、数据绑定与典型显示方式，帮助在长耗时任务期间以一致的 UI 反馈用户。

## 概览
- 等待窗口视图文件：[WaitingView.xaml](src/Rouyan/Pages/View/WaitingView.xaml)
- 视图代码隐藏：[WaitingView.xaml.cs](src/Rouyan/Pages/View/WaitingView.xaml.cs:7)
- 视图模型（绑定文案）：[WaitingViewModel.Text](src/Rouyan/Pages/ViewModel/WaitingViewModel.cs:10)
- 主题/样式依赖（MaterialDesign）：[BundledTheme](src/Rouyan/App.xaml:27), [MaterialDesign3.Defaults.xaml](src/Rouyan/App.xaml:28)

## 等待窗口视图定义（WaitingView.xaml）
等待窗口由 WPF 窗口定义，核心属性如下：
- 窗口类型与类声明：[Window x:Class="Rouyan.Pages.View.WaitingView"](src/Rouyan/Pages/View/WaitingView.xaml:1)
- 标题：[Title](src/Rouyan/Pages/View/WaitingView.xaml:9) = "WaitingWindow"
- 尺寸：[Height](src/Rouyan/Pages/View/WaitingView.xaml:10) = 200、[Width](src/Rouyan/Pages/View/WaitingView.xaml:11) = 350
- 样式：设为工具窗口 [WindowStyle="ToolWindow"](src/Rouyan/Pages/View/WaitingView.xaml:12)、不允许调整大小 [ResizeMode="NoResize"](src/Rouyan/Pages/View/WaitingView.xaml:14)
- 显示位置：居中屏幕 [WindowStartupLocation="CenterScreen"](src/Rouyan/Pages/View/WaitingView.xaml:13)
- 置顶显示：[Topmost="True"](src/Rouyan/Pages/View/WaitingView.xaml:15)

视觉元素：
- 顶部文案区绑定视图模型的文案属性：[Text="{Binding Text}"](src/Rouyan/Pages/View/WaitingView.xaml:26)
- 使用 MaterialDesign 的圆形不定进度条样式：[Style="{StaticResource MaterialDesignCircularProgressBar}"](src/Rouyan/Pages/View/WaitingView.xaml:37)，前景色来自主题色：[SecondaryHueMidBrush](src/Rouyan/Pages/View/WaitingView.xaml:38)
- 底部辅助提示文案“请稍候...”：[Text="请稍候..."](src/Rouyan/Pages/View/WaitingView.xaml:43)

代码隐藏仅负责初始化组件：
- 构造函数：[WaitingView.WaitingView()](src/Rouyan/Pages/View/WaitingView.xaml.cs:7)

## 视图模型（WaitingViewModel）
等待窗口的文案来自视图模型的 Text 属性：
- 默认文案：[public string Text { get; set; } = "处理中...";](src/Rouyan/Pages/ViewModel/WaitingViewModel.cs:10)

提示：
- 若需要在任务执行过程中动态更新 Text（例如显示不同阶段提示），建议将 Text 改为带通知的属性（使用 Stylet 的 SetAndNotify），例如：
  - private string _text; public string Text { get => _text; set => SetAndNotify(ref _text, value); }
  以上写法可参考项目中其它使用 SetAndNotify 的模式，例如 [HumanApprovalDialogViewModel.Title](src/Rouyan/Pages/ViewModel/HumanApprovalDialogViewModel.cs:14)。

## 样式与主题依赖（MaterialDesign）
等待窗口使用的颜色资源与进度条样式来自 MaterialDesign 主题，已在应用级资源中合并：
- 主题注入：[materialDesign:BundledTheme](src/Rouyan/App.xaml:27)
- 默认样式字典：[MaterialDesign3.Defaults.xaml](src/Rouyan/App.xaml:28)

因此，无需在等待窗口内重复引入样式字典，即可使用：
- PrimaryHueMidBrush、SecondaryHueMidBrush、MaterialDesignBody
- MaterialDesignCircularProgressBar

## 显示等待窗口的典型方式

说明：WaitingView 当前未在仓库中直接调用（未检索到 new WaitingView/ShowDialog 的现用代码），以下给出两种推荐调用模式。请按业务需要选择阻塞（模态）或非阻塞（无模式）显示。

### 方式一：非阻塞显示（推荐，适合后台执行任务）
- 适用场景：在 UI 线程上立即反馈等待窗口，同时后台线程执行耗时工作，完成后关闭窗口。
- 要点：窗口通过 Show() 非模态显示；耗时工作通过 Task.Run 执行；完成后在 UI 线程 Close()。

示例（可在任意触发点调用，如命令处理、按钮事件等）：
```csharp
using System.Threading.Tasks;
using System.Windows;
using Rouyan.Pages.View;
using Rouyan.Pages.ViewModel;

public async Task RunWithWaitingAsync()
{
    var vm = new WaitingViewModel { Text = "处理中，请稍候..." };
    var waiting = new WaitingView { DataContext = vm };

    waiting.Show(); // 非模态显示，UI 不被阻塞

    try
    {
        await Task.Run(() =>
        {
            // TODO: 在后台执行长耗时任务
            System.Threading.Thread.Sleep(3000);
        });
    }
    finally
    {
        // 关闭等待窗口，确保在 UI 线程调用
        if (waiting.IsVisible)
        {
            if (!waiting.Dispatcher.CheckAccess())
                waiting.Dispatcher.Invoke(() => waiting.Close());
            else
                waiting.Close();
        }
    }
}
```

动态更新文案（若 Text 支持通知）：
```csharp
vm.Text = "正在下载数据...";
```

### 方式二：模态显示（阻塞，简单流程）
- 适用场景：任务必须阻塞当前交互，且希望用 ShowDialog() 形成简单的阻塞流程。
- 要点：ShowDialog() 会阻塞当前调用线程；通常配合后台任务并在任务完成时从其它上下文关闭窗口。

示例：
```csharp
var vm = new WaitingViewModel { Text = "正在执行任务..." };
var waiting = new WaitingView { DataContext = vm };

// 开一个后台任务执行逻辑，结束时关闭窗口
_ = Task.Run(() =>
{
    // TODO: 长耗时任务
    System.Threading.Thread.Sleep(3000);

    // 任务结束后关闭等待窗口（切回 UI 线程）
    waiting.Dispatcher.Invoke(() => waiting.Close());
});

// 模态显示（阻塞当前调用栈，直到窗口被关闭）
waiting.ShowDialog();
```

注意：如果在 UI 线程直接执行耗时任务且同步调用 ShowDialog()，会造成界面卡顿，应避免。正确做法是让耗时任务在后台线程执行，并在任务结束时关闭窗口。

## 关闭与异常处理建议
- 始终在 finally 中关闭等待窗口，确保异常情况下也能释放 UI。
- 关闭窗口需在 UI 线程执行（使用 Dispatcher.Invoke()）。
- 如果用户可能中途取消（例如额外提供取消按钮），请在取消逻辑中同样确保关闭窗口。

## 常见问题
- 动态更新文案未生效：请将 Text 改为带通知的属性（SetAndNotify），并在 UI 线程更新。
- 窗口遮挡问题：等待窗口已设置 [Topmost](src/Rouyan/Pages/View/WaitingView.xaml:15)，一般可避免被遮挡；若需跟随主窗体居中，可改为 CenterOwner 并设置 Owner。
- 样式引用失败：确认应用级资源中已合并 MaterialDesign 主题，[BundledTheme](src/Rouyan/App.xaml:27) 与 [Defaults](src/Rouyan/App.xaml:28) 未被移除/更改。
- 与主窗体交互：非模态显示时主窗体不被阻塞；如需阻塞交互，使用模态 ShowDialog()。

## 相关文件索引
- 视图定义：[WaitingView.xaml](src/Rouyan/Pages/View/WaitingView.xaml)
  - [Title](src/Rouyan/Pages/View/WaitingView.xaml:9)、[Topmost](src/Rouyan/Pages/View/WaitingView.xaml:15)、[MaterialDesignCircularProgressBar](src/Rouyan/Pages/View/WaitingView.xaml:37)、[Text 绑定](src/Rouyan/Pages/View/WaitingView.xaml:26)
- 视图代码隐藏：[WaitingView.WaitingView()](src/Rouyan/Pages/View/WaitingView.xaml.cs:7)
- 视图模型文案：[WaitingViewModel.Text](src/Rouyan/Pages/ViewModel/WaitingViewModel.cs:10)
- 主题资源：[BundledTheme](src/Rouyan/App.xaml:27)、[MaterialDesign3.Defaults.xaml](src/Rouyan/App.xaml:28)
