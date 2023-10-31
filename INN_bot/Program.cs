using CRMPTelegramBotINN;
using Dadata;
using System.Configuration;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

string tgToken = ReadSetting("tgToken");
string fnsToken = ReadSetting("fnsToken");
UserManager userManager = new();
var botClient = new TelegramBotClient(tgToken);

using CancellationTokenSource cts = new();
ReceiverOptions receiverOptions = new()
{
    AllowedUpdates = Array.Empty<UpdateType>(),
    ThrowPendingUpdates = true
};
try
{
    botClient.StartReceiving(
        updateHandler: HandleUpdateAsync,
        pollingErrorHandler: HandlePollingErrorAsync,
        receiverOptions: receiverOptions,
        cancellationToken: cts.Token
    );
    var me = await botClient.GetMeAsync();
    Console.WriteLine($"Start listening for @{me.Username}");
    Console.ReadLine();
}
catch (ApiRequestException exception)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };
    Console.WriteLine(ErrorMessage);
    return;
}
cts.Cancel();


async Task HandleInnRequestAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var api = new SuggestClientAsync(fnsToken);
    foreach (string line in update.Message.Text.Split('\n'))
    {
        if (line.All(Char.IsDigit) && line.Length == 10)
        {
            var response = await api.FindParty(line);
            if (response.suggestions.Count == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: Constants.EmptyMessage(line),
                    cancellationToken: cancellationToken);
            }
            else
            {
                var party = response.suggestions[0].data;
                if (!userManager.GetById(update.Message.From.Id).RequestsContains(line))
                    userManager.GetById(update.Message.From.Id).AddRequest(line);
                StringBuilder sb = new();
                sb.AppendFormat("ИНН: {0}\nНазвание: {1}\nАдрес:\n{2}", party.inn, party.name.full, party.address.value);
                await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: sb.ToString(),
                    cancellationToken: cancellationToken);
            }

        }
        else
        {
            await botClient.SendTextMessageAsync(
                    chatId: update.Message.Chat.Id,
                    text: Constants.WrongMessage(line),
                    cancellationToken: cancellationToken);
        }
    }
}

async Task HandleFullRequestAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    var api = new SuggestClientAsync(fnsToken);
    string line = update.Message.Text.Split('\n')[0];
    if (line.All(Char.IsDigit) && line.Length == 10)
    {
        var response = await api.FindParty(line);
        if (response.suggestions.Count == 0)
        {
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: Constants.EmptyMessage(line),
                cancellationToken: cancellationToken);
        }
        else
        {
            var party = response.suggestions[0].data;
            if (!userManager.GetById(update.Message.From.Id).RequestsContains(line))
                userManager.GetById(update.Message.From.Id).AddRequest(line);
            StringBuilder sb = new();
            sb.AppendFormat("ИНН: {0}\nНазвание: {1}\nАдрес:\n{2}\n", party.inn, party.name.full, party.address.value);
            sb.AppendFormat("КПП: {0}\nОГРН: {1}\n{2}: {3}\nКоличество филиалов: {4}", party.kpp, party.ogrn, party.management.post, party.management.name, party.branch_count);
            if (party.founders is not null)
            {
                sb.Append("Учредители:\n");
                foreach (var founder in party.founders)
                {
                    sb.AppendFormat("{0} : {1} {2}\n", founder.name, founder.share.value.ToString(), founder.share.type.ToString());
                }
            }
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: sb.ToString(),
                cancellationToken: cancellationToken);
        }
    }
    else
    {
        await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: Constants.WrongMessage(line),
                cancellationToken: cancellationToken);
    }
}


async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    if (update.Message is not { } message)
        return;
    if (message.Text is not { } messageText)
        return;
    var chatId = message.Chat.Id;
    if (message.From is not { } messageFrom)
        return;
    if (userManager.GetById(messageFrom.Id) == null) userManager.AddUser(new TelegramUser(messageFrom));

    Console.WriteLine($"Received a '{messageText}' message in chat {chatId}. User state: {userManager.GetById(messageFrom.Id).State}");
    switch (messageText)
    {
        case "/start":
            await HandleStartMessageAsync(botClient, update, cancellationToken);
            break;
        case "/inn":
            await HandleInnMessageAsync(botClient, update, cancellationToken);
            break;
        case "/full":
            await HandleFullMessageAsync(botClient, update, cancellationToken);
            break;
        case "/help":
            await HandleHelpMessageAsync(botClient, update, cancellationToken);
            break;
        case "/hello":
            await HandleHelloMessageAsync(botClient, update, cancellationToken);
            break;
        case "/egrul":
            await HandleEgrulMessageAsync(botClient, update, cancellationToken);
            break;
        case "Назад":
            if (userManager.GetById(update.Message.From.Id).State != UserState.Base)
                await HandleBackMessageAsync(botClient, update, cancellationToken);
            break;
        default:
            await HandleOtherMessageAsync(botClient, update, cancellationToken);
            break;
    }
}

