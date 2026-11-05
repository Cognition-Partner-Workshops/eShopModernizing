SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'eShopWinForms_Legacy'
    THROW 50001, 'Select eShopWinForms_Legacy before applying winforms-parity-v1.', 1;

DECLARE @identity bit = CONVERT(bit, COLUMNPROPERTY(OBJECT_ID(N'dbo.DiscountItems'), N'Id', 'IsIdentity'));
BEGIN TRY
    BEGIN TRANSACTION;
    DELETE FROM dbo.DiscountItems WHERE Id = 7;
    IF @identity = 1 SET IDENTITY_INSERT dbo.DiscountItems ON;
    INSERT dbo.DiscountItems (Id, [Start], [End], Size)
    VALUES (7, '20260101', '20301231', CONVERT(float(53), 0.15));
    IF @identity = 1 SET IDENTITY_INSERT dbo.DiscountItems OFF;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF @identity = 1 SET IDENTITY_INSERT dbo.DiscountItems OFF;
    THROW;
END CATCH;
