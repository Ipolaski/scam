using System.Configuration;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ScamIvan.Telegram
{

    /// <summary>
    /// Работа с отправкой сообщений в телеграмм
    /// </summary>
    public class TelegramWorker
    {
        private readonly ChatId _chatId;
        private readonly string _filepath;
        private readonly ITelegramBotClient _botClient;

        //readonly private byte _notificationCount = 0;

        private readonly Dictionary<string, string> _emoji = new Dictionary<string, string>() {
            { "Направление:", "📈 <b>Направление:</b>" },
            { "Временной промежуток:", "⏳ <b>Временной промежуток:</b>" },
            { "Описание:", "📊 <b>Описание:</b>" }
        };


        public TelegramWorker(string pathToScreenshot, string screenshotName)
        {
            _botClient = new TelegramBotClient(ConfigurationManager.AppSettings["tgToken"]);
            _chatId = ConfigurationManager.AppSettings["chatId"];
            _filepath = Path.Combine(pathToScreenshot, screenshotName);
            //_notificationCount = (byte)_notification.Count();

        }

        /// <summary>
        /// Уведомление за две минуты до токо, как отправится прогноз
        /// </summary>
        /// <param name="caption">Сообщение</param>
        /// <returns></returns>
        public async Task<bool> SendNotificationMessage(string tradePair = "")
        {
            var answer = false;
            Message message = new Message();
            string _notification = $"<b>💱Валютная пара сигнала:</b> {tradePair} \n\n📊 Сигнал от бота в течение нескольких минут, будьте готовы!\n\n💹 Подготовьте сумму входа в соответствии с риск-менеджментом – не более 2% от вашего баланса.";

            Console.WriteLine("Отправляю сообщение-уведомление перед сигналом");
            message = await _botClient.SendTextMessageAsync(chatId: _chatId, text: _notification, parseMode: ParseMode.Html);

            if (message != null)
            {
                answer = message.MessageId > 0;
            }

            return answer;
        }

        /// <summary>
        /// Отправляет аналитику по картинке в ТГ канал
        /// </summary>
        /// <param name="caption">Подпись</param>
        /// <param name="tradePair">Торговая пара</param>
        /// <returns></returns>
        public async Task<bool> SendAnalyticMessage(string caption = "", string tradePair = "")
        {
            var answer = false;

            Console.WriteLine($"Собираю сообщение для бота");

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"💱 <b>Валютная пара:</b> {tradePair}");
            stringBuilder.AppendLine($"{caption}");
            MakePrettyMessage(stringBuilder);



            Message message = new Message();
            using (Stream stream = System.IO.File.OpenRead(_filepath))
            {
                Console.WriteLine("Отправляю сигнал");

                message = await _botClient.SendPhotoAsync(chatId: _chatId, photo: new InputFileStream(stream), caption: stringBuilder.ToString(), parseMode: ParseMode.Html);
            }

            if (message != null)
            {
                answer = message.MessageId > 0;
            }

            return answer;
        }

        private bool MakePrettyMessage(StringBuilder textFromAI)
        {

            foreach (string k in _emoji.Keys)
            {
                textFromAI.Replace(k, _emoji[k]);
            }

            return true;
        }
    }
}