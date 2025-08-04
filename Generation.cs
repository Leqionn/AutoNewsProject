using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Authentication;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using SkiaSharp;
using Telegram.Bot;


namespace AINews
{
    public static class ChartGenerator
    {
        public static MemoryStream GenerateFuturesImage(
        string symbol,
        string direction,
        string leverage,
        decimal pnlText,
        string pnlPercent,
        string entryPrice,
        string closePrice,
        decimal hacim,
        decimal tlkazanc,
        DateTime timestamp)
        {
            // Arka planı yükle
            using var background = SKBitmap.Decode(Path.Combine(AppContext.BaseDirectory, "a.jpg"));
            using var surface = SKSurface.Create(new SKImageInfo(background.Width, background.Height));
            var canvas = surface.Canvas;

            // Arka planı çiz
            canvas.DrawBitmap(background, 0, 0);

            // Renkler
            var CoinBaslik = new SKPaint { Color = SKColors.White, IsAntialias = true, TextSize = 60, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) };
            var Title = new SKPaint { Color = SKColors.Gray, IsAntialias = true, TextSize = 30, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) };
            var miniTitle = new SKPaint { Color = SKColors.White, IsAntialias = true, TextSize = 35, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) };
            var saatTitle = new SKPaint { Color = SKColors.Gray, IsAntialias = true, TextSize = 30, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal) };

            var x = 60;
            var CoinAlti = new SKPaint
            {
                Color = direction == "Long" ? SKColors.MediumSpringGreen : SKColors.OrangeRed,
                IsAntialias = true,
                TextSize = 35,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            };

            var pnlFont = new SKPaint
            {
                Color = pnlText >= 0 ? SKColors.MediumSpringGreen : SKColors.OrangeRed,
                TextSize = 100,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            var pnlFontalti = new SKPaint
            {
                Color = pnlText >= 0 ? SKColors.MediumSpringGreen : SKColors.OrangeRed,
                TextSize = 35,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal)
            };
            // Yazılar
            canvas.DrawText(symbol.ToUpper(), x, 120, CoinBaslik);
            canvas.DrawText(direction + " " + leverage + "x", x, 170, CoinAlti);
            canvas.DrawText($"{(pnlText >= 0 ? "+" : "")}{pnlText:F2}$", x, 370, pnlFont);
            canvas.DrawText($"({(pnlText >= 0 ? "+" : "")}{pnlPercent:F2}%)", x, 415, pnlFontalti);


            canvas.DrawText("Entry Price", x, 670, Title);
            canvas.DrawText(entryPrice, x, 710, miniTitle);


            canvas.DrawText("Close Price", x + 250, 670, Title);
            canvas.DrawText(closePrice, x + 250, 710, miniTitle);

            canvas.DrawText("volume$:", x + 500, 670, Title);
            canvas.DrawText(hacim.ToString(), x + 640, 670, miniTitle);

            canvas.DrawText("Earning₺:", x + 500, 710, Title);
            canvas.DrawText(tlkazanc.ToString(), x + 640, 710, miniTitle);

            canvas.DrawText(timestamp.ToString("dd.MM.yyyy HH:mm:ss"), x + 850, 710, saatTitle);

            // Belleğe yaz
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var stream = new MemoryStream();
            data.SaveTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

    }

    public static class RSITracker
    {
        public static Dictionary<string, List<Quote>> SorguListe = [];

        public static Dictionary<string, List<IBinanceKline>> KlineDurumu = [];

        public static async Task IlkVeri(List<string> coinList, string identifier)
        {
            var tasks = new List<Task>();

            foreach (string symbol in coinList)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var result = await binanceClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                        symbol: symbol,
                        interval: KlineInterval.FiveMinutes,
                        limit: 300);

                    if (result.Success)
                        KlineDurumu[symbol] = [.. result.Data];
                    else
                        ConsoleLog($"Veri alınamadı: {symbol} - {result.Error}");
                }));
            }

            // Hepsini aynı anda başlat, sonra istersen bekle
            await Task.WhenAll(tasks);

            LogTut($"{identifier}.Tüm coin verisi alındı. {coinList.Count}");
        }

    }

    public static class Mongo_DB
    {
        public static readonly MongoClient Client = new("mongodb+srv://mmertsoylu:fvvu9KsjVDhiLr4y@mertdb.takrj1k.mongodb.net/?retryWrites=true&w=majority");

        public static readonly IMongoDatabase _database = Client.GetDatabase("newsDB");
        public static readonly IMongoCollection<GetLoginMongoDB> Login = _database.GetCollection<GetLoginMongoDB>("MLogin");
        public static readonly IMongoCollection<BinanceFuturesStreamOrderUpdateData> OrderData = _database.GetCollection<BinanceFuturesStreamOrderUpdateData>("OrderData");


        public static void LoginTaxMongoDBAsync()
        {
            try
            {
                // MongoDB koleksiyonuna erişim

                // İlk kaydı LINQ ile çekme<
                var x = Login.AsQueryable().FirstOrDefault();

                if (x != null)
                {
                    apikeyMert = x.Key;
                    secretMert = x.Secret;
                    Token = x.Token;
                    Boss = x.TelegramBoss;
                    BotAPI = new TelegramBotClient(Token);
                    binanceClient = new BinanceRestClient(options =>
                    {
                        options.ApiCredentials = new ApiCredentials(apikeyMert, secretMert);
                        options.Environment = BinanceEnvironment.Live;
                    });
                    _ = Task.Run(BotListen);

                    LogTut("Config dosyası çekildi.");
                }
                else
                {
                    LogTut("Config dosyası bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                LogTut($"Bağlantı hatası: {ex.Message}");
            }
        }

        public static async Task OrderData_Ekle(BinanceFuturesStreamOrderUpdateData update)
        {
            await OrderData.InsertOneAsync(update);
        }

        public static async Task<BinanceFuturesStreamOrderUpdateData> OrderData_Getir(long orderId)
        {
            var filter = Builders<BinanceFuturesStreamOrderUpdateData>.Filter.Eq(x => x.OrderId, orderId);
            return await OrderData.Find(filter).FirstOrDefaultAsync();
        }
    }

}