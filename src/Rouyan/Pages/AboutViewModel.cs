using System;
using Stylet;

namespace Rouyan.Pages;

public class AboutViewModel : Screen
{
    private readonly INavigationController navigationController;
    
    public AboutViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }
    
    public void NavigateToHome()
    {
        this.navigationController.NavigateToHome();
    }
    
    public void NavigateToSettings()
    {
        this.navigationController.NavigateToSettings();
    }
}