async Task HandleStartMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: Constants.StartMessage,
        cancellationToken: cancellationToken);

    if (userManager.Users.Contains(update.Message.From))
    {
        return;
    }
    userManager.AddUser(new TelegramUser(update.Message.From));
}

async Task HandleHelloMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: Constants.HelloMessage,
        cancellationToken: cancellationToken);
}

async Task HandleHelpMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: Constants.HelpMessage,
        cancellationToken: cancellationToken);
}

async Task HandleInnMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    userManager.GetById(update.Message.From.Id).State = UserState.Search;
    ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
    {
                new KeyboardButton[] { "/back" },
            })
    {
        ResizeKeyboard = true,
        InputFieldPlaceholder = "Назад"
    };

    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: Constants.InnMessage,
        replyMarkup: replyKeyboardMarkup,
        cancellationToken: cancellationToken);

}

static KeyboardButton[][] GetKeyboard(string[] stringArray)
{
    var keyboard = new KeyboardButton[1][];
    var keyboardButtons = new KeyboardButton[stringArray.Length + 1];
    for (var i = 0; i < stringArray.Length; i++)
    {
        keyboardButtons[i] = new KeyboardButton(stringArray[i]);
    }
    keyboardButtons[stringArray.Length] = new KeyboardButton("Назад");
    keyboard[0] = keyboardButtons;
    return keyboard;
}


async Task HandleFullMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    userManager.GetById(update.Message.From.Id).State = UserState.ExtendedSearch;
    ReplyKeyboardMarkup replyKeyboardMarkup = new(GetKeyboard(userManager.GetById(update.Message.From.Id).Requests.ToArray()))
    {
        ResizeKeyboard = true,
    };


    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: Constants.FullMessage,
        replyMarkup: replyKeyboardMarkup,
        cancellationToken: cancellationToken);

}

async Task HandleBackMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: Constants.BackMessage,
        replyMarkup: new ReplyKeyboardRemove(),
        cancellationToken: cancellationToken);

    userManager.GetById(update.Message.From.Id).State = UserState.Base;
}

async Task HandleOtherMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    switch (userManager.GetById(update.Message.From.Id).State)
    {
        case UserState.Search:
            await HandleInnRequestAsync(botClient, update, cancellationToken);
            break;

        case UserState.ExtendedSearch:
            await HandleFullRequestAsync(botClient, update, cancellationToken);
            break;

        default:
            await botClient.SendTextMessageAsync(
                chatId: update.Message.Chat.Id,
                text: Constants.UnknownMessage,
                cancellationToken: cancellationToken);
            break;

    }
}

Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
{
    var ErrorMessage = exception switch
    {
        ApiRequestException apiRequestException
            => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
        _ => exception.ToString()
    };

    Console.WriteLine(ErrorMessage);
    return Task.CompletedTask;
}

async Task HandleEgrulMessageAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    userManager.GetById(update.Message.From.Id).State = UserState.Egrul;
    ReplyKeyboardMarkup replyKeyboardMarkup = new(new[]
    {
                new KeyboardButton[] {"Назад" }
    })
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true,
        InputFieldPlaceholder = "Назад"
    };
    await botClient.SendTextMessageAsync(
        chatId: update.Message.Chat.Id,
        text: "Данный функционал пока еще не был реализован.",
        replyMarkup: replyKeyboardMarkup,
        cancellationToken: cancellationToken);
}

string ReadSetting(string key)
{
    try
    {
        var appSettings = ConfigurationManager.AppSettings;
        return appSettings[key] ?? "Not Found";
    }
    catch (ConfigurationErrorsException)
    {
        Console.WriteLine("Error reading app settings");
        return "Not Found";
    }
}