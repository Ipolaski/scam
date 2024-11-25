namespace ScamIvan.BrowserOperations.Entities
{
    /// <summary>
    /// Ответ ИИ с аналитикой
    /// </summary>
    internal class PrognozEntity
    {
        public string status { get; set; }
        /// <summary>
        /// Содержит основную информацию, то есть прогноз на тех анализе по скриншоту
        /// </summary>
        public string response { get; set; }
    }
}