namespace IslamicCalendar.Api;

public class TelegramBotOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Токен бота от @BotFather. Если пусто — бот отключён, API работает как обычно.</summary>
    public string BotToken { get; set; } = string.Empty;

    /// <summary>Публичный HTTPS-адрес мини-приложения (тот же, что указан у @BotFather).</summary>
    public string MiniAppUrl { get; set; } = string.Empty;
}
