using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace IslamicCalendar.Api.Services;

/// <summary>
/// Слушает сообщения телеграм-бота через long polling и на команду /start
/// присылает кнопку, открывающую Mini App (Telegram.WebApp).
///
/// Long polling выбран вместо webhook специально: он не требует публичного
/// HTTPS-адреса для самого бота и работает даже при локальном запуске —
/// удобно для разработки. Сам Mini App (Telegram:MiniAppUrl), тем не менее,
/// обязан быть на HTTPS — иначе Telegram откажется его открывать.
///
/// ВАЖНО: пакет Telegram.Bot довольно часто меняет сигнатуры методов между
/// мажорными версиями (SendMessage/SendTextMessageAsync, GetMe/GetMeAsync
/// и т.д.). Код ниже написан под актуальные на момент написания версии
/// (~19+, без суффикса Async у большинства методов). Если после
/// dotnet restore у вас подтянется другая версия и сборка не пройдёт —
/// смотрите на сообщения компилятора, обычно достаточно добавить/убрать
/// суффикс Async у 2-3 вызовов ниже.
/// </summary>
public class TelegramBotHostedService : BackgroundService
{
    private readonly ITelegramBotClient? _bot;
    private readonly TelegramBotOptions _options;
    private readonly ILogger<TelegramBotHostedService> _logger;

    public TelegramBotHostedService(
        IOptions<TelegramBotOptions> options,
        ILogger<TelegramBotHostedService> logger)
    {
        _options = options.Value;
        _logger = logger;

        if (!string.IsNullOrWhiteSpace(_options.BotToken))
        {
            _bot = new TelegramBotClient(_options.BotToken);
        }
        else
        {
            _logger.LogWarning(
                "Telegram:BotToken не задан в конфигурации — бот отключён. " +
                "API и мини-приложение при этом продолжают работать как обычно.");
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_bot is null) return;

        var me = await _bot.GetMe(stoppingToken);
        _logger.LogInformation("Телеграм-бот @{Username} запущен (long polling)", me.Username);

        _bot.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [UpdateType.Message] },
            cancellationToken: stoppingToken);

        // StartReceiving работает в фоне сам по себе — просто ждём отмены токена
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // штатное завершение при остановке приложения
        }
    }

    private async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        var message = update.Message;
        if (message?.Text is null) return;

        if (!message.Text.StartsWith("/start", StringComparison.OrdinalIgnoreCase)) return;

        if (string.IsNullOrWhiteSpace(_options.MiniAppUrl))
        {
            await bot.SendMessage(
                message.Chat.Id,
                "Ассаляму алейкум! Адрес мини-приложения ещё не настроен (Telegram:MiniAppUrl в appsettings).",
                cancellationToken: ct);
            return;
        }

        var keyboard = new InlineKeyboardMarkup(
            InlineKeyboardButton.WithWebApp("📅 Открыть календарь", _options.MiniAppUrl));

        await bot.SendMessage(
            chatId: message.Chat.Id,
            text: "Ассаляму алейкум ва рахмату-Ллахи ва баракятух!\n\n" +
                  "Нажмите на кнопку ниже, чтобы открыть исламский календарь: даты по Хиджре, " +
                  "время намазов, фаза Луны и напоминания по пятницам.",
            replyMarkup: keyboard,
            cancellationToken: ct);
    }

    private Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken ct)
    {
        _logger.LogError(exception, "Ошибка в телеграм-боте");
        return Task.CompletedTask;
    }
}
