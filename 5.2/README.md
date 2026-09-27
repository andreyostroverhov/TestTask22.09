# Домашняя библиотека — ASP.NET Core MVC

Приложение для ведения домашней библиотеки: список книг с поиском, карточка книги,
создание / редактирование / удаление записей, а также визуальное редактирование **оглавления**,
которое в базе данных хранится в поле типа **XML**.

Ключевые особенности:
- **все операции с данными выполняются через хранимые процедуры MS SQL Server** (Dapper);
- оглавление хранится в колонке типа `XML` (HTML оборачивается в секцию CDATA);
- поиск по оглавлению выполняется средствами серверного **XQuery** (`xml.exist`);
- редактирование оглавления — через **HTML-редактор** (Quill) в карточке книги.

---

## Технологии

| Назначение | Технология |
|-----------|------------|
| Платформа | .NET 10, ASP.NET Core MVC |
| Доступ к данным | Dapper + хранимые процедуры |
| СУБД | MS SQL Server / LocalDB |
| UI | Razor Views, Bootstrap 5, Quill 2 (HTML-редактор) |

---

## Структура проекта

```
5.2/
├── HomeLibrary.slnx
├── .gitignore
├── db/
│   └── 01_schema_and_procedures.sql       # схема БД + все хранимые процедуры
└── src/
    └── HomeLibrary.Web/
        ├── HomeLibrary.Web.csproj
        ├── Program.cs                     # конфигурация и запуск
        ├── Data/
        │   └── DbConnectionFactory.cs     # фабрика открытых SqlConnection
        ├── Models/
        │   ├── Book.cs                    # модель книги (DataAnnotations)
        │   ├── BookListViewModel.cs       # модель страницы списка
        │   ├── PagedResult.cs             # постраничный результат
        │   └── TableOfContentsHelper.cs   # сборка/разбор XML-оглавления (CDATA)
        ├── Services/
        │   ├── BookService.cs             # Dapper поверх хранимых процедур
        │   └── DatabaseInitializer.cs     # демонстрационные данные
        ├── Controllers/
        │   └── BooksController.cs         # список, карточка, CRUD
        ├── Views/Books/                   # Index, Details, Create, Edit, Delete + partials
        └── wwwroot/                       # статика (bootstrap, jquery, css)
```

---

## Схема базы данных

Таблица `dbo.Books` (создаётся скриптом `db/01_schema_and_procedures.sql`):

| Колонка | Тип | Описание |
|---------|-----|----------|
| `Id` | `INT IDENTITY` | Первичный ключ |
| `Title` | `NVARCHAR(300)` | Название |
| `Author` | `NVARCHAR(200)` | Автор |
| `PublicationYear` | `INT` | Год издания (1400–2100) |
| `TableOfContents` | `XML` | **Оглавление книги** (структурированный XML) |
| `Isbn` | `NVARCHAR(20)` | ISBN (необязательно) |
| `Publisher` | `NVARCHAR(200)` | Издательство (необязательно) |
| `PageCount` | `INT` | Количество страниц (необязательно, ≥ 0) |
| `Note` | `NVARCHAR(1000)` | Заметка (необязательно) |
| `CreatedAtUtc` | `DATETIME2(3)` | Дата создания (UTC) |
| `UpdatedAtUtc` | `DATETIME2(3)` | Дата изменения (UTC) |

