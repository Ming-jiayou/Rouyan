using System;
using System.Threading.Tasks;
using System.Windows;
using Stylet;
using Rouyan.Services;
using Rouyan.Interfaces;
using Rouyan.Models;

namespace Rouyan.Pages.ViewModel;

public class SettingsViewModel : Screen
{   
    private readonly IEnvConfigService envConfigService;

    private string _chatApiKey = "";
    private string _chatBaseUrl = "";
    private string _chatModel = "";
    private string _visionApiKey = "";
    private string _visionBaseUrl = "";
    private string _visionModel = "";

    public SettingsViewModel(IEnvConfigService envConfigService)
    {
        this.envConfigService = envConfigService ?? throw new ArgumentNullException(nameof(envConfigService));
    }

    public string ChatApiKey
    {
        get => _chatApiKey;
        set => SetAndNotify(ref _chatApiKey, value);
    }

    public string ChatBaseUrl
    {
        get => _chatBaseUrl;
        set => SetAndNotify(ref _chatBaseUrl, value);
    }

    public string ChatModel
    {
        get => _chatModel;
        set => SetAndNotify(ref _chatModel, value);
    }

    public string VisionApiKey
    {
        get => _visionApiKey;
        set => SetAndNotify(ref _visionApiKey, value);
    }

    public string VisionBaseUrl
    {
        get => _visionBaseUrl;
        set => SetAndNotify(ref _visionBaseUrl, value);
    }

    public string VisionModel
    {
        get => _visionModel;
        set => SetAndNotify(ref _visionModel, value);
    }

    protected override async void OnActivate()
    {
        base.OnActivate();
        await LoadConfigAsync();
    }

    private async Task LoadConfigAsync()
    {
        try
        {
            var config = await envConfigService.LoadConfigAsync();
            ChatApiKey = config.ChatApiKey;
            ChatBaseUrl = config.ChatBaseUrl;
            ChatModel = config.ChatModel;
            VisionApiKey = config.VisionApiKey;
            VisionBaseUrl = config.VisionBaseUrl;
            VisionModel = config.VisionModel;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public async void SaveConfig()
    {
        try
        {
            var config = new EnvConfig
            {
                ChatApiKey = ChatApiKey,
                ChatBaseUrl = ChatBaseUrl,
                ChatModel = ChatModel,
                VisionApiKey = VisionApiKey,
                VisionBaseUrl = VisionBaseUrl,
                VisionModel = VisionModel
            };

            await envConfigService.SaveConfigAsync(config);
            MessageBox.Show("配置保存成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}