using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BingXBotApi.Model.ConfigBot;
using Microsoft.Extensions.Options;
using TelegramBotClientXakzone;
using TelegramBotClientXakzone.Interfaces;

public class BotService
{
    private readonly TelBot _TelBot;

    // private readonly Bot _botSettings;
    private readonly ITelegramBotSimple telegramBotSimple;

    private DateTime _lastSendTime;
    private readonly TimeSpan _minTimeBetweenSends;

    //private readonly HttpClient _httpClient;

    public BotService(IOptions<TelBot> TelBot, ITelegramBotSimple telegramBotSimple
       )
    {
        _TelBot = TelBot.Value;
       // _httpClient = httpClient;
        this.telegramBotSimple = telegramBotSimple;
        _lastSendTime = DateTime.MinValue;
        _minTimeBetweenSends = TimeSpan.FromMinutes(_TelBot.IntervalMinutSend);
    }
    private bool CanSendMessage()
    {
        var currentTime = DateTime.Now;
        if (currentTime - _lastSendTime >= _minTimeBetweenSends)
        {
            _lastSendTime = currentTime;
            return true;
        }
        return false;
    }
    public async Task SendNotificationAsync(string message)
    {
        if (!_TelBot.IsSendTelegram || !CanSendMessage())
        {
            return;
        }


        //var payload = new
        //{
        //    chat_id = _botSettings.ChatId,
        //    text = message
        //};

        var response = await telegramBotSimple.sendMessageSimple(_TelBot.TelegramChatID, message);
        //response.EnsureSuccessStatusCode();
    }
    public async Task SendNotificationAsync(StringBuilder message)
    {
        if (!_TelBot.IsSendTelegram || !CanSendMessage())
        {
            return;
        }


        //var payload = new
        //{
        //    chat_id = _botSettings.ChatId,
        //    text = message
        //};

        var response = await telegramBotSimple.sendMessageSimple(_TelBot.TelegramChatID, message);
        //response.EnsureSuccessStatusCode();
    }
}