# Rouyan中显示等待窗口的方式

## 前言

在WPF应用开发中，<span style="color: dodgerblue;">等待窗口（Waiting Window）</span>是提升用户体验的重要组件。当应用执行耗时操作（如网络请求、数据处理、AI对话等）时，通过显示等待窗口可以向用户传达"程序正在工作"的信息，避免用户误以为程序卡死。本文以Rouyan为例，详细说明如何在WPF应用中实现一个美观且实用的等待窗口。

## 等待窗口的重要性

在Rouyan中，等待窗口主要用于以下场景：

*1、LLM对话等待：调用大语言模型API时的响应等待*

*2、图像处理：VLM（视觉语言模型）分析图片内容*

*3、文件操作：读取配置、写入翻译结果等IO操作*

*4、网络请求：与各种AI服务的通信*

## 设计理念

Rouyan的等待窗口设计遵循以下原则：

*1、**简洁明了**：避免过多装饰，专注于传达等待状态*

*2、**视觉吸引**：使用Material Design风格的动画效果*

*3、**信息丰富**：可自定义等待文案，告知用户具体在做什么*

*4、**用户友好**：始终置顶显示，不会被其他窗口遮挡*

## 具体实现

### 第一步：创建等待窗口视图模型

**WaitingViewModel**非常简洁，只包含一个显示文本属性：

```csharp
public class WaitingViewModel : Screen
{
    public string Text { get; set; } = "处理中...";
}
```

设计要点：

- **继承Screen**：使用Stylet框架的Screen基类，便于窗口管理
- **简单属性**：Text属性用于显示自定义等待信息
- **默认文案**："处理中..."作为通用提示

### 第二步：设计等待窗口界面

**WaitingView.xaml**采用Material Design风格：

```xaml
<Window x:Class="Rouyan.Pages.View.WaitingView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Title="WaitingWindow"
        Height="200"
        Width="350"
        WindowStyle="ToolWindow"
        WindowStartupLocation="CenterScreen"
        ResizeMode="NoResize"
        Topmost="True">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- 顶部标题区域 -->
        <Border Grid.Row="0"
                Background="{DynamicResource PrimaryHueMidBrush}"
                Padding="16 8">
            <TextBlock HorizontalAlignment="Center"
                       Text="{Binding Text}"
                       FontSize="16"/>
        </Border>

        <!-- 进度条区域 -->
        <StackPanel Grid.Row="1"
                    Orientation="Vertical"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center"
                    Margin="20">

            <ProgressBar Style="{StaticResource MaterialDesignCircularProgressBar}"
                         Foreground="{DynamicResource SecondaryHueMidBrush}"
                         Width="60"
                         Height="60"
                         IsIndeterminate="True"/>

            <TextBlock Text="请稍候..."
                       FontSize="14"
                       HorizontalAlignment="Center"
                       Foreground="{DynamicResource MaterialDesignBody}"
                       Margin="0,10,0,0"/>

        </StackPanel>
    </Grid>
</Window>
```

界面特点：

- **Material Design风格**：使用圆形进度条和主题色彩
- **居中显示**：`WindowStartupLocation="CenterScreen"`
- **工具窗口**：`WindowStyle="ToolWindow"`减少标题栏高度
- **始终置顶**：`Topmost="True"`确保不被遮挡
- **不可调整大小**：`ResizeMode="NoResize"`保持固定尺寸

### 第三步：在业务逻辑中使用等待窗口

Rouyan在多个场景中使用了等待窗口，以`HomeViewModel.cs:122`中的LLM调用为例：

```csharp
public async Task RunLLMPrompt1()
{
    try
    {
        await EnsurePromptsLoadedAsync();

        // 显示等待窗口
        var waitingViewModel = container.Get<WaitingViewModel>();
        windowManager.ShowWindow(waitingViewModel);

        // 获取剪切板文本
        string clipboardText = string.Empty;
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            ClipboardText = string.Empty;
            if (Clipboard.ContainsText())
            {
                clipboardText = Clipboard.GetText();
                ClipboardText = clipboardText;
            }
        });

        if (string.IsNullOrEmpty(clipboardText))
        {
            MessageBox.Show("剪贴板中没有文本内容", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 调用LLM API进行处理
        DotEnv.Load();
        var envVars = DotEnv.Read();
        ApiKeyCredential apiKeyCredential = new ApiKeyCredential(envVars["OPENAI_API_KEY"]);

        OpenAIClientOptions openAIClientOptions = new OpenAIClientOptions();
        openAIClientOptions.Endpoint = new Uri(envVars["OPENAI_BASE_URL"]);

        IChatClient client = new OpenAI.Chat.ChatClient(envVars["OPENAI_CHAT_MODEL"], apiKeyCredential, openAIClientOptions)
                           .AsIChatClient();

        var ChatClient = new ChatClientBuilder(client)
             .UseFunctionInvocation()
             .Build();

        var LLMPrompt1 = promptService.CurrentLLMPrompt1;

        IList<Microsoft.Extensions.AI.ChatMessage> Messages =
           [
               new(ChatRole.System, $"{LLMPrompt1}"),
           ];

        Messages.Add(new(ChatRole.User, ClipboardText));

        var response = await ChatClient.GetResponseAsync(Messages);

        // 将结果写入文件
        if (!string.IsNullOrEmpty(SelectedFilePath))
        {
            try
            {
                await File.AppendAllTextAsync(SelectedFilePath, response.Text + Environment.NewLine);
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"写入文件失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        // 关闭等待窗口
        waitingViewModel.RequestClose();
    }
    catch (Exception ex)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            MessageBox.Show($"执行操作时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        });
    }
}
```

