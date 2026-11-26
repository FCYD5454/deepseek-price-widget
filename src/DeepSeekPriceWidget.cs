using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

[assembly: AssemblyTitle("DeepSeek Price Widget")]
[assembly: AssemblyDescription("DeepSeek Price Widget")]
[assembly: AssemblyProduct("DeepSeek Price Widget")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyVersion("3.6.0.0")]
[assembly: AssemblyFileVersion("3.6.0.0")]

class Holiday
{
    public int Year;
    public string Name;
    public string Start;
    public string End;
    public Holiday(int year, string name, string start, string end)
    {
        Year = year; Name = name; Start = start; End = end;
    }
}

static class Program
{
    static string appDir;
    static string assetsDir;
    static string dataDir;
    static string logsDir;
    static string configPath;
    static string historyPath;
    static string priceUrl = "https://api-docs.deepseek.com/zh-cn/quick_start/pricing.md";
    static string statusFeedUrl = "https://status.deepseek.com/feed.rss?lang=en";
    static string fxUrl = "https://api.frankfurter.app/latest?from=CNY";
    static readonly CultureInfo ZhTw = CultureInfo.GetCultureInfo("zh-TW");

    static List<Holiday> holidays;
    static List<int> holYears;
    static string holYearsText = "";
    static Dictionary<string, Dictionary<string, string[]>> prices;
    static string corner = "bottomright";
    static string forceHolidayDate = "";
    static int consecutiveFailures;
    static string holidaySource = "";
    static string priceUpdateDate = "";
    static bool locked;
    static int estModelIdx;
    static string estInputText = "100000";
    static string estOutputText = "20000";
    static string estHitText = "80";
    static string currency = "CNY";
    static bool autoCurrency;
    static Dictionary<string, double> fxRates = new Dictionary<string, double>();
    static string fxDate = "";
    static bool fxFetching;
    static string themeMode = "auto";
    static Dictionary<string, string> langStrings = new Dictionary<string, string>();
    static string languageMode = "auto";
    static string activeLangCode = "zh-HK";
    static CultureInfo langCulture = ZhTw;
    static List<KeyValuePair<string, string>> availableLangs = new List<KeyValuePair<string, string>>();
    static string serviceStatus = "unknown";
    static string serviceStatusTitle = "";
    static List<Dictionary<string, object>> priceHistory = new List<Dictionary<string, object>>();
    static IntPtr hotkeyHwnd = IntPtr.Zero;
    static HwndSource hotkeySource;
    static bool hotkeyRegistered;
    const int HotkeyId = 0x4457;
    static double? posLeft;
    static double? posTop;
    static DateTime? testDate;
    static TimeZoneInfo tz;
    static bool smoke;

    static Window win;
    static Border rootBorder;
    static Border badge;
    static TextBlock badgeText;
    static TextBlock clockText;
    static TextBlock dateText;
    static TextBlock summaryText;
    static TextBlock countdownText;
    static StackPanel detailPanel;
    static TextBlock headOff;
    static TextBlock headPeak;
    static TextBlock priceNoteText;
    static TextBlock holidayNoteText;
    static TextBlock pinHint;
    static TextBlock staleText;
    static TextBlock lockText;
    static TextBlock statusText;
    static TextBlock hkdText;
    static TextBlock unitText;
    static TextBlock fixedSearchText;
    static TextBlock fixedOcrText;
    static TextBlock lblFlashHit;
    static TextBlock lblFlashMiss;
    static TextBlock lblFlashOut;
    static TextBlock lblProHit;
    static TextBlock lblProMiss;
    static TextBlock lblProOut;
    static TextBlock lblFees;
    static TextBlock lblStatus;
    static TextBlock notePeak1;
    static TextBlock notePeak2;
    static readonly string[] cellNames = new string[] {
        "FlashHitOff", "FlashHitPeak", "FlashMissOff", "FlashMissPeak", "FlashOutOff", "FlashOutPeak",
        "ProHitOff", "ProHitPeak", "ProMissOff", "ProMissPeak", "ProOutOff", "ProOutPeak"
    };
    static Dictionary<string, Border> cells = new Dictionary<string, Border>();
    static MenuItem cornerRoot;
    static Dictionary<string, MenuItem> cornerItems = new Dictionary<string, MenuItem>();
    static MenuItem themeRoot;
    static MenuItem themeAuto;
    static MenuItem themeLight;
    static MenuItem themeDark;
    static MenuItem languageRoot;
    static MenuItem currencyRoot;
    static MenuItem holidayItem;
    static MenuItem lockItem;
    static MenuItem estItem;
    static MenuItem refreshItem;
    static MenuItem reloadHolidaysItem;
    static MenuItem openItem;
    static MenuItem historyItem;
    static MenuItem helpItem;
    static MenuItem exitItem;
    static Forms.NotifyIcon tray;
    static Forms.ToolStripItem trayShowItem;
    static Forms.ToolStripItem trayRefreshItem;
    static Forms.ToolStripItem trayExitItem;
    static DispatcherTimer uiTimer;
    static DispatcherTimer refreshTimer;
    static bool fetching;
    static bool expanded;
    static bool pinned;
    static bool menuOpen;
    static bool mouseActive;
    static bool dragged;
    static string lastPeriod;
    static string lastCountdownKey = "";
    static Drawing.Point downScreen;
    static double downLeft;
    static double downTop;
    static double lastDragScale = 1.0;
    static Window estWindow;
    static bool priceFetching;
    static bool priceFetchFailed;
    static bool hideHintShown;

    static Brush brushPeak;
    static Brush brushOff;
    static Brush clearBrush;
    static Brush badgePeak;
    static Brush badgeOff;
    static Brush headNormal;
    static Brush headActive;
    static Brush pinBrush;
    static Brush borderNormal;
    static Brush statusRed;

    static DispatcherTimer smokeTimer;
    static int smokeStep;
    static double smokeH1;
    static double smokeH2;
    static double smokeH3;
    static bool trayOk;

