namespace HomeLibrary.Web.Controllers;

using HomeLibrary.Web.Models;
using HomeLibrary.Web.Services;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Управление домашней библиотекой: список с поиском, карточка, создание, редактирование, удаление.
/// Все операции выполняются через сервис, работающий с хранимыми процедурами.
/// </summary>
public class BooksController : Controller
{
    private const int PageSize = 10;

    private readonly BookService _bookService;

    public BooksController(BookService bookService)
    {
        _bookService = bookService;
    }

    /// <summary>Список книг с поиском и пагинацией.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? title,
        string? author,
        string? tableOfContents,
        int? publicationYearFrom,
        int? publicationYearTo,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var result = await _bookService.SearchAsync(
            title,
            author,
            tableOfContents,
            publicationYearFrom,
            publicationYearTo,
            page,
            PageSize,
            cancellationToken);

        var viewModel = new BookListViewModel
        {
            Items = result.Items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Title = title,
            Author = author,
            TableOfContents = tableOfContents,
            PublicationYearFrom = publicationYearFrom,
            PublicationYearTo = publicationYearTo
        };

        return View(viewModel);
    }

    /// <summary>Просмотр карточки книги.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        var book = await _bookService.GetByIdAsync(id, cancellationToken);

        if (book is null)
        {
            TempData["Error"] = $"Книга с идентификатором {id} не найдена.";
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    /// <summary>Форма создания книги.</summary>
    [HttpGet]
    public IActionResult Create()
    {
        var model = new Book
        {
            PublicationYear = DateTime.UtcNow.Year,
            TableOfContentsHtml = "<h2>Часть первая</h2><ul><li>Глава 1</li></ul>"
        };

        return View(model);
    }

    /// <summary>Обработка создания книги.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Book model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        var id = await _bookService.CreateAsync(model, cancellationToken);

        TempData["Success"] = "Книга успешно добавлена.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Форма редактирования книги.</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var book = await _bookService.GetByIdAsync(id, cancellationToken);

        if (book is null)
        {
            TempData["Error"] = $"Книга с идентификатором {id} не найдена.";
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    /// <summary>Обработка редактирования книги.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Book model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return View(model);

        await _bookService.UpdateAsync(model, cancellationToken);

        TempData["Success"] = "Изменения сохранены.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    /// <summary>Подтверждение удаления книги.</summary>
    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken = default)
    {
        var book = await _bookService.GetByIdAsync(id, cancellationToken);

        if (book is null)
        {
            TempData["Error"] = $"Книга с идентификатором {id} не найдена.";
            return RedirectToAction(nameof(Index));
        }

        return View(book);
    }

    /// <summary>Обработка удаления книги.</summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken = default)
    {
        var deleted = await _bookService.DeleteAsync(id, cancellationToken);

        if (deleted)
            TempData["Success"] = "Книга удалена.";
        else
            TempData["Error"] = $"Книга с идентификатором {id} не найдена.";

        return RedirectToAction(nameof(Index));
    }
}