using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace Rouyan.Services
{
    /// <summary>
    /// 全局热键序列服务，支持Tab+字母组合键
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

        // 按键常量（Tab + 字母 序列）
        private const int VK_TAB = 0x09;
        private const int VK_K = 0x4B;
        private const int VK_L = 0x4C;
        private const int VK_U = 0x55;
        private const int VK_I = 0x49;
        private const int VK_S = 0x53;
        private const int VK_D = 0x44;
        private const int VK_W = 0x57;
        private const int VK_E = 0x45;

        // 序列超时时间（毫秒）
        private const int SEQUENCE_TIMEOUT_MS = 2000;

        #endregion

        #region Enums

        private enum HotkeyMode
        {
            None,
            WaitingAfterTab
        }

        #endregion

        #region Fields

        private readonly Action _runLLMPrompt1;
        private readonly Action _runLLMPrompt1Streaming;
        private readonly Action _runLLMPrompt2;
        private readonly Action _runLLMPrompt2Streaming;
        private readonly Action _runVLMPrompt1;
        private readonly Action _runVLMPrompt1Streaming;
        private readonly Action _runVLMPrompt2;
        private readonly Action _runVLMPrompt2Streaming;
        private IntPtr _hookID = IntPtr.Zero;
        private readonly LowLevelKeyboardProc _proc;

        // 键序列状态
        private HotkeyMode _currentMode = HotkeyMode.None;
        private DateTime _sequenceStartTime = DateTime.MinValue;

        #endregion

        #region Constructor & Initialization

        public KeySequenceService(
            Action runLLMPrompt1,
            Action runLLMPrompt1Streaming,
            Action runLLMPrompt2,
            Action runLLMPrompt2Streaming,
            Action runVLMPrompt1,
            Action runVLMPrompt1Streaming,
            Action runVLMPrompt2,
            Action runVLMPrompt2Streaming)
        {
            _runLLMPrompt1 = runLLMPrompt1 ?? throw new ArgumentNullException(nameof(runLLMPrompt1));
            _runLLMPrompt1Streaming = runLLMPrompt1Streaming ?? throw new ArgumentNullException(nameof(runLLMPrompt1Streaming));
            _runLLMPrompt2 = runLLMPrompt2 ?? throw new ArgumentNullException(nameof(runLLMPrompt2));
            _runLLMPrompt2Streaming = runLLMPrompt2Streaming ?? throw new ArgumentNullException(nameof(runLLMPrompt2Streaming));
            _runVLMPrompt1 = runVLMPrompt1 ?? throw new ArgumentNullException(nameof(runVLMPrompt1));
            _runVLMPrompt1Streaming = runVLMPrompt1Streaming ?? throw new ArgumentNullException(nameof(runVLMPrompt1Streaming));
            _runVLMPrompt2 = runVLMPrompt2 ?? throw new ArgumentNullException(nameof(runVLMPrompt2));
            _runVLMPrompt2Streaming = runVLMPrompt2Streaming ?? throw new ArgumentNullException(nameof(runVLMPrompt2Streaming));
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
                    Console.WriteLine("全局热键已注册：\n" +
                        "Tab+K (RunLLMPrompt1)\n" +
                        "Tab+L (RunLLMPrompt1Streaming)\n" +
                        "Tab+U (RunLLMPrompt2)\n" +
                        "Tab+I (RunLLMPrompt2Streaming)\n" +
                        "Tab+S (RunVLMPrompt1)\n" +
                        "Tab+D (RunVLMPrompt1Streaming)\n" +
                        "Tab+W (RunVLMPrompt2)\n" +
                        "Tab+E (RunVLMPrompt2Streaming)");
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
                    if (vkCode == VK_TAB)
                    {
                        _currentMode = HotkeyMode.WaitingAfterTab;
                        _sequenceStartTime = DateTime.Now;
                        Console.WriteLine("检测到 Tab 键，等待按下后续字母键...");
                    }
                    break;

                case HotkeyMode.WaitingAfterTab:
                    if (IsTimeout())
                    {
                        Console.WriteLine("按键序列超时");
                    }
                    else
                    {
                        switch (vkCode)
                        {
                            case VK_K:
                                Console.WriteLine("检测到完整组合键 Tab+K，执行 RunLLMPrompt1...");
                                ExecuteAction(_runLLMPrompt1);
                                break;

                            case VK_L:
                                Console.WriteLine("检测到完整组合键 Tab+L，执行 RunLLMPrompt1Streaming...");
                                ExecuteAction(_runLLMPrompt1Streaming);
                                break;

                            case VK_U:
                                Console.WriteLine("检测到完整组合键 Tab+U，执行 RunLLMPrompt2...");
                                ExecuteAction(_runLLMPrompt2);
                                break;

                            case VK_I:
                                Console.WriteLine("检测到完整组合键 Tab+I，执行 RunLLMPrompt2Streaming...");
                                ExecuteAction(_runLLMPrompt2Streaming);
                                break;

                            case VK_S:
                                Console.WriteLine("检测到完整组合键 Tab+S，执行 RunVLMPrompt1...");
                                ExecuteAction(_runVLMPrompt1);
                                break;

                            case VK_D:
                                Console.WriteLine("检测到完整组合键 Tab+D，执行 RunVLMPrompt1Streaming...");
                                ExecuteAction(_runVLMPrompt1Streaming);
                                break;

                            case VK_W:
                                Console.WriteLine("检测到完整组合键 Tab+W，执行 RunVLMPrompt2...");
                                ExecuteAction(_runVLMPrompt2);
                                break;

                            case VK_E:
                                Console.WriteLine("检测到完整组合键 Tab+E，执行 RunVLMPrompt2Streaming...");
                                ExecuteAction(_runVLMPrompt2Streaming);
                                break;

                            default:
                                Console.WriteLine($"检测到 Tab 后的无效按键: {vkCode}");
                                break;
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
