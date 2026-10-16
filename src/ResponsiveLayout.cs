using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SmokeSimulator
{
    // Scale from immutable design bounds so repeated maximize/restore never accumulates rounding.
    internal sealed class ResponsiveLayout : IDisposable
    {
        private sealed class Item
        {
            public Control Control;
            public Rectangle Bounds;
            public Font Original, Assigned;
        }
        private readonly Panel viewport, canvas;
        private readonly List<Item> items = new List<Item>();
        private bool updating;
        private float previousScale;
        internal float Scale { get; private set; }
        internal ResponsiveLayout(Panel viewport, Panel canvas)
        {
            this.viewport = viewport; this.canvas = canvas;
            Capture(canvas); viewport.SizeChanged += Resized; Update();
        }
        private void Capture(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                items.Add(new Item { Control = child, Bounds = child.Bounds, Original = (Font)child.Font.Clone() });
                // Native input controls manage their own internal children.
                if (child is Panel) Capture(child);
            }
        }
        private void Resized(object sender, EventArgs e) { Update(); }
        internal void Update()
        {
            if (updating || viewport.ClientSize.Width <= 0 || viewport.ClientSize.Height <= 0) return;
            updating = true; viewport.SuspendLayout(); canvas.SuspendLayout();
            try
            {
                Scale = Math.Max(1, Math.Min(viewport.ClientSize.Width / 1040f, viewport.ClientSize.Height / 824f));
                Size size = new Size((int)Math.Round(1040 * Scale), (int)Math.Round(824 * Scale));
                viewport.AutoScrollMinSize = size;
                if (Math.Abs(Scale - previousScale) > .0001f)
                {
                    foreach (Item item in items)
                    {
                        Rectangle r = item.Bounds;
                        item.Control.SetBounds((int)Math.Round(r.X * Scale), (int)Math.Round(r.Y * Scale), (int)Math.Round(r.Width * Scale), (int)Math.Round(r.Height * Scale));
                        Font old = item.Assigned;
                        item.Assigned = new Font(item.Original.FontFamily, item.Original.SizeInPoints * Scale, item.Original.Style);
                        item.Control.Font = item.Assigned;
                        if (old != null) old.Dispose();
                        item.Control.Invalidate();
                    }
                    previousScale = Scale;
                }
                Point scroll = viewport.AutoScrollPosition;
                canvas.Bounds = new Rectangle(Math.Max(0, (viewport.ClientSize.Width - size.Width) / 2) + scroll.X,
                    Math.Max(0, (viewport.ClientSize.Height - size.Height) / 2) + scroll.Y, size.Width, size.Height);
            }
            finally { canvas.ResumeLayout(false); viewport.ResumeLayout(false); updating = false; }
        }
        public void Dispose()
        {
            viewport.SizeChanged -= Resized;
            foreach (Item item in items) { item.Original.Dispose(); if (item.Assigned != null) item.Assigned.Dispose(); }
        }
    }
}