使用模式说明：

1. **显示等待窗口**：在耗时操作开始前显示
2. **执行业务逻辑**：进行实际的耗时操作
3. **关闭等待窗口**：操作完成或异常时关闭

### 第四步：自定义等待信息

在不同场景中，可以自定义等待窗口的显示文案：

```csharp
// 终端Agent中的使用示例
waitingVm = new WaitingViewModel { Text = "正在分析请求，请稍候..." };
_windowManager.ShowWindow(waitingVm);
```

**不同场景的文案示例**：

- 文本翻译：`"正在翻译文本，请稍候..."`
- 图像分析：`"正在分析图像，请稍候..."`
- 文件操作：`"正在保存文件，请稍候..."`
- 网络请求：`"正在连接服务器，请稍候..."`

## 依赖与配置

### Material Design依赖

等待窗口使用了Material Design控件和主题，需要在`App.xaml`中配置：

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <materialDesign:BundledTheme
                BaseTheme="Light"
                PrimaryColor="DeepPurple"
                SecondaryColor="Lime" />
            <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign3.Defaults.xaml" />
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

这样配置后，等待窗口就可以使用以下资源：

- `{DynamicResource PrimaryHueMidBrush}`：主要颜色
- `{DynamicResource SecondaryHueMidBrush}`：次要颜色
- `{StaticResource MaterialDesignCircularProgressBar}`：圆形进度条样式

### Stylet框架集成

等待窗口通过Stylet的`IWindowManager`进行管理：

```csharp
// 通过依赖注入获取
var waitingViewModel = container.Get<WaitingViewModel>();

// 显示窗口
windowManager.ShowWindow(waitingViewModel);

// 关闭窗口
waitingViewModel.RequestClose();
```

## 最佳实践

### 1、异常处理

**始终在finally块中关闭等待窗口**：

```csharp
WaitingViewModel? waitingVm = null;
try
{
    waitingVm = new WaitingViewModel { Text = "处理中..." };
    _windowManager.ShowWindow(waitingVm);

    // 执行耗时操作
    await DoSomethingAsync();
}
finally
{
    waitingVm?.RequestClose();
}
```

### 2、UI线程考虑

**确保在UI线程上操作窗口**：

```csharp
// 如果在非UI线程中，需要调度到UI线程
Application.Current.Dispatcher.Invoke(() =>
{
    waitingViewModel.RequestClose();
});
```

### 3、用户取消支持

**对于长时间操作，考虑提供取消机制**：

```csharp
private CancellationTokenSource? _cts;

public async Task LongRunningTask()
{
    _cts = new CancellationTokenSource();

    try
    {
        var waitingVm = new WaitingViewModel { Text = "正在处理，点击取消可中断..." };
        _windowManager.ShowWindow(waitingVm);

        await DoLongOperationAsync(_cts.Token);
    }
    catch (OperationCanceledException)
    {
        // 用户取消了操作
    }
    finally
    {
        waitingVm?.RequestClose();
    }
}

public void Cancel()
{
    _cts?.Cancel();
}
```

## 扩展建议

### 1、进度显示

如果需要显示具体进度，可以扩展WaitingViewModel：

```csharp
public class WaitingViewModel : Screen
{
    private string _text = "处理中...";
    public string Text
    {
        get => _text;
        set => SetAndNotify(ref _text, value);
    }

    private double _progress = 0;
    public double Progress
    {
        get => _progress;
        set => SetAndNotify(ref _progress, value);
    }

    private bool _isIndeterminate = true;
    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        set => SetAndNotify(ref _isIndeterminate, value);
    }
}
```

### 2、多种样式

可以根据不同场景提供不同的等待窗口样式：

- **简单等待**：只有进度条和文字
- **详细等待**：包含操作步骤列表
- **带图标等待**：根据操作类型显示相应图标

## 总结

Rouyan的等待窗口实现具有以下优点：

- **设计美观**：采用Material Design风格，视觉效果佳
- **使用简单**：通过Stylet框架轻松管理窗口生命周期
- **功能完善**：支持自定义文案，适应不同使用场景
- **用户友好**：始终置顶显示，避免被遮挡

这种实现方式为WPF应用提供了良好的用户等待体验，值得在类似项目中借鉴和应用。

项目地址：https://github.com/Ming-jiayou/Rouyan
