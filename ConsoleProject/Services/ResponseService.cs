using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleProject.Types;

namespace ConsoleProject.Services;

public class ResponseService
{
	private readonly Dictionary<long, (Queue<Action>, Queue<MessageHandle>)> _waitingForResponse;

	public ResponseService()
	{
		_waitingForResponse = new Dictionary<long, (Queue<Action>, Queue<MessageHandle>)>();
	}

	public bool IsResponseExpected(long id)
	{
		return _waitingForResponse.ContainsKey(id);
	}

	/// <summary>
	/// «апускает метод, который ожидает ответа.
	/// </summary>
	/// <returns> false - если ответ не ожидаетс€ или проблемы с сообщением, иначе true </returns>
	public async Task<bool> ReplyAsync(ITelegramBotClient botClient, Message message)
	{
		var user = message.From;
		if (user is null)
			return false;

		var id = user.Id;

		if (IsResponseExpected(id))
		{
			await _waitingForResponse[id].Item2.Dequeue().Invoke(botClient, message);
			if(_waitingForResponse[id].Item1.TryDequeue(out var action))
			{
				action.Invoke();
			}
			return true;
		}

		return false;
	}

    /// <summary>
    /// ƒобавить в очередь действие, которое должно ждать ответа от пользовател€.
    /// ѕосле того, как все действи€ были добавлены в очередь, нужно вызвать метод StartActions.
    /// </summary>
    /// <param name="id"> “елеграмм ID пользовател€. </param>
    /// <param name="action"> ƒействие, которое совершаетс€ перед ожиданием ответа. </param>
    /// <param name="handle"> ћетод, ожидающий получение ответа. </param>
    public void AddActionForWait(long id, Action action, MessageHandle handle)
	{
		if (!IsResponseExpected(id))
		{
			_waitingForResponse[id] = (new Queue<Action>(), new Queue<MessageHandle>());
		}

		_waitingForResponse[id].Item1.Enqueue(action);
		_waitingForResponse[id].Item2.Enqueue(handle);
    }

    /// <summary>
    /// ¬ызывает первое действие в очереди.
    /// Ётот метод нужно вызывать только после метода AddActionForWait.
    /// </summary>
    /// <param name="id"> “елеграмм ID пользовател€. </param>
    /// <returns> true - если очередь существует, иначе false </returns>
    public bool StartActions(long id)
	{
		if (!IsResponseExpected(id))
			return false;

		_waitingForResponse[id].Item1.Dequeue().Invoke();
		return true;
    }
}