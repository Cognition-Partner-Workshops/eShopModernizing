using System.ComponentModel.DataAnnotations;
using eShop.Catalog.Domain;
using Xunit;

namespace eShop.Catalog.Domain.Tests;

public class CatalogItemTests
{
    [Fact]
    public void NewCatalogItem_UsesTheDefaultPictureName()
    {
        var item = new CatalogItem();

        Assert.Equal("dummy.png", item.PictureFileName);
        Assert.Equal(CatalogItem.DefaultPictureName, item.PictureFileName);
    }

    [Fact]
    public void CatalogItem_WithoutAName_FailsValidation()
    {
        var item = new CatalogItem { Name = string.Empty, Price = 10m };
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(item, new ValidationContext(item), results, validateAllProperties: true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CatalogItem.Name)));
    }

    [Fact]
    public void CatalogItem_HasNoEntityFrameworkDependency()
    {
        var referenced = typeof(CatalogItem).Assembly.GetReferencedAssemblies();

        Assert.DoesNotContain(referenced, a => a.Name is not null && a.Name.StartsWith("EntityFramework", StringComparison.Ordinal));
        Assert.DoesNotContain(referenced, a => a.Name is not null && a.Name.StartsWith("System.Data.Entity", StringComparison.Ordinal));
    }
}
