using System.Configuration;

namespace ServerBrowser
{
    /// <summary>
    /// Настройки приложения из App.config.
    /// Секреты и адреса вынесены сюда, чтобы не хранить их в коде.
    /// </summary>
    internal static class AppConfig
    {
        /// <summary>Токен Telegram-бота. Пустая строка — бот не запускается.</summary>
        public static string BotToken => Get("BotToken", string.Empty);

        /// <summary>Подсеть для сканирования без последнего октета, например "192.168.0.".</summary>
        public static string ScanSubnet => Get("ScanSubnet", "192.168.0.");

        /// <summary>Задержка перед выключением удалённой машины, секунды.</summary>
        public static int ShutdownDelaySeconds => GetInt("ShutdownDelaySeconds", 100);

        private static string Get(string key, string fallback)
        {
            string value = ConfigurationManager.AppSettings[key];
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int GetInt(string key, int fallback)
        {
            return int.TryParse(Get(key, null), out int value) ? value : fallback;
        }
    }
}
