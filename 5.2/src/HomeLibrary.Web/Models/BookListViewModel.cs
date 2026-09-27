namespace HomeLibrary.Web.Models;

/// <summary>
/// Модель страницы списка книг: параметры поиска и пагинация.
/// </summary>
public class BookListViewModel
{
    public IReadOnlyList<Book> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 10;

    public string? Title { get; set; }

    public string? Author { get; set; }

    public string? TableOfContents { get; set; }

    public int? PublicationYearFrom { get; set; }

    public int? PublicationYearTo { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}