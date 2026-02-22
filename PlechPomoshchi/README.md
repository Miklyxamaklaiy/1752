# Плечо помощи — прототип (ASP.NET Core 8 + EF Core + SQLite + Bootstrap + JS)

## Быстрый старт
1) Установи .NET 8 SDK
2) В папке проекта:
   - `dotnet restore`
   - `dotnet run`
3) Открой: `https://localhost:5001` или адрес из консоли

## Демо-пользователи
- Admin: `admin@admin.com` / `admin`
- Volunteer: `volunteer@demo.com` / `demo12345`
- Requester: `requester@demo.com` / `demo12345`

## Почта (заявка волонтерской организации)
В `appsettings.json` → `Smtp` укажи SMTP (Host/User/Password/To).
Если пусто — отправка пропускается, но заявка сохраняется в БД.

## Парсер организаций
Админ-панель: `/Admin` → кнопка "Запустить парсер сейчас".
Парсер пытается искать по запросам с ключом "СВО" + доп. теги и геокодить адреса.
Чтобы проект всегда был демонстрируемым, при недостатке данных БД дозаполняется демо-точками до 1000.

## Примечание про миграции
Проект использует `EnsureCreated()` внутри Seed'ера (учебный режим).
Если хочешь строго через миграции — создай миграции локально:
- `dotnet ef migrations add Init`
- `dotnet ef database update`
и замени в Seeder `EnsureCreated()` на `Migrate()`.