> Скрипт начинается с `SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;` — это обязательное требование
> для использования XML-методов (см. [Поиск по оглавлению](#поиск-по-оглавлению-xquery)).

---

## Хранимые процедуры

Вся работа с данными ведётся **только** через эти процедуры:

| Процедура | Назначение |
|-----------|------------|
| `dbo.Books_SelectById` | Выборка книги по идентификатору |
| `dbo.Books_Select` | Поиск с фильтрами + пагинация; возвращает выборку и общее количество |
| `dbo.Books_Insert` | Создание книги, возвращает новый `Id` |
| `dbo.Books_Update` | Обновление книги и оглавления |
| `dbo.Books_Delete` | Удаление книги |

Пример вызова из Dapper (`BookService`):

```csharp
var command = new CommandDefinition(
    "dbo.Books_Select",
    new { Title, Author, TableOfContents, PublicationYearFrom, PublicationYearTo, Offset, Fetch },
    commandType: CommandType.StoredProcedure,
    cancellationToken: cancellationToken);

using var grid = await connection.QueryMultipleAsync(command);
var rows = (await grid.ReadAsync<BookRow>()).ToList();
var totalCount = await grid.ReadSingleAsync<int>();
```

---

## Хранение оглавления в XML

Оглавление хранится в колонке типа `XML` с корневым элементом `<toc>`. HTML, приходящий из
редактора, помещается внутрь секции **CDATA**, чтобы не нарушать структуру XML:

```xml
<toc><![CDATA[<h2>Часть первая</h2><ul><li>Глава 1</li></ul>]]></toc>
```

Логика сосредоточена в `TableOfContentsHelper`:

- `ToXml(html)` — оборачивает HTML в `<toc><![CDATA[...]]></toc>`, экранируя `]]>`;
- `ToHtml(xml)` — извлекает HTML-содержимое для отображения и редактирования.

---

## Поиск по оглавлению (XQuery)

Поиск по названию и автору выполняется через `LIKE`, а по оглавлению — средствами XQuery.
Пользовательский ввод предварительно экранируется, чтобы его можно было безопасно подставить
в XQuery-выражение через `sql:variable`:

```sql
DECLARE @XmlSearch NVARCHAR(500);

IF @TableOfContents IS NOT NULL AND LTRIM(RTRIM(@TableOfContents)) <> N''
    SET @XmlSearch = REPLACE(REPLACE(REPLACE(@TableOfContents, N'&', N'&amp;'), N'<', N'&lt;'), N'>', N'&gt;');

-- ...

AND (
        @XmlSearch IS NULL
        OR TableOfContents.exist(N'/toc[contains(., sql:variable("@XmlSearch"))]') = 1
    )
```

Такой подход позволяет искать подстроку по **текстовому значению** всего оглавления
вне зависимости от вложенности HTML-тегов.

---

## Требования

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- MS SQL Server 2019+ **или** SQL Server Express / LocalDB

По умолчанию строка подключения указывает на LocalDB (`(localdb)\MSSQLLocalDB`).

---

## Запуск

### 1. Создание базы данных

Примените скрипт к вашему экземпляру SQL Server (создаёт БД `HomeLibrary`, таблицу и процедуры):

```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -E -I -f 65001 -i "db/01_schema_and_procedures.sql"
```

Параметры:
- `-E` — доверенная аутентификация (Windows);
- `-I` — включает `QUOTED_IDENTIFIER` (нужно для XML-методов);
- `-f 65001` — кодировка UTF-8 (корректная загрузка кириллицы).

> Скрипт идемпотентен: БД и объекты создаются только при их отсутствии.

### 2. Настройка строки подключения (при необходимости)

Файл `src/HomeLibrary.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "HomeLibrary": "Server=(localdb)\\MSSQLLocalDB;Database=HomeLibrary;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
  }
}
```

### 3. Запуск

```powershell
dotnet run --project src/HomeLibrary.Web
```

При старте приложение вызовет `DatabaseInitializer`, который при пустой таблице добавит три
демонстрационные книги. После этого откройте в браузере адрес из `launchSettings.json`
(по умолчанию — `http://localhost:5095`).

---

## Маршруты приложения

Корневой маршрут ведёт к списку книг.

| Метод | Маршрут | Действие |
|-------|---------|----------|
| `GET` | `/` или `/Books` | Список книг с поиском и пагинацией |
| `GET` | `/Books/Details/{id}` | Карточка книги |
| `GET` | `/Books/Create` | Форма создания |
| `POST` | `/Books/Create` | Создание книги |
| `GET` | `/Books/Edit/{id}` | Форма редактирования |
| `POST` | `/Books/Edit` | Сохранение изменений |
| `GET` | `/Books/Delete/{id}` | Подтверждение удаления |
| `POST` | `/Books/Delete/{id}` | Удаление книги |

Параметры поиска (`GET /Books`): `title`, `author`, `tableOfContents`, `publicationYearFrom`,
`publicationYearTo`, `page`.

---

## Демонстрационные данные

Демо-книги создаются приложением (`DatabaseInitializer`) через хранимую процедуру
`dbo.Books_Insert`. Это гарантирует корректное сохранение кириллицы (используются
Unicode-параметры Dapper), в том числе внутри XML-оглавления. Загрузка выполняется один раз —
если таблица не пуста, инициализация пропускается.

---

## Принятые решения

- **Хранимые процедуры вместо inline SQL.** В соответствии с требованием задания все операции
  чтения и записи идут через `dbo.Books_*`. Dapper выбран для тонкого контроля над вызовом
  процедур и маппингом результатов.
- **XML для оглавления.** Секция CDATA сохраняет произвольный HTML без потери разметки, а XQuery
  даёт серверный поиск по тексту оглавления.
- **`SET QUOTED_IDENTIFIER ON`.** Обязательно для XML-методов; включено в начале скрипта.
- **`-f 65001` при загрузке скрипта.** Предотвращает искажение кириллицы при выполнении через `sqlcmd`.
- **Единый источник XML-оглавления.** Сборка XML выполняется только в `TableOfContentsHelper`.
- **Сознательный минимализм.** Приложение реализовано компактно (один проект, сервис + Dapper +
  Razor) без избыточных надстроек (CQRS, ORM, DDD) — это соответствует масштабу задачи.
