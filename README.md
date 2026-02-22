# Плечо помощи — прототип

В этом репозитории **два режима**:

## 1) GitHub Pages (статическое демо)
Папка: корень репозитория

- Работает **без сервера** (чистый HTML/CSS/JS).
- Карта: Leaflet + OpenStreetMap + геокодер.
- Данные организаций: `assets/data/orgs.json` (**1000+** записей).
- Избранное работает **через localStorage** (в браузере).

### Как включить GitHub Pages
1. Залей репозиторий на GitHub.
2. Settings → Pages
3. Source: **Deploy from a branch**
4. Branch: `main` / folder: **/docs**
5. Сохрани — GitHub выдаст ссылку на сайт.

## 2) Полная версия на ASP.NET Core (MVC) + EF Core + SQLite
Папка: `PlechPomoshchi/`

Есть то, что требует сервер:
- регистрация/вход/выход
- роли: Admin / Requester / Volunteer
- заявки + статусы + комментарии
- админ‑панель
- избранное хранится в БД

### Запуск backend локально
```bash
cd PlechPomoshchi
dotnet restore
dotnet run
```

Демо‑аккаунты:
- Admin: `admin@admin.com` / `admin`
- Volunteer: `volunteer@demo.com` / `demo12345`
- Requester: `requester@demo.com` / `demo12345`

> Важно: GitHub Pages не умеет запускать .NET сервер. Поэтому серверная часть работает отдельно (локально или на любом хостинге).
