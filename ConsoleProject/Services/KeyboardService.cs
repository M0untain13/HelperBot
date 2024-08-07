﻿using Microsoft.Extensions.Logging;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConsoleProject.Services;

public class KeyboardService
{
	private readonly Dictionary<string, InlineKeyboardMarkup> _inlineKeyboards;
    private readonly Dictionary<string, ReplyKeyboardMarkup> _replyKeyboards;
	private readonly ILogger _logger;

	public KeyboardService(ILogger logger)
	{
		_logger = logger;
        _inlineKeyboards = new Dictionary<string, InlineKeyboardMarkup>();
        _replyKeyboards = new Dictionary<string, ReplyKeyboardMarkup>();
        _inlineKeyboards["user"] = new InlineKeyboardMarkup(
            new InlineKeyboardButton[][]
            {
                [
                    InlineKeyboardButton.WithCallbackData("FAQ", "user_faq_button"),
                    InlineKeyboardButton.WithCallbackData("Настроение за пять дней", "user_mood_button")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("Задать вопрос", "user_ask_button"),
                    InlineKeyboardButton.WithCallbackData("Мои открытые вопросы", "user_open_questions_button"),

                ],
                
            }
        );
        _inlineKeyboards["hr"] = new InlineKeyboardMarkup(
            new InlineKeyboardButton[][]{
                [
                    InlineKeyboardButton.WithCallbackData("Добавить пользователя", "hr_adduser_button"),
                    InlineKeyboardButton.WithCallbackData("Удалить пользователя", "hr_deluser_button")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("График настроений", "hr_mood_button"),
                    InlineKeyboardButton.WithCallbackData("Открытые вопросы", "hr_getask_button")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("Редактировать FAQ", "hr_editfaq_button")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("Получить список пользователей", "hr_get_all_users_button")
                ]
            }
        );
        _inlineKeyboards["edit_faq"] = new InlineKeyboardMarkup(
             new InlineKeyboardButton[][]
             {
                [
                    InlineKeyboardButton.WithCallbackData("Добавить новый FAQ", "hr_add_faq"),
                    InlineKeyboardButton.WithCallbackData("Изменить FAQ", "hr_modify_faq")
                ],
                [
                    InlineKeyboardButton.WithCallbackData("Удалить FAQ", "hr_delete_faq"),
                    InlineKeyboardButton.WithCallbackData("Вернуться назад", "hr_back_to_main")
                ]
             }
         );

         _inlineKeyboards["moods"] = new InlineKeyboardMarkup(
             new InlineKeyboardButton[][]
             {
                 [
                     InlineKeyboardButton.WithCallbackData("Узнать полный график настроения пользователя",
                         "hr_all_mood_button"),
                 ],
                 [
                     InlineKeyboardButton.WithCallbackData("Узнать настроение за конкретный период", "hr_all_mood_period_button")
                 ]
             });

         _replyKeyboards["menu"] = new ReplyKeyboardMarkup(
             new KeyboardButton[][]
             {
                 [
                     new KeyboardButton("/menu"),
                 ],
                 [
                     new KeyboardButton("/help"),
                 ],
                 [
                     new KeyboardButton("/clear"),
                 ],
             }
         );
    }

	public string[] GetNames() => _inlineKeyboards.Keys.ToArray();

	public InlineKeyboardMarkup? GetInlineKeyboard(string name)
	{
		if (!GetNames().Contains(name))
		{
			_logger.LogError($"Keyboard \"{name}\" does not exist.");
			return null;
		}

		return _inlineKeyboards[name];
	}
    
    public ReplyKeyboardMarkup? GetReplyKeyboard(string name)
    {
        if (!_replyKeyboards.Keys.Contains(name))
        {
            _logger.LogError($"Keyboard \"{name}\" does not exist.");
            return null;
        }

        return _replyKeyboards[name];
    }
}
