using Telegram.Bot;
using Telegram.Bot.Types;
using ConsoleProject.Types;

namespace ConsoleProject.Services;

public class ResponseService
{
	// TODO: Òàì ãäå èñïîëüçóåòñÿ ìüþòåêñ, ââåñòè âîçìîæíîñòü îòìåíû, åñëè ñëèøêîì äîëãî çàáëî÷åí
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
	/// Çàïóñêàåò ìåòîä, êîòîðûé îæèäàåò îòâåòà.
	/// </summary>
	/// <returns> false - åñëè îòâåò íå îæèäàåòñÿ èëè ïðîáëåìû ñ ñîîáùåíèåì, èíà÷å true </returns>
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
				// Çàâåðøàåì öåïî÷êó äåéñòâèé
				_waitingForResponse[id].Item1.ReleaseMutex();
			}
			return true;
		}

		return false;
	}

	/// <summary>
	/// Äîáàâèòü â î÷åðåäü äåéñòâèå, êîòîðîå äîëæíî æäàòü îòâåòà îò ïîëüçîâàòåëÿ.
	/// Ïîñëå òîãî, êàê âñå äåéñòâèÿ áûëè äîáàâëåíû â î÷åðåäü, íóæíî âûçâàòü ìåòîä StartActions.
	/// </summary>
	/// <param name="id"> Òåëåãðàìì ID ïîëüçîâàòåëÿ. </param>
	/// <param name="action"> Äåéñòâèå, êîòîðîå ñîâåðøàåòñÿ ïåðåä îæèäàíèåì îòâåòà. </param>
	/// <param name="handle"> Ìåòîä, îæèäàþùèé ïîëó÷åíèå îòâåòà. </param>
	public async Task AddActionForWaitAsync(long id, Task action, MessageHandle handle)
	{
		await Task.Run(() =>
		{
			if (!IsResponseExpected(id))
			{
				_waitingForResponse[id] = (new Mutex(), new Queue<Task>(), new Queue<MessageHandle>());
			}

			// Åñëè ìüþòåêñ çàêðûò, çíà÷èò âûïîëíÿåòñÿ öåïî÷êà äåéñòâèé èëè äîáàâëÿåòñÿ äåéñòâèå â öåïî÷êó
			_waitingForResponse[id].Item1.WaitOne();
			_waitingForResponse[id].Item2.Enqueue(action);
			_waitingForResponse[id].Item3.Enqueue(handle);
			_waitingForResponse[id].Item1.ReleaseMutex();
		});
	}

	/// <summary>
	/// Âûçûâàåò ïåðâîå äåéñòâèå â î÷åðåäè.
	/// Ýòîò ìåòîä íóæíî âûçûâàòü òîëüêî ïîñëå ìåòîäà AddActionForWait.
	/// </summary>
	/// <param name="id"> Òåëåãðàìì ID ïîëüçîâàòåëÿ. </param>
	/// <returns> true - åñëè î÷åðåäü ñóùåñòâóåò, èíà÷å false </returns>
	public async Task<bool> StartActionsAsync(long id)
	{
		if (!IsResponseExpected(id))
			return false;

		// Ñòàðòóåì öåïî÷êó äåéñòâèé
		_waitingForResponse[id].Item1.WaitOne();
		await _waitingForResponse[id].Item2.Dequeue().ConfigureAwait(false);
		return true;
	}
}