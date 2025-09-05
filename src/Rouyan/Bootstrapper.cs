using Stylet;
using StyletIoC;
using Rouyan.Pages;

namespace Rouyan;

public class Bootstrapper : Bootstrapper<ShellViewModel>
{
    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        // 绑定导航控制器
        builder.Bind<NavigationController>().And<INavigationController>().To<NavigationController>().InSingletonScope();
        
        // 绑定页面ViewModel工厂
        builder.Bind<Func<HomeViewModel>>().ToFactory<Func<HomeViewModel>>(c => () => c.Get<HomeViewModel>());
        builder.Bind<Func<AboutViewModel>>().ToFactory<Func<AboutViewModel>>(c => () => c.Get<AboutViewModel>());
        builder.Bind<Func<SettingsViewModel>>().ToFactory<Func<SettingsViewModel>>(c => () => c.Get<SettingsViewModel>());
    }

    protected override void OnLaunch()
    {
        // 解决循环依赖问题
        NavigationController navigationController = this.Container.Get<NavigationController>();
        navigationController.Delegate = this.RootViewModel;
        navigationController.NavigateToHome();
    }
}