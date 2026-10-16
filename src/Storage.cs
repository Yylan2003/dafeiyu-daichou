using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace SmokeSimulator
{
    public static class Storage
    {
        private static bool Finite(double x) { return !Double.IsNaN(x) && !Double.IsInfinity(x); }
        public static SaveData Load(string path, out string warning)
        {
            warning = null;
            if (!File.Exists(path)) return new SaveData { StartupFunds200Applied = true };
            try
            {
                SaveData d;
                using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null }))
                    d = (SaveData)new XmlSerializer(typeof(SaveData)).Deserialize(reader);
                if (d.Version >= 1 && d.Version <= 4)
                {
                    string backup = path + ".v" + d.Version + ".bak";
                    if (!File.Exists(backup)) File.Copy(path, backup);
                    if (d.Version == 1) MigrateV1(d);
                    if (d.Version == 2) MigrateV2(d);
                    if (d.Version == 3) MigrateV3(d);
                    MigrateV4(d);
                }
                Validate(d);
                if (!d.StartupFunds200Applied)
                {
                    if (d.Money == 0) d.Money = SaveData.InitialMoney;
                    d.StartupFunds200Applied = true;
                }
                return d;
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException || ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is XmlException)) throw;
                string backup = path + ".corrupt-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
                try { File.Copy(path, backup); }
                catch (Exception backupError) { throw new IOException("无法备份原存档，已停止加载以避免覆盖。", backupError); }
                warning = "存档无法读取，已保留备份：" + backup + "\n本次从初始状态开始。";
                return new SaveData { StartupFunds200Applied = true };
            }
        }
        private static string NewProductId(string id)
        {
            switch (id)
            {
                case "classic": return "parliament";
                case "mint": return "mevius";
                case "slim_cn": return "yingchun";
                case "slim_us": return "kent";
                case "classic_jp": return "camel";
                case "long_uk": return "dunhill";
                default: return id;
            }
        }
        private static void MigrateV4(SaveData d)
        {
            if (d.Stocks == null || d.Stocks.Any(s => s == null)) throw new ArgumentException("Invalid legacy inventory");
            foreach (Stock s in d.Stocks)
            {
                s.Id = NewProductId(s.Id);
                if (s.Id == "vape" && s.VapeRemaining > 0 && s.VapeRemaining <= 100) s.VapeRemaining += Engine.VapeCapacity - 100;
            }
            d.Selected = NewProductId(d.Selected);
            if (d.Active != null) d.Active.ProductId = NewProductId(d.Active.ProductId);
            d.Version = 5;
        }
        private static void MigrateV3(SaveData d)
        {
            if (d.Health == null || d.Health.Any(h => h == null)) throw new ArgumentException("Invalid legacy health history");
            d.SmokeFreeSeconds = d.Active != null ? 0 : d.Health.Count == 0 ? 600 : Math.Max(0, Math.Min(600, d.RuntimeSeconds - d.Health.Max(h => h.Time)));
            if (d.SmokeFreeSeconds >= 600) d.Health.Clear();
            if (d.Collapsed) { d.Selected = ""; d.Active = null; }
            d.Version = 4;
        }
        private static void MigrateV2(SaveData d)
        {
            if (d.Recent == null || d.RecentHalf == null || d.VapeHealthPuffs < 0 || d.VapeHealthPuffs > 9)
                throw new ArgumentException("Invalid legacy health history");
            d.Health = d.Recent.Select(t => new HealthEntry { Time = t, Amount = 1 }).Concat(
                d.RecentHalf.Select(t => new HealthEntry { Time = t, Amount = 0.5 })).ToList();
            if (d.VapeHealthPuffs > 0) d.Health.Add(new HealthEntry { Time = d.RuntimeSeconds, Amount = d.VapeHealthPuffs / 20.0 });
            d.Recent.Clear(); d.RecentHalf.Clear(); d.VapeHealthPuffs = 0;
            d.IncomeRemainder = (d.WorkSeconds % 60) / 60;
            d.Version = 3;
        }
        private static void MigrateV1(SaveData d)
        {
            if (d.Stocks == null || d.Stocks.Any(s => s == null)) throw new ArgumentException("Invalid legacy inventory");
            d.Version = 2;
            if (d.Collapsed) d.ReviveAtUtcTicks = DateTime.UtcNow.AddHours(2).Ticks;
            if (d.Active != null)
            {
                Product p = Catalog.Get(d.Active.ProductId);
                if (p != null && p.Vape)
                {
                    Stock s = d.Stocks.FirstOrDefault(x => x.Id == p.Id);
                    if (s == null) { s = new Stock { Id = p.Id }; d.Stocks.Add(s); }
                    s.VapeRemaining = 100 - d.Active.Puffs;
                    d.Active.Puffs %= 20;
                }
                d.Active.Suspended = true;
                d.Selected = d.Active.ProductId;
            }
            if (!d.Stocks.Any(s => s.Id == d.Selected && (s.Count > 0 || s.VapeRemaining > 0)) && d.Active == null) d.Selected = "";
        }
        private static void Validate(SaveData d)
        {
            // Older XML keeps the 100% initializer; an invalid preference must not erase inventory.
            if (d != null && (d.PetScalePercent < 50 || d.PetScalePercent > 200)) d.PetScalePercent = 100;
            if (d == null || d.Version != 5 || d.Money < 0 || d.Money > 1000000000 ||
                !Finite(d.SmokeFreeSeconds) || d.SmokeFreeSeconds < 0 || d.SmokeFreeSeconds > 600 ||
                !Finite(d.WorkSeconds) || d.WorkSeconds < 0 || !Finite(d.RuntimeSeconds) || d.RuntimeSeconds < 0 ||
                !Finite(d.ContinuousWorkSeconds) || d.ContinuousWorkSeconds < 0 || d.ContinuousWorkSeconds > 5400.1 ||
                !Finite(d.BreakSeconds) || d.BreakSeconds < 0 || d.BreakSeconds > 600 ||
                !Finite(d.IncomeRemainder) || d.IncomeRemainder < 0 || d.IncomeRemainder >= 1 ||
                !Finite(d.NextFatigueAt) || d.NextFatigueAt < 0 || !Finite(d.FatigueSeconds) || d.FatigueSeconds < 0 || d.FatigueSeconds > 20 ||
                !new[] { "", "crave", "walk", "sleep", "smash" }.Contains(d.FatigueAction) || d.Health == null ||
                d.Avatar < 0 || d.Avatar > 2 || (d.Selected != "" && (!Catalog.Exists(d.Selected) || d.Selected == "rice")) ||
                d.Stocks == null || d.Recent == null || d.RecentHalf == null || d.Litter == null || d.NextDebrisId < 1 ||
                !Finite(d.RiceSecondsRemaining) || d.RiceSecondsRemaining <= 0 || d.RiceSecondsRemaining > (d.RiceCaught ? 10 : 20) ||
                (d.RiceCaught && !d.RiceDeployed) ||
                d.RiceCaughtDeadlineUtcTicks < 0 || d.RiceCaughtDeadlineUtcTicks > DateTime.MaxValue.Ticks ||
                d.VapeHealthPuffs < 0 || d.VapeHealthPuffs > 9 || d.Completed < 0 ||
                d.ReviveAtUtcTicks < 0 || d.ReviveAtUtcTicks > DateTime.MaxValue.Ticks || (d.Collapsed && d.ReviveAtUtcTicks == 0))
                throw new ArgumentException("Invalid save state");
            if (d.Stocks.Any(s => s == null || !Catalog.Exists(s.Id) || s.Count < 0 || s.Count > 100020 || s.VapeRemaining < 0 || s.VapeRemaining > Engine.VapeCapacity || (!Catalog.Get(s.Id).Vape && s.VapeRemaining != 0)) ||
                d.Stocks.Select(s => s.Id).Distinct().Count() != d.Stocks.Count ||
                d.Recent.Any(t => !Finite(t) || t < 0 || t > d.RuntimeSeconds) ||
                d.RecentHalf.Any(t => !Finite(t) || t < 0 || t > d.RuntimeSeconds) ||
                d.Health.Any(h => h == null || !Finite(h.Time) || h.Time < 0 || h.Time > d.RuntimeSeconds ||
                    !Finite(h.Amount) || h.Amount <= 0 || h.Amount > 1) ||
                d.Litter.Any(l => l == null || l.Id < 1 || l.Id >= d.NextDebrisId || l.Clicks < 0 || l.Clicks > 4) ||
                d.Litter.Select(l => l.Id).Distinct().Count() != d.Litter.Count ||
                (d.RiceDeployed && !d.Stocks.Any(s => s.Id == "rice" && s.Count > 0)))
                throw new ArgumentException("Invalid inventory or history");
            Session a = d.Active;
            if (a != null && (!Catalog.Exists(a.ProductId) || a.ProductId == "rice" || a.ProductId != d.Selected ||
                !Finite(a.Burned) || a.Burned < 0 || !Finite(a.Age) || a.Age < 0 ||
                !Finite(a.BoostUntil) || a.BoostUntil < 0 || !Finite(a.PuffElapsed) || a.PuffElapsed < 0 ||
                a.Puffs < 0 || a.Puffs >= 20 ||
                (!Catalog.Get(a.ProductId).Vape && a.Burned >= Catalog.Get(a.ProductId).Duration) ||
                (Catalog.Get(a.ProductId).Vape && !d.Stocks.Any(s => s.Id == a.ProductId && s.VapeRemaining > 0))))
                throw new ArgumentException("Invalid active session");
            d.Recent.RemoveAll(t => d.RuntimeSeconds - t >= Engine.HealthWindow);
            d.RecentHalf.RemoveAll(t => d.RuntimeSeconds - t >= Engine.HealthWindow);
            d.Health.RemoveAll(h => d.RuntimeSeconds - h.Time >= Engine.HealthWindow);
        }
        public static void Save(string path, SaveData data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            string temp = path + ".tmp";
            using (FileStream stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new XmlSerializer(typeof(SaveData)).Serialize(stream, data); stream.Flush(true);
            }
            if (File.Exists(path)) File.Replace(temp, path, path + ".bak"); else File.Move(temp, path);
        }
    }
}
