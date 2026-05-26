-- ============================================================
-- HiLo sequences for application-side ID generation
-- Ported from: dbo.catalog_hilo.Sequence.sql,
--              dbo.catalog_brand_hilo.Sequence.sql,
--              dbo.catalog_type_hilo.Sequence.sql
-- ============================================================

CREATE SEQUENCE [dbo].[catalog_hilo]
    AS BIGINT
    START WITH 1
    INCREMENT BY 10
    MINVALUE -9223372036854775808
    MAXVALUE 9223372036854775807
    CACHE;

CREATE SEQUENCE [dbo].[catalog_brand_hilo]
    AS BIGINT
    START WITH 1
    INCREMENT BY 10
    MINVALUE -9223372036854775808
    MAXVALUE 9223372036854775807
    CACHE;

CREATE SEQUENCE [dbo].[catalog_type_hilo]
    AS BIGINT
    START WITH 1
    INCREMENT BY 10
    MINVALUE -9223372036854775808
    MAXVALUE 9223372036854775807
    CACHE;

-- ============================================================
-- CatalogType table
-- Ported from: CatalogDBContext.ConfigureCatalogType
-- ============================================================

CREATE TABLE [dbo].[CatalogType] (
    [Id]   INT           NOT NULL IDENTITY(1,1),
    [Type] NVARCHAR(100) NOT NULL,
    CONSTRAINT [PK_CatalogType] PRIMARY KEY CLUSTERED ([Id])
);

-- ============================================================
-- CatalogBrand table
-- Ported from: CatalogDBContext.ConfigureCatalogBrand
-- ============================================================

CREATE TABLE [dbo].[CatalogBrand] (
    [Id]    INT           NOT NULL IDENTITY(1,1),
    [Brand] NVARCHAR(100) NOT NULL,
    CONSTRAINT [PK_CatalogBrand] PRIMARY KEY CLUSTERED ([Id])
);

-- ============================================================
-- Catalog table (maps to CatalogItem entity)
-- Ported from: CatalogDBContext.ConfigureCatalogItem
-- Id uses DatabaseGeneratedOption.None (manual assignment via HiLo)
-- ============================================================

CREATE TABLE [dbo].[Catalog] (
    [Id]                INT            NOT NULL,
    [Name]              NVARCHAR(50)   NOT NULL,
    [Description]       NVARCHAR(MAX)  NULL,
    [Price]             DECIMAL(18,2)  NOT NULL,
    [PictureFileName]   NVARCHAR(MAX)  NOT NULL,
    [CatalogTypeId]     INT            NOT NULL,
    [CatalogBrandId]    INT            NOT NULL,
    [AvailableStock]    INT            NOT NULL DEFAULT 0,
    [RestockThreshold]  INT            NOT NULL DEFAULT 0,
    [MaxStockThreshold] INT            NOT NULL DEFAULT 0,
    [OnReorder]         BIT            NOT NULL DEFAULT 0,
    CONSTRAINT [PK_Catalog] PRIMARY KEY CLUSTERED ([Id]),
    CONSTRAINT [FK_Catalog_CatalogBrand] FOREIGN KEY ([CatalogBrandId])
        REFERENCES [dbo].[CatalogBrand] ([Id]),
    CONSTRAINT [FK_Catalog_CatalogType] FOREIGN KEY ([CatalogTypeId])
        REFERENCES [dbo].[CatalogType] ([Id])
);
