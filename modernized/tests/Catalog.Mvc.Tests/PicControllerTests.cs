using Catalog.Mvc.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Catalog.Mvc.Tests;

/// <summary>
/// Tests the ported <see cref="PicController"/> image endpoint, which serves the
/// item picture from the app's <c>wwwroot/Pics</c> folder.
/// </summary>
public class PicControllerTests : IDisposable
{
    private readonly CatalogDbContextFixture _fixture = new();
    private readonly string _webRoot;

    public PicControllerTests()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), "catalog-mvc-pics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_webRoot, "Pics"));
        // Item 1 is seeded with PictureFileName "1.png".
        File.WriteAllBytes(Path.Combine(_webRoot, "Pics", "1.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });
    }

    private PicController CreateSut() =>
        new(_fixture.CreateContext(), new FakeWebHostEnvironment(_webRoot), NullLogger<PicController>.Instance);

    [Fact]
    public async Task Index_Returns_Png_File_For_Existing_Item()
    {
        var sut = CreateSut();

        var result = await sut.Index(1);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("image/png", file.ContentType);
        Assert.NotEmpty(file.FileContents);
    }

    [Fact]
    public async Task Index_NonPositive_Id_Returns_BadRequest()
    {
        var sut = CreateSut();

        Assert.IsType<BadRequestResult>(await sut.Index(0));
    }

    [Fact]
    public async Task Index_Unknown_Item_Returns_NotFound()
    {
        var sut = CreateSut();

        Assert.IsType<NotFoundResult>(await sut.Index(99999));
    }

    [Fact]
    public async Task Index_Missing_File_Returns_NotFound()
    {
        var sut = CreateSut();

        // Item 2 is seeded (PictureFileName "2.png") but no file was written.
        Assert.IsType<NotFoundResult>(await sut.Index(2));
    }

    public void Dispose()
    {
        _fixture.Dispose();
        if (Directory.Exists(_webRoot))
        {
            Directory.Delete(_webRoot, recursive: true);
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string webRootPath)
        {
            WebRootPath = webRootPath;
            WebRootFileProvider = new PhysicalFileProvider(webRootPath);
            ContentRootPath = webRootPath;
            ContentRootFileProvider = new PhysicalFileProvider(webRootPath);
        }

        public string WebRootPath { get; set; }
        public IFileProvider WebRootFileProvider { get; set; }
        public string ApplicationName { get; set; } = "Catalog.Mvc.Tests";
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
        public string EnvironmentName { get; set; } = "Testing";
    }
}
