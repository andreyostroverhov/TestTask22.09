namespace HomeLibrary.Web.Models;

using System.Xml.Linq;

/// <summary>
/// Вспомогательные методы для работы с оглавлением, которое хранится в БД
/// в поле типа XML с корневым элементом &lt;toc&gt;.
/// HTML из визуального редактора помещается внутрь секции CDATA.
/// </summary>
public static class TableOfContentsHelper
{
    public const string RootElementName = "toc";

    /// <summary>Пустое оглавление в виде XML.</summary>
    public const string EmptyXml = "<toc />";

    /// <summary>
    /// Формирует XML-оглавление из HTML редактора, помещая HTML в секцию CDATA.
    /// </summary>
    public static string ToXml(string? html)
    {
        var safeHtml = (html ?? string.Empty).Replace("]]>", "]]]]><![CDATA[>");

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(RootElementName, new XCData(safeHtml)));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    /// <summary>
    /// Извлекает HTML-содержимое оглавления из XML. При некорректном XML возвращает пустую строку.
    /// </summary>
    public static string ToHtml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
            return string.Empty;

        try
        {
            var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            return document.Root?.Value ?? string.Empty;
        }
        catch (System.Xml.XmlException)
        {
            return string.Empty;
        }
    }
}
