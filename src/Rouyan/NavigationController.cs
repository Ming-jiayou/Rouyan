using System;
using Rouyan.Pages;
using Stylet;

namespace Rouyan;

// 导航控制器接口
public interface INavigationController
{
    void NavigateToHome();
    void NavigateToAbout();
    void NavigateToSettings();
}

// 导航委托接口
public interface INavigationControllerDelegate
{
    void NavigateTo(IScreen screen);
}

// 导航控制器实现
public class NavigationController : INavigationController
{
    private readonly Func<HomeViewModel> homeViewModelFactory;
    private readonly Func<AboutViewModel> aboutViewModelFactory;
    private readonly Func<SettingsViewModel> settingsViewModelFactory;

    public INavigationControllerDelegate Delegate { get; set; }

    public NavigationController(Func<HomeViewModel> homeViewModelFactory, 
        Func<AboutViewModel> aboutViewModelFactory, 
        Func<SettingsViewModel> settingsViewModelFactory)
    {
        this.homeViewModelFactory = homeViewModelFactory ?? throw new ArgumentNullException(nameof(homeViewModelFactory));
        this.aboutViewModelFactory = aboutViewModelFactory ?? throw new ArgumentNullException(nameof(aboutViewModelFactory));
        this.settingsViewModelFactory = settingsViewModelFactory ?? throw new ArgumentNullException(nameof(settingsViewModelFactory));
    }

    public void NavigateToHome()
    {
        this.Delegate?.NavigateTo(this.homeViewModelFactory());
    }

    public void NavigateToAbout()
    {
        var vm = this.aboutViewModelFactory();
        this.Delegate?.NavigateTo(vm);
    }

    public void NavigateToSettings()
    {
        var vm = this.settingsViewModelFactory();
        this.Delegate?.NavigateTo(vm);
    }
}