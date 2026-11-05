SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME() <> N'eShopWinForms_Grpc'
    THROW 50001, 'Select eShopWinForms_Grpc before applying winforms-parity-v1.', 1;

DECLARE @identity bit = CONVERT(bit, COLUMNPROPERTY(OBJECT_ID(N'dbo.DiscountItems'), N'Id', 'IsIdentity'));
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE c SET Name = v.Name, Description = v.Name, PictureFileName = v.Picture
    FROM dbo.Catalog AS c
    JOIN (VALUES
        (1, N'.NET Bot Black Hoodie', N'2.png'),
        (2, N'.NET Black & White Mug', N'11.png'),
        (3, N'Prism White T-Shirt', N'7.png'),
        (4, N'.NET Foundation T-shirt', N'5.png'),
        (5, N'Roslyn Red Sheet', N'9.png'),
        (6, N'.NET Blue Hoodie', N'1.png'),
        (7, N'Roslyn Red T-Shirt', N'6.png'),
        (8, N'Kudu Purple Hoodie', N'3.png'),
        (9, N'Cup<T> White Mug', N'12.png'),
        (10, N'.NET Foundation Sheet', N'8.png'),
        (11, N'Cup<T> Sheet', N'10.png'),
        (12, N'Cup<T> TShirt', N'4.png')
    ) AS v(Id, Name, Picture) ON c.Id = v.Id;
    IF @@ROWCOUNT <> 12 THROW 50002, 'Expected all twelve default catalog items.', 1;

    DELETE FROM dbo.CatalogItemsStock WHERE StockId BETWEEN 1 AND 6;
    INSERT dbo.CatalogItemsStock (StockId, [Date], CatalogItemId, AvailableStock)
    VALUES (1, '20170920', 1, 100), (2, '20170921', 1, 120),
           (3, '20170922', 1, 80), (4, '20170920', 2, 45),
           (5, '20170925', 4, 65), (6, '20170928', 5, 22);

    DELETE FROM dbo.DiscountItems WHERE Id BETWEEN 1 AND 7;
    IF @identity = 1 SET IDENTITY_INSERT dbo.DiscountItems ON;
    INSERT dbo.DiscountItems (Id, [Start], [End], Size)
    VALUES (1, '20170918', '20170921', CONVERT(float(53), '0.30000001192092896')),
           (2, '20170922', '20170926', CONVERT(float(53), '0.25')),
           (3, '20170927', '20170930', CONVERT(float(53), '0.10000000149011612')),
           (4, '20171005', '20171020', CONVERT(float(53), '0.5')),
           (5, '20171113', '20171125', CONVERT(float(53), '0.30000001192092896')),
           (6, '20171220', '20171225', CONVERT(float(53), '0.25')),
           (7, '20260101', '20301231', CONVERT(float(53), '0.15'));
    IF @identity = 1 SET IDENTITY_INSERT dbo.DiscountItems OFF;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF @identity = 1 SET IDENTITY_INSERT dbo.DiscountItems OFF;
    THROW;
END CATCH;
