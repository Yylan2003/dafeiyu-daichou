using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using SmokeSimulator;

internal static class AssetTests
{
    private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static int Main(string[] args)
    {
        try
        {
            string output = args[0]; Directory.CreateDirectory(output);
            foreach (Product p in Catalog.All)
            {
                Bitmap image = AssetArt.Get("items/" + p.Id);
                Assert(image != null && image.Size == new Size(384, 384), "missing or non-uniform item: " + p.Id);
                Assert(image.GetPixel(0, 0).A == 0, "item has opaque outside padding: " + p.Id);
            }
            using (Bitmap sheet = new Bitmap(5 * 230, 5 * 270))
            using (Graphics g = Graphics.FromImage(sheet))
            using (Font font = new Font("Microsoft YaHei", 11))
            {
                g.Clear(Color.FromArgb(237, 241, 231));
                for (int i = 0; i < Catalog.All.Length; i++)
                {
                    Product p = Catalog.All[i]; int x = i % 5 * 230, y = i / 5 * 270;
                    Assert(AssetArt.Item(g, p, new Rectangle(x + 40, y + 15, 150, 200)), "item fallback: " + p.Id);
                    g.DrawString(p.Name + " · ¥" + p.Price, font, Brushes.Black, x + 12, y + 223);
                    g.DrawString(p.Format, font, Brushes.DimGray, x + 12, y + 246);
                }
                sheet.Save(Path.Combine(output, "商品图总览.png"), ImageFormat.Png);
            }
            string[] poses = { "read", "equip", "count", "take", "light", "smoke", "think", "crave", "walk", "sleep", "smash", "collapsed", "close_book", "cough", "drunk", "revive" };
            using (Bitmap sheet = new Bitmap(4 * 268, 4 * 270))
            using (Graphics g = Graphics.FromImage(sheet))
            using (Font font = new Font("Microsoft YaHei", 10))
            {
                g.Clear(Color.FromArgb(237, 241, 231));
                for (int i = 0; i < poses.Length; i++)
                {
                    GraphicsState state = g.Save(); g.TranslateTransform(i % 4 * 268, i / 4 * 270);
                    Assert(AssetArt.Person(g, Catalog.Get("liqun"), poses[i], .6f, 1, 0, false, 5, false, false), "pose fallback: " + poses[i]);
                    g.DrawString(poses[i], font, Brushes.Black, 90, 240); g.Restore(state);
                }
                sheet.Save(Path.Combine(output, "人物动作接入.png"), ImageFormat.Png);
            }
            string[] ids = { "liqun", "daguanyuan", "black_devil_vanilla", "lucky_strike", "relx5", "duckbill", "lost_mary" };
            using (Bitmap sheet = new Bitmap(4 * 268, 2 * 290))
            using (Graphics g = Graphics.FromImage(sheet))
            using (Font font = new Font("Microsoft YaHei", 10))
            {
                g.Clear(Color.FromArgb(237, 241, 231)); g.SmoothingMode = SmoothingMode.AntiAlias;
                for (int i = 0; i < ids.Length; i++)
                {
                    Product p = Catalog.Get(ids[i]); GraphicsState state = g.Save(); g.TranslateTransform(i % 4 * 268, i / 4 * 290);
                    SmokeTrail trail = new SmokeTrail(); trail.Emit(p); trail.Frame(.6); trail.Draw(g);
                    Assert(AssetArt.Person(g, p, "smoke", .7f, 1, 0, false, 0, p.Price > 40, p.Price > 40, true), "smoke fallback");
                    g.DrawString(p.Name, font, Brushes.Black, 70, 239);
                    g.DrawString(p.Format + " / " + p.SmokeStyle, font, Brushes.DimGray, 30, 261); g.Restore(state);
                }
                sheet.Save(Path.Combine(output, "手持与烟雾.png"), ImageFormat.Png);
            }
            // Actual inhale/exhale/cough sprites and the common colored smoke layer.
            string[] cycleIds = { "liqun", "daguanyuan", "black_devil_vanilla", "lost_mary" };
            using (Bitmap sheet = new Bitmap(268 * 3, 260 * 4))
            using (Graphics g = Graphics.FromImage(sheet))
            using (Font font = new Font("Microsoft YaHei", 10))
            {
                g.Clear(Color.FromArgb(237, 241, 231));
                for (int row = 0; row < cycleIds.Length; row++) for (int col = 0; col < 3; col++)
                {
                    Product p = Catalog.Get(cycleIds[row]); GraphicsState state = g.Save(); g.TranslateTransform(col * 268, row * 260);
                    if (col > 0) { SmokeTrail trail = new SmokeTrail(); trail.Emit(p); trail.Frame(.6); trail.Draw(g); }
                    Assert(AssetArt.Person(g, p, "smoke", .5f, 1, 0, col == 2, 0, false, false, col == 1), "cycle pose missing");
                    g.DrawString(p.Name + " · " + new[] { "吸气", "吐气", "咳嗽烟雾颜色" }[col], font, Brushes.Black, 40, 235);
                    g.Restore(state);
                }
                sheet.Save(Path.Combine(output, "吸吐与咳嗽.png"), ImageFormat.Png);
            }
            // The mouth plume may cross the lips, but must leave the eyes and lowered hand clear.
            using (Bitmap plain = new Bitmap(268, 232)) using (Bitmap smoky = new Bitmap(268, 232))
            {
                Product p = Catalog.Get("lost_mary");
                using (Graphics g = Graphics.FromImage(plain)) { g.Clear(Color.White); AssetArt.Person(g, p, "smoke", .5f, 1, 0, false, 0, false, false, true); }
                using (Graphics g = Graphics.FromImage(smoky)) { g.Clear(Color.White); SmokeTrail trail = new SmokeTrail(); trail.Emit(p); trail.Frame(.3); trail.Draw(g); AssetArt.Person(g, p, "smoke", .5f, 1, 0, false, 0, false, false, true); trail.DrawMouth(g); }
                foreach (Rectangle protectedArea in new[] { new Rectangle(106, 62, 44, 22), new Rectangle(99, 107, 22, 25) })
                    for (int y = protectedArea.Top; y < protectedArea.Bottom; y++) for (int x = protectedArea.Left; x < protectedArea.Right; x++)
                    { Color a = plain.GetPixel(x, y), b = smoky.GetPixel(x, y); Assert(Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B) <= 6, "smoke obscures face/hand"); }
            }
            Color gray = AssetArt.SmokeColor(Catalog.Get("liqun"), 0), green = AssetArt.SmokeColor(Catalog.Get("daguanyuan"), 0), pink = AssetArt.SmokeColor(Catalog.Get("black_devil_vanilla"), 0);
            using (Bitmap a = new Bitmap(268, 232)) using (Bitmap b = new Bitmap(268, 232))
            {
                using (Graphics g = Graphics.FromImage(a)) { g.Clear(Color.White); AssetArt.Person(g, Catalog.Get("liqun"), "smoke", .5f, 1, 0, false, 0, false, false, false, .2f); }
                using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.White); AssetArt.Person(g, Catalog.Get("liqun"), "smoke", .5f, 1, 0, false, 0, false, false, true, .7f); }
                Rectangle movingHand = new Rectangle(80, 82, 63, 50);
                int movement = 0;
                for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++)
                    if (a.GetPixel(x, y) != b.GetPixel(x, y)) { Assert(movingHand.Contains(x, y), "body/left arm moved during tobacco breath"); movement++; }
                Assert(movement > 20, "right hand never moved");
            }
            int[] smokeCoverage = new int[2];
            for (int i = 0; i < 2; i++) using (Bitmap cloud = new Bitmap(268, 232))
            {
                using (Graphics g = Graphics.FromImage(cloud)) { SmokeTrail trail = new SmokeTrail(); trail.Emit(Catalog.Get(i == 0 ? "liqun" : "duckbill")); trail.Frame(.9); trail.Draw(g); }
                for (int y = 0; y < cloud.Height; y++) for (int x = 0; x < cloud.Width; x++) if (cloud.GetPixel(x, y).A > 20) smokeCoverage[i]++;
            }
            Assert(smokeCoverage[1] > smokeCoverage[0] * 1.7, "vape vapor not visibly larger");
            RenderRigReview(output);
            Assert(Math.Abs(gray.R - gray.G) < 15 && green.G > green.R && pink.R > pink.G, "smoke color mapping incorrect");
            Assert(Enumerable.Range(0, 7).Select(i => AssetArt.SmokeColor(Catalog.Get("lost_mary"), i).ToArgb()).Distinct().Count() == 7, "rainbow has fewer than seven colors");
            // A switch must visibly change the prop, and burn progress must visibly shorten a cigarette.
            Func<string, float, Bitmap> render = delegate(string id, float remaining) { Bitmap b = new Bitmap(268, 240); using (Graphics g = Graphics.FromImage(b)) { g.Clear(Color.White); AssetArt.Person(g, Catalog.Get(id), "smoke", remaining, 1, 0, false, 0, false, false); } return b; };
            using (Bitmap a = render("liqun", 1)) using (Bitmap b = render("liqun", .1f)) using (Bitmap c = render("relx5", 1)) using (Bitmap d = render("lost_mary", 1))
            {
                Assert(DifferentPixels(a, b) > 12, "burn progress invisible");
                Assert(DifferentPixels(c, d) > 50, "vapes look identical");
            }
            RenderActionReview(output);
            RenderPropReview(output);
            Console.WriteLine("PASS: 25 uniform PNG items, all live poses, seven smoke colors, visible product switching and burning; static drunk pose and alternating count loop; rendered review sheets."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static int DifferentPixels(Bitmap a, Bitmap b)
    {
        int count = 0; for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++) if (a.GetPixel(x, y) != b.GetPixel(x, y)) count++; return count;
    }
    private static void RenderPropReview(string output)
    {
        using (Bitmap plume = new Bitmap(268, 232)) using (Graphics g = Graphics.FromImage(plume))
        {
            SmokeTrail trail = new SmokeTrail(); trail.Emit(Catalog.Get("lost_mary")); trail.Frame(.15); trail.DrawMouth(g);
            Assert(plume.GetPixel(125, 88).A > 0, "vape plume disconnected from lips");
            Assert(plume.GetPixel(125, 80).A == 0 && plume.GetPixel(125, 105).A == 0, "mouth plume covers eyes or gripping hand");
        }
        string[] ids = { "relx5", "duckbill", "lost_mary", "black_devil_vanilla" };
        using (Bitmap sheet = new Bitmap(536 * 3, 464 * ids.Length)) using (Graphics g = Graphics.FromImage(sheet))
        {
            g.Clear(Color.FromArgb(237, 241, 231));
            for (int row = 0; row < ids.Length; row++) for (int col = 0; col < 3; col++)
            {
                Product p = Catalog.Get(ids[row]); bool exhale = col == 2;
                string pose = col == 0 ? p.Vape ? "vape_raise" : "count" : "smoke";
                GraphicsState state = g.Save(); g.TranslateTransform(col * 536, row * 464); g.ScaleTransform(2, 2); g.SmoothingMode = SmoothingMode.AntiAlias;
                SmokeTrail trail = new SmokeTrail(); if (exhale) { trail.Emit(p); trail.Frame(.3); trail.Draw(g); }
                AssetArt.Person(g, p, pose, 1, 1.8f, 0, false, 0, false, false, exhale, exhale ? .7f : .2f, col == 0 ? 0 : 7);
                if (exhale) trail.DrawMouth(g); g.Restore(state);
            }
            sheet.Save(Path.Combine(output, "vape-grip-and-black-devil.png"));
        }
    }
    private static Bitmap ActionFrame(string pose, float time)
    {
        Bitmap b = new Bitmap(536, 464);
        using (Graphics g = Graphics.FromImage(b))
        {
            g.Clear(Color.FromArgb(237, 241, 231)); g.ScaleTransform(2, 2); g.SmoothingMode = SmoothingMode.AntiAlias;
            Assert(AssetArt.Person(g, Catalog.Get("liqun"), pose, .6f, time, 0, false, 0, false, false), "missing action: " + pose);
        }
        return b;
    }
    private static void RenderActionReview(string output)
    {
        using (Bitmap a = ActionFrame("drunk", 0)) using (Bitmap b = ActionFrame("drunk", 1))
            Assert(DifferentPixels(a, b) == 0, "drunk body still shakes");
        using (Bitmap a = ActionFrame("count", 0)) using (Bitmap b = ActionFrame("count", 1.8f)) using (Bitmap c = ActionFrame("count", 3.8f)) using (Bitmap d = ActionFrame("count", 5.8f))
        {
            Assert(DifferentPixels(a, b) > 100, "count did not pull stick out");
            Assert(DifferentPixels(a, c) == 0, "count did not return stick fully");
            Assert(DifferentPixels(b, d) > 50, "count did not change adjacent stick");
            // The motion is restricted to the sleeve, hand and box area; head and feet stay still.
            for (int y = 0; y < a.Height; y++) for (int x = 0; x < a.Width; x++)
                if (a.GetPixel(x, y) != b.GetPixel(x, y)) Assert(y >= 160 && y < 290, "count moved head or legs");
        }
        string frames = Path.Combine(output, "action-v4-frames"); Directory.CreateDirectory(frames);
        for (int i = 0; i < 32; i++) using (Bitmap b = ActionFrame("count", i / 4f)) b.Save(Path.Combine(frames, "count-" + i + ".png"));
        string[] poses = { "close_book", "cough", "drunk", "revive" };
        using (Bitmap sheet = new Bitmap(1072, 928)) using (Graphics g = Graphics.FromImage(sheet))
        {
            for (int i = 0; i < poses.Length; i++) using (Bitmap b = ActionFrame(poses[i], 0))
            { b.Save(Path.Combine(frames, poses[i] + ".png")); g.DrawImageUnscaled(b, i % 2 * 536, i / 2 * 464); }
            sheet.Save(Path.Combine(output, "actions-v4.png"));
        }
        File.WriteAllText(Path.Combine(output, "动作更新验收.html"), "<!doctype html><meta charset='utf-8'><title>大肥鱼动作更新验收</title><style>body{font:16px Microsoft YaHei;background:#edf1e7;color:#324733;margin:32px}img{width:402px;max-width:90vw}section{display:inline-block;vertical-align:top}input{width:300px}button{font:inherit}</style><h1>动作更新验收</h1><p>数烟使用软件实际绘制：每根4秒，两根交替循环。暂停后可拖动查看。其余图片展示关键姿态，软件切换时另有短暂过渡。</p><section><h2>05 数烟</h2><img id='frame'><p><button id='play'>暂停</button> <input id='seek' type='range' min='0' max='31' value='0'></p></section><section><h2>02 收书 · 2秒后离开桌面</h2><img src='action-v4-frames/close_book.png'></section><section><h2>14 咳嗽 · 左手捂嘴、右手持烟</h2><img src='action-v4-frames/cough.png'></section><section><h2>15 醉醺醺 · 进入时展示20秒</h2><img src='action-v4-frames/drunk.png'></section><section><h2>23 复活 · 展示3秒</h2><img src='action-v4-frames/revive.png'></section><script>let running=true;function draw(){frame.src='action-v4-frames/count-'+seek.value+'.png'};play.onclick=()=>{running=!running;play.textContent=running?'暂停':'播放'};seek.oninput=()=>{running=false;play.textContent='播放';draw()};setInterval(()=>{if(running){seek.value=(+seek.value+1)%32;draw()}},250);draw();</script>", System.Text.Encoding.UTF8);
    }
    private static void RenderRigReview(string output)
    {
        string frames = Path.Combine(output, "rig-frames"); Directory.CreateDirectory(frames);
        // The review uses the actual renderer, at 4 frames/second with interpolation in the app at 30 fps.
        foreach (string id in new[] { "marlboro_red", "lost_mary" })
        {
            Product p = Catalog.Get(id); float prep = p.Vape ? 2 : 7, period = p.Vape ? 6 : 8;
            int total = (int)((prep + period + 1.5f) * 4);
            for (int frame = 0; frame < total; frame++)
            {
                float t = frame / 4f, phase = Math.Max(0, Math.Min(.999f, (t - prep) / period));
                string state = t >= prep + period ? p.Vape ? "pocket" : "toss" : t >= prep ? "smoke" : p.Vape ? "vape_raise" : t < 1.5 ? "take" : t < 3 ? "place" : t < 4 ? "stow_pack" : t < 5 ? "lighter_out" : t < 6 ? "light" : "stow_lighter";
                using (Bitmap b = new Bitmap(536, 464)) using (Graphics g = Graphics.FromImage(b))
                {
                    g.Clear(Color.FromArgb(237, 241, 231)); g.ScaleTransform(2, 2); g.SmoothingMode = SmoothingMode.AntiAlias;
                    SmokeTrail trail = new SmokeTrail();
                    if (state == "smoke" && phase >= .55f) { trail.Emit(p); trail.Frame((phase - .55) * period); trail.Draw(g); }
                    AssetArt.Person(g, p, state, .7f, t, 0, false, 0, false, false, phase >= .55f, phase, t, Math.Max(0, prep + period + 1.5f - t));
                    trail.DrawMouth(g);
                    b.Save(Path.Combine(frames, id + "-" + frame + ".png"), ImageFormat.Png);
                }
            }
        }
        string html = "<!doctype html><meta charset='utf-8'><title>大肥鱼 · 动作验收</title><style>body{font:16px Microsoft YaHei;background:#edf1e7;color:#324733;margin:32px}img{width:402px;max-width:90vw}button,input{margin:12px;font:inherit}section{display:inline-block;vertical-align:top}</style><h1>分层动作验收</h1><p>按真实节奏播放；可暂停后拖动进度检查手部。此处压缩为一轮吸吐后展示收尾，软件仍按完整时长/口数结算。</p><button id='play'>暂停</button><section><h2>卷烟 · 取烟 → 点火 → 吸吐 → 丢烟头</h2><img id='c'><p id='cl'></p><input id='cs' type='range' min='0' max='65' value='0'></section><section><h2>电子烟 · 双手持握 → 吸吐 → 放回口袋</h2><img id='v'><p id='vl'></p><input id='vs' type='range' min='0' max='37' value='0'></section><script>let running=true;const labels=['取烟','放到嘴边','收烟盒','拿打火机','点火','收打火机','吸吐循环','丢烟头'];function draw(){let c=+cs.value,v=+vs.value;cImg.src='rig-frames/marlboro_red-'+c+'.png';vImg.src='rig-frames/lost_mary-'+v+'.png';cl.textContent=(c/4).toFixed(2)+'秒 · '+labels[c<6?0:c<12?1:c<16?2:c<20?3:c<24?4:c<28?5:c<60?6:7];vl.textContent=(v/4).toFixed(2)+'秒 · '+(v<8?'拿出电子烟':v<32?'慢速吸吐':'放回口袋')}const cImg=document.getElementById('c'),vImg=document.getElementById('v');play.onclick=()=>{running=!running;play.textContent=running?'暂停':'播放'};for(let s of [cs,vs])s.oninput=()=>{running=false;play.textContent='播放';draw()};setInterval(()=>{if(running){cs.value=(+cs.value+1)%66;vs.value=(+vs.value+1)%38;draw()}},250);draw();</script>";
        File.WriteAllText(Path.Combine(output, "动作验收.html"), html, System.Text.Encoding.UTF8);
    }
}
