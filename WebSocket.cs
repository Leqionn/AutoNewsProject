
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using Binance.Net.Enums;
using Newtonsoft.Json;
using Websocket.Client;

namespace AINews
{
    class WebSocket_
    {
        public static Uri url = new("wss://news.treeofalpha.com/ws");
        public static ClientWebSocket client;
        public static CancellationTokenSource _cts = new();
        public static int _reconnectDelay = 1000; // 1 saniye
        public static async Task ThreeWSSAsync()
        {
            while (true)
            {
                try
                {
                    client = new ClientWebSocket();
                    await client.ConnectAsync(url, _cts.Token);
                    LogTut($"Soket-Bağlantı sağlandı --> wss://news.treeofalpha.com/ws açıldı");

                    await ReceiveAsync();
                }
                catch (Exception ex)
                {
                    LogTut($"[Error] {ex.Message}");
                }
                LogTut($"Soket-Bağlantı koptu --> [Reconnect] 1 saniye sonra tekrar deneniyor...");

                await Task.Delay(_reconnectDelay);
            }
        }
        public static async Task ReceiveAsync()
        {
            var buffer = new byte[8192];

            while (client.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result;
                var ms = new MemoryStream();

                do
                {
                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        LogTut("[Disconnect] Sunucu bağlantıyı kapattı");
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, _cts.Token);
                        return;
                    }

                    ms.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                string message = Encoding.UTF8.GetString(ms.ToArray());
                if (JsonConvert.DeserializeObject<ToSource>(message).Source == null)
                {
                    ProcessTwitter(JsonConvert.DeserializeObject<ToTwitter>(message));
                }
                else
                {
                    ProcessSource(JsonConvert.DeserializeObject<ToSource>(message));

                }

            }
        }

        public static async Task Bwe()
        {
            var url = new Uri("wss://bwenews-api.bwe-ws.com/ws");
            LogTut($"Soket-Bağlantı -->wss://bwenews-api.bwe-ws.com/ws");

            var exitEvent = new ManualResetEvent(false);

            using (var client = new WebsocketClient(url))
            {
                client.MessageReceived.Subscribe(

                    msg =>
                    {
                        ConsoleLog(ColorCode("magenta", msg.Text));
                        LogToFileAnother(msg.Text);
                    });
                await client.Start();
                exitEvent.WaitOne();
            }

            await Task.Delay(-1);

        }

        private static void ProcessTwitter(ToTwitter main)
        {
            main.Body = main.Body.Replace("\n", "");
            main.Title = main.Title.Contains("(@")
                ? "@" + main.Title.Split("(@")[1].Replace(")", "")
                : "@" + main.Title;
            ProcessMain(main.Title, main.Body, main.Time, main.Suggestions, "X", isSource: false);

        }

        private static void ProcessSource(ToSource main)
        {
            main.Title = main.Source == "Blogs" || main.Source == "Proposals"
                            ? main.Title.Split(": ")[0] : main.Source;

            main.En = main.En.Replace("\n", "").Replace(main.Title + ": ", "").Replace(main.Source, "");

            ProcessMain(main.Title, main.En, main.Time, main.Suggestions, main.Source, isSource: true);
        }

        private static void ProcessMain(string title, string body, long time, List<Suggestion> suggestions, string Src, bool isSource)
        {
            var sb = new StringBuilder();
            var symbolList = suggestions.SelectMany(s => s.Symbols).Where(s => s.Exchange == "binance-futures" && s.Symbol.EndsWith("USDT") && !s.Symbol.StartsWith("USDC"))
                .Select(s => new JSListeler.SymbolStatus
                {
                    Symbol = s.Symbol,
                    Fiyat = Read_Price(s.Symbol),
                    Rsi = Read_RSI(s.Symbol)
                }).ToList();

            foreach (var sy in symbolList)
            {
                string rsiText;

                if (sy.Rsi >= 70)
                {
                    rsiText = ColorCode("red", $"{sy.Symbol}({FixTo8Dec(sy.Fiyat)}) / {sy.Rsi:F2}");
                }
                else if (sy.Rsi <= 30)
                {
                    rsiText = ColorCode("yellow", $"{sy.Symbol}({FixTo8Dec(sy.Fiyat)}) / {sy.Rsi:F2}");
                }
                else
                {
                    rsiText = $"{sy.Symbol}({FixTo8Dec(sy.Fiyat)}) / {sy.Rsi:F2}";
                }

                sb.Append(rsiText + " | ");
            }


            ConsoleLog($"{sb}\n\u001b[1;34m{Src} | {title}: \u001b[1;0m{body}");

            // --- ❷ ALGORITMA SADECE SEMBOL VARSA ---
            if (symbolList.Count == 0) return;

            var algJson = new NewsJson
            {
                Zaman = LongToDT(time),
                Who = title,
                En = body,
                SymbolList = symbolList
            };

            _ = Task.Run(() => Algoritma(algJson));
        }

        public static async Task Algoritma(NewsJson x)
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


                        // RSI 60dan küçük-35dan büyükse %1 büyüklük durumunda işlem olacak
                        if (priceChange > 1m && coin.Rsi <= 60 && coin.Rsi > 30

                        && !BlockCoins(coin.Symbol) && !PozStatus && !IslemGirildimi_15Min(coin.Symbol))
                        {

                            string fullText = $"FP: {FixTo8Dec(coin.Fiyat)} | Rsi: {FixTo2Dec((decimal)coin.Rsi)}\n" +
                                              $"LP: {FixTo8Dec(Read_Price(coin.Symbol))} | Rsi: {FixTo2Dec((decimal)rsi)} | %{FixTo2Dec(priceChange)} | {watch.ElapsedMilliseconds} ms";
                            AcilanIslemler.Add(new() { Zaman = x.Zaman, Coin = coin.Symbol, FullText = fullText, Body = HttpTemizle(x.En), Who = x.Who, IlkFiyat = coin.Fiyat });

                            var result = await GoFutures(coin.Symbol, OrderSide.Buy);
                            if (!result.Success)
                            {
                                AcilanIslemler.RemoveAll(i => i.Coin == coin.Symbol);
                                await ToTelegram($"❌Başarısız | {coin.Symbol} | {await Translate(result.Error?.Message)}");
                            }

                            PlayAlarm();
                            takip.Mesgul = true;
                            Ponzy = true;

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

    }
}


/*
Upbit listeleme
omni 
haberden önce %15 yükselmiş
haberden sonra %105 yükselmiş toplam %120
rsi haberde 75 idi
*/