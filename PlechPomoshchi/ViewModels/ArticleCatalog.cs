namespace PlechPomoshchi.ViewModels;

public static class ArticleCatalog
{
    public static readonly List<ArticleVm> All = new()
    {
        new(1,  "Как найти помощь быстро",                "/img/articles/a1.svg",  "Коротко о шагах и контактах.",  DummyBody(1)),
        new(2,  "Права и льготы: чек‑лист",              "/img/articles/a2.svg",  "Что проверить по документам.",  DummyBody(2)),
        new(3,  "Психологическая поддержка",             "/img/articles/a3.svg",  "Куда обратиться за поддержкой.", DummyBody(3)),
        new(4,  "Госпитали и волонтёрские сборы",        "/img/articles/a4.svg",  "Как устроена помощь в госпиталях.", DummyBody(4)),
        new(5,  "Реабилитация: базовые маршруты",        "/img/articles/a5.svg",  "Примерная логика действий.",     DummyBody(5)),
        new(6,  "Поддержка семей",                       "/img/articles/a6.svg",  "Где искать поддержку семье.",    DummyBody(6)),
        new(7,  "Медицинская помощь: документы",         "/img/articles/a7.svg",  "На что обратить внимание.",      DummyBody(7)),
        new(8,  "Юридическая помощь: типовые вопросы",   "/img/articles/a8.svg",  "Частые кейсы и решения.",        DummyBody(8)),
        new(9,  "Финансовая помощь и фонды",             "/img/articles/a9.svg",  "Как отличать надёжные фонды.",   DummyBody(9)),
        new(10, "Как волонтёрам работать безопасно",     "/img/articles/a10.svg", "Про базовые правила и риски.",   DummyBody(10)),
        new(11, "Карта помощи: как пользоваться",        "/img/articles/a11.svg", "Фильтры, избранное и поиск.",    DummyBody(11)),
    };

    private static string DummyBody(int n) =>
@"Это заглушка текста статьи №" + n + @".

Ты потом заменишь содержимое на своё. Здесь можно хранить:
- вводный абзац,
- основной текст,
- ссылки/контакты,
- полезные советы.

Важно: в реальном проекте добавь модерацию и проверку источников.";
}
