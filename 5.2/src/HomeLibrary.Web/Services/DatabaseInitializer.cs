namespace HomeLibrary.Web.Services;

using HomeLibrary.Web.Models;

/// <summary>
/// Загрузка демонстрационных данных при первом запуске приложения (если таблица пуста).
/// </summary>
public class DatabaseInitializer
{
    private readonly BookService _bookService;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(BookService bookService, ILogger<DatabaseInitializer> logger)
    {
        _bookService = bookService;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _bookService.SearchAsync(null, null, null, null, null, 1, 1, cancellationToken);

        if (existing.TotalCount > 0)
        {
            _logger.LogInformation("Демонстрационные данные уже загружены: {Count} книг.", existing.TotalCount);
            return;
        }

        foreach (var book in DemoBooks())
        {
            await _bookService.CreateAsync(book, cancellationToken);
        }

        _logger.LogInformation("Демонстрационные книги успешно добавлены.");
    }

    private static IEnumerable<Book> DemoBooks()
    {
        yield return new Book
        {
            Title = "Война и мир",
            Author = "Лев Толстой",
            PublicationYear = 1869,
            Isbn = "9785001000001",
            Publisher = "Русский вестник",
            PageCount = 1300,
            Note = "Классика русской литературы.",
            TableOfContentsHtml =
                "<h2>Том первый</h2><ul><li>Часть первая</li><li>Часть вторая</li></ul>" +
                "<h2>Том второй</h2><ul><li>Часть третья</li></ul>"
        };

        yield return new Book
        {
            Title = "Преступление и наказание",
            Author = "Фёдор Достоевский",
            PublicationYear = 1866,
            Isbn = "9785001000002",
            Publisher = "Русский вестник",
            PageCount = 671,
            TableOfContentsHtml =
                "<h2>Часть первая</h2><ul><li>Глава 1</li><li>Глава 2</li></ul><h2>Эпилог</h2>"
        };

        yield return new Book
        {
            Title = "Мастер и Маргарита",
            Author = "Михаил Булгаков",
            PublicationYear = 1967,
            Isbn = "9785001000003",
            Publisher = "Художественная литература",
            PageCount = 480,
            Note = "Рукопись не горит.",
            TableOfContentsHtml =
                "<h2>Часть первая</h2><ul><li>Никогда не разговаривайте с неизвестными</li></ul>" +
                "<h2>Часть вторая</h2>"
        };
    }
}
