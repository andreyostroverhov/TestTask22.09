/**************************************************************************************************
    Домашняя библиотека — схема базы данных и хранимые процедуры (MS SQL Server / T-SQL)
    Все операции CRUD и поиск выполняются исключительно через хранимые процедуры.
    Оглавление книги хранится в поле типа XML.
**************************************************************************************************/

-- XML-методы (xml.exist) и связанные объекты требуют QUOTED_IDENTIFIER ON / ANSI_NULLS ON.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF DB_ID(N'HomeLibrary') IS NULL
BEGIN
    CREATE DATABASE [HomeLibrary];
END
GO

USE [HomeLibrary];
GO

/*------------------------------------------------------------------------------------------------
    Таблица Books
------------------------------------------------------------------------------------------------*/
IF OBJECT_ID(N'dbo.Books', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Books
    (
        Id              INT             IDENTITY(1,1)   NOT NULL,
        Title           NVARCHAR(300)                   NOT NULL,
        Author          NVARCHAR(200)                   NOT NULL,
        PublicationYear INT                             NOT NULL,
        -- Оглавление книги в формате XML.
        TableOfContents XML                             NOT NULL
            CONSTRAINT DF_Books_TableOfContents DEFAULT (N'<toc />'),
        Isbn            NVARCHAR(20)                    NULL,
        Publisher       NVARCHAR(200)                   NULL,
        PageCount       INT                             NULL,
        Note            NVARCHAR(1000)                  NULL,
        CreatedAtUtc    DATETIME2(3)                    NOT NULL  CONSTRAINT DF_Books_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc    DATETIME2(3)                    NOT NULL  CONSTRAINT DF_Books_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Books PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Books_PublicationYear CHECK (PublicationYear BETWEEN 1400 AND 2100),
        CONSTRAINT CK_Books_PageCount CHECK (PageCount IS NULL OR PageCount >= 0)
    );
END
GO

/*------------------------------------------------------------------------------------------------
    Примечание: демонстрационные данные загружаются приложением при первом запуске
    (HomeLibrary.Web.Services.DatabaseInitializer) с корректной обработкой Unicode — это
    гарантирует правильное сохранение кириллицы, в том числе внутри XML.
------------------------------------------------------------------------------------------------*/

/*------------------------------------------------------------------------------------------------
    SP: Books_SelectById — выборка книги по идентификатору
------------------------------------------------------------------------------------------------*/
CREATE OR ALTER PROCEDURE dbo.Books_SelectById
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Id,
        Title,
        Author,
        PublicationYear,
        CAST(TableOfContents AS NVARCHAR(MAX)) AS TableOfContentsXml,
        Isbn,
        Publisher,
        PageCount,
        Note,
        CreatedAtUtc,
        UpdatedAtUtc
    FROM dbo.Books
    WHERE Id = @Id;
END
GO

/*------------------------------------------------------------------------------------------------
    SP: Books_Select — поиск с фильтрами и пагинацией
    Поиск по названию/автору — LIKE, по оглавлению — средствами XQuery (xml.exist).
------------------------------------------------------------------------------------------------*/
CREATE OR ALTER PROCEDURE dbo.Books_Select
    @Title               NVARCHAR(300) = NULL,
    @Author              NVARCHAR(200) = NULL,
    @TableOfContents     NVARCHAR(400) = NULL,
    @PublicationYearFrom INT           = NULL,
    @PublicationYearTo   INT           = NULL,
    @Offset              INT           = 0,
    @Fetch               INT           = 10
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @XmlSearch NVARCHAR(500) = NULL;

    IF @TableOfContents IS NOT NULL AND LTRIM(RTRIM(@TableOfContents)) <> N''
    BEGIN
        -- Экранирование символов, запрещённых в XML-литерале XQuery.
        SET @XmlSearch = REPLACE(REPLACE(REPLACE(@TableOfContents, N'&', N'&amp;'), N'<', N'&lt;'), N'>', N'&gt;');
    END

    ;WITH Filtered AS
    (
        SELECT
            Id,
            Title,
            Author,
            PublicationYear,
            CAST(TableOfContents AS NVARCHAR(MAX)) AS TableOfContentsXml,
            Isbn,
            Publisher,
            PageCount,
            Note,
            CreatedAtUtc,
            UpdatedAtUtc
        FROM dbo.Books
        WHERE
            (@Title IS NULL OR @Title = N'' OR Title LIKE N'%' + @Title + N'%')
            AND (@Author IS NULL OR @Author = N'' OR Author LIKE N'%' + @Author + N'%')
            AND (@PublicationYearFrom IS NULL OR PublicationYear >= @PublicationYearFrom)
            AND (@PublicationYearTo IS NULL OR PublicationYear <= @PublicationYearTo)
            AND (
                    @XmlSearch IS NULL
                    OR TableOfContents.exist(N'/toc[contains(., sql:variable("@XmlSearch"))]') = 1
                )
    )
    SELECT *
    FROM Filtered
    ORDER BY Id DESC
    OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY;

    -- Общее количество для пагинации.
    SELECT COUNT(1) AS TotalCount
    FROM dbo.Books
    WHERE
        (@Title IS NULL OR @Title = N'' OR Title LIKE N'%' + @Title + N'%')
        AND (@Author IS NULL OR @Author = N'' OR Author LIKE N'%' + @Author + N'%')
        AND (@PublicationYearFrom IS NULL OR PublicationYear >= @PublicationYearFrom)
        AND (@PublicationYearTo IS NULL OR PublicationYear <= @PublicationYearTo)
        AND (
                @XmlSearch IS NULL
                OR TableOfContents.exist(N'/toc[contains(., sql:variable("@XmlSearch"))]') = 1
            );
END
GO

/*------------------------------------------------------------------------------------------------
    SP: Books_Insert — создание книги, возвращает идентификатор
------------------------------------------------------------------------------------------------*/
CREATE OR ALTER PROCEDURE dbo.Books_Insert
    @Title           NVARCHAR(300),
    @Author          NVARCHAR(200),
    @PublicationYear INT,
    @TableOfContents XML,
    @Isbn            NVARCHAR(20)  = NULL,
    @Publisher       NVARCHAR(200) = NULL,
    @PageCount       INT           = NULL,
    @Note            NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Books
        (Title, Author, PublicationYear, TableOfContents, Isbn, Publisher, PageCount, Note, CreatedAtUtc, UpdatedAtUtc)
    VALUES
        (@Title, @Author, @PublicationYear, @TableOfContents, @Isbn, @Publisher, @PageCount, @Note, SYSUTCDATETIME(), SYSUTCDATETIME());

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS Id;
END
GO

/*------------------------------------------------------------------------------------------------
    SP: Books_Update — обновление книги и оглавления
------------------------------------------------------------------------------------------------*/
CREATE OR ALTER PROCEDURE dbo.Books_Update
    @Id              INT,
    @Title           NVARCHAR(300),
    @Author          NVARCHAR(200),
    @PublicationYear INT,
    @TableOfContents XML,
    @Isbn            NVARCHAR(20)  = NULL,
    @Publisher       NVARCHAR(200) = NULL,
    @PageCount       INT           = NULL,
    @Note            NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Books
    SET
        Title           = @Title,
        Author          = @Author,
        PublicationYear = @PublicationYear,
        TableOfContents = @TableOfContents,
        Isbn            = @Isbn,
        Publisher       = @Publisher,
        PageCount       = @PageCount,
        Note            = @Note,
        UpdatedAtUtc    = SYSUTCDATETIME()
    WHERE Id = @Id;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

/*------------------------------------------------------------------------------------------------
    SP: Books_Delete — удаление книги
------------------------------------------------------------------------------------------------*/
CREATE OR ALTER PROCEDURE dbo.Books_Delete
    @Id INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Books WHERE Id = @Id;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

PRINT N'Схема HomeLibrary и хранимые процедуры успешно созданы.';
GO
