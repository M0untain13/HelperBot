using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleProject.Types;

namespace ConsoleProject.Services;

public class ResponseService
{
	// TODO: ��� ��� ������������ �������, ������ ����������� ������, ���� ������� ����� ��������
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
	/// ��������� �����, ������� ������� ������.
	/// </summary>
	/// <returns> false - ���� ����� �� ��������� ��� �������� � ����������, ����� true </returns>
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
				// ��������� ������� ��������
				_waitingForResponse[id].Item1.ReleaseMutex();
			}
			return true;
		}

		return false;
	}

	/// <summary>
	/// �������� � ������� ��������, ������� ������ ����� ������ �� ������������.
	/// ����� ����, ��� ��� �������� ���� ��������� � �������, ����� ������� ����� StartActions.
	/// </summary>
	/// <param name="id"> ��������� ID ������������. </param>
	/// <param name="action"> ��������, ������� ����������� ����� ��������� ������. </param>
	/// <param name="handle"> �����, ��������� ��������� ������. </param>
	public async Task AddActionForWaitAsync(long id, Task action, MessageHandle handle)
	{
		await Task.Run(() =>
		{
			if (!IsResponseExpected(id))
			{
				_waitingForResponse[id] = (new Mutex(), new Queue<Task>(), new Queue<MessageHandle>());
			}

			// ���� ������� ������, ������ ����������� ������� �������� ��� ����������� �������� � �������
			_waitingForResponse[id].Item1.WaitOne();
			_waitingForResponse[id].Item2.Enqueue(action);
			_waitingForResponse[id].Item3.Enqueue(handle);
			_waitingForResponse[id].Item1.ReleaseMutex();
		});
	}

	/// <summary>
	/// �������� ������ �������� � �������.
	/// ���� ����� ����� �������� ������ ����� ������ AddActionForWait.
	/// </summary>
	/// <param name="id"> ��������� ID ������������. </param>
	/// <returns> true - ���� ������� ����������, ����� false </returns>
	public async Task<bool> StartActionsAsync(long id)
	{
		if (!IsResponseExpected(id))
			return false;

		// �������� ������� ��������
		_waitingForResponse[id].Item1.WaitOne();
		await _waitingForResponse[id].Item2.Dequeue().ConfigureAwait(false);
		return true;
	}
}