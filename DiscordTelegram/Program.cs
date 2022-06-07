using System.Collections.Specialized;
using System.Net;
using Discord.Webhook;
using Microsoft.Extensions.Configuration;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using File = System.IO.File;

namespace DiscordTelegram
{
    class Program 
    {
        private static readonly IConfiguration Config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        private static readonly ITelegramBotClient Bot = new TelegramBotClient(Config["Token"]);
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var d = new DiscordWebhookClient(Config["WebhookUrl"]);
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if(update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                var message = update.Message;
                if (message == null)
                {
                    return;
                }

                string raw = "";
                string path = "";
                if (message.VideoNote != null)
                {
                    raw = message.VideoNote.FileId;
                    path = raw + ".mp4";
                }
                else if (message.Video != null)
                {
                    raw = message.Video.FileId;
                    path = raw + ".mp4";
                }
                else if (message.Voice != null)
                {
                    raw = message.Voice.FileId;
                    path = raw + ".ogg";
                }
                else if(message.Text != null)
                {
                    await d.SendMessageAsync(message.Text, username: message.From.FirstName);
                    return;
                }
                else
                {
                    Console.WriteLine("хуй");
                    return;
                }
                
                var file = await Bot.GetFileAsync(raw, cancellationToken: cancellationToken);
                await using var saveImageStream = new FileStream(path, FileMode.Create);
                if (file.FilePath != null)
                    await Bot.DownloadFileAsync(file.FilePath, saveImageStream, cancellationToken);
                saveImageStream.Close();
                    
                await d.SendFileAsync(path, "** **", username: message.From.FirstName);
                    
                File.Delete(path);
            }
        }

        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        
        
        static void Main(string[] args) 
        {
            Console.WriteLine("Запущен бот " + Bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            Bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
            
            
            
        } 
    }
}