using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

namespace CVR.CCKEditor.Util
{
    public static class TaskbarBlinker
    {
        #region DLL Imports
        
        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref Flashwinfo pwfi);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        
        #endregion DLL Imports
        
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct Flashwinfo
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }
        
        private const uint FlashwStop = 0;
        private const uint FlashwCaption = 1;
        private const uint FlashwTray = 2;
        private const uint FlashwAll = FlashwCaption | FlashwTray;
        private const uint FlashwTimernofg = 12;
        
        private static IntPtr _cachedWindowHandle = IntPtr.Zero;
        
        #region Public Methods

        /// <summary>
        /// Starts blinking the Unity Editor taskbar icon for the specified number of times.
        /// </summary>
        /// <param name="blinkCount">Number of times to blink the taskbar icon.</param>
        public static void StartBlink(int blinkCount = 5)
        {
            IntPtr hwnd = GetCachedOrCurrentProcessWindowHandle();
            
            if (hwnd == IntPtr.Zero)
            {
                Debug.LogWarning("Unity Editor window handle not found.");
                return;
            }

            Flashwinfo fw = new()
            {
                cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(Flashwinfo))),
                hwnd = hwnd,
                dwFlags = FlashwAll | FlashwTimernofg,
                uCount = (uint)blinkCount,
                dwTimeout = 0
            };

            FlashWindowEx(ref fw);
        }

        /// <summary>
        /// Stops any ongoing blinking of the Unity Editor taskbar icon.
        /// </summary>
        public static void StopBlink()
        {
            IntPtr hwnd = GetCachedOrCurrentProcessWindowHandle();

            if (hwnd == IntPtr.Zero)
            {
                Debug.LogWarning("Unity Editor window handle not found.");
                return;
            }

            Flashwinfo fw = new()
            {
                cbSize = Convert.ToUInt32(Marshal.SizeOf(typeof(Flashwinfo))),
                hwnd = hwnd,
                dwFlags = FlashwStop,
                uCount = 0,
                dwTimeout = 0
            };

            FlashWindowEx(ref fw);
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the cached window handle of the Unity Editor, or finds it if not cached.
        /// </summary>
        /// <returns>Handle to the Unity Editor window.</returns>
        private static IntPtr GetCachedOrCurrentProcessWindowHandle()
        {
            if (_cachedWindowHandle == IntPtr.Zero)
                _cachedWindowHandle = FindCurrentProcessMainWindowHandle();
            return _cachedWindowHandle;
        }

        /// <summary>
        /// Finds the main window handle for the current Unity Editor process.
        /// </summary>
        /// <returns>Main window handle of the current process.</returns>
        private static IntPtr FindCurrentProcessMainWindowHandle()
        {
            IntPtr foundWindow = IntPtr.Zero;
            uint currentProcessId = (uint)Process.GetCurrentProcess().Id;

            EnumWindows(delegate (IntPtr hWnd, IntPtr _)
            {
                GetWindowThreadProcessId(hWnd, out uint processId);

                // Check if the window belongs to the current process
                if (processId != currentProcessId) 
                    return true; // Continue
                
                foundWindow = hWnd;
                return false; // Stop, we found

            }, IntPtr.Zero);

            return foundWindow;
        }

        #endregion Private Methods
    }
}