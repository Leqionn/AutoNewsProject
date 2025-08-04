using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Web;
using Binance.Net.Enums;
using MongoDB.Driver;
using NAudio.Wave;
using Newtonsoft.Json;
using Telegram.Bot;

namespace AINews
{
    class FNC
    {
        public static async Task InternetKontrolAsync()
        {
            while (true)
            {

                try
                {
                    using Ping ping = new();
                    // Google'ın genellikle erişilebilir bir IP'sini kullanabilirsiniz.
                    PingReply reply = ping.Send("8.8.8.8");
                    if (reply.Status == IPStatus.Success)
                    {
                        LogTut("İnternet bağlantısı var.");
                        break;
                    }

                }
                catch (PingException)
                {
                    LogTut("İnternet bağlantısı yok.");
                    await Task.Delay(1000);
                }
            }

        }

        public static async Task KesintiKontrol()
        {
            string metrik = default!;
            DateTime kesimZaman = default!;
            using Ping ping = new();
            PingReply reply = default!;
            while (true)
            {
                try
                {
                    reply = ping.Send("8.8.8.8");
                    if (reply.Status == IPStatus.Success && metrik != default!)
                    {
                        metrik += $"\n{DateTime.Now} | İnternet bağlantısı geldi.";
                        metrik += $"\nGeçen Zaman: {DateTime.Now - kesimZaman}";
                        LogTut(metrik);
                        metrik = default!;
                        kesimZaman = default!;
                    }
                }
                catch
                {
                    if (metrik == default)
                    {
                        metrik = $"{DateTime.Now} | İnternet kesildi.";
                        kesimZaman = DateTime.Now;
                    }
                }
                await Task.Delay(1000);
            }
        }

        public static async Task<string> Translate(string word)
        {
            try
            {
                var toLanguage = "tr";//
                var fromLanguage = "en";//
                string translation = "";
                var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={fromLanguage}&tl={toLanguage}&dt=t&q={HttpUtility.UrlEncode(word)}";
                var client = new HttpClient();
                var response = await client.GetStringAsync(url);
                //JsonConvert.DeserializeObject<TwitterVersion1>(geldi);
                var jsonData = JsonConvert.DeserializeObject<List<dynamic>>(response);
                var translationItems = jsonData[0];

                // Translation Data
                foreach (object item in translationItems)
                {
                    // Convert the item array to IEnumerable
                    IEnumerable translationLineObject = item as IEnumerable;

                    // Convert the IEnumerable translationLineObject to a IEnumerator
                    IEnumerator translationLineString = translationLineObject.GetEnumerator();

                    // Get first object in IEnumerator
                    translationLineString.MoveNext();
                    if (Convert.ToString(translationLineString.Current).Contains("https") == false)
                    {
                        // Save its value (translated text)
                        translation += string.Format(" {0}", Convert.ToString(translationLineString.Current).Trim());
                    }
                }
                if (translation.Length < 5)
                {
                    translation = word;
                }
                return translation.TrimStart();

            }
            catch
            {
                return "Error";
            }
        }

        public static void CizgiCek()
        {
            string lineCharacter = "┄┄┄"; // Çizgi karakteri126
            string line = new(lineCharacter[0], 212);
            Console.WriteLine("\u001b[1;34m" + line);
        }

        public static string MsgCizgi()
        {
            string lineCharacter = "┄┄┄"; // Çizgi karakteri126
            return new(lineCharacter[0], 24);
        }

        public static void KonsolTemizle()
        {
            Console.WriteLine("\u001b[2J");
        }

        public static async Task ToTelegram(string mesaj)
        {
            var a = await BotAPI.SendMessage(Channel, mesaj);
            Ikaz(mesaj);
        }


