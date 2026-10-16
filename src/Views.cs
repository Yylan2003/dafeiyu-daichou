using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SmokeSimulator
{
    internal class CloseChoiceDialog : Form
    {
        public CloseChoiceDialog()
        {
            Text = "关闭抽烟模拟器"; ClientSize = new Size(448, 172);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent; BackColor = Theme.Background;
            Controls.Add(Theme.Label("要关闭软件，还是继续在后台运行？", 22, 20, 408, 30, 12, Theme.Ink));
            Controls.Add(Theme.Label("选择最小化后，软件会进入 Windows 右下角通知区域。", 22, 60, 408, 34, 9, Theme.Muted));
            Button exit = Theme.Button("关闭软件", 22, 112, 188, Theme.Green); exit.DialogResult = DialogResult.Yes;
            Button tray = Theme.Button("最小化到任务栏", 230, 112, 194, Theme.Green); tray.DialogResult = DialogResult.No;
            Controls.Add(exit); Controls.Add(tray);
        }
    }
    internal class NoticePopup : Form
    {
        public NoticePopup(string message)
        {
            Text = "提示"; ClientSize = new Size(352, 150); BackColor = Theme.Background;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            ShowInTaskbar = false; StartPosition = FormStartPosition.CenterParent;
            Controls.Add(Theme.Label(message, 20, 21, 312, 62, 11, Theme.Ink));
            Button okay = Theme.Button("知道了", 110, 94, 132, Theme.Green);
            okay.Click += delegate { Close(); }; Controls.Add(okay); AcceptButton = okay; CancelButton = okay;
        }
    }
    internal class BagSlot : Control
    {
        public Product Item;
        public string Description;
        public bool Favorite, Selected, Equipped;
        public event Action FavoriteClick;
        public BagSlot()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            Cursor = Cursors.Hand; TabStop = true;
        }
        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (Item != null && !Item.Rice && e.X * 114f / Width >= 88 && e.Y * 101f / Height <= 25 && FavoriteClick != null) FavoriteClick();
            base.OnMouseClick(e);
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter) { OnClick(EventArgs.Empty); e.Handled = true; }
            base.OnKeyDown(e);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(Width / 114f, Height / 101f);
            g.Clear(Selected ? Color.FromArgb(222, 234, 217) : Color.FromArgb(250, 249, 245));
            using (Pen pen = new Pen(Selected ? Theme.Green : Color.FromArgb(212, 216, 203), Selected ? 2 : 1)) g.DrawRectangle(pen, 1, 1, 111, 98);
            if (Item == null)
            {
                using (Font f = Theme.Font(19, FontStyle.Regular)) using (Brush b = new SolidBrush(Color.FromArgb(213, 216, 204))) g.DrawString("+", f, b, 43, 29);
                return;
            }
            Art.Item(g, Item, new Rectangle(8, 7, 29, 32));
            using (Font f = Theme.Font(8, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Ink))
            using (StringFormat format = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                string name = Item.Name.Replace("（示例）", "");
                g.DrawString(name, f, b, new RectangleF(5, 41, 104, 18), format);
            }
            using (Font f = Theme.Font(8, FontStyle.Regular)) using (Brush b = new SolidBrush(Theme.Muted))
                g.DrawString(Description, f, b, new RectangleF(5, 61, 106, 38));
            using (Font f = Theme.Font(9, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Green))
            {
                if (Item.Rice) g.DrawString("置顶", f, b, 70, 9);
                else g.DrawString(Favorite ? "★" : "☆", f, b, 90, 7);
                if (Equipped) g.DrawString("装备", f, b, 40, 10);
            }
            if (Focused) ControlPaint.DrawFocusRectangle(g, new Rectangle(4, 4, 106, 93));
        }
    }
    internal class ProductPanel : Panel
    {
        public Product Item;
        public ProductPanel() { DoubleBuffered = true; }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if (Item == null) return;
            e.Graphics.ScaleTransform(Width / 146f, Height / 145f);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Art.Item(e.Graphics, Item, new Rectangle(24, 7, 100, 80));
            using (Font f = Theme.Font(9, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Ink))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
                e.Graphics.DrawString(Item.Name, f, b, new RectangleF(4, 90, 138, 22), format);
            using (Font f = Theme.Font(12, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Green))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center })
                e.Graphics.DrawString("¥ " + Item.Price + (Item.Rice ? " / 碗" : Item.Vape ? " / 支" : " / 包"), f, b, new RectangleF(4, 117, 138, 27), format);
        }
    }
    internal class PreviewPanel : Panel
    {
        private readonly Engine engine;
        public string Selection = "";
        private float animation;
        private readonly SmokeTrail smoke = new SmokeTrail();
        private readonly PoseTransition poseTransition = new PoseTransition();
        public PreviewPanel(Engine engine) { this.engine = engine; DoubleBuffered = true; engine.Puffed += Emit; }
        private void Emit() { if (Selection == "") smoke.Emit(Catalog.Get(engine.Data.Active == null ? engine.Data.Selected : engine.Data.Active.ProductId), engine.Pose); }
        public void Frame(double elapsed)
        {
            poseTransition.Frame(elapsed);
            animation += (float)elapsed;
            if (!engine.Smoking || Selection != "") smoke.Clear();
            smoke.Frame(elapsed * engine.Speed);
            if (Visible) Invalidate();
        }
        protected override void Dispose(bool disposing) { if (disposing) { engine.Puffed -= Emit; poseTransition.Dispose(); } base.Dispose(disposing); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.ScaleTransform(Width / 480f, Height / 350f);
            using (Brush b = new SolidBrush(Color.FromArgb(239, 243, 230))) g.FillEllipse(b, 105, 43, 270, 270);
            using (Pen p = new Pen(Color.FromArgb(201, 212, 193))) g.DrawEllipse(p, 91, 29, 298, 298);
            string pose = engine.Pose;
            if (!engine.PetVisible && pose == "read") pose = "sleep";
            Product selection = Catalog.Get(Selection);
            Product item = engine.VisualProduct;
            if (!engine.Data.Collapsed && engine.DrunkSeconds == 0 && engine.ReviveSeconds == 0 && engine.ClosingSeconds == 0 && selection != null && !selection.Rice) { item = selection; pose = "equip"; }
            float remaining = engine.Data.Active == null || item == null || item.Vape ? 1 : (float)(1 - engine.Data.Active.Burned / item.Duration);
            GraphicsState state = g.Save(); g.TranslateTransform(106, 63);
            if (pose == "walk") g.TranslateTransform((float)Math.Sin(animation * 1.5) * 25, 0);
            if (pose == "smoke" || pose == "cough" || pose == "drunk") smoke.Draw(g);
            poseTransition.Draw(g, pose, delegate(Graphics layer)
            {
                Art.Person(layer, engine.Data.Avatar, item, pose, remaining, pose == "count" ? (float)engine.CountSeconds : animation, 0, 0, engine.CoughAnimation, (float)(20 - engine.Data.FatigueSeconds), true, selection == null && engine.HeartEyes, selection == null && engine.LightBulb, selection == null && engine.Exhaling, (float)engine.BreathPhase, (float)(engine.Data.Active == null ? 0 : engine.Data.Active.Age), (float)engine.FinishSeconds);
            });
            if (pose == "smoke" || pose == "cough" || pose == "drunk") smoke.DrawMouth(g);
            g.Restore(state);
            if (selection != null && selection.Rice) Art.Rice(g, new Rectangle(376, 232, 78, 58));
            using (Font f = Theme.Font(12, FontStyle.Bold)) using (Brush b = new SolidBrush(engine.Data.Collapsed ? Color.FromArgb(163, 78, 54) : Theme.Green))
            {
                string title = engine.Data.Collapsed ? "自动复活  " + FormatRemaining(engine.RecoveryRemaining) : "大肥鱼";
                g.DrawString(title, f, b, 18, 14);
            }
            if (!engine.Data.Collapsed)
                using (Font f = Theme.Font(9, FontStyle.Regular)) using (Brush b = new SolidBrush(Theme.Muted))
                    g.DrawString("连续未抽烟 " + FormatRemaining(engine.Data.ContinuousWorkSeconds) + " · 收益 " + (engine.IncomeRate * 100).ToString("0") + "%", f, b, 18, 40);
            string caption = engine.Data.Collapsed ? "喂一碗大米饭，可以提前复活" : selection != null ? "预览：" + selection.Name + " · 装备后应用" : pose == "read" ? "工作中 · 双击桌面小人开始抽烟" : pose == "think" ? "没抽的赶紧借" : pose == "crave" ? "吸溜吸溜…" : pose == "walk" ? "不看了，出去走走" : pose == "sleep" ? "把头埋进书里，睡一会儿" : pose == "smash" ? "正在砸屏幕玻璃" : "手持：" + (item == null ? "无" : item.Name);
            using (Font f = Theme.Font(9, FontStyle.Regular)) using (Brush b = new SolidBrush(Theme.Muted))
                g.DrawString(caption, f, b, 18, 326);
        }
        public static string FormatRemaining(double seconds)
        {
            int s = (int)Math.Ceiling(seconds); return String.Format("{0:00}:{1:00}:{2:00}", s / 3600, s / 60 % 60, s % 60);
        }
    }
}
