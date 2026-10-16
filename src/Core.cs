using System;
using System.Collections.Generic;
using System.Linq;

namespace SmokeSimulator
{
    public class Product
    {
        public string Id, Name, Category, Country, Format, Color;
        public int Price, Units;
        public double Duration;
        public bool Vape;
        public bool Rice { get { return Id == "rice"; } }
        public string SmokeStyle { get { return Id == "daguanyuan" ? "green" : Id == "black_devil_vanilla" ? "pink" : Id == "lost_mary" ? "rainbow" : "gray"; } }
        public string ShopGroup { get { return Rice ? "大米饭" : Vape ? "电子烟" : Country == "中国" ? "中国烟" : "外国烟"; } }
        public Product(string id, string name, string category, string country, string format, int price, double duration, string color, bool vape)
        {
            Id = id; Name = name; Category = category; Country = country; Format = format;
            Price = price; Duration = duration; Color = color; Vape = vape; Units = vape || Rice ? 1 : 20;
        }
    }
    public static class Catalog
    {
        public static readonly Product[] All = {
            new Product("hongmei", "红梅", "卷烟", "中国", "粗烟", 8, 240, "#BF5C51", false),
            new Product("yingchun", "迎春", "卷烟", "中国", "细烟", 19, 300, "#DFBE79", false),
            new Product("liqun", "白利群", "卷烟", "中国", "粗烟", 19, 240, "#E6DBC8", false),
            new Product("yinchai", "银钗", "卷烟", "中国", "细烟", 22, 300, "#C6CBD3", false),
            new Product("furongwang", "芙蓉王", "卷烟", "中国", "粗烟", 25, 240, "#D2B467", false),
            new Product("feima", "飞马", "卷烟", "中国", "粗烟", 25, 240, "#77C4DA", false),
            new Product("yuxi", "玉溪", "卷烟", "中国", "粗烟", 25, 240, "#B86458", false),
            new Product("jiangnanyun", "江南韵", "卷烟", "中国", "细烟", 27, 300, "#A1B99A", false),
            new Product("jinchai", "金钗", "卷烟", "中国", "细烟", 28, 300, "#CFB36C", false),
            new Product("kuanzhai", "宽窄", "卷烟", "中国", "粗烟", 30, 240, "#BFA680", false),
            new Product("daguanyuan", "大观园", "卷烟", "中国", "细烟", 30, 300, "#B6CBB7", false),
            new Product("hehua", "荷花", "卷烟", "中国", "粗烟", 45, 240, "#6C9E89", false),
            new Product("zhonghua_hard", "硬中华", "卷烟", "中国", "粗烟", 50, 240, "#C45F53", false),
            new Product("yuhuashi", "南京雨花石", "卷烟", "中国", "细烟", 55, 300, "#C9B98B", false),
            new Product("zhonghua_soft", "软中华", "卷烟", "中国", "粗烟", 70, 240, "#AB514B", false),
            new Product("hetianxia", "和天下", "卷烟", "中国", "粗烟", 100, 240, "#8F699A", false),
            new Product("esse_watermelon", "爱喜爆珠", "爆珠烟", "外国", "细烟", 19, 300, "#9DCB95", false),
            new Product("marlboro_red", "万宝路硬红", "卷烟", "外国", "粗烟", 25, 240, "#BD5149", false),
            new Product("peel_redwine", "百乐爆珠", "爆珠烟", "外国", "细烟", 25, 300, "#99566C", false),
            new Product("black_devil_vanilla", "小恶魔香草", "卷烟", "外国", "粗烟", 30, 240, "#CF438E", false),
            new Product("lucky_strike", "Lucky Strike", "卷烟", "外国", "Super King Size", 50, 420, "#BB5B4E", false),
            new Product("relx5", "悦刻五代", "电子烟", "电子烟", "800 口 / 支", 19, 0, "#899AC9", true),
            new Product("duckbill", "鸭嘴兽电子烟", "电子烟", "电子烟", "800 口 / 支", 30, 0, "#747A82", true),
            new Product("lost_mary", "LOST MARY", "电子烟", "电子烟", "800 口 / 支", 41, 0, "#9F88BF", true),
            new Product("rice", "大米饭", "道具", "道具", "拖到死亡小人身上复活", 100, 0, "#FAF2DC", false)
        };
        // Keep retired IDs readable so existing inventory and active sessions survive updates.
        // Retired products do not appear in the shop.
        private static readonly Product[] Legacy = {
            new Product("chesterfield", "切斯特菲尔德", "卷烟", "外国", "粗烟", 18, 240, "#BA795F", false),
            new Product("lm", "L&M", "卷烟", "外国", "粗烟", 20, 240, "#BF6A64", false),
            new Product("ld", "LD", "卷烟", "外国", "粗烟", 22, 240, "#A36E67", false),
            new Product("winston", "云斯顿", "卷烟", "外国", "粗烟", 25, 240, "#B77F6B", false),
            new Product("camel", "骆驼", "卷烟", "外国", "粗烟", 30, 240, "#C5AF78", false),
            new Product("mevius", "MEVIUS", "爆珠烟", "外国", "细烟", 35, 300, "#80B2BB", false),
            new Product("kent", "健牌", "卷烟", "外国", "细烟", 40, 300, "#B4C5D8", false),
            new Product("marlboro", "万宝路", "卷烟", "外国", "Super King Size", 45, 420, "#B8806F", false),
            new Product("dunhill", "登喜路", "卷烟", "外国", "Super King Size", 55, 420, "#9D665C", false),
            new Product("parliament", "百乐门", "卷烟", "外国", "Super King Size", 65, 420, "#7897B7", false),
            // Original fictional devices; all have the same 800-puff capacity.
            new Product("vape_graphite", "石墨短杆", "电子烟", "原创", "800 口 / 支", 18, 0, "#747A82", true),
            new Product("vape_cloud", "云白方盒", "电子烟", "原创", "800 口 / 支", 30, 0, "#D8D7CC", true),
            new Product("vape", "雾蓝圆杆", "电子烟", "原创", "800 口 / 支", 39, 0, "#899AC9", true),
            new Product("vape_obsidian", "曜黑扁杆", "电子烟", "原创", "800 口 / 支", 58, 0, "#515964", true),
            new Product("vape_aurora", "极光渐变", "电子烟", "原创", "800 口 / 支", 88, 0, "#9F88BF", true)
        };
        public static Product Get(string id) { return All.Concat(Legacy).FirstOrDefault(p => p.Id == id); }
        public static bool Exists(string id) { return Get(id) != null; }
    }
    public class Stock
    {
        public string Id;
        // Cigarettes: individual sticks. Vapes/rice: unopened devices/bowls.
        public int Count, VapeRemaining;
        public bool Favorite;
    }
    public class Debris { public int Id, Clicks, X, Y; public bool Vape; public string ProductId = ""; }
    public class HealthEntry { public double Time, Amount; }
    public class Session
    {
        public string ProductId;
        public double Burned, Age, BoostUntil, PuffElapsed;
        public int Puffs;
        public bool Suspended, HalfwayEffectTriggered;
    }
    public class SaveData
    {
        public const int InitialMoney = 200;
        public int Version = 5;
        public int Money = InitialMoney, Avatar, NextDebrisId = 1, Completed, VapeHealthPuffs;
        // Missing in legacy XML; Storage marks the one-time startup-funds migration.
        public bool StartupFunds200Applied;
        public double WorkSeconds, RuntimeSeconds;
        public double SmokeFreeSeconds;
        public double ContinuousWorkSeconds, BreakSeconds, IncomeRemainder, NextFatigueAt, FatigueSeconds;
        public string FatigueAction = "", DeathReason = "";
        public bool HasWorked, Collapsed, RiceDeployed, PriceDescending;
        public bool RiceCaught;
        public double RiceSecondsRemaining = 20;
        public long RiceCaughtDeadlineUtcTicks;
        public long ReviveAtUtcTicks;
        public string Selected = "";
        public int PetX = -1, PetY = -1;
        public int PetScalePercent = 100;
        public List<Stock> Stocks = new List<Stock>();
        public List<double> Recent = new List<double>();
        public List<double> RecentHalf = new List<double>();
        public List<HealthEntry> Health = new List<HealthEntry>();
        public List<Debris> Litter = new List<Debris>();
        public Session Active;
    }
    public class Engine
    {
        public const int CoughThreshold = 5, CollapseThreshold = 10;
        public const double HealthWindow = 1200;
        public const int RecoverySeconds = 7200;
        public const int VapeCapacity = 800;
        public const double CigarettePreparation = 7, VapePreparation = 2;
        public const double CigaretteBreathSeconds = 8, VapeBreathSeconds = 6;
        public SaveData Data;
        private readonly Func<DateTime> utcNow;
        private readonly Random random = new Random();
        private DateTime Now { get { return utcNow(); } }
        public bool Working { get; private set; }
        public bool PetVisible { get; private set; }
        private bool reading = true;
        private Session effectSession;
        private double effectWait;
        private double boostRemaining;
        public double DrunkSeconds { get; private set; }
        public double ReviveSeconds { get; private set; }
        public double ClosingSeconds { get; private set; }
        public double CountSeconds { get; private set; }
        public double FinishSeconds { get; private set; }
        public string FinishProductId { get; private set; }
        private string finishPose;
        public Product VisualProduct { get { return Catalog.Get(Smoking ? Data.Active.ProductId : FinishSeconds > 0 ? FinishProductId : Data.Selected); } }
        public double PreparationSeconds { get { return Smoking && Catalog.Get(Data.Active.ProductId).Vape ? VapePreparation : CigarettePreparation; } }
        public double BreathSeconds { get { return Smoking && Catalog.Get(Data.Active.ProductId).Vape ? VapeBreathSeconds : CigaretteBreathSeconds; } }
        public string PriceEffect { get; private set; }
        public double PriceEffectRemaining { get; private set; }
        public bool PriceCoughing { get { return Smoking && Data.Active == effectSession && Catalog.Get(Data.Active.ProductId).Price < 20 && PriceEffect == "cough" && PriceEffectRemaining > 0; } }
        public bool HeartEyes { get { return false; } }
        public bool LightBulb { get { return Smoking && Data.Active.Age >= PreparationSeconds && Catalog.Get(Data.Active.ProductId).Price > 40 && Data.Active == effectSession && PriceEffect == "bulb" && PriceEffectRemaining > 0; } }
        public bool CoughAnimation { get { return PriceCoughing && DrunkSeconds == 0 && ClosingSeconds == 0 && ReviveSeconds == 0; } }
        public double BreathPhase { get { return !Smoking ? 0 : Math.Min(.999999, Data.Active.PuffElapsed / BreathSeconds); } }
        public bool Exhaling { get { return Smoking && Pose == "smoke" && BreathPhase >= .55; } }
        public double VapeBattery(string id) { Stock s = FindStock(id); return s == null ? 0 : s.VapeRemaining * 100.0 / VapeCapacity; }
        public double EquipmentDisplaySeconds { get; private set; }
        public string Notice = "开始工作，让你的代抽来到桌面。";
        public event Action<Debris> Dropped;
        public event Action Puffed;
        public Engine(SaveData data) : this(data, delegate { return DateTime.UtcNow; }) { }
        public Engine(SaveData data, Func<DateTime> now)
        {
            Data = data; utcNow = now;
            reading = true;
            if (Data.Collapsed) { Data.Selected = ""; Data.Active = null; }
            if (Data.Active != null) Data.Active.Suspended = true;
            CheckRecovery();
            if (Data.RiceDeployed && Data.RiceCaught && Data.RiceCaughtDeadlineUtcTicks == 0)
                Data.RiceCaughtDeadlineUtcTicks = Now.AddSeconds(Data.RiceSecondsRemaining).Ticks;
            AdvanceRice(0);
        }
        public double RecentCount { get { return Math.Round(Data.Health.Sum(h => h.Amount), 2); } }
        public double IncomeRate { get { return Data.Collapsed ? 0 : Data.ContinuousWorkSeconds >= 3600 ? 0.1 : Data.ContinuousWorkSeconds >= 1800 ? 0.5 : 1; } }
        public bool Coughing { get { return !Data.Collapsed && RecentCount > CoughThreshold; } }
        public string HealthStatus { get { return Data.Collapsed ? "死亡" : Coughing ? "醉醺醺的" : "状态良好"; } }
        public string EfficiencyStatus { get { return Data.Collapsed ? "死亡" : IncomeRate == 1 ? "效率正常" : "效率 " + (IncomeRate * 100).ToString("0") + "%"; } }
        public bool Smoking { get { return PetVisible && !Data.Collapsed && Data.Active != null && !Data.Active.Suspended; } }
        public double RecoveryRemaining
        {
            get { return Data.Collapsed ? Math.Max(0, (Data.ReviveAtUtcTicks - Now.Ticks) / (double)TimeSpan.TicksPerSecond) : 0; }
        }
        public string Pose
        {
            get
            {
                if (Data.Collapsed) return "collapsed";
                if (ClosingSeconds > 0) return "close_book";
                if (ReviveSeconds > 0) return "revive";
                if (CoughAnimation) return "cough";
                if (DrunkSeconds > 0) return "drunk";
                if (Smoking)
                {
                    Session s = Data.Active;
                    if (Catalog.Get(s.ProductId).Vape) return s.Age < VapePreparation ? "vape_raise" : "smoke";
                    return s.Age < 1.5 ? "take" : s.Age < 3 ? "place" : s.Age < 4 ? "stow_pack" : s.Age < 5 ? "lighter_out" : s.Age < 6 ? "light" : s.Age < 7 ? "stow_lighter" : "smoke";
                }
                if (PetVisible && FinishSeconds > 0) return finishPose;
                if (EquipmentDisplaySeconds > 0) return "equip";
                if (Working && Data.FatigueAction != "") return Data.FatigueAction;
                return reading ? "read" : String.IsNullOrEmpty(Data.Selected) ? "think" : "count";
            }
        }
        public double Speed
        {
            get
            {
                if (!Smoking || Data.Active.Age < PreparationSeconds || boostRemaining <= 0) return 1;
                return 1 + 3 * Math.Min(1, boostRemaining / 0.6);
            }
        }
        public Stock FindStock(string id) { return Data.Stocks.FirstOrDefault(x => x.Id == id); }
        public int StockOf(string id) { Stock s = FindStock(id); return s == null ? 0 : s.Count; }
        public bool Owns(string id)
        {
            Stock s = FindStock(id);
            return s != null && (s.Count > 0 || s.VapeRemaining > 0 || (Data.Active != null && Data.Active.ProductId == id));
        }
        public List<Stock> Backpack()
        {
            return Data.Stocks.Where(s => Owns(s.Id)).OrderBy(s => s.Id == "rice" ? 0 : s.Favorite ? 1 : 2)
                .ThenBy(s => Catalog.Get(s.Id).Price * (Data.PriceDescending ? -1 : 1)).ThenBy(s => s.Id, StringComparer.Ordinal).ToList();
        }
        public List<Stock> BagPage(int page)
        {
            List<Stock> bag = Backpack();
            int index = Math.Max(0, Math.Min(page, Math.Max(0, (bag.Count - 1) / 8)));
            return bag.Skip(index * 8).Take(8).ToList();
        }
        public string StockDescription(string id)
        {
            Stock s = FindStock(id); Product p = Catalog.Get(id);
            if (s == null || p == null) return "未拥有";
            if (p.Rice) return s.Count + " 碗" + (Data.RiceDeployed ? " · 1 碗已派出" : "");
            if (p.Vape) return s.VapeRemaining > 0
                ? "本支 " + s.VapeRemaining + " 口\n备用 " + s.Count + " 支 · " + VapeBattery(id).ToString("0") + "%"
                : "未开 " + s.Count + " 支\n每支 " + VapeCapacity + " 口";
            return "整包 " + s.Count / 20 + " 包\n已开 " + s.Count % 20 + " 支";
        }
        public void ToggleFavorite(string id)
        {
            Stock s = FindStock(id);
            if (s != null && id != "rice") s.Favorite = !s.Favorite;
        }
        public void ToggleWork()
        {
            ClosingSeconds = 0; CountSeconds = 0;
            if (Working)
            {
                Working = false; reading = false; EquipmentDisplaySeconds = 0; StopFatigueAction();
                Notice = Smoking ? "工作已暂停，这根抽完后再休息。" : "工作暂停，计时已保留。双击小人可以抽烟。";
            }
            else
            {
                if (Smoking) PutAwayActive(1);
                Working = true; PetVisible = true; Data.HasWorked = true; reading = true; Data.BreakSeconds = 0; EquipmentDisplaySeconds = 0; FinishSeconds = 0; StopFatigueAction();
                if (Data.Active != null) Data.Active.Suspended = true;
                Notice = Data.Collapsed ? "小人已死亡，复活后才能赚钱。" : "开始工作，当前收益速度 " + (IncomeRate * 100).ToString("0") + "% 。";
            }
        }
        public void EndWork()
        {
            if (ClosingSeconds > 0) return;
            bool closeBook = PetVisible && !Smoking && !Data.Collapsed;
            PutAwayActive(1);
            FinishSeconds = 0; ReviveSeconds = 0; CountSeconds = 0;
            ClosingSeconds = closeBook && !Data.Collapsed ? 2 : 0;
            Working = false; PetVisible = ClosingSeconds > 0; reading = true; EquipmentDisplaySeconds = 0; StopFatigueAction();
            Notice = ClosingSeconds > 0 ? "正在收书，随后离开桌面。" : "已结束工作，小人已离开桌面。";
        }
        public bool Buy(string id) { return Buy(id, 1); }
        public bool Buy(string id, int quantity)
        {
            Product p = Catalog.Get(id);
            if (p == null || quantity < 1 || quantity > 999) { Notice = "购买数量需为 1 至 999。"; return false; }
            int cost = p.Price * quantity;
            if (Data.Money < cost) { Notice = "余额不足，还差 " + (cost - Data.Money) + " 元。"; return false; }
            Stock stock = FindStock(id);
            if (stock != null && stock.Count + p.Units * quantity > 100000) { Notice = "库存已达到上限。"; return false; }
            if (stock == null) { stock = new Stock { Id = id }; Data.Stocks.Add(stock); }
            Data.Money -= cost;
            stock.Count += p.Units * quantity;
            Notice = "已购入 " + quantity + (p.Rice ? " 碗 " : p.Vape ? " 支 " : " 包 ") + p.Name + "，已放入背包。";
            return true;
        }
        public bool Equip(string id)
        {
            if (Data.Collapsed) { Notice = "小人死亡，无法装备"; return false; }
            Product p = Catalog.Get(id);
            if (p == null || p.Rice || !Owns(id)) { Notice = "请从背包选择已拥有的烟。"; return false; }
            if (Data.Active != null && Data.Active.ProductId != id) PutAwayActive();
            if (Data.Collapsed) { Notice = "小人死亡，无法装备"; return false; }
            Data.Selected = id;
            FinishSeconds = 0;
            if (Data.Active != null) Data.Active.Suspended = true;
            reading = false;
            StopFatigueAction();
            EquipmentDisplaySeconds = 5;
            Notice = "已装备 " + p.Name + "，举过头顶 5 秒后" + (PetVisible && !Working ? "继续数烟。" : "继续看书。");
            return true;
        }
        public void Unequip()
        {
            PutAwayActive(); Data.Selected = ""; reading = Working || !PetVisible; EquipmentDisplaySeconds = 0; FinishSeconds = 0; StopFatigueAction();
            Notice = reading ? "已卸下烟盒，回到看书状态。" : "已卸下烟盒，没抽的赶紧借。";
        }
        private void PutAwayActive(double? forcedCigaretteAmount = null, bool discardVape = false)
        {
            if (Data.Active == null) return;
            Product p = Catalog.Get(Data.Active.ProductId);
            if (!p.Vape)
            {
                double amount = forcedCigaretteAmount ?? (Data.Active.Burned <= p.Duration / 2 + 0.000001 ? 0.5 : 1);
                Data.Active = null; Drop(false, p.Id); RegisterHealth(amount);
            }
            else
            {
                if (discardVape) { FindStock(p.Id).VapeRemaining = 0; Drop(true, p.Id); }
                Data.Active = null; // Each puff has already been counted, never count it twice.
            }
        }
        public bool DiscardSmoking()
        {
            if (!Smoking) return false;
            Product p = Catalog.Get(Data.Active.ProductId);
            double amount = p.Vape ? Data.Active.Puffs / 20.0 : Data.Active.Burned <= p.Duration / 2 + 0.000001 ? 0.5 : 1;
            PutAwayActive(null, true); reading = Working; EquipmentDisplaySeconds = 0;
            if (!Data.Collapsed) Notice = "已抖掉，本轮计入 " + amount.ToString("0.00") + " 根。";
            return true;
        }
        public bool StartSmoking()
        {
            if (ClosingSeconds > 0) { Notice = "正在收书，请先开始工作。"; return false; }
            if (!PetVisible) { Notice = "请先开始工作，让小人来到桌面。"; return false; }
            if (Data.Collapsed) { Notice = "小人已倒地，等待自动复活，或把大米饭拖到它身上。"; return false; }
            if (DrunkSeconds > 0) { Notice = "大肥鱼醉醺醺的，等缓过来再抽烟。"; return false; }
            if (String.IsNullOrEmpty(Data.Selected)) { Notice = "我兜里真没烟"; return false; }
            if (Data.Active != null) { Data.Active.Suspended = false; ResetContinuousWork(); Data.SmokeFreeSeconds = 0; EquipmentDisplaySeconds = 0; return true; }
            Stock stock = FindStock(Data.Selected); Product p = Catalog.Get(Data.Selected);
            if (stock == null || (stock.Count == 0 && stock.VapeRemaining == 0)) { Notice = "烟已经用完，去商店补货吧。"; return false; }
            if (p.Vape)
            {
                if (stock.VapeRemaining == 0) { stock.Count--; stock.VapeRemaining = VapeCapacity; }
            }
            else stock.Count--;
            Data.Active = new Session { ProductId = Data.Selected };
            FinishSeconds = 0; boostRemaining = 0;
            ResetContinuousWork(); Data.SmokeFreeSeconds = 0; EquipmentDisplaySeconds = 0;
            Notice = p.Vape ? "本轮抽 20 口，结束后回去看书。" : "正在抽烟，单击可加速。";
            return true;
        }
        // A single click never starts another cigarette or vape batch.
        public bool ClickPet()
        {
            if (!Smoking || DrunkSeconds > 0) return false;
            Data.Active.BoostUntil = Data.RuntimeSeconds + 1.2;
            boostRemaining = 1.2;
            return true;
        }
        public void Advance(double seconds, double smokingTimeScale = 1)
        {
            if (Double.IsNaN(seconds) || Double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException("seconds");
            if (Double.IsNaN(smokingTimeScale) || Double.IsInfinity(smokingTimeScale) || smokingTimeScale < 0) throw new ArgumentOutOfRangeException("smokingTimeScale");
            CheckRecovery();
            while (seconds > 0.000001)
            {
                double dt = Math.Min(0.05, seconds); Tick(dt, dt * smokingTimeScale); seconds -= dt;
            }
        }
        private void Tick(double dt, double smokeDt)
        {
            Data.RuntimeSeconds += dt;
            Data.Health.RemoveAll(h => Data.RuntimeSeconds - h.Time >= HealthWindow);
            Data.SmokeFreeSeconds = Smoking ? 0 : Math.Min(600, Math.Round(Data.SmokeFreeSeconds + dt, 6));
            if (Data.SmokeFreeSeconds >= 600) Data.Health.Clear();
            if (!Working)
            {
                Data.BreakSeconds = Math.Min(600, Math.Round(Data.BreakSeconds + dt, 6));
                if (Data.BreakSeconds >= 600) ResetContinuousWork(false);
            }
            if (Working && !Data.Collapsed)
            {
                Data.WorkSeconds += dt;
                Data.IncomeRemainder += dt * IncomeRate / 20;
                int earned = (int)Math.Floor(Data.IncomeRemainder + 0.00000001);
                if (earned > 0) { Data.Money = (int)Math.Min(1000000000L, Data.Money + (long)earned); Data.IncomeRemainder = Math.Max(0, Data.IncomeRemainder - earned); }
                if (!Smoking)
                {
                    double before = Data.ContinuousWorkSeconds;
                    Data.ContinuousWorkSeconds = Math.Round(before + dt, 6);
                    UpdateFatigue(dt, before);
                }
            }
            if (!Smoking || DrunkSeconds > 0) return;
            Session s = Data.Active; Product p = Catalog.Get(s.ProductId);
            double preparation = PreparationSeconds, speed = Speed, period = BreathSeconds, beforeBreath = s.PuffElapsed;
            // The preparation sequence is never click-accelerated. Burn and puffs start after lighting/raising.
            if (s.Age < preparation)
            {
                double used = Math.Min(smokeDt, preparation - s.Age);
                s.Age = Math.Round(s.Age + used, 9); smokeDt -= used;
                if (s.Age + .000001 >= preparation) s.Age = preparation;
            }
            boostRemaining = Math.Max(0, boostRemaining - smokeDt);
            if (smokeDt <= .000001) return;
            s.Age += smokeDt * speed;
            s.PuffElapsed += smokeDt * speed;
            // One exhalation per cycle, after the hand has moved away from the mouth.
            if (beforeBreath + .000001 < period * .55 && s.PuffElapsed + .000001 >= period * .55 && Puffed != null) Puffed();
            if (s.PuffElapsed + .000001 >= period)
            {
                s.PuffElapsed = Math.Max(0, s.PuffElapsed - period);
                if (p.Vape) { PuffVape(); return; }
            }
            if (p.Vape) return;
            s.Burned += smokeDt * speed;
            CheckHalfwayEffect(s, p);
            if (s.Burned + 0.000001 >= p.Duration)
            {
                Data.Completed++; Data.Active = null; reading = Working; Finish(p.Id, "toss"); Drop(false, p.Id); RegisterHealth();
                if (!Data.Collapsed) Notice = Working ? "这根抽完了，回去看书。下次需要再次双击。" : "这根抽完了，继续休息。下次需要再次双击。";
            }
        }
        private void PuffVape()
        {
            Session s = Data.Active; Stock stock = FindStock(s.ProductId);
            s.Puffs++; stock.VapeRemaining--;
            CheckHalfwayEffect(s, Catalog.Get(s.ProductId));
            bool empty = stock.VapeRemaining == 0;
            if (s.Puffs >= 20 || empty)
            {
                Data.Active = null; reading = Working;
                Finish(s.ProductId, empty ? "toss" : "pocket");
                if (empty) Drop(true, s.ProductId);
                Notice = Working ? "这一轮结束，回去看书。双击再抽 20 口。" : "这一轮结束，继续休息。双击再抽 20 口。";
            }
            RegisterHealth(0.05);
        }
        private void Finish(string productId, string pose)
        {
            FinishProductId = productId; finishPose = pose; FinishSeconds = 1.5;
        }
        private void RegisterHealth(double amount = 1)
        {
            double before = RecentCount;
            Data.SmokeFreeSeconds = 0;
            Data.Health.Add(new HealthEntry { Time = Data.RuntimeSeconds, Amount = amount });
            if (RecentCount >= CollapseThreshold && !Data.Collapsed)
            {
                Die("overdose");
                Notice = "小人倒地了。两小时后自动复活，也可以喂一碗大米饭。";
            }
            else if (Coughing)
            {
                if (before <= CoughThreshold) { DrunkSeconds = 20; boostRemaining = 0; }
                Notice = "最近抽得有些多，小人醉醺醺的。";
            }
        }
        private void CheckHalfwayEffect(Session s, Product p)
        {
            if (s.HalfwayEffectTriggered || (p.Vape ? s.Puffs < 10 : s.Burned + .000001 < p.Duration / 2)) return;
            s.HalfwayEffectTriggered = true;
            if (p.Price >= 20 && p.Price <= 40) return;
            effectSession = s;
            TriggerPriceEffect(p.Price, true);
        }
        private void TriggerPriceEffect(int price, bool halfway)
        {
            PriceEffect = price < 20 ? "cough" : "bulb";
            PriceEffectRemaining = price < 20 ? halfway ? 2 : 1 + random.NextDouble() : 5;
            // Coughing uses the same product-colored smoke path as a normal exhalation.
            if (price < 20 && Puffed != null) Puffed();
        }
        private void Die(string reason)
        {
            if (Data.Collapsed) return;
            Data.Collapsed = true; Data.DeathReason = reason; Data.ReviveAtUtcTicks = Now.AddSeconds(RecoverySeconds).Ticks;
            Data.Active = null; Data.Selected = ""; reading = true; StopFatigueAction(); EquipmentDisplaySeconds = 0; FinishSeconds = 0;
            DrunkSeconds = 0; ReviveSeconds = 0; ClosingSeconds = 0;
            Notice = reason == "fatigue" ? "连续工作 90 分钟没有抽烟，小人死亡，收益停止。" : "小人已死亡，收益停止。";
        }
        private void ResetContinuousWork(bool resetBreak = true)
        {
            Data.ContinuousWorkSeconds = 0; Data.NextFatigueAt = 0; StopFatigueAction();
            if (resetBreak) Data.BreakSeconds = 0;
        }
        private void StopFatigueAction() { Data.FatigueAction = ""; Data.FatigueSeconds = 0; }
        private void StartFatigueAction(string action)
        {
            Data.FatigueAction = action; Data.FatigueSeconds = 20;
            Data.NextFatigueAt = Data.ContinuousWorkSeconds + (action == "smash" ? random.Next(180, 301) : random.Next(60, 181));
        }
        private void UpdateFatigue(double dt, double before)
        {
            double current = Data.ContinuousWorkSeconds;
            if (current >= 5400) { Die("fatigue"); return; }
            if (Data.FatigueSeconds > 0)
            {
                Data.FatigueSeconds = Math.Max(0, Data.FatigueSeconds - dt);
                if (Data.FatigueSeconds <= 0.000001) StopFatigueAction();
            }
            if (before < 1800 && current >= 1800) { StartFatigueAction("crave"); return; }
            if (before < 3600 && current >= 3600) { StopFatigueAction(); StartFatigueAction("smash"); return; }
            if (current >= 3300 && current < 3600) { StopFatigueAction(); return; }
            if (Data.FatigueAction != "" || current < 1800 || current < Data.NextFatigueAt) return;
            if (current < 3300 && current + 20 <= 3300)
                StartFatigueAction(new[] { "crave", "walk", "sleep" }[random.Next(3)]);
            else if (current >= 3600 && current + 20 <= 5400) StartFatigueAction("smash");
        }
        public void AdvancePresentation(double seconds)
        {
            if (Double.IsNaN(seconds) || Double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException("seconds");
            DrunkSeconds = Math.Max(0, Math.Round(DrunkSeconds - seconds, 6));
            ReviveSeconds = Math.Max(0, Math.Round(ReviveSeconds - seconds, 6));
            if (ClosingSeconds > 0)
            {
                ClosingSeconds = Math.Max(0, Math.Round(ClosingSeconds - seconds, 6));
                if (ClosingSeconds == 0) { PetVisible = false; Notice = "已结束工作，小人已离开桌面。"; }
            }
            CountSeconds = Pose == "count" ? CountSeconds + seconds : 0;
            FinishSeconds = Math.Max(0, FinishSeconds - seconds);
            if (EquipmentDisplaySeconds > 0)
            {
                EquipmentDisplaySeconds = Math.Max(0, EquipmentDisplaySeconds - seconds);
                if (EquipmentDisplaySeconds == 0) reading = Working || !PetVisible;
            }
            if (!Smoking || Data.Active != effectSession)
            {
                effectSession = Smoking ? Data.Active : null; PriceEffect = ""; PriceEffectRemaining = 0;
                effectWait = 2 + random.NextDouble() * 6;
            }
            if (!Smoking || DrunkSeconds > 0 || Data.Active.Age < PreparationSeconds) return;
            int price = Catalog.Get(Data.Active.ProductId).Price;
            if (price >= 20 && price <= 40) { PriceEffect = ""; PriceEffectRemaining = 0; return; }
            while (seconds > 0.000001)
            {
                if (PriceEffectRemaining > 0)
                {
                    double used = Math.Min(seconds, PriceEffectRemaining); seconds -= used; PriceEffectRemaining -= used;
                    if (PriceEffectRemaining <= 0.000001) { PriceEffectRemaining = 0; PriceEffect = ""; effectWait = 4 + random.NextDouble() * 12; }
                }
                else
                {
                    double used = Math.Min(seconds, effectWait); seconds -= used; effectWait -= used;
                    if (effectWait <= 0.000001) TriggerPriceEffect(price, false);
                }
            }
        }
        private void Drop(bool vape, string productId)
        {
            Debris d = new Debris { Id = Data.NextDebrisId++, Vape = vape, ProductId = productId };
            Data.Litter.Add(d);
            // The desktop adapter chooses a point across connected monitor working areas.
            if (Dropped != null) Dropped(d);
        }
        public bool Clean(int id)
        {
            Debris d = Data.Litter.FirstOrDefault(x => x.Id == id);
            if (d == null) return false;
            if (++d.Clicks >= 5) { Data.Litter.Remove(d); return true; }
            return false;
        }
        public bool DeployRice()
        {
            if (!PetVisible) { Notice = "需要让小人出现在桌面上，请先点击“开始工作”。"; return false; }
            if (StockOf("rice") < 1) { Notice = "请先在商店购买大米饭。"; return false; }
            if (Data.RiceDeployed) { Notice = "已有一碗大米饭在桌面，抓住它拖到小人身上。"; return false; }
            Data.RiceDeployed = true;
            Data.RiceCaught = false; Data.RiceSecondsRemaining = 20; Data.RiceCaughtDeadlineUtcTicks = 0;
            Notice = "大米饭跑出来了！20 秒内抓住它，再于 10 秒内拖到死亡小人身上。";
            return true;
        }
        public bool GrabRice()
        {
            if (AdvanceRice(0)) return false;
            if (!PetVisible || !Data.RiceDeployed) return false;
            if (!Data.RiceCaught)
            {
                Data.RiceCaught = true; Data.RiceSecondsRemaining = 10;
                Data.RiceCaughtDeadlineUtcTicks = Now.AddSeconds(10).Ticks;
            }
            return true;
        }
        // Real desktop time: deliberately separate from the accelerated simulation clock.
        public bool AdvanceRice(double seconds)
        {
            if (Double.IsNaN(seconds) || Double.IsInfinity(seconds) || seconds < 0) throw new ArgumentOutOfRangeException("seconds");
            if (!Data.RiceDeployed || (!PetVisible && !Data.RiceCaught)) return false;
            Data.RiceSecondsRemaining = Math.Max(0, Data.RiceSecondsRemaining - seconds);
            if (Data.RiceCaught && Data.RiceCaughtDeadlineUtcTicks > 0)
                Data.RiceSecondsRemaining = Math.Min(Data.RiceSecondsRemaining,
                    Math.Max(0, (Data.RiceCaughtDeadlineUtcTicks - Now.Ticks) / (double)TimeSpan.TicksPerSecond));
            if (Data.RiceSecondsRemaining > 0.000001) return false;
            if (!Data.RiceCaught)
            {
                FindStock("rice").Count--;
                Notice = "20 秒内没有抓住，大米饭消失了，消耗 1 碗。";
            }
            else Notice = "抓住后 10 秒内没有喂到，大米饭已回到背包，没有消耗。";
            ClearRice(); return true;
        }
        private void ClearRice()
        {
            Data.RiceDeployed = false; Data.RiceCaught = false; Data.RiceSecondsRemaining = 20; Data.RiceCaughtDeadlineUtcTicks = 0;
        }
        public bool FeedRice()
        {
            if (AdvanceRice(0)) return false;
            if (!PetVisible || !Data.RiceDeployed || !Data.RiceCaught || !Data.Collapsed || StockOf("rice") < 1)
            { Notice = "把饭拖到死亡的小人身上才会消耗，小人现在不需要复活。"; return false; }
            FindStock("rice").Count--; ClearRice();
            Recover(); Notice = "吃下大米饭，小人复活了！"; return true;
        }
        public void CheckRecovery()
        {
            if (Data.Collapsed && Data.ReviveAtUtcTicks > 0 && Now.Ticks >= Data.ReviveAtUtcTicks)
            {
                Recover(); ClearRice();
                Notice = "两小时到了，小人已自动复活。未使用的大米饭仍在背包。";
            }
        }
        private void Recover()
        {
            Data.Collapsed = false; Data.DeathReason = ""; Data.ReviveAtUtcTicks = 0; Data.Health.Clear(); Data.Recent.Clear(); Data.RecentHalf.Clear(); Data.VapeHealthPuffs = 0;
            ResetContinuousWork();
            if (Data.Active != null) Data.Active.Suspended = true;
            reading = true; DrunkSeconds = 0; ClosingSeconds = 0; ReviveSeconds = 3;
        }
    }

    // Six deliberate vertical legs (three up/down cycles) within one mouse hold.
    public class VerticalShake
    {
        private int anchorX, extremeY, direction, legs;
        private double lastMovement;
        private bool active;
        public void Begin(int x, int y, double seconds)
        {
            anchorX = x; extremeY = y; direction = 0; legs = 0; lastMovement = seconds; active = true;
        }
        public void End() { active = false; }
        public bool Move(int x, int y, double seconds)
        {
            if (!active) return false;
            if (Math.Abs(x - anchorX) > 100 || seconds - lastMovement > 1.2)
            { Begin(x, y, seconds); return false; }
            int dy = y - extremeY;
            if (direction == 0)
            {
                if (Math.Abs(dy) >= 24) { direction = Math.Sign(dy); extremeY = y; legs = 1; lastMovement = seconds; }
            }
            else if (dy * direction > 0) { extremeY = y; lastMovement = seconds; }
            else if (dy * direction <= -24)
            {
                direction = -direction; extremeY = y; legs++; lastMovement = seconds;
                if (legs >= 6) { End(); return true; }
            }
            return false;
        }
    }
}
