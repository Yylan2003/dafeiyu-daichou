using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SmokeSimulator
{
    internal static class Program
    {
        [DllImport("user32.dll")] private static extern bool SetProcessDPIAware();
        [STAThread]
        private static void Main(string[] args)
        {
            SetProcessDPIAware();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            #if UI_TESTS
            bool smokeTest = args.Contains("--smoke-test");
#else
            bool smokeTest = false;
#endif
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmokeSimulator");
            int index = Array.IndexOf(args, "--data-dir");
            if (index >= 0 && index + 1 < args.Length) directory = Path.GetFullPath(args[index + 1]);
            string path = Path.Combine(directory, "save.xml");
            // A stable path hash isolates test instances while protecting each save file.
            uint hash = 2166136261;
            foreach (char c in path.ToUpperInvariant()) hash = (hash ^ c) * 16777619;
            bool created;
            using (Mutex mutex = new Mutex(true, "Local\\SmokeSimulator-" + hash.ToString("X8"), out created))
            {
                if (!created) { MessageBox.Show("抽烟模拟器已经运行，请在右下角托盘打开配置窗口。", "抽烟模拟器"); return; }
                try
                {
                    string warning;
                    SaveData data = Storage.Load(path, out warning);
                    if (warning != null) MessageBox.Show(warning, "存档提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    using (MainForm form = new MainForm(new Engine(data), path, smokeTest)) Application.Run(form);
                }
                catch (Exception ex)
                {
                    if (smokeTest)
                    {
                        Directory.CreateDirectory(directory);
                        File.WriteAllText(Path.Combine(directory, "smoke-test-error.txt"), ex.ToString());
                        Environment.ExitCode = 1;
                    }
                    else MessageBox.Show("程序遇到问题：\n" + ex.Message, "抽烟模拟器", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }

    internal static class Theme
    {
        public static readonly Color Background = Color.FromArgb(243, 241, 235);
        public static readonly Color Ink = Color.FromArgb(40, 58, 53);
        public static readonly Color Muted = Color.FromArgb(108, 121, 113);
        public static readonly Color Green = Color.FromArgb(57, 105, 85);
        public static readonly Color Accent = Color.FromArgb(222, 151, 87);
        public static Font Font(float size, FontStyle style) { return new Font("Microsoft YaHei UI", size, style); }
        public static Label Label(string text, int x, int y, int w, int h, float size, Color color)
        {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h),
                Font = Font(size, FontStyle.Regular), ForeColor = color, BackColor = Color.Transparent };
        }
        public static Button Button(string text, int x, int y, int w, Color color)
        {
            Button b = new Button { Text = text, Location = new Point(x, y), Size = new Size(w, 40),
                BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                Font = Font(10, FontStyle.Bold), Cursor = Cursors.Hand };
            b.FlatAppearance.BorderSize = 0;
            return b;
        }
    }

    internal static class Art
    {
        private static readonly Color[] Shirts = { Color.FromArgb(81, 125, 101), Color.FromArgb(104, 127, 160), Color.FromArgb(181, 122, 91) };
        public static void Person(Graphics g, int avatar, Product product, string state, float remaining, float time, float throwing, float puff, bool cough, float actionElapsed = 0, bool drawWalkingBook = true, bool hearts = false, bool bulb = false, bool exhaling = false, float breathPhase = -1, float sessionAge = 7, float finishSeconds = 0)
        {
            if (AssetArt.Person(g, product, state, remaining, time, throwing, cough, actionElapsed, hearts, bulb, exhaling, breathPhase, sessionAge, finishSeconds)) return;
            GraphicsState original = g.Save();
            Color shirt = Shirts[Math.Max(0, Math.Min(2, avatar))];
            Color skin = Color.FromArgb(237, 200, 165);
            bool collapsed = state == "collapsed", reading = state == "read" || state == "crave" || state == "sleep";
            if (collapsed) { g.TranslateTransform(26, 235); g.RotateTransform(-80); g.ScaleTransform(0.82f, 0.82f); }
            else if (cough) g.TranslateTransform((float)Math.Sin(time * 20) * 2.4f, 0);
            else g.TranslateTransform(0, (float)Math.Sin(time * 2) * 1.2f);
            using (Brush shadow = new SolidBrush(Color.FromArgb(184, 196, 178))) g.FillEllipse(shadow, 61, 211, 142, 14);
            using (Pen leg = new Pen(Color.FromArgb(69, 79, 74), 19))
            {
                leg.StartCap = LineCap.Round; leg.EndCap = LineCap.Round;
                if (reading)
                {
                    g.DrawLines(leg, new[] { new Point(111, 177), new Point(82, 196), new Point(151, 210) });
                    g.DrawLines(leg, new[] { new Point(149, 177), new Point(179, 196), new Point(109, 210) });
                }
                else
                {
                    float step = state == "walk" ? (float)Math.Sin(time * 9) * 13 : 0;
                    g.DrawLine(leg, 111, 169, 108 + step, 211); g.DrawLine(leg, 149, 169, 154 - step, 211);
                }
            }
            using (Brush shoe = new SolidBrush(Color.FromArgb(49, 61, 55)))
            { g.FillEllipse(shoe, 86, 205, 41, 16); g.FillEllipse(shoe, 142, 205, 41, 16); }
            using (Brush body = new SolidBrush(shirt)) g.FillEllipse(body, 88, 114, 84, 80);
            using (Brush neck = new SolidBrush(skin)) g.FillRectangle(neck, 116, 102, 28, 23);
            GraphicsState head = g.Save();
            if (state == "crave") { g.TranslateTransform(130, 100); g.RotateTransform(-24); g.TranslateTransform(-130, -100); }
            if (state == "sleep") { g.TranslateTransform(12, 49); g.TranslateTransform(130, 90); g.RotateTransform(12); g.TranslateTransform(-130, -90); }
            using (Brush face = new SolidBrush(skin)) g.FillEllipse(face, 87, 42, 84, 80);
            using (Brush hair = new SolidBrush(Color.FromArgb(63, 62, 50)))
            {
                g.FillPie(hair, 84, 29, 89, 71, 180, 180);
                g.FillEllipse(hair, 85, 48, 18, 37);
                g.FillEllipse(hair, 107, 30, 44, 27);
            }
            using (Pen line = new Pen(Theme.Ink, 2.2f))
            {
                if (collapsed)
                {
                    g.DrawLine(line, 111, 78, 121, 88); g.DrawLine(line, 121, 78, 111, 88);
                    g.DrawLine(line, 142, 78, 152, 88); g.DrawLine(line, 152, 78, 142, 88);
                    for (int i = 0; i < 5; i++) g.FillEllipse(Brushes.White, 139 + i * 5, 104 + i % 2 * 6, 11, 11);
                }
                else
                {
                    if (hearts) { Heart(g, 107, 73); Heart(g, 139, 73); }
                    else { g.DrawArc(line, 110, 78, 10, 7, 180, 180); g.DrawArc(line, 141, 78, 10, 7, 180, 180); }
                    if (puff > 0 || cough) g.DrawEllipse(line, 142, 98, 10, 8);
                    else g.DrawArc(line, 126, 94, 21, 12, 5, 140);
                    if (avatar == 1) { g.DrawRectangle(line, 103, 73, 23, 17); g.DrawRectangle(line, 135, 73, 23, 17); g.DrawLine(line, 126, 80, 135, 80); }
                }
            }
            g.Restore(head);
            using (Pen arm = new Pen(skin, 14))
            {
                arm.StartCap = LineCap.Round; arm.EndCap = LineCap.Round;
                g.DrawLine(arm, 96, 138, 77, 165);
                if (state == "equip") g.DrawLines(arm, new[] { new Point(165, 138), new Point(190, 81), new Point(145, 19) });
                else if (throwing > 0) g.DrawLine(arm, 165, 138, 212, 110);
                else if (state == "smash")
                {
                    float reach = (float)Math.Max(0, Math.Sin(time * 10)) * 37;
                    g.DrawLine(arm, 165, 138, 177 + reach, 93);
                    g.FillEllipse(Brushes.Bisque, 172 + reach, 82, 20, 20);
                }
                else if (state == "walk") g.DrawLine(arm, 165, 138, 177 + (float)Math.Sin(time * 9) * 12, 170);
                else if (state == "smoke" || state == "light") g.DrawLine(arm, 165, 138, 166, 106);
                else if (state == "take") g.DrawLine(arm, 165, 138, 111, 158);
                else if (state == "count") g.DrawLine(arm, 165, 138, 104 + (float)Math.Sin(time * 4) * 11, 163);
                else g.DrawLine(arm, 165, 138, 180, 167);
            }
            if (reading)
            {
                using (Brush book = new SolidBrush(Color.FromArgb(194, 150, 91))) g.FillPolygon(book, new[] { new Point(85, 143), new Point(130, 151), new Point(176, 143), new Point(176, 182), new Point(130, 190), new Point(85, 182) });
                using (Pen paper = new Pen(Color.FromArgb(248, 236, 210), 2)) { g.DrawLine(paper, 130, 153, 130, 186); for (int i = 0; i < 3; i++) { g.DrawLine(paper, 94, 154 + i * 8, 121, 159 + i * 8); g.DrawLine(paper, 139, 159 + i * 8, 168, 154 + i * 8); } }
            }
            else
            {
                if (product != null && !collapsed && state != "walk" && state != "smash")
                {
                    if (state == "equip")
                    {
                        GraphicsState held = g.Save(); g.TranslateTransform(123, 0); g.ScaleTransform(0.55f, 0.55f); Pack(g, product, 0, 0); g.Restore(held);
                    }
                    else Pack(g, product, 58, 145);
                }
                if (state == "count" && product != null && !product.Vape)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float y = 145 + i * 7 + (float)Math.Sin(time * 4 + i) * 2;
                        g.FillRectangle(Brushes.Ivory, 102, y, 24, 4); g.FillRectangle(Brushes.SandyBrown, 102, y, 6, 4);
                    }
                }
                if (product != null && (state == "smoke" || state == "light" || state == "take") && !collapsed)
                {
                    float x = state == "take" ? 103 : 160, y = state == "take" ? 140 : 104;
                    if (product.Vape)
                    {
                        using (Brush b = new SolidBrush(ColorTranslator.FromHtml(product.Color))) g.FillRectangle(b, x, y, 12, 36);
                        g.FillRectangle(Brushes.DimGray, x + 2, y - 5, 8, 6);
                    }
                    else
                    {
                        float length = 8 + 34 * Math.Max(0, remaining);
                        g.FillRectangle(Brushes.SandyBrown, x, y, 9, 5);
                        g.FillRectangle(Brushes.Ivory, x + 9, y, length, 5);
                        using (Brush ember = new SolidBrush((int)(time * 8) % 2 == 0 ? Color.OrangeRed : Color.DarkOrange)) g.FillEllipse(ember, x + 8 + length, y - 1, 5, 7);
                        if (state == "light")
                        {
                            g.FillRectangle(Brushes.SlateGray, x + length, y + 19, 12, 21);
                            g.FillEllipse(Brushes.Orange, x + length + 1, y + 4, 10, 18);
                            g.FillEllipse(Brushes.LightYellow, x + length + 3, y + 10, 6, 10);
                        }
                    }
                }
                if (throwing > 0)
                {
                    float travel = 1 - throwing / 0.8f;
                    g.FillRectangle(Brushes.SandyBrown, 207 + travel * 32, 108 + travel * travel * 75, 15, 5);
                }
            }
            if (state == "walk" && drawWalkingBook)
            {
                float progress = Math.Min(1, actionElapsed);
                Book(g, new Rectangle((int)(90 - progress * 72), (int)(145 + progress * 62), 54, 26));
            }
            if (state == "crave")
            {
                g.FillEllipse(Brushes.Ivory, 147, 47, 10, 10); g.FillEllipse(Brushes.Ivory, 154, 35, 13, 13);
                g.FillEllipse(Brushes.Ivory, 157, 0, 107, 39);
                using (Font f = Theme.Font(10, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Ink)) g.DrawString("吸溜吸溜", f, b, 179, 12);
            }
            if (state == "sleep")
                using (Font f = Theme.Font(15, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Muted)) g.DrawString("Z z z", f, b, 170, 75 - (float)Math.Sin(time * 2) * 5);
            if (state == "smash") Crack(g, new PointF(212, 93), 63, actionElapsed / 20f);
            if (state == "think")
            {
                g.FillEllipse(Brushes.Ivory, 156, 47, 8, 8); g.FillEllipse(Brushes.Ivory, 163, 35, 12, 12);
                g.FillEllipse(Brushes.Ivory, 153, 0, 112, 39);
                using (Font f = Theme.Font(9, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Ink))
                    g.DrawString("没抽的赶紧借", f, b, 166, 12);
            }
            if (bulb && !collapsed)
            {
                using (Brush glow = new SolidBrush(Color.FromArgb(248, 221, 123))) g.FillEllipse(glow, 201, 4, 30, 32);
                using (Pen rays = new Pen(Color.Goldenrod, 2))
                {
                    g.DrawLine(rays, 216, 0, 216, 2); g.DrawLine(rays, 195, 12, 200, 15); g.DrawLine(rays, 234, 14, 240, 11);
                    g.DrawLine(rays, 211, 24, 215, 34); g.DrawLine(rays, 221, 24, 217, 34);
                }
                g.FillRectangle(Brushes.SlateGray, 210, 35, 13, 7);
            }
            g.Restore(original);
        }
        private static void Heart(Graphics g, float x, float y)
        {
            using (Brush pink = new SolidBrush(Color.FromArgb(218, 85, 118)))
            {
                g.FillEllipse(pink, x, y, 9, 9); g.FillEllipse(pink, x + 7, y, 9, 9);
                g.FillPolygon(pink, new[] { new PointF(x + 1, y + 6), new PointF(x + 15, y + 6), new PointF(x + 8, y + 17) });
            }
        }
        public static void Book(Graphics g, Rectangle r)
        {
            using (Brush b = new SolidBrush(Color.FromArgb(194, 150, 91))) g.FillRectangle(b, r);
            using (Pen p = new Pen(Color.FromArgb(248, 236, 210), 2))
            {
                g.DrawLine(p, r.Left + r.Width / 2, r.Top + 2, r.Left + r.Width / 2, r.Bottom - 2);
                for (int i = 1; i < 4; i++) g.DrawLine(p, r.Left + 4, r.Top + i * r.Height / 5, r.Right - 4, r.Top + i * r.Height / 5);
            }
        }
        public static void Crack(Graphics g, PointF center, float radius, float progress)
        {
            int arms = 3 + (int)(Math.Min(1, Math.Max(0, progress)) * 7);
            using (Pen dark = new Pen(Color.FromArgb(100, 100, 100), 2))
            using (Pen light = new Pen(Color.FromArgb(238, 238, 238), 1))
            {
                for (int i = 0; i < arms; i++)
                {
                    double angle = i * 2.39996;
                    PointF middle = new PointF(center.X + (float)Math.Cos(angle + 0.15) * radius * 0.48f, center.Y + (float)Math.Sin(angle + 0.15) * radius * 0.48f);
                    PointF end = new PointF(center.X + (float)Math.Cos(angle) * radius, center.Y + (float)Math.Sin(angle) * radius);
                    PointF fork = new PointF(middle.X + (float)Math.Cos(angle + 0.8) * radius * 0.36f, middle.Y + (float)Math.Sin(angle + 0.8) * radius * 0.36f);
                    g.DrawLines(dark, new[] { center, middle, end }); g.DrawLine(dark, middle, fork);
                    g.DrawLine(light, center.X + 2, center.Y, middle.X + 2, middle.Y);
                }
            }
        }
        public static void Item(Graphics g, Product product, Rectangle bounds)
        {
            if (product == null) return;
            if (AssetArt.Item(g, product, bounds)) return;
            if (product.Rice) { Rice(g, bounds); return; }
            GraphicsState saved = g.Save();
            g.TranslateTransform(bounds.X, bounds.Y); g.ScaleTransform(bounds.Width / 41f, bounds.Height / 51f);
            Pack(g, product, 0, 0); g.Restore(saved);
        }
        public static void Rice(Graphics g, Rectangle bounds)
        {
            if (AssetArt.Item(g, Catalog.Get("rice"), bounds)) return;
            GraphicsState saved = g.Save();
            g.TranslateTransform(bounds.X, bounds.Y); g.ScaleTransform(bounds.Width / 80f, bounds.Height / 60f);
            using (Brush b = new SolidBrush(Color.FromArgb(244, 242, 222))) g.FillEllipse(b, 9, 6, 62, 34);
            for (int i = 0; i < 7; i++) g.FillEllipse(Brushes.White, 14 + i * 7, 5 + (i % 3) * 3, 13, 17);
            using (Brush b = new LinearGradientBrush(new Rectangle(4, 22, 72, 30), Color.Silver, Color.FromArgb(98, 104, 115), LinearGradientMode.Horizontal))
                g.FillPie(b, 4, 7, 72, 46, 0, 180);
            using (Pen p = new Pen(Color.FromArgb(106, 112, 122), 2)) g.DrawArc(p, 4, 7, 72, 46, 0, 180);
            using (Pen p = new Pen(Color.FromArgb(222, 225, 230), 3)) g.DrawEllipse(p, 4, 20, 72, 13);
            g.Restore(saved);
        }
        private static void Pack(Graphics g, Product product, int x, int y)
        {
            using (Brush b = new SolidBrush(ColorTranslator.FromHtml(product.Color))) g.FillRectangle(b, x, y, 41, 51);
            using (Pen p = new Pen(Color.FromArgb(115, 125, 110))) g.DrawRectangle(p, x, y, 41, 51);
            using (Brush b = new SolidBrush(Color.FromArgb(246, 241, 225))) g.FillRectangle(b, x + 2, y + 12, 37, 21);
            using (Font f = Theme.Font(6, FontStyle.Bold))
            using (Brush b = new SolidBrush(Theme.Ink))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                g.DrawString(product.Name.Length > 4 ? product.Name.Substring(0, 4) : product.Name, f, b, new RectangleF(x + 1, y + 15, 39, 20), format);
        }
    }
}
