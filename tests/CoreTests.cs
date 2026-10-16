using System;
using System.IO;
using System.Linq;
using SmokeSimulator;

internal static class CoreTests
{
    private static int passed;
    private static void Assert(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static Engine Ready(string id)
    {
        Engine e = new Engine(new SaveData { Money = 10000 }); e.ToggleWork(); e.Buy(id); e.Equip(id); return e;
    }
    // Existing rule tests measure lit/breathing time; preparation itself is tested separately.
    private static bool Start(Engine e)
    {
        bool started = e.StartSmoking();
        if (started && e.Data.Active.Age < e.PreparationSeconds)
            e.Advance(e.PreparationSeconds - e.Data.Active.Age);
        return started;
    }
    private static void SmokeFast(Engine e)
    {
        // Sequential smoking scenarios wait out the mandatory drunk animation.
        WaitUntilSober(e);
        Assert(Start(e), "cannot start");
        for (int i = 0; i < 120; i++) { e.ClickPet(); e.Advance(0.5); }
    }
    private static void WaitUntilSober(Engine e)
    {
        if (e.DrunkSeconds > 0) { double wait = e.DrunkSeconds; e.Advance(wait); e.AdvancePresentation(wait); }
    }
    private static int Main()
    {
        try
        {
            Check("drunk entrance is a single twenty-second pose without persistent coughing", delegate
            {
                Engine e = Ready("marlboro_red");
                for (int i = 0; i < 5; i++) SmokeFast(e);
                Assert(e.DrunkSeconds == 0 && !e.CoughAnimation, "five triggered drunk pose");
                SmokeFast(e); Assert(e.DrunkSeconds == 20 && e.Pose == "drunk" && !e.CoughAnimation, "drunk entrance missing or cough persists");
                e.AdvancePresentation(7); Assert(!e.StartSmoking(), "smoked while drunk");
                Assert(e.DrunkSeconds == 13, "blocked smoking restarted drunk pose");
                e.AdvancePresentation(12.9); Assert(e.Pose == "drunk", "drunk ended early");
                e.AdvancePresentation(.1); Assert(e.DrunkSeconds == 0 && e.Pose == "read" && !e.CoughAnimation, "drunk pose continued indefinitely");
                e.Advance(600);
                for (int i = 0; i < 6; i++) SmokeFast(e);
                Assert(e.DrunkSeconds == 20, "new threshold crossing did not animate");
                for (int i = 0; i < 4; i++) SmokeFast(e);
                Assert(e.DrunkSeconds == 0 && e.Pose == "collapsed", "drunk pose masks death");
            });
            Check("drunk lock blocks cigarettes and vapes in work and pause until twenty seconds expire", delegate
            {
                foreach (string id in new[] { "marlboro_red", "duckbill" })
                foreach (bool paused in new[] { false, true })
                {
                    Engine e = Ready("marlboro_red");
                    for (int i = 0; i < 6; i++) SmokeFast(e);
                    if (id != "marlboro_red") e.Buy(id);
                    e.Equip(id); if (paused) e.ToggleWork();
                    int stock = e.StockOf(id), puffs = e.FindStock(id).VapeRemaining;
                    double count = e.RecentCount;
                    Assert(!e.StartSmoking() && e.Notice.Contains("醉醺醺") && !e.ClickPet(), "drunk start or boost accepted");
                    Assert(e.Data.Active == null && e.StockOf(id) == stock && e.FindStock(id).VapeRemaining == puffs && e.RecentCount == count, "blocked attempt consumed stock or changed health");
                    e.AdvancePresentation(19.9); Assert(!e.StartSmoking(), "lock ended early");
                    e.AdvancePresentation(.1); Assert(e.StartSmoking(), "lock did not end at twenty seconds");
                }
            });
            Check("drunk vape pauses puffs without consuming stock and resumes after the animation", delegate
            {
                Engine e = Ready("duckbill");
                e.Data.Health.Add(new HealthEntry { Time = e.Data.RuntimeSeconds, Amount = 5 });
                Start(e); e.Advance(6);
                Assert(e.DrunkSeconds == 20 && e.Pose == "drunk" && e.Data.Active.Puffs == 1, "vape did not trigger drunk");
                double age = e.Data.Active.Age;
                Assert(!e.ClickPet() && !e.StartSmoking(), "drunk vape accepted input");
                e.Advance(20);
                Assert(e.Data.Active.Age == age && e.Data.Active.Puffs == 1 && e.FindStock("duckbill").VapeRemaining == 799 && e.RecentCount == 5.05, "vape kept smoking while drunk");
                e.AdvancePresentation(20); e.Advance(6);
                Assert(e.Data.Active.Puffs == 2 && e.FindStock("duckbill").VapeRemaining == 798, "vape did not resume");
            });
            Check("work toggles and ending work cannot skip the drunk lock", delegate
            {
                Engine e = Ready("marlboro_red");
                for (int i = 0; i < 6; i++) SmokeFast(e);
                e.ToggleWork(); e.ToggleWork(); Assert(!e.StartSmoking(), "pause and resume skipped lock");
                e.EndWork(); e.AdvancePresentation(2); e.ToggleWork();
                Assert(e.DrunkSeconds == 18 && !e.StartSmoking(), "end and restart skipped lock");
                e.AdvancePresentation(18); Assert(e.StartSmoking(), "restart prevented recovery");
            });
            Check("ending idle work closes the book for two seconds and stops pay immediately", delegate
            {
                Engine e = Ready("liqun"); e.AdvancePresentation(5); int money = e.Data.Money;
                e.EndWork(); Assert(!e.Working && e.PetVisible && e.Pose == "close_book", "close-book entrance missing");
                Assert(!e.StartSmoking(), "smoking interrupted shutdown");
                e.Advance(20); Assert(e.Data.Money == money, "closing still paid wages");
                e.AdvancePresentation(1.9); e.EndWork(); Assert(e.PetVisible && e.ClosingSeconds == .1, "repeated end extended transition");
                e.AdvancePresentation(.1); Assert(!e.PetVisible && e.ClosingSeconds == 0, "closing failed to hide");
                e.ToggleWork(); e.EndWork(); e.ToggleWork(); e.AdvancePresentation(3);
                Assert(e.Working && e.PetVisible && e.Pose == "read", "restart retained scheduled hide");
            });
            Check("rice and automatic revival show pose twenty-three for three seconds", delegate
            {
                Engine e = Ready("liqun"); for (int i = 0; i < 10; i++) SmokeFast(e);
                e.Buy("rice"); e.DeployRice(); e.GrabRice();
                Assert(e.FeedRice() && e.Pose == "revive" && e.ReviveSeconds == 3, "rice revival pose missing");
                e.AdvancePresentation(2.9); Assert(e.Pose == "revive", "revival too short");
                e.AdvancePresentation(.1); Assert(e.Pose == "read", "revival did not return to read");
                DateTime now = DateTime.UtcNow;
                e = new Engine(new SaveData { Collapsed = true, ReviveAtUtcTicks = now.AddSeconds(1).Ticks }, delegate { return now; });
                now = now.AddSeconds(1); e.CheckRecovery();
                Assert(e.Pose == "revive" && !e.PetVisible, "hidden auto revival missing or reopened desktop");
                e.AdvancePresentation(3); Assert(e.Pose == "read", "auto revival stuck");
            });
            Check("desktop size persists; old and invalid size preferences keep inventory", delegate
            {
                WithSave(delegate(string path)
                {
                    Engine e = Ready("liqun"); e.Data.PetScalePercent = 175; Storage.Save(path, e.Data);
                    string warning; SaveData saved = Storage.Load(path, out warning);
                    Assert(warning == null && saved.PetScalePercent == 175 && saved.Stocks.Single().Count == 20, "size preference did not roundtrip");
                    File.WriteAllText(path, File.ReadAllText(path).Replace("<PetScalePercent>175</PetScalePercent>", ""));
                    saved = Storage.Load(path, out warning);
                    Assert(warning == null && saved.PetScalePercent == 100 && saved.Money == e.Data.Money, "old save reset wallet/size");
                    saved.PetScalePercent = 999; Storage.Save(path, saved); saved = Storage.Load(path, out warning);
                    Assert(warning == null && saved.PetScalePercent == 100 && saved.Stocks.Single().Count == 20, "invalid size erased inventory");
                });
            });
            Check("preparation is ordered, cannot be click-skipped, and never consumes burn time or puffs", delegate
            {
                Engine e = Ready("marlboro_red"); e.StartSmoking(); int clouds = 0; e.Puffed += delegate { clouds++; };
                string[] states = { "take", "place", "stow_pack", "lighter_out", "light", "stow_lighter", "smoke" };
                double[] times = { 0, 1.5, 3, 4, 5, 6, 7 };
                for (int i = 0; i < times.Length; i++)
                {
                    if (i > 0) { e.ClickPet(); e.Advance(times[i] - times[i - 1]); }
                    Assert(e.Pose == states[i] && e.Data.Active.Burned == 0 && clouds == 0, "skipped preparation or lit too early: " + e.Pose);
                }
                e = Ready("duckbill"); e.StartSmoking(); e.Advance(1.99);
                Assert(e.Pose == "vape_raise" && e.FindStock("duckbill").VapeRemaining == 800, "vape puff before raising");
                e.Advance(.01); Assert(e.Pose == "smoke", "vape never reached mouth");
            });
            Check("unopened vape description shows capacity and live description follows every puff", delegate
            {
                Engine e = Ready("duckbill"); e.Buy("duckbill");
                Assert(e.StockDescription("duckbill").Contains("未开 2 支") && e.StockDescription("duckbill").Contains("800 口"), "unopened vape shown empty");
                Start(e);
                for (int i = 1; i <= 20; i++)
                {
                    e.Advance(6);
                    Assert(e.FindStock("duckbill").VapeRemaining == 800 - i && e.StockDescription("duckbill").Contains("本支 " + (800 - i) + " 口") && e.StockDescription("duckbill").Contains("备用 1 支"), "live backpack puff count stale");
                }
                Assert(e.Pose == "pocket" && e.Data.Litter.Count == 0, "round end dropped device instead of pocketing");
                e.AdvancePresentation(1.5); Assert(e.Pose == "read", "pocket animation never returned to book");
            });
            Check("all forty vape rounds consume exactly 800 puffs and only final round drops the same device", delegate
            {
                Engine e = Ready("lost_mary");
                for (int round = 1; round <= 40; round++)
                {
                    Assert(Start(e), "cannot begin round " + round);
                    for (int puff = 0; puff < 20; puff++)
                    {
                        WaitUntilSober(e);
                        e.Advance(6);
                    }
                    Assert(e.FindStock("lost_mary").VapeRemaining == 800 - round * 20, "capacity drift");
                    Assert(e.Data.Litter.Count == (round == 40 ? 1 : 0), "premature or duplicate device drop");
                    if (round < 40) Assert(e.FinishSeconds > 0, "missing pocket action");
                    e.AdvancePresentation(1.5);
                    if (round % 8 == 0) { e.ToggleWork(); e.Advance(600); e.ToggleWork(); }
                }
                Assert(e.Data.Litter.Single().Vape && e.Data.Litter.Single().ProductId == "lost_mary" && !e.Owns("lost_mary"), "wrong exhausted device model or phantom stock");
            });
            Check("partial vape round survives reopening with the same mouthful balance", delegate
            {
                WithSave(delegate(string path)
                {
                    Engine e = Ready("duckbill"); Start(e); e.Advance(17 * 6 + 3);
                    Storage.Save(path, e.Data); string warning;
                    Engine reopened = new Engine(Storage.Load(path, out warning)); reopened.ToggleWork(); reopened.StartSmoking();
                    Assert(warning == null && reopened.FindStock("duckbill").VapeRemaining == 783 && reopened.Data.Active.Puffs == 17, "reload reset device/round count");
                    reopened.Advance(15);
                    Assert(!reopened.Smoking && reopened.FindStock("duckbill").VapeRemaining == 780 && reopened.Data.Litter.Count == 0, "resumed round consumed wrong count");
                });
            });
            Check("halfway effects fire once at burned half, including accelerated smoking", delegate
            {
                foreach (string id in new[] { "liqun", "hehua", "lucky_strike", "esse_watermelon" })
                {
                    Engine e = Ready(id); Start(e); double half = Catalog.Get(id).Duration / 2;
                    e.Advance(half - .1); Assert(!e.Data.Active.HalfwayEffectTriggered, "early midpoint");
                    e.ClickPet(); e.Advance(.05);
                    Assert(e.Data.Active.HalfwayEffectTriggered, "accelerated midpoint skipped");
                    bool cheap = Catalog.Get(id).Price < 20;
                    Assert(cheap ? e.PriceCoughing : e.LightBulb, "midpoint effect missing");
                    double duration = cheap ? 2 : 5;
                    Assert(e.PriceEffectRemaining == duration, "midpoint effect length");
                    e.AdvancePresentation(duration - .05); Assert(cheap ? e.PriceCoughing : e.LightBulb, "effect ended early");
                    e.AdvancePresentation(.05); Assert(!e.PriceCoughing && !e.LightBulb, "effect not ended");
                    e.Advance(1); Assert(e.PriceEffectRemaining == 0, "midpoint fired twice");
                }
                foreach (string id in new[] { "marlboro_red", "black_devil_vanilla", "kent" })
                { Engine e = Ready(id); Start(e); e.Advance(Catalog.Get(id).Duration / 2); Assert(!e.PriceCoughing && !e.LightBulb, "middle price gained effect"); }
            });
            Check("vape halfway is tenth puff and each new batch gets its own effect", delegate
            {
                foreach (string id in new[] { "relx5", "lost_mary" })
                {
                    Engine e = Ready(id); Start(e); e.Advance(59.9);
                    Assert(e.Data.Active.Puffs == 9 && !e.Data.Active.HalfwayEffectTriggered, "early vape midpoint");
                    e.Advance(.1); Assert(e.Data.Active.Puffs == 10 && e.Data.Active.HalfwayEffectTriggered, "tenth puff missed");
                    Assert(id == "relx5" ? e.PriceCoughing : e.LightBulb, "vape midpoint effect absent");
                    e.Advance(60); Start(e); Assert(!e.Data.Active.HalfwayEffectTriggered, "new batch inherited marker");
                    e.Advance(60); Assert(id == "relx5" ? e.PriceCoughing : e.LightBulb, "next batch effect absent");
                }
            });
            Check("halfway marker survives restart and does not repeat", delegate
            {
                WithSave(delegate(string path)
                {
                    Engine e = Ready("hehua"); Start(e); e.Advance(120);
                    Storage.Save(path, e.Data); string warning; SaveData data = Storage.Load(path, out warning);
                    Assert(warning == null && data.Active.HalfwayEffectTriggered, "midpoint marker lost");
                    Engine resumed = new Engine(data); resumed.ToggleWork(); Start(resumed); resumed.Advance(.1);
                    Assert(!resumed.LightBulb, "midpoint replayed after restart");
                });
            });
            Check("one exhalation per breath, none during inhale, faster clicks accelerate both vape and cigarette cycles", delegate
            {
                foreach (string id in new[] { "marlboro_red", "duckbill" })
                {
                    Engine e = Ready(id); Start(e); int emitted = 0; e.Puffed += delegate { emitted++; };
                    // Preparation completed by Start; reset to a known cycle boundary
                    e.Data.Active.PuffElapsed = 0;
                    double cycle = e.BreathSeconds;
                    e.Advance(cycle * .5); Assert(!e.Exhaling && emitted == 0, "smoke during inhale");
                    e.Advance(cycle * .1); Assert(e.Exhaling && emitted == 1, "exhale start missing");
                    e.Advance(cycle * .4); Assert(!e.Exhaling && emitted == 1, "duplicate exhale");
                    e.Advance(cycle); Assert(emitted == 2, "next breath not emitted");
                    int before = emitted;
                    for (int i = 0; i < 30; i++) { e.ClickPet(); e.Advance(.05); }
                    Assert(emitted > before, "clicked cycle did not accelerate");
                }
            });
            Check("discarded vape keeps its product identity through save and reload", delegate
            {
                Engine e = Ready("lost_mary"); Start(e); e.Advance(4); e.DiscardSmoking();
                Assert(e.Data.Litter.Single().Vape && e.Data.Litter.Single().ProductId == "lost_mary", "discarded wrong device");
                string dir = Path.Combine(Path.GetTempPath(), "smoke-debris-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "save.xml");
                try { Storage.Save(path, e.Data); string warning; SaveData restored = Storage.Load(path, out warning); Assert(warning == null && restored.Litter.Single().ProductId == "lost_mary", "device identity lost"); }
                finally { Directory.Delete(dir, true); }
            });
            Check("Super King Size lasts seven minutes and preserves halfway discard accounting", delegate
            {
                Assert(Catalog.Get("lucky_strike").Format == "Super King Size", "wrong format");
                Assert(!Catalog.All.Any(p => p.Format == "King Size"), "retired format in shop");
                Engine e = Ready("lucky_strike"); Start(e); e.Advance(419.9);
                Assert(e.Smoking, "superking completed early"); e.Advance(.1);
                Assert(!e.Smoking && e.RecentCount == 1, "superking did not finish at seven minutes");
                e = Ready("lucky_strike"); Start(e); e.Advance(210); e.DiscardSmoking();
                Assert(e.RecentCount == .5, "halfway discard incorrect");
            });
            Check("new save has empty backpack and no equipped box", delegate
            {
                Engine e = new Engine(new SaveData());
                Assert(e.Backpack().Count == 0 && e.Data.Selected == "" && !e.PetVisible && !e.Working, "not fresh");
                Assert(e.BagPage(12).Count == 0, "empty page invalid");
            });
            Check("new wallet has 200; work pays every twenty seconds and preserves remainder", delegate
            {
                Engine e = new Engine(new SaveData()); e.Advance(120); Assert(e.Data.Money == 200, "initial wallet or idle income");
                e.ToggleWork(); e.Advance(19.9); Assert(e.Data.Money == 200, "early payment");
                e.Advance(0.1); Assert(e.Data.Money == 201, "twenty second payment missing");
                e.Advance(10); e.ToggleWork(); e.Advance(100); Assert(e.Data.Money == 201, "paused paid");
                e.EndWork(); e.Advance(100); e.ToggleWork(); e.Advance(10); Assert(e.Data.Money == 202, "remainder lost");
            });
            Check("fresh startup funds persist and cannot be collected again after spending", delegate
            {
                WithSave(delegate(string path)
                {
                    string warning; SaveData fresh = Storage.Load(path, out warning);
                    Assert(warning == null && fresh.Money == 200 && fresh.StartupFunds200Applied, "fresh startup funds missing");
                    Engine e = new Engine(fresh); Assert(e.Buy("rice", 2) && e.Data.Money == 0, "spending starter funds failed");
                    Storage.Save(path, e.Data);
                    SaveData reopened = Storage.Load(path, out warning);
                    Assert(warning == null && reopened.Money == 0 && reopened.Stocks.Single().Count == 2, "restart duplicated startup funds");
                });
            });
            Check("legacy zero wallet receives 200 once without losing progress", delegate
            {
                WithSave(delegate(string path)
                {
                    SaveData old = new SaveData { Money = 0, WorkSeconds = 123, RuntimeSeconds = 100, HasWorked = true };
                    old.Stocks.Add(new Stock { Id = "liqun", Count = 17, Favorite = true });
                    old.Health.Add(new HealthEntry { Time = 90, Amount = 0.5 });
                    Storage.Save(path, old);
                    File.WriteAllText(path, File.ReadAllText(path).Replace("<StartupFunds200Applied>false</StartupFunds200Applied>", ""));
                    string warning;
                    SaveData upgraded = Storage.Load(path, out warning);
                    Assert(warning == null && upgraded.Money == 200 && upgraded.StartupFunds200Applied, "legacy zero wallet was not funded");
                    Assert(upgraded.WorkSeconds == 123 && upgraded.HasWorked && upgraded.Stocks.Single().Count == 17 && upgraded.Stocks[0].Favorite && upgraded.Health.Single().Amount == 0.5, "upgrade lost progress");
                    upgraded.Money = 0; Storage.Save(path, upgraded);
                    Assert(Storage.Load(path, out warning).Money == 0, "legacy top-up repeated");
                });
            });
            Check("legacy nonzero balances are preserved and not topped up on later restarts", delegate
            {
                WithSave(delegate(string path)
                {
                    foreach (int money in new[] { 7, 100, 350 })
                    {
                        Storage.Save(path, new SaveData { Money = money });
                        string warning; SaveData upgraded = Storage.Load(path, out warning);
                        Assert(warning == null && upgraded.Money == money && upgraded.StartupFunds200Applied, "existing funds changed");
                        upgraded.Money = 0; Storage.Save(path, upgraded);
                        Assert(Storage.Load(path, out warning).Money == 0, "spent existing balance was refilled");
                    }
                });
            });
            Check("normal purchases require funds and deduct the full cost", delegate
            {
                SaveData data = new SaveData { Money = 0 }; Engine normal = new Engine(data);
                Assert(!normal.Buy("rice") && normal.StockOf("rice") == 0, "insufficient funds accepted");
                data.Money = 100; Assert(normal.Buy("rice") && data.Money == 0, "purchase did not charge");
            });
            Check("quantity purchase is atomic and stacks cigarettes by pack", delegate
            {
                Engine e = new Engine(new SaveData { Money = 56 });
                Assert(!e.Buy("liqun", 3) && e.StockOf("liqun") == 0 && e.Data.Money == 56, "failed purchase mutated");
                e.Data.Money = 100; Assert(e.Buy("liqun", 3) && e.Data.Money == 43 && e.StockOf("liqun") == 60, "quantity cost");
                Assert(e.Buy("liqun", 1) && e.Backpack().Count == 1 && e.StockOf("liqun") == 80, "not stacked");
                e.ToggleWork(); e.Equip("liqun"); Start(e);
                Assert(e.StockDescription("liqun").Contains("整包 3 包") && e.StockDescription("liqun").Contains("已开 19 支"), "opened pack count");
                int balance = e.Data.Money; Assert(!e.Buy("liqun", 0) && !e.Buy("liqun", 1000) && e.Data.Money == balance, "invalid quantity accepted");
            });
            Check("rice first, favorites next, bidirectional price sort and eight-slot pages", delegate
            {
                Engine e = new Engine(new SaveData { Money = 10000 });
                foreach (Product p in Catalog.All) e.Buy(p.Id);
                e.ToggleFavorite("hehua");
                var bag = e.Backpack(); Assert(bag[0].Id == "rice" && bag[1].Id == "hehua" && bag[2].Id == "hongmei", "ascending priority");
                e.Data.PriceDescending = true; bag = e.Backpack();
                Assert(bag[0].Id == "rice" && bag[1].Id == "hehua" && bag[2].Id == "hetianxia", "descending priority");
                Assert(e.BagPage(0).Count == 8 && e.BagPage(1).Count == 8 && e.BagPage(100).Count == 1 && e.Backpack().Count == 25, "pagination");
                e.ToggleFavorite("hehua"); Assert(e.Backpack()[1].Id == "hetianxia", "unfavorite ordering");
            });
            Check("paused equip returns to counting, unequip returns to thinking, working and hidden still read", delegate
            {
                Engine e = Ready("liqun"); e.ToggleWork(); e.Unequip();
                Assert(e.Pose == "think" && !e.Working, "empty pause did not think");
                e.Equip("liqun"); e.AdvancePresentation(4.9); Assert(e.Pose == "equip", "paused display too short");
                e.AdvancePresentation(.1); Assert(e.Pose == "count" && !e.Working, "paused equip returned to reading");
                e.Unequip(); Assert(e.Pose == "think", "paused unequip returned to reading");
                e.Equip("liqun"); e.ToggleWork(); e.AdvancePresentation(5); Assert(e.Pose == "read", "resume kept paused pose");
                e.Unequip(); Assert(e.Pose == "read", "working unequip did not read");
                e.Equip("liqun"); e.ToggleWork(); e.AdvancePresentation(5); Assert(e.Pose == "count", "pause during display lost state");
                e.EndWork(); e.AdvancePresentation(2); e.Unequip(); e.Equip("liqun"); e.AdvancePresentation(5);
                Assert(!e.PetVisible && e.Pose == "read", "hidden equip showed paused pose");
            });
            Check("hidden equipment previews immediately; visible equipment lasts five seconds", delegate
            {
                Engine e = new Engine(new SaveData { Money = 200 }); e.Buy("liqun");
                Assert(e.Data.Selected == "" && e.Equip("liqun") && !e.PetVisible && e.Pose == "equip", "hidden equipment failed");
                e.AdvancePresentation(4.9); Assert(e.Pose == "equip", "hidden display too short");
                e.AdvancePresentation(0.1); Assert(e.Pose == "read", "hidden display did not finish");
                e.ToggleWork(); Assert(e.Pose == "read", "start did not read");
                Assert(e.Equip("liqun") && e.Data.Selected == "liqun", "equip failed");
                e.AdvancePresentation(4.9); Assert(e.Pose == "equip", "display too short");
                e.AdvancePresentation(0.1); Assert(e.Pose == "read", "display did not finish");
                e.Buy("hehua"); Assert(e.Data.Selected == "liqun", "shop changed equipment");
                Assert(!e.Equip("rice") && !e.Equip("vape"), "equipped invalid/unowned");
                e.EndWork(); Assert(e.Equip("hehua") && e.Data.Selected == "hehua", "hidden equip failed");
                e.ToggleWork(); e.Advance(1800); Assert(e.Pose == "crave", "fatigue setup");
                e.Equip("liqun"); e.AdvancePresentation(5); Assert(e.Pose == "read", "equipment left stale fatigue action");
                e.Data.FatigueAction = "sleep"; e.Data.FatigueSeconds = 10;
                Engine reopened = new Engine(e.Data); reopened.ToggleWork(); Assert(reopened.Pose == "read", "restart did not start reading");
            });
            Check("work, pause count/think, unequip think, end hide", delegate
            {
                Engine e = Ready("liqun"); e.ToggleWork(); Assert(e.Pose == "count" && e.PetVisible, "paused equipped pose");
                e.Unequip(); Assert(e.Pose == "think" && e.Data.Selected == "", "paused unequip pose");
                e.ToggleWork(); Assert(e.Pose == "read", "work read");
                e.ToggleWork(); Assert(e.Pose == "think", "empty pause thought");
                e.EndWork(); e.AdvancePresentation(2); Assert(!e.PetVisible && !e.Working, "end work visibility");
            });
            foreach (string id in new[] { "liqun", "mevius", "parliament" })
            {
                string productId = id;
                Check("double-click session required; natural duration " + id, delegate
                {
                    Engine e = Ready(productId);
                    Assert(!e.ClickPet() && e.Data.Active == null && e.StockOf(productId) == 20, "single click started");
                    Assert(Start(e), "start failed"); e.Advance(Catalog.Get(productId).Duration - 0.1);
                    Assert(e.Data.Active != null, "finished early"); e.Advance(0.1); e.AdvancePresentation(1.5);
                    Assert(e.Data.Active == null && e.Data.Litter.Count == 1 && e.Pose == "read", "finish state");
                    for (int i = 0; i < 20; i++) e.ClickPet();
                    Assert(e.Data.Active == null && e.StockOf(productId) == 19, "click restarted after finish");
                });
            }
            Check("continuous clicks cap at four times and coarse cigarette takes 60 seconds", delegate
            {
                Engine e = Ready("liqun"); Start(e);
                for (int i = 0; i < 1000; i++) e.ClickPet(); Assert(e.Data.Active.Burned == 0, "click spam burned");
                for (int i = 0; i < 119; i++) { e.ClickPet(); e.Advance(0.5); }
                Assert(e.Data.Active != null, "finished early"); e.ClickPet(); e.Advance(0.5);
                Assert(e.Data.Active == null && e.Data.Completed == 1, "wrong accelerated duration");
            });
            Check("pausing work keeps smoking until finished, then shows paused pose", delegate
            {
                Engine e = Ready("liqun"); Start(e); e.Advance(10); e.ToggleWork();
                int money = e.Data.Money; e.Advance(100);
                Assert(e.Smoking && Math.Abs(e.Data.Active.Burned - 110) < 0.001 && e.Data.Money == money, "pause stopped smoking or paid income");
                e.Advance(130); e.AdvancePresentation(1.5); Assert(e.Data.Active == null && e.Pose == "count" && e.RecentCount == 1, "wrong paused finish pose");
                Assert(Start(e) && e.Smoking, "paused mode cannot start smoke");
            });
            Check("starting work during paused smoking discards for one and reads", delegate
            {
                Engine e = Ready("liqun"); e.ToggleWork(); Start(e); e.Advance(10); e.ToggleWork();
                Assert(e.Working && e.Data.Active == null && e.Pose == "read" && e.Data.Litter.Count == 1 && e.RecentCount == 1, "resume did not discard one");
                e.Advance(60); Assert(e.Data.Litter.Count == 1 && e.RecentCount == 1, "discard counted twice");
            });
            Check("end work discards active session and next smoke uses a new stick", delegate
            {
                Engine e = Ready("liqun"); Start(e); e.Advance(10);
                e.EndWork(); e.Advance(100); Assert(!Start(e), "hidden smoke resumed");
                e.ToggleWork(); Assert(e.Pose == "read" && !e.Smoking && e.RecentCount == 1, "end accounting wrong");
                Start(e); e.Advance(10); Assert(Math.Abs(e.Data.Active.Burned - 10) < 0.001 && e.StockOf("liqun") == 18, "old cigarette resumed");
            });
            Check("switch/unequip discards used cigarette without refund and restores reading", delegate
            {
                Engine e = Ready("liqun"); e.Buy("hehua"); Start(e); e.Advance(5); e.Equip("hehua");
                Assert(e.Data.Active == null && e.StockOf("liqun") == 19 && e.Data.Litter.Count == 1 && e.RecentCount == 0.5, "switch lost active cigarette accounting");
                Start(e); e.Unequip(); Assert(e.Data.Active == null && e.Pose == "read" && e.Data.Litter.Count == 2, "unequip state");
            });
            Check("vape exactly 20 puffs per batch; preserves device until empty", delegate
            {
                Engine e = Ready("vape"); Start(e);
                for (int i = 0; i < 100; i++) e.ClickPet(); Assert(e.Data.Active.Puffs == 0, "vape bypassed cooldown");
                e.Advance(120); e.AdvancePresentation(1.5); Assert(e.Data.Active == null && e.FindStock("vape").VapeRemaining == 780 && e.Pose == "read", "batch exceeded 20");
                e.Advance(120); e.ClickPet(); Assert(e.FindStock("vape").VapeRemaining == 780, "auto next batch");
                for (int batch = 0; batch < 4; batch++) { Start(e); e.Advance(120); }
                Assert(e.FindStock("vape").VapeRemaining == 700 && e.Data.Litter.Count == 0 && e.VapeBattery("vape") == 87.5, "800-puff device capacity/battery");
                Assert(!e.Data.Collapsed && e.RecentCount == 5, "vape health threshold");
                e.FindStock("vape").VapeRemaining = 20; Start(e); e.Advance(6);
                Assert(e.DrunkSeconds == 20, "sixth vape round did not trigger drunk");
                e.Advance(20); e.AdvancePresentation(20); e.Advance(114);
                Assert(e.FindStock("vape").VapeRemaining == 0 && e.Data.Litter.Count == 1 && e.Data.Litter[0].Vape, "empty vape not discarded");
            });
            Check("putting away vape preserves remaining charge and unused devices", delegate
            {
                Engine e = Ready("vape"); e.Buy("vape", 2); Start(e); e.Advance(30); e.Unequip();
                Assert(e.FindStock("vape").VapeRemaining == 795 && e.StockOf("vape") == 2 && e.Data.Litter.Count == 0, "vape lost on unequip");
                e.Equip("vape"); Start(e); e.Advance(120); Assert(e.FindStock("vape").VapeRemaining == 775, "wrong remaining charge");
            });
            Check("individual debris needs exactly five cleanup clicks", delegate
            {
                Engine e = Ready("liqun"); Start(e); e.Advance(240); Start(e); e.Advance(240);
                int id = e.Data.Litter[0].Id;
                for (int i = 0; i < 4; i++) Assert(!e.Clean(id), "cleaned early");
                Assert(e.Clean(id) && e.Data.Litter.Count == 1 && !e.Clean(id), "cleaned wrong item");
            });
            Check("cough/collapse and dead pet may be hidden and shown without revival", delegate
            {
                Engine e = Ready("liqun");
                for (int i = 0; i < 10; i++) { SmokeFast(e); if (i == 4) Assert(!e.Coughing && e.HealthStatus == "状态良好", "five is still good"); if (i == 5) Assert(e.Coughing && e.HealthStatus == "醉醺醺的", "missing drunken status"); }
                Assert(e.Data.Collapsed && e.RecoveryRemaining > 7190 && !Start(e), "collapse missing");
                e.EndWork(); Assert(!e.PetVisible && e.Data.Collapsed, "end revived");
                e.ToggleWork(); Assert(e.PetVisible && e.Pose == "collapsed", "start revived");
                int money = e.Data.Money; e.Advance(60); Assert(e.Data.Money == money, "dead pet still earning");
            });
            Check("rice reservation, failed feed, hidden pet and successful feed accounting", delegate
            {
                Engine e = Ready("liqun"); e.Buy("rice", 2); Assert(e.StockOf("rice") == 2, "rice missing");
                e.EndWork(); e.AdvancePresentation(2); Assert(!e.DeployRice() && !e.Data.RiceDeployed, "deployed without pet");
                e.ToggleWork(); Assert(e.DeployRice() && !e.DeployRice() && e.StockOf("rice") == 2, "duplicate deploy or early spend");
                Assert(!e.FeedRice() && e.StockOf("rice") == 2, "healthy feeding spent rice");
                for (int i = 0; i < 10; i++) SmokeFast(e);
                e.EndWork(); Assert(!e.FeedRice() && e.Data.RiceDeployed, "hidden feed");
                e.ToggleWork(); e.GrabRice(); Assert(e.FeedRice() && !e.Data.Collapsed && e.StockOf("rice") == 1 && !e.Data.RiceDeployed, "feeding failed");
                Assert(e.RecentCount == 0 && e.Pose == "revive" && e.Data.Litter.Count == 10, "revive reset wrong");
                Assert(!e.FeedRice() && e.StockOf("rice") == 1, "duplicate feeding");
            });
            Check("real two-hour recovery independent of work/game clock and preserves rice", delegate
            {
                DateTime now = new DateTime(2026, 10, 16, 1, 0, 0, DateTimeKind.Utc);
                SaveData data = new SaveData { Collapsed = true, ReviveAtUtcTicks = now.AddHours(2).Ticks, Money = 500 };
                Engine e = new Engine(data, delegate { return now; }); e.Buy("rice"); e.ToggleWork(); e.DeployRice();
                e.Advance(7200); Assert(e.Data.Collapsed, "game time advanced real recovery");
                e.EndWork(); now = now.AddSeconds(7199); e.CheckRecovery(); Assert(e.Data.Collapsed, "early recovery");
                now = now.AddSeconds(1); e.CheckRecovery();
                Assert(!e.Data.Collapsed && !e.PetVisible && e.StockOf("rice") == 1 && !e.Data.RiceDeployed, "auto revival state");
            });
            Check("expired death recovers immediately after reopening", delegate
            {
                DateTime now = DateTime.UtcNow;
                Engine e = new Engine(new SaveData { Collapsed = true, ReviveAtUtcTicks = now.AddMinutes(-1).Ticks }, delegate { return now; });
                Assert(!e.Data.Collapsed && !e.PetVisible, "expired death not restored");
            });
            Check("twenty minute rolling window still expires during intermittent smoking", delegate
            {
                Engine e = Ready("liqun"); Start(e); e.DiscardSmoking();
                for (int i = 0; i < 2; i++) { e.Advance(500); Start(e); e.DiscardSmoking(); }
                e.Advance(185.9); Assert(e.RecentCount == 1.5, "expired early");
                e.Advance(0.2); Assert(e.RecentCount == 1, "old record retained");
            });
            Check("save retains favorites, open vape, reserved rice, timer and hidden restart", delegate
            {
                WithSave(delegate(string path)
                {
                    Engine e = Ready("vape"); e.ToggleFavorite("vape"); e.Buy("rice"); e.DeployRice(); Start(e); e.Advance(18);
                    e.Data.PriceDescending = true;
                    Storage.Save(path, e.Data); Storage.Save(path, e.Data);
                    string warning; SaveData d = Storage.Load(path, out warning);
                    Assert(warning == null && d.Version == 5 && d.Stocks.First(s => s.Id == "vape").VapeRemaining == 797 && d.Stocks.First(s => s.Id == "vape").Favorite && d.RiceDeployed && d.PriceDescending, "roundtrip mismatch");
                    Engine reopened = new Engine(d); Assert(!reopened.Working && !reopened.PetVisible && d.Active.Suspended, "restart not paused");
                });
            });
            Check("v1 migration keeps balances, stock, partial vape and a permanent original backup", delegate
            {
                WithSave(delegate(string path)
                {
                    SaveData old = new SaveData { Version = 1, Money = 128, Selected = "liqun", Collapsed = true,
                        Active = new Session { ProductId = "vape", Puffs = 37, Age = 150 } };
                    old.Stocks.Add(new Stock { Id = "vape", Count = 2 }); Storage.Save(path, old);
                    string warning; SaveData d = Storage.Load(path, out warning);
                    Assert(warning == null && d.Money == 128 && d.Version == 5 && d.Selected == "" && d.Active == null && d.Stocks[0].VapeRemaining == 763 && d.Stocks[0].Count == 2, "migration lost inventory or retained dead equipment");
                    Assert(d.ReviveAtUtcTicks > DateTime.UtcNow.Ticks && File.Exists(path + ".v1.bak"), "migration missing deadline/backup");
                });
            });
            Check("corrupt save is preserved before resetting", delegate
            {
                WithSave(delegate(string path)
                {
                    File.WriteAllText(path, "broken XML"); string warning; SaveData d = Storage.Load(path, out warning);
                    Assert(warning != null && d.Money == 200 && Directory.GetFiles(Path.GetDirectoryName(path), "*.corrupt-*").Length == 1, "lost corrupt save");
                });
            });
            Check("shake uses burned half boundary; twenty early discards cause death", delegate
            {
                foreach (double seconds in new[] { 0.0, 119.9, 120.0, 120.1, 239.0 })
                {
                    Engine partial = Ready("liqun"); Start(partial); partial.Advance(seconds);
                    Assert(partial.DiscardSmoking() && partial.RecentCount == (seconds <= 120 ? 0.5 : 1) && !partial.DiscardSmoking(), "wrong half boundary");
                }
                Engine e = Ready("liqun");
                for (int i = 0; i < 19; i++) { WaitUntilSober(e); Assert(Start(e), "cannot start discard"); e.DiscardSmoking(); }
                Assert(e.RecentCount == 9.5 && !e.Data.Collapsed && e.Coughing, "early half-score collapse");
                Start(e); e.DiscardSmoking();
                Assert(e.RecentCount == 10 && e.Data.Collapsed && e.Data.Litter.Count == 20 && e.Data.Completed == 0, "half threshold or debris wrong");
            });
            Check("half counts survive save/load and reset after ten smoke-free minutes", delegate
            {
                WithSave(delegate(string path)
                {
                    Engine e = Ready("liqun"); Start(e); e.DiscardSmoking(); e.Advance(30);
                    Start(e); e.DiscardSmoking(); Storage.Save(path, e.Data);
                    string warning; Engine loaded = new Engine(Storage.Load(path, out warning));
                    Assert(warning == null && loaded.RecentCount == 1 && loaded.Data.Health.Count == 2, "lost half records");
                    loaded.Advance(599.9); Assert(loaded.RecentCount == 1, "idle reset too early");
                    loaded.Advance(0.1); Assert(loaded.RecentCount == 0, "ten minute reset missing");
                });
            });
            Check("three vertical cycles trigger once; ordinary drag, jitter, release and gaps do not", delegate
            {
                VerticalShake g = new VerticalShake(); g.Begin(100, 100, 0);
                for (int i = 0; i < 5; i++) Assert(!g.Move(100, i % 2 == 0 ? 64 : 100, (i + 1) * 0.1), "gesture fired before three cycles");
                Assert(g.Move(100, 100, 0.6) && !g.Move(100, 64, 0.7), "gesture repeat or missing");
                g.Begin(100, 100, 0);
                for (int i = 1; i <= 20; i++) Assert(!g.Move(100, 100 + i * 30, i * 0.05), "straight drag fired");
                g.Begin(100, 100, 0);
                for (int i = 1; i <= 20; i++) Assert(!g.Move(100, 100 + (i % 2) * 8, i * 0.05), "jitter fired");
                g.Begin(100, 100, 0); g.Move(100, 64, 0.1); g.End();
                Assert(!g.Move(100, 100, 0.2), "release retained gesture");
                g.Begin(100, 100, 0);
                for (int i = 0; i < 10; i++) Assert(!g.Move(100, i % 2 == 0 ? 64 : 100, (i + 1) * 2), "slow movement fired");
            });
            Check("ungrabbed rice consumes exactly one at 20 real seconds", delegate
            {
                Engine e = Ready("liqun"); e.Buy("rice", 2); e.DeployRice();
                e.Advance(200); Assert(e.Data.RiceSecondsRemaining == 20, "simulation accelerated rice");
                Assert(!e.AdvanceRice(19.9) && e.StockOf("rice") == 2, "early rice expiry");
                Assert(e.AdvanceRice(0.1) && !e.Data.RiceDeployed && e.StockOf("rice") == 1, "missing timeout consumption");
                Assert(!e.AdvanceRice(100) && !e.GrabRice() && e.StockOf("rice") == 1, "expired rice reused");
            });
            Check("grab starts a single 10-second deadline and expiry returns rice without spending", delegate
            {
                Engine e = Ready("liqun"); e.Buy("rice"); e.DeployRice(); e.AdvanceRice(19.9);
                Assert(e.GrabRice() && e.Data.RiceSecondsRemaining == 10, "grab timer wrong");
                e.AdvanceRice(5); e.GrabRice(); Assert(e.Data.RiceSecondsRemaining == 5, "regrab extended timer");
                e.AdvanceRice(4.9); Assert(e.Data.RiceDeployed, "grabbed rice expired early");
                Assert(e.AdvanceRice(0.1) && e.StockOf("rice") == 1 && !e.Data.RiceDeployed && !e.FeedRice(), "grab timeout consumed rice or allowed late feed");
            });
            Check("caught rice deadline persists and keeps running while hidden or closed", delegate
            {
                WithSave(delegate(string path)
                {
                    DateTime now = DateTime.UtcNow;
                    Engine e = new Engine(new SaveData { Money = 200 }, delegate { return now; }); e.ToggleWork();
                    e.Buy("rice"); e.DeployRice(); e.GrabRice(); now = now.AddSeconds(4); e.AdvanceRice(4);
                    e.EndWork(); Assert(e.Data.RiceSecondsRemaining == 6, "hide changed rice deadline");
                    Storage.Save(path, e.Data); string warning;
                    Engine reopened = new Engine(Storage.Load(path, out warning), delegate { return now; });
                    Assert(warning == null && reopened.Data.RiceCaught && reopened.Data.RiceSecondsRemaining == 6, "rice timer lost");
                    reopened.AdvanceRice(6); Assert(!reopened.Data.RiceDeployed && reopened.StockOf("rice") == 1, "hidden rice failed to return");
                    now = now.AddSeconds(6);
                    Engine lateReopen = new Engine(Storage.Load(path, out warning), delegate { return now; });
                    Assert(!lateReopen.Data.RiceDeployed && lateReopen.StockOf("rice") == 1, "offline rice deadline reset");
                });
            });
            Check("late feed checks real deadline even before next timer tick", delegate
            {
                DateTime now = DateTime.UtcNow;
                Engine e = new Engine(new SaveData { Money = 200, Collapsed = true, ReviveAtUtcTicks = now.AddHours(2).Ticks }, delegate { return now; });
                e.ToggleWork(); e.Buy("rice"); e.DeployRice(); e.GrabRice(); now = now.AddSeconds(10);
                Assert(!e.FeedRice() && e.Data.Collapsed && !e.Data.RiceDeployed && e.StockOf("rice") == 1, "late feed consumed rice or revived");
            });
            Check("empty equipment speaks exact requested text in working and paused states", delegate
            {
                Engine e = new Engine(new SaveData()); e.ToggleWork();
                Assert(!Start(e) && e.Notice == "我兜里真没烟", "working text mismatch");
                e.ToggleWork(); Assert(!Start(e) && e.Notice == "我兜里真没烟", "paused text mismatch");
            });
            Check("30/55/60/90 minute boundaries and fractional income", delegate
            {
                Engine e = new Engine(new SaveData()); e.ToggleWork(); e.Advance(1799.9);
                Assert(e.IncomeRate == 1 && e.Data.FatigueAction == "", "early fatigue");
                e.Advance(0.1); Assert(e.IncomeRate == 0.5 && e.Pose == "crave" && e.Data.FatigueSeconds == 20 && e.Data.Money == 290, "30 minute boundary");
                e.Advance(19.9); Assert(e.Pose == "crave", "action too short");
                e.Advance(0.1); Assert(e.Data.FatigueAction == "", "action too long");
                e.Advance(100); Assert(e.Data.Money == 293, "half income lost fraction");
                e.Advance(1380); Assert(e.Data.ContinuousWorkSeconds == 3300 && e.Data.FatigueAction == "", "55 minute overlap");
                for (int i = 0; i < 299; i++) { e.Advance(1); Assert(e.Data.FatigueAction == "", "action in quiet interval"); }
                e.Advance(1); Assert(e.Pose == "smash" && e.Data.FatigueSeconds == 20 && e.IncomeRate == 0.1 && e.Data.Money == 335, "60 minute boundary");
                e.Advance(600); Assert(e.Data.Money == 338, "tenth income wrong");
                e.Advance(1199.9); Assert(!e.Data.Collapsed, "early death");
                e.Advance(0.1); Assert(e.Data.Collapsed && e.Data.DeathReason == "fatigue" && e.IncomeRate == 0 && e.Data.FatigueAction == "" && e.Data.Money == 344, "90 minute death");
                e.Advance(600); Assert(e.Data.Money == 344, "death earned income");
            });
            Check("random actions obey duration, spacing and stage restrictions", delegate
            {
                Engine e = new Engine(new SaveData()); e.ToggleWork(); e.Advance(1800);
                double last = 1800, next = e.Data.NextFatigueAt;
                Assert(next >= 1860 && next <= 1980, "first interval outside range");
                int starts = 0;
                for (int i = 0; i < 1500; i++)
                {
                    e.Advance(1);
                    if (e.Data.NextFatigueAt != next)
                    {
                        double start = e.Data.ContinuousWorkSeconds;
                        Assert(start - last >= 60 && start - last <= 181, "random spacing outside range");
                        Assert(new[] { "crave", "walk", "sleep" }.Contains(e.Data.FatigueAction), "invalid first stage action");
                        Assert(e.Data.FatigueSeconds > 19 && e.Data.FatigueSeconds <= 20, "wrong duration");
                        next = e.Data.NextFatigueAt; last = start; starts++;
                    }
                }
                Assert(starts > 5 && e.Data.FatigueAction == "", "missing random actions");
                e.Advance(300); last = 3600; next = e.Data.NextFatigueAt;
                Assert(next >= 3780 && next <= 3900, "smash schedule outside range");
                for (int i = 0; i < 1500; i++)
                {
                    e.Advance(1);
                    if (next != e.Data.NextFatigueAt)
                    {
                        Assert(e.Data.ContinuousWorkSeconds - last >= 180 && e.Data.ContinuousWorkSeconds - last <= 301 && e.Pose == "smash", "smash spacing/action");
                        last = e.Data.ContinuousWorkSeconds; next = e.Data.NextFatigueAt;
                    }
                }
            });
            Check("pause/end reset only after ten minutes; smoking resets immediately", delegate
            {
                Engine e = Ready("liqun"); e.Advance(1800); e.ToggleWork(); e.Advance(599.9);
                Assert(e.Data.ContinuousWorkSeconds == 1800, "short break reset");
                e.ToggleWork(); e.Advance(60); Assert(e.Data.ContinuousWorkSeconds == 1860, "resume lost streak");
                e.ToggleWork(); e.Advance(300); e.EndWork(); e.Advance(299.9);
                Assert(e.Data.ContinuousWorkSeconds == 1860, "end reset break clock");
                e.Advance(0.1); Assert(e.Data.ContinuousWorkSeconds == 0 && e.IncomeRate == 1, "ten minute reset missing");
                e.ToggleWork(); e.Advance(3600); Assert(Start(e) && e.Data.ContinuousWorkSeconds == 0 && e.Data.FatigueAction == "" && e.IncomeRate == 1, "smoke did not reset");
                e.Advance(240); Assert(e.Data.ContinuousWorkSeconds == 0, "smoking counted as abstinence");
                e.Advance(1); Assert(e.Data.ContinuousWorkSeconds == 1, "new streak missing");
            });
            Check("offline restart preserves streak, break, fractional earnings and weighted health", delegate
            {
                WithSave(delegate(string path)
                {
                    DateTime now = DateTime.UtcNow;
                    Engine e = Ready("vape"); Start(e); e.Advance(42); e.DiscardSmoking();
                    e.Advance(61.5); e.ToggleWork(); e.Advance(100); Storage.Save(path, e.Data);
                    string warning; Engine loaded = new Engine(Storage.Load(path, out warning), delegate { return now.AddDays(3); });
                    Assert(warning == null && loaded.RecentCount == 0.35 && loaded.Data.ContinuousWorkSeconds == 61.5 && loaded.Data.BreakSeconds == 100, "offline aged work or health");
                    Assert(Math.Abs(loaded.Data.IncomeRemainder - e.Data.IncomeRemainder) < 0.000001, "fraction lost");
                    loaded.Advance(499.9); Assert(loaded.Data.ContinuousWorkSeconds == 61.5, "offline break counted");
                    loaded.Advance(0.1); Assert(loaded.Data.ContinuousWorkSeconds == 0, "saved break did not resume");
                });
            });
            Check("vape puffs count immediately and shaking consumes device without double counting", delegate
            {
                Engine e = Ready("vape"); Start(e); e.Advance(6); Assert(e.RecentCount == 0.05, "first puff missing");
                e.Advance(36); Assert(e.DiscardSmoking() && e.RecentCount == 0.35 && e.FindStock("vape").VapeRemaining == 0 && e.Data.Litter.Single().Vape, "vape shake accounting");
                e = Ready("vape"); e.Buy("vape");
                for (int i = 0; i < 10; i++) { WaitUntilSober(e); Assert(Start(e), "cannot start batch"); for (int n = 0; n < 60; n++) { WaitUntilSober(e); e.ClickPet(); e.Advance(.5); } if (i < 9) Assert(!e.Data.Collapsed, "early vape death"); }
                Assert(e.RecentCount == 10 && e.Data.Collapsed && e.Data.Litter.Count == 0 && e.FindStock("vape").VapeRemaining == 600, "200 puff threshold");
            });
            Check("end/resume can cause death and preserve hidden/visible death states", delegate
            {
                foreach (bool paused in new[] { false, true })
                {
                    Engine e = Ready("liqun"); if (paused) e.ToggleWork();
                    for (int i = 0; i < 9; i++) e.Data.Health.Add(new HealthEntry { Time = e.Data.RuntimeSeconds, Amount = 1 });
                    Start(e); e.EndWork();
                    Assert(e.RecentCount == 10 && e.Data.Collapsed && !e.PetVisible && e.Pose == "collapsed" && e.Data.Litter.Count == 1, "end death missing");
                    e.ToggleWork(); Assert(e.PetVisible && e.Pose == "collapsed", "dead restart became alive");
                }
                Engine resumed = Ready("liqun"); resumed.ToggleWork();
                for (int i = 0; i < 9; i++) resumed.Data.Health.Add(new HealthEntry { Amount = 1 });
                Start(resumed); resumed.ToggleWork();
                Assert(resumed.Data.Collapsed && resumed.PetVisible && resumed.Working && resumed.RecentCount == 10, "paused resume death missing");
            });
            Check("v2 migration preserves weighted history and fractional income", delegate
            {
                WithSave(delegate(string path)
                {
                    SaveData old = new SaveData { Version = 2, WorkSeconds = 90, RuntimeSeconds = 100, VapeHealthPuffs = 3 };
                    old.Recent.Add(50); old.RecentHalf.Add(75); Storage.Save(path, old);
                    string warning; SaveData loaded = Storage.Load(path, out warning);
                    Assert(warning == null && loaded.Version == 5 && File.Exists(path + ".v2.bak"), "v2 upgrade missing");
                    Assert(new Engine(loaded).RecentCount == 1.65 && loaded.IncomeRemainder == 0.5 && loaded.Recent.Count == 0, "v2 history lost");
                });
            });
            Check("smoke-free reset restarts on smoking and persists across offline time", delegate
            {
                WithSave(delegate(string path)
                {
                    Engine e = Ready("liqun"); Start(e); e.DiscardSmoking(); e.Advance(599.9);
                    Assert(e.RecentCount == 0.5, "reset early");
                    Start(e); e.Advance(239.9); Assert(e.RecentCount == 0.5 && e.Data.SmokeFreeSeconds == 0, "active smoking aged idle timer");
                    e.Advance(0.1); e.EndWork(); e.Advance(599.9); Storage.Save(path, e.Data);
                    string warning; Engine reopened = new Engine(Storage.Load(path, out warning), delegate { return DateTime.UtcNow.AddDays(7); });
                    Assert(warning == null && reopened.RecentCount == 1 && reopened.Data.SmokeFreeSeconds == 599.9, "offline changed idle history");
                    reopened.Advance(0.1); Assert(reopened.RecentCount == 0 && reopened.HealthStatus == "状态良好", "saved idle timer failed to reset");
                });
            });
            Check("death unequips and rejects equipment without consuming inventory", delegate
            {
                Engine e = new Engine(new SaveData { Money = 10000 }); e.ToggleWork(); e.Buy("liqun"); e.Equip("liqun"); e.Advance(5400); e.EndWork();
                Assert(e.Data.Selected == "" && e.Pose == "collapsed" && e.HealthStatus == "死亡" && e.EfficiencyStatus == "死亡", "dead state inconsistent");
                Assert(!e.Equip("liqun") && e.Notice == "小人死亡，无法装备" && e.StockOf("liqun") == 20, "dead equipment mutated inventory");
                e.ToggleWork(); Assert(!e.Equip("liqun") && e.Data.Selected == "", "visible death permitted equipment");
                e.Buy("rice"); e.DeployRice(); e.GrabRice(); Assert(e.FeedRice() && e.Data.Selected == "" && e.Pose == "revive", "revive auto equipped");
                e.AdvancePresentation(3);
                Assert(e.Equip("liqun") && e.Pose == "equip", "revive did not allow equipment");
            });
            Check("death during equipment switch cannot equip the new box", delegate
            {
                Engine e = Ready("liqun"); e.Buy("hehua");
                for (int i = 0; i < 9; i++) e.Data.Health.Add(new HealthEntry { Amount = 1 });
                e.Data.Health.Add(new HealthEntry { Amount = 0.5 });
                Start(e); Assert(!e.Equip("hehua") && e.Data.Collapsed && e.Data.Selected == "" && e.RecentCount == 10, "switch equipped after fatal discard");
                Assert(e.StockOf("hehua") == 20 && e.StockOf("liqun") == 19, "switch changed stock");
            });
            Check("v3 migration keeps existing wallet and seeds smoke-free history", delegate
            {
                WithSave(delegate(string path)
                {
                    SaveData old = new SaveData { Version = 3, Money = 7, RuntimeSeconds = 599.9, IncomeRemainder = 0.75 };
                    old.Health.Add(new HealthEntry { Time = 0, Amount = 0.5 }); Storage.Save(path, old);
                    string warning; SaveData loaded = Storage.Load(path, out warning);
                    Assert(warning == null && loaded.Version == 5 && loaded.Money == 7 && loaded.IncomeRemainder == 0.75 && File.Exists(path + ".v3.bak"), "v3 wallet changed");
                    Engine e = new Engine(loaded); Assert(e.RecentCount == 0.5 && e.Data.SmokeFreeSeconds == 599.9, "v3 history lost");
                    e.Advance(0.1); Assert(e.RecentCount == 0, "v3 idle timer not migrated");
                });
            });
            Check("catalog has sixteen Chinese, five foreign and three 800-puff devices", delegate
            {
                Assert(Catalog.All.Count(p => p.Country == "中国" && !p.Vape) == 16 && Catalog.All.Count(p => p.Country == "外国") == 5 && Catalog.All.Count(p => p.Vape) == 3, "catalog counts");
                Assert(Catalog.Get("hehua").Price == 45 && Catalog.Get("liqun").Price == 19 && Catalog.Get("hetianxia").Price == 100, "requested prices");
                Assert(Catalog.All.Select(p => p.Id).Distinct().Count() == Catalog.All.Length, "duplicate ID");
                Assert(Catalog.All.Select(p => p.ShopGroup).Distinct().Count() == 4 && Catalog.Get("rice").Name == "大米饭", "shop groups or rice name");
                Assert(Catalog.Get("marlboro_red").Price == 25 && Catalog.Get("marlboro_red").Duration == 240 && Catalog.Get("esse_watermelon").Price == 19 && Catalog.Get("peel_redwine").Price == 25 && Catalog.Get("black_devil_vanilla").Price == 30 && Catalog.Get("lucky_strike").Price == 50 && Catalog.Get("lucky_strike").Duration == 420, "foreign settings");
                Assert(Catalog.Get("relx5").Price == 19 && Catalog.Get("duckbill").Price == 30 && Catalog.Get("lost_mary").Price == 41, "vape settings");
                foreach (Product p in Catalog.All.Where(p => p.Vape))
                {
                    Engine e = Ready(p.Id); Start(e);
                    Assert(e.FindStock(p.Id).VapeRemaining == 800 && e.VapeBattery(p.Id) == 100, "inconsistent capacity");
                }
            });
            Check("price animation boundaries, random timing and five real second bulb", delegate
            {
                foreach (string id in new[] { "liqun", "lm", "kent", "hehua", "esse_watermelon", "marlboro_red", "lucky_strike", "relx5", "duckbill", "lost_mary" })
                {
                    Engine e = Ready(id); Start(e); e.Advance(3);
                    int price = Catalog.Get(id).Price, triggers = 0; bool wasOn = false; double lit = 0;
                    Assert(!e.HeartEyes, "heart eyes still enabled");
                    for (int i = 0; i < 1200; i++)
                    {
                        e.AdvancePresentation(0.05);
                        bool on = e.PriceCoughing || e.LightBulb;
                        if (on && !wasOn) { triggers++; lit = 0; }
                        if (on) lit += 0.05;
                        if (!on && wasOn && price > 40) Assert(Math.Abs(lit - 5) < 0.051, "bulb duration not five seconds");
                        Assert(price < 20 ? !e.LightBulb && !e.HeartEyes : price > 40 ? !e.PriceCoughing : !on && !e.HeartEyes, "effect leaked into wrong tier");
                        wasOn = on;
                    }
                    Assert(price >= 20 && price <= 40 ? triggers == 0 : triggers >= 2, "random effects missing");
                    Assert(e.RecentCount == 0, "visual effects changed health");
                    e.EndWork(); e.AdvancePresentation(1); Assert(!e.PriceCoughing && !e.LightBulb && !e.HeartEyes && e.PriceEffectRemaining == 0, "effect survived session");
                }
                Engine swapped = Ready("liqun"); swapped.Buy("hehua"); Start(swapped); swapped.Advance(3);
                for (int i = 0; i < 200 && !swapped.PriceCoughing; i++) swapped.AdvancePresentation(0.05);
                Assert(swapped.PriceCoughing, "cough setup"); swapped.Equip("hehua"); Start(swapped); swapped.Advance(3);
                Assert(!swapped.PriceCoughing && !swapped.LightBulb && !swapped.HeartEyes, "previous cigarette effect leaked into next session");
            });
            Check("v4 catalog migration preserves stock and consumed vape puffs", delegate
            {
                WithSave(delegate(string path)
                {
                    SaveData old = new SaveData { Version = 4, Selected = "classic", Active = new Session { ProductId = "classic", Burned = 200 } };
                    old.Stocks.Add(new Stock { Id = "classic", Count = 19, Favorite = true });
                    old.Stocks.Add(new Stock { Id = "mint", Count = 40 }); old.Stocks.Add(new Stock { Id = "vape", Count = 2, VapeRemaining = 63 });
                    Storage.Save(path, old); string warning; SaveData loaded = Storage.Load(path, out warning);
                    Assert(warning == null && loaded.Version == 5 && loaded.Selected == "parliament" && loaded.Active.ProductId == "parliament" && loaded.Active.Burned == 200, "active migration failed");
                    Assert(loaded.Stocks.Single(s => s.Id == "parliament").Favorite && loaded.Stocks.Single(s => s.Id == "mevius").Count == 40 && loaded.Stocks.Single(s => s.Id == "vape").VapeRemaining == 763 && File.Exists(path + ".v4.bak"), "inventory migration failed");
                });
            });
            Check("retired v5 inventory and active duration survive the smaller shop catalog", delegate
            {
                WithSave(delegate(string path)
                {
                    SaveData old = new SaveData { Selected = "marlboro", Active = new Session { ProductId = "marlboro", Burned = 300 } };
                    old.Stocks.Add(new Stock { Id = "marlboro", Count = 39, Favorite = true });
                    old.Stocks.Add(new Stock { Id = "vape_aurora", Count = 2, VapeRemaining = 678 });
                    Storage.Save(path, old); string warning; SaveData loaded = Storage.Load(path, out warning);
                    Assert(warning == null && loaded.Active.Burned == 300 && Catalog.Get(loaded.Active.ProductId).Duration == 420, "retired session lost");
                    Assert(loaded.Stocks.Single(s => s.Id == "marlboro").Count == 39 && loaded.Stocks.Single(s => s.Id == "vape_aurora").VapeRemaining == 678, "retired stock lost");
                    Assert(!Catalog.All.Any(p => p.Id == "marlboro" || p.Id == "vape_aurora"), "retired product returned to shop");
                });
            });
            Check("reject non-finite time", delegate
            {
                bool rejected = false; try { new Engine(new SaveData()).Advance(Double.NaN); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Assert(rejected, "NaN accepted");
            });
            Console.WriteLine("All " + passed + " tests passed."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
    private static void WithSave(Action<string> action)
    {
        string root = Path.GetFullPath(Path.GetTempPath());
        string dir = Path.GetFullPath(Path.Combine(root, "SmokeSimulatorTests-" + Guid.NewGuid().ToString("N")));
        if (!dir.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new Exception("Unsafe test directory");
        Directory.CreateDirectory(dir);
        try { action(Path.Combine(dir, "save.xml")); }
        finally { if (Directory.Exists(dir)) Directory.Delete(dir, true); }
    }
}
