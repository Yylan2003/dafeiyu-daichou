using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SmokeSimulator
{
    internal class DesktopForm : Form
    {
        protected static readonly Color ClearColor = Color.FromArgb(255, 0, 255);
        protected static readonly Random Random = new Random();
        public DesktopForm()
        {
            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; TopMost = true;
            BackColor = ClearColor; TransparencyKey = ClearColor; DoubleBuffered = true;
            StartPosition = FormStartPosition.Manual; AutoScaleMode = AutoScaleMode.None;
        }
        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get { CreateParams p = base.CreateParams; p.ExStyle |= 0x08000080; return p; }
        }
        public static Point Clamp(Point point, Size size)
        {
            Rectangle area = Screen.FromPoint(point).WorkingArea;
            return new Point(Math.Max(area.Left, Math.Min(point.X, area.Right - size.Width)), Math.Max(area.Top, Math.Min(point.Y, area.Bottom - size.Height)));
        }
        public static Point RandomPoint(Size size)
        {
            // Pick an actual monitor, never a gap in the virtual desktop, including negative coordinates.
            Screen screen = Screen.AllScreens[Random.Next(Screen.AllScreens.Length)]; Rectangle area = screen.WorkingArea;
            return new Point(area.Left + Random.Next(Math.Max(1, area.Width - size.Width + 1)), area.Top + Random.Next(Math.Max(1, area.Height - size.Height + 1)));
        }
    }

    internal class PetForm : DesktopForm
    {
        private readonly Engine engine;
        private readonly Action changed;
        private readonly SmokeTrail smoke = new SmokeTrail();
        private readonly PoseTransition poseTransition = new PoseTransition();
        private Point mouseStart, formStart, lastClickPosition;
        private bool pressed, dragged;
        private long lastClickTicks;
        private float animation, throwTime, puffTime;
        private Product thrownProduct;
        private readonly ToolTip tip = new ToolTip();
        private readonly VerticalShake shake = new VerticalShake();
        private readonly Stopwatch gestureClock = Stopwatch.StartNew();
        private SceneEffectForm groundBook, cracks;
        private Point walkStart;
        private bool walking;
        private double walkOffset;
        private float desktopScale = 1;
        public PetForm(Engine engine, Action settings, Action exit, Action work, Action end, Action changed)
        {
            this.engine = engine; this.changed = changed; desktopScale = engine.Data.PetScalePercent / 100f;
            ClientSize = new Size((int)Math.Round(268 * desktopScale), (int)Math.Round(232 * desktopScale));
            TransparencyKey = Color.Empty; BackColor = Color.Black;
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = engine.Data.PetX == -1 && engine.Data.PetY == -1 ? new Point(area.Right - 300, area.Bottom - 290) : Clamp(new Point(engine.Data.PetX, engine.Data.PetY), Size);
            engine.Data.PetX = Left; engine.Data.PetY = Top;
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("打开背包 / 商店", null, delegate { settings(); });
            menu.Items.Add("开始 / 暂停工作", null, delegate { work(); });
            menu.Items.Add("结束工作", null, delegate { end(); });
            menu.Items.Add("退出软件", null, delegate { exit(); }); ContextMenuStrip = menu; Cursor = Cursors.Hand;
            MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left) return;
                pressed = true; dragged = false; mouseStart = Cursor.Position; formStart = Location; Capture = true;
                shake.Begin(mouseStart.X, mouseStart.Y, gestureClock.Elapsed.TotalSeconds);
            };
            MouseMove += delegate
            {
                if (!pressed) return;
                Point now = Cursor.Position; int dx = now.X - mouseStart.X, dy = now.Y - mouseStart.Y;
                if (Math.Abs(dx) + Math.Abs(dy) > 6) dragged = true;
                if (dragged) Location = Clamp(new Point(formStart.X + dx, formStart.Y + dy), Size);
                if (engine.Smoking && shake.Move(now.X, now.Y, gestureClock.Elapsed.TotalSeconds) && engine.DiscardSmoking())
                {
                    engine.Data.PetX = Left; engine.Data.PetY = Top;
                    tip.Show(engine.Notice, this, 5, 5, 2200); changed();
                }
            };
            MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left || !pressed) return;
                pressed = false; Capture = false; shake.End();
                if (!dragged) HandleClick(Cursor.Position, DateTime.UtcNow.Ticks);
                else { lastClickTicks = 0; if (walking) { walkStart = Location; walkOffset = 0; } }
                engine.Data.PetX = Left; engine.Data.PetY = Top; changed(); Invalidate();
            };
            MouseCaptureChanged += delegate { if (!Capture) { pressed = false; shake.End(); } };
            LocationChanged += delegate { AlignCracks(); };
            ApplyScale();
        }
        private void AlignCracks()
        {
            if (cracks == null || cracks.IsDisposed) return;
            // Keep the impact fixed to the fist even at monitor edges: clip the effect, never clamp it separately.
            cracks.Location = new Point(Left + (int)Math.Round(AssetArt.SmashImpact.X * desktopScale) - cracks.Width / 2,
                Top + (int)Math.Round(AssetArt.SmashImpact.Y * desktopScale) - cracks.Height / 2);
        }
        public void ApplyScale()
        {
            float scale = engine.Data.PetScalePercent / 100f;
            if (Math.Abs(scale - desktopScale) < .0001f) return;
            Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
            int center = Left + Width / 2, bottom = Bottom;
            Point oldLocation = Location;
            desktopScale = scale;
            ClientSize = new Size((int)Math.Round(268 * scale), (int)Math.Round(232 * scale));
            // Anchor the feet while resizing, and keep the pet on its current monitor.
            Location = new Point(Math.Max(area.Left, Math.Min(center - Width / 2, area.Right - Width)),
                Math.Max(area.Top, Math.Min(bottom - Height, area.Bottom - Height)));
            if (walking) walkStart.Offset(Left - oldLocation.X, Top - oldLocation.Y);
            if (groundBook != null) groundBook.SetScale(scale);
            if (cracks != null) { cracks.SetScale(scale); AlignCracks(); }
            engine.Data.PetX = Left; engine.Data.PetY = Top;
            if (Visible) Frame(0);
        }
        private void HandleClick(Point position, long ticks)
        {
            if (engine.Smoking) { engine.ClickPet(); lastClickTicks = 0; return; }
            bool doubleClick = lastClickTicks > 0 && ticks - lastClickTicks <= SystemInformation.DoubleClickTime * TimeSpan.TicksPerMillisecond &&
                Math.Abs(position.X - lastClickPosition.X) <= SystemInformation.DoubleClickSize.Width && Math.Abs(position.Y - lastClickPosition.Y) <= SystemInformation.DoubleClickSize.Height;
            if (doubleClick)
            {
                lastClickTicks = 0;
                if (!engine.StartSmoking()) tip.Show(engine.Notice, this, 5, 5, 2200);
            }
            else { lastClickTicks = ticks; lastClickPosition = position; }
        }
        internal void TestSingleClick() { lastClickTicks = 0; HandleClick(new Point(100, 100), DateTime.UtcNow.Ticks); }
        internal void TestDoubleClick()
        {
            lastClickTicks = 0; long now = DateTime.UtcNow.Ticks;
            HandleClick(new Point(100, 100), now); HandleClick(new Point(100, 100), now + TimeSpan.TicksPerMillisecond * 80);
        }
        internal void TestShake()
        {
            shake.Begin(100, 100, 0);
            for (int i = 0; i < 6; i++)
                if (shake.Move(100, i % 2 == 0 ? 64 : 100, 0.1 * (i + 1))) engine.DiscardSmoking();
            changed();
        }
        public bool HitBody(Point point)
        {
            Point local = PointToClient(point);
            RectangleF body = engine.Data.Collapsed ? new RectangleF(22, 0, 224, 224) : new RectangleF(55, 15, 160, 212);
            return body.Contains(local.X / desktopScale, local.Y / desktopScale);
        }
        public void Emit()
        {
            puffTime = 0.65f;
            Product product = Catalog.Get(engine.Data.Active == null ? engine.Data.Selected : engine.Data.Active.ProductId);
            smoke.Emit(product, engine.Pose);
        }
        public void Throw(string productId) { thrownProduct = Catalog.Get(productId); throwTime = 0.8f; }
        public void Frame(double dt)
        {
            poseTransition.Frame(dt);
            string pose = engine.Pose;
            if (Visible && pose == "walk")
            {
                if (!walking)
                {
                    walking = true; walkStart = Location; walkOffset = 0;
                    groundBook = new SceneEffectForm(false); groundBook.SetScale(desktopScale); groundBook.Show();
                }
                float bookTravel = Math.Min(1, (float)(20 - engine.Data.FatigueSeconds));
                groundBook.Location = Clamp(new Point(walkStart.X + (int)((90 - bookTravel * 72) * desktopScale), walkStart.Y + (int)((145 + bookTravel * 62) * desktopScale)), groundBook.Size);
                walkOffset += dt;
                if (!pressed) Location = Clamp(new Point(walkStart.X + (int)(Math.Sin(walkOffset * 0.55) * 115 * desktopScale), walkStart.Y), Size);
            }
            else if (walking)
            {
                walking = false; Location = Clamp(walkStart, Size);
                if (groundBook != null) { groundBook.Close(); groundBook.Dispose(); groundBook = null; }
            }
            if (Visible && pose == "smash")
            {
                if (cracks == null) { cracks = new SceneEffectForm(true); cracks.SetScale(desktopScale); AlignCracks(); cracks.Show(this); }
                AlignCracks();
                cracks.Progress = (float)(1 - engine.Data.FatigueSeconds / 20); cracks.RefreshEffect();
            }
            else if (cracks != null) { cracks.Close(); cracks.Dispose(); cracks = null; }
            animation += (float)dt; throwTime = Math.Max(0, throwTime - (float)dt); puffTime = Math.Max(0, puffTime - (float)dt);
            if (!engine.Smoking) smoke.Clear();
            smoke.Frame(dt * engine.Speed);
            if (Visible) PresentFrame();
        }
        protected override CreateParams CreateParams { get { CreateParams p = base.CreateParams; p.ExStyle |= 0x80000; return p; } }
        protected override void OnShown(EventArgs e) { base.OnShown(e); PresentFrame(); }
        private void PresentFrame()
        {
            using (Bitmap image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb))
            {
                using (Graphics g = Graphics.FromImage(image)) { g.Clear(Color.Transparent); PaintPet(g); }
                AlphaWindow.Present(this, image);
            }
        }
        internal void CaptureAppearance(string path)
        {
            using (Bitmap image = new Bitmap(Width, Height))
            { using (Graphics g = Graphics.FromImage(image)) { g.Clear(Color.Transparent); PaintPet(g); } image.Save(path); }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); PaintPet(e.Graphics);
        }
        private void PaintPet(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(desktopScale, desktopScale);
            Session s = engine.Data.Active; Product p = engine.VisualProduct;
            float remaining = s == null || p == null || p.Vape ? 1 : (float)(1 - s.Burned / p.Duration);
            smoke.Draw(g);
            string pose = engine.Pose;
            poseTransition.Draw(g, pose + (throwTime > 0 ? ":throw" : ""), delegate(Graphics layer)
            {
                Art.Person(layer, engine.Data.Avatar, throwTime > 0 ? thrownProduct : p, pose, remaining, pose == "count" ? (float)engine.CountSeconds : animation, throwTime, puffTime, engine.CoughAnimation, (float)(20 - engine.Data.FatigueSeconds), false, engine.HeartEyes, engine.LightBulb, engine.Exhaling, (float)engine.BreathPhase, (float)(s == null ? 0 : s.Age), (float)engine.FinishSeconds);
            }, desktopScale);
            smoke.DrawMouth(g);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { tip.Dispose(); poseTransition.Dispose(); if (groundBook != null) groundBook.Dispose(); if (cracks != null) cracks.Dispose(); }
            base.Dispose(disposing);
        }
    }
    internal class SceneEffectForm : DesktopForm
    {
        private readonly bool isCrack;
        private float displayScale = 1;
        public float Progress;
        public SceneEffectForm(bool crack)
        {
            isCrack = crack; ClientSize = crack ? new Size(360, 320) : new Size(70, 35);
            TransparencyKey = Color.Empty; BackColor = Color.Black;
        }
        public void SetScale(float scale)
        {
            displayScale = scale;
            ClientSize = new Size((int)Math.Round((isCrack ? 360 : 70) * scale), (int)Math.Round((isCrack ? 320 : 35) * scale));
            RefreshEffect();
        }
        protected override CreateParams CreateParams
        {
            get { CreateParams p = base.CreateParams; p.ExStyle |= 0x20 | 0x80000; return p; }
        }
        protected override void OnShown(EventArgs e) { base.OnShown(e); RefreshEffect(); }
        internal Bitmap RenderEffect()
        {
            Bitmap image = new Bitmap(Width, Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(image)) { g.Clear(Color.Transparent); DrawEffect(g); }
            return image;
        }
        internal void RefreshEffect() { if (Visible) using (Bitmap image = RenderEffect()) AlphaWindow.Present(this, image); }
        private void DrawEffect(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(displayScale, displayScale);
            if (isCrack) Art.Crack(g, new PointF(180, 160), 150, Progress);
            else Art.Book(g, new Rectangle(3, 4, 62, 27));
        }
        protected override void OnPaint(PaintEventArgs e) { DrawEffect(e.Graphics); }
    }
    internal class DebrisForm : DesktopForm
    {
        private readonly Engine engine; private readonly Debris debris; private readonly Action changed;
        private readonly Timer progressTimer = new Timer { Interval = 5000 };
        public DebrisForm(Engine engine, Debris debris, Action changed)
        {
            this.engine = engine; this.debris = debris; this.changed = changed;
            ClientSize = new Size(72, 42); Location = Clamp(new Point(debris.X, debris.Y), Size); debris.X = Left; debris.Y = Top; Cursor = Cursors.Hand;
            // Count each release, including the second click of a Windows double-click.
            MouseUp += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left) PerformClean(); };
            progressTimer.Tick += delegate { progressTimer.Stop(); Invalidate(); };
        }
        public void PerformClean()
        {
            bool removed = engine.Clean(debris.Id);
            // Restart only the visibility timeout; keep the accumulated cleaning clicks.
            progressTimer.Stop();
            if (!removed) progressTimer.Start();
            changed(); if (removed) Close(); else Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(13, 10); g.RotateTransform(-10);
            bool photographed = false;
            if (debris.Vape && Catalog.Get(debris.ProductId) != null)
            {
                GraphicsState item = g.Save(); g.TranslateTransform(1, 9); g.RotateTransform(-80);
                photographed = AssetArt.Item(g, Catalog.Get(debris.ProductId), new RectangleF(-5, 0, 15, 42)); g.Restore(item);
            }
            if (!photographed) using (Brush b = new SolidBrush(debris.Vape ? Color.FromArgb(105, 124, 161) : Color.FromArgb(206, 153, 87))) g.FillRectangle(b, 0, 0, debris.Vape ? 40 : 24, 9);
            if (!debris.Vape) { g.FillRectangle(Brushes.Ivory, 24, 0, 13, 9); g.FillRectangle(Brushes.DimGray, 37, 0, 4, 9); }
            g.ResetTransform();
            if (progressTimer.Enabled)
            {
                using (Brush track = new SolidBrush(Color.FromArgb(215, 222, 217)))
                using (Brush fill = new SolidBrush(Theme.Green))
                {
                    g.FillRectangle(track, 14, 28, 44, 4);
                    g.FillRectangle(fill, 14, 28, 44 * Math.Min(5, debris.Clicks) / 5f, 4);
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) progressTimer.Dispose();
            base.Dispose(disposing);
        }
    }
    internal class RiceForm : DesktopForm
    {
        private readonly Engine engine;
        private readonly Func<Point, bool> dropped;
        private readonly Action changed;
        private bool dragging;
        private Point grabOffset;
        private double vx, vy, redirect, x, y;
        public RiceForm(Engine engine, Func<Point, bool> dropped, Action changed)
        {
            this.engine = engine; this.dropped = dropped; this.changed = changed;
            ClientSize = new Size(106, 61); Location = RandomPoint(Size); x = Left; y = Top;
            Cursor = Cursors.Hand; ChooseVelocity();
            MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button != MouseButtons.Left || !Grab()) return;
                dragging = true; grabOffset = e.Location; Capture = true;
            };
            MouseMove += delegate
            {
                if (!dragging) return;
                Location = Clamp(new Point(Cursor.Position.X - grabOffset.X, Cursor.Position.Y - grabOffset.Y), Size); x = Left; y = Top;
            };
            MouseUp += delegate(object sender, MouseEventArgs e) { if (e.Button == MouseButtons.Left && dragging) Release(Cursor.Position); };
            MouseCaptureChanged += delegate { if (!Capture) dragging = false; };
        }
        private void ChooseVelocity()
        {
            double angle = Random.NextDouble() * Math.PI * 2; vx = Math.Cos(angle) * 210; vy = Math.Sin(angle) * 210;
        }
        private void Release(Point point)
        {
            dragging = false; Capture = false;
            if (dropped(point)) Hide(); else { ChooseVelocity(); x = Left; y = Top; }
        }
        private bool Grab()
        {
            if (!engine.GrabRice()) return false;
            changed(); return true;
        }
        internal void TestGrab() { dragging = Grab(); }
        internal void TestRelease(Point point) { Release(point); }
        public void Frame(double dt)
        {
            Invalidate();
            if (dragging || dt <= 0) return;
            redirect += dt;
            if (redirect >= 3.5)
            {
                redirect = 0; ChooseVelocity();
                // Occasionally hop monitors; continuous movement otherwise stays on a real screen.
                if (Screen.AllScreens.Length > 1) { Location = RandomPoint(Size); x = Left; y = Top; }
            }
            Rectangle area = Screen.FromControl(this).WorkingArea;
            x += vx * dt; y += vy * dt;
            if (x < area.Left || x > area.Right - Width) { vx = -vx; x = Math.Max(area.Left, Math.Min(x, area.Right - Width)); }
            if (y < area.Top || y > area.Bottom - Height) { vy = -vy; y = Math.Max(area.Top, Math.Min(y, area.Bottom - Height)); }
            Location = new Point((int)x, (int)y); Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Art.Rice(e.Graphics, new Rectangle(13, 3, 80, 55));
        }
    }
}
