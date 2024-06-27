using ConsoleProject.Types.Delegates;
using Telegram.Bot.Types;
using Telegram.Bot;
using ConsoleProject.Types.Enums;

namespace ConsoleProject.Types.Classes;

public class Session
{
    private readonly List<Task> _tasks;
    private readonly List<MessageHandle> _handles;
    private SessionState _state;
    public SessionState State => _state;

    public Session()
    {
        _tasks = new List<Task>();
        _handles = new List<MessageHandle>();
        _state = SessionState.Wait;
    }

    public bool Add(Task task, MessageHandle handle)
    {
        if (_state != SessionState.Close)
        {
            _tasks.Add(task);
            _handles.Add(handle);
            return true;
        }

        return false;
    }

    private async Task<bool> InvokeTaskAsync()
    {
        if (_tasks.Count == 0 && _state == SessionState.Open)
            return false;

        var task = _tasks[0];
        _tasks.Remove(task);
        task.RunSynchronously(); // TODO: переделать

        return true;
    }

    public async Task<bool> InvokeHandleAsync(ITelegramBotClient botClient, Message message)
    {
        if (_handles.Count == 0 && _state == SessionState.Open)
            return false;

        var handle = _handles[0];
        _handles.Remove(handle);
        await handle(botClient, message);

        if (_tasks.Count > 0)
            await InvokeTaskAsync();

        if (_handles.Count == 0)
            Wait();

        return true;
    }

    public async Task StartAsync()
    {
        if (_state != SessionState.Close)
        {
            _state = SessionState.Open;
            await InvokeTaskAsync();
        }
    }

    public void Wait()
    {
        if (_state != SessionState.Close)
            _state = SessionState.Wait;
    }

    public void Close()
    {
        _state = SessionState.Close;
    }
}
