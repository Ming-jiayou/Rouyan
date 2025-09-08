using System;
using System.Threading.Tasks;
using StyletIoC;
using Rouyan.Pages;
using System.Windows;

namespace Rouyan.Services
{
    /// <summary>
    /// 全局热键服务管理器
    /// 负责初始化和管理Ctrl+T+C热键组合
    /// </summary>
    public class HotkeyService : IDisposable
    {
        private KeySequenceService? _keySequenceService;
        private readonly IContainer _container;

        public HotkeyService(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        /// <summary>
        /// 初始化热键服务
        /// </summary>
        /// <param name="mainWindow">主窗口</param>
        public void Initialize(Window mainWindow)
        {
            try
            {
                _keySequenceService = new KeySequenceService(mainWindow, ExecuteTranslateAction);
                _keySequenceService.RegisterHotKeys();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化热键服务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行翻译操作
        /// 当检测到Ctrl+T+C组合键时调用
        /// </summary>
        private async void ExecuteTranslateAction()
        {
            try
            {
                // 获取HomeViewModel单例实例
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.TranslateToChinese();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行热键操作失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _keySequenceService?.Dispose();
        }
    }
}