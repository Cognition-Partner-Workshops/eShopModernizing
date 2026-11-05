using Google.Protobuf.Reflection;
using Xunit;
using Local = eShopModernizedWinForms.Grpc.Protos;
using Reference = eShop.Catalog.Grpc.Protos;

namespace eShopWinForms.Tests;

public sealed class ContractTests
{
    [Fact]
    public void DescriptorMatchesPinnedPr226WireContract()
    {
        var expected = Reference.CatalogReflection.Descriptor;
        var actual = Local.CatalogReflection.Descriptor;
        Assert.Equal("eshop.catalog.v1", actual.Package);
        Assert.Equal(expected.Package, actual.Package);
        Assert.Equal(expected.Services.Select(ServiceShape), actual.Services.Select(ServiceShape));
        Assert.Equal(expected.MessageTypes.Select(MessageShape), actual.MessageTypes.Select(MessageShape));
        Assert.Equal(10, actual.Services.Single().Methods.Count);
    }

    private static string ServiceShape(ServiceDescriptor service) =>
        service.FullName + ":" + string.Join(";", service.Methods.Select(m =>
            $"{m.Name}/{m.InputType.FullName}/{m.OutputType.FullName}/{m.IsClientStreaming}/{m.IsServerStreaming}"));

    private static string MessageShape(MessageDescriptor message) =>
        message.FullName + ":" + string.Join(";", message.Fields.InFieldNumberOrder().Select(f =>
            $"{f.Name}/{f.FieldNumber}/{f.FieldType}/{f.IsRepeated}/{(f.FieldType == FieldType.Message ? f.MessageType.FullName : "")}"));
}
