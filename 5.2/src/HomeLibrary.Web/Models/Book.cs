namespace HomeLibrary.Web.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Книга домашней библиотеки.
/// </summary>
public class Book
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Укажите название книги.")]
    [StringLength(300)]
    [Display(Name = "Название")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите автора.")]
    [StringLength(200)]
    [Display(Name = "Автор")]
    public string Author { get; set; } = string.Empty;

    [Range(1400, 2100, ErrorMessage = "Некорректный год издания.")]
    [Display(Name = "Год издания")]
    public int PublicationYear { get; set; } = DateTime.UtcNow.Year;

    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string? Isbn { get; set; }

    [StringLength(200)]
    [Display(Name = "Издательство")]
    public string? Publisher { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Количество страниц не может быть отрицательным.")]
    [Display(Name = "Страниц")]
    public int? PageCount { get; set; }

    [StringLength(1000)]
    [Display(Name = "Заметка")]
    public string? Note { get; set; }

    /// <summary>Оглавление книги в виде XML (хранится в БД в поле типа XML).</summary>
    public string TableOfContentsXml { get; set; } = TableOfContentsHelper.EmptyXml;

    /// <summary>HTML-содержимое оглавления, извлекаемое из XML для отображения и редактирования.</summary>
    public string TableOfContentsHtml { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
