using Stylet;
using StyletIoC;
using Rouyan.Pages;
using Rouyan.Services;
using System.Windows;
using Rouyan.Pages.ViewModel;
using Rouyan.Interfaces;

namespace Rouyan;

public class Bootstrapper : Bootstrapper<ShellViewModel>
{
    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        // 绑定导航控制器
        builder.Bind<NavigationController>().And<INavigationController>().To<NavigationController>().InSingletonScope();

        // 绑定环境配置服务
        builder.Bind<IEnvConfigService>().To<EnvConfigService>().InSingletonScope();

        // 绑定提示词管理服务
        builder.Bind<PromptManagementService>().To<PromptManagementService>().InSingletonScope();

        // 将HomeViewModel绑定为单例，这样状态会保持一致
        builder.Bind<HomeViewModel>().To<HomeViewModel>().InSingletonScope();

        // 绑定页面ViewModel工厂
        builder.Bind<Func<HomeViewModel>>().ToFactory<Func<HomeViewModel>>(c => () => c.Get<HomeViewModel>());
        builder.Bind<Func<PromptManagementViewModel>>().ToFactory<Func<PromptManagementViewModel>>(c => () => c.Get<PromptManagementViewModel>());
        builder.Bind<Func<SettingsViewModel>>().ToFactory<Func<SettingsViewModel>>(c => () => c.Get<SettingsViewModel>());
        builder.Bind<Func<AboutViewModel>>().ToFactory<Func<AboutViewModel>>(c => () => c.Get<AboutViewModel>());
        builder.Bind<Func<TerminalAgentViewModel>>().ToFactory<Func<TerminalAgentViewModel>>(c => () => c.Get<TerminalAgentViewModel>());

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