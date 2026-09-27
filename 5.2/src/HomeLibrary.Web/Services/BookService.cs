namespace HomeLibrary.Web.Services;

using System.Data;
using Dapper;
using HomeLibrary.Web.Data;
using HomeLibrary.Web.Models;

/// <summary>
/// Сервис работы с книгами. Все операции выполняются через хранимые процедуры
/// MS SQL Server посредством Dapper.
/// </summary>
public class BookService
{
    private readonly DbConnectionFactory _connectionFactory;

    public BookService(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>Выборка книги по идентификатору.</summary>
    public async Task<Book?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.Books_SelectById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<BookRow>(command);

        return row is null ? null : MapToBook(row);
    }

    /// <summary>Поиск книг по названию, автору, оглавлению и диапазону года с пагинацией.</summary>
    public async Task<PagedResult<Book>> SearchAsync(
        string? title,
        string? author,
        string? tableOfContents,
        int? publicationYearFrom,
        int? publicationYearTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 10;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var parameters = new
        {
            Title = title,
            Author = author,
            TableOfContents = tableOfContents,
            PublicationYearFrom = publicationYearFrom,
            PublicationYearTo = publicationYearTo,
            Offset = (page - 1) * pageSize,
            Fetch = pageSize
        };

        var command = new CommandDefinition(
            "dbo.Books_Select",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        using var grid = await connection.QueryMultipleAsync(command);

        var rows = (await grid.ReadAsync<BookRow>()).ToList();
        var totalCount = await grid.ReadSingleAsync<int>();

        return new PagedResult<Book>
        {
            Items = rows.Select(MapToBook).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>Создание книги. Возвращает идентификатор новой записи.</summary>
    public async Task<int> CreateAsync(Book book, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.Books_Insert",
            new
            {
                book.Title,
                book.Author,
                book.PublicationYear,
                TableOfContents = TableOfContentsHelper.ToXml(book.TableOfContentsHtml),
                book.Isbn,
                book.Publisher,
                book.PageCount,
                book.Note
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<int>(command);
    }

    /// <summary>Обновление книги и её оглавления.</summary>
    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.Books_Update",
            new
            {
                book.Id,
                book.Title,
                book.Author,
                book.PublicationYear,
                TableOfContents = TableOfContentsHelper.ToXml(book.TableOfContentsHtml),
                book.Isbn,
                book.Publisher,
                book.PageCount,
                book.Note
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        await connection.ExecuteAsync(command);
    }

    /// <summary>Удаление книги. Возвращает true, если запись была удалена.</summary>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var command = new CommandDefinition(
            "dbo.Books_Delete",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);

        var affected = await connection.ExecuteScalarAsync<int>(command);

        return affected > 0;
    }

    private static Book MapToBook(BookRow row) => new()
    {
        Id = row.Id,
        Title = row.Title,
        Author = row.Author,
        PublicationYear = row.PublicationYear,
        Isbn = row.Isbn,
        Publisher = row.Publisher,
        PageCount = row.PageCount,
        Note = row.Note,
        TableOfContentsXml = row.TableOfContentsXml ?? TableOfContentsHelper.EmptyXml,
        TableOfContentsHtml = TableOfContentsHelper.ToHtml(row.TableOfContentsXml),
        CreatedAtUtc = row.CreatedAtUtc,
        UpdatedAtUtc = row.UpdatedAtUtc
    };

    /// <summary>Плоская модель строки таблицы Books для маппинга Dapper.</summary>
    private sealed class BookRow
    {
        public int Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;
        public int PublicationYear { get; init; }
        public string? TableOfContentsXml { get; init; }
        public string? Isbn { get; init; }
        public string? Publisher { get; init; }
        public int? PageCount { get; init; }
        public string? Note { get; init; }
        public DateTime CreatedAtUtc { get; init; }
        public DateTime UpdatedAtUtc { get; init; }
    }
}
