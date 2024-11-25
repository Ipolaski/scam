namespace ScamIvan.BrowserOperations.Entities
{
    /// <summary>
    /// Для работы с торговыми парами из конфига
    /// </summary>
    internal class TradePairsEntity
    {
        public TradePairsEntity(string name, string value)
        {
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Ключ-поле, подходит для вывода пользователю
        /// </summary>
        internal string Name { get; set; }

        /// <summary>
        /// Значение, необходимо для правильного составления ссылки перехода на TradingView
        /// </summary>
        internal string Value { get; set; }
    }
}