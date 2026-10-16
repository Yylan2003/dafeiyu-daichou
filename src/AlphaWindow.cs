using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SmokeSimulator
{
    // Per-pixel alpha keeps translucent smoke its real color on any desktop wallpaper.
    internal static class AlphaWindow
    {
        [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; public NativePoint(int x, int y) { X = x; Y = y; } }
        [StructLayout(LayoutKind.Sequential)] private struct NativeSize { public int Width, Height; public NativeSize(int w, int h) { Width = w; Height = h; } }
        [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { public byte Operation, Flags, Alpha, Format; }
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr window, IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
        [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr value);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr targetDC, ref NativePoint target, ref NativeSize size, IntPtr sourceDC, ref NativePoint source, int key, ref Blend blend, int flags);
        internal static void Present(Form window, Bitmap image)
        {
            IntPtr screen = GetDC(IntPtr.Zero), memory = IntPtr.Zero, bitmap = IntPtr.Zero, previous = IntPtr.Zero;
            try
            {
                memory = CreateCompatibleDC(screen); bitmap = image.GetHbitmap(Color.FromArgb(0)); previous = SelectObject(memory, bitmap);
                NativePoint target = new NativePoint(window.Left, window.Top), source = new NativePoint(0, 0);
                NativeSize size = new NativeSize(image.Width, image.Height);
                Blend blend = new Blend { Alpha = 255, Format = 1 };
                if (!UpdateLayeredWindow(window.Handle, screen, ref target, ref size, memory, ref source, 0, ref blend, 2))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (previous != IntPtr.Zero) SelectObject(memory, previous);
                if (bitmap != IntPtr.Zero) DeleteObject(bitmap);
                if (memory != IntPtr.Zero) DeleteDC(memory);
                if (screen != IntPtr.Zero) ReleaseDC(IntPtr.Zero, screen);
            }
        }
    }
}
