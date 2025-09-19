using System;
using Rouyan.Pages;
using Stylet;
using Rouyan.Pages.ViewModel;

namespace Rouyan;

// 导航控制器接口
public interface INavigationController
{
    void NavigateToHome();
    void NavigateToPromptManagement();
    void NavigateToSettings();
    void NavigateToAbout();
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
    private readonly Func<PromptManagementViewModel> promptManagementViewModelFactory;
    private readonly Func<SettingsViewModel> settingsViewModelFactory;
    private readonly Func<AboutViewModel> aboutViewModelFactory;

    public INavigationControllerDelegate Delegate { get; set; }

    public NavigationController(Func<HomeViewModel> homeViewModelFactory,
        Func<PromptManagementViewModel> promptManagementViewModelFactory,
        Func<SettingsViewModel> settingsViewModelFactory,
        Func<AboutViewModel> aboutViewModelFactory)
    {
        this.homeViewModelFactory = homeViewModelFactory ?? throw new ArgumentNullException(nameof(homeViewModelFactory));
        this.promptManagementViewModelFactory = promptManagementViewModelFactory ?? throw new ArgumentNullException(nameof(promptManagementViewModelFactory));
        this.settingsViewModelFactory = settingsViewModelFactory ?? throw new ArgumentNullException(nameof(settingsViewModelFactory));
        this.aboutViewModelFactory = aboutViewModelFactory ?? throw new ArgumentNullException(nameof(aboutViewModelFactory));
    }

    public void NavigateToHome()
    {
        this.Delegate?.NavigateTo(this.homeViewModelFactory());
    }

    public void NavigateToPromptManagement()
    {
        var vm = this.promptManagementViewModelFactory();
        this.Delegate?.NavigateTo(vm);
    }

    public void NavigateToSettings()
    {
        var vm = this.settingsViewModelFactory();
        this.Delegate?.NavigateTo(vm);
    }

    public void NavigateToAbout()
    {
        var vm = this.aboutViewModelFactory();
        this.Delegate?.NavigateTo(vm);
    }
}