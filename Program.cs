global using static AINews.Bino;
global using static AINews.ChartGenerator;
global using static AINews.FNC;
global using static AINews.JSListeler;
global using static AINews.Mongo_DB;
global using static AINews.Program;
global using static AINews.Telegram;
global using static AINews.WebSocket_;

namespace AINews
{
    class Program
    {

        public static string[] kelimecikler = ["upbit", "coinbase", "bithumb", "binance", "trump", "sec"]; //
        public static string[] semboller = ["BTC", "ETH", "XRP", "TRX", "TRUMP", "SOL", "BNB"]; //
        public static List<OnemliKelimeler> Onemliler =
        [
            new OnemliKelimeler { Borsa = "Binance", Kelime = "With Retroactive BNB Simple Earn Subscriptions" },
            new OnemliKelimeler { Borsa = "Upbit", Kelime = "new trading support" }
        ];

        static async Task Main(string[] args)
        {
            CizgiCek();
            await InternetKontrolAsync();
            LoginTaxMongoDBAsync();

            USDT_Limit_Bolumu = 4;
            await GuncelleAsync();


            PozStatus = await Read_Positions();

            await Read_FuturesSymbol();
            await CoinKline();
            await Socket_UserData();

            _ = Task.Run(ThreeWSSAsync);
            _ = Task.Run(KesintiKontrol);


            while (true)
            {
                string girilen = Console.ReadLine();
                if (girilen == "exit")
                {
                    dongu = false;

                }
                else
                {

                    bool SyVarmi = CoinList_Threads.Any(x => x.Symbol.Contains(girilen, StringComparison.CurrentCultureIgnoreCase));

                    if (SyVarmi)
                    {
                        _ = Task.Run(() => RsBilgi(girilen + "USDT", Read_Price(girilen.ToUpper() + "USDT")));
                        ConsoleLog(ColorCode("red", "Sembol bulundu."));
                    }
                    else
                    {
                        ConsoleLog(ColorCode("red", "Sembol bulunamadı!"));
                    }
                }

                Thread.Sleep(100);
            }
        }

        public static bool dongu = false;



    }

}