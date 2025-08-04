using System.Globalization;
using MongoDB.Bson;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace AINews
{
    class Telegram
    {
        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery.From.Id.ToString() == Boss)
            {
                switch (update.Type)
                {
                    case UpdateType.CallbackQuery:
                        switch (update.CallbackQuery.Data)
                        {
                            case "botaktif":
                                await FNC.ToTelegram($"Bot Aktif \n{FNC.MsgCizgi()}\n{DateTime.Now}");
                                break;

                            case "TpAyar":
                                TpAyar = !TpAyar;
                                FNC.ConsoleLog($"Take Profit Ayarlama durumu: {TpAyar}");
                                await GuncelleAsync();
                                break;

                            case "bakiyever":
                                var existingMessage = update.CallbackQuery.Message;
                                var existingText = existingMessage.Text;
                                string Guncel = await Read_Wallet();
                                if (existingText != Guncel)
                                {
                                    FNC.ConsoleLog(Guncel + "\n" + existingMessage.MessageId);
                                    await BotAPI.EditMessageText(existingMessage.Chat.Id, existingMessage.MessageId, Guncel, replyMarkup: existingMessage.ReplyMarkup,
                                        cancellationToken: cancellationToken);
                                }
                                break;

                            case "iptal":
                                var XexistingMessage = update.CallbackQuery.Message;
                                await BotAPI.EditMessageText(XexistingMessage.Chat.Id, XexistingMessage.MessageId, XexistingMessage.Text,
                                        cancellationToken: cancellationToken);
                                break;
                        }
                        break;

                    default:
                        FNC.ConsoleLog($"{update.Type}: {update.ToJson()}");
                        break;
                }
            }
            if (update.Type == UpdateType.Message && update.Message!.Text != null)
            {
                var chatId = update.Message.Chat.Id;
                var chatName = update.Message.Chat.FirstName + " " + update.Message.Chat.LastName;
                var messageText = update.Message.Text;
                var messageDate = update.Message.Date.ToString("ddd HH:mm:ss", new CultureInfo("tr-TR"));

                FNC.ConsoleLog($"{FNC.ColorCode("yellow", $"{messageDate} | {chatName} ({chatId}): {messageText}")}");
                if (update.Message!.Text.Contains("USDT", StringComparison.CurrentCultureIgnoreCase))
                {
                    await FNC.ToTelegram($"{update.Message!.Text.ToUpper()}\nPrice: {Read_Price(update.Message!.Text.ToUpper())}\nRsi: {Read_RSI(update.Message!.Text.ToUpper())}\nLvrg: {Read_Leverage(update.Message!.Text.ToUpper())}");

                }
                if (update.Message!.Text.Contains("ver", StringComparison.CurrentCultureIgnoreCase))
                {
                    FNC.ConsoleLog(CoinList_Threads.OrderBy(x => x.Symbol).ToJson());
                }
            }
        }

        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Hata oluştu: {exception.Message}");
            return Task.CompletedTask;
        }

        private static CancellationTokenSource Cts = new();

        public static string Token = default!;

        public static TelegramBotClient BotAPI = default!;

        public static string Boss = default!;

        public static int BossPinnedMessage = 9165;//8082;

        public static long Channel = -4923572096;

        public static async Task BotAcilisBilgi()
        {
            string lolo = "Futures$: " + FundingFiyat + "\nMiktar$: " + USDT_Limit;
            Console.WriteLine(lolo);
            InlineKeyboardMarkup inlineKeyboard = new(
            [
                [InlineKeyboardButton.WithCallbackData(text: "🪬 Durum", callbackData: "botaktif"),
                InlineKeyboardButton.WithCallbackData(text: "💸 Bakiye", callbackData: "bakiyever"),
                ],

            ]);

            var a = await BotAPI.SendMessage(Channel, lolo, replyMarkup: inlineKeyboard);
            Console.WriteLine(a.Chat.Id);
            Console.WriteLine(a.MessageId);

        }

        public static async Task BotListen()
        {
            Cts?.Cancel();
            Cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [] // Tüm güncellemeleri al
            };

            BotAPI.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                Cts.Token
            );

            var botUser = await BotAPI.GetMe();
            LogTut($"Telegram Bot Başladı: @{botUser.Username}");

        }

        public static async Task GuncelleAsync()
        {
            var inlineKeyboard = new InlineKeyboardMarkup(
             [
                [
                    InlineKeyboardButton.WithCallbackData($"🌀Tp Ayar - {TpAyar} ", "TpAyar"),
                ]
            ]);
            // Mesajı güncelleme

            await BotAPI.EditMessageText(chatId: Channel, messageId: BossPinnedMessage, text: await Read_Wallet(), replyMarkup: inlineKeyboard);

            LogTut($"Take Profit Ayarlama durumu: {TpAyar}");
        }

        public static async Task GuncelleAsync2()
        {
            var inlineKeyboard = new InlineKeyboardMarkup(
             [
                [
                    InlineKeyboardButton.WithCallbackData($"🌀Tp Ayar - {TpAyar} ", "TpAyar"),
                ]
            ]);
            // Mesajı güncelleme
            USDT_Limit = FundingFiyat / USDT_Limit_Bolumu;

            string guncl = $"Kasa$: {Math.Round(FundingFiyat, 3)} | Hacim$:{Math.Round(USDT_Limit, 2)}\n{MsgCizgi()}\n⏱️{DateTime.Now}";
            string kasa = $"Kasa$: {Math.Round(FundingFiyat, 2)}";
            string hacim = $"Hacim$: {Math.Round(USDT_Limit, 2)}";
            int totalWidth = 80;

            string line = kasa.PadRight(totalWidth - hacim.Length) + hacim;

            string tarih = $"⏱️{DateTime.Now:dd.MM.yyyy HH:mm:ss}";

            string mesaj = $"{line}\n┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n{tarih}";
            await BotAPI.EditMessageText(chatId: Channel, messageId: BossPinnedMessage, text: mesaj, replyMarkup: inlineKeyboard);
        }



    }
}
