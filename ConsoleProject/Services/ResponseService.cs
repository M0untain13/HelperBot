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
    /// �������� � ������� ��������, ������� ������ ����� ������ �� ������������.
    /// ����� ����, ��� ��� �������� ���� ��������� � �������, ����� ������� ����� StartActions.
    /// </summary>
    /// <param name="id"> ��������� ID ������������. </param>
    /// <param name="action"> ��������, ������� ����������� ����� ��������� ������. </param>
    /// <param name="handle"> �����, ��������� ��������� ������. </param>
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
    /// �������� ������ �������� � �������.
    /// ���� ����� ����� �������� ������ ����� ������ AddActionForWait.
    /// </summary>
    /// <param name="id"> ��������� ID ������������. </param>
    /// <returns> true - ���� ������� ����������, ����� false </returns>
    public bool StartActions(long id)
	{
		if (!IsResponseExpected(id))
			return false;

		_waitingForResponse[id].Item1.Dequeue().Invoke();
		return true;
    }
}