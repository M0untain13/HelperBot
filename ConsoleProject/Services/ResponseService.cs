using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleProject.Types;

namespace ConsoleProject.Services;

public class ResponseService
{
	// TODO: Там где используется мьютекс, ввести возможность отмены, если слишком долго заблочен
	private readonly Dictionary<long, (Mutex, Queue<Task>, Queue<MessageHandle>)> _waitingForResponse;

	public ResponseService()
	{
		_waitingForResponse = new Dictionary<long, (Mutex, Queue<Task>, Queue<MessageHandle>)>();
	}

	public bool IsResponseExpected(long id)
	{
		return _waitingForResponse.ContainsKey(id);
	}

	/// <summary>
	/// Запускает метод, который ожидает ответа.
	/// </summary>
	/// <returns> false - если ответ не ожидается или проблемы с сообщением, иначе true </returns>
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
				await action.ConfigureAwait(false);
			}
			else
			{
				// Завершаем цепочку действий
				_waitingForResponse[id].Item1.ReleaseMutex();
			}
			return true;
		}

		return false;
	}

	/// <summary>
	/// Добавить в очередь действие, которое должно ждать ответа от пользователя.
	/// После того, как все действия были добавлены в очередь, нужно вызвать метод StartActions.
	/// </summary>
	/// <param name="id"> Телеграмм ID пользователя. </param>
	/// <param name="action"> Действие, которое совершается перед ожиданием ответа. </param>
	/// <param name="handle"> Метод, ожидающий получение ответа. </param>
	public async Task AddActionForWaitAsync(long id, Task action, MessageHandle handle)
	{
		await Task.Run(() =>
		{
			if (!IsResponseExpected(id))
			{
				_waitingForResponse[id] = (new Mutex(), new Queue<Task>(), new Queue<MessageHandle>());
			}

			// Если мьютекс закрыт, значит выполняется цепочка действий или добавляется действие в цепочку
			_waitingForResponse[id].Item1.WaitOne();
			_waitingForResponse[id].Item2.Enqueue(action);
			_waitingForResponse[id].Item3.Enqueue(handle);
			_waitingForResponse[id].Item1.ReleaseMutex();
		});
	}

	/// <summary>
	/// Вызывает первое действие в очереди.
	/// Этот метод нужно вызывать только после метода AddActionForWait.
	/// </summary>
	/// <param name="id"> Телеграмм ID пользователя. </param>
	/// <returns> true - если очередь существует, иначе false </returns>
	public async Task<bool> StartActionsAsync(long id)
	{
		if (!IsResponseExpected(id))
			return false;

		// Стартуем цепочку действий
		_waitingForResponse[id].Item1.WaitOne();
		await _waitingForResponse[id].Item2.Dequeue().ConfigureAwait(false);
		return true;
	}
}