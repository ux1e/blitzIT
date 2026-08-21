using ServerBrowser.Functions;
using ServerBrowser.Pages;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Windows;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace ServerBrowser
{
    public class UserState
    {
        public string CurrentState { get; set; } = "default"; // Начальное состояние
        // Другие поля для хранения данных пользователя могут быть добавлены здесь
    }

    public class TelegramBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly OurIPList _ourIPList;
        private ConcurrentDictionary<long, UserState> userStates = new ConcurrentDictionary<long, UserState>();

        public TelegramBot(string token, OurIPList ourIPList)
        {
            _botClient = new TelegramBotClient(token);
            _ourIPList = ourIPList;
            StartReceiving();
        }

        private async void StartReceiving()
        {
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, System.Threading.CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                await HandleMessageAsync(botClient, update.Message);
            }
            else if (update.Type == UpdateType.CallbackQuery)
            {
                await HandleCallbackQueryAsync(botClient, update.CallbackQuery);
            }
        }

        private async Task HandleMessageAsync(ITelegramBotClient botClient, Message message)
        {
            if (message.Text == "/start")
            {
                await SendMainKeyboard(botClient, message.Chat.Id);
                return;
            }

            if (userStates.TryGetValue(message.Chat.Id, out var userState))
            {
                await ProcessUserStateAsync(botClient, message, userState);
            }
            else
            {
                await SendMainKeyboard(botClient, message.Chat.Id);
            }
        }

        private async Task ProcessUserStateAsync(ITelegramBotClient botClient, Message message, UserState userState)
        {
            var ip = message.Text.Trim();
            switch (userState.CurrentState)
            {
                case "awaiting_ip":
                    await HandleAddIpAsync(botClient, message.Chat.Id, ip);
                    userState.CurrentState = "default"; // Сброс состояния после обработки
                    break;
                case "awaiting_delete_ip":
                    await HandleDeleteIpAsync(botClient, message.Chat.Id, ip);
                    userState.CurrentState = "default"; // Сброс состояния после обработки
                    break;
                case "awaiting_shutdown_ip":
                    await HandleShutdownIpAsync(botClient, message.Chat.Id, ip);
                    userState.CurrentState = "default"; // Сброс состояния после обработки
                    break;
            }
            // Обновляем состояние пользователя в словаре
            userStates[message.Chat.Id] = userState;
        }

        private async Task HandleAddIpAsync(ITelegramBotClient botClient, long chatId, string ip)
        {
            if (_ourIPList.AddIpAddress(ip))
            {
                await botClient.SendMessage(chatId, $"IP-адрес {ip} добавлен.");
                Application.Current.Dispatcher.Invoke(() => _ourIPList.UpdateListBox());
            }
            else
            {
                await botClient.SendMessage(chatId, $"Некорректный IP-адрес или он уже существует. Пожалуйста, введите корректный IP-адрес:");
            }
        }

        private async Task HandleDeleteIpAsync(ITelegramBotClient botClient, long chatId, string ip)
        {
            if (_ourIPList.RemoveIpAddress(ip))
            {
                await botClient.SendMessage(chatId, $"IP-адрес {ip} удален.");
            }
            else
            {
                await botClient.SendMessage(chatId, $"IP-адрес {ip} не найден.");
            }
        }

        private async Task HandleShutdownIpAsync(ITelegramBotClient botClient, long chatId, string ip)
        {
            Helper.ShutDownBy(ip, ServerBrowser.AppConfig.ShutdownDelaySeconds);
            await botClient.SendMessage(chatId, $"Команда на выключение отправлена для IP-адреса {ip}.");
        }

        private async Task HandleCallbackQueryAsync(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var userState = userStates.GetOrAdd(callbackQuery.Message.Chat.Id, new UserState());
            switch (callbackQuery.Data)
            {
                case "add_ip":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Пожалуйста, введите IP-адрес:");
                    userState.CurrentState = "awaiting_ip";
                    break;
                case "get_all_ips":
                    var allIps = _ourIPList.GetAllIpAddresses();
                    var responseMessage = allIps.Count > 0 ? string.Join("\n", allIps) : "Нет доступных IP-адресов.";
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, responseMessage);
                    return;
                case "delete_ip":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Пожалуйста, введите IP-адрес для удаления:");
                    userState.CurrentState = "awaiting_delete_ip";
                    break;
                case "shutdown_ip":
                    await botClient.SendMessage(callbackQuery.Message.Chat.Id, "Пожалуйста, введите IP-адрес для выключения:");
                    userState.CurrentState = "awaiting_shutdown_ip";
                    break;
            }

            // Обновляем состояние пользователя в словаре
            userStates[callbackQuery.Message.Chat.Id] = userState;
        }

        private async Task SendMainKeyboard(ITelegramBotClient botClient, long chatId)
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Добавить IP", "add_ip"),
                    InlineKeyboardButton.WithCallbackData("Получить все IP", "get_all_ips")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Удалить IP", "delete_ip"),
                    InlineKeyboardButton.WithCallbackData("Выключить по IP", "shutdown_ip")
                }
            });

            // Отправляем сообщение с клавиатурой
            await botClient.SendMessage(chatId, "Выберите действие:", replyMarkup: keyboard);
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, System.Threading.CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}