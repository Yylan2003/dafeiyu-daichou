using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace SmokeSimulator
{
    // A brief crossfade between poses; continuous hand motion stays within its current pose.
    internal sealed class PoseTransition : IDisposable
    {
        private Bitmap last, from;
        private string key;
        private double elapsed = .25;
        internal void Frame(double seconds) { elapsed = Math.Min(.25, elapsed + Math.Max(0, seconds)); }
        internal void Draw(Graphics target, string nextKey, Action<Graphics> render, float scale = 1)
        {
            int width = (int)Math.Round(268 * scale), height = (int)Math.Round(232 * scale);
            if (last != null && (last.Width != width || last.Height != height)) Reset();
            if (key != nextKey)
            {
                if (from != null) from.Dispose();
                from = last == null ? null : new Bitmap(last);
                elapsed = from == null ? .25 : 0; key = nextKey;
            }
            Bitmap current = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(current))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.ScaleTransform(scale, scale); render(g);
            }
            if (from != null && elapsed < .25)
            {
                Bitmap blended = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(blended))
                {
                    float t = (float)(elapsed / .25); t = t * t * (3 - 2 * t);
                    DrawAlpha(g, from, 1 - t); DrawAlpha(g, current, t);
                }
                current.Dispose(); current = blended;
            }
            else if (from != null) { from.Dispose(); from = null; }
            target.DrawImage(current, new RectangleF(0, 0, 268, 232));
            if (last != null) last.Dispose(); last = current;
        }
        private static void DrawAlpha(Graphics g, Bitmap image, float alpha)
        {
            using (ImageAttributes attributes = new ImageAttributes())
            {
                ColorMatrix matrix = new ColorMatrix(); matrix.Matrix33 = alpha; attributes.SetColorMatrix(matrix);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
        }
        private void Reset()
        {
            if (last != null) last.Dispose(); if (from != null) from.Dispose();
            last = from = null; key = null; elapsed = .25;
        }
        public void Dispose() { Reset(); }
    }
}
