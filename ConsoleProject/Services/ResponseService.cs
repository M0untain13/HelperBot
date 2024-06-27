using ConsoleProject.Types.Classes;
using ConsoleProject.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

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

	public bool IsResponseExpected(long id)
	{
		return _sessions.ContainsKey(id) && _sessions[id].Count > 0 && _sessions[id].Any(s => s.State == SessionState.Open);
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
				case SessionState.Open:
					return session;
				case SessionState.Wait:
					return null;
				case SessionState.Close:
					_sessions[id].Remove(session);
					break;
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
