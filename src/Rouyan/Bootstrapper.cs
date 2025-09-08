using Stylet;
using StyletIoC;
using Rouyan.Pages;
using Rouyan.Services;
using System.Windows;

namespace Rouyan;

public class Bootstrapper : Bootstrapper<ShellViewModel>
{
    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        // 绑定导航控制器
        builder.Bind<NavigationController>().And<INavigationController>().To<NavigationController>().InSingletonScope();
        
        // 将HomeViewModel绑定为单例，这样状态会保持一致
        builder.Bind<HomeViewModel>().To<HomeViewModel>().InSingletonScope();
        
        // 绑定页面ViewModel工厂
        builder.Bind<Func<HomeViewModel>>().ToFactory<Func<HomeViewModel>>(c => () => c.Get<HomeViewModel>());
        builder.Bind<Func<AboutViewModel>>().ToFactory<Func<AboutViewModel>>(c => () => c.Get<AboutViewModel>());
        builder.Bind<Func<SettingsViewModel>>().ToFactory<Func<SettingsViewModel>>(c => () => c.Get<SettingsViewModel>());
        
        // 绑定全局快捷键服务为单例
        builder.Bind<HotkeyService>().To<HotkeyService>().InSingletonScope();
    }

    protected override void OnLaunch()
    {
        // 解决循环依赖问题
        NavigationController navigationController = this.Container.Get<NavigationController>();
        navigationController.Delegate = this.RootViewModel;
        navigationController.NavigateToHome();

        // 初始化和获取全局快捷键服务
        try
        {
            var _hotkeyService = this.Container.Get<HotkeyService>();
            if (Application.Current?.MainWindow != null)
            {
                _hotkeyService.Initialize(Application.Current.MainWindow);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化全局快捷键失败: {ex.Message}");
        }
    }
}