        public static void ConsoleLog(string mesaj)
        {
            Console.WriteLine($"\u001b[1;34m[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\u001b[0m {mesaj}");
            CizgiCek();
        }
        public static void LogTut(string mesaj)
        {
            Console.WriteLine(ColorCode("purple", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mesaj}"));
            CizgiCek();
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"Log.ini");
                File.AppendAllText(filePath, $"\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mesaj}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log yazılamadı: " + ex.Message);
            }
        }
        public static void Ikaz(string mesaj)
        {
            Console.WriteLine(ColorCode("red", mesaj));
            CizgiCek();
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "Log.ini");
                File.AppendAllText(filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mesaj}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log yazılamadı: " + ex.Message);
            }
        }

        public static void RsiLog(string symbol, string mesaj)
        {
            mesaj = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {mesaj}\n";

            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{DateTime.Now.ToShortDateString()} - {symbol}.ini");
                File.AppendAllText(filePath, mesaj);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log yazılamadı: " + ex.Message);
            }
        }
        public static void NewsLog(string mesaj)
        {
            try
            {
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"NewsLog.ini");
                File.AppendAllText(filePath, "\n" + mesaj);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log yazılamadı: " + ex.Message);
            }
        }
        public static string HttpTemizle(string gelen)
        {
            return gelen.Replace(".com", "").Replace(".co", "").Replace(".http", "").Replace("https", "");
        }

        public static string LongToTime(long _longtime)
        {

            DateTime start = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime date = start.AddMilliseconds(_longtime).ToLocalTime();
            return date.ToString("yyyy-MM-dd HH:mm:ss", new CultureInfo("tr-TR"));

        }

        public static DateTime LongToDT(long _longtime)
        {
            DateTime start = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime date = start.AddMilliseconds(_longtime).ToLocalTime();
            return date;

        }

        public static bool IslemGirildimi_15Min(string Coin)
        {
            if (AcilanIslemler.Count == 0)
                return false;

            return AcilanIslemler
                .All(p => p.Coin == Coin && (DateTime.Now - p.Zaman).TotalMinutes <= 15);
        }

        public static string FixTo8Dec(decimal number)
        {
            string str = number.ToString(CultureInfo.InvariantCulture);

            if (str.Length > 8)
                return str[..8]; // Fazlaysa kes
            else
                return str.PadRight(8, '0'); // Kısaysa sağa 0 ekle
        }


        public static string FixTo2Dec(decimal number)
        {
            string[] parts = number.ToString(CultureInfo.InvariantCulture).Split('.');
            if (parts.Length == 1)
                return parts[0] + ".00";

            string integerPart = parts[0];
            string decimalPart = parts[1];

            if (decimalPart.Length > 2)
                decimalPart = decimalPart[..2];
            else
                decimalPart = decimalPart.PadRight(2, '0');

            return $"{integerPart}.{decimalPart}";
        }

        public static void PlayAlarm()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory);

            using var audioFile = new AudioFileReader(path + "alarm.mp3");
            using var outputDevice = new WaveOutEvent();
            outputDevice.Init(audioFile);
            outputDevice.Play();

            while (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                Thread.Sleep(500);
            }
        }

        public static bool BlockText(string kelime)
        {
            return kelimecikler.Any(k => kelime.Contains(k, StringComparison.CurrentCultureIgnoreCase));
        }

        public static bool BlockCoins(string coin)
        {
            return semboller.Any(k => coin.StartsWith(k, StringComparison.CurrentCultureIgnoreCase));
        }

        public static string ColorCode(string renk, string metin)
        {
            string sonuc = "\u001b[0m";
            switch (renk.ToLower()) // Büyük/küçük harf duyarlılığını kaldırmak için ToLower() ekledim
            {
                case "red": sonuc = "\u001b[1;31m"; break;
                case "green": sonuc = "\u001b[1;32m"; break;
                case "yellow": sonuc = "\u001b[1;33m"; break;
                case "blue": sonuc = "\u001b[1;34m"; break;
                case "purple": sonuc = "\u001b[1;35m"; break;
                case "magenta": sonuc = "\u001b[1;35m"; break;
                case "cyan": sonuc = "\u001b[1;36m"; break;
                case "white": sonuc = "\u001b[1;37m"; break; // Beyaz
                case "black": sonuc = "\u001b[1;30m"; break; // Siyah (arka plan rengiyle birlikte anlamlı)
                case "brightred": sonuc = "\u001b[91m"; break; // Parlak Kırmızı
                case "brightgreen": sonuc = "\u001b[92m"; break; // Parlak Yeşil
                case "brightyellow": sonuc = "\u001b[93m"; break; // Parlak Sarı
                case "brightblue": sonuc = "\u001b[94m"; break; // Parlak Mavi
                case "brightmagenta": sonuc = "\u001b[95m"; break; // Parlak Magenta
                case "brightcyan": sonuc = "\u001b[96m"; break; // Parlak Cyan
                case "brightwhite": sonuc = "\u001b[97m"; break; // Parlak Beyaz
                case "reset": sonuc = "\u001B[0m"; break;
                default: break; // Geçersiz renk için varsayılan (sıfırlama)
            }
            return sonuc + metin.Replace("\n", "\n" + sonuc) + "\u001b[0m";
        }

        public static void LogToFile(string content)
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "UserData.ini");
                File.AppendAllText(filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {content}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log yazılamadı: " + ex.Message);
            }
        }

        public static void LogToFileAnother(string content)
        {
            try
            {
                string filePath = Path.Combine(AppContext.BaseDirectory, "log.ini");
                File.AppendAllText(filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {content}\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Log yazılamadı: " + ex.Message);
            }
        }

        public static decimal Calc(decimal baslangicFiyati, decimal guncelFiyat)
        {
            if (baslangicFiyati == 0)
                return 0;

            return Math.Round((guncelFiyat - baslangicFiyati) / baslangicFiyati * 100, 2, MidpointRounding.AwayFromZero);

        }

        public static List<TPJson> TPNokta(decimal EntryOran, int TpNoktaSayisi, decimal Aralik, decimal quant, OrderSide GelenSide)
        {
            List<TPJson> listem = [];

            for (int i = 1; i <= TpNoktaSayisi; i++)
            {
                if (GelenSide == OrderSide.Sell)
                    listem.Add(new TPJson { Amount = quant / TpNoktaSayisi, TpPrice = EntryOran - (EntryOran / 100 * Aralik * i) });
                else if (GelenSide == OrderSide.Buy)
                    listem.Add(new TPJson { Amount = quant / TpNoktaSayisi, TpPrice = EntryOran + (EntryOran / 100 * Aralik * i) });
            }

            return listem;
        }

        public static void GoURL(string symbol)
        {
            string url = $"https://www.binance.com/en/futures/{symbol}";
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        public static async Task Algoritma1(NewsJson x)
        {
            List<string> a = [];
            Stopwatch watch = Stopwatch.StartNew();
            bool isActionTriggered = false;
            decimal priceChange = default;
            string fullText = string.Empty;
            decimal priceThreshold = 0.85m;

            foreach (var coin in x.SymbolList)
            {
                var filteredSymbols = Takiptekiler.Where(ox => ox.Coin == coin.Symbol);
                if (!filteredSymbols.Any())
                {
                    Takiptekiler.Add(new() { Coin = coin.Symbol });
                }
                else
                {
                    var sayac = (DateTime.Now - filteredSymbols.First().Date).TotalSeconds;
                    if (sayac < 61)
                    {
                        filteredSymbols.First().Mesgul = true;
                    }
                    else
                    {
                        Takiptekiler.RemoveAll(item => item.Coin == coin.Symbol);
                        Takiptekiler.Add(new() { Coin = coin.Symbol });
                    }
                }
            }

            while (watch.ElapsedMilliseconds <= 7000)
            {
                foreach (var coin in x.SymbolList)
                {
                    var coinLock = CoinLocks.GetOrAdd(coin.Symbol, _ => new SemaphoreSlim(1, 1));
                    await coinLock.WaitAsync();
                    try
                    {
                        var filteredSymbols = Takiptekiler.Where(ox => ox.Coin == coin.Symbol);
                        priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));

                        if (priceChange >= priceThreshold && !filteredSymbols.First().Mesgul && IslemGirildimi_15Min(coin.Symbol) && !PozStatus)
                        {
                            filteredSymbols.First().Mesgul = true;
                            isActionTriggered = true;

                            var result = await GoFutures(coin.Symbol, OrderSide.Sell);

                            fullText += $"FP: {coin.Fiyat}";
                            fullText += $"\nLP: {FixTo8Dec(Read_Price(coin.Symbol))} | %{FixTo8Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";
                            fullText = $"{HttpTemizle(x.En)}\n{MsgCizgi()}\n{result} | {x.Who}\n{MsgCizgi()}\n{fullText}";

                            AcilanIslemler.Add(new() { Zaman = x.Zaman, Coin = coin.Symbol, FullText = fullText, IlkFiyat = coin.Fiyat });
                            break;
                        }
                    }
                    finally
                    {
                        coinLock.Release();
                    }
                }

                Thread.Sleep(25);
                if (isActionTriggered) break;
            }
        }

        public static async Task Algoritma(NewsJson x)
        {
            List<string> a = [];
            Stopwatch watch = Stopwatch.StartNew();
            bool isActionTriggered = false;
            decimal priceChange = default;
            string fullText = string.Empty;
            decimal PriceDuvar1 = 1m;
            decimal PriceDuvar2 = 2m;
            foreach (var coin in x.SymbolList)
            {
                var filteredSymbols = Takiptekiler.Where(ox => ox.Coin == coin.Symbol);
                if (!filteredSymbols.Any())
                {
                    Takiptekiler.Add(new() { Coin = coin.Symbol });
                }
                else
                {
                    var sayac = (DateTime.Now - filteredSymbols.First().Date).TotalSeconds;
                    if (sayac < 61)
                    {
                        filteredSymbols.First().Mesgul = true;
                    }
                    else
                    {
                        Takiptekiler.RemoveAll(item => item.Coin == coin.Symbol);
                        Takiptekiler.Add(new() { Coin = coin.Symbol });
                    }
                }
            }

            while (watch.ElapsedMilliseconds <= 7000)
            {
                foreach (var coin in x.SymbolList)
                {

                    var coinLock = CoinLocks.GetOrAdd(coin.Symbol, _ => new SemaphoreSlim(1, 1));
                    await coinLock.WaitAsync();
                    try
                    {
                        var filteredSymbols = Takiptekiler.Where(ox => ox.Coin == coin.Symbol);
                        priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));

                        if (priceChange >= PriceDuvar1 && !filteredSymbols.First().Mesgul)
                        {
                            filteredSymbols.First().Mesgul = true;
                            x.En = await Translate(x.En);
                            isActionTriggered = true;

                            fullText = $"FP: {coin.Fiyat}";
                            fullText += $"\nLP: {FixTo8Dec(Read_Price(coin.Symbol))} | %{FixTo8Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";

                            ConsoleLog(ColorCode("red", $"{x.En}\n{MsgCizgi()}\n{x.Who} | {coin.Symbol}\n{fullText}"));

                            while (DateTime.Now.Second != 0)
                                Thread.Sleep(10);

                            priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));

                            if (IslemGirildimi_15Min(coin.Symbol) && priceChange >= PriceDuvar2 && !PozStatus)
                            {
                                //string result = await Binance.GoFutures(coin.Symbol, OrderSide.Sell);

                                fullText += $"\nCL: {FixTo8Dec(Read_Price(coin.Symbol))} | %{FixTo8Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";
                                fullText = $"{HttpTemizle(x.En)}\n{MsgCizgi()}\n{"result"} | {x.Who}\n{MsgCizgi()}\n{fullText}";
                                await ToTelegram(fullText);


                                AcilanIslemler.Add(new()
                                {
                                    Zaman = x.Zaman,
                                    Coin = coin.Symbol,
                                    FullText = fullText,
                                    IlkFiyat = coin.Fiyat
                                });
                            }
                            break;
                        }
                    }
                    finally
                    {
                        coinLock.Release();
                    }
                }

                Thread.Sleep(25);
                if (isActionTriggered) break;
            }
        }


        public static async Task AlgoritmaRSI(NewsJson x)
        {

            var watch = Stopwatch.StartNew();
            bool Ponzy = false;
            double rsiDuvar1 = 60, rsiDuvar2 = 88;
            foreach (var coin in x.SymbolList)
            {
                var takip = Takiptekiler.FirstOrDefault(t => t.Coin == coin.Symbol);
                if (takip == null)
                    Takiptekiler.Add(new() { Coin = coin.Symbol });
                else
                {
                    var saniye = (DateTime.Now - takip.Date).TotalSeconds;
                    if (saniye < 61)
                        takip.Mesgul = true;
                    else
                    {
                        Takiptekiler.RemoveAll(t => t.Coin == coin.Symbol);
                        Takiptekiler.Add(new() { Coin = coin.Symbol });
                    }
                }
            }

            while (watch.ElapsedMilliseconds <= 7000)
            {
                foreach (var coin in x.SymbolList)
                {
                    var coinLock = CoinLocks.GetOrAdd(coin.Symbol, _ => new SemaphoreSlim(1, 1));
                    await coinLock.WaitAsync();

                    try
                    {
                        var takip = Takiptekiler.FirstOrDefault(t => t.Coin == coin.Symbol);
                        if (takip == null || takip.Mesgul) continue;

                        decimal priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));
                        double rsi = Read_RSI(coin.Symbol);

                        if (priceChange > 1m && !BlockText(x.En + x.Who))
                        {
                            _ = Task.Run(() => RsBilgi(coin.Symbol, coin.Fiyat));
                            PlayAlarm();


                            takip.Mesgul = true;
                            Ponzy = true;
                            x.En = await Translate(x.En);

                            string fullText = $"FP: {FixTo8Dec(coin.Fiyat)} | Rsi: {FixTo2Dec((decimal)coin.Rsi)}\n" +
                                              $"LP: {FixTo8Dec(Read_Price(coin.Symbol))} | Rsi: {FixTo2Dec((decimal)rsi)} | %{FixTo2Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";

                            DateTime now = DateTime.Now;
                            DateTime next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
                            TimeSpan delay = next - now;
                            await Task.Delay(delay);
                            while (rsi >= rsiDuvar1 && rsi <= rsiDuvar2 && priceChange >= 1m && watch.ElapsedMilliseconds <= 140_000)
                            {
                                Thread.Sleep(10);
                                rsi = Read_RSI(coin.Symbol);
                                priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));
                            }
                            fullText += $"\nCL: {FixTo8Dec(Read_Price(coin.Symbol))} | Rsi: {FixTo2Dec((decimal)rsi)} | %{FixTo2Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";


                            if (rsi >= rsiDuvar2 && !PozStatus && !IslemGirildimi_15Min(coin.Symbol))
                            {
                                var result = await GoFutures(coin.Symbol, OrderSide.Sell);

                                string finalText = $"{x.Who}: {HttpTemizle(x.En)}\n{MsgCizgi()}\n{coin.Symbol}\n{fullText}";

                                AcilanIslemler.Add(new() { Zaman = x.Zaman, Coin = coin.Symbol, FullText = finalText, IlkFiyat = coin.Fiyat });
                            }
                            else
                            {

                                dongu = false;
                                string neden = "";

                                if (rsi <= rsiDuvar1)
                                    neden += $"🔴 Rsi ≤ {rsiDuvar1} - Status: {FixTo2Dec((decimal)rsi)}\n";
                                if (rsi < rsiDuvar2 && rsi > rsiDuvar1)
                                    neden += $"🟡 Rsi < {rsiDuvar2} - Status: {FixTo2Dec((decimal)rsi)}\n";
                                if (priceChange <= 1m)
                                    neden += $"🔴 %{FixTo2Dec(priceChange)}\n";

                                string debugText = $"{watch.ElapsedMilliseconds}ms\n{neden}{MsgCizgi()}\n{x.Who}: {HttpTemizle(x.En)}\n{MsgCizgi()}\n{coin.Symbol}\n{fullText}\n{MsgCizgi()}\n{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                                RsiLog(coin.Symbol, debugText);
                                await ToTelegram(debugText);
                            }

                            break;
                        }
                    }
                    finally
                    {
                        coinLock.Release();
                    }
                }

                Thread.Sleep(25);
                if (Ponzy) break;
            }
        }


        public static async Task AlgoritmaSon(NewsJson x)
        {
            var watch = Stopwatch.StartNew();
            bool Ponzy = false;
            //meşguliyet kontrolü
            foreach (var coin in x.SymbolList)
            {
                var takip = Takiptekiler.FirstOrDefault(t => t.Coin == coin.Symbol);
                if (takip == null)
                    Takiptekiler.Add(new() { Coin = coin.Symbol });
                else
                {
                    var saniye = (DateTime.Now - takip.Date).TotalSeconds;
                    if (saniye < 61)
                        takip.Mesgul = true;
                    else
                    {
                        Takiptekiler.RemoveAll(t => t.Coin == coin.Symbol);
                        Takiptekiler.Add(new() { Coin = coin.Symbol });
                    }
                }
            }

            // Onemli kelimeler kontrolü
            foreach (var ok in Onemliler)
            {
                if (x.Who.Contains(ok.Borsa, StringComparison.OrdinalIgnoreCase) && x.En.Contains(ok.Kelime, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var coin in x.SymbolList)
                    {
                        var coinLock = CoinLocks.GetOrAdd(coin.Symbol, _ => new SemaphoreSlim(1, 1));
                        await coinLock.WaitAsync();
                        try
                        {
                            if (!BlockCoins(coin.Symbol) && coin.Rsi < 60)
                            {
                                var takip = Takiptekiler.FirstOrDefault(t => t.Coin == coin.Symbol);
                                if (takip == null || takip.Mesgul) continue;
                                takip.Mesgul = true;
                                Ponzy = true;
                                decimal priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));
                                double rsi = Read_RSI(coin.Symbol);

                                string fullText = $"FP: {FixTo8Dec(coin.Fiyat)} | Rsi: {FixTo2Dec((decimal)coin.Rsi)}\n" +
                                                  $"LP: {FixTo8Dec(Read_Price(coin.Symbol))} | Rsi: {FixTo2Dec((decimal)rsi)} | %{FixTo2Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";
                                if (!PozStatus && !IslemGirildimi_15Min(coin.Symbol))
                                {
                                    var result = await GoFutures(coin.Symbol, OrderSide.Buy);
                                    string finalText = $"{x.Who}: {HttpTemizle(x.En)}\n{MsgCizgi()}\n{coin.Symbol}\n{fullText}";
                                    AcilanIslemler.Add(new() { Zaman = x.Zaman, Coin = coin.Symbol, FullText = finalText, IlkFiyat = coin.Fiyat });
                                }
                            }
                        }
                        finally
                        {
                            coinLock.Release();
                        }
                    }
                }
            }
            // Algoritma çalıştırma
            while (watch.ElapsedMilliseconds <= 7000)
            {
                foreach (var coin in x.SymbolList)
                {
                    var coinLock = CoinLocks.GetOrAdd(coin.Symbol, _ => new SemaphoreSlim(1, 1));
                    await coinLock.WaitAsync();

                    try
                    {
                        var takip = Takiptekiler.FirstOrDefault(t => t.Coin == coin.Symbol);
                        if (takip == null || takip.Mesgul) continue;

                        decimal priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));
                        double rsi = Read_RSI(coin.Symbol);

                        if (priceChange > 1m && !BlockCoins(coin.Symbol) && !BlockText(x.Who + x.En))
                        {
                            _ = Task.Run(() => RsBilgi(coin.Symbol, coin.Fiyat));
                            PlayAlarm();

                            takip.Mesgul = true;
                            Ponzy = true;
                            x.En = await Translate(x.En);

                            string fullText = $"FP: {FixTo8Dec(coin.Fiyat)} | Rsi: {FixTo2Dec((decimal)coin.Rsi)}\n" +
                                              $"LP: {FixTo8Dec(Read_Price(coin.Symbol))} | Rsi: {FixTo2Dec((decimal)rsi)} | %{FixTo2Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";

                            DateTime now = DateTime.Now;
                            DateTime next = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
                            TimeSpan delay = next - now;
                            await Task.Delay(delay);

                            rsi = Read_RSI(coin.Symbol);
                            priceChange = Calc(coin.Fiyat, Read_Price(coin.Symbol));

                            fullText += $"\nCL: {FixTo8Dec(Read_Price(coin.Symbol))} | Rsi: {FixTo2Dec((decimal)rsi)} | %{FixTo2Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";

                            if (priceChange > 1m && !PozStatus && !IslemGirildimi_15Min(coin.Symbol))
                            {
                                var result = await GoFutures(coin.Symbol, OrderSide.Sell);
                                string finalText = $"{x.Who}: {HttpTemizle(x.En)}\n{MsgCizgi()}\n{coin.Symbol}\n{fullText}";
                                AcilanIslemler.Add(new() { Zaman = x.Zaman, Coin = coin.Symbol, FullText = finalText, IlkFiyat = coin.Fiyat });

                            }
                            break;
                        }
                    }
                    finally
                    {
                        coinLock.Release();
                    }
                }
                Thread.Sleep(25);
                if (Ponzy) break;
            }
            if (dongu)
            {
                while (DateTime.Now.Second != 0)
                {
                }
                dongu = false;
            }

        }

        public static async Task RsiChecker()
        {
            List<RsStatus> rsList = [];
            ConsoleLog($"{ColorCode("red", $"{DateTime.Now} | RsiChecker başlatıldı.")}");

            while (true)
            {
                foreach (var item in CoinList_Threads)
                {
                    if (item.RsiDegeri > 75 && item.RsiDegeri != null && item.RsiDegeri != 0 && !PozStatus)
                    {
                        // Liste içinde o sembol daha önce eklenmiş mi?
                        var kayitli = rsList.FirstOrDefault(x => x.Symbol == item.Symbol);

                        if (kayitli != null)
                        {
                            // 15 dakika geçmediyse işlem yapma
                            if ((DateTime.Now - kayitli.Zaman).TotalMinutes < 30)
                                continue;

                            // 15 dakika geçmişse listeden çıkar
                            rsList.Remove(kayitli);
                        }

                        // Yeni kaydı ekle
                        rsList.Add(new RsStatus
                        {
                            Symbol = item.Symbol,
                            Zaman = DateTime.Now
                        });
                        var result = await GoFutures(item.Symbol, OrderSide.Sell);
                        string koruma = default!;
                        if (result != null)
                        {
                            koruma = await PlaceTPandSLAsync(item.Symbol, OrderSide.Sell, item.Price);
                        }

                        ConsoleLog($"{ColorCode("red", $"{item.Symbol} RSI: {Math.Round((decimal)item.RsiDegeri, 2)} Price: {item.Price} {koruma}")}");
                        PlayAlarm();
                    }
                }

                await Task.Delay(10);
            }
        }

        public static async Task<string> PlaceTPandSLAsync(string sym, OrderSide side, decimal entryPrice)
        {
            bool isLong = side == OrderSide.Buy;

            // TP %0.50, SL %1.5
            decimal tpPercentage = 0.50m;
            decimal slPercentage = 1.50m;

            // TP ve SL fiyat hesaplama
            decimal tpFiyat = entryPrice * (1 + tpPercentage / 100 * (isLong ? 1 : -1));
            decimal slFiyat = entryPrice * (1 + slPercentage / 100 * (isLong ? -1 : 1));

            // Emirleri gönder
            var tpResult = await TP_SL_Order(FuturesOrderType.TakeProfitMarket, sym, side, tpFiyat);
            var slResult = await TP_SL_Order(FuturesOrderType.StopMarket, sym, side, slFiyat);

            return $"TP: {tpFiyat} | SL: {slFiyat}\nTP Durumu: {tpResult}\nSL Durumu: {slResult}";
        }

        public static async Task RsBilgi(string symbol, decimal Price)
        {
            dongu = true;
            string sy = symbol.ToUpper();
            while (dongu)
            {
                RsiLog(sy, $"Symbol: {sy} | Price: {FixTo8Dec(Read_Price(sy))} | Rsi: {FixTo2Dec((decimal)Read_RSI(sy))} | %{FixTo2Dec(Calc(Price, Read_Price(sy)))}");
                await Task.Delay(1000);
            }
        }
    }
}
