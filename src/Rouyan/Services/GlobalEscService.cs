using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows;

namespace Rouyan.Services
{
    /// <summary>
    /// 全局ESC键服务
    /// 用于监听全局ESC键并关闭ShowMessageView窗口
    /// </summary>
    public class GlobalEscService : IDisposable
    {
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint WH_KEYBOARD_LL = 13;
        private const uint WM_KEYDOWN = 0x0100;
        private const int VK_ESCAPE = 0x1B;
        
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelKeyboardProc _hookProc;
        private bool _isDisposed = false;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public GlobalEscService()
        {
            _hookProc = HookCallback;
        }

        /// <summary>
        /// 注册全局ESC键钩子
        /// </summary>
        public void Register()
        {
            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_hookProc);
            }
        }

        /// <summary>
        /// 注销全局ESC键钩子
        /// </summary>
        public void Unregister()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                
                // 检查是否按下了ESC键
                if (vkCode == VK_ESCAPE)
                {
                    // 查找并关闭ShowMessageView窗口
                    CloseShowMessageWindow();
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 查找并关闭ShowMessageView窗口
        /// </summary>
        private void CloseShowMessageWindow()
        {
            // 在UI线程上执行窗口查找和关闭操作
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 遍历所有打开的窗口
                foreach (Window window in Application.Current.Windows)
                {
                    // 检查是否是ShowMessageView类型的窗口
                    if (window is Rouyan.Pages.View.ShowMessageView showMessageWindow)
                    {
                        showMessageWindow.Close();
                        break; // 找到并关闭后退出循环
                    }
                }
            });
        }

        #region Win32 API 导入

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(uint idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        #region IDisposable 实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                }

                // 释放非托管资源
                Unregister();
                _isDisposed = true;
            }
        }

        ~GlobalEscService()
        {
            Dispose(false);
        }

        #endregion
    }
}
