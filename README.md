# HelperBot
Телеграм-бот для адаптации сотрудников компании.

## Использованные технологии
Телеграм-бот разработан на платформе .NET 8.0. 
Была использована библиотека Telegram.Bot. 
Был использован фреймворк EntityFramework для работы с СУБД PostgreSQL.

## Функционал
Функционал описан в диаграмме:

![image](Docs/Images/Диаграмма.jpg)

Несколько примеров функционала в скринах:

![image](Docs/Images/Рисунок1.png)
![image](Docs/Images/Рисунок2.png)
![image](Docs/Images/Рисунок3.png)
![image](Docs/Images/Рисунок4.png)
![image](Docs/Images/Рисунок5.png)
![image](Docs/Images/Рисунок6.png)

## Запуск
Запустить в терминале: 
docker-compose up --build

Чтобы добавить первого пользователя:
dotnet run --project ConnectToSocket 80
