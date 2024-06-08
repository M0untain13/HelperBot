using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public delegate Task UserMessageHandle(ITelegramBotClient botClient, Message message);

public class ResponseService
{
    private readonly Dictionary<long, UserMessageHandle> _waitingForResponse;

    public ResponseService()
    {
        _waitingForResponse = new Dictionary<long, UserMessageHandle>();
    }

    public bool IsResponseExpected(long id)
    {
        return _waitingForResponse.ContainsKey(id);
    }

    public async Task ReplyAsync(ITelegramBotClient botClient, Message message)
    {
        var user = message.From;
        if (user is null)
            return;

        var id = user.Id;

        if (IsResponseExpected(id))
        {
            var handle = _waitingForResponse[id];
            _waitingForResponse.Remove(id);
            await handle.Invoke(botClient, message);
        }
            
    }

    public bool WaitResponse(long id, UserMessageHandle handle)
    {
        if (IsResponseExpected(id))
            return false;
        
        _waitingForResponse[id] = handle;
        return true;
    }
}