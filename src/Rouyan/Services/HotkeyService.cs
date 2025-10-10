using System;
using System.Threading.Tasks;
using StyletIoC;
using Rouyan.Pages;
using System.Windows;
using Rouyan.Pages.ViewModel;

namespace Rouyan.Services
{
    /// <summary>
    /// 全局热键服务管理器
    /// 负责初始化和管理Tab+字母热键组合以及全局ESC键
    /// </summary>
    public class HotkeyService : IDisposable
    {
        private KeySequenceService? _keySequenceService;
        private GlobalEscService? _globalEscService;
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
                // 初始化Tab+字母组合键服务
                _keySequenceService = new KeySequenceService(
                    ExecuteRunLLMPrompt1,
                    ExecuteRunLLMPrompt1Streaming,
                    ExecuteRunLLMPrompt2,
                    ExecuteRunLLMPrompt2Streaming,
                    ExecuteRunVLMPrompt1,
                    ExecuteRunVLMPrompt1Streaming,
                    ExecuteRunVLMPrompt2,
                    ExecuteRunVLMPrompt2Streaming);
                _keySequenceService.RegisterHotKeys();

                // 初始化全局ESC键服务
                _globalEscService = new GlobalEscService();
                _globalEscService.Register();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化热键服务失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunLLMPrompt1操作
        /// 当检测到 Tab+K 组合键时调用
        /// </summary>
        private async void ExecuteRunLLMPrompt1()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunLLMPrompt1();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+K热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunLLMPrompt1Streaming操作
        /// 当检测到 Tab+L 组合键时调用
        /// </summary>
        private async void ExecuteRunLLMPrompt1Streaming()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunLLMPrompt1Streaming();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+L热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunLLMPrompt2操作
        /// 当检测到 Tab+U 组合键时调用
        /// </summary>
        private async void ExecuteRunLLMPrompt2()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunLLMPrompt2();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+U热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunLLMPrompt2Streaming操作
        /// 当检测到 Tab+I 组合键时调用
        /// </summary>
        private async void ExecuteRunLLMPrompt2Streaming()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunLLMPrompt2Streaming();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+I热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunVLMPrompt1操作
        /// 当检测到 Tab+S 组合键时调用
        /// </summary>
        private async void ExecuteRunVLMPrompt1()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunVLMPrompt1();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+S热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunVLMPrompt1Streaming操作
        /// 当检测到 Tab+D 组合键时调用
        /// </summary>
        private async void ExecuteRunVLMPrompt1Streaming()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunVLMPrompt1Streaming();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+D热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunVLMPrompt2操作
        /// 当检测到 Tab+W 组合键时调用
        /// </summary>
        private async void ExecuteRunVLMPrompt2()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunVLMPrompt2();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+W热键操作失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行RunVLMPrompt2Streaming操作
        /// 当检测到 Tab+E 组合键时调用
        /// </summary>
        private async void ExecuteRunVLMPrompt2Streaming()
        {
            try
            {
                var homeViewModel = _container.Get<HomeViewModel>();
                if (homeViewModel != null)
                {
                    await homeViewModel.RunVLMPrompt2Streaming();
                }
                else
                {
                    Console.WriteLine("警告: 无法获取HomeViewModel实例");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行Tab+E热键操作失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _keySequenceService?.Dispose();
            _globalEscService?.Dispose();
        }
    }
}
