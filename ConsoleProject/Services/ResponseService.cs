using ConsoleProject.Types;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public enum SessionState
{
	Wait,
	Open,
	Close
}

public class SessionProxy
{
	private Session _session;

	public SessionState State => _session.State;

	public SessionProxy(Session session)
	{
		_session = session;
	}
	
	public bool Add(Task task, MessageHandle handle) => _session.Add(task, handle);
	public void Start() => _session.Start();
	public void Close() => _session.Close();
}

public class Session
{
	private List<Task> _tasks;
	private List<MessageHandle> _handles;
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

	private bool InvokeTask()
	{
		if (_tasks.Count == 0 && _state == SessionState.Open)
			return false;

		var task = _tasks[0];
		_tasks.Remove(task);
		task.RunSynchronously();

		return true;
	}

	public bool InvokeHandle(ITelegramBotClient botClient, Message message)
	{
		if (_handles.Count == 0 && _state == SessionState.Open)
			return false;

		var handle = _handles[0];
		_handles.Remove(handle);
		handle(botClient, message);

		if (_tasks.Count > 0)
			InvokeTask();

		if (_handles.Count == 0)
			Wait();

		return true;
	}

	public void Start()
	{
		if(_state != SessionState.Close)
		{
			_state = SessionState.Open;
			InvokeTask();
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

public class ResponseService
{
	private Dictionary<long, List<Session>> _sessions;

	public ResponseService()
	{
		_sessions = new Dictionary<long, List<Session>>();
	}

	public SessionProxy CreateSession(long id)
	{
		if (!_sessions.ContainsKey(id))
			_sessions[id] = new List<Session>();

		var session = new Session();
		_sessions[id].Add(session);

		return new SessionProxy(session);
	}

	public void Reply(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		if (user is null)
			return;

		var id = user.Id;

		var session = GetSession(id);

		if (session is null)
			return;

		// TODO: возможно тут стоит сделать выход из цикла, если слишком долгое ожидание
		while(session.State == SessionState.Wait)
			Thread.Sleep(1000);

		session.InvokeHandle(botClient, message);
	}

	private Session? GetSession(long id)
	{
		if (!_sessions.ContainsKey(id))
			return null;

		// Я знаю, это ужасная конструкция.
		// Но, как по мне, код читаем, а значит все норм!
		while (true)
		{
			if (_sessions[id].Count == 0)
				return null;

			var session = _sessions[id][0];
			switch (session.State)
			{
				case SessionState.Close:
					_sessions[id].Remove(session);
					break;
				default:
					return session;
			}
		}
	}

	
	public SessionProxy? GetSessionProxy(long id)
	{
		var session = GetSession(id);

		if (session is null)
			return null;

		return new SessionProxy(session);
	}
}
