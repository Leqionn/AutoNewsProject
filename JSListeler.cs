
using System.Collections.Concurrent;
using Binance.Net.Enums;

namespace AINews
{
    public static class JSListeler
    {
        public static List<OpeningThreads> AcilanIslemler = [];
        public static List<KontrolDetaylandirma> Takiptekiler = [];
        public static readonly ConcurrentDictionary<string, SemaphoreSlim> CoinLocks = new();

        public class Action
        {
            public string ActionType { get; set; }
            public string Title { get; set; }
            public string Icon { get; set; }
        }

        public class Info
        {
            public string TwitterId { get; set; }
            public bool IsReply { get; set; }
            public bool IsRetweet { get; set; }
            public bool IsQuote { get; set; }
            public bool IsSelfReply { get; set; }
        }

        public class Suggestion
        {
            public List<string> Found { get; set; }
            public string Coin { get; set; }
            public List<SymbolX> Symbols { get; set; }
            public double? Supply { get; set; }
            public bool IsAccountMapped { get; set; }
        }

        public class SymbolX
        {
            public string Exchange { get; set; }
            public string Symbol { get; set; }
        }

        public class ToTwitter
        {
            public List<Action> Actions { get; set; }
            public string Body { get; set; }
            public string Coin { get; set; }
            public string Icon { get; set; }
            public string Image { get; set; }
            public Info Info { get; set; }
            public string Link { get; set; }
            public bool RequireInteraction { get; set; }
            public bool ShowFeed { get; set; }
            public bool ShowNotif { get; set; }
            public List<Suggestion> Suggestions { get; set; }
            public long Time { get; set; }
            public string Title { get; set; }
            public string Type { get; set; }
            public List<object> Urls { get; set; }
            public string _Id { get; set; }
            public List<SymbolStatus> SymbolList { get; set; } = [];
        }

        public class ToSource
        {
            public List<Action> Actions { get; set; }
            public string En { get; set; }
            public int Delay { get; set; }
            public Dictionary<string, double> FirstPrice { get; set; }
            public string Source { get; set; }
            public List<Suggestion> Suggestions { get; set; }
            public List<string> Symbols { get; set; }
            public long Time { get; set; }
            public string Url { get; set; }
            public string Title { get; set; }
            public string _Id { get; set; }
            public List<SymbolStatus> SymbolList { get; set; } = [];
        }

        public class SymbolStatus
        {
            public string Symbol { get; set; }
            public double Rsi { get; set; }
            public decimal Fiyat { get; set; }
            public decimal PriceDegisim { get; set; }
            public decimal DegisimYuzde { get; set; }
            public string DegisimZaman { get; set; }
        }
        public class RsStatus
        {
            public string Symbol { get; set; }
            public DateTime Zaman { get; set; }
        }
        public class NewsJson
        {
            public DateTime Zaman { get; set; }
            public string Who { get; set; }
            public string En { get; set; }
            public List<SymbolStatus> SymbolList { get; set; } = [];
            public string Idm { get; set; }

        }

        public class GetLoginMongoDB
        {
            public object Id { get; set; }
            public string Secret { get; set; }
            public string Key { get; set; }
            public string EmptyThreads { get; set; }
            public string Threads { get; set; }
            public string Token { get; set; }
            public string TelegramBoss { get; set; }
        }

        public class OpeningThreads
        {
            public DateTime Zaman { get; set; }
            public string Coin { get; set; }
            public string FullText { get; set; }
            public string Body { get; set; }
            public string Who { get; set; }

            public decimal IlkFiyat { get; set; }
        }

        public class TPJson
        {
            public decimal TpPrice { get; set; }
            public decimal Amount { get; set; }
        }

        public class KontrolDetaylandirma
        {
            public string Coin { get; set; }
            public DateTime Date { get; set; } = DateTime.Now;
            public bool Mesgul { get; set; } = false;
        }

        public class LeverageJson
        {
            public object Id { get; set; }
            public string Symbol { get; set; }
            public long Cap { get; set; }
            public decimal Price { get; set; }
            public double? RsiDegeri { get; set; }
            public FuturesMarginType? MarginType { get; set; }
            public bool IsAutoAddMargin { get; set; }
            public decimal Leverage { get; set; }
            public decimal MaxNotionalValue { get; set; }
        }
        public class OnemliKelimeler
        {
            public string Borsa { get; set; }
            public string Kelime { get; set; }
        }

    }
}