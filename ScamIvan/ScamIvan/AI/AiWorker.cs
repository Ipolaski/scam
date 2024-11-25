using System.Configuration;
using System.Text.Json;
using ScamIvan.BrowserOperations.Entities;

namespace ScamIvan.AI
{
    /// <summary>
    /// Работа с ИИ запросами
    /// </summary>
    public class AiWorker
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _pathToScreenshot = string.Empty;
        private readonly string _screenshotName = string.Empty;

        //readonly string _periodOfInactivity = string.Empty;
        private readonly string _assistantId = string.Empty;
        private readonly string _token = string.Empty;
        private readonly string _checkStatusBaseAdress = string.Empty;
        private readonly string _createThreadBaseAdress = string.Empty;
        private readonly int _waitForAI = 0;
        private readonly int _maxRetry = 1;
        private readonly string _waitingStatus = string.Empty;
        private readonly string _message = string.Empty;

        public AiWorker(string pathToScreenshot, string screenshotName)
        {
            _pathToScreenshot = Path.Combine(pathToScreenshot, screenshotName);
            _screenshotName = screenshotName;

            _createThreadBaseAdress = ConfigurationManager.AppSettings["baseAddress"];
            _assistantId = ConfigurationManager.AppSettings["assistantId"];
            _token = ConfigurationManager.AppSettings["token"];
            _message = ConfigurationManager.AppSettings["message"];

            _waitingStatus = ConfigurationManager.AppSettings["completedstatus"];

            _checkStatusBaseAdress = ConfigurationManager.AppSettings["checkStatusAddress"];

            if (!int.TryParse($"{ConfigurationManager.AppSettings["waitingForAI"]}000", out _waitForAI))
            {
                Console.WriteLine($"Warn: Не получилось преобразовать конфиг waitingForAI = {ConfigurationManager.AppSettings["waitingForAI"]} в int");
            }

            if (!int.TryParse(ConfigurationManager.AppSettings["maximumNumberOfRepeatedRequests"], out _maxRetry))
            {
                Console.WriteLine($"Warn: Не получилось преобразовать конфиг maximumNumberOfRepeatedRequests = {ConfigurationManager.AppSettings["maximumNumberOfRepeatedRequests"]} в int");
            }
        }

        /// <summary>
        /// Отправляет запрос в ИИ со скриншотом графика и получает Id задачи которую нужно ждать
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetThreadId()
        {
            string answer = string.Empty;
            MultipartFormDataContent multipartFormDataContent = [];
            HttpResponseMessage responce;
            using (var fileStream = new StreamContent(File.OpenRead(_pathToScreenshot)))
            {
                fileStream.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
                multipartFormDataContent.Add(fileStream, name: "file", fileName: _screenshotName);
                multipartFormDataContent.Add(new StringContent(_message), name: "message");
                multipartFormDataContent.Add(new StringContent(_assistantId), "assistantId");
                multipartFormDataContent.Add(new StringContent(_token), "token");
                Console.WriteLine($"Создан контент для запроса");

                Console.WriteLine($"Отправляю POST запрос");

                responce = await _client.PostAsync(_createThreadBaseAdress, multipartFormDataContent);
            }

            if (responce != null)
            {
                ThreadIdEntity threadEntity = JsonSerializer.Deserialize<ThreadIdEntity>(responce.Content.ReadAsStream());

                if (threadEntity == null)
                {
                    Console.WriteLine("Warn: ThreadIdEntity == null");
                }
                else
                {
                    answer = threadEntity.threadId;
                }
            }
            else
            {
                Console.WriteLine(@"Warn: Пустой ответ
                    _client.PostAsync(string.Empty, multipartFormDataContent)");
            }

            return answer;
        }

        /// <summary>
        /// Отправляет запрос о статусе в ИИ и получает аналитику по отправленному ранее скриншоту, которую отдаёт
        /// </summary>
        /// <remarks>Если статус не завершён, то через промежуток времени делает повторный запрос</remarks>
        /// <param Name="threadId">Id задачи, которая была создана ранее POST запросом</param>
        /// <returns>Возвращает прогноз от ИИ по скриншоту</returns>
        public async Task<string> GetPrognoze(string threadId)
        {
            string addUri = $"?threadId={threadId}&token={_token}";
            // Todo: сделать бы это немножко аккуратнее, но быстро у меня не получилось, а тратить на это много времени не захотел.
            var url = $"{_checkStatusBaseAdress}{addUri}";
            PrognozEntity prognoz = new();

            HttpResponseMessage? responce;
            int i = 0;
            try
            {
                do
                {
                    Thread.Sleep(_waitForAI);

                    Console.WriteLine($"Проверяю статус по адресу: {url}");
                    responce = await _client.GetAsync(url);

                    if (responce != null)
                    {
                        prognoz = await JsonSerializer.DeserializeAsync<PrognozEntity>(responce.Content.ReadAsStream());

                        if (prognoz != null)
                        {
                            Console.WriteLine($"Проверка#{++i}: " +
                            $"status={prognoz.status}");
                        }
                        else
                        {
                            Console.WriteLine($@"Warn: Не получилось сереализовать ответ в прогноз
                                JsonSerializer.DeserializeAsync<PrognozEntity>(responce.Content.ReadAsStream());");
                        }
                    }
                    else
                    {
                        Console.WriteLine($@"Warn: Пришёл пустой ответ от ИИ
                            responce = await _client.GetAsync(url);");
                    }
                }
                while (prognoz.status != _waitingStatus && i <= _maxRetry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: message= {ex.Message}");
            }

            Console.WriteLine($@"Возвращаю полученый ответ в программу");
            return prognoz == null ? string.Empty : prognoz.response;
        }
    }
}
