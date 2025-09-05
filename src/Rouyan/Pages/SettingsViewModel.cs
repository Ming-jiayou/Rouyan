using System;
using Stylet;

namespace Rouyan.Pages;

public class SettingsViewModel : Screen
{
    private readonly INavigationController navigationController;
    
    public SettingsViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }
    
    public void NavigateToHome()
    {
        this.navigationController.NavigateToHome();
    }
    
    public void NavigateToAbout()
    {
        this.navigationController.NavigateToAbout();
    }
}