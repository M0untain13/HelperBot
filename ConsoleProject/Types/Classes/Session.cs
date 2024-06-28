using ConsoleProject.Types.Delegates;
using Telegram.Bot.Types;
using Telegram.Bot;
using ConsoleProject.Types.Enums;

namespace ConsoleProject.Types.Classes;

public class Session : IDisposable
{
	private readonly List<Task> _tasks;
	private readonly List<MessageHandle> _handles;
	
	public SessionState State { get; private set; }
	public DateTime CreationDate { get; }

	public Session()
	{
		_tasks = new List<Task>();
		_handles = new List<MessageHandle>();
		State = SessionState.Wait;
		CreationDate = DateTime.Now;
	}

	public bool Add(Task task, MessageHandle handle)
	{
		if (State != SessionState.Close)
		{
			_tasks.Add(task);
			_handles.Add(handle);
			return true;
		}

		return false;
	}

	private async Task<bool> InvokeTaskAsync()
	{
		if (_tasks.Count == 0 && State == SessionState.Open)
			return false;

		var task = _tasks[0];
		_tasks.Remove(task);

        // К сожалению, иначе никак это не запустить асинхронно
        await Task.Run(() =>
		{
			task.RunSynchronously();
		});

		return true;
	}

	public async Task<bool> InvokeHandleAsync(ITelegramBotClient botClient, Message message)
	{
		if (_handles.Count == 0 && State == SessionState.Open)
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
		if (State != SessionState.Close)
		{
			State = SessionState.Open;
			await InvokeTaskAsync();
		}
	}

	public void Wait()
	{
		if (State != SessionState.Close)
			State = SessionState.Wait;
	}

	public void Close()
	{
		State = SessionState.Close;
	}

	//==================================================

	private bool disposedValue;

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				foreach(var task in _tasks)
				{
					task.Dispose();
				}
				_tasks.Clear();
				_handles.Clear();
			}

			disposedValue = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
