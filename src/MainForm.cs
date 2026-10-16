using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace SmokeSimulator
{
    internal class MainForm : Form
    {
        private readonly Engine engine;
        private readonly string savePath;
        #if UI_TESTS
        private readonly bool smokeTest;
#endif
        private readonly Timer timer = new Timer { Interval = 33 };
        private readonly Stopwatch clock = Stopwatch.StartNew();
        private double last, saveElapsed, uiElapsed;
        private bool exiting, loading = true, saveFailed;
        private readonly NotifyIcon tray;
        private readonly PetForm pet;
        private RiceForm rice;
        private NoticePopup popup;
        private readonly Dictionary<int, DebrisForm> litter = new Dictionary<int, DebrisForm>();
        private readonly List<Product> choices = new List<Product>();
        private readonly BagSlot[] slots = new BagSlot[8];
        private Panel content;
        private Panel viewport;
        private ResponsiveLayout responsiveLayout;
        private ComboBox avatar, country, product, sort;
        private NumericUpDown quantity;
        private Label balance, workTime, health, progress, total, itemDetails, pageLabel, selectedLabel;
        private Button work, previous, next, equip, favorite, buy;
        private PreviewPanel preview;
        private TrackBar petSize;
        private Label petSizeValue;
        private ProductPanel shopImage;
        private string bagSelection = "";
        private bool previewOverride;
        private int page;

        public MainForm(Engine engine, string savePath, bool smokeTest)
        {
            this.engine = engine; this.savePath = savePath; 
#if UI_TESTS
            this.smokeTest = smokeTest;
#endif
            Text = "大肥鱼代抽1.0";
            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1040, screen.Width - 60), Math.Min(824, screen.Height - 80));
            MinimumSize = new Size(740, 600); StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Background; Font = Theme.Font(10, FontStyle.Regular); Icon = SystemIcons.Application;
            AutoScaleMode = AutoScaleMode.None;
            viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Theme.Background };
            content = new Panel { Size = new Size(1040, 824), BackColor = Theme.Background };
            Controls.Add(viewport); viewport.Controls.Add(content);
            BuildUI();
            responsiveLayout = new ResponsiveLayout(viewport, content);
            pet = new PetForm(engine, ShowSettings, ExitApp, ToggleWork, EndWork, OnInteraction);
            engine.Dropped += delegate(Debris d) { SpawnDebris(d, true); };
            engine.Puffed += pet.Emit;
            tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "大肥鱼代抽1.0 · 双击打开配置", Visible = true };
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("打开背包 / 商店", null, delegate { ShowSettings(); });
            menu.Items.Add("开始 / 暂停工作", null, delegate { ToggleWork(); });
            menu.Items.Add("结束工作", null, delegate { EndWork(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("退出软件", null, delegate { ExitApp(); });
            tray.ContextMenuStrip = menu; tray.DoubleClick += delegate { ShowSettings(); };
            FormClosing += delegate(object sender, FormClosingEventArgs e)
            {
                if (!exiting && e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    using (CloseChoiceDialog dialog = new CloseChoiceDialog())
                    {
                        DialogResult choice = dialog.ShowDialog(this);
                        if (choice == DialogResult.Yes) BeginInvoke((Action)ExitApp);
                        else if (choice == DialogResult.No) HideToTray();
                    }
                }
                else { Persist(); tray.Visible = false; }
            };
            Shown += delegate
            {
                foreach (Debris d in engine.Data.Litter.ToArray()) SpawnDebris(d, false);
#if UI_TESTS
                if (smokeTest) StartSmokeTest();
#endif
            };
            loading = false; RefreshStatus();
            timer.Tick += Tick; timer.Start();
        }
        private Panel Card(int x, int y, int w, int h)
        {
            Panel card = new Panel { Location = new Point(x, y), Size = new Size(w, h), BackColor = Color.FromArgb(250, 249, 245) };
            content.Controls.Add(card); return card;
        }
        private Label Label(Control parent, string text, int x, int y, int w, int h, float size, Color color)
        {
            Label label = Theme.Label(text, x, y, w, h, size, color); parent.Controls.Add(label); return label;
        }
        private Button Button(Control parent, string text, int x, int y, int w, Action action)
        {
            Button button = Theme.Button(text, x, y, w, Theme.Green);
            button.Click += delegate { action(); }; parent.Controls.Add(button); return button;
        }
        private ComboBox Combo(Control parent, int x, int y, int width, string[] items)
        {
            ComboBox combo = new ComboBox { Location = new Point(x, y), Width = width,
                DropDownStyle = ComboBoxStyle.DropDownList, Font = Theme.Font(10, FontStyle.Regular) };
            if (items != null) combo.Items.AddRange(items);
            parent.Controls.Add(combo); return combo;
        }
        private void BuildUI()
        {
            Label(content, "大肥鱼代抽1.0", 25, 18, 480, 64, 24, Theme.Ink);
            preview = new PreviewPanel(engine) { Location = new Point(24, 99), Size = new Size(480, 350), BackColor = Color.FromArgb(227, 233, 220) };
            content.Controls.Add(preview);
            Label(content, "背包", 26, 463, 80, 27, 13, Theme.Ink);
            sort = Combo(content, 112, 461, 154, new[] { "价格从低到高", "价格从高到低" });
            sort.SelectedIndex = engine.Data.PriceDescending ? 1 : 0;
            sort.SelectedIndexChanged += delegate { engine.Data.PriceDescending = sort.SelectedIndex == 1; page = 0; OnInteraction(); };
            favorite = Button(content, "收藏选中", 373, 457, 131, ToggleFavorite); favorite.Height = 33;
            for (int i = 0; i < 8; i++)
            {
                BagSlot slot = new BagSlot { Location = new Point(24 + (i % 4) * 122, 500 + (i / 4) * 109), Size = new Size(114, 101) };
                slot.Click += delegate { if (slot.Item != null) SelectBag(slot.Item.Id); };
                slot.FavoriteClick += delegate { if (slot.Item != null) { engine.ToggleFavorite(slot.Item.Id); OnInteraction(); } };
                slots[i] = slot; content.Controls.Add(slot);
            }
            previous = Button(content, "‹", 24, 722, 44, delegate { page--; RefreshBag(); }); previous.Height = 29;
            next = Button(content, "›", 158, 722, 44, delegate { page++; RefreshBag(); }); next.Height = 29;
            pageLabel = Label(content, "1 / 1", 72, 726, 83, 26, 10, Theme.Muted); pageLabel.TextAlign = ContentAlignment.TopCenter;
            selectedLabel = Label(content, "选择物品进行预览", 215, 725, 289, 28, 9, Theme.Muted);
            equip = Button(content, "装备", 24, 765, 232, ConfirmBag);
            Button(content, "卸下烟盒", 272, 765, 232, delegate
            {
                engine.Unequip(); bagSelection = ""; previewOverride = false; OnInteraction();
            });

            Panel avatarCard = Card(530, 99, 485, 148);
            Label(avatarCard, "1  你的代抽", 18, 12, 440, 28, 13, Theme.Ink);
            avatar = Combo(avatarCard, 18, 53, 449, new[] { "大肥鱼" });
            engine.Data.Avatar = 0;
            avatar.SelectedIndex = 0;
            avatar.SelectedIndexChanged += delegate { engine.Data.Avatar = avatar.SelectedIndex; OnInteraction(); };

            Label(avatarCard, "人物大小", 18, 95, 85, 24, 10, Theme.Ink);
            petSizeValue = Label(avatarCard, engine.Data.PetScalePercent + "%", 409, 95, 58, 24, 10, Theme.Ink);
            petSize = new TrackBar { Location = new Point(102, 90), Size = new Size(299, 34), AutoSize = false,
                Minimum = 50, Maximum = 200, SmallChange = 5, LargeChange = 25, TickFrequency = 25,
                Value = engine.Data.PetScalePercent, BackColor = avatarCard.BackColor, AccessibleName = "人物大小" };
            avatarCard.Controls.Add(petSize);
            petSize.ValueChanged += delegate
            {
                engine.Data.PetScalePercent = petSize.Value; petSizeValue.Text = petSize.Value + "%";
                if (pet != null) pet.ApplyScale();
            };
            petSize.MouseUp += delegate { if (!loading) Persist(); };
            petSize.KeyUp += delegate { if (!loading) Persist(); };

            Panel shop = Card(530, 262, 485, 286);
            Label(shop, "2  商店", 18, 13, 200, 28, 13, Theme.Ink);
            Label(shop, "游戏内虚拟商品", 316, 16, 151, 22, 9, Theme.Muted);
            shopImage = new ProductPanel { Location = new Point(18, 58), Size = new Size(146, 145), BackColor = Color.FromArgb(236, 235, 225) };
            shop.Controls.Add(shopImage);
            Label(shop, "各国香烟", 184, 45, 283, 24, 9, Theme.Muted);
            country = Combo(shop, 184, 68, 283, new[] { "中国烟", "外国烟", "电子烟", "大米饭" });
            Label(shop, "具体商品", 184, 103, 283, 23, 9, Theme.Muted);
            product = Combo(shop, 184, 126, 283, null);
            Label(shop, "购买数量（包 / 支 / 碗）", 184, 160, 283, 23, 9, Theme.Muted);
            quantity = new NumericUpDown { Location = new Point(184, 184), Width = 110, Minimum = 1, Maximum = 999, Value = 1 };
            shop.Controls.Add(quantity);
            total = Label(shop, "", 311, 184, 158, 28, 12, Theme.Ink);
            itemDetails = Label(shop, "", 18, 211, 449, 23, 9, Theme.Muted);
            buy = Button(shop, "购买并放入背包", 18, 240, 449, delegate
            {
                if (Chosen != null && engine.Buy(Chosen.Id, (int)quantity.Value))
                {
                    int index = engine.Backpack().FindIndex(s => s.Id == Chosen.Id); page = Math.Max(0, index / 8);
                }
                else if (Chosen != null) ShowNoticePopup(engine.Notice);
                OnInteraction();
            });
            country.SelectedIndex = 0;
            country.SelectedIndexChanged += delegate { UpdateProducts(); };
            product.SelectedIndexChanged += delegate { RefreshShop(); };
            quantity.ValueChanged += delegate { RefreshShop(); };
            UpdateProducts();

            Panel status = Card(530, 563, 485, 242);
            Label(status, "3  小人数据", 18, 8, 449, 26, 13, Theme.Ink);
            health = Label(status, "", 18, 38, 449, 24, 10, Theme.Ink);
            workTime = Label(status, "", 18, 65, 449, 46, 10, Theme.Muted);
            balance = Label(status, "", 18, 114, 449, 26, 12, Theme.Ink);
            progress = Label(status, "", 18, 153, 449, 23, 10, Theme.Muted);
            work = Button(status, "开始工作", 18, 197, 216, ToggleWork);
            Button(status, "结束工作", 251, 197, 216, EndWork);
        }
        private Product Chosen { get { return product.SelectedIndex < 0 || product.SelectedIndex >= choices.Count ? null : choices[product.SelectedIndex]; } }
        private void UpdateProducts()
        {
            string previousId = Chosen == null ? null : Chosen.Id;
            product.Items.Clear(); choices.Clear();
            foreach (Product p in Catalog.All.Where(p => p.ShopGroup == country.Text).OrderBy(p => p.Price))
            { choices.Add(p); product.Items.Add(p.Name); }
            if (choices.Count > 0) product.SelectedIndex = Math.Max(0, choices.FindIndex(p => p.Id == previousId));
            RefreshShop();
        }
        private void RefreshShop()
        {
            Product p = Chosen; shopImage.Item = p; shopImage.Invalidate();
            total.Text = p == null ? "暂无商品" : "合计 ¥ " + (p.Price * (int)quantity.Value);
            itemDetails.Text = p == null ? "请选择各国香烟和具体商品。" : p.Rice ? "抓住跑动的大米饭，拖到死亡的小人身上才能复活。" : p.Vape ? "每次 20 口 · 每支 800 口 · 耗尽后丢弃" : "每包 20 支 · " + p.Format + " · 自然抽完 " + (p.Duration / 60) + " 分钟";
        }
        private void SelectBag(string id)
        {
            bagSelection = id; previewOverride = true;
            engine.Notice = id == "rice" ? "已预览大米饭，点击“装备”派到桌面。" : "仅预览，点击“装备”保存装备。";
            RefreshStatus();
        }
        private void ToggleFavorite()
        {
            engine.ToggleFavorite(bagSelection); OnInteraction();
        }
        private void ConfirmBag()
        {
            if (bagSelection == "rice" && !engine.PetVisible)
            {
                engine.Notice = "需要让小人出现在桌面上，请先点击“开始工作”。";
                ShowNoticePopup(engine.Notice);
                RefreshStatus(); return;
            }
            if (bagSelection == "rice")
            {
                if (engine.DeployRice()) SyncDesktop();
            }
            else if (engine.Equip(bagSelection)) previewOverride = false;
            else if (engine.Data.Collapsed) { previewOverride = false; ShowNoticePopup("小人死亡，无法装备"); }
            OnInteraction();
        }
        private void ShowNoticePopup(string message)
        {
            if (popup != null) popup.Close();
            popup = new NoticePopup(message) { StartPosition = FormStartPosition.Manual };
            popup.Location = new Point(Left + (Width - popup.Width) / 2, Top + (Height - popup.Height) / 2);
            popup.Show(this);
        }
        private void RefreshBag()
        {
            List<Stock> all = engine.Backpack();
            int pages = Math.Max(1, (all.Count + 7) / 8); page = Math.Max(0, Math.Min(page, pages - 1));
            List<Stock> visible = engine.BagPage(page);
            for (int i = 0; i < 8; i++)
            {
                Stock s = i < visible.Count ? visible[i] : null;
                slots[i].Item = s == null ? null : Catalog.Get(s.Id);
                slots[i].Description = s == null ? "" : engine.StockDescription(s.Id);
                slots[i].Favorite = s != null && s.Favorite;
                slots[i].Selected = s != null && s.Id == bagSelection;
                slots[i].Equipped = s != null && s.Id == engine.Data.Selected;
                slots[i].Invalidate();
            }
            previous.Enabled = page > 0; next.Enabled = page < pages - 1;
            pageLabel.Text = (page + 1) + " / " + pages;
            Product selected = Catalog.Get(bagSelection);
            selectedLabel.Text = selected == null ? "选择物品进行预览" : "已选：" + selected.Name;
            equip.Enabled = selected != null && engine.Owns(selected.Id);
            Stock selectedStock = engine.FindStock(bagSelection);
            favorite.Enabled = selectedStock != null && bagSelection != "rice" && engine.Owns(bagSelection);
            favorite.Text = selectedStock != null && selectedStock.Favorite ? "取消收藏" : "收藏选中";
        }
        private void ToggleWork()
        {
            engine.ToggleWork(); previewOverride = false; OnInteraction();
        }
        private void EndWork()
        {
            engine.EndWork(); previewOverride = false; OnInteraction();
        }
        private void SyncDesktop()
        {
            if (pet == null) return;
            if (engine.PetVisible) { if (!pet.Visible) pet.Show(); }
            else pet.Hide();
            if (engine.PetVisible && engine.Data.RiceDeployed)
            {
                if (rice == null || rice.IsDisposed) rice = new RiceForm(engine, TryFeedRice, Persist);
                if (!rice.Visible) rice.Show();
            }
            else if (rice != null)
            {
                rice.Close(); rice.Dispose(); rice = null;
            }
        }
        private bool TryFeedRice(Point mousePosition)
        {
            // Only a release over the character's visible body is a successful drop.
            if (!pet.Visible || !pet.HitBody(mousePosition))
            { engine.Notice = "没喂到，再抓住大米饭拖到小人身上。"; RefreshStatus(); return false; }
            bool fed = engine.FeedRice();
            if (fed) previewOverride = false;
            // Close the dragged window after its mouse event has returned.
            BeginInvoke((Action)delegate { OnInteraction(); });
            return fed;
        }
        private void OnInteraction() { if (loading) return; SyncDesktop(); Persist(); RefreshStatus(); }
        private void MinimizeToTaskbar() { ShowInTaskbar = true; WindowState = FormWindowState.Minimized; }
        private void HideToTray() { Hide(); ShowInTaskbar = false; }
        private void ShowSettings() { ShowInTaskbar = true; WindowState = FormWindowState.Normal; Show(); Activate(); RefreshStatus(); }
        private void SpawnDebris(Debris d, bool randomize)
        {
            if (litter.ContainsKey(d.Id)) return;
            if (randomize)
            {
                Point point = DesktopForm.RandomPoint(new Size(72, 42)); d.X = point.X; d.Y = point.Y;
                pet.Throw(d.ProductId);
            }
            DebrisForm form = new DebrisForm(engine, d, delegate
            {
                if (!engine.Data.Litter.Any(item => item.Id == d.Id)) litter.Remove(d.Id);
                OnInteraction();
            });
            litter[d.Id] = form; form.Show();
        }
        private void Tick(object sender, EventArgs e)
        {
            double now = clock.Elapsed.TotalSeconds, elapsed = now - last; last = now;
            if (engine.AdvanceRice(elapsed)) { SyncDesktop(); Persist(); RefreshStatus(); }
            engine.AdvancePresentation(elapsed);
            if (elapsed > 2) elapsed = 0;
            engine.Advance(elapsed); pet.Frame(elapsed); preview.Frame(elapsed);
            if (rice != null && rice.Visible) rice.Frame(elapsed);
            saveElapsed += elapsed; uiElapsed += elapsed;
            if (uiElapsed > 0.25) { SyncDesktop(); RefreshStatus(); uiElapsed = 0; }
            if (saveElapsed > 5) { Persist(); saveElapsed = 0; }
        }
        private void RefreshStatus()
        {
            if (loading) return;
            balance.Text = "钱包：" + ("¥ " + engine.Data.Money);
            workTime.Text = "小人效率：" + engine.EfficiencyStatus + "\n连续工作未摄入尼古丁 " + (engine.Data.ContinuousWorkSeconds / 60).ToString("0.0") + " 分钟";
            work.Text = engine.Working ? "暂停工作" : "开始工作";
            health.Text = "小人状态：" + engine.HealthStatus + " · 20 分钟内 " + engine.RecentCount.ToString("0.00") + " 根";
            health.ForeColor = engine.Data.Collapsed || engine.Coughing ? Color.FromArgb(166, 82, 58) : Theme.Green;
            Product equipped = Catalog.Get(engine.Data.Selected);
            progress.Text = "桌面：" + (engine.PetVisible ? "已出现" : "已离开") + " · 装备：" + (equipped == null ? "无" : equipped.Name);
            preview.Selection = previewOverride ? bagSelection : "";
            preview.Invalidate(); RefreshBag();
        }
        private void Persist()
        {
            try { Storage.Save(savePath, engine.Data); saveFailed = false; }
            catch (Exception ex)
            {
                if (!(ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)) throw;
                if (!saveFailed && tray != null) tray.ShowBalloonTip(3000, "存档未保存", "请检查目录权限或磁盘空间。", ToolTipIcon.Warning);
                saveFailed = true;
            }
        }
        private void ExitApp()
        {
            Persist();
            if (saveFailed && MessageBox.Show("最新进度尚未保存，仍要退出吗？", "存档失败", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            exiting = true; timer.Stop(); tray.Visible = false;
            foreach (DebrisForm f in litter.Values.ToArray()) f.Close();
            if (popup != null) popup.Close();
            if (rice != null) rice.Close(); pet.Close(); Close();
        }
#if UI_TESTS
        private void CapturePreview(string name)
        {
            using (Bitmap image = new Bitmap(content.Width, content.Height))
            {
                // UI tests intentionally freeze the engine clock. Settle only the render transition.
                content.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                preview.Frame(.3); pet.Frame(0); pet.Frame(.3);
                content.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                image.Save(Path.Combine(Path.GetDirectoryName(savePath), name + ".png"));
            }
        }
        private void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
        private void CheckMovingCracks(string directory)
        {
            SceneEffectForm effect = Application.OpenForms.OfType<SceneEffectForm>().Single();
            Point originalLocation = pet.Location; int originalScale = engine.Data.PetScalePercent;
            foreach (int percent in new[] { 50, 100, 200 })
            {
                petSize.Value = percent; float scale = percent / 100f;
                foreach (Screen screen in Screen.AllScreens)
                {
                    Rectangle area = screen.WorkingArea;
                    foreach (Point point in new[] { area.Location, new Point(area.Right - pet.Width, area.Bottom - pet.Height), new Point(area.Left + area.Width / 2, area.Top + area.Height / 2) })
                    {
                        pet.Location = point; // Assert before another timer frame can repair the position.
                        Require(Math.Abs(effect.Left + effect.Width / 2 - pet.Left - AssetArt.SmashImpact.X * scale) <= 1 &&
                            Math.Abs(effect.Top + effect.Height / 2 - pet.Top - AssetArt.SmashImpact.Y * scale) <= 1, "cracks drifted from fist while moving/scaling");
                    }
                }
            }
            petSize.Value = originalScale; pet.Location = originalLocation;
            Require(effect.TransparencyKey.IsEmpty, "cracks still use a colored transparency key");
            using (Bitmap image = effect.RenderEffect())
            {
                int visible = 0;
                for (int y = 0; y < image.Height; y++) for (int x = 0; x < image.Width; x++)
                {
                    Color pixel = image.GetPixel(x, y); if (pixel.A == 0) continue;
                    visible++; Require(pixel.R == pixel.G && pixel.G == pixel.B, "cracks contain a tinted edge");
                }
                Require(visible > 80 && image.GetPixel(0, 0).A == 0, "cracks missing or background opaque");
                image.Save(Path.Combine(directory, "07-gray-cracks.png"));
                string petPath = Path.Combine(directory, "07-smash-pet.png"); pet.CaptureAppearance(petPath);
                using (Bitmap petImage = new Bitmap(petPath)) using (Bitmap composite = new Bitmap(image.Width, image.Height)) using (Graphics g = Graphics.FromImage(composite))
                {
                    g.Clear(Color.FromArgb(45, 49, 57)); g.DrawImageUnscaled(petImage, pet.Left - effect.Left, pet.Top - effect.Top); g.DrawImageUnscaled(image, 0, 0);
                    composite.Save(Path.Combine(directory, "07-smash-alignment.png"));
                }
            }
        }
        private void CheckResponsiveLayout(string directory)
        {
            Size originalSize = ClientSize; Rectangle previewBounds = preview.Bounds;
            float fontSize = work.Font.SizeInPoints;
            for (int i = 0; i < 3; i++)
            {
                WindowState = FormWindowState.Maximized; PerformLayout(); responsiveLayout.Update();
                Require(content.Width >= 1040 && preview.Width >= previewBounds.Width, "maximized layout did not grow");
                Require(Math.Abs(content.Left - Math.Max(0, (viewport.ClientSize.Width - content.Width) / 2)) <= 1, "maximized canvas not centered");
                Require(work.Parent.Bottom <= content.Height && slots[7].Bottom <= content.Height, "maximized bottom controls clipped");
                if (i == 0)
                {
                    CapturePreview("01-maximized-content");
                    using (Bitmap image = new Bitmap(Width, Height))
                    { DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(directory, "01-maximized-window.png")); }
                }
                WindowState = FormWindowState.Normal; ClientSize = originalSize; PerformLayout(); responsiveLayout.Update();
                Require(preview.Bounds == previewBounds && Math.Abs(work.Font.SizeInPoints - fontSize) < .01f, "restore accumulated scale drift");
            }
            ClientSize = new Size(740, 600); PerformLayout(); responsiveLayout.Update();
            Require(viewport.HorizontalScroll.Visible && viewport.VerticalScroll.Visible, "small window lost scrollbars");
            viewport.AutoScrollPosition = new Point(2000, 2000);
            using (Bitmap image = new Bitmap(Width, Height))
            { DrawToBitmap(image, new Rectangle(Point.Empty, image.Size)); image.Save(Path.Combine(directory, "01-small-window-scrolled.png")); }
            viewport.AutoScrollPosition = Point.Empty; ClientSize = originalSize; PerformLayout(); responsiveLayout.Update();
        }
        private void TestCloseChoice(DialogResult result)
        {
            using (Timer responder = new Timer { Interval = 80 })
            {
                responder.Tick += delegate
                {
                    CloseChoiceDialog dialog = Application.OpenForms.OfType<CloseChoiceDialog>().FirstOrDefault();
                    if (dialog == null) return;
                    responder.Stop();
                    using (Bitmap image = new Bitmap(dialog.Width, dialog.Height))
                    {
                        dialog.DrawToBitmap(image, new Rectangle(Point.Empty, image.Size));
                        image.Save(Path.Combine(Path.GetDirectoryName(savePath), "close-choice.png"));
                    }
                    if (result == DialogResult.Cancel) dialog.Close();
                    else dialog.Controls.OfType<Button>().Single(b => b.DialogResult == result).PerformClick();
                };
                responder.Start(); Close();
            }
        }
        private void StartSmokeTest()
        {
            timer.Stop();
            Timer test = new Timer { Interval = 180 };
            int step = 0;
            string directory = Path.GetDirectoryName(savePath); Directory.CreateDirectory(directory);
            test.Tick += delegate
            {
                test.Stop();
                try
                {
                    if (step == 0)
                    {
                        Require(engine.Backpack().Count == 0 && !pet.Visible, "fresh bag/pet not empty");
                        Require(petSize.Value == 100 && petSize.Parent == avatar.Parent && petSize.Top > avatar.Bottom && petSize.AccessibleName == "人物大小", "missing size slider below avatar");
                        petSize.Value = 150;
                        Require(!pet.Visible && pet.ClientSize == new Size(402, 348) && engine.Data.PetScalePercent == 150, "hidden size setting showed pet or failed to resize");
                        petSize.Value = 100;
                        Require(balance.Text.Contains("¥"), "balance display incorrect");
                        Require(content.Controls.OfType<Label>().Any(l => l.Text == "大肥鱼代抽1.0") && !content.Controls.OfType<Button>().Any(b => b.Text == "最小化"), "header/minimize UI not updated");
                        Require(content.Controls.OfType<Panel>().SelectMany(p => p.Controls.OfType<Label>()).Any(l => l.Text == "1  你的代抽"), "avatar section title changed");
                        CheckResponsiveLayout(directory);
                        MinimizeToTaskbar(); Require(Visible && ShowInTaskbar && WindowState == FormWindowState.Minimized, "minimize went to tray");
                        ShowSettings(); TestCloseChoice(DialogResult.Cancel); Require(Visible && !exiting, "cancel close changed window");
                        TestCloseChoice(DialogResult.No); Require(!Visible && !ShowInTaskbar && tray.Visible, "close choice failed to hide to tray"); ShowSettings();
                        CapturePreview("01-empty"); engine.Data.Money = 5000;
                        Require(avatar.Items.Count == 1 && avatar.Text == "大肥鱼" && country.Items.Count == 4, "character/group options incorrect");
                        foreach (string group in new[] { "中国烟", "外国烟", "电子烟", "大米饭" })
                        {
                            country.SelectedItem = group;
                            Require(choices.Count > 0 && choices.All(p => p.ShopGroup == group) && choices.Select(p => p.Price).SequenceEqual(choices.Select(p => p.Price).OrderBy(p => p)), "shop filter or price order incorrect");
                        }
                        country.SelectedItem = "大米饭";
                        Require(Chosen != null && Chosen.Rice, "rice shop filter failed");
                        quantity.Value = 2; Require(total.Text == "合计 ¥ 200", "rice quantity quote incorrect");
                        buy.PerformClick(); Require(engine.StockOf("rice") == 2 && engine.Data.Money == 4800, "shop purchase incorrect");
                        country.SelectedIndex = 0;
                        foreach (Product p in Catalog.All.Where(p => !p.Rice)) engine.Buy(p.Id, 2);
                        RefreshStatus(); Require(engine.Backpack().Count == 25 && slots.Count(s => s.Item != null) == 8 && next.Enabled, "bag pagination missing");
                        next.PerformClick(); Require(slots.Count(s => s.Item != null) == 8 && previous.Enabled, "second page wrong");
                        previous.PerformClick(); SelectBag("liqun"); ConfirmBag(); Require(engine.Data.Selected == "liqun" && !pet.Visible && engine.Pose == "equip", "hidden equip failed");
                        Require(popup == null || !popup.Visible, "equipment still requires pet popup");
                        Require(equip.Text == "装备" && balance.Text.StartsWith("钱包：") && health.Text.Contains("状态良好") && workTime.Text.Contains("效率正常"), "data area labels wrong");
                        CapturePreview("02-hidden-equip");
                        engine.AdvancePresentation(5); RefreshStatus(); Require(engine.Pose == "read", "hidden equip display did not end");
                        work.PerformClick(); Require(pet.Visible && engine.Pose == "read", "start work did not show reading pet");
                        foreach (int percent in new[] { 50, 200 })
                        {
                            petSize.Value = percent; float factor = percent / 100f;
                            Require(pet.Visible && pet.Width == (int)(268 * factor) && pet.Height == (int)(232 * factor), "live slider did not resize window");
                            Require(pet.HitBody(pet.PointToScreen(new Point((int)(135 * factor), (int)(160 * factor)))) && !pet.HitBody(pet.PointToScreen(new Point(0, 0))), "scaled rice hit region incorrect");
                            pet.CaptureAppearance(Path.Combine(directory, "desktop-size-" + percent + ".png"));
                        }
                        Persist(); string sizeWarning; SaveData sizeSave = Storage.Load(savePath, out sizeWarning);
                        Require(sizeWarning == null && sizeSave.PetScalePercent == 200, "slider size was not saved");
                        using (PetForm reopenedPet = new PetForm(new Engine(sizeSave), delegate { }, delegate { }, delegate { }, delegate { }, delegate { }))
                            Require(reopenedPet.Size == pet.Size && reopenedPet.Location == pet.Location, "saved size moved or resized pet on reopen");
                        CapturePreview("02-size-slider");
                        petSize.Value = 100;
                        ConfirmBag(); Require(engine.Data.Selected == "liqun" && engine.Pose == "equip", "equip failed");
                        SelectBag("hehua"); Require(engine.Data.Selected == "liqun" && preview.Selection == "hehua", "preview changed equipment");
                        ToggleFavorite(); Require(engine.Backpack()[0].Id == "rice" && engine.Backpack()[1].Id == "hehua", "favorite priority wrong");
                        sort.SelectedIndex = 1; Require(engine.Backpack()[2].Id == "hetianxia", "price descending UI did not update bag");
                        sort.SelectedIndex = 0;
                        CapturePreview("02-backpack");
                    }
                    else if (step == 1)
                    {
                        SelectBag("liqun"); ConfirmBag();
                        pet.TestSingleClick(); Require(engine.Data.Active == null, "single click started cigarette");
                        pet.TestDoubleClick(); Require(engine.Data.Active != null, "double click failed");
                        engine.Advance(247); engine.AdvancePresentation(1.5); Require(litter.Count == 1 && engine.Pose == "read", "finish did not create litter/read");
                        pet.TestSingleClick(); Require(engine.Data.Active == null, "single click restarted");
                        DebrisForm debris = litter.Values.First();
                        Require(Screen.AllScreens.Any(s => s.WorkingArea.Contains(debris.Bounds)), "debris outside screens");
                        for (int i = 0; i < 5; i++) debris.PerformClean(); Require(litter.Count == 0, "five click cleanup failed");
                        pet.TestDoubleClick(); engine.Advance(5); ToggleWork();
                        Require(engine.Smoking && !engine.Working, "pause interrupted smoke");
                        engine.Advance(242); engine.AdvancePresentation(1.5); Require(engine.Pose == "count" && !engine.Smoking, "paused finish did not count cigarettes");
                        pet.TestDoubleClick(); engine.Advance(5); double before = engine.RecentCount; ToggleWork();
                        Require(!engine.Smoking && engine.Pose == "read" && engine.RecentCount == before + 1, "start work did not discard one");
                        pet.TestDoubleClick(); engine.Advance(5); before = engine.RecentCount; pet.TestShake();
                        Require(!engine.Smoking && engine.RecentCount == before + 0.5, "shake did not discard half");
                        ToggleWork(); Require(engine.Pose == "count" && pet.Visible, "pause equipped state wrong");
                        engine.Unequip(); Require(engine.Pose == "think", "paused unequip did not think");
                        ToggleWork(); ToggleWork(); Require(engine.Pose == "think", "pause empty state wrong");
                        pet.TestDoubleClick(); Require(engine.Notice == "我兜里真没烟", "empty pocket tooltip text wrong");
                        EndWork(); Require(engine.Pose == "close_book" && pet.Visible && !engine.Working, "book-close transition missing");
                        pet.Frame(.3); pet.Frame(.3); CapturePreview("02-close-book");
                        pet.CaptureAppearance(Path.Combine(directory, "desktop-close-book.png"));
                        engine.AdvancePresentation(2); SyncDesktop(); RefreshStatus();
                        CapturePreview("02-off-desktop-sleep");
                        HideToTray(); Require(!pet.Visible && !Visible && tray.Visible, "end/hide reintroduced pet"); ShowSettings();
                    }
                    else if (step == 2)
                    {
                        ToggleWork(); SelectBag("liqun"); ConfirmBag();
                        engine.Data.Health.Clear(); for (int i = 0; i < 9; i++) engine.Data.Health.Add(new HealthEntry { Time = engine.Data.RuntimeSeconds, Amount = 1 });
                        engine.StartSmoking(); engine.Advance(247);
                        Require(engine.Data.Collapsed && engine.Pose == "collapsed", "collapse missing");
                        Require(engine.Data.Selected == "", "death retained equipment");
                        ConfirmBag(); Require(popup != null && popup.Visible && engine.Notice == "小人死亡，无法装备" && engine.Data.Selected == "", "dead equipment popup missing"); popup.Close();
                        RefreshStatus(); CapturePreview("03-collapsed");
                        EndWork(); Require(!pet.Visible && engine.Data.Collapsed, "dead pet did not disappear");
                        SelectBag("rice"); ConfirmBag(); Require(!engine.Data.RiceDeployed, "rice deployed without pet");
                        Require(popup != null && popup.Visible, "missing rice popup");
                        Require(Math.Abs(popup.Left + popup.Width / 2 - Left - Width / 2) <= 1 && Math.Abs(popup.Top + popup.Height / 2 - Top - Height / 2) <= 1, "rice popup not centered"); popup.Close();
                        ToggleWork(); Require(pet.Visible && engine.Pose == "collapsed", "dead pet did not reappear dead");
                        CapturePreview("03-rice-preview");
                        ConfirmBag(); Require(rice != null && rice.Visible, "rice window missing");
                        Point before = rice.Location; for (int i = 0; i < 20; i++) rice.Frame(0.05);
                        Require(rice.Location != before, "rice did not roam");
                        rice.TestGrab(); Point grabbed = rice.Location; rice.Frame(1); Require(rice.Location == grabbed, "grabbed rice moved");
                        rice.TestRelease(new Point(-100000, -100000)); Require(engine.Data.Collapsed && engine.StockOf("rice") == 2, "miss consumed rice");
                        rice.TestGrab(); rice.TestRelease(pet.PointToScreen(new Point(135, 160)));
                        Require(!engine.Data.Collapsed && engine.StockOf("rice") == 1, "rice drop did not revive");
                    }
                    else
                    {
                        SyncDesktop(); RefreshStatus(); Require(rice == null, "rice not removed after consumption");
                        engine.Buy("rice", 2); SelectBag("rice"); ConfirmBag();
                        int bowls = engine.StockOf("rice"); engine.AdvanceRice(20); SyncDesktop();
                        Require(rice == null && engine.StockOf("rice") == bowls - 1, "ungrabbed rice timeout UI wrong");
                        ConfirmBag(); rice.TestGrab(); bowls = engine.StockOf("rice");
                        engine.AdvanceRice(10); SyncDesktop();
                        Require(rice == null && engine.StockOf("rice") == bowls, "grabbed rice timeout UI wrong");
                        HideToTray(); Require(!Visible && pet.Visible, "tray hide removed working pet"); ShowSettings();
                        Persist(); Require(!saveFailed, "save failed");
                        Require(engine.Pose == "revive", "revival animation missing");
                        pet.Frame(.3); pet.Frame(.3); CapturePreview("04-revived");
                        pet.CaptureAppearance(Path.Combine(directory, "desktop-revived.png"));
                        engine.AdvancePresentation(3);
                        foreach (string effectId in new[] { "liqun", "hehua" })
                        {
                            SelectBag(effectId); ConfirmBag(); pet.Frame(1); pet.TestDoubleClick(); engine.Advance(7);
                            for (int frame = 0; frame < 200 && !(engine.PriceCoughing || engine.LightBulb); frame++) engine.AdvancePresentation(0.05);
                            Require(effectId == "liqun" ? engine.PriceCoughing : engine.LightBulb && !engine.HeartEyes, "price animation missing");
                            RefreshStatus(); CapturePreview(effectId == "liqun" ? "05-price-cough" : "05-price-bulb");
                            engine.Advance(Catalog.Get(effectId).Duration / 2 - engine.Data.Active.Burned);
                            Require(engine.Data.Active.HalfwayEffectTriggered && (effectId == "liqun" ? engine.PriceCoughing : engine.LightBulb), "halfway effect not connected to UI");
                            pet.Frame(.03); preview.Frame(.03); RefreshStatus(); CapturePreview("05-halfway-" + effectId);
                            pet.CaptureAppearance(Path.Combine(directory, "desktop-halfway-" + effectId + ".png"));
                            EndWork(); ToggleWork();
                        }
                        SelectBag("daguanyuan"); ConfirmBag(); pet.Frame(1); pet.TestDoubleClick(); engine.Advance(7);
                        engine.Data.Active.PuffElapsed = 0;
                        pet.Frame(0); pet.CaptureAppearance(Path.Combine(directory, "desktop-inhale.png"));
                        engine.Advance(5.6); pet.Frame(.6);
                        Require(engine.Exhaling && pet.Height == 232, "desktop breath/bar state wrong");
                        pet.CaptureAppearance(Path.Combine(directory, "desktop-exhale.png"));
                        EndWork(); ToggleWork();
                        SelectBag("duckbill"); ConfirmBag(); pet.Frame(2);
                        page = engine.Backpack().FindIndex(s => s.Id == "duckbill") / 8; RefreshBag();
                        Require(slots.Any(s => s.Item != null && s.Item.Id == "duckbill" && s.Description.Contains("800 口")), "unopened vape UI shows zero puffs");
                        int trashBefore = litter.Count;
                        pet.TestDoubleClick(); engine.Advance(8); RefreshStatus();
                        Require(slots.Any(s => s.Item != null && s.Item.Id == "duckbill" && s.Description.Contains("799 口")), "live vape backpack UI stale");
                        engine.Advance(114); RefreshStatus();
                        Require(engine.Pose == "pocket" && litter.Count == trashBefore && engine.FindStock("duckbill").VapeRemaining == 780, "20-puff UI batch dropped device");
                        engine.AdvancePresentation(.8); pet.Frame(.8); preview.Frame(.8); CapturePreview("08-vape-pocket");
                        pet.CaptureAppearance(Path.Combine(directory, "desktop-vape-pocket.png"));
                        engine.AdvancePresentation(.7); Require(engine.Pose == "read", "vape pocket never ended");
                        engine.FindStock("duckbill").VapeRemaining = 1;
                        pet.TestDoubleClick(); engine.Advance(8); RefreshStatus();
                        Debris lastDevice = engine.Data.Litter.Last();
                        Require(lastDevice.Vape && lastDevice.ProductId == "duckbill" && litter.Count == trashBefore + 1, "exhausted vape debris identity missing");
                        DebrisForm deviceForm = litter[lastDevice.Id];
                        using (Bitmap device = new Bitmap(deviceForm.Width, deviceForm.Height))
                        {
                            deviceForm.DrawToBitmap(device, new Rectangle(Point.Empty, device.Size));
                            Require(device.GetPixel(4, 20).ToArgb() == Color.FromArgb(255, 0, 255).ToArgb(), "debris background circle still present");
                            device.Save(Path.Combine(directory, "empty-vape-debris.png"));
                        }
                        EndWork(); ToggleWork();
                        SelectBag("liqun"); ConfirmBag(); engine.AdvancePresentation(5); RefreshStatus();
                        Require(engine.Pose == "read" && preview.Selection == "", "equipped preview did not return to reading");
                        engine.Advance(1800); pet.Frame(0.05); RefreshStatus();
                        Require(engine.Pose == "crave" && health.Text.Contains("0.00"), "30 minute state/precision"); CapturePreview("05-crave");
                        foreach (string action in new[] { "walk", "sleep" })
                        {
                            engine.Data.FatigueAction = action; engine.Data.FatigueSeconds = 15; pet.Frame(0.5); RefreshStatus(); CapturePreview("06-" + action);
                        }
                        Require(!Application.OpenForms.OfType<SceneEffectForm>().Any(), "ground book did not clear");
                        engine.Advance(1500); pet.Frame(0.05); Require(engine.Data.FatigueAction == "", "55 minute action leak");
                        engine.Advance(300); pet.Frame(0.05); RefreshStatus(); CapturePreview("07-smash");
                        Require(Application.OpenForms.OfType<SceneEffectForm>().Any(), "screen crack missing");
                        CheckMovingCracks(directory);
                        engine.Advance(20); pet.Frame(0.05); Require(!Application.OpenForms.OfType<SceneEffectForm>().Any(), "screen crack not cleared");
                        Persist(); Require(!saveFailed, "new state save failed");
                        File.WriteAllText(Path.Combine(directory, "smoke-test-ok.txt"), "PASS: window controls, hidden equipment, 5s preview, centered rice popup, smoke transitions, shake, rice deadlines, bag/shop, debris/revival, fatigue actions and crack cleanup, persistence.");
                        test.Dispose(); TestCloseChoice(DialogResult.Yes); return;
                    }
                    step++;
                    test.Start();
                }
                catch (Exception ex)
                {
                    File.WriteAllText(Path.Combine(directory, "smoke-test-error.txt"), ex.ToString()); Environment.ExitCode = 1;
                    test.Stop(); test.Dispose(); ExitApp();
                }
            };
            test.Start();
        }
#endif
        protected override void Dispose(bool disposing)
        {
            if (disposing && responsiveLayout != null) responsiveLayout.Dispose();
            if (disposing) { timer.Dispose(); if (tray != null) tray.Dispose(); if (pet != null) pet.Dispose(); if (rice != null) rice.Dispose(); if (popup != null) popup.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
