using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects;
using MongoDB.Bson;
using MongoDB.Driver;
using Skender.Stock.Indicators;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace AINews
{
    class Bino
    {
        #region Tanımlamalar  Listeler - Değişkenler
        public static bool PozStatus = false;

        public static decimal USDT_Limit, USDT_Limit_Bolumu, USDT_TRY, FundingFiyat, SpotFiyat;
        public static bool TpAyar = false;
        public static string apikeyMert = default!, secretMert = default!;
        public static BinanceRestClient binanceClient = new();
        public static BinanceFuturesUsdtExchangeInfo BFUExchangeInfo = new();
        public static List<LeverageJson> CoinList_Threads = [];
        public static List<BinanceFuturesStreamOrderUpdateData> PozData = [];
        public static List<BinancePositionDetailsUsdt> AcikPoz = [];

        #endregion

        #region Get - Read Fonksiyonlar
        public static async Task<string> Read_Wallet()
        {

            var xSpot = await binanceClient.SpotApi.Account.GetBalancesAsync();
            var xFtrs = await binanceClient.UsdFuturesApi.Account.GetBalancesAsync();

            var xSpotPara = xSpot.Data?.FirstOrDefault(a => a.Asset == "USDT");
            var xFtrsPara = xFtrs.Data?.FirstOrDefault(a => a.Asset == "USDT");

            SpotFiyat = xSpotPara == null ? 0 : Math.Round(xSpotPara.Available, 2);
            FundingFiyat = Math.Round(xFtrsPara.WalletBalance, 2);

            USDT_TRY = Math.Round((await binanceClient.SpotApi.ExchangeData.GetTickerAsync("USDTTRY")).Data.LastPrice, 2);
            USDT_Limit = Math.Round(FundingFiyat / USDT_Limit_Bolumu, 2);

            LogTut($"Hacim: {USDT_Limit}");
            string kasa = $"Kasa$: {Math.Round(FundingFiyat, 2)}";
            string hacim = $"Hacim$: {Math.Round(USDT_Limit, 2)}";
            int totalWidth = 80;

            string line = kasa.PadRight(totalWidth - hacim.Length) + hacim;

            string tarih = $"⏱️{DateTime.Now:dd.MM.yyyy HH:mm:ss}";

            return $"{line}\n{MsgCizgi()}\n{tarih}";
            //return $"Kasa$: {FundingFiyat} | Hacim$: {USDT_Limit}\n{MsgCizgi()}\n⏱️{DateTime.Now}";
        }

        public static async Task Read_FuturesSymbol()
        {
            BFUExchangeInfo = (await binanceClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync()).Data;

            var symbolConfigResult = await binanceClient.UsdFuturesApi.Account.GetSymbolConfigurationAsync();
            var asdasf = await binanceClient.UsdFuturesApi.ExchangeData.GetBookPricesAsync();
            string[] yasakliCoins = ["BTCSTUSDT"];

            foreach (var item in symbolConfigResult.Data)
            {
                if (item.Symbol.EndsWith("USDT") && asdasf.Data.Any(b => b.Symbol == item.Symbol))//&& !yasakliCoins.Any(item.Symbol.Contains)
                {
                    // Yeni coin bilgisi ekle
                    var leverage = new LeverageJson
                    {
                        Symbol = item.Symbol,
                        Leverage = item.Leverage,
                        Cap = 0,
                        Price = 0,
                        RsiDegeri = 0
                    };

                    CoinList_Threads.Add(leverage);
                }


            }

            LogTut($"{CoinList_Threads.Count} adet Sembol - Leverage eşleştirmesi tamamlandı. ");
        }

        public static async Task<bool> Read_Positions()
        {
            var a = await binanceClient.UsdFuturesApi.Account.GetPositionInformationAsync();
            AcikPoz = [.. a.Data.Where(x => x.EntryPrice != 0)];

            bool durum = AcikPoz.Count != 0;

            LogTut($"Poz Status: {durum}");


            return durum;
        }


        public static decimal Read_Price(string _CoinName)
        {
            return CoinList_Threads.FirstOrDefault(x => x.Symbol == _CoinName)?.Price ?? 0;
        }

        public static decimal Read_Leverage(string _CoinName)
        {
            return CoinList_Threads.FirstOrDefault(x => x.Symbol == _CoinName)?.Leverage ?? 20;
        }

        public static double Read_RSI(string _CoinName)
        {
            return Math.Round(CoinList_Threads.FirstOrDefault(x => x.Symbol == _CoinName)?.RsiDegeri ?? 0, 2);
        }

        public static long Read_Cap(string _CoinName)
        {
            return CoinList_Threads.FirstOrDefault(x => x.Symbol == _CoinName)?.Cap ?? 0;
        }
        #endregion

        #region Set - Write Fonksiyonlar
        public static void Write_PriceVal(string _CoinName, decimal fiyatla)
        {
            CoinList_Threads.FirstOrDefault(x => x.Symbol == _CoinName).Price = fiyatla;

        }

        public static void Write_RSIVal(string _CoinName, double? rsiVal)
        {
            CoinList_Threads.FirstOrDefault(x => x.Symbol == _CoinName).RsiDegeri = rsiVal;

        }

        public static void Write_Leverage(string Coin, decimal yeniLeverage)
        {
            CoinList_Threads.FirstOrDefault(x => x.Symbol == Coin).Leverage = yeniLeverage;
        }

        #endregion

        #region Order İşlemleri
        public static async Task<string> GoFuturesTrailing(string _CoinName, OrderSide _Order, decimal Koruma)
        {
            var _symbolData = BFUExchangeInfo.Symbols.SingleOrDefault(s => string.Equals(s.Name, _CoinName));
            var minQty = _symbolData.LotSizeFilter?.MinQuantity;
            var maxQty = _symbolData.LotSizeFilter?.MaxQuantity;
            var stepSize = _symbolData.LotSizeFilter?.StepSize;

            string gonderilecek = default!;
            OrderSide OrdDurum = OrderSide.Sell;
            decimal girilecektutar = USDT_Limit * Read_Leverage(_CoinName);
            decimal amount = girilecektutar / Read_Price(_CoinName);
            var Quantity = BinanceHelpers.ClampQuantity(minQty.Value, maxQty!.Value, stepSize!.Value, amount);

            LogTut($"Coin: {_CoinName} | Çarpan: {Read_Leverage(_CoinName)} | Miktar: {USDT_Limit} | Girilecek Tutar: {girilecektutar} | Adet: {Quantity} | {Read_Price(_CoinName)}");
            var orderId = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(_CoinName, _Order, FuturesOrderType.Market, Quantity);
            if (!orderId.Success)
            {
                gonderilecek = $"❌Başarısız | {_CoinName.Replace("USDT", "")} | {orderId.Error?.Message}";
            }
            else
            {
                gonderilecek = $"✅Başarılı | {_CoinName.Replace("USDT", "")}";
                OrdDurum = (_Order == OrderSide.Sell) ? OrderSide.Buy : OrdDurum;

                var Trailing = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(_CoinName, OrdDurum, FuturesOrderType.TrailingStopMarket, orderId.Data.Quantity, callbackRate: Koruma);

                if (Trailing.Success)
                {
                    gonderilecek += $"\nEntry: {Trailing.Data.ActivatePrice}\nQuanty: {Trailing.Data.Quantity}";
                }
            }

            return gonderilecek;

        }

        public static async Task<WebCallResult<BinanceUsdFuturesOrder>> GoFutures(string _CoinName, OrderSide _Order)
        {
            var _symbolData = BFUExchangeInfo.Symbols.SingleOrDefault(s => string.Equals(s.Name, _CoinName));
            var minQty = _symbolData.LotSizeFilter.MinQuantity;
            var maxQty = _symbolData.LotSizeFilter.MaxQuantity;
            var stepSize = _symbolData.LotSizeFilter.StepSize;

            decimal _TruLeverage = Read_Leverage(_CoinName);

            if (_TruLeverage < 20 && USDT_Limit > 240)
            {
                Ikaz($"Coin: {_CoinName} | Çarpan: {_TruLeverage} | ExMiktar: {USDT_Limit} | Yeni Miktar: 240 | Çarpan 20'ye çekildi, miktar sınırlandı.");
                _TruLeverage = 20;
                USDT_Limit = 240;
            }

            decimal girilecektutar = USDT_Limit * _TruLeverage;
            decimal amount = girilecektutar / Read_Price(_CoinName);
            var Quantity = BinanceHelpers.ClampQuantity(minQty, maxQty, stepSize, amount);

            Ikaz(ColorCode("green", $"Coin: {_CoinName} | Çarpan: {_TruLeverage} | Miktar: {USDT_Limit} | Girilecek Tutar: {girilecektutar} | Adet: {Quantity} | Fiyat: {Read_Price(_CoinName)}"));

            // Market emri oluşturma
            var orderId = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(_CoinName, side: _Order, FuturesOrderType.Market, Quantity);


            return orderId;


        }

        public static async Task CancelOrders(string _CoinName)
        {
            var openOrdersResult = await binanceClient.UsdFuturesApi.Trading.GetOpenOrdersAsync(_CoinName);
            if (!openOrdersResult.Success)
            {
                // Hata yönetimi
                LogTut($"GetOpenOrders-> Hata: {openOrdersResult.Error}");
                return;
            }
            foreach (var order in openOrdersResult.Data)
            {
                var cancelResult = await binanceClient.UsdFuturesApi.Trading.CancelOrderAsync(
                    symbol: order.Symbol,
                    orderId: order.Id);

                if (cancelResult.Success)
                    LogTut($"CancelOrder-> Sembol: {order.Symbol} | İptal edildi: {order.Id}");
                else
                    LogTut($"CancelOrder-> İptal hatası: {cancelResult.Error}");
            }
        }

        public static async Task<string> TP_SL_Order(FuturesOrderType TpmiSLmi, string _CoinName, OrderSide _Order, decimal PozKapat)
        {
            // Sembol bilgilerini al
            var _symbolData = BFUExchangeInfo.Symbols.SingleOrDefault(s => string.Equals(s.Name, _CoinName));
            if (_symbolData == null)
                return $"\n❌ TP başarısız: {_CoinName} için sembol bilgisi bulunamadı.";

            // TickSize'a göre fiyatı yuvarla
            var tickSize = _symbolData.PriceFilter.TickSize;
            int tickDecimals = 0;
            decimal tempTick = tickSize;

            while (tempTick < 1)
            {
                tempTick *= 10;
                tickDecimals++;
            }

            PozKapat = Math.Round(PozKapat, tickDecimals);

            // Emir gönder
            var result = await binanceClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: _CoinName,
                side: _Order == OrderSide.Buy ? OrderSide.Sell : OrderSide.Buy,
                type: TpmiSLmi,
                stopPrice: PozKapat,
                closePosition: true,
                quantity: 0
            );
            string a = $"TP/SL Emir Gönderildi: {_CoinName} - {TpmiSLmi} - {PozKapat}- {_Order}- {PozKapat.ToString("F" + tickDecimals)}";
            LogTut(a);
            return result.Success
                ? $"✅ {a}"
                : $"❌ TP başarısız: {result.Error?.Message}";
        }

        #endregion

        public static async Task ConfigureBinanceFutures()
        {
            int i = 1;
            var AllList = await binanceClient.UsdFuturesApi.ExchangeData.GetTickersAsync();
            if (AllList.Success)
            {
                foreach (var asset in AllList.Data)
                {
                    if (asset.Symbol.EndsWith("USDT"))
                    {

                        var a = await binanceClient.UsdFuturesApi.Account.ChangeMarginTypeAsync(asset.Symbol, FuturesMarginType.Isolated);
                        if (!a.Success)
                        {
                            var b = await binanceClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(asset.Symbol, 20);
                            if (!b.Success)
                            {
                                LogTut(asset.Symbol + " - " + b.Error);
                            }

                            LogTut(asset.Symbol + " - " + a.Error);
                        }

                    }
                }

                LogTut(i + " Kaldıraç bilgileri girildi.");

            }

        }

        public static async Task CoinKline()
        {
            int CCount = (int)Math.Ceiling(CoinList_Threads.Count / 100.0);  // Yuvarlama hatasını önledik

            for (int i = 0; i < CCount; i++)
            {
                var coins = CoinList_Threads
                    .OrderBy(x => x.Symbol)
                    .Skip(i * 100)
                    .Take(100)
                    .Select(x => x.Symbol)
                    .ToList();

                await RSITracker.IlkVeri(coins, (i + 1).ToString());
                await SubscribeToKlineUpdates(coins, (i + 1).ToString());

                await Task.Delay(100);  // Binance API'yi zorlamamak için 100ms gecikme koyduk
            }

        }

        public static async Task SubscribeToKlineUpdates(List<string> coinList, string identifier)
        {
            var socketClient = new BinanceSocketClient();
            var stat = await socketClient.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(coinList, interval: KlineInterval.FiveMinutes, data =>
            {


                if (RSITracker.KlineDurumu[data.Data.Symbol].Count > 0)
                {
                    if (data.Data.Data.OpenTime != RSITracker.KlineDurumu[data.Data.Symbol][^1].OpenTime)
                    {
                        RSITracker.KlineDurumu[data.Data.Symbol].RemoveAt(0);

                    }

                    else
                    {
                        RSITracker.KlineDurumu[data.Data.Symbol].RemoveAt(RSITracker.KlineDurumu[data.Data.Symbol].Count - 1);
                    }
                    RSITracker.KlineDurumu[data.Data.Symbol].Add(data.Data.Data);

                    RSITracker.SorguListe[data.Data.Symbol] = [.. RSITracker.KlineDurumu[data.Data.Symbol].Select(k => new Quote
                {
                    Date = k.OpenTime,
                    Open = k.OpenPrice,
                    High = k.HighPrice,
                    Low = k.LowPrice,
                    Close = k.ClosePrice
                })];
                    Write_PriceVal(data.Data.Symbol, data.Data.Data.ClosePrice);
                    Write_RSIVal(data.Data.Symbol, RSITracker.SorguListe[data.Data.Symbol].GetRsi(22).LastOrDefault()?.Rsi);

                }


            });

            if (stat.Success)
            {
                LogTut($"{identifier}.Coin bilgi Soketi açıldı. {coinList.Count}");
            }
            else
            {
                LogTut($"{identifier}.Coin bilgi Soketi açılamadı. Tekrarlanıyor.\nHata Kodu: {stat.Error.Message}");
            }
        }

        public static async Task Socket_UserData()
        {
            var ListenKey = await binanceClient.UsdFuturesApi.Account.StartUserStreamAsync();

            if (!ListenKey.Success) return;
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(59));
                    await binanceClient.UsdFuturesApi.Account.KeepAliveUserStreamAsync(ListenKey.Data);

                }
            });

            BinanceSocketClient binanceSocketClient = new();
            var UserDataSocket = await binanceSocketClient.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(ListenKey.Data,

                onLeverageUpdate =>
                {
                    var data = onLeverageUpdate.Data;
                    LogTut($"{data.EventTime.AddHours(3)} : Olay(Leverage Change) Coin: {data.LeverageUpdateData.Symbol} | Leverage: {data.LeverageUpdateData.Leverage}");
                    Write_Leverage(data.LeverageUpdateData.Symbol, data.LeverageUpdateData.Leverage);
                },

                onMarginUpdate =>
                {
                    foreach (var a in onMarginUpdate.Data.Positions)
                    {
                        LogTut($"{onMarginUpdate.Data.EventTime.AddHours(3)} : Olay(Margin Call) Coin: {a.Symbol} | MarginType: {a.MarginType} | Side:  {a.PositionSide} | Unrealized: {a.UnrealizedPnl} | Kalan: {a.MaintMargin}");
                    }
                },

                async onAccountUpdate =>
                {
                    LogToFile(onAccountUpdate.Data.ToJson());
                    switch (onAccountUpdate.Data.UpdateData.Reason)
                    {
                        case AccountUpdateReason.Deposit:
                            foreach (var a in onAccountUpdate.Data.UpdateData.Balances)
                            {
                                FundingFiyat = a.WalletBalance; SpotFiyat += a.BalanceChange * -1;
                                LogTut($"{onAccountUpdate.Data.EventTime.AddHours(3)} : Spot --> Futures : {a.BalanceChange}$");
                            }
                            await GuncelleAsync2();
                            break;

                        case AccountUpdateReason.Withdraw:
                            foreach (var a in onAccountUpdate.Data.UpdateData.Balances)
                            {
                                FundingFiyat = a.WalletBalance; SpotFiyat += a.BalanceChange * -1;
                                LogTut($"{onAccountUpdate.Data.EventTime.AddHours(3)} : Futures --> Spot : {a.BalanceChange}$");

                            }

                            await GuncelleAsync2();

                            break;

                        case AccountUpdateReason.Order:
                            FundingFiyat = onAccountUpdate.Data.UpdateData.Balances.FirstOrDefault().WalletBalance;

                            if (onAccountUpdate.Data.UpdateData.Positions.Any(p => p.Quantity == 0))
                            {
                                await GuncelleAsync2();
                            }


                            break;

                        case AccountUpdateReason.MarginTypeChange:
                            foreach (var a in onAccountUpdate.Data.UpdateData.Positions)
                                LogTut($"{onAccountUpdate.Data.EventTime.AddHours(3)} : Olay(MarginType Change) Coin: {a.Symbol} | MarginType: {a.MarginType}");
                            break;
                    }

                },

                async onOrderUpdate =>
                {
                    var XData = onOrderUpdate.Data.UpdateData;
                    LogToFile(XData.ToJson());
                    PozData.Add(XData);

                    if (XData.Status != OrderStatus.Filled || XData.ExecutionType != ExecutionType.Trade)
                        return;

                    bool isLong = XData.Side == OrderSide.Sell;
                    string nedir = isLong ? "Short" : "Long";
                    decimal leverage = Read_Leverage(XData.Symbol);
                    DateTime time = onOrderUpdate.Data.EventTime.AddHours(3);

                    // POZİSYON AÇILDIĞINDA
                    var hacim = Math.Round(XData.AveragePrice * XData.Quantity / leverage, 2);


                    if (XData.RealizedProfit == 0)
                    {
                        PozStatus = true;
                        Thread.Sleep(500);

                        var acilan = AcilanIslemler.FirstOrDefault(v => v.Coin.Contains(XData.Symbol));
                        var acikPoz = (AcikPoz ?? Enumerable.Empty<BinancePositionDetailsUsdt>())
                                        .Where(b => b.Symbol == XData.Symbol);
                        string message;

                        string header = $"Coin: {XData.Symbol}\nSide: {nedir}\nEntry: {XData.AveragePrice}" +
                                        $"\nHacim$: {hacim}" +
                                        $"\nLeverage: {leverage}\nRsi: {FixTo2Dec((decimal)Read_RSI(XData.Symbol))}";

                        if (acilan == null)
                        {
                            bool ilkIslem = !acikPoz.Any(); // yani hiç eşleşen yoksa
                            message = (ilkIslem ? "📣İşlem Açıldı\n" : "📣Ekleme yapıldı\n") + header + $"\n{MsgCizgi()}\n{time}";
                        }
                        else
                        {
                            decimal rsi = Convert.ToDecimal(Read_RSI(onOrderUpdate.Symbol));
                            decimal fark = Calc(acilan.IlkFiyat, XData.AveragePrice);
                            double gecenMs = (time - acilan.Zaman).TotalMilliseconds;

                            acilan.FullText = $"{acilan.Who}: {await Translate(acilan.Body)}\n{MsgCizgi()}\n{XData.Symbol}\n{acilan.FullText}";

                            message = $"🔹{acilan.FullText}\nEP: {FixTo8Dec(XData.AveragePrice)} | Rsi: {FixTo2Dec(rsi)}" +
                                      $" | %{FixTo2Dec(fark)} | {Math.Floor(gecenMs)} ms\n{MsgCizgi()}" +
                                      $"\nSide: {nedir} - Hacim$: {hacim} - Leverage: {leverage}" +
                                      $"\n{MsgCizgi()}\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]";

                            if (TpAyar)
                            {
                                decimal yuzdeFark = fark * 0.70m;
                                decimal tpFiyat = XData.AveragePrice * (1 + yuzdeFark / 100 * (isLong ? -1 : 1));
                                var result = await TP_SL_Order(FuturesOrderType.TakeProfitMarket, XData.Symbol, XData.Side, tpFiyat);
                                await ToTelegram($"{XData.Symbol} - Safely: {result.ToJson()}");
                            }
                        }
                        GoURL(XData.Symbol);

                        RsiLog(XData.Symbol, message);
                        await ToTelegram(message);
                        PozStatus = await Read_Positions();
                        AcilanIslemler.RemoveAll(i => i.Coin == XData.Symbol);

                        return;
                    }


                    // POZİSYON KAPANDIĞINDA
                    if (XData.RealizedProfit != 0 && XData.ExecutionType == ExecutionType.Trade)
                    {
                        if (XData.RealizedProfit != 0)
                        {

                            // Liquidation kontrolü
                            if (XData.Type == FuturesOrderType.Liquidation)
                            {
                                string msg = $"💸 {XData.Symbol.Replace("USDT", "")} | Liquidation\n❌ Zarar: ${Math.Round(XData.RealizedProfit + XData.Fee, 3)}\n{MsgCizgi()}\n{time}";
                                AcilanIslemler.RemoveAll(i => i.Coin == XData.Symbol);
                                PozStatus = await Read_Positions();
                                return;
                            }

                            // Pozisyon tamamen kapanmışsa (tam miktar dolduysa)
                            if (XData.Status == OrderStatus.Filled)
                            {
                                var PozisyonDatasi = PozData.Where(p => p.Symbol == XData.Symbol).ToList();
                                decimal toplamProfit = PozisyonDatasi.Sum(p => p.RealizedProfit);
                                decimal toplamFee = PozisyonDatasi.Sum(p => p.Fee);
                                decimal netProfit = toplamProfit - toplamFee;
                                decimal yuzde = Math.Round(netProfit / (XData.AveragePrice * XData.Quantity / leverage) * 100, 2);
                                string kz = netProfit > 0 ? "✅ Kâr" : "❌ Zarar";

                                await CancelOrders(XData.Symbol);
                                var Acik = AcikPoz.Where(b => b.Symbol == XData.Symbol);

                                var stream = GenerateFuturesImage(
                                    symbol: XData.Symbol,
                                    direction: isLong ? "Long" : "Short",
                                    leverage: leverage.ToString(),
                                    pnlText: Math.Round(netProfit, 3),
                                    pnlPercent: yuzde.ToString(),
                                    entryPrice: FixTo8Dec(Acik.First().EntryPrice).ToString(),
                                    closePrice: FixTo8Dec(XData.AveragePrice).ToString(),
                                    hacim: hacim,
                                    tlkazanc: Math.Round(netProfit * USDT_TRY, 2),
                                    timestamp: time
                                );
                                string msg2 = string.Join("\n", new[]
                                {
                                    $"💸 {XData.Symbol.Replace("USDT", "")} | %{yuzde}",
                                    $"📈 Giriş: ${FixTo8Dec(Acik.First().EntryPrice)}",
                                    $"📉 Kapanış: ${FixTo8Dec(XData.AveragePrice)}",
                                    $"📊 Hacim$: {hacim}",
                                    $"💰 Kar: ${Math.Round(netProfit, 3)} / ₺{Math.Round(netProfit * USDT_TRY, 2)}",
                                    $"⚙️ Pozisyon: {(isLong ? "Long" : "Short")} x{leverage}",
                                    $"🕒 {time:yyyy-MM-dd HH:mm:ss}",
                                    MsgCizgi()
                                });
                                LogTut("\n" + msg2);
                                RsiLog(XData.Symbol, $"\n{msg2}");
                                await BotAPI.SendPhoto(Channel, new InputFileStream(stream));
                                AcilanIslemler.RemoveAll(i => i.Coin == XData.Symbol);

                                PozStatus = await Read_Positions();
                                dongu = false;

                                PozData.RemoveAll(p => p.Symbol.Contains(XData.Symbol, StringComparison.OrdinalIgnoreCase));
                            }
                            else
                            {
                                decimal kirpilan = XData.Quantity * XData.PriceLastFilledTrade / leverage;
                                await ToTelegram($"Poz Kırpıldı: {kirpilan}$\n{MsgCizgi()}\n{time}");
                            }


                        }
                    }
                },

                onTradeUpdateata => { },

                onListenKeyExpired => { },

                onGridUpdate => { },

                onStrategyUpdate => { },

                onOrderTriggerUpdate => { });

            if (UserDataSocket.Success)
            {
                LogTut($"Account info Soketi açıldı.");
            }
        }
    }
}