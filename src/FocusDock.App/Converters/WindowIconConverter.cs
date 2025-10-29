using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FocusDock.SystemInterop;

namespace FocusDock.App.Converters
{
    public class WindowIconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not WindowInfo w) return null;
            try
            {
                var hIcon = GetIconHandleForWindow((IntPtr)w.Hwnd);
                if (hIcon == IntPtr.Zero) return null;
                var src = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    System.Windows.Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                src.Freeze();
                return src;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

        private static IntPtr GetIconHandleForWindow(IntPtr hwnd)
        {
            // Try WM_GETICON with big/small icons
            IntPtr hIcon = User32.SendMessage(hwnd, (uint)User32.WM_GETICON, (IntPtr)User32.ICON_BIG, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = User32.SendMessage(hwnd, (uint)User32.WM_GETICON, (IntPtr)User32.ICON_SMALL, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
                hIcon = User32.SendMessage(hwnd, (uint)User32.WM_GETICON, (IntPtr)User32.ICON_SMALL2, IntPtr.Zero);

            // Fallback to class icon
            if (hIcon == IntPtr.Zero)
            {
                if (IntPtr.Size == 8)
                {
                    hIcon = User32.GetClassLongPtr64(hwnd, User32.GCL_HICON);
                    if (hIcon == IntPtr.Zero)
                        hIcon = User32.GetClassLongPtr64(hwnd, User32.GCL_HICONSM);
                }
                else
                {
                    var handle32 = (IntPtr)unchecked((int)User32.GetClassLong32(hwnd, User32.GCL_HICON));
                    if (handle32 != IntPtr.Zero)
                        hIcon = handle32;
                    if (hIcon == IntPtr.Zero)
                        hIcon = (IntPtr)unchecked((int)User32.GetClassLong32(hwnd, User32.GCL_HICONSM));
                }
            }

            return hIcon;
        }
    }
}
