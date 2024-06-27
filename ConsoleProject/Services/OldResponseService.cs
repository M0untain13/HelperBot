using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleProject.Types.Delegates;

namespace ConsoleProject.Services;

public class OldResponseService
{
	private readonly Dictionary<long, (Mutex, Queue<Task>, Queue<MessageHandle>)> _waitingForResponse;

	public OldResponseService()
	{
		_waitingForResponse = new Dictionary<long, (Mutex, Queue<Task>, Queue<MessageHandle>)>();
	}

	public bool IsResponseExpected(long id)
	{
		return _waitingForResponse.ContainsKey(id) && _waitingForResponse[id].Item3.Count > 0;
	}

	/// <summary>
	/// Обработать ответ.
	/// </summary>
	/// <returns> true - обработано, иначе false </returns>
	public async Task<bool> ReplyAsync(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		if (user is null)
			return false;

		var id = user.Id;

		if (IsResponseExpected(id))
		{
			await _waitingForResponse[id].Item3.Dequeue().Invoke(botClient, message);
			if(_waitingForResponse[id].Item2.TryDequeue(out var action))
			{
				action.RunSynchronously();
			}
			else
			{
				_waitingForResponse[id].Item1.ReleaseMutex();
			}
			return true;
		}

		return false;
	}

	/// <summary>
	/// Добавить действие в очередь.
	/// После этого метода нужно вызвать StartActions.
	/// </summary>
	/// <param name="id"> Telegram ID. </param>
	/// <param name="action"> Действие перед ожиданием ответа. </param>
	/// <param name="handle"> Обработчик ответа. </param>
	public async Task AddActionForWaitAsync(long id, Task action, MessageHandle handle)
	{
		await Task.Run(() =>
		{
			if (!IsResponseExpected(id))
			{
				_waitingForResponse[id] = (new Mutex(), new Queue<Task>(), new Queue<MessageHandle>());
			}

			_waitingForResponse[id].Item1.WaitOne();
			_waitingForResponse[id].Item2.Enqueue(action);
			_waitingForResponse[id].Item3.Enqueue(handle);
			_waitingForResponse[id].Item1.ReleaseMutex();
		});
	}

	/// <summary>
	/// Начать цепочку действий.
	/// Использовать только после AddActionForWait.
	/// </summary>
	/// <param name="id"> Telegram ID. </param>
	/// <returns> true - цепочка запущена, иначе false </returns>
	public async Task<bool> StartActionsAsync(long id)
	{
		if (!IsResponseExpected(id))
			return false;

		_waitingForResponse[id].Item1.WaitOne();
		_waitingForResponse[id].Item2.Dequeue().RunSynchronously();
		return true;
	}
}