namespace EventAggregator.API.Models;

public class Filter
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty; // Напр: "Діти інженерів", "IT-конференція"

    // Тип: "Genre", "Artist", "City", "Technology"
    public string Type { get; set; } = string.Empty;

    // Ключове слово для пошуку в тексті RSS
    public string SearchKeyword { get; set; } = string.Empty;

    // Зв'язок з користувачами (хто на що підписаний)
    public ICollection<UserFilter> UserFilters { get; set; } = new List<UserFilter>();
}