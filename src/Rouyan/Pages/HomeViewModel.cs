using System;
using Stylet;

namespace Rouyan.Pages;

public class HomeViewModel : Screen
{
    private readonly INavigationController navigationController;
    
    public HomeViewModel(INavigationController navigationController)
    {
        this.navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }
    
    public void NavigateToAbout()
    {
        this.navigationController.NavigateToAbout();
    }
    
    public void NavigateToSettings()
    {
        this.navigationController.NavigateToSettings();
    }
}