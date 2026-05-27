using EventAggregator.API.Models;

namespace EventAggregator.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        SeedFeedSources(db);
        SeedFilters(db);
    }

    private static void SeedFeedSources(AppDbContext db)
    {
        if (db.FeedSources.Any()) return;

        db.FeedSources.AddRange(
            // --- Новини ---
            new FeedSource { Name = "Українська правда",    FeedUrl = "https://www.pravda.com.ua/rss/view_news/",                    Category = "news",    Language = "uk" },
            new FeedSource { Name = "LIGA.net",             FeedUrl = "https://www.liga.net/en/news/rss.xml",                        Category = "news",    Language = "en" },
            new FeedSource { Name = "AIN.EN",               FeedUrl = "https://en.ain.ua/feed/",                                     Category = "tech",    Language = "en" },
            new FeedSource { Name = "dev.ua",               FeedUrl = "https://dev.ua/rss",                                          Category = "tech",    Language = "uk" },
            new FeedSource { Name = "BBC News",             FeedUrl = "http://feeds.bbci.co.uk/news/rss.xml",                        Category = "news",    Language = "en" },
            new FeedSource { Name = "The Guardian",         FeedUrl = "https://www.theguardian.com/world/rss",                       Category = "news",    Language = "en" },
            new FeedSource { Name = "MIT News",             FeedUrl = "https://news.mit.edu/rss/feed",                               Category = "science", Language = "en" },

            // --- IT-події ---
            new FeedSource { Name = "DOU Calendar",         FeedUrl = "https://dou.ua/calendar/feed/",                               Category = "events",  Language = "uk" },

            // --- Академічні події ---
            new FeedSource { Name = "Stanford Events",      FeedUrl = "https://events.stanford.edu/rss",                             Category = "events",  Language = "en" },
            new FeedSource { Name = "Harvard Gazette",      FeedUrl = "https://news.harvard.edu/gazette/feed/harvard-events/",       Category = "events",  Language = "en" },

            // --- Культурні / концертні події ---
            new FeedSource { Name = "Афіша Запоріжжя",     FeedUrl = "https://afisha.zp.ua/docs/feedrss/",                          Category = "events",  Language = "uk" },
            new FeedSource { Name = "AfishaLviv Концерти", FeedUrl = "https://afishalviv.net/category/koncerti-ta-festivali/feed/", Category = "events",  Language = "uk" },
            new FeedSource { Name = "LvivOnline",           FeedUrl = "https://lviv-online.com/ua/feed/",                            Category = "events",  Language = "uk" },
            new FeedSource { Name = "OBX Things Events",    FeedUrl = "https://www.obxthings.com/events.rss",                       Category = "events",  Language = "en" },
            new FeedSource { Name = "Kingston Live",        FeedUrl = "https://kingstonlive.ca/calendar/rss",                        Category = "events",  Language = "en" }
        );

        db.SaveChanges();
    }

    private static void SeedFilters(AppDbContext db)
    {
        if (db.Filters.Any()) return;

        db.Filters.AddRange(
            // --- Виконавці ---
            new Filter { Name = "Діти інженерів",   Type = "Artist",    SearchKeyword = "діти інженерів" },
            new Filter { Name = "Лея",               Type = "Artist",    SearchKeyword = "лея" },
            new Filter { Name = "Жадан і Собаки",   Type = "Artist",    SearchKeyword = "жадан" },
            new Filter { Name = "Океан Ельзи",       Type = "Artist",    SearchKeyword = "океан ельзи" },
            new Filter { Name = "Антитіла",          Type = "Artist",    SearchKeyword = "антитіла" },

            // --- Типи подій ---
            new Filter { Name = "Концерт",           Type = "EventType", SearchKeyword = "концерт" },
            new Filter { Name = "Фестиваль",         Type = "EventType", SearchKeyword = "фестиваль" },
            new Filter { Name = "Виставка",          Type = "EventType", SearchKeyword = "виставка" },
            new Filter { Name = "Вебінар",           Type = "EventType", SearchKeyword = "вебінар" },
            new Filter { Name = "Мітап",             Type = "EventType", SearchKeyword = "мітап" },
            new Filter { Name = "Конференція",       Type = "EventType", SearchKeyword = "конференція" },
            new Filter { Name = "Майстер-клас",      Type = "EventType", SearchKeyword = "майстер-клас" },

            // --- Міста ---
            new Filter { Name = "Київ",              Type = "City",      SearchKeyword = "київ" },
            new Filter { Name = "Львів",             Type = "City",      SearchKeyword = "львів" },
            new Filter { Name = "Харків",            Type = "City",      SearchKeyword = "харків" },
            new Filter { Name = "Одеса",             Type = "City",      SearchKeyword = "одеса" },

            // --- Технології (для IT-подій) ---
            new Filter { Name = ".NET / C#",         Type = "Technology", SearchKeyword = ".net" },
            new Filter { Name = "JavaScript",        Type = "Technology", SearchKeyword = "javascript" },
            new Filter { Name = "Python",            Type = "Technology", SearchKeyword = "python" },
            new Filter { Name = "AI / ML",           Type = "Technology", SearchKeyword = "ai" }
        );

        db.SaveChanges();
    }
}
