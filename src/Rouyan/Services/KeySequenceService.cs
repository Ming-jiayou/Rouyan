using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Rouyan.Services
{
    /// <summary>
    /// 全局热键序列服务，支持多种组合键
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
        private const int VK_T = 0x54;
        private const int VK_C = 0x43;
        private const int VK_M = 0x4D;
        private const int VK_D = 0x44;
        private const int VK_E = 0x45;
        private const int VK_I = 0x49;
        
        // 序列超时时间（毫秒）
        private const int SEQUENCE_TIMEOUT_MS = 2000;

        #endregion

        #region Enums

        private enum HotkeyMode
        {
            None,
            WaitingForC,
            WaitingForD,
            WaitingForI
        }

        #endregion

        #region Fields

        private readonly Action _tCAction;
        private readonly Action _mDAction;
        private readonly Action _eIAction;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;
        
        // 键序列状态
        private HotkeyMode _currentMode = HotkeyMode.None;
        private DateTime _sequenceStartTime = DateTime.MinValue;

        #endregion

        #region Constructor & Initialization

        public KeySequenceService(Window window, Action tcAction, Action mdAction, Action eiAction)
        {
            _tCAction = tcAction ?? throw new ArgumentNullException(nameof(tcAction));
            _mDAction = mdAction ?? throw new ArgumentNullException(nameof(mdAction));
            _eIAction = eiAction ?? throw new ArgumentNullException(nameof(eiAction));
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
                    Console.WriteLine("全局热键已注册：T+C (翻译), M+D (表格翻译), E+I (图片解释)");
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
            switch (_currentMode)
            {
                case HotkeyMode.None:
                    if (vkCode == VK_T)
                    {
                        _currentMode = HotkeyMode.WaitingForC;
                        _sequenceStartTime = DateTime.Now;
                        Console.WriteLine("检测到 T 键，等待按下 C 键...");
                    }
                    else if (vkCode == VK_M)
                    {
                        _currentMode = HotkeyMode.WaitingForD;
                        _sequenceStartTime = DateTime.Now;
                        Console.WriteLine("检测到 M 键，等待按下 D 键...");
                    }
                    else if (vkCode == VK_E)
                    {
                        _currentMode = HotkeyMode.WaitingForI;
                        _sequenceStartTime = DateTime.Now;
                        Console.WriteLine("检测到 E 键，等待按下 I 键...");
                    }
                    break;

                case HotkeyMode.WaitingForC:
                    if (vkCode == VK_C)
                    {
                        if (IsTimeout())
                        {
                            Console.WriteLine("按键序列 T+C 超时");
                        }
                        else
                        {
                            Console.WriteLine("检测到完整组合键 T+C，执行翻译操作...");
                            ExecuteTCAction();
                        }
                    }
                    ResetState();
                    break;

                case HotkeyMode.WaitingForD:
                    if (vkCode == VK_D)
                    {
                        if (IsTimeout())
                        {
                            Console.WriteLine("按键序列 M+D 超时");
                        }
                        else
                        {
                            Console.WriteLine("检测到完整组合键 M+D，执行表格翻译操作...");
                            ExecuteMDAction();
                        }
                    }
                    ResetState();
                    break;

                case HotkeyMode.WaitingForI:
                    if (vkCode == VK_I)
                    {
                        if (IsTimeout())
                        {
                            Console.WriteLine("按键序列 E+I 超时");
                        }
                        else
                        {
                            Console.WriteLine("检测到完整组合键 E+I，执行图片解释操作...");
                            ExecuteEIAction();
                        }
                    }
                    ResetState();
                    break;
            }

            // 检查超时并重置状态
            if (_currentMode != HotkeyMode.None && IsTimeout())
            {
                Console.WriteLine("按键序列超时");
                ResetState();
            }
        }

        private bool IsTimeout()
        {
            return (DateTime.Now - _sequenceStartTime).TotalMilliseconds > SEQUENCE_TIMEOUT_MS;
        }

        private void ResetState()
        {
            _currentMode = HotkeyMode.None;
            _sequenceStartTime = DateTime.MinValue;
        }

        private void ExecuteTCAction()
        {
            ExecuteAction(_tCAction);
        }

        private void ExecuteMDAction()
        {
            ExecuteAction(_mDAction);
        }

        private void ExecuteEIAction()
        {
            ExecuteAction(_eIAction);
        }

        private void ExecuteAction(Action action)
        {
            try
            {
                // 在UI线程上执行操作
                Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action?.Invoke();
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