    [STAThread]
    static int Main(string[] args)
    {
        appDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        assetsDir = Path.Combine(appDir, "assets");
        dataDir = Path.Combine(appDir, "data");
        logsDir = Path.Combine(appDir, "logs");
        try
        {
            Directory.CreateDirectory(assetsDir);
            Directory.CreateDirectory(dataDir);
            Directory.CreateDirectory(logsDir);
        }
        catch
        {
        }
        MigrateFile("widget-config.json", dataDir);
        MigrateFile("price-history.json", dataDir);
        MigrateFile("holidays.json", dataDir);
        MigrateFile("widget.log", logsDir);
        configPath = Path.Combine(dataDir, "widget-config.json");
        historyPath = Path.Combine(dataDir, "price-history.json");
        try { ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12; }
        catch { }
        RotateLog();
        Log("app start v" + Assembly.GetExecutingAssembly().GetName().Version.ToString() + (args.Length > 0 ? " mode=" + args[0] : ""));
        LoadCurrencies();
        LoadFxProviders();
        string mode = "";
        if (args.Length > 0) { mode = args[0].ToLowerInvariant(); }
        try
        {
            if (mode == "--selftest") { return RunSelfTest(); }
            if (mode == "--fetchtest") { return RunFetchTest(); }
            if (mode == "--statustest") { return RunStatusTest(); }
            if (mode == "--fxtest") { return RunFxTest(); }
            if (mode == "--smoke") { smoke = true; }
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].ToLowerInvariant() == "--testtime" && i + 1 < args.Length)
                {
                    DateTime parsed;
                    string[] fmts = new string[] { "yyyy-MM-ddTHH:mm", "yyyy-MM-dd HH:mm", "yyyy-MM-ddTHH:mm:ss" };
                    if (DateTime.TryParseExact(args[i + 1], fmts, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                    {
                        testDate = parsed;
                    }
                }
            }
            return RunGui();
        }
        catch (Exception ex)
        {
            try { File.WriteAllText(Path.Combine(appDir, "error.log"), ex.ToString(), new UTF8Encoding(true)); }
            catch { }
            if (!smoke && mode != "--selftest" && mode != "--fetchtest")
            {
                MessageBox.Show(ex.Message, S("app.title", "DeepSeek API 價格"));
            }
            return 1;
        }
    }

    static Dictionary<string, Dictionary<string, string[]>> NewPrices()
    {
        var d = new Dictionary<string, Dictionary<string, string[]>>();
        d["flash"] = new Dictionary<string, string[]>();
        d["flash"]["hit"] = new string[] { "0.02", "0.04" };
        d["flash"]["miss"] = new string[] { "1", "2" };
        d["flash"]["out"] = new string[] { "4", "8" };
        d["pro"] = new Dictionary<string, string[]>();
        d["pro"]["hit"] = new string[] { "0.15", "0.30" };
        d["pro"]["miss"] = new string[] { "4.5", "9.0" };
        d["pro"]["out"] = new string[] { "13.5", "27" };
        return d;
    }

    static void AddHoliday(int year, string name, string start, string end)
    {
        holidays.Add(new Holiday(year, name, start, end));
    }

    static void InitHolidays()
    {
        holidays = new List<Holiday>();
        AddHoliday(2026, "元旦", "01-01", "01-03");
        AddHoliday(2026, "春節", "02-15", "02-23");
        AddHoliday(2026, "清明節", "04-04", "04-06");
        AddHoliday(2026, "勞動節", "05-01", "05-05");
        AddHoliday(2026, "端午節", "06-19", "06-21");
        AddHoliday(2026, "中秋節", "09-25", "09-27");
        AddHoliday(2026, "國慶節", "10-01", "10-07");
        AddHoliday(2027, "元旦", "01-01", "01-03");
        AddHoliday(2027, "春節", "02-05", "02-13");
        AddHoliday(2027, "清明節", "04-03", "04-05");
        AddHoliday(2027, "勞動節", "05-01", "05-05");
        AddHoliday(2027, "端午節", "06-09", "06-09");
        AddHoliday(2027, "中秋節", "09-15", "09-15");
        AddHoliday(2027, "國慶節", "10-01", "10-07");
        holidaySource = "source.builtin";
        ComputeHolidayYears();
    }

    static void ComputeHolidayYears()
    {
        holYears = new List<int>();
        for (int i = 0; i < holidays.Count; i++)
        {
            if (!holYears.Contains(holidays[i].Year)) { holYears.Add(holidays[i].Year); }
        }
        holYears.Sort();
        holYearsText = string.Join("、", holYears.ConvertAll(delegate (int y) { return y.ToString(); }).ToArray());
    }

    static bool ForceHolidayActive(DateTime dt)
    {
        return forceHolidayDate == dt.ToString("yyyy-MM-dd");
    }

    static void WriteHolidaysFile(string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("[");
        for (int i = 0; i < holidays.Count; i++)
        {
            Holiday h = holidays[i];
            string comma = i < holidays.Count - 1 ? "," : "";
            sb.AppendLine("  {\"year\":" + h.Year.ToString() + ",\"name\":\"" + h.Name + "\",\"start\":\"" + h.Start + "\",\"end\":\"" + h.End + "\"}" + comma);
        }
        sb.AppendLine("]");
        WriteFileAtomic(path, sb.ToString());
    }

    static void LoadHolidays()
    {
        InitHolidays();
        string path = Path.Combine(dataDir, "holidays.json");
        try
        {
            if (!File.Exists(path))
            {
                WriteHolidaysFile(path);
                Log("holidays: created default holidays.json (" + holidays.Count.ToString() + " entries)");
                return;
            }
            var ser = new JavaScriptSerializer();
            var arr = ser.Deserialize<List<Dictionary<string, object>>>(File.ReadAllText(path));
            var list = new List<Holiday>();
            if (arr != null)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    Dictionary<string, object> d = arr[i];
                    if (d == null || !d.ContainsKey("year")) { continue; }
                    int year = Convert.ToInt32(d["year"]);
                    string name = d.ContainsKey("name") ? Convert.ToString(d["name"]) : "";
                    string s = d.ContainsKey("start") ? Convert.ToString(d["start"]) : "";
                    string e = d.ContainsKey("end") ? Convert.ToString(d["end"]) : "";
                    if (s != null && e != null && s.Length == 5 && e.Length == 5)
                    {
                        list.Add(new Holiday(year, name, s, e));
                    }
                }
            }
            if (list.Count > 0)
            {
                holidays = list;
                holidaySource = "source.file";
                ComputeHolidayYears();
                Log("holidays: loaded holidays.json (" + list.Count.ToString() + " entries)");
            }
            else
            {

                holidaySource = "source.empty";
                Log("holidays: holidays.json has no valid entries, using builtin");
            }
        }
        catch (Exception ex)
        {

            holidaySource = "source.error";
            Log("holidays: load failed: " + ex.Message);
        }
    }

    static TimeZoneInfo GetTz()
    {
        if (tz == null)
        {
            try { tz = TimeZoneInfo.FindSystemTimeZoneById("China Standard Time"); }
            catch { tz = null; }
        }
        return tz;
    }

    static DateTime BeijingNow()
    {
        TimeZoneInfo z = GetTz();
        if (z != null) { return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, z); }
        return DateTime.UtcNow.AddHours(8);
    }

    static DateTime TargetNow()
    {
        if (testDate.HasValue) { return testDate.Value; }
        return BeijingNow();
    }

    static bool IsWeekend(DateTime dt)
    {
        return dt.DayOfWeek == DayOfWeek.Saturday || dt.DayOfWeek == DayOfWeek.Sunday;
    }

    static string HolidayName(DateTime dt)
    {
        for (int i = 0; i < holidays.Count; i++)
        {
            Holiday h = holidays[i];
            if (h.Year != dt.Year) { continue; }
            DateTime s = DateTime.ParseExact(h.Year.ToString() + "-" + h.Start, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            DateTime e = DateTime.ParseExact(h.Year.ToString() + "-" + h.End, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (dt.Date >= s.Date && dt.Date <= e.Date) { return h.Name; }
        }
        return null;
    }

    static bool IsWorkday(DateTime dt)
    {
        if (IsWeekend(dt)) { return false; }
        if (HolidayName(dt) != null) { return false; }
        return true;
    }

    static string GetPeriod(DateTime dt)
    {
        if (ForceHolidayActive(dt)) { return "offpeak"; }
        if (IsWeekend(dt)) { return "offpeak"; }
        if (HolidayName(dt) != null) { return "offpeak"; }
        TimeSpan t = dt.TimeOfDay;
        if ((t >= TimeSpan.FromHours(9) && t < TimeSpan.FromHours(12)) || (t >= TimeSpan.FromHours(14) && t < TimeSpan.FromHours(18)))
        {
            return "peak";
        }
        return "offpeak";
    }

    static DateTime? GetNextChange(DateTime now)
    {
        if (!ForceHolidayActive(now))
        {
            if (IsWorkday(now))
            {
                TimeSpan t = now.TimeOfDay;
                if (t < TimeSpan.FromHours(9)) { return now.Date.AddHours(9); }
                if (t < TimeSpan.FromHours(12)) { return now.Date.AddHours(12); }
                if (t < TimeSpan.FromHours(14)) { return now.Date.AddHours(14); }
                if (t < TimeSpan.FromHours(18)) { return now.Date.AddHours(18); }
            }
        }
        DateTime d = now.Date.AddDays(1);
        for (int i = 0; i < 400; i++)
        {
            DateTime c = d.AddDays(i);
            if (IsWorkday(c)) { return c.AddHours(9); }
        }
        return null;
    }

    static string FormatCountdown(TimeSpan span)
    {
        if (span.TotalSeconds < 60) { return S("countdown.lessMinute", "少於 1 分鐘"); }
        if (span.TotalDays >= 1)
        {
            int d = (int)Math.Floor(span.TotalDays);
            if (span.Hours == 0) { return SF("countdown.days", "{0} 天", d); }
            return SF("countdown.daysHours", "{0} 天 {1} 小時", d, span.Hours);
        }
        if (span.TotalMinutes >= 60)
        {
            int h = (int)Math.Floor(span.TotalHours);
            if (span.Minutes == 0) { return SF("countdown.hours", "{0} 小時", h); }
            return SF("countdown.hoursMin", "{0} 小時 {1} 分", h, span.Minutes);
        }
        return SF("countdown.minutes", "{0} 分鐘", (int)Math.Ceiling(span.TotalMinutes));
    }

    static bool IsPriceStale(DateTime now)
    {
        if (consecutiveFailures >= 3) { return true; }
        if (string.IsNullOrEmpty(priceUpdateDate)) { return true; }
        DateTime upd;
        if (!DateTime.TryParseExact(priceUpdateDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out upd))
        {
            return true;
        }
        return (now.Date - upd.Date).TotalDays > 3;
    }

    static string[] CurrencyCodes = new string[] { "CNY", "HKD", "USD", "EUR", "JPY", "GBP", "AUD", "CAD", "CHF", "SGD", "KRW", "THB", "BRL", "TWD", "VND", "RUB" };

    class CurrencyDef
    {
        public string Code;
        public string Symbol;
        public string Name;
    }

    class FxProvider
    {
        public string Url;
        public string Container;
        public bool LowerKeys;
    }

    static List<CurrencyDef> currencyDefs = new List<CurrencyDef>();
    static Dictionary<string, string> languageDefaults = new Dictionary<string, string>();
    static List<FxProvider> fxProviders = BuildDefaultProviders();

    static List<FxProvider> BuildDefaultProviders()
    {
        var list = new List<FxProvider>();
        var p1 = new FxProvider();
        p1.Url = "https://open.er-api.com/v6/latest/CNY";
        p1.Container = "rates";
        p1.LowerKeys = false;
        list.Add(p1);
        var p2 = new FxProvider();
        p2.Url = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies/cny.json";
        p2.Container = "cny";
        p2.LowerKeys = true;
        list.Add(p2);
        var p3 = new FxProvider();
        p3.Url = "https://api.frankfurter.app/latest?from=CNY";
        p3.Container = "rates";
        p3.LowerKeys = false;
        list.Add(p3);
        return list;
    }

    static void WriteDefaultCurrencies(string path)
    {
        string[] symbols = new string[] { "CN¥", "HK$", "US$", "€", "JP¥", "£", "A$", "C$", "CHF ", "S$", "₩", "฿", "R$", "NT$", "₫", "₽" };
        string[] names = new string[] { "人民幣", "港幣", "美元", "歐元", "日圓", "英鎊", "澳幣", "加幣", "瑞士法郎", "新加坡幣", "韓元", "泰銖", "巴西雷亞爾", "新台幣", "越南盾", "俄羅斯盧布" };
        var list = new List<object>();
        for (int i = 0; i < CurrencyCodes.Length; i++)
        {
            var d = new Dictionary<string, object>();
            d["code"] = CurrencyCodes[i];
            d["symbol"] = symbols[i];
            d["name"] = names[i];
            list.Add(d);
        }
        var defaults = new Dictionary<string, object>();
        defaults["zh-HK"] = "HKD";
        defaults["zh-TW"] = "TWD";
        defaults["zh-CN"] = "CNY";
        defaults["ja"] = "JPY";
        defaults["ko"] = "KRW";
        defaults["ru"] = "RUB";
        defaults["pt-BR"] = "BRL";
        defaults["fr"] = "EUR";
        defaults["de"] = "EUR";
        defaults["es"] = "EUR";
        defaults["it"] = "EUR";
        defaults["en"] = "USD";
        var root = new Dictionary<string, object>();
        root["currencies"] = list;
        root["languageDefaults"] = defaults;
        WriteFileAtomic(path, new JavaScriptSerializer().Serialize(root));
    }

    static void LoadCurrencies()
    {
        string path = Path.Combine(dataDir, "currencies.json");
        try
        {
            if (!File.Exists(path)) { WriteDefaultCurrencies(path); }
            var ser = new JavaScriptSerializer();
            var root = ser.Deserialize<Dictionary<string, object>>(File.ReadAllText(path));
            var arr = root != null && root.ContainsKey("currencies") ? root["currencies"] as IList : null;
            var defs = new List<CurrencyDef>();
            if (arr != null)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    var d = arr[i] as Dictionary<string, object>;
                    if (d == null) { continue; }
                    string code = Convert.ToString(d.ContainsKey("code") ? d["code"] : "");
                    string sym = Convert.ToString(d.ContainsKey("symbol") ? d["symbol"] : "");
                    string nm = Convert.ToString(d.ContainsKey("name") ? d["name"] : "");
                    if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(sym)) { continue; }
                    var cd = new CurrencyDef();
                    cd.Code = code; cd.Symbol = sym; cd.Name = nm;
                    defs.Add(cd);
                }
            }
            if (defs.Count > 0)
            {
                currencyDefs = defs;
                string[] codes = new string[defs.Count];
                for (int i = 0; i < defs.Count; i++) { codes[i] = defs[i].Code; }
                CurrencyCodes = codes;
            }
            var ld = root != null && root.ContainsKey("languageDefaults") ? root["languageDefaults"] as Dictionary<string, object> : null;
            if (ld != null)
            {
                languageDefaults = new Dictionary<string, string>();
                foreach (KeyValuePair<string, object> kv in ld)
                {
                    if (kv.Value != null) { languageDefaults[kv.Key] = Convert.ToString(kv.Value); }
                }
            }
            Log("currencies: loaded " + CurrencyCodes.Length + " definitions");
        }
        catch (Exception ex)
        {
            Log("currencies: load failed: " + ex.Message);
        }
    }

    static void WriteDefaultProviders(string path)
    {
        var list = new List<object>();
        for (int i = 0; i < fxProviders.Count; i++)
        {
            var d = new Dictionary<string, object>();
            d["url"] = fxProviders[i].Url;
            d["container"] = fxProviders[i].Container;
            d["lowerKeys"] = fxProviders[i].LowerKeys;
            list.Add(d);
        }
        WriteFileAtomic(path, new JavaScriptSerializer().Serialize(list));
    }

    static void LoadFxProviders()
    {
        string path = Path.Combine(dataDir, "fx-providers.json");
        try
        {
            if (!File.Exists(path)) { WriteDefaultProviders(path); }
            var ser = new JavaScriptSerializer();
            var arr = ser.Deserialize<List<Dictionary<string, object>>>(File.ReadAllText(path));
            var list = new List<FxProvider>();
            if (arr != null)
            {
                for (int i = 0; i < arr.Count; i++)
                {
                    var d = arr[i];
                    if (d == null) { continue; }
                    var p = new FxProvider();
                    p.Url = Convert.ToString(d.ContainsKey("url") ? d["url"] : "");
                    p.Container = Convert.ToString(d.ContainsKey("container") ? d["container"] : "rates");
                    p.LowerKeys = d.ContainsKey("lowerKeys") && Convert.ToBoolean(d["lowerKeys"]);
                    if (!string.IsNullOrEmpty(p.Url)) { list.Add(p); }
                }
            }
            if (list.Count > 0) { fxProviders = list; }
            Log("fxproviders: loaded " + fxProviders.Count + " providers");
        }
        catch (Exception ex)
        {
            Log("fxproviders: load failed: " + ex.Message);
        }
    }

    static string CurrencySymbol(string code)
    {
        for (int i = 0; i < currencyDefs.Count; i++)
        {
            if (currencyDefs[i].Code == code) { return currencyDefs[i].Symbol; }
        }
        if (code == "CNY") { return "CN¥"; }
        if (code == "HKD") { return "HK$"; }
        if (code == "USD") { return "US$"; }
        if (code == "EUR") { return "€"; }
        if (code == "JPY") { return "JP¥"; }
        if (code == "GBP") { return "£"; }
        if (code == "AUD") { return "A$"; }
        if (code == "CAD") { return "C$"; }
        if (code == "CHF") { return "CHF "; }
        if (code == "SGD") { return "S$"; }
        if (code == "KRW") { return "₩"; }
        if (code == "THB") { return "฿"; }
        if (code == "BRL") { return "R$"; }
        if (code == "TWD") { return "NT$"; }
        if (code == "VND") { return "₫"; }
        if (code == "RUB") { return "₽"; }
        return code + " ";
    }

    static string CurrencyFallbackName(string code)
    {
        for (int i = 0; i < currencyDefs.Count; i++)
        {
            if (currencyDefs[i].Code == code) { return currencyDefs[i].Name; }
        }
        if (code == "CNY") { return "人民幣"; }
        if (code == "HKD") { return "港幣"; }
        if (code == "USD") { return "美元"; }
        if (code == "EUR") { return "歐元"; }
        if (code == "JPY") { return "日圓"; }
        if (code == "GBP") { return "英鎊"; }
        if (code == "AUD") { return "澳幣"; }
        if (code == "CAD") { return "加幣"; }
        if (code == "CHF") { return "瑞士法郎"; }
        if (code == "SGD") { return "新加坡幣"; }
        if (code == "KRW") { return "韓元"; }
        if (code == "THB") { return "泰銖"; }
        if (code == "BRL") { return "巴西雷亞爾"; }
        if (code == "TWD") { return "新台幣"; }
        if (code == "VND") { return "越南盾"; }
        if (code == "RUB") { return "俄羅斯盧布"; }
        return code;
    }

    static double GetFxRate()
    {
        if (currency == "CNY") { return 1.0; }
        double r;
        if (fxRates.TryGetValue(currency, out r)) { return r; }
        return 0.0;
    }

    static bool ConvertedMode()
    {
        return currency != "CNY" && GetFxRate() > 0;
    }

    static string EffectiveCurrency()
    {
        if (currency == "CNY") { return "CNY"; }
        return GetFxRate() > 0 ? currency : "CNY";
    }

    static string DispPrice(string rmbValue)
    {
        double v;
        if (ConvertedMode() && double.TryParse(rmbValue, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
        {
            double hv = v * GetFxRate();
            if (Math.Abs(hv) >= 1) { return hv.ToString("0.##", CultureInfo.InvariantCulture); }
            return hv.ToString("0.####", CultureInfo.InvariantCulture);
        }
        return rmbValue;
    }

    static string Money(string rmbValue)
    {
        if (!ConvertedMode()) { return CurrencySymbol("CNY") + rmbValue; }
        return CurrencySymbol(currency) + DispPrice(rmbValue);
    }

    static string MoneyValue(double rmbValue)
    {
        return Money(rmbValue.ToString("0.####", CultureInfo.InvariantCulture));
    }

    static double GetEstimateCost(string model, double inTok, double outTok, double hitPct, int periodIdx)
    {
        if (!prices.ContainsKey(model)) { return 0.0; }
        Dictionary<string, string[]> p = prices[model];
        double hitPrice = Convert.ToDouble(p["hit"][periodIdx], CultureInfo.InvariantCulture);
        double missPrice = Convert.ToDouble(p["miss"][periodIdx], CultureInfo.InvariantCulture);
        double outPrice = Convert.ToDouble(p["out"][periodIdx], CultureInfo.InvariantCulture);
        double ratio = hitPct / 100.0;
        if (ratio < 0) { ratio = 0; }
        if (ratio > 1) { ratio = 1; }
        return (inTok / 1000000.0) * (ratio * hitPrice + (1 - ratio) * missPrice) + (outTok / 1000000.0) * outPrice;
    }

    static Dictionary<string, object> GetDict(Dictionary<string, object> d, string key)
    {
        object o;
        if (d != null && d.TryGetValue(key, out o)) { return o as Dictionary<string, object>; }
        return null;
    }

    static string GetStr(Dictionary<string, object> d, string key)
    {
        object o;
        if (d != null && d.TryGetValue(key, out o) && o != null) { return Convert.ToString(o); }
        return null;
    }

    static double? GetNum(Dictionary<string, object> d, string key)
    {
        object o;
        if (d != null && d.TryGetValue(key, out o) && o != null)
        {
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch { return null; }
        }
        return null;
    }

    static string[] ToStrArray(object o)
    {
        IList list = o as IList;
        if (list == null || list.Count != 2) { return null; }
        return new string[] { Convert.ToString(list[0]), Convert.ToString(list[1]) };
    }

    static void LoadConfig()
    {
        try
        {
            if (!File.Exists(configPath)) { return; }
            string text = File.ReadAllText(configPath);
            var ser = new JavaScriptSerializer();
            var cfg = ser.Deserialize<Dictionary<string, object>>(text);
            if (cfg == null) { return; }
            string c = GetStr(cfg, "corner");
            if (c == "bottomright" || c == "bottomleft" || c == "topright" || c == "topleft") { corner = c; }
            string fhd = GetStr(cfg, "forceHolidayDate");
            if (!string.IsNullOrEmpty(fhd)) { forceHolidayDate = fhd; }
            else
            {
                object fh;
                if (cfg.TryGetValue("forceHoliday", out fh) && fh != null && Convert.ToBoolean(fh))
                {
                    forceHolidayDate = BeijingNow().ToString("yyyy-MM-dd");
                }
            }
            object lk;
            if (cfg.TryGetValue("locked", out lk) && lk != null) { locked = Convert.ToBoolean(lk); }
            double? em = GetNum(cfg, "estModel");
            if (em.HasValue && (em.Value == 0 || em.Value == 1)) { estModelIdx = (int)em.Value; }
            string ei = GetStr(cfg, "estInput");
            if (ei != null) { estInputText = ei; }
            string eo = GetStr(cfg, "estOutput");
            if (eo != null) { estOutputText = eo; }
            string eh = GetStr(cfg, "estHit");
            if (eh != null) { estHitText = eh; }
            string cur = GetStr(cfg, "currency");
            if (!string.IsNullOrEmpty(cur))
            {
                for (int ci = 0; ci < CurrencyCodes.Length; ci++)
                {
                    if (CurrencyCodes[ci] == cur) { currency = cur; break; }
                }
            }
            else
            {
                object hk;
                if (cfg.TryGetValue("showHkd", out hk) && hk != null && Convert.ToBoolean(hk)) { currency = "HKD"; }
            }
            var ratesDict = GetDict(cfg, "fxRates");
            if (ratesDict != null)
            {
                for (int ci = 0; ci < CurrencyCodes.Length; ci++)
                {
                    string code = CurrencyCodes[ci];
                    if (code == "CNY") { continue; }
                    double? r = GetNum(ratesDict, code);
                    if (r.HasValue && r.Value > 0) { fxRates[code] = r.Value; }
                }
            }
            double? legacyFx = GetNum(cfg, "fxRate");
            if (legacyFx.HasValue && legacyFx.Value > 0 && !fxRates.ContainsKey("HKD")) { fxRates["HKD"] = legacyFx.Value; }
            string fd = GetStr(cfg, "fxDate");
            if (fd != null) { fxDate = fd; }
            object ac;
            if (cfg.TryGetValue("autoCurrency", out ac) && ac != null) { autoCurrency = Convert.ToBoolean(ac); }
            string th = GetStr(cfg, "theme");
            if (th == "auto" || th == "light" || th == "dark") { themeMode = th; }
            string lg = GetStr(cfg, "language");
            if (!string.IsNullOrEmpty(lg)) { languageMode = lg; }
            double? l = GetNum(cfg, "posLeft");
            double? t = GetNum(cfg, "posTop");
            if (l.HasValue && t.HasValue) { posLeft = l; posTop = t; }
            string pd = GetStr(cfg, "priceUpdateDate");
            if (!string.IsNullOrEmpty(pd)) { priceUpdateDate = pd; }
            var pdict = GetDict(cfg, "prices");
            if (pdict != null)
            {
                var loaded = new Dictionary<string, Dictionary<string, string[]>>();
                bool ok = true;
                string[] models = new string[] { "flash", "pro" };
                string[] kinds = new string[] { "hit", "miss", "out" };
                for (int mi = 0; mi < models.Length && ok; mi++)
                {
                    var md = GetDict(pdict, models[mi]);
                    if (md == null) { ok = false; break; }
                    var kd = new Dictionary<string, string[]>();
                    for (int ki = 0; ki < kinds.Length; ki++)
                    {
                        object ao;
                        if (!md.TryGetValue(kinds[ki], out ao)) { ok = false; break; }
                        string[] arr = ToStrArray(ao);
                        if (arr == null) { ok = false; break; }
                        kd[kinds[ki]] = arr;
                    }
                    loaded[models[mi]] = kd;
                }
                if (ok) { prices = loaded; }
            }
        }
        catch (Exception ex)
        {
            Log("config: load failed: " + ex.Message);
        }
    }

    static void MigrateFile(string name, string targetDir)
    {
        try
        {
            string oldPath = Path.Combine(appDir, name);
            string newPath = Path.Combine(targetDir, name);
            if (!File.Exists(newPath) && File.Exists(oldPath))
            {
                File.Move(oldPath, newPath);
            }
        }
        catch
        {
        }
    }

    static string LangDir()
    {
        return Path.Combine(appDir, "lang");
    }

    static string HolidaySourceFallback(string code)
    {
        if (code == "source.file") { return "holidays.json"; }
        if (code == "source.empty") { return "內建（holidays.json 無有效資料）"; }
        if (code == "source.error") { return "內建（holidays.json 載入失敗）"; }
        return "內建";
    }

    static string S(string key, string fallback)
    {
        string v;
        if (langStrings.TryGetValue(key, out v)) { return v; }
        return fallback;
    }

    static string SF(string key, string fallback, params object[] args)
    {
        return string.Format(CultureInfo.CurrentCulture, S(key, fallback), args);
    }

    static string ResolveLanguageCode()
    {
        if (languageMode != "auto") { return languageMode; }
        string ui = CultureInfo.CurrentUICulture.Name;
        if (ui.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) { return "zh-HK"; }
        return "en";
    }

    static CultureInfo CultureForLang(string code)
    {
        try
        {
            if (code == "en") { return CultureInfo.GetCultureInfo("en-US"); }
            if (code == "zh-HK" || code == "zh-TW") { return CultureInfo.GetCultureInfo("zh-TW"); }
            if (code == "zh-CN") { return CultureInfo.GetCultureInfo("zh-CN"); }
            if (code == "ja") { return CultureInfo.GetCultureInfo("ja-JP"); }
            if (code == "ko") { return CultureInfo.GetCultureInfo("ko-KR"); }
            if (code == "fr") { return CultureInfo.GetCultureInfo("fr-FR"); }
            if (code == "de") { return CultureInfo.GetCultureInfo("de-DE"); }
            if (code == "es") { return CultureInfo.GetCultureInfo("es-ES"); }
            if (code == "pt-BR") { return CultureInfo.GetCultureInfo("pt-BR"); }
            return CultureInfo.GetCultureInfo(code);
        }
        catch
        {
            return code.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? ZhTw : CultureInfo.GetCultureInfo("en-US");
        }
    }

    static bool LoadLanguage(string code)
    {
        try
        {
            string path = Path.Combine(LangDir(), code + ".json");
            if (!File.Exists(path))
            {
                langStrings = new Dictionary<string, string>();
                activeLangCode = code;
                langCulture = CultureForLang(code);
                return false;
            }
            var ser = new JavaScriptSerializer();
            var dict = ser.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            langStrings = dict != null ? dict : new Dictionary<string, string>();
            activeLangCode = code;
            langCulture = CultureForLang(code);
            Log("language: loaded " + code);
            return true;
        }
        catch (Exception ex)
        {
            Log("language: load failed " + code + ": " + ex.Message);
            return false;
        }
    }

    static void LoadAllLanguages()
    {
        availableLangs = new List<KeyValuePair<string, string>>();
        try
        {
            string dir = LangDir();
            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir, "*.json");
                Array.Sort(files);
                for (int i = 0; i < files.Length; i++)
                {
                    string code = Path.GetFileNameWithoutExtension(files[i]);
                    string name = code;
                    try
                    {
                        var ser = new JavaScriptSerializer();
                        var d = ser.Deserialize<Dictionary<string, string>>(File.ReadAllText(files[i]));
                        string nm;
                        if (d != null && d.TryGetValue("_name", out nm) && !string.IsNullOrEmpty(nm)) { name = nm; }
                    }
                    catch
                    {
                    }
                    availableLangs.Add(new KeyValuePair<string, string>(code, name));
                }
            }
        }
        catch
        {
        }
        string wanted = ResolveLanguageCode();
        if (!LoadLanguage(wanted))
        {
            if (wanted != "zh-HK") { LoadLanguage("en"); }
        }
    }

    static void BuildLanguageMenu()
    {
        if (languageRoot == null) { return; }
        languageRoot.Items.Clear();
        MenuItem auto = new MenuItem();
        auto.Header = S("menu.language.auto", "跟隨系統");
        auto.IsCheckable = true;
        auto.IsChecked = languageMode == "auto";
        auto.Click += delegate { SetLanguage("auto"); };
        languageRoot.Items.Add(auto);
        MenuItem zh = new MenuItem();
        zh.Header = "繁體中文（香港）";
        zh.IsCheckable = true;
        zh.IsChecked = languageMode == "zh-HK";
        zh.Click += delegate { SetLanguage("zh-HK"); };
        languageRoot.Items.Add(zh);
        for (int i = 0; i < availableLangs.Count; i++)
        {
            string code = availableLangs[i].Key;
            if (code == "zh-HK") { continue; }
            MenuItem mi = new MenuItem();
            mi.Header = availableLangs[i].Value;
            mi.IsCheckable = true;
            mi.IsChecked = languageMode == code;
            mi.Click += delegate { SetLanguage(code); };
            languageRoot.Items.Add(mi);
        }
    }

    static void SetLanguage(string code)
    {
        languageMode = code;
        SaveConfig();
        LoadAllLanguages();
        BuildLanguageMenu();
        ApplyLanguageUi();
        if (autoCurrency) { ApplyCurrency(DefaultCurrencyFor(activeLangCode), false); }
        UpdatePriceCells();
        lastPeriod = null;
        UpdateUi();
        if (win != null && win.IsVisible) { AdjustHeightKeepAnchor(); }
    }

    static string DefaultCurrencyFor(string lang)
    {
        string mapped;
        if (languageDefaults.TryGetValue(lang, out mapped) && !string.IsNullOrEmpty(mapped)) { return mapped; }
        if (lang == "zh-HK") { return "HKD"; }
        if (lang == "zh-TW") { return "TWD"; }
        if (lang == "zh-CN") { return "CNY"; }
        if (lang == "ja") { return "JPY"; }
        if (lang == "ko") { return "KRW"; }
        if (lang == "ru") { return "RUB"; }
        if (lang == "pt-BR") { return "BRL"; }
        if (lang == "fr" || lang == "de" || lang == "es" || lang == "it") { return "EUR"; }
        if (lang == "en") { return "USD"; }
        return "CNY";
    }

    static void BuildCurrencyMenu()
    {
        if (currencyRoot == null) { return; }
        currencyRoot.Items.Clear();
        for (int i = 0; i < CurrencyCodes.Length; i++)
        {
            string code = CurrencyCodes[i];
            MenuItem mi = new MenuItem();
            mi.Header = S("currency." + code, CurrencyFallbackName(code)) + " (" + code + ")";
            mi.IsCheckable = true;
            mi.IsChecked = currency == code;
            mi.Click += delegate { SetCurrency(code); };
            currencyRoot.Items.Add(mi);
        }
        currencyRoot.Items.Add(new Separator());
        MenuItem auto = new MenuItem();
        auto.Header = S("menu.currency.auto", "跟隨語言自動切換");
        auto.IsCheckable = true;
        auto.IsChecked = autoCurrency;
        auto.Click += delegate
        {
            autoCurrency = auto.IsChecked;
            SaveConfig();
            if (autoCurrency) { ApplyCurrency(DefaultCurrencyFor(activeLangCode), false); }
            BuildCurrencyMenu();
        };
        currencyRoot.Items.Add(auto);
    }

    static void SetCurrency(string code)
    {
        ApplyCurrency(code, true);
    }

    static void ApplyCurrency(string code, bool manual)
    {
        currency = code;
        if (manual) { autoCurrency = false; }
        SaveConfig();
        BuildCurrencyMenu();
        if (currency != "CNY" && GetFxRate() <= 0) { StartFxFetch(); }
        UpdatePriceCells();
        lastPeriod = null;
        UpdateUi();
        if (win != null && win.IsVisible) { AdjustHeightKeepAnchor(); }
    }

    static void ApplyLanguageUi()
    {
        if (win == null) { return; }
        win.Title = S("app.title", "DeepSeek API 價格");
        if (cornerRoot != null) { cornerRoot.Header = S("menu.corner", "視窗角落"); }
        if (cornerItems.ContainsKey("bottomright")) { cornerItems["bottomright"].Header = S("menu.corner.br", "右下角"); }
        if (cornerItems.ContainsKey("bottomleft")) { cornerItems["bottomleft"].Header = S("menu.corner.bl", "左下角"); }
        if (cornerItems.ContainsKey("topright")) { cornerItems["topright"].Header = S("menu.corner.tr", "右上角"); }
        if (cornerItems.ContainsKey("topleft")) { cornerItems["topleft"].Header = S("menu.corner.tl", "左上角"); }
        if (themeRoot != null) { themeRoot.Header = S("menu.appearance", "外觀"); }
        if (themeAuto != null) { themeAuto.Header = S("menu.appearance.auto", "跟隨系統"); }
        if (themeLight != null) { themeLight.Header = S("menu.appearance.light", "淺色"); }
        if (themeDark != null) { themeDark.Header = S("menu.appearance.dark", "深色"); }
        if (languageRoot != null) { languageRoot.Header = S("menu.language", "語言"); }
        if (holidayItem != null) { holidayItem.Header = S("menu.holiday", "今天當作假日（空閒價）"); }
        if (lockItem != null) { lockItem.Header = S("menu.lock", "鎖定位置"); }
        if (currencyRoot != null) { currencyRoot.Header = S("menu.currency", "貨幣"); }
        if (estItem != null) { estItem.Header = S("menu.est", "成本估算…"); }
        if (refreshItem != null) { refreshItem.Header = S("menu.refresh", "重新整理價格"); }
        if (reloadHolidaysItem != null) { reloadHolidaysItem.Header = S("menu.reload", "重新載入假日表"); }
        if (openItem != null) { openItem.Header = S("menu.open", "開啟 DeepSeek 定價頁"); }
        if (historyItem != null) { historyItem.Header = S("menu.history", "價格歷史…"); }
        if (helpItem != null) { helpItem.Header = S("menu.help", "快捷鍵說明…"); }
        if (exitItem != null) { exitItem.Header = S("menu.exit", "退出"); }
        if (lblFlashHit != null) { lblFlashHit.Text = S("row.hit", "輸入（快取命中）"); }
        if (lblFlashMiss != null) { lblFlashMiss.Text = S("row.miss", "輸入（快取未命中）"); }
        if (lblFlashOut != null) { lblFlashOut.Text = S("row.out", "輸出"); }
        if (lblProHit != null) { lblProHit.Text = S("row.hit", "輸入（快取命中）"); }
        if (lblProMiss != null) { lblProMiss.Text = S("row.miss", "輸入（快取未命中）"); }
        if (lblProOut != null) { lblProOut.Text = S("row.out", "輸出"); }
        if (lblFees != null) { lblFees.Text = S("section.fees", "其他費用"); }
        if (lblStatus != null) { lblStatus.Text = S("section.status", "資料狀態"); }
        if (headOff != null) { headOff.Text = S("period.offpeak", "空閒"); }
        if (headPeak != null) { headPeak.Text = S("period.peak", "高峰"); }
        if (notePeak1 != null) { notePeak1.Text = S("note.peak1", "高峰＝週一至五（非假日）09:00–12:00、14:00–18:00"); }
        if (notePeak2 != null) { notePeak2.Text = S("note.peak2", "（北京時間）"); }
        if (pinHint != null) { pinHint.Text = S("pin.hint", "已釘住"); }
        if (staleText != null) { staleText.ToolTip = S("tooltip.stale", "價格資料可能已過期，右鍵可重新整理"); }
        if (lockText != null) { lockText.ToolTip = S("tooltip.locked", "位置已鎖定（右鍵可解除）"); }
        if (tray != null)
        {
            tray.Text = S("tray.tooltip", "DeepSeek 價格小工具");
            if (trayShowItem != null) { trayShowItem.Text = S("tray.showhide", "顯示 / 隱藏（Ctrl+Alt+D）"); }
            if (trayRefreshItem != null) { trayRefreshItem.Text = S("tray.refresh", "重新整理價格"); }
            if (trayExitItem != null) { trayExitItem.Text = S("tray.exit", "退出"); }
        }
        BuildCurrencyMenu();
    }

    static void Log(string msg)
    {
        try
        {
            string line = "[" + BeijingNow().ToString("yyyy-MM-dd HH:mm:ss") + "] " + msg + "\r\n";
            File.AppendAllText(Path.Combine(logsDir, "widget.log"), line, new UTF8Encoding(false));
        }
        catch
        {
        }
    }

    static void RotateLog()
    {
        try
        {
            string p = Path.Combine(logsDir, "widget.log");
            if (!File.Exists(p)) { return; }
            FileInfo fi = new FileInfo(p);
            if (fi.Length > 262144)
            {
                string bak = p + ".1";
                if (File.Exists(bak)) { File.Delete(bak); }
                File.Move(p, bak);
            }
        }
        catch
        {
        }
    }

    static void WriteFileAtomic(string path, string content)
    {
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(true));
        if (File.Exists(path))
        {
            File.Replace(tmp, path, null);
        }
        else
        {
            File.Move(tmp, path);
        }
    }

    static void SaveConfig()
    {
        try
        {
            var d = new Dictionary<string, object>();
            d["corner"] = corner;
            d["forceHolidayDate"] = forceHolidayDate;
            d["priceUpdateDate"] = priceUpdateDate;
            d["locked"] = locked;
            d["estModel"] = estModelIdx;
            d["estInput"] = estInputText;
            d["estOutput"] = estOutputText;
            d["estHit"] = estHitText;
            d["currency"] = currency;
            d["autoCurrency"] = autoCurrency;
            d["fxRates"] = fxRates;
            d["fxDate"] = fxDate;
            d["theme"] = themeMode;
            d["language"] = languageMode;
            var pd = new Dictionary<string, object>();
            string[] models = new string[] { "flash", "pro" };
            for (int i = 0; i < models.Length; i++)
            {
                var md = new Dictionary<string, object>();
                md["label"] = models[i] == "flash" ? "deepseek-flash" : "deepseek-v4-pro";
                md["hit"] = prices[models[i]]["hit"];
                md["miss"] = prices[models[i]]["miss"];
                md["out"] = prices[models[i]]["out"];
                pd[models[i]] = md;
            }
            d["prices"] = pd;
            if (posLeft.HasValue && posTop.HasValue)
            {
                d["posLeft"] = posLeft.Value;
                d["posTop"] = posTop.Value;
            }
            else
            {
                d["posLeft"] = null;
                d["posTop"] = null;
            }
            var ser = new JavaScriptSerializer();
            WriteFileAtomic(configPath, ser.Serialize(d));
        }
        catch
        {
        }
    }

    static Dictionary<string, Dictionary<string, string[]>> ParsePrices(string text)
    {
        if (string.IsNullOrEmpty(text)) { return null; }
        string[][] flash = new string[3][];
        string[][] pro = new string[3][];
        for (int i = 0; i < 3; i++)
        {
            flash[i] = new string[] { "", "" };
            pro[i] = new string[] { "", "" };
        }
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.IndexOf("空闲时段", StringComparison.Ordinal) < 0 && line.IndexOf("高峰时段", StringComparison.Ordinal) < 0) { continue; }
            if (line.IndexOf("输入", StringComparison.Ordinal) < 0 && line.IndexOf("输出", StringComparison.Ordinal) < 0) { continue; }
            if (!Regex.IsMatch(line, @"^\s*\|")) { continue; }
            int kind = -1;
            if (line.IndexOf("输出", StringComparison.Ordinal) >= 0) { kind = 2; }
            else if (line.IndexOf("未命中", StringComparison.Ordinal) >= 0) { kind = 1; }
            else if (line.IndexOf("命中", StringComparison.Ordinal) >= 0) { kind = 0; }
            if (kind < 0) { continue; }
            int idx = 0;
            if (line.IndexOf("高峰时段", StringComparison.Ordinal) >= 0) { idx = 1; }
            MatchCollection nums = Regex.Matches(line, @"(\d+(?:\.\d+)?)\s*元");
            if (nums.Count < 2) { continue; }
            flash[kind][idx] = nums[0].Groups[1].Value;
            pro[kind][idx] = nums[1].Groups[1].Value;
        }
        for (int k = 0; k < 3; k++)
        {
            for (int i = 0; i < 2; i++)
            {
                if (flash[k][i] == "" || pro[k][i] == "") { return null; }
            }
        }
        var result = new Dictionary<string, Dictionary<string, string[]>>();
        result["flash"] = new Dictionary<string, string[]>();
        result["flash"]["hit"] = flash[0];
        result["flash"]["miss"] = flash[1];
        result["flash"]["out"] = flash[2];
        result["pro"] = new Dictionary<string, string[]>();
        result["pro"]["hit"] = pro[0];
        result["pro"]["miss"] = pro[1];
        result["pro"]["out"] = pro[2];
        return result;
    }

    static void LoadHistory()
    {
        priceHistory = new List<Dictionary<string, object>>();
        try
        {
            if (!File.Exists(historyPath)) { return; }
            var ser = new JavaScriptSerializer();
            var arr = ser.Deserialize<List<Dictionary<string, object>>>(File.ReadAllText(historyPath));
            if (arr != null) { priceHistory = arr; }
        }
        catch
        {
            priceHistory = new List<Dictionary<string, object>>();
        }
    }

    static void SaveHistory()
    {
        try
        {
            var ser = new JavaScriptSerializer();
            WriteFileAtomic(historyPath, ser.Serialize(priceHistory));
        }
        catch
        {
        }
    }

    static Dictionary<string, object> SnapshotModel(Dictionary<string, Dictionary<string, string[]>> p, string model)
    {
        var d = new Dictionary<string, object>();
        d["hit"] = p[model]["hit"];
        d["miss"] = p[model]["miss"];
        d["out"] = p[model]["out"];
        return d;
    }

    static bool PricesEqual(Dictionary<string, Dictionary<string, string[]>> a, Dictionary<string, Dictionary<string, string[]>> b)
    {
        if (a == null || b == null) { return false; }
        string[] models = new string[] { "flash", "pro" };
        string[] kinds = new string[] { "hit", "miss", "out" };
        for (int mi = 0; mi < models.Length; mi++)
        {
            for (int ki = 0; ki < kinds.Length; ki++)
            {
                for (int i = 0; i < 2; i++)
                {
                    if (a[models[mi]][kinds[ki]][i] != b[models[mi]][kinds[ki]][i]) { return false; }
                }
            }
        }
        return true;
    }

    static string LastPriceChangeDate()
    {
        if (priceHistory.Count == 0) { return ""; }
        Dictionary<string, object> last = priceHistory[priceHistory.Count - 1];
        string at = GetStr(last, "at");
        if (string.IsNullOrEmpty(at)) { return ""; }
        if (at.Length >= 10) { return at.Substring(0, 10); }
        return at;
    }

    static string[] ParseStatusFromRss(string rss)
    {
        if (string.IsNullOrEmpty(rss)) { return new string[] { "unknown", "" }; }
        try
        {
            MatchCollection items = Regex.Matches(rss, @"<item>(.*?)</item>", RegexOptions.Singleline);
            for (int i = 0; i < items.Count; i++)
            {
                string block = items[i].Groups[1].Value;
                string title = "";
                Match tm = Regex.Match(block, @"<title>(.*?)</title>", RegexOptions.Singleline);
                if (tm.Success) { title = tm.Groups[1].Value.Trim(); }
                string desc = "";
                Match dm = Regex.Match(block, @"<description>(.*?)</description>", RegexOptions.Singleline);
                if (dm.Success) { desc = dm.Groups[1].Value; }
                Match sm = Regex.Match(desc, @"(?:<strong>|&lt;strong&gt;)\s*Status:\s*(?:</strong>|&lt;/strong&gt;)\s*([A-Za-z_ ]+)", RegexOptions.IgnoreCase);
                string st = sm.Success ? sm.Groups[1].Value.Trim().ToLowerInvariant() : "";
                if (st.IndexOf("investigat", StringComparison.Ordinal) >= 0 || st.IndexOf("identif", StringComparison.Ordinal) >= 0 || st.IndexOf("monitor", StringComparison.Ordinal) >= 0 || st.IndexOf("in_progress", StringComparison.Ordinal) >= 0 || st.IndexOf("ongoing", StringComparison.Ordinal) >= 0)
                {
                    return new string[] { "issue", title };
                }
                if (st.IndexOf("scheduled", StringComparison.Ordinal) >= 0 || st.IndexOf("maintenance", StringComparison.Ordinal) >= 0)
                {
                    return new string[] { "maintenance", title };
                }
            }
            return new string[] { "normal", "" };
        }
        catch
        {
            return new string[] { "unknown", "" };
        }
    }

    static int RunSelftestInternal(StringBuilder sb)
    {
        InitHolidays();
        prices = NewPrices();
        languageMode = "zh-HK";
        langStrings = new Dictionary<string, string>();
        langCulture = ZhTw;
        forceHolidayDate = "";
        int fail = 0;
        string[][] periodCases = new string[][] {
            new string[]{"2026-11-25T10:00","peak","Wed morning peak"},
            new string[]{"2026-11-25T09:00","peak","peak starts 09:00"},
            new string[]{"2026-11-25T11:59","peak","still morning peak"},
            new string[]{"2026-11-25T12:00","offpeak","lunch break"},
            new string[]{"2026-11-25T12:30","offpeak","lunch break"},
            new string[]{"2026-11-25T14:00","peak","afternoon peak starts"},
            new string[]{"2026-11-25T17:59","peak","still afternoon peak"},
            new string[]{"2026-11-25T18:00","offpeak","evening offpeak"},
            new string[]{"2026-11-25T18:30","offpeak","evening offpeak"},
            new string[]{"2026-11-26T08:59","offpeak","before 09:00"},
            new string[]{"2026-11-28T10:00","offpeak","Saturday"},
            new string[]{"2026-11-29T10:00","offpeak","Sunday"},
            new string[]{"2026-02-18T10:00","offpeak","Spring Festival"},
            new string[]{"2026-05-01T10:00","offpeak","Labour Day"},
            new string[]{"2026-10-01T10:00","offpeak","National Day"},
            new string[]{"2026-01-04T10:00","offpeak","make-up workday is Sunday, offpeak"},
            new string[]{"2027-02-08T10:00","offpeak","2027 Spring Festival"},
            new string[]{"2027-06-09T10:00","offpeak","2027 Dragon Boat"},
            new string[]{"2027-02-15T10:00","peak","2027 workday after Spring Festival"}
        };
        for (int i = 0; i < periodCases.Length; i++)
        {
            DateTime dt = DateTime.ParseExact(periodCases[i][0], "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
            string got = GetPeriod(dt);
            bool ok = got == periodCases[i][1];
            if (!ok) { fail++; }
            sb.AppendLine(string.Format("PERIOD {0}  want={1}  got={2}  {3}  ({4})", periodCases[i][0], periodCases[i][1], got, ok ? "OK" : "FAIL", periodCases[i][2]));
        }
        string[][] nextCases = new string[][] {
            new string[]{"2026-11-25T10:00","2026-11-25 12:00","morning peak ends"},
            new string[]{"2026-11-25T12:30","2026-11-25 14:00","afternoon peak starts"},
            new string[]{"2026-11-25T18:30","2026-11-26 09:00","next workday"},
            new string[]{"2026-11-27T18:30","2026-11-30 09:00","Friday to Monday"},
            new string[]{"2026-11-28T10:00","2026-11-30 09:00","Saturday to Monday"},
            new string[]{"2026-02-18T10:00","2026-02-24 09:00","after Spring Festival"},
            new string[]{"2026-01-04T10:00","2026-01-05 09:00","Sunday make-up day to Monday"},
            new string[]{"2027-02-12T10:00","2027-02-15 09:00","after 2027 Spring Festival"}
        };
        for (int i = 0; i < nextCases.Length; i++)
        {
            DateTime dt = DateTime.ParseExact(nextCases[i][0], "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
            DateTime? n = GetNextChange(dt);
            string got = n.HasValue ? n.Value.ToString("yyyy-MM-dd HH:mm") : "none";
            bool ok = got == nextCases[i][1];
            if (!ok) { fail++; }
            sb.AppendLine(string.Format("NEXT   {0}  want={1}  got={2}  {3}  ({4})", nextCases[i][0], nextCases[i][1], got, ok ? "OK" : "FAIL", nextCases[i][2]));
        }
        TimeSpan[] spans = new TimeSpan[] { TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(90), TimeSpan.FromHours(26.25), TimeSpan.FromHours(7), TimeSpan.FromDays(1) };
        string[] spanWant = new string[] { "30 分鐘", "1 小時 30 分", "1 天 2 小時", "7 小時", "1 天" };
        for (int i = 0; i < spans.Length; i++)
        {
            string got = FormatCountdown(spans[i]);
            bool ok = got == spanWant[i];
            if (!ok) { fail++; }
            sb.AppendLine(string.Format("FORMAT {0}  want={1}  got={2}  {3}", spans[i], spanWant[i], got, ok ? "OK" : "FAIL"));
        }
        string[][] estCases = new string[][] {
            new string[]{"flash","1000000","0","100","0","0.02"},
            new string[]{"flash","1000000","0","0","0","1"},
            new string[]{"flash","1000000","0","50","0","0.51"},
            new string[]{"flash","0","1000000","0","1","8"},
            new string[]{"pro","1000000","0","0","0","4.5"},
            new string[]{"pro","0","1000000","0","1","27"}
        };
        for (int i = 0; i < estCases.Length; i++)
        {
            double want = Convert.ToDouble(estCases[i][5], CultureInfo.InvariantCulture);
            double got = GetEstimateCost(estCases[i][0], Convert.ToDouble(estCases[i][1], CultureInfo.InvariantCulture), Convert.ToDouble(estCases[i][2], CultureInfo.InvariantCulture), Convert.ToDouble(estCases[i][3], CultureInfo.InvariantCulture), Convert.ToInt32(estCases[i][4], CultureInfo.InvariantCulture));
            bool ok = Math.Abs(got - want) < 0.0000001;
            if (!ok) { fail++; }
            sb.AppendLine(string.Format("EST    {0} in={1} out={2} hit={3} p={4}  want={5}  got={6}  {7}", estCases[i][0], estCases[i][1], estCases[i][2], estCases[i][3], estCases[i][4], estCases[i][5], got, ok ? "OK" : "FAIL"));
        }
        forceHolidayDate = "2026-11-25";
        DateTime fd1 = DateTime.ParseExact("2026-11-25T10:00", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        DateTime fd2 = DateTime.ParseExact("2026-11-26T10:00", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        string fp1 = GetPeriod(fd1);
        string fp2 = GetPeriod(fd2);
        DateTime? fn = GetNextChange(fd1);
        string fnext = fn.HasValue ? fn.Value.ToString("yyyy-MM-dd HH:mm") : "none";
        bool okForce = fp1 == "offpeak" && fp2 == "peak" && fnext == "2026-11-26 09:00";
        if (!okForce) { fail++; }
        sb.AppendLine(string.Format("FORCE  2026-11-25 forced: day1={0} day2={1} next={2}  {3}", fp1, fp2, fnext, okForce ? "OK" : "FAIL"));
        forceHolidayDate = "";
        priceUpdateDate = "2026-11-25";
        consecutiveFailures = 0;
        DateTime st1 = DateTime.ParseExact("2026-11-28T10:00", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        DateTime st2 = DateTime.ParseExact("2026-11-29T10:00", "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);
        bool okFresh = !IsPriceStale(st1) && IsPriceStale(st2);
        priceUpdateDate = "";
        bool okEmpty = IsPriceStale(st1);
        consecutiveFailures = 3;
        priceUpdateDate = "2026-11-25";
        bool okFail = IsPriceStale(st1);
        consecutiveFailures = 0;
        priceUpdateDate = "";
        bool okStale = okFresh && okEmpty && okFail;
        if (!okStale) { fail++; }
        sb.AppendLine(string.Format("STALE  fresh={0} empty={1} fail3={2}  {3}", okFresh, okEmpty, okFail, okStale ? "OK" : "FAIL"));
        string rssNormal = "<rss><channel><item><title>Old</title><description>&lt;p&gt;&lt;strong&gt;Status:&lt;/strong&gt; resolved&lt;/p&gt;</description></item></channel></rss>";
        string rssIssue = "<rss><channel><item><title>API down</title><description>&lt;p&gt;&lt;strong&gt;Status:&lt;/strong&gt; investigating&lt;/p&gt;</description></item></channel></rss>";
        string rssMaint = "<rss><channel><item><title>Maint</title><description><p><strong>Status:</strong> scheduled</p></description></item></channel></rss>";
        string[] rs1 = ParseStatusFromRss(rssNormal);
        string[] rs2 = ParseStatusFromRss(rssIssue);
        string[] rs3 = ParseStatusFromRss(rssMaint);
        string[] rs4 = ParseStatusFromRss("");
        bool okStatus = rs1[0] == "normal" && rs2[0] == "issue" && rs2[1] == "API down" && rs3[0] == "maintenance" && rs4[0] == "unknown";
        if (!okStatus) { fail++; }
        sb.AppendLine(string.Format("STATUS normal={0} issue={1} maint={2} unknown={3}  {4}", rs1[0], rs2[0], rs3[0], rs4[0], okStatus ? "OK" : "FAIL"));
        var pa = NewPrices();
        var pb = NewPrices();
        bool cmp1 = PricesEqual(pa, NewPrices());
        pb["flash"]["out"][1] = "9";
        bool cmp2 = !PricesEqual(pa, pb);
        bool okCmp = cmp1 && cmp2;
        if (!okCmp) { fail++; }
        sb.AppendLine(string.Format("CMP    equal={0} diff={1}  {2}", cmp1, cmp2, okCmp ? "OK" : "FAIL"));
        string tpath = Path.Combine(Path.GetTempPath(), "dpw-selftest-" + Guid.NewGuid().ToString("N") + ".json");
        bool okIo = false;
        try
        {
            WriteFileAtomic(tpath, "a");
            WriteFileAtomic(tpath, "b");
            okIo = File.ReadAllText(tpath) == "b";
        }
        catch
        {
            okIo = false;
        }
        finally
        {
            try { if (File.Exists(tpath)) { File.Delete(tpath); } }
            catch { }
        }
        if (!okIo) { fail++; }
        sb.AppendLine("IO     atomic=" + okIo + "  " + (okIo ? "OK" : "FAIL"));
        priceHistory = new List<Dictionary<string, object>>();
        var he = new Dictionary<string, object>();
        he["at"] = "2026-11-26 10:00";
        var hf = new Dictionary<string, object>();
        hf["hit"] = new string[] { "0.02", "0.04" };
        hf["miss"] = new string[] { "1", "2" };
        hf["out"] = new string[] { "4", "8" };
        var hp2 = new Dictionary<string, object>();
        hp2["hit"] = new string[] { "0.15", "0.30" };
        hp2["miss"] = new string[] { "4.5", "9.0" };
        hp2["out"] = new string[] { "13.5", "27" };
        he["flash"] = hf;
        he["pro"] = hp2;
        priceHistory.Add(he);
        string htxt = FormatHistoryText();
        bool okHist = htxt.Contains("2026-11-26 10:00") && htxt.Contains("flash") && htxt.Contains("4/8") && htxt.Contains("pro");
        if (!okHist) { fail++; }
        sb.AppendLine("HIST   format=" + okHist + "  " + (okHist ? "OK" : "FAIL"));
        priceHistory = new List<Dictionary<string, object>>();
        var lightPal = GetPalette(true);
        var darkPal = GetPalette(false);
        bool okTheme = lightPal.ContainsKey("BgBrush") && darkPal.ContainsKey("BgBrush") && lightPal["BgBrush"] == "#F5FFFFFF" && darkPal["BgBrush"] == "#F012161D" && lightPal.Count == darkPal.Count;
        if (!okTheme) { fail++; }
        sb.AppendLine("THEME  palette=" + okTheme + "  " + (okTheme ? "OK" : "FAIL"));
        bool okKeys = GetShortcutAction("Q", true, false) == ""
            && GetShortcutAction("Q", true, true) == "quit"
            && GetShortcutAction("E", true, false) == "estimate"
            && GetShortcutAction("E", true, true) == ""
            && GetShortcutAction("R", true, false) == "refresh"
            && GetShortcutAction("H", true, false) == "hide"
            && GetShortcutAction("H", false, false) == "";
        if (!okKeys) { fail++; }
        sb.AppendLine("KEYS   mapping=" + okKeys + "  " + (okKeys ? "OK" : "FAIL"));
        prices = NewPrices();
        currency = "CNY";
        fxRates.Clear();
        bool hkA = DispPrice("0.02") == "0.02" && Money("4") == "CN¥4";
        currency = "HKD";
        fxRates.Clear();
        bool hkB = DispPrice("0.02") == "0.02";
        fxRates["HKD"] = 1.1741;
        double hv;
        bool hkC = double.TryParse(DispPrice("4"), NumberStyles.Any, CultureInfo.InvariantCulture, out hv) && Math.Abs(hv - 4.7) < 0.0001;
        bool hkD = double.TryParse(DispPrice("27"), NumberStyles.Any, CultureInfo.InvariantCulture, out hv) && Math.Abs(hv - 31.7) < 0.0001;
        bool hkE = double.TryParse(DispPrice("0.02"), NumberStyles.Any, CultureInfo.InvariantCulture, out hv) && Math.Abs(hv - 0.0235) < 0.0001;
        bool hkF = Money("4") == "HK$4.7";
        currency = "CNY";
        fxRates.Clear();
        bool okHkd = hkA && hkB && hkC && hkD && hkE && hkF;
        if (!okHkd) { fail++; }
        sb.AppendLine("HKD    convert=" + okHkd + "  " + (okHkd ? "OK" : "FAIL"));
        prices = NewPrices();
        currency = "HKD";
        fxRates["HKD"] = 1.1741;
        string sPeak = FormatSummaryText("peak", false);
        string sOff = FormatSummaryText("offpeak", false);
        string sOffFull = FormatSummaryText("offpeak", true);
        bool okSumm = sPeak.Contains("flash 輸出：HK$9.39 / M tokens") && sPeak.Contains("pro 輸出：HK$31.7 / M tokens")
            && sOff.Contains("flash 輸出：HK$4.7 / M tokens") && sOff.Contains("pro 輸出：HK$15.85 / M tokens")
            && sOffFull.StartsWith("輸出價：") && sOffFull.Contains("/ M tokens") && sOffFull.Contains("HK$");
        currency = "CNY";
        fxRates.Clear();
        string sRmb = FormatSummaryText("offpeak", true);
        okSumm = okSumm && sRmb.StartsWith("輸出價：") && sRmb.Contains("/ M tokens") && sRmb.Contains("CN¥");
        if (!okSumm) { fail++; }
        sb.AppendLine("SUMM   labels=" + okSumm + "  " + (okSumm ? "OK" : "FAIL"));
        bool okAuto = DefaultCurrencyFor("zh-HK") == "HKD" && DefaultCurrencyFor("zh-CN") == "CNY" && DefaultCurrencyFor("zh-TW") == "TWD" && DefaultCurrencyFor("ja") == "JPY" && DefaultCurrencyFor("fr") == "EUR" && DefaultCurrencyFor("en") == "USD" && DefaultCurrencyFor("ko") == "KRW" && DefaultCurrencyFor("ru") == "RUB" && DefaultCurrencyFor("pt-BR") == "BRL" && DefaultCurrencyFor("it") == "EUR";
        if (!okAuto) { fail++; }
        sb.AppendLine("AUTOC  mapping=" + okAuto + "  " + (okAuto ? "OK" : "FAIL"));
        var erSample = ParseRatesJson("{\"result\":\"success\",\"rates\":{\"TWD\":4.9,\"VND\":3861,\"USD\":0.1498}}", "rates", false);
        var fzSample = ParseRatesJson("{\"date\":\"2026-11-25\",\"cny\":{\"twd\":4.96,\"vnd\":3860,\"usd\":0.1499}}", "cny", true);
        bool okFxp = erSample != null && erSample.ContainsKey("TWD") && erSample.ContainsKey("VND") && fzSample != null && fzSample.ContainsKey("TWD") && fzSample.ContainsKey("VND");
        if (!okFxp) { fail++; }
        sb.AppendLine("FXP    parsers=" + okFxp + "  " + (okFxp ? "OK" : "FAIL"));
        string[] testLangs = new string[] { "en", "zh-CN", "ja", "ko", "fr", "de", "es", "it", "pt-BR", "zh-TW", "ru" };
        bool okI18n = true;
        int baseCount = -1;
        int loadedCount = 0;
        for (int li = 0; li < testLangs.Length; li++)
        {
            if (!LoadLanguage(testLangs[li])) { okI18n = false; continue; }
            loadedCount++;
            int cnt = langStrings.Count;
            if (baseCount < 0) { baseCount = cnt; }
            if (cnt != baseCount) { okI18n = false; }
            if (!langStrings.ContainsKey("est.detail") || !langStrings.ContainsKey("help.text")) { okI18n = false; }
        }
        if (LoadLanguage("en"))
        {
            if (S("menu.exit", "退出") == "退出") { okI18n = false; }
        }
        langStrings = new Dictionary<string, string>();
        languageMode = "zh-HK";
        langCulture = ZhTw;
        if (!okI18n) { fail++; }
        sb.AppendLine("I18N   langs=" + loadedCount.ToString() + "/" + testLangs.Length.ToString() + "  " + (okI18n ? "OK" : "FAIL"));
        if (fail > 0) { sb.AppendLine("SELFTEST FAIL: " + fail); } else { sb.AppendLine("SELFTEST ALL PASS"); }
        return fail;
    }

    static int RunSelfTest()
    {
        var sb = new StringBuilder();
        int fail = RunSelftestInternal(sb);
        File.WriteAllText(Path.Combine(logsDir, "selftest-result.txt"), sb.ToString(), new UTF8Encoding(true));
        return fail > 0 ? 1 : 0;
    }

    static int RunFetchTest()
    {
        string content;
        try
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(priceUrl);
            req.Timeout = 20000;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
            {
                content = sr.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(logsDir, "fetchtest-result.txt"), "FETCH ERROR: " + ex.Message, new UTF8Encoding(true));
            return 1;
        }
        var p = ParsePrices(content);
        if (p == null)
        {
            File.WriteAllText(Path.Combine(logsDir, "fetchtest-result.txt"), "PARSE FAILED", new UTF8Encoding(true));
            return 1;
        }
        var sb = new StringBuilder();
        sb.AppendLine("flash hit=" + string.Join(" / ", p["flash"]["hit"]) + "  miss=" + string.Join(" / ", p["flash"]["miss"]) + "  out=" + string.Join(" / ", p["flash"]["out"]));
        sb.AppendLine("pro   hit=" + string.Join(" / ", p["pro"]["hit"]) + "  miss=" + string.Join(" / ", p["pro"]["miss"]) + "  out=" + string.Join(" / ", p["pro"]["out"]));
        File.WriteAllText(Path.Combine(logsDir, "fetchtest-result.txt"), sb.ToString(), new UTF8Encoding(true));
        return 0;
    }

    static string FetchUrlSync(string url, int timeoutMs)
    {
        HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
        req.Timeout = timeoutMs;
        using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
        using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
        {
            return sr.ReadToEnd();
        }
    }

    static int RunStatusTest()
    {
        try
        {
            string text = FetchUrlSync(statusFeedUrl, 15000);
            string[] r = ParseStatusFromRss(text);
            string result = "indicator=" + r[0] + "\r\ntitle=" + r[1] + "\r\n";
            File.WriteAllText(Path.Combine(logsDir, "status-result.txt"), result, new UTF8Encoding(true));
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(logsDir, "status-result.txt"), "STATUS TEST ERROR: " + ex.Message, new UTF8Encoding(true));
            return 1;
        }
    }

    static int RunFxTest()
    {
        try
        {
            string text = FetchUrlSync(fxUrl, 15000);
            var ser = new JavaScriptSerializer();
            var d = ser.Deserialize<Dictionary<string, object>>(text);
            var rates = GetDict(d, "rates");
            double? r = GetNum(rates, "HKD");
            string dt = GetStr(d, "date");
            string result = "hkd=" + (r.HasValue ? r.Value.ToString(CultureInfo.InvariantCulture) : "none") + "\r\ndate=" + dt + "\r\n";
            File.WriteAllText(Path.Combine(logsDir, "fx-result.txt"), result, new UTF8Encoding(true));
            if (!r.HasValue) { return 1; }
            return 0;
        }
        catch (Exception ex)
        {
            File.WriteAllText(Path.Combine(logsDir, "fx-result.txt"), "FX TEST ERROR: " + ex.Message, new UTF8Encoding(true));
            return 1;
        }
    }

    static Brush BrushFrom(string hex)
    {
        return (Brush)new BrushConverter().ConvertFromString(hex);
    }

    static Dictionary<string, string> GetPalette(bool light)
    {
        var p = new Dictionary<string, string>();
        if (light)
        {
            p["BgBrush"] = "#F5FFFFFF";
            p["BorderBrushCard"] = "#33000000";
            p["TextPrimary"] = "#14181F";
            p["TextBody"] = "#333C47";
            p["TextSecondary"] = "#55616E";
            p["TextMuted"] = "#6B7683";
            p["TextFaint"] = "#7A8590";
            p["CellText"] = "#1A1D24";
            p["NameFlash"] = "#0B6FA4";
            p["NamePro"] = "#A65A00";
            p["InputBg"] = "#FFFFFF";
            p["ButtonBg"] = "#E9ECEF";
            p["HeadActive"] = "#10141A";
            p["PinBorder"] = "#B3D97706";
            p["DividerBrush"] = "#26000000";
        }
        else
        {
            p["BgBrush"] = "#F012161D";
            p["BorderBrushCard"] = "#4DFFFFFF";
            p["TextPrimary"] = "#F4F7FA";
            p["TextBody"] = "#C9D4DF";
            p["TextSecondary"] = "#9AA7B4";
            p["TextMuted"] = "#7F8C99";
            p["TextFaint"] = "#66727E";
            p["CellText"] = "#E8EEF4";
            p["NameFlash"] = "#8FD3FF";
            p["NamePro"] = "#FFC98F";
            p["InputBg"] = "#1F2A33";
            p["ButtonBg"] = "#2A3540";
            p["HeadActive"] = "#FFFFFF";
            p["PinBorder"] = "#99FFC042";
            p["DividerBrush"] = "#2EFFFFFF";
        }
        return p;
    }

    static bool IsLightTheme()
    {
        if (themeMode == "light") { return true; }
        if (themeMode == "dark") { return false; }
        try
        {
            using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (k != null)
                {
                    object v = k.GetValue("AppsUseLightTheme");
                    if (v != null) { return Convert.ToInt32(v) == 1; }
                }
            }
        }
        catch
        {
        }
        return false;
    }

    static void ApplyPalette(Window w, Dictionary<string, string> pal)
    {
        foreach (KeyValuePair<string, string> kv in pal)
        {
            w.Resources[kv.Key] = BrushFrom(kv.Value);
        }
    }

    static void UpdateThemeChecks()
    {
        if (themeAuto == null) { return; }
        themeAuto.IsChecked = themeMode == "auto";
        themeLight.IsChecked = themeMode == "light";
        themeDark.IsChecked = themeMode == "dark";
    }

    static void ApplyTheme()
    {
        bool light = IsLightTheme();
        Dictionary<string, string> pal = GetPalette(light);
        ApplyPalette(win, pal);
        brushPeak = BrushFrom(light ? "#3DFF9F43" : "#59FF9F43");
        brushOff = BrushFrom(light ? "#3D2ECC71" : "#592ECC71");
        badgePeak = BrushFrom("#FF9F43");
        badgeOff = BrushFrom("#2ECC71");
        headNormal = BrushFrom(pal["TextMuted"]);
        headActive = BrushFrom(pal["HeadActive"]);
        pinBrush = BrushFrom(pal["PinBorder"]);
        borderNormal = BrushFrom(pal["BorderBrushCard"]);
        statusRed = BrushFrom("#E74C3C");
        if (lastPeriod != null) { UpdatePeriodVisuals(lastPeriod); }
        UpdatePinVisuals();
        UpdateUi();
    }

    static void SetTheme(string mode)
    {
        themeMode = mode;
        SaveConfig();
        UpdateThemeChecks();
        ApplyTheme();
        Log("theme: " + mode);
    }

    static string GetShortcutAction(string keyName, bool ctrl, bool shift)
    {
        if (!ctrl) { return ""; }
        if (keyName == "Q") { return shift ? "quit" : ""; }
        if (shift) { return ""; }
        if (keyName == "E") { return "estimate"; }
        if (keyName == "R") { return "refresh"; }
        if (keyName == "H") { return "hide"; }
        return "";
    }

    static void BuildUi()
    {
        string xaml = File.ReadAllText(Path.Combine(assetsDir, "MainWindow.xaml"));
        win = (Window)XamlReader.Parse(xaml);
        rootBorder = (Border)win.FindName("RootBorder");
        badge = (Border)win.FindName("Badge");
        badgeText = (TextBlock)win.FindName("BadgeText");
        clockText = (TextBlock)win.FindName("ClockText");
        dateText = (TextBlock)win.FindName("DateText");
        summaryText = (TextBlock)win.FindName("SummaryText");
        countdownText = (TextBlock)win.FindName("CountdownText");
        detailPanel = (StackPanel)win.FindName("DetailPanel");
        headOff = (TextBlock)win.FindName("HeadOff");
        headPeak = (TextBlock)win.FindName("HeadPeak");
        priceNoteText = (TextBlock)win.FindName("PriceNoteText");
        holidayNoteText = (TextBlock)win.FindName("HolidayNoteText");
        pinHint = (TextBlock)win.FindName("PinHint");
        staleText = (TextBlock)win.FindName("StaleText");
        lockText = (TextBlock)win.FindName("LockText");
        statusText = (TextBlock)win.FindName("StatusText");
        hkdText = (TextBlock)win.FindName("HkdText");
        unitText = (TextBlock)win.FindName("UnitText");
        fixedSearchText = (TextBlock)win.FindName("FixedSearchText");
        fixedOcrText = (TextBlock)win.FindName("FixedOcrText");
        lblFlashHit = (TextBlock)win.FindName("LblFlashHit");
        lblFlashMiss = (TextBlock)win.FindName("LblFlashMiss");
        lblFlashOut = (TextBlock)win.FindName("LblFlashOut");
        lblProHit = (TextBlock)win.FindName("LblProHit");
        lblProMiss = (TextBlock)win.FindName("LblProMiss");
        lblProOut = (TextBlock)win.FindName("LblProOut");
        lblFees = (TextBlock)win.FindName("LblFees");
        lblStatus = (TextBlock)win.FindName("LblStatus");
        notePeak1 = (TextBlock)win.FindName("NotePeak1");
        notePeak2 = (TextBlock)win.FindName("NotePeak2");
        for (int i = 0; i < cellNames.Length; i++)
        {
            Border b = (Border)win.FindName(cellNames[i]);
            if (b == null) { throw new Exception("Missing XAML element: " + cellNames[i]); }
            cells[cellNames[i]] = b;
        }
        brushPeak = BrushFrom("#59FF9F43");
        brushOff = BrushFrom("#592ECC71");
        clearBrush = Brushes.Transparent;
        badgePeak = BrushFrom("#FF9F43");
        badgeOff = BrushFrom("#2ECC71");
        headNormal = BrushFrom("#7F8C99");
        headActive = BrushFrom("#FFFFFF");
        pinBrush = BrushFrom("#99FFC042");
        borderNormal = BrushFrom("#4DFFFFFF");
        statusRed = BrushFrom("#E74C3C");
        ContextMenu cm = rootBorder.ContextMenu;
        cornerRoot = (MenuItem)cm.Items[0];
        cornerItems["bottomright"] = (MenuItem)cornerRoot.Items[0];
        cornerItems["bottomleft"] = (MenuItem)cornerRoot.Items[1];
        cornerItems["topright"] = (MenuItem)cornerRoot.Items[2];
        cornerItems["topleft"] = (MenuItem)cornerRoot.Items[3];
        themeRoot = (MenuItem)cm.Items[1];
        themeAuto = (MenuItem)themeRoot.Items[0];
        themeLight = (MenuItem)themeRoot.Items[1];
        themeDark = (MenuItem)themeRoot.Items[2];
        languageRoot = (MenuItem)cm.Items[2];
        currencyRoot = (MenuItem)cm.Items[3];
        holidayItem = (MenuItem)cm.Items[4];
        lockItem = (MenuItem)cm.Items[5];
        estItem = (MenuItem)cm.Items[7];
        refreshItem = (MenuItem)cm.Items[8];
        reloadHolidaysItem = (MenuItem)cm.Items[9];
        openItem = (MenuItem)cm.Items[10];
        historyItem = (MenuItem)cm.Items[11];
        helpItem = (MenuItem)cm.Items[12];
        exitItem = (MenuItem)cm.Items[14];
        win.Focusable = true;
        CreateTray();
        WireEvents(cm);
    }

    static void CreateTray()
    {
        try
        {
            Drawing.Icon icon;
            string iconPath = Path.Combine(assetsDir, "app.ico");
            if (File.Exists(iconPath))
            {
                icon = new Drawing.Icon(iconPath);
            }
            else
            {
                string psExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "WindowsPowerShell", "v1.0", "powershell.exe");
                icon = Drawing.Icon.ExtractAssociatedIcon(psExe);
            }
            tray = new Forms.NotifyIcon();
            tray.Icon = icon;
            tray.Text = S("tray.tooltip", "DeepSeek 價格小工具");
            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();
            trayShowItem = menu.Items.Add(S("tray.showhide", "顯示 / 隱藏（Ctrl+Alt+D）"));
            trayRefreshItem = menu.Items.Add(S("tray.refresh", "重新整理價格"));
            menu.Items.Add("-");
            trayExitItem = menu.Items.Add(S("tray.exit", "退出"));
            tray.ContextMenuStrip = menu;
            tray.DoubleClick += delegate { ToggleVisibility(); };
            trayShowItem.Click += delegate { ToggleVisibility(); };
            trayRefreshItem.Click += delegate { StartFetch(); };
            trayExitItem.Click += delegate { win.Close(); };
            tray.Visible = true;
        }
        catch
        {
            tray = null;
        }
    }

    static void ToggleVisibility()
    {
        if (win.IsVisible) { HideToTray(); }
        else { win.Show(); win.Activate(); }
    }

    static void HideToTray()
    {
        if (tray == null && !hotkeyRegistered) { return; }
        win.Hide();
        if (tray != null && !hideHintShown)
        {
            hideHintShown = true;
            try
            {
                tray.ShowBalloonTip(3000, S("balloon.hidden.title", "小工具已收到系統匣"), S("balloon.hidden.text", "雙擊托盤圖示或按 Ctrl+Alt+D 可恢復"), Forms.ToolTipIcon.Info);
            }
            catch
            {
            }
        }
        Log("ui: hidden to tray");
    }

    static void WireEvents(ContextMenu cm)
    {
        cornerItems["bottomright"].Click += delegate { SetCorner("bottomright"); };
        cornerItems["bottomleft"].Click += delegate { SetCorner("bottomleft"); };
        cornerItems["topright"].Click += delegate { SetCorner("topright"); };
        cornerItems["topleft"].Click += delegate { SetCorner("topleft"); };
        themeAuto.Click += delegate { SetTheme("auto"); };
        themeLight.Click += delegate { SetTheme("light"); };
        themeDark.Click += delegate { SetTheme("dark"); };
        holidayItem.Click += delegate
        {
            if (holidayItem.IsChecked)
            {
                forceHolidayDate = TargetNow().ToString("yyyy-MM-dd");
            }
            else
            {
                forceHolidayDate = "";
            }
            SaveConfig();
            lastPeriod = null;
            UpdateUi();
        };
        lockItem.Click += delegate
        {
            locked = lockItem.IsChecked;
            SaveConfig();
            UpdateUi();
        };

        estItem.Click += delegate { ShowEstimator(); };
        refreshItem.Click += delegate { StartFetch(); };
        reloadHolidaysItem.Click += delegate
        {
            LoadHolidays();
            lastPeriod = null;
            UpdateUi();
        };
        openItem.Click += delegate { Process.Start("https://api-docs.deepseek.com/zh-cn/quick_start/pricing/"); };
        historyItem.Click += delegate { ShowHistory(); };
        helpItem.Click += delegate
        {
            MessageBox.Show(
                S("help.text", "Ctrl+Alt+D：顯示 / 隱藏小工具（全域）\r\nCtrl+E：成本估算\r\nCtrl+R：重新整理價格\r\nCtrl+H：收到系統匣（Ctrl+Alt+D 或雙擊托盤圖示可恢復）\r\nCtrl+Shift+Q：退出\r\nEsc：關閉成本估算視窗"),
                S("help.title", "快捷鍵說明"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        };
        exitItem.Click += delegate { win.Close(); };
        cm.Opened += delegate
        {
            menuOpen = true;
            holidayItem.IsChecked = ForceHolidayActive(TargetNow());
            lockItem.IsChecked = locked;
            UpdateThemeChecks();
        };
        cm.Closed += delegate
        {
            menuOpen = false;
            if (!pinned && !rootBorder.IsMouseOver) { SetExpanded(false); }
        };
        rootBorder.MouseEnter += delegate { if (!smoke && !menuOpen) { SetExpanded(true); } };
        rootBorder.MouseLeave += delegate
        {
            if (!smoke && !menuOpen && !pinned) { SetExpanded(false); }
        };
        rootBorder.MouseLeftButtonDown += delegate
        {
            if (locked) { return; }
            mouseActive = true;
            dragged = false;
            downScreen = Forms.Cursor.Position;
            downLeft = win.Left;
            downTop = win.Top;
            lastDragScale = GetDpiScale();
            win.CaptureMouse();
        };
        win.MouseMove += delegate
        {
            if (!mouseActive) { return; }
            if (Mouse.LeftButton != MouseButtonState.Pressed) { return; }
            Drawing.Point p = Forms.Cursor.Position;
            double scale = GetDpiScale();
            if (Math.Abs(scale - lastDragScale) > 0.01)
            {
                downScreen = p;
                downLeft = win.Left;
                downTop = win.Top;
                lastDragScale = scale;
                return;
            }
            int dx = p.X - downScreen.X;
            int dy = p.Y - downScreen.Y;
            if (!dragged)
            {
                if (Math.Abs(dx) <= 6 && Math.Abs(dy) <= 6) { return; }
                dragged = true;
            }
            win.Left = downLeft + dx / scale;
            win.Top = downTop + dy / scale;
        };
        win.MouseLeftButtonUp += delegate
        {
            mouseActive = false;
            try { win.ReleaseMouseCapture(); }
            catch { }
            if (!dragged)
            {
                pinned = !pinned;
                if (pinned) { SetExpanded(true); }
                else if (!rootBorder.IsMouseOver) { SetExpanded(false); }
                UpdatePinVisuals();
            }
            else
            {
                SnapAfterDrag();
            }
            dragged = false;
        };
        win.KeyDown += delegate (object sender, KeyEventArgs e)
        {
            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) != 0;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            string action = GetShortcutAction(e.Key.ToString(), ctrl, shift);
            if (action == "quit") { win.Close(); e.Handled = true; }
            else if (action == "estimate") { ShowEstimator(); e.Handled = true; }
            else if (action == "refresh") { StartFetch(); e.Handled = true; }
            else if (action == "hide") { HideToTray(); e.Handled = true; }
        };
    }

    [DllImport("user32.dll")]
    static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    static void RegisterGlobalHotkey()
    {
        try
        {
            hotkeyHwnd = new WindowInteropHelper(win).Handle;
            hotkeySource = HwndSource.FromHwnd(hotkeyHwnd);
            if (hotkeySource != null)
            {
                hotkeySource.AddHook(HotkeyHook);
                hotkeyRegistered = RegisterHotKey(hotkeyHwnd, HotkeyId, 0x0001 | 0x0002, 0x44);
            }
        }
        catch
        {
        }
    }

    static IntPtr HotkeyHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotkeyId)
        {
            ToggleVisibility();
            handled = true;
        }
        else if (msg == 0x001A)
        {
            if (win != null && win.Dispatcher != null)
            {
                win.Dispatcher.BeginInvoke(new Action(delegate
                {
                    if (themeMode == "auto") { ApplyTheme(); }
                }));
            }
        }
        return IntPtr.Zero;
    }

    static void UnregisterGlobalHotkey()
    {
        try
        {
            if (hotkeyRegistered && hotkeyHwnd != IntPtr.Zero) { UnregisterHotKey(hotkeyHwnd, HotkeyId); }
        }
        catch
        {
        }
        try
        {
            if (hotkeySource != null) { hotkeySource.RemoveHook(HotkeyHook); }
        }
        catch
        {
        }
        hotkeyRegistered = false;
        hotkeySource = null;
    }

    static void ApplyCornerChecks()
    {
        foreach (KeyValuePair<string, MenuItem> kv in cornerItems)
        {
            kv.Value.IsChecked = kv.Key == corner;
        }
    }

    static Rect GetWorkAreaDip()
    {
        try
        {
            IntPtr h = new WindowInteropHelper(win).Handle;
            Forms.Screen scr = h != IntPtr.Zero ? Forms.Screen.FromHandle(h) : Forms.Screen.PrimaryScreen;
            if (scr == null) { return SystemParameters.WorkArea; }
            Drawing.Rectangle wr = scr.WorkingArea;
            double scale = GetDpiScale();
            if (scale <= 0) { scale = 1.0; }
            return new Rect(wr.Left / scale, wr.Top / scale, wr.Width / scale, wr.Height / scale);
        }
        catch
        {
            return SystemParameters.WorkArea;
        }
    }

    static void PlaceWidget()
    {
        Rect wa = GetWorkAreaDip();
        double m = 16;
        double w = win.Width;
        double h = win.Height;
        double l = wa.Left + m;
        double t = wa.Top + m;
        if (corner == "bottomright") { l = wa.Right - w - m; t = wa.Bottom - h - m; }
        else if (corner == "bottomleft") { l = wa.Left + m; t = wa.Bottom - h - m; }
        else if (corner == "topright") { l = wa.Right - w - m; t = wa.Top + m; }
        win.Left = l;
        win.Top = t;
    }

    static void RestorePosition()
    {
        if (!posLeft.HasValue || !posTop.HasValue)
        {
            PlaceWidget();
            return;
        }
        double minL = SystemParameters.VirtualScreenLeft;
        double minT = SystemParameters.VirtualScreenTop;
        double maxL = minL + SystemParameters.VirtualScreenWidth - win.Width;
        double maxT = minT + SystemParameters.VirtualScreenHeight - win.Height;
        double l = posLeft.Value;
        double t = posTop.Value;
        if (l < minL) { l = minL; }
        if (l > maxL) { l = maxL; }
        if (t < minT) { t = minT; }
        if (t > maxT) { t = maxT; }
        win.Left = l;
        win.Top = t;
    }

    static void SnapAfterDrag()
    {
        Rect wa = GetWorkAreaDip();
        double th = 16;
        double l = win.Left;
        double t = win.Top;
        double w = win.Width;
        double h = win.Height;
        if (Math.Abs(l - wa.Left) <= th) { l = wa.Left; }
        if (Math.Abs(l + w - wa.Right) <= th) { l = wa.Right - w; }
        if (Math.Abs(t - wa.Top) <= th) { t = wa.Top; }
        if (Math.Abs(t + h - wa.Bottom) <= th) { t = wa.Bottom - h; }
        win.Left = l;
        win.Top = t;
        double cx = l + w / 2;
        double cy = t + h / 2;
        string horiz = cx > wa.Left + wa.Width / 2 ? "right" : "left";
        string vert = cy > wa.Top + wa.Height / 2 ? "bottom" : "top";
        corner = vert + horiz;
        ApplyCornerChecks();
        posLeft = win.Left;
        posTop = win.Top;
        SaveConfig();
    }

    static void SetCorner(string c)
    {
        corner = c;
        posLeft = null;
        posTop = null;
        ApplyCornerChecks();
        PlaceWidget();
        SaveConfig();
    }

    static double FitHeight()
    {
        win.SizeToContent = SizeToContent.Height;
        win.UpdateLayout();
        double h = win.ActualHeight;
        win.SizeToContent = SizeToContent.Manual;
        if (h > 0) { win.Height = h; }
        return win.Height;
    }

    static void AdjustHeightKeepAnchor()
    {
        double oldH = win.Height;
        double newH = FitHeight();
        double dh = newH - oldH;
        if (corner == "bottomright" || corner == "bottomleft") { win.Top = win.Top - dh; }
    }

    static void SetExpanded(bool expand)
    {
        if (expanded == expand) { return; }
        expanded = expand;
        double oldW = win.Width;
        double oldH = win.Height;
        detailPanel.Visibility = expand ? Visibility.Visible : Visibility.Collapsed;
        if (lastPeriod != null) { UpdatePeriodVisuals(lastPeriod); }
        double newW = expand ? 320 : 250;
        win.Width = newW;
        double newH = FitHeight();
        double dw = newW - oldW;
        double dh = newH - oldH;
        if (corner == "bottomright")
        {
            win.Left = win.Left - dw;
            win.Top = win.Top - dh;
        }
        else if (corner == "bottomleft")
        {
            win.Top = win.Top - dh;
        }
        else if (corner == "topright")
        {
            win.Left = win.Left - dw;
        }
    }

    static double GetDpiScale()
    {
        try
        {
            PresentationSource src = PresentationSource.FromVisual(win);
            if (src != null && src.CompositionTarget != null)
            {
                return src.CompositionTarget.TransformToDevice.M11;
            }
        }
        catch
        {
        }
        return 1.0;
    }

    static void UpdatePinVisuals()
    {
        if (pinned)
        {
            pinHint.Visibility = Visibility.Visible;
            rootBorder.BorderBrush = pinBrush;
        }
        else
        {
            pinHint.Visibility = Visibility.Collapsed;
            rootBorder.BorderBrush = borderNormal;
        }
    }

    static void UpdatePriceCells()
    {
        var map = new Dictionary<string, string>();
        map["FlashHitOff"] = prices["flash"]["hit"][0];
        map["FlashHitPeak"] = prices["flash"]["hit"][1];
        map["FlashMissOff"] = prices["flash"]["miss"][0];
        map["FlashMissPeak"] = prices["flash"]["miss"][1];
        map["FlashOutOff"] = prices["flash"]["out"][0];
        map["FlashOutPeak"] = prices["flash"]["out"][1];
        map["ProHitOff"] = prices["pro"]["hit"][0];
        map["ProHitPeak"] = prices["pro"]["hit"][1];
        map["ProMissOff"] = prices["pro"]["miss"][0];
        map["ProMissPeak"] = prices["pro"]["miss"][1];
        map["ProOutOff"] = prices["pro"]["out"][0];
        map["ProOutPeak"] = prices["pro"]["out"][1];
        foreach (KeyValuePair<string, string> kv in map)
        {
            TextBlock tb = (TextBlock)win.FindName(kv.Key + "V");
            if (tb != null) { tb.Text = DispPrice(kv.Value); }
        }
        string sym = CurrencySymbol(EffectiveCurrency());
        unitText.Text = SF("unit", "{0} / M tokens", sym);
        fixedSearchText.Text = SF("fees.search", "Search / Fetch API：{0} / 1k 次成功請求", Money("5"));
        fixedOcrText.Text = SF("fees.ocr", "文件解析 / OCR：{0} / 1k 頁", Money("1.5"));
    }

    static string FormatSummaryText(string period, bool isExpanded)
    {
        int i = period == "peak" ? 1 : 0;
        string f = Money(prices["flash"]["out"][i]);
        string p = Money(prices["pro"]["out"][i]);
        if (isExpanded)
        {
            return SF("summary.full", "輸出價：flash {0} · pro {1} / M tokens", f, p);
        }
        return SF("summary.compact", "flash 輸出：{0} / M tokens\npro 輸出：{1} / M tokens", f, p);
    }

    static void UpdatePeriodVisuals(string period)
    {
        int i = period == "peak" ? 1 : 0;
        if (i == 1)
        {
            badge.Background = badgePeak;
            badgeText.Text = S("badge.peak", "高峰");
            headPeak.Foreground = headActive;
            headOff.Foreground = headNormal;
        }
        else
        {
            badge.Background = badgeOff;
            badgeText.Text = S("badge.offpeak", "空閒");
            headOff.Foreground = headActive;
            headPeak.Foreground = headNormal;
        }
        for (int n = 0; n < cellNames.Length; n++)
        {
            bool isPeakCell = cellNames[n].EndsWith("Peak", StringComparison.Ordinal);
            Brush b = clearBrush;
            if (isPeakCell && i == 1) { b = brushPeak; }
            else if (!isPeakCell && i == 0) { b = brushOff; }
            cells[cellNames[n]].Background = b;
        }
        summaryText.Text = FormatSummaryText(period, expanded);
    }

    static void UpdateUi()
    {
        if (win == null) { return; }
        DateTime now = TargetNow();
        clockText.Text = now.ToString("HH:mm:ss");
        string ds = now.ToString(S("date.format", "M月d日 ddd"), langCulture);
        string hol = HolidayName(now);
        if (hol != null) { ds = ds + " · " + hol; }
        if (testDate.HasValue) { ds = ds + S("app.testSuffix", " · 測試"); }
        if (dateText.Text != ds) { dateText.Text = ds; }
        DateTime? next = GetNextChange(now);
        string cd = "";
        if (next.HasValue)
        {
            string label = (next.Value.Hour == 9 || next.Value.Hour == 14) ? S("countdown.toPeak", "距高峰開始") : S("countdown.toOffpeak", "距高峰結束");
            string dur = FormatCountdown(next.Value - now);
            Brush countdownBrush = GetPeriod(now) == "peak" ? badgePeak : badgeOff;
            cd = label + " " + dur;
            string key = label + "|" + dur;
            if (lastCountdownKey != key)
            {
                lastCountdownKey = key;
                countdownText.Inlines.Clear();
                countdownText.Inlines.Add(new Run(label + " "));
                countdownText.Inlines.Add(new Run(dur) { Foreground = countdownBrush });
            }
        }
        else
        {
            if (lastCountdownKey != "")
            {
                lastCountdownKey = "";
                countdownText.Inlines.Clear();
            }
        }
        if (headOff != null) { headOff.Text = S("period.offpeak", "空閒"); }
        if (headPeak != null) { headPeak.Text = S("period.peak", "高峰"); }
        if (lblFees != null) { lblFees.Text = S("section.fees", "其他費用"); }
        if (lblStatus != null) { lblStatus.Text = S("section.status", "資料狀態"); }
        string pn = S("pricenote.default", "價格資料：內建預設（尚未更新）");
        if (!string.IsNullOrEmpty(priceUpdateDate)) { pn = SF("pricenote.updated", "價格資料：{0} 自官網更新", priceUpdateDate); }
        if (priceFetching)
        {
            pn = S("pricenote.fetching", "價格資料：正在向官網更新…");
        }
        else if (priceFetchFailed)
        {
            if (consecutiveFailures > 1)
            {
                pn = pn + SF("pricenote.failedN", "（本次更新失敗，已連續 {0} 次）", consecutiveFailures);
            }
            else
            {
                pn = pn + S("pricenote.failed", "（本次更新失敗）");
            }
        }
        string lastChange = LastPriceChangeDate();
        if (!string.IsNullOrEmpty(lastChange)) { pn = pn + SF("pricenote.lastChange", "；上次變價 {0}", lastChange); }
        if (currency != "CNY")
        {
            double rr = GetFxRate();
            if (rr > 0) { pn = pn + SF("pricenote.rate", "；匯率 1元≈{0}", rr.ToString("0.####") + " " + currency); }
            else { pn = pn + S("pricenote.rateMissing", "；匯率未取得，暫以人民幣顯示"); }
        }
        if (priceNoteText.Text != pn) { priceNoteText.Text = pn; }
        string hn;
        string srcText = S(holidaySource, HolidaySourceFallback(holidaySource));
        if (holYears.Contains(now.Year))
        {
            hn = SF("holiday.note", "假日表：{0}（{1}）", holYearsText, srcText);
        }
        else
        {
            hn = SF("holiday.missing", "未收錄 {0} 年假日表（現有：{1}）；可編輯 holidays.json", now.Year, holYearsText);
        }
        if (holidayNoteText.Text != hn) { holidayNoteText.Text = hn; }
        bool stale = IsPriceStale(now);
        staleText.Visibility = stale ? Visibility.Visible : Visibility.Collapsed;
        lockText.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;
        string stTxt;
        Brush stBrush;
        if (serviceStatus == "normal")
        {
            stTxt = S("status.normal", "服務狀態：正常（官方狀態頁）");
            stBrush = badgeOff;
        }
        else if (serviceStatus == "issue")
        {
            stTxt = SF("status.issue", "服務狀態：事件進行中 — {0}", serviceStatusTitle);
            stBrush = statusRed;
        }
        else if (serviceStatus == "maintenance")
        {
            stTxt = SF("status.maintenance", "服務狀態：維護中 — {0}", serviceStatusTitle);
            stBrush = badgePeak;
        }
        else
        {
            stTxt = S("status.unknown", "服務狀態：未知（未能讀取狀態頁）");
            stBrush = headNormal;
        }
        if (statusText.Text != stTxt) { statusText.Text = stTxt; }
        statusText.Foreground = stBrush;
        if (currency != "CNY")
        {
            hkdText.Visibility = Visibility.Visible;
            string hk;
            double rr = GetFxRate();
            if (rr > 0)
            {
                string rateText = rr.ToString("0.####") + " " + currency;
                if (!string.IsNullOrEmpty(fxDate))
                {
                    hk = SF("fx.rateDate", "匯率：1 CNY ≈ {0}（{1}）", rateText, fxDate);
                }
                else
                {
                    hk = SF("fx.rate", "匯率：1 CNY ≈ {0}", rateText);
                }
            }
            else
            {
                hk = S("fx.pending", "匯率：尚未取得");
            }
            if (hkdText.Text != hk) { hkdText.Text = hk; }
        }
        else
        {
            hkdText.Visibility = Visibility.Collapsed;
        }
        string period = GetPeriod(now);
        if (period != lastPeriod)
        {
            lastPeriod = period;
            UpdatePeriodVisuals(period);
        }
        if (tray != null)
        {
            string tt = "DeepSeek " + badgeText.Text + " " + now.ToString("HH:mm");
            if (cd != "") { tt = tt + " · " + cd; }
            if (tt.Length > 63) { tt = tt.Substring(0, 63); }
            if (tray.Text != tt) { tray.Text = tt; }
        }
    }

    static async void StartFetch()
    {
        if (fetching) { return; }
        fetching = true;
        priceFetching = true;
        priceFetchFailed = false;
        UpdateUi();
        string text = null;
        string fetchError = "";
        try
        {
            using (HttpClient http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(15);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("DeepSeekPriceWidget/2.4");
                text = await http.GetStringAsync(priceUrl);
            }
        }
        catch (Exception ex)
        {
            fetchError = ex.Message;
        }
        fetching = false;
        priceFetching = false;
        var parsed = ParsePrices(text);
        if (parsed == null)
        {
            priceFetchFailed = true;
            consecutiveFailures++;
            if (text == null)
            {
                Log("price: fetch failed: " + fetchError);
            }
            else
            {
                Log("price: parse failed (content length " + text.Length.ToString() + ")");
            }
            UpdateUi();
            return;
        }
        consecutiveFailures = 0;
        Log("price: ok (flash out " + parsed["flash"]["out"][0] + "/" + parsed["flash"]["out"][1] + ")");
        if (!PricesEqual(parsed, prices))
        {
            var entry = new Dictionary<string, object>();
            entry["at"] = BeijingNow().ToString("yyyy-MM-dd HH:mm");
            entry["flash"] = SnapshotModel(parsed, "flash");
            entry["pro"] = SnapshotModel(parsed, "pro");
            priceHistory.Add(entry);
            while (priceHistory.Count > 50) { priceHistory.RemoveAt(0); }
            SaveHistory();
            Log("price: changed -> flash out " + parsed["flash"]["out"][0] + "/" + parsed["flash"]["out"][1] + ", pro out " + parsed["pro"]["out"][0] + "/" + parsed["pro"]["out"][1]);
            if (tray != null)
            {
                try
                {
                    tray.ShowBalloonTip(4000, S("balloon.price.title", "DeepSeek 價格已變更"), SF("balloon.price.text", "flash 輸出 {0}/{1} 元（空閒/高峰）", parsed["flash"]["out"][0], parsed["flash"]["out"][1]), Forms.ToolTipIcon.Info);
                }
                catch
                {
                }
            }
        }
        prices = parsed;
        priceUpdateDate = BeijingNow().ToString("yyyy-MM-dd");
        SaveConfig();
        UpdatePriceCells();
        lastPeriod = null;
        UpdateUi();
    }

    static async void StartStatusFetch()
    {
        string text = null;
        string err = "";
        try
        {
            using (HttpClient http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(10);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("DeepSeekPriceWidget/2.4");
                text = await http.GetStringAsync(statusFeedUrl);
            }
        }
        catch (Exception ex)
        {
            err = ex.Message;
        }
        if (text == null)
        {
            serviceStatus = "unknown";
            serviceStatusTitle = "";
            Log("status: fetch failed: " + err);
            UpdateUi();
            return;
        }
        string[] r = ParseStatusFromRss(text);
        serviceStatus = r[0];
        serviceStatusTitle = r[1];
        Log("status: " + r[0] + (r[1] != "" ? " - " + r[1] : ""));
        UpdateUi();
    }

    static async Task<string> FetchTextAsync(string url)
    {
        try
        {
            using (HttpClient http = new HttpClient())
            {
                http.Timeout = TimeSpan.FromSeconds(10);
                http.DefaultRequestHeaders.UserAgent.ParseAdd("DeepSeekPriceWidget/3.4");
                return await http.GetStringAsync(url);
            }
        }
        catch
        {
            return null;
        }
    }

    static string ExtractDateFromJson(string text)
    {
        Match m = Regex.Match(text ?? "", "\"date\"\\s*:\\s*\"(\\d{4}-\\d{2}-\\d{2})\"");
        return m.Success ? m.Groups[1].Value : "";
    }

    static Dictionary<string, double> ParseRateMap(Dictionary<string, object> source, bool lowerKeys)
    {
        if (source == null) { return null; }
        var loaded = new Dictionary<string, double>();
        for (int ci = 0; ci < CurrencyCodes.Length; ci++)
        {
            string code = CurrencyCodes[ci];
            if (code == "CNY") { continue; }
            string key = lowerKeys ? code.ToLowerInvariant() : code;
            double? r = GetNum(source, key);
            if (r.HasValue && r.Value > 0) { loaded[code] = r.Value; }
        }
        return loaded.Count > 0 ? loaded : null;
    }

    static Dictionary<string, double> ParseRatesJson(string text, string containerKey, bool lowerKeys)
    {
        try
        {
            var ser = new JavaScriptSerializer();
            var root = ser.Deserialize<Dictionary<string, object>>(text);
            return ParseRateMap(GetDict(root, containerKey), lowerKeys);
        }
        catch
        {
            return null;
        }
    }

    static async void StartFxFetch()
    {
        if (fxFetching) { return; }
        fxFetching = true;
        Dictionary<string, double> loaded = null;
        string source = "";
        string dateStr = "";
        for (int pi = 0; pi < fxProviders.Count && loaded == null; pi++)
        {
            FxProvider prov = fxProviders[pi];
            string text = await FetchTextAsync(prov.Url);
            if (text == null) { continue; }
            loaded = ParseRatesJson(text, prov.Container, prov.LowerKeys);
            if (loaded != null)
            {
                try { source = new Uri(prov.Url).Host; }
                catch { source = prov.Url; }
                dateStr = ExtractDateFromJson(text);
            }
        }
        fxFetching = false;
        if (loaded != null)
        {
            fxRates = loaded;
            if (string.IsNullOrEmpty(dateStr)) { dateStr = BeijingNow().ToString("yyyy-MM-dd"); }
            fxDate = dateStr;
            SaveConfig();
            if (win != null) { UpdatePriceCells(); lastPeriod = null; }
            Log("fx: loaded " + loaded.Count.ToString() + " currencies from " + source + " (" + fxDate + ")");
        }
        else
        {
            Log("fx: all providers failed");
        }
        UpdateUi();
    }

    static void ShowEstimator()
    {
        if (estWindow != null && estWindow.IsVisible)
        {
            estWindow.Activate();
            return;
        }
        string xaml = File.ReadAllText(Path.Combine(assetsDir, "EstimatorWindow.xaml"));
        Window w = (Window)XamlReader.Parse(xaml);
        w.Owner = win;
        ApplyPalette(w, GetPalette(IsLightTheme()));
        ComboBox modelBox = (ComboBox)w.FindName("EstModel");
        TextBox inBox = (TextBox)w.FindName("EstInput");
        TextBox outBox = (TextBox)w.FindName("EstOutput");
        TextBox hitBox = (TextBox)w.FindName("EstHit");
        TextBlock result = (TextBlock)w.FindName("EstResult");
        TextBlock detail = (TextBlock)w.FindName("EstDetail");
        TextBlock periodText = (TextBlock)w.FindName("EstPeriodText");
        Button closeBtn = (Button)w.FindName("EstClose");
        Button copyBtn = (Button)w.FindName("EstCopy");
        w.Title = S("est.title", "成本估算");
        ((TextBlock)w.FindName("LblEstHeading")).Text = S("est.heading", "成本估算");
        ((TextBlock)w.FindName("LblEstModel")).Text = S("est.model", "模型");
        ((TextBlock)w.FindName("LblEstInput")).Text = S("est.input", "輸入 tokens");
        ((TextBlock)w.FindName("LblEstOutput")).Text = S("est.output", "輸出 tokens");
        ((TextBlock)w.FindName("LblEstHit")).Text = S("est.hit", "快取命中率（%，0–100）");
        ((TextBlock)w.FindName("LblEstNote")).Text = S("est.note", "以目前時段價格計算；高峰價為空閒價 2 倍。");
        copyBtn.Content = S("est.copy", "複製結果");
        closeBtn.Content = S("est.close", "關閉");
        int initModel = estModelIdx;
        if (initModel < 0 || initModel > 1) { initModel = 0; }
        modelBox.SelectedIndex = initModel;
        inBox.Text = estInputText;
        outBox.Text = estOutputText;
        hitBox.Text = estHitText;
        Action update = delegate
        {
            DateTime now = TargetNow();
            int periodIdx = GetPeriod(now) == "peak" ? 1 : 0;
            string periodName = periodIdx == 1 ? S("period.peak", "高峰") : S("period.offpeak", "空閒");
            string pt = SF("est.period", "以 {0} 價計算", periodName) + " · " + EffectiveCurrency();
            if (periodText.Text != pt) { periodText.Text = pt; }
            string modelKey = modelBox.SelectedIndex == 1 ? "pro" : "flash";
            double inTok = 0.0;
            double outTok = 0.0;
            double hitPct = 0.0;
            bool ok = true;
            string inText = inBox.Text.Trim();
            string outText = outBox.Text.Trim();
            string hitText = hitBox.Text.Trim();
            if (inText == "") { inTok = 0.0; }
            else if (!double.TryParse(inText, out inTok)) { ok = false; }
            if (outText == "") { outTok = 0.0; }
            else if (!double.TryParse(outText, out outTok)) { ok = false; }
            if (hitText == "") { hitPct = 0.0; }
            else if (!double.TryParse(hitText, out hitPct)) { ok = false; }
            if (!ok || inTok < 0 || outTok < 0 || hitPct < 0)
            {
                result.Text = "—";
                result.Foreground = headNormal;
                detail.Text = S("est.invalid", "請輸入有效的非負數字");
                return;
            }
            if (hitPct > 100) { hitPct = 100; }
            double cost = GetEstimateCost(modelKey, inTok, outTok, hitPct, periodIdx);
            Dictionary<string, string[]> p = prices[modelKey];
            result.Text = "≈ " + MoneyValue(cost);
            result.Foreground = periodIdx == 1 ? badgePeak : badgeOff;
            double hitTok = inTok * (hitPct / 100.0);
            double missTok = inTok - hitTok;
            string unitLabel = SF("unit.perM", "{0}/M", CurrencySymbol(EffectiveCurrency()));
            detail.Text = SF("est.detail", "命中 {0:N0} × {1} + 未命中 {2:N0} × {3}；輸出 {4:N0} × {5}（{6}）", hitTok, Money(p["hit"][periodIdx]), missTok, Money(p["miss"][periodIdx]), outTok, Money(p["out"][periodIdx]), unitLabel);
        };
        inBox.TextChanged += delegate { update(); };
        outBox.TextChanged += delegate { update(); };
        hitBox.TextChanged += delegate { update(); };
        modelBox.SelectionChanged += delegate { update(); };
        copyBtn.Click += delegate
        {
            try
            {
                string modelName = modelBox.SelectedIndex == 1 ? "deepseek-v4-pro" : "deepseek-flash";
                int pi = GetPeriod(TargetNow()) == "peak" ? 1 : 0;
                string pname = pi == 1 ? S("period.peak", "高峰") : S("period.offpeak", "空閒");
                string datePart = string.IsNullOrEmpty(priceUpdateDate) ? "-" : priceUpdateDate;
                Clipboard.SetText(SF("est.copyline", "{0}｜輸入 {1}（命中 {2}%）｜輸出 {3}｜{4}｜{5}價｜價格日期 {6}", modelName, inBox.Text, hitBox.Text, outBox.Text, result.Text, pname, datePart));
                copyBtn.Content = S("est.copied", "已複製");
                DispatcherTimer revert = new DispatcherTimer();
                revert.Interval = TimeSpan.FromSeconds(1.5);
                revert.Tick += delegate
                {
                    revert.Stop();
                    copyBtn.Content = S("est.copy", "複製結果");
                };
                revert.Start();
            }
            catch
            {
            }
        };
        closeBtn.Click += delegate { w.Close(); };
        w.KeyDown += delegate (object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { w.Close(); }
        };
        w.Loaded += delegate
        {
            inBox.Focus();
            inBox.SelectAll();
            update();
        };
        estWindow = w;
        DispatcherTimer liveTimer = new DispatcherTimer();
        liveTimer.Interval = TimeSpan.FromSeconds(1);
        liveTimer.Tick += delegate { update(); };
        liveTimer.Start();
        w.Closed += delegate { liveTimer.Stop(); };
        w.ShowDialog();
        estWindow = null;
        estModelIdx = modelBox.SelectedIndex < 0 ? 0 : modelBox.SelectedIndex;
        estInputText = inBox.Text;
        estOutputText = outBox.Text;
        estHitText = hitBox.Text;
        SaveConfig();
    }

    static string CellText(Dictionary<string, object> model, string kind)
    {
        object o;
        if (!model.TryGetValue(kind, out o)) { return "?"; }
        string[] arr = ToStrArray(o);
        if (arr == null) { return "?"; }
        return DispPrice(arr[0]) + "/" + DispPrice(arr[1]);
    }

    static string FormatHistoryText()
    {
        if (priceHistory.Count == 0) { return S("history.empty", "尚無變動記錄。目前價格自開始記錄以來未改變。"); }
        var sb = new StringBuilder();
        for (int i = priceHistory.Count - 1; i >= 0; i--)
        {
            Dictionary<string, object> e = priceHistory[i];
            string at = GetStr(e, "at");
            if (at == null) { at = "（未知時間）"; }
            var f = GetDict(e, "flash");
            var p = GetDict(e, "pro");
            string unit = SF("unit.perM", "{0}/M", CurrencySymbol(EffectiveCurrency()));
            sb.AppendLine(at);
            if (f != null)
            {
                sb.AppendLine(SF("history.line", "{0}  命中 {1}　未命中 {2}　輸出 {3}（{4}）", "flash", CellText(f, "hit"), CellText(f, "miss"), CellText(f, "out"), unit));
            }
            if (p != null)
            {
                sb.AppendLine(SF("history.line", "{0}  命中 {1}　未命中 {2}　輸出 {3}（{4}）", "pro", CellText(p, "hit"), CellText(p, "miss"), CellText(p, "out"), unit));
            }
            if (i > 0) { sb.AppendLine(); }
        }
        return sb.ToString();
    }

    static void ShowHistory()
    {
        string xaml = File.ReadAllText(Path.Combine(assetsDir, "HistoryWindow.xaml"));
        Window w = (Window)XamlReader.Parse(xaml);
        w.Owner = win;
        ApplyPalette(w, GetPalette(IsLightTheme()));
        TextBlock txt = (TextBlock)w.FindName("HistText");
        Button closeBtn = (Button)w.FindName("HistClose");
        w.Title = S("history.title", "價格歷史");
        ((TextBlock)w.FindName("LblHistHeading")).Text = S("history.heading", "價格變動歷史");
        closeBtn.Content = S("est.close", "關閉");
        txt.Text = FormatHistoryText();
        closeBtn.Click += delegate { w.Close(); };
        w.KeyDown += delegate (object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { w.Close(); }
        };
        w.ShowDialog();
    }

    static void SetupSmoke()
    {
        smokeStep = 0;
        smokeTimer = new DispatcherTimer();
        smokeTimer.Interval = TimeSpan.FromMilliseconds(700);
        smokeTimer.Tick += delegate
        {
            if (smokeStep == 0)
            {
                smokeH1 = win.Height;
                SetExpanded(true);
                smokeStep = 1;
            }
            else if (smokeStep == 1)
            {
                smokeH2 = win.Height;
                SetExpanded(false);
                smokeStep = 2;
            }
            else
            {
                smokeH3 = win.Height;
                smokeTimer.Stop();
                win.Close();
            }
        };
        smokeTimer.Start();
    }

    static int RunGui()
    {
        LoadHolidays();
        prices = NewPrices();
        LoadConfig();
        LoadHistory();
        bool acquired = false;
        using (Mutex mutex = new Mutex(false, @"Local\DeepSeekPriceWidget"))
        {
            try { acquired = mutex.WaitOne(0); }
            catch (AbandonedMutexException) { acquired = true; }
            if (!acquired) { return 0; }
            BuildUi();
            ApplyTheme();
            LoadAllLanguages();
            if (autoCurrency) { currency = DefaultCurrencyFor(activeLangCode); }
            BuildLanguageMenu();
            ApplyLanguageUi();
            ApplyCornerChecks();
            holidayItem.IsChecked = ForceHolidayActive(TargetNow());
            SaveConfig();
            UpdatePriceCells();
            RestorePosition();
            UpdateUi();
            uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromSeconds(1);
            uiTimer.Tick += delegate { UpdateUi(); };
            uiTimer.Start();
            win.Loaded += delegate
            {
                RegisterGlobalHotkey();
                FitHeight();
                RestorePosition();
                UpdateUi();
                if (!smoke)
                {
                    StartFetch();
                    StartStatusFetch();
                        if (currency != "CNY") { StartFxFetch(); }
                    refreshTimer = new DispatcherTimer();
                    refreshTimer.Interval = TimeSpan.FromHours(6);
                    refreshTimer.Tick += delegate
                    {
                        StartFetch();
                        StartStatusFetch();
                    if (currency != "CNY") { StartFxFetch(); }
                    };
                    refreshTimer.Start();
                }
            };
            string smokeEst = "fail";
            string smokeHist = "fail";
            if (smoke)
            {
                try
                {
                    XamlReader.Parse(File.ReadAllText(Path.Combine(assetsDir, "EstimatorWindow.xaml")));
                    smokeEst = "ok";
                }
                catch { }
                try
                {
                    XamlReader.Parse(File.ReadAllText(Path.Combine(assetsDir, "HistoryWindow.xaml")));
                    smokeHist = "ok";
                }
                catch { }
                SetupSmoke();
            }
            win.Closed += delegate { Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Normal); };
            win.Show();
            Dispatcher.Run();
            Log("app exit");
            bool hotkeyOk = hotkeyRegistered;
            UnregisterGlobalHotkey();
            trayOk = tray != null;
            try
            {
                if (tray != null)
                {
                    tray.Visible = false;
                    tray.Dispose();
                    tray = null;
                }
            }
            catch { }
            if (smoke)
            {
                bool themeBgOk = false;
                try
                {
                    Border rb = (Border)win.FindName("RootBorder");
                    TextBlock ct = (TextBlock)win.FindName("ClockText");
                    themeBgOk = rb != null && rb.Background != null && ct != null && ct.Foreground != null;
                }
                catch { }
                string result = "ESTXAML=" + smokeEst + "\r\nHISTXAML=" + smokeHist + "\r\nTHEMEBG=" + (themeBgOk ? "yes" : "no") + "\r\nHEIGHTS compact=" + smokeH1.ToString(CultureInfo.InvariantCulture) + " expanded=" + smokeH2.ToString(CultureInfo.InvariantCulture) + " back=" + smokeH3.ToString(CultureInfo.InvariantCulture) + "\r\nTRAY=" + (trayOk ? "yes" : "no") + "\r\nHOTKEY=" + (hotkeyOk ? "yes" : "no") + "\r\nHOLSRC=" + holidaySource + "\r\nGUI OK\r\n";
                File.WriteAllText(Path.Combine(logsDir, "smoke-result.txt"), result, new UTF8Encoding(true));
            }
            try { mutex.ReleaseMutex(); }
            catch { }
        }
        return 0;
    }
}

