using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SmokeSimulator
{
    // All coordinates use a 1254-square authoring canvas. Props are separate from the body.
    internal static class AssetArt
    {
        private const float SmokingPropScale = 1.25f;
        internal static readonly PointF SmashImpact = new PointF(111, 96);
        private static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();
        private static readonly Dictionary<string, Rectangle> Bounds = new Dictionary<string, Rectangle>();
        internal static readonly string Root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets");
        internal static Bitmap Get(string key)
        {
            Bitmap value;
            if (Cache.TryGetValue(key, out value)) return value;
            string file = Path.Combine(Root, key + ".png");
            if (!File.Exists(file)) { Cache[key] = null; return null; }
            using (Image source = Image.FromFile(file)) value = new Bitmap(source);
            Cache[key] = value;
            return value;
        }
        private static Rectangle Content(string key, Bitmap image)
        {
            Rectangle result;
            if (Bounds.TryGetValue(key, out result)) return result;
            int left = image.Width, top = image.Height, right = 0, bottom = 0;
            for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                if (image.GetPixel(x, y).A > 32) { left = Math.Min(left, x); top = Math.Min(top, y); right = Math.Max(right, x); bottom = Math.Max(bottom, y); }
            result = left > right ? new Rectangle(0, 0, image.Width, image.Height) : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
            Bounds[key] = result; return result;
        }
        internal static bool Item(Graphics g, Product product, RectangleF box)
        {
            if (product == null) return false;
            string key = "items/" + product.Id;
            Bitmap image = Get(key); if (image == null) return false;
            Rectangle source = Content(key, image);
            float scale = Math.Min(box.Width / source.Width, box.Height / source.Height);
            RectangleF target = new RectangleF(box.X + (box.Width - source.Width * scale) / 2, box.Y + (box.Height - source.Height * scale) / 2, source.Width * scale, source.Height * scale);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, target, source, GraphicsUnit.Pixel); return true;
        }
        internal static Color SmokeColor(Product p, int index)
        {
            string style = p == null ? "gray" : p.SmokeStyle;
            if (style == "green") return Color.FromArgb(102, 191, 117);
            if (style == "pink") return Color.FromArgb(237, 139, 184);
            if (style == "rainbow") return new[] { Color.LightCoral, Color.Orange, Color.Gold, Color.MediumSeaGreen, Color.DeepSkyBlue, Color.MediumPurple, Color.HotPink }[(index % 7 + 7) % 7];
            return Color.FromArgb(169, 173, 179);
        }
        internal static bool Person(Graphics g, Product product, string state, float remaining, float time, float throwing, bool cough, float actionElapsed, bool hearts, bool bulb, bool exhaling = false, float breathPhase = -1, float sessionAge = 7, float finishSeconds = 0)
        {
            if (state == "drunk" || state == "revive" || state == "close_book") { throwing = 0; cough = false; }
            if (state == "cough" || (cough && (state == "smoke" || state == "light"))) return CoughPerson(g, product, remaining, time);
            if (state == "count" && product != null && !product.Vape) return CountPerson(g, product, time);
            if (product != null && state != "collapsed" && (state == "smoke" || state == "take" || state == "place" || state == "stow_pack" || state == "lighter_out" || state == "light" || state == "stow_lighter" || state == "vape_raise" || state == "pocket" || state == "toss" || throwing > 0))
                return RigPerson(g, product, state, remaining, time, throwing, cough, hearts, bulb, breathPhase < 0 ? exhaling ? .7f : .2f : breathPhase, sessionAge, finishSeconds);
            bool smoking = state == "smoke" || state == "light";
            string pose = state == "collapsed" ? "dead" : state == "smoke" || state == "light" ? product != null && product.Vape ? "vape" : "smoke" : state == "take" ? "count" : state;
            if (state == "smoke" && exhaling) pose = product != null && product.Vape ? "vape_exhale" : "exhale";
            if (cough && smoking) pose = "cough";
            if (throwing > 0 && state != "collapsed") pose = "throw";
            if (state == "walk" && actionElapsed < .8f) pose = "book_throw";
            Bitmap image = Get("poses/" + pose); if (image == null) return false;
            GraphicsState saved = g.Save();
            float bob = state == "walk" ? (float)Math.Sin(time * 9) : 0;
            g.TranslateTransform(0, bob);
            // Keep every pose registered to the same canvas, rather than fitting each silhouette separately.
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(image, new RectangleF(22, 0, 224, 224));
            g.TranslateTransform(22, 0); g.ScaleTransform(224f / 1254, 224f / 1254);
            if (product != null && state != "collapsed" && throwing <= 0)
            {
                if (state == "equip")
                {
                    HeldItem(g, product, new RectangleF(324, 106, 109, 153));
                    Fingers(g, image, new Rectangle(326, 204, 34, 57));
                }
                else if (state == "count" || state == "take")
                {
                    HeldItem(g, product, new RectangleF(508, 610, 104, 133));
                    if (!product.Vape)
                    {
                        for (int i = 0; i < 3; i++) Cigarette(g, product, 527 + i * 21, 638, .9f, time, -90, false);
                    }
                    Fingers(g, image, new Rectangle(562, 680, 88, 66));
                }
                else if (smoking)
                {
                    if (cough)
                    {
                        if (product.Vape) HeldItem(g, product, new RectangleF(822, 807, 64, 142));
                        else Cigarette(g, product, 836, 829, remaining, time, 7);
                        Fingers(g, image, new Rectangle(803, 812, 43, 36));
                    }
                    else if (product.Vape)
                    {
                        GraphicsState held = g.Save(); g.TranslateTransform(exhaling ? 503 : 565, exhaling ? 548 : 482); g.RotateTransform(exhaling ? 12 : 20);
                        HeldItem(g, product, new RectangleF(-26, 0, 64, 142)); g.Restore(held);
                        Fingers(g, image, exhaling ? new Rectangle(449, 598, 42, 65) : new Rectangle(478, 515, 53, 62));
                    }
                    else if (exhaling)
                    {
                        HeldItem(g, product, new RectangleF(710, 595, 90, 108));
                        Fingers(g, image, new Rectangle(742, 643, 38, 56));
                        Cigarette(g, product, 404, 556, remaining, time, 172);
                        Fingers(g, image, new Rectangle(391, 545, 21, 24));
                    }
                    else
                    {
                        HeldItem(g, product, new RectangleF(776, 821, 70, 93));
                        Fingers(g, image, new Rectangle(777, 803, 44, 40));
                        Cigarette(g, product, 558, 492, remaining, time, 172);
                        Fingers(g, image, new Rectangle(529, 486, 27, 26));
                        if (state == "light")
                        {
                            g.FillRectangle(Brushes.SlateGray, 440, 526, 27, 43);
                            g.FillEllipse(Brushes.Orange, 443, 494, 21, 37);
                            g.FillEllipse(Brushes.LightYellow, 448, 508, 11, 23);
                        }
                    }
                }
            }
            if (throwing > 0 && state != "collapsed" && product != null)
            {
                float travel = 1 - Math.Min(1, throwing / .8f);
                GraphicsState thrown = g.Save(); g.TranslateTransform(230 - travel * 190, 640 + travel * travel * 340); g.RotateTransform(travel * 290);
                if (product.Vape) HeldItem(g, product, new RectangleF(-24, 0, 50, 110));
                else { g.FillRectangle(Brushes.SandyBrown, 0, 0, 28, 12); g.FillRectangle(Brushes.Gray, 28, 0, 8, 12); }
                g.Restore(thrown);
            }
            g.Restore(saved);
            if (state == "crave" || state == "think")
            {
                g.FillEllipse(Brushes.Ivory, 151, 27, 112, 34);
                using (Font f = Theme.Font(9, FontStyle.Bold)) using (Brush b = new SolidBrush(Theme.Ink)) g.DrawString(state == "crave" ? "吸溜吸溜" : "没抽的赶紧借", f, b, 163, 35);
            }
            if (state == "smash") Art.Crack(g, SmashImpact, 63, actionElapsed / 20f);
            if (bulb && state != "collapsed") LightBulb(g);
            return true;
        }
        private static float Smooth(float x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }
        private static float Mix(float a, float b, float t) { return a + (b - a) * Smooth(t); }
        private static bool CoughPerson(Graphics g, Product p, float remaining, float time)
        {
            Bitmap body = Get("poses/cough_left"); if (body == null) return false;
            GraphicsState s = g.Save(); g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(body, new RectangleF(22, 0, 224, 224));
            g.TranslateTransform(22, 0); g.ScaleTransform(224f / 1254, 224f / 1254);
            if (p != null)
            {
                if (p.Vape) HeldItem(g, p, new RectangleF(352, 839, 95, 178));
                else Cigarette(g, p, 410, 858, remaining, time, 170);
                Fingers(g, body, new Rectangle(375, 813, 82, 86));
            }
            g.Restore(s); return true;
        }
        private static bool CountPerson(Graphics g, Product p, float time)
        {
            Bitmap body = Get("poses/count_body"), arm = Get("poses/count_arm"); if (body == null || arm == null) return false;
            float t = time % 4, liftFraction = t < .6f ? 0 : t < 1.7f ? Smooth((t - .6f) / 1.1f) : t < 2.2f ? 1 : t < 3.4f ? 1 - Smooth((t - 2.2f) / 1.2f) : 0;
            int slot = (int)(time / 4) % 2;
            float x = 535 + slot * 22, full = (24 + (p.Format == "Super King Size" ? 120 : 88)) * SmokingPropScale;
            float lift = (full - 14) * liftFraction;
            GraphicsState saved = g.Save(); g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(body, new RectangleF(22, 0, 224, 224));
            g.TranslateTransform(22, 0); g.ScaleTransform(224f / 1254, 224f / 1254);
            using (Pen sleeve = new Pen(Color.FromArgb(36, 47, 78), 52))
            {
                sleeve.StartCap = sleeve.EndCap = LineCap.Round;
                g.DrawBezier(sleeve, new PointF(512, 596), new PointF(466, 625), new PointF(432, 682 - lift / 2), new PointF(466, 715 - lift));
            }
            Color pack = ColorTranslator.FromHtml(p.Color);
            using (Brush lid = new SolidBrush(pack))
                g.FillPolygon(lid, new[] { new PointF(518, 632), new PointF(605, 632), new PointF(604, 603), new PointF(520, 603) });
            g.FillPolygon(Brushes.DimGray, new[] { new PointF(518, 641), new PointF(530, 628), new PointF(605, 628), new PointF(605, 641) });
            for (int i = 0; i < 3; i++) if (i != slot) g.FillEllipse(p.Id == "black_devil_vanilla" ? Brushes.Black : Brushes.Wheat, 532 + i * 22, 633, 9, 5);
            // Draw the entire selected stick behind the front wall: only the extracted part is visible.
            GraphicsState insideBox = g.Save();
            g.SetClip(new RectangleF(0, 0, 1254, 640), CombineMode.Intersect);
            Cigarette(g, p, x, 640 - lift, 1, time, 90, false);
            g.Restore(insideBox);
            HeldItem(g, p, new RectangleF(508, 640, 104, 133));
            g.DrawLine(Pens.Silver, 518, 640, 605, 640);
            Rectangle source = Content("poses/count_arm", arm);
            g.DrawImage(arm, new RectangleF(408 + x - 542, 555 + 49 - lift, 137, 165), source, GraphicsUnit.Pixel);
            Fingers(g, body, new Rectangle(562, 680, 88, 76));
            g.Restore(saved); return true;
        }
        private static PointF RigHand(bool left, float angle)
        {
            double r = angle * Math.PI / 180; float x = left ? -93.6f : 93.6f, y = -86.4f;
            return new PointF((left ? 680 : 470) + (float)(x * Math.Cos(r) - y * Math.Sin(r)), 580 + (float)(x * Math.Sin(r) + y * Math.Cos(r)));
        }
        private static PointF VapeGrip(bool left, float angle)
        {
            // Center of the curled fingers, rather than the raised index fingertip.
            double r = angle * Math.PI / 180; float x = left ? -78 : 78, y = -55.2f;
            return new PointF((left ? 680 : 470) + (float)(x * Math.Cos(r) - y * Math.Sin(r)), 580 + (float)(x * Math.Sin(r) + y * Math.Cos(r)));
        }
        private static void RigArm(Graphics g, Bitmap arm, bool left, float angle)
        {
            GraphicsState s = g.Save(); g.TranslateTransform(left ? 680 : 470, 580); g.RotateTransform(angle);
            g.ScaleTransform(left ? -.12f : .12f, .12f); g.TranslateTransform(-280, -820);
            g.DrawImage(arm, new RectangleF(0, 0, 1254, 1254)); g.Restore(s);
        }
        private static bool RigPerson(Graphics g, Product p, string state, float remaining, float time, float throwing, bool cough, bool hearts, bool bulb, float phase, float age, float finish)
        {
            Bitmap body = Get("poses/rig_body"), arm = Get("poses/rig_arm");
            if (body == null || arm == null) return false;
            bool toss = state == "toss" || throwing > 0, pocket = state == "pocket";
            float ending = finish > 0 ? 1 - finish / 1.5f : 1 - throwing / .8f;
            // Eight-second tobacco cycle: hold at lips, ease away, exhale, then return.
            float away = phase < .38f ? 0 : phase < .55f ? Smooth((phase - .38f) / .17f) : phase < .82f ? 1 : 1 - Smooth((phase - .82f) / .18f);
            float right = away * 13, left = -120;
            bool pack = false, lighter = false, lit = state == "smoke" || toss;
            if (p.Vape)
            {
                right = 30 + away * 9; left = -right;
                if (state == "vape_raise") { right = Mix(100, 30, age / 2); left = -right; }
                if (pocket) { right = Mix(30, 100, ending); left = -right; }
            }
            else
            {
                if (state == "take") { left = Mix(-120, -70, age / .5f); right = Mix(110, 75, age / 1.5f); pack = true; }
                if (state == "place") { left = -70; right = Mix(75, 0, (age - 1.5f) / 1.5f); pack = true; }
                if (state == "stow_pack") { left = Mix(-70, -120, age - 3); right = 0; pack = age < 3.8f; }
                if (state == "lighter_out") { right = age < 4.4f ? Mix(0, 110, (age - 4) / .4f) : Mix(110, 75, (age - 4.4f) / .6f); lighter = age >= 4.4f; }
                if (state == "light") { right = 75; lighter = true; lit = true; }
                if (state == "stow_lighter") { right = age < 6.5f ? Mix(75, 110, (age - 6) * 2) : Mix(110, 0, (age - 6.5f) * 2); lighter = age < 6.5f; lit = true; }
            }
            if (toss) { right = Mix(13, -65, ending / .55f); left = -120; }
            GraphicsState saved = g.Save();
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            // This image is identical in every frame. No bobbing or full-body pose swaps.
            g.DrawImage(body, new RectangleF(22, 0, 224, 224));
            g.TranslateTransform(22, 0); g.ScaleTransform(224f / 1254, 224f / 1254);
            PointF hand = RigHand(false, right), other = RigHand(true, left);
            if (pack) HeldItem(g, p, new RectangleF(other.X - 36, other.Y - 35, 75, 105));
            if (p.Vape && !toss && (!pocket || ending < .8f))
            {
                PointF grip = VapeGrip(false, right), otherGrip = VapeGrip(true, left);
                float middleX = (grip.X + otherGrip.X) / 2, middleY = (grip.Y + otherGrip.Y) / 2;
                // The device follows both palms through raising, breathing and pocketing.
                HeldItem(g, p, new RectangleF(middleX - 43 * SmokingPropScale, middleY - 75 * SmokingPropScale, 86 * SmokingPropScale, 150 * SmokingPropScale));
            }
            if (!p.Vape && !toss)
            {
                if (state == "lighter_out" || state == "light" || state == "stow_lighter")
                    Cigarette(g, p, 582, 487, remaining, time, state == "stow_lighter" ? Mix(100, 172, age - 6) : state == "lighter_out" ? Mix(172, 100, (age - 4) / .6f) : 100, lit);
                else Cigarette(g, p, hand.X + 18, hand.Y - 4, remaining, time, state == "take" ? -90 : state == "place" ? Mix(-90, -188, (age - 1.5f) / 1.5f) : 172, lit);
            }
            if (lighter)
            {
                GraphicsState ignition = g.Save();
                float extraLength = (24 + (p.Format == "Super King Size" ? 120 : 88)) * (SmokingPropScale - 1);
                // Keep the flame at the enlarged cigarette tip during the existing lighting motion.
                g.TranslateTransform(-.173648f * extraLength, .984808f * extraLength);
                g.FillRectangle(Brushes.SlateGray, hand.X - 12, hand.Y - 32, 28, 43);
                g.FillRectangle(Brushes.Silver, hand.X - 12, hand.Y - 38, 28, 10);
                if (state == "light") { g.FillEllipse(Brushes.Orange, hand.X - 7, hand.Y - 67, 19, 32); g.FillEllipse(Brushes.LightYellow, hand.X - 2, hand.Y - 53, 9, 17); }
                g.Restore(ignition);
            }
            RigArm(g, arm, true, left); RigArm(g, arm, false, right);
            if (toss)
            {
                float flight = Smooth((ending - .22f) / .78f);
                GraphicsState prop = g.Save(); g.TranslateTransform(hand.X - flight * 420, hand.Y + flight * flight * 430); g.RotateTransform(flight * 320);
                if (p.Vape) HeldItem(g, p, new RectangleF(-28, 0, 56, 115));
                else Cigarette(g, p, 0, 0, 0, time, 0, false);
                g.Restore(prop);
            }
            g.Restore(saved);
            if (bulb) LightBulb(g);
            return true;
        }
        private static void HeldItem(Graphics g, Product product, RectangleF box)
        {
            if (!Item(g, product, box)) Art.Item(g, product, Rectangle.Round(box));
        }
        private static void Fingers(Graphics g, Bitmap image, Rectangle area)
        {
            // Repaint only the grip from the clean body above the prop.
            RectangleF source = new RectangleF(area.X * image.Width / 1254f, area.Y * image.Height / 1254f, area.Width * image.Width / 1254f, area.Height * image.Height / 1254f);
            g.DrawImage(image, area, source, GraphicsUnit.Pixel);
        }
        private static void LightBulb(Graphics g)
        {
            GraphicsState saved = g.Save();
            // Keep the icon beside the upper-right hair silhouette, in character coordinates.
            g.TranslateTransform(178, 13); g.RotateTransform(12, MatrixOrder.Prepend);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 4; i > 0; i--)
                using (Brush glow = new SolidBrush(Color.FromArgb(20 + (4 - i) * 7, 255, 227, 123)))
                    g.FillEllipse(glow, -i * 1.7f, -i * 1.7f, 24 + i * 3.4f, 27 + i * 3.4f);
            using (GraphicsPath glass = new GraphicsPath())
            {
                glass.AddBezier(7, 27, 7, 21, 1, 20, 1, 12);
                glass.AddBezier(1, 12, 1, -2, 23, -2, 23, 12);
                glass.AddBezier(23, 12, 23, 20, 17, 21, 17, 27); glass.CloseFigure();
                using (LinearGradientBrush fill = new LinearGradientBrush(new RectangleF(1, 0, 22, 28), Color.FromArgb(255, 255, 249), Color.FromArgb(255, 211, 87), 55)) g.FillPath(fill, glass);
                using (Pen edge = new Pen(Color.FromArgb(193, 132, 38), 1.1f)) g.DrawPath(edge, glass);
            }
            using (Pen shine = new Pen(Color.White, 2.8f))
            { shine.StartCap = shine.EndCap = LineCap.Round; g.DrawBezier(shine, 6, 12, 5, 8, 7, 5, 10, 5); }
            using (Pen filament = new Pen(Color.FromArgb(173, 101, 28), 1.1f))
            {
                g.DrawLine(filament, 10, 27, 8, 16); g.DrawLine(filament, 14, 27, 16, 16);
                g.DrawLines(filament, new[] { new PointF(8, 16), new PointF(10, 18), new PointF(12, 16), new PointF(14, 18), new PointF(16, 16) });
            }
            using (Pen hotFilament = new Pen(Color.FromArgb(255, 255, 239), .8f))
                g.DrawLines(hotFilament, new[] { new PointF(8, 16), new PointF(10, 18), new PointF(12, 16), new PointF(14, 18), new PointF(16, 16) });
            using (LinearGradientBrush metal = new LinearGradientBrush(new Rectangle(7, 27, 10, 7), Color.FromArgb(220, 226, 230), Color.FromArgb(87, 99, 113), 0f))
                g.FillRectangle(metal, 7, 27, 10, 7);
            using (Pen groove = new Pen(Color.FromArgb(104, 113, 126), .8f))
            { g.DrawLine(groove, 7, 29, 17, 28); g.DrawLine(groove, 7, 32, 17, 31); }
            using (Brush contact = new SolidBrush(Color.FromArgb(71, 80, 91))) g.FillEllipse(contact, 9, 33, 6, 3);
            using (Pen ray = new Pen(Color.FromArgb(255, 241, 180), 1.9f))
            { ray.StartCap = ray.EndCap = LineCap.Round; g.DrawLine(ray, 12, -4, 12, -7); g.DrawLine(ray, 27, 5, 30, 3); g.DrawLine(ray, 28, 17, 32, 18); }
            g.Restore(saved);
        }
        private static void Cigarette(Graphics g, Product p, float x, float y, float remaining, float time, float angle, bool lit = true)
        {
            GraphicsState saved = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(angle);
            g.ScaleTransform(SmokingPropScale, SmokingPropScale);
            float width = p.Format == "细烟" ? 8 : 12, full = p.Format == "Super King Size" ? 120 : 88;
            // Only tobacco burns down. The filter stays anchored at the mouth/hand.
            float length = full * Math.Max(0, Math.Min(1, remaining));
            g.FillRectangle(p.Id == "black_devil_vanilla" ? Brushes.Black : Brushes.SandyBrown, 0, -width / 2, 24, width);
            g.FillRectangle(p.Id == "black_devil_vanilla" ? Brushes.Pink : Brushes.Ivory, 24, -width / 2, length, width);
            if (lit) g.FillRectangle((int)(time * 8) % 2 == 0 ? Brushes.OrangeRed : Brushes.DarkOrange, 24 + length, -width / 2, 5, width);
            g.Restore(saved);
        }
    }
}
