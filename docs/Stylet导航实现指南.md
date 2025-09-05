# 使用Stylet实现导航功能完整指南

Stylet是一个轻量级的WPF MVVM框架，提供了简洁的方式来实现导航功能。本指南将详细介绍如何使用Stylet实现页面导航，基于官方示例进行说明。

## 目录结构

首先，让我们了解一下典型的Stylet导航项目结构：

```
YourProject/
├── App.xaml
├── App.xaml.cs
├── Bootstrapper.cs
├── NavigationController.cs
├── YourProject.csproj
└── Pages/
    ├── ShellView.xaml
    ├── ShellViewModel.cs
    ├── HeaderView.xaml
    ├── HeaderViewModel.cs
    ├── Page1View.xaml
    ├── Page1ViewModel.cs
    ├── Page2View.xaml
    └── Page2ViewModel.cs
```

## 核心组件说明

### 1. 应用程序入口 - App.xaml

在`App.xaml`中配置Stylet的ApplicationLoader和Bootstrapper：

```xml
<Application x:Class="YourProject.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:local="clr-namespace:YourProject"
             xmlns:s="https://github.com/canton7/Stylet">
    <Application.Resources>
        <s:ApplicationLoader>
            <s:ApplicationLoader.Bootstrapper>
                <local:Bootstrapper />
            </s:ApplicationLoader.Bootstrapper>
        </s:ApplicationLoader>
    </Application.Resources>
</Application>
```

### 2. 启动配置 - Bootstrapper.cs

Bootstrapper负责配置IoC容器和应用程序启动逻辑：

```csharp
using System;
using Stylet;
using StyletIoC;
using YourProject.Pages;

namespace YourProject;

public class Bootstrapper : Bootstrapper<ShellViewModel>
{
    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        // 绑定导航控制器
        builder.Bind<NavigationController>().And<INavigationController>().To<NavigationController>().InSingletonScope();
        
        // 绑定页面ViewModel工厂
        builder.Bind<Func<Page1ViewModel>>().ToFactory<Func<Page1ViewModel>>(c => () => c.Get<Page1ViewModel>());
        builder.Bind<Func<Page2ViewModel>>().ToFactory<Func<Page2ViewModel>>(c => () => c.Get<Page2ViewModel>());
    }

    protected override void OnLaunch()
    {
        // 解决循环依赖问题
        NavigationController navigationController = this.Container.Get<NavigationController>();
        navigationController.Delegate = this.RootViewModel;
        navigationController.NavigateToPage1();
    }
}
```

### 3. 导航控制器 - NavigationController.cs

导航控制器是实现导航逻辑的核心组件：

```csharp
using System;

namespace YourProject;

// 导航控制器接口
public interface INavigationController
{
    void NavigateToPage1();
    void NavigateToPage2(string initiator);
}

// 导航委托接口
public interface INavigationControllerDelegate
{
    void NavigateTo(IScreen screen);
}

// 导航控制器实现
public class NavigationController : INavigationController
{
    private readonly Func<Page1ViewModel> page1ViewModelFactory;
    private readonly Func<Page2ViewModel> page2ViewModelFactory;

    public INavigationControllerDelegate Delegate { get; set; }

    public NavigationController(Func<Page1ViewModel> page1ViewModelFactory, Func<Page2ViewModel> page2ViewModelFactory)
    {
        this.page1ViewModelFactory = page1ViewModelFactory ?? throw new ArgumentNullException(nameof(page1ViewModelFactory));
        this.page2ViewModelFactory = page2ViewModelFactory ?? throw new ArgumentNullException(nameof(page2ViewModelFactory));
    }

    public void NavigateToPage1()
    {
        this.Delegate?.NavigateTo(this.page1ViewModelFactory());
    }

    public void NavigateToPage2(string initiator)
    {
        Page2ViewModel vm = this.page2ViewModelFactory();
        vm.Initiator = initiator;
        this.Delegate?.NavigateTo(vm);
    }
}
```

### 4. 主窗口 - ShellView.xaml 和 ShellViewModel.cs

Shell是应用程序的主窗口，负责显示导航栏和当前页面：

**ShellView.xaml:**
```xml
<Window x:Class="YourProject.Pages.ShellView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
        xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
        xmlns:local="clr-namespace:YourProject.Pages"
        mc:Ignorable="d"
        Title="Navigation Controller sample" Height="450" Width="800"
        xmlns:s="https://github.com/canton7/Stylet"
        d:DataContext="{d:DesignInstance local:ShellViewModel}">
    <DockPanel>
        <!-- 导航栏 -->
        <ContentControl DockPanel.Dock="Top" s:View.Model="{Binding HeaderViewModel}"/>
        <!-- 页面内容区域 -->
        <ContentControl s:View.Model="{Binding ActiveItem}"/>
    </DockPanel>
</Window>
```

**ShellViewModel.cs:**
```csharp
using System;
using Stylet;

namespace YourProject.Pages;

public class ShellViewModel : Conductor<IScreen>, INavigationControllerDelegate
{
    public HeaderViewModel HeaderViewModel { get; }

    public ShellViewModel(HeaderViewModel headerViewModel)
    {
        this.HeaderViewModel = headerViewModel ?? throw new ArgumentNullException(nameof(headerViewModel));
    }

    public void NavigateTo(IScreen screen)
    {
        this.ActivateItem(screen);
    }
}
```

