using ConsoleProject.Types.Classes;
using ConsoleProject.Types.Enums;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConsoleProject.Services;

public class ResponseService
{
	private Dictionary<long, List<Session>> _sessions;

	public ResponseService(int sessionClearDelay)
	{
		_sessions = new Dictionary<long, List<Session>>();

		// Механизм отчистки сессий
		Task.Run(async () =>
		{
			while (true)
			{
				// Отчистку делаем с определенным периодом
				await Task.Delay(sessionClearDelay);
				await ClearSessionsAsync(sessionClearDelay);
			}
		});
	}

	private async Task ClearSessionsAsync(int sessionClearDelay)
	{
		await Task.Run(() =>
		{
			foreach (var id in _sessions.Keys)
			{
				var sessions = _sessions[id];

				if (sessions is null)
					continue;

				foreach (var session in sessions)
				{
					// Закрываем те сессии, которые слишком долго открыты
					if (DateTime.Now - session.CreationDate > TimeSpan.FromMicroseconds(sessionClearDelay))
					{
						session.Close();
					}

					// Закрытые сессии удаляем
					if (session.State == SessionState.Close)
					{
						session.Dispose();
						sessions.Remove(session);
					}
				}

				// Если у пользователя сессий не осталось, тогда удаляем его из словаря
				if (sessions.Count == 0)
				{
					_sessions.Remove(id);
				}
			}
		});
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

	public async Task ReplyAsync(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		if (user is null)
			return;

		var id = user.Id;

		var session = await GetSessionAsync(id);

		if (session is null)
			return;

		await session.InvokeHandleAsync(botClient, message);
	}

	private async Task<Session?> GetSessionAsync(long id)
	{
		if (!_sessions.ContainsKey(id))
			return null;

		Session? session = null;

		await Task.Run(() =>
		{
			var isStart = true;
			while (isStart)
			{
				if (_sessions[id].Count == 0)
				{
					session = null;
					isStart = false;
				}
				else
				{
					session = _sessions[id][0];
					switch (session.State)
					{
						case SessionState.Open:
							isStart = false;
							break;
						case SessionState.Wait:
							session = null;
							isStart = false;
							break;
						case SessionState.Close:
							_sessions[id].Remove(session);
							session.Dispose();
							session = null;
							break;
					}
				}
			}
		});

		return session;
	}

	
	public async Task<SessionProxy?> GetSessionProxyAsync(long id)
	{
		var session = await GetSessionAsync(id);

		if (session is null)
			return null;

		return new SessionProxy(session);
	}
}
