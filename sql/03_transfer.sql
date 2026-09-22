CREATE OR ALTER PROCEDURE dbo.TransferMoney
    @N1 int,
    @N2 int,
    @S decimal(18, 2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @S <= 0
        THROW 50001, 'Сумма перевода должна быть больше нуля.', 1;

    IF @N1 = @N2
        THROW 50002, 'Счета отправителя и получателя должны отличаться.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- UPDLOCK не дает двум параллельным переводам одновременно
        -- принять решение на основании одного и того же остатка.
        DECLARE @Balance decimal(18, 2);

        SELECT @Balance = S
        FROM dbo.T WITH (UPDLOCK, ROWLOCK)
        WHERE N = @N1;

        IF @Balance IS NULL
            THROW 50003, 'Счет отправителя не найден.', 1;

        IF @Balance < @S
            THROW 50004, 'Недостаточно средств.', 1;

        IF NOT EXISTS (SELECT 1 FROM dbo.T WHERE N = @N2)
            THROW 50005, 'Счет получателя не найден.', 1;

        UPDATE dbo.T
        SET S = S - @S
        WHERE N = @N1;

        UPDATE dbo.T
        SET S = S + @S
        WHERE N = @N2;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