### 5. 导航栏 - HeaderView.xaml 和 HeaderViewModel.cs

导航栏提供页面切换的按钮：

**HeaderView.xaml:**
```xml
<UserControl x:Class="YourProject.Pages.HeaderView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:local="clr-namespace:YourProject.Pages"
             xmlns:s="https://github.com/canton7/Stylet"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
    <StackPanel Orientation="Horizontal">
        <TextBlock>Go to: </TextBlock>
        <Button Margin="5,0" Command="{s:Action NavigateToPage1}">Page 1</Button>
        <Button Margin="5,0" Command="{s:Action NavigateToPage2}">Page 2</Button>
    </StackPanel>
</UserControl>
```

**HeaderViewModel.cs:**
```csharp
using System;

namespace YourProject.Pages;

public class HeaderViewModel : Screen
{
    private readonly INavigationController navigationController;

    public HeaderViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToPage1() => this.navigationController.NavigateToPage1();
    public void NavigateToPage2() => this.navigationController.NavigateToPage2("the Header");
}
```

### 6. 页面示例 - Page1 和 Page2

**Page1View.xaml:**
```xml
<UserControl x:Class="YourProject.Pages.Page1View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:s="https://github.com/canton7/Stylet"
             xmlns:local="clr-namespace:YourProject.Pages"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
    <Grid HorizontalAlignment="Center" VerticalAlignment="Center">
        <StackPanel>
            <TextBlock>This is page 1</TextBlock>
            <Button Command="{s:Action NavigateToPage2}">Go to page 2</Button>
        </StackPanel>
    </Grid>
</UserControl>
```

**Page1ViewModel.cs:**
```csharp
using System;

namespace YourProject.Pages;

public class Page1ViewModel : Screen
{
    private readonly INavigationController navigationController;

    public Page1ViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToPage2() => this.navigationController.NavigateToPage2("Page 1");
}
```

**Page2View.xaml:**
```xml
<UserControl x:Class="YourProject.Pages.Page2View"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006" 
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008" 
             xmlns:s="https://github.com/canton7/Stylet"
             xmlns:local="clr-namespace:YourProject.Pages"
             mc:Ignorable="d" 
             d:DesignHeight="450" d:DesignWidth="800">
    <Grid HorizontalAlignment="Center" VerticalAlignment="Center">
        <StackPanel>
            <TextBlock>This is page 2</TextBlock>
            <TextBlock Text="{Binding Initiator, StringFormat='You got here from {0}'}"/>
            <Button Command="{s:Action NavigateToPage1}">Go to page 1</Button>
        </StackPanel>
    </Grid>
</UserControl>
```

**Page2ViewModel.cs:**
```csharp
using System;

namespace YourProject.Pages;

public class Page2ViewModel : Screen
{
    private readonly INavigationController navigationController;

    private string _initiator;
    public string Initiator
    {
        get => this._initiator;
        set => this.SetAndNotify(ref this._initiator, value);
    }

    public Page2ViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToPage1() => this.navigationController.NavigateToPage1();
}
```

## 实现步骤总结

1. **配置应用程序入口**：在`App.xaml`中设置Stylet的ApplicationLoader和Bootstrapper
2. **创建导航控制器**：定义`INavigationController`接口和`NavigationController`实现类
3. **实现主窗口**：创建`ShellViewModel`继承`Conductor<IScreen>`并实现`INavigationControllerDelegate`接口
4. **创建导航栏**：实现`HeaderViewModel`用于页面切换
5. **创建页面**：为每个页面创建View和ViewModel
6. **配置IoC容器**：在`Bootstrapper`中绑定所有组件
7. **处理循环依赖**：在`OnLaunch`方法中解决ShellViewModel和NavigationController之间的循环依赖

## 关键概念解释

### 1. 控制反转(IoC)和依赖注入(DI)
Stylet使用IoC容器来管理对象的生命周期和依赖关系。通过`ConfigureIoC`方法配置绑定关系。

### 2. 工厂模式
使用`ToFactory`创建ViewModel工厂，避免直接创建实例，提高代码的可测试性和灵活性。

### 3. Conductor模式
`ShellViewModel`继承自`Conductor<IScreen>`，负责管理子屏幕（页面）的激活和停用。

### 4. 循环依赖处理
由于ShellViewModel -> HeaderViewModel -> NavigationController -> ShellViewModel形成了循环依赖，需要在启动时手动设置委托关系。

## 最佳实践

1. **使用接口抽象**：定义`INavigationController`和`INavigationControllerDelegate`接口，提高代码的可测试性和可维护性
2. **工厂模式创建ViewModel**：使用工厂模式创建页面ViewModel，避免紧耦合
3. **合理使用IoC容器**：在Bootstrapper中正确配置依赖关系
4. **处理循环依赖**：在OnLaunch方法中手动解决循环依赖问题
5. **使用s:Action命令绑定**：在XAML中使用Stylet的Action命令绑定简化事件处理

通过以上步骤和组件，您可以轻松地在WPF应用程序中使用Stylet实现灵活的导航功能。