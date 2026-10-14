using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace HanoiTowers.Helpers
{
    public static class WindowDrag
    {
        [DllImport("user32.dll")]
        private static extern void SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public static void Drag(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
        }
    }
}