using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Rouyan.Services
{
    /// <summary>
    /// 全局热键序列服务，支持Ctrl+T+C组合键
    /// </summary>
    public class KeySequenceService : IDisposable
    {
        #region Win32 APIs
        
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #endregion

        #region Constants

        // 低级键盘钩子常量
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        
        // 按键常量
        private const int VK_CONTROL = 0x11;
        private const int VK_T = 0x54;
        private const int VK_C = 0x43;
        
        // 序列超时时间（毫秒）
        private const int SEQUENCE_TIMEOUT_MS = 2000;

        #endregion

        #region Fields

        private readonly Action _executeAction;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        
        // 键序列状态
        private bool _waitingForC = false;
        private DateTime _lastTKeyTime = DateTime.MinValue;

        #endregion

        #region Constructor & Initialization

        public KeySequenceService(Window window, Action executeAction)
        {
            _executeAction = executeAction ?? throw new ArgumentNullException(nameof(executeAction));
            _proc = HookCallback;
        }

        public void RegisterHotKeys()
        {
            try
            {
                _hookID = SetHook(_proc);
                if (_hookID == IntPtr.Zero)
                {
                    Console.WriteLine("警告: 无法安装全局键盘钩子");
                }
                else
                {
                    Console.WriteLine("全局热键 Ctrl+T+C 已注册");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"注册热键失败: {ex.Message}");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = System.Diagnostics.Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            
            if (curModule?.ModuleName != null)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
            return IntPtr.Zero;
        }

        #endregion

        #region Keyboard Hook Logic

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                HandleKeyDown(vkCode);
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private void HandleKeyDown(int vkCode)
        {
            bool ctrlPressed = (GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0;

            if (ctrlPressed && vkCode == VK_T)
            {
                // 检测到 Ctrl+T，开始等待 C 键
                _waitingForC = true;
                _lastTKeyTime = DateTime.Now;
                Console.WriteLine("检测到 Ctrl+T，等待按下 C 键...");
            }
            else if (_waitingForC && vkCode == VK_C)
            {
                // 检查是否在超时时间内按下了 C 键
                if ((DateTime.Now - _lastTKeyTime).TotalMilliseconds <= SEQUENCE_TIMEOUT_MS)
                {
                    Console.WriteLine("检测到完整组合键 Ctrl+T+C，执行操作...");
                    ResetState();
                    ExecuteAction();
                }
                else
                {
                    Console.WriteLine("按键超时，重置状态");
                    ResetState();
                }
            }
            else if (_waitingForC)
            {
                // 按下了其他键或超时，重置状态
                if ((DateTime.Now - _lastTKeyTime).TotalMilliseconds > SEQUENCE_TIMEOUT_MS)
                {
                    Console.WriteLine("按键序列超时");
                }
                ResetState();
            }
        }

        private void ResetState()
        {
            _waitingForC = false;
            _lastTKeyTime = DateTime.MinValue;
        }

        private void ExecuteAction()
        {
            try
            {
                // 在UI线程上执行操作
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        _executeAction?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"执行热键操作时出错: {ex.Message}");
                    }
                }), DispatcherPriority.Normal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调度热键操作时出错: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            try
            {
                if (_hookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_hookID);
                    _hookID = IntPtr.Zero;
                    Console.WriteLine("全局热键已卸载");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理热键资源时出错: {ex.Message}");
            }
        }

        #endregion
    }
}