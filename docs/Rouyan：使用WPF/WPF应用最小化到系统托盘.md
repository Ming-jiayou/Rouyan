# WPF应用最小化到系统托盘

## 前言

在现代桌面应用开发中，<span style="color: dodgerblue;">系统托盘（System Tray）</span>功能已经成为用户体验的重要组成部分。特别是对于需要长时间运行的应用程序，如即时通讯工具、音乐播放器、系统监控工具等，最小化到系统托盘不仅可以节省任务栏空间，还能让应用在后台持续运行，随时为用户提供服务。

本文以[Rouyan](https://github.com/Ming-jiayou/Rouyan)这个WPF应用为例，说明在WPF中如何实现最小化到系统托盘。

## 选择

在WPF中实现系统托盘功能，开发者通常有以下几种选择：

*1、System.Windows.Forms.NotifyIcon：最传统的方式，需要引用WinForms*

*2、Hardcodet.NotifyIcon.Wpf：专为WPF设计的托盘组件*

*3、H.NotifyIcon.Wpf：Hardcodet的现代维护版本*

这里我选择的是<span style="color: dodgerblue;">H.NotifyIcon.Wpf</span>，选择H.NotifyIcon.Wpf的优势：

*1、现代化维护：相比原版Hardcodet，H.NotifyIcon.Wpf有更活跃的维护和bug修复*

*2、完全兼容：保留了原版的API和XAML命名空间，迁移成本为零*

*3、WPF原生：无需引入WinForms依赖，与WPF的数据绑定和样式系统完美集成*

*4、功能丰富：支持气泡提示、上下文菜单、多种鼠标事件等*

H.NotifyIcon.Wpf项目地址：*https://github.com/HavenDV/H.NotifyIcon*

![](https://files.mdnice.com/user/50031/35eba36e-2a6f-42ec-9e61-9f922369911e.png)

## 使用


先描述一下，我们想要实现的效果，我想要点击最小化与关闭的时候，让这个应用最小化到系统托盘，然后点击系统托盘的图标显示这个应用，或者右键系统托盘的图标，有两个选项，一个是显示窗口，一个是退出，点击退出才真的退出程序。

第一步安装nuget包：

![](https://files.mdnice.com/user/50031/63fae3fb-1999-4837-bd96-05c561c087eb.png)

第二步在主窗口中添加控件：

先添加`xmlns:tb="http://www.hardcodet.net/taskbar"`与`StateChanged="Window_StateChanged"`。


![](https://files.mdnice.com/user/50031/c2d3c8ed-8224-48c8-91b7-785a2d70b7c5.png)


```xaml
 <!-- 系统托盘图标 -->
 <tb:TaskbarIcon x:Name="TrayIcon"
          IconSource="/Assets/福州肉燕.ico"
          ToolTipText="Rouyan"
          TrayLeftMouseDown="TrayIcon_TrayLeftMouseDown">
     <tb:TaskbarIcon.ContextMenu>
         <ContextMenu>
             <MenuItem Header="显示窗口" Click="ShowWindow_Click"/>
             <Separator/>
             <MenuItem Header="退出" Click="ExitApp_Click"/>
         </ContextMenu>
     </tb:TaskbarIcon.ContextMenu>
 </tb:TaskbarIcon>
```
ToolTipText是你鼠标悬浮在图标那会出现的文字，TrayLeftMouseDown是鼠标左键点击系统托盘图标事件，ContextMenu是右键系统托盘图标会出现的选项。

现在在code-behind也就是主页面的xaml.cs中写这些事件处理程序即可。

首先关闭应用时，让其不关闭而是最小化：

```csharp
protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
{
    e.Cancel = true;
    WindowState = WindowState.Minimized;
}
```

然后是窗体改变事件处理程序：

```csharp
 private void Window_StateChanged(object sender, System.EventArgs e)
 {
     if (WindowState == WindowState.Minimized)
     {
         Hide();
         TrayIcon.Visibility = Visibility.Visible;
     }
 }
```

鼠标左键点击系统托盘图标：

```csharp
 private void TrayIcon_TrayLeftMouseDown(object sender, RoutedEventArgs e)
 {
     ShowMainWindow();
 }
 
 private void ShowMainWindow()
{
    Show();
    WindowState = WindowState.Normal;
    Activate();
    TrayIcon.Visibility = Visibility.Collapsed;
}
```

鼠标右键系统托盘出现的显示窗口与退出的事件处理程序：

```csharp
private void ShowWindow_Click(object sender, RoutedEventArgs e)
 {
     ShowMainWindow();
 }

 private void ExitApp_Click(object sender, RoutedEventArgs e)
 {
     TrayIcon.Dispose();
     Application.Current.Shutdown();
 }
```

全部代码：

![](https://files.mdnice.com/user/50031/3b550e81-613e-410d-bf25-ce751ec9d4f3.png)

项目地址：https://github.com/Ming-jiayou/Rouyan

最终效果：

这样就成功实现了在WPF应用中实现最小化到系统托盘，希望对你有所帮助。