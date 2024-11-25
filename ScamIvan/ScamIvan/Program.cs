// See https://aka.ms/new-console-template for more information
using System.Configuration;
using ScamIvan.AI;
using ScamIvan.BrowserOperations.Entities;
using ScamIvan.CustomSectionIntoConfig;
using ScamIvan.Telegram;
using ScamIvan.TradingViewOpertors;

#region Preparing
Console.WriteLine("Получаю торговые пары из конфига...");
List<TradePairsEntity> tradePairs = [];
var tradePairsSection = ConfigurationManager.GetSection("tradepairs") as TradePairsSection;
foreach (TradePairsElement tradePair in tradePairsSection.Instances)
{
    tradePairs.Add(new TradePairsEntity(tradePair.Name, tradePair.Value));
}
tradePairsSection = null;

Console.WriteLine("Получаю путь до картинки и период постинга из конфига...");
string pathToScreenshot = ConfigurationManager.AppSettings["pathToScreenshot"];
string screenshotName = ConfigurationManager.AppSettings["screenshotName"];
string minPeriodOfInactivity = ConfigurationManager.AppSettings["minPeriodOfInactivity"];
string maxPeriodOfInactivity = ConfigurationManager.AppSettings["maxPeriodOfInactivity"];
string timeForSignalAfterNotification = ConfigurationManager.AppSettings["timeForSignalAfterNotification"];

BrowserWorker browserWorker = new BrowserWorker(pathToScreenshot, screenshotName);

Console.WriteLine("Рассчитываю и заполняю внутренние переменные...");
AiWorker aiWorker = new AiWorker(pathToScreenshot, screenshotName);
TelegramWorker telegramWorker = new TelegramWorker(pathToScreenshot, screenshotName);
int timeBetwinPrognoses = GetRandomInt(int.Parse(minPeriodOfInactivity), int.Parse(maxPeriodOfInactivity));

timeBetwinPrognoses *= 1000;
if (!int.TryParse(timeForSignalAfterNotification, out int timeBeforeSignal))
{
    Console.WriteLine($"Ошибка парсинга timeForSignalAfterNotification {ConfigurationManager.AppSettings["timeForSignalAfterNotification"]} в int");
}

ushort tradePairCount = (ushort)tradePairs.Count;
ushort tradePairIndex = 0;
List<string> publishedPairNames = [];
#endregion


#region MainLogic
Console.WriteLine("Запускаю скам...");
TimeSpan randomWaiting = new TimeSpan(0, 1, 0);
do
{
    PauseUntilWorkTime();
    TradePairsEntity currentPair = null;
    Console.WriteLine("Беру случайную торговую парую");
    currentPair = GetActualTradingPair();

    await browserWorker.MakeSkreenshot(currentPair.Value);

    string threadId = await aiWorker.GetThreadId();
    string prognoze = await aiWorker.GetPrognoze(threadId);

    await telegramWorker.SendNotificationMessage(currentPair.Name);
    Console.WriteLine($"Задержка между уведомлением и сигналом");
    randomWaiting = new TimeSpan(0, GetRandomUInt((ushort)timeBeforeSignal), 0);
    Thread.Sleep(randomWaiting);

    await telegramWorker.SendAnalyticMessage(prognoze, currentPair.Name);
    Console.WriteLine($"Задержка между сигналами");
    Thread.Sleep(timeBetwinPrognoses);

    Console.Clear();
}
while (true);
#endregion

#region LolacFunctions
ushort GetRandomUInt(ushort maxValue)
{
    Random random = new Random();
    ushort randomInt = maxValue > 0 ? (ushort)random.Next(1, maxValue) : (ushort)0;

    return randomInt;
}

int GetRandomInt(int minValue, int maxValue)
{
    Random random = new Random();
    int randomInt = maxValue > 0 ? (ushort)random.Next(minValue, maxValue) : 0;

    return randomInt;
}

void PauseUntilWorkTime()
{
    //Получаю московское время
    DateTime currentDateTime = DateTime.UtcNow.AddHours(3);
    if (currentDateTime.DayOfWeek == DayOfWeek.Sunday || currentDateTime.DayOfWeek == DayOfWeek.Saturday)
    {
        DateTime nextMonday = currentDateTime.AddDays(8 - (int)currentDateTime.DayOfWeek);
        TimeSpan periodToPause = nextMonday.Date.AddHours(9).Subtract(currentDateTime);

        Console.WriteLine($"Сегодня выходной день. Приложение ставится на паузу на {periodToPause}");
        Thread.Sleep(periodToPause);
    }
    else
    {
        if (currentDateTime.Hour < 9 && currentDateTime.Hour > 21)
        {
            DateTime nextworkDay = currentDateTime.AddDays(1).Date.AddHours(9);
            TimeSpan periodToPause = nextworkDay.Subtract(currentDateTime);

            Console.WriteLine($"Сейчас не рабочее время. Приложение ставится на паузу на {periodToPause}");
            Thread.Sleep(periodToPause);
        }
    }
}

TradePairsEntity GetActualTradingPair()
{
    TradePairsEntity currentPair = null;
    bool currentPairNotRepeatedFlag = true;
    do
    {
        tradePairIndex = GetRandomUInt(tradePairCount);
        currentPair = tradePairs[tradePairIndex];
        Console.WriteLine($"currentPair = {currentPair.Name}");
        if (publishedPairNames.Count > 10)
        {
            publishedPairNames.RemoveAt(0);
        }

        if (publishedPairNames.Where(name => name == currentPair.Name).Count() < 1)
        {
            currentPairNotRepeatedFlag = false;
            publishedPairNames.Add(currentPair.Name);
        }
        else
        {
            currentPairNotRepeatedFlag = true;
            Console.WriteLine($"Пара повторяется, беру другую!");
        }
    } while (currentPairNotRepeatedFlag);

    return currentPair;
}
#endregion