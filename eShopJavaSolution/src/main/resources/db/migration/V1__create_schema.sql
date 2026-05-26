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
-- catalog_type table
-- Ported from: CatalogDBContext.ConfigureCatalogType
-- Spring Boot CamelCaseToUnderscoresNamingStrategy maps
-- @Table(name = "CatalogType") -> catalog_type
-- ============================================================

CREATE TABLE [dbo].[catalog_type] (
    [id]   INT           NOT NULL IDENTITY(1,1),
    [type] NVARCHAR(100) NOT NULL,
    CONSTRAINT [PK_catalog_type] PRIMARY KEY CLUSTERED ([id])
);

-- ============================================================
-- catalog_brand table
-- Ported from: CatalogDBContext.ConfigureCatalogBrand
-- Spring Boot CamelCaseToUnderscoresNamingStrategy maps
-- @Table(name = "CatalogBrand") -> catalog_brand
-- ============================================================

CREATE TABLE [dbo].[catalog_brand] (
    [id]    INT           NOT NULL IDENTITY(1,1),
    [brand] NVARCHAR(100) NOT NULL,
    CONSTRAINT [PK_catalog_brand] PRIMARY KEY CLUSTERED ([id])
);

-- ============================================================
-- catalog table (maps to CatalogItem entity)
-- Ported from: CatalogDBContext.ConfigureCatalogItem
-- Id uses DatabaseGeneratedOption.None (manual assignment via HiLo)
-- Column names use snake_case per Spring Boot's physical naming strategy
-- ============================================================

CREATE TABLE [dbo].[catalog] (
    [id]                 INT            NOT NULL,
    [name]               NVARCHAR(50)   NOT NULL,
    [description]        NVARCHAR(MAX)  NULL,
    [price]              DECIMAL(18,2)  NOT NULL,
    [picture_file_name]  NVARCHAR(MAX)  NOT NULL,
    [catalog_type_id]    INT            NOT NULL,
    [catalog_brand_id]   INT            NOT NULL,
    [available_stock]    INT            NOT NULL DEFAULT 0,
    [restock_threshold]  INT            NOT NULL DEFAULT 0,
    [max_stock_threshold] INT           NOT NULL DEFAULT 0,
    [on_reorder]         BIT            NOT NULL DEFAULT 0,
    CONSTRAINT [PK_catalog] PRIMARY KEY CLUSTERED ([id]),
    CONSTRAINT [FK_catalog_catalog_brand] FOREIGN KEY ([catalog_brand_id])
        REFERENCES [dbo].[catalog_brand] ([id]),
    CONSTRAINT [FK_catalog_catalog_type] FOREIGN KEY ([catalog_type_id])
        REFERENCES [dbo].[catalog_type] ([id])
);
