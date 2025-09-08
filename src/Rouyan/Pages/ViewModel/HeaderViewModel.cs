using System;
using Stylet;

namespace Rouyan.Pages;

public class HeaderViewModel : Screen
{
    private readonly INavigationController navigationController;

    public HeaderViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToHome() => this.navigationController.NavigateToHome();
    public void NavigateToAbout() => this.navigationController.NavigateToAbout();
    public void NavigateToSettings() => this.navigationController.NavigateToSettings();
}