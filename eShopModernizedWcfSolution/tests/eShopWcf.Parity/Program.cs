using eShop.Catalog.Grpc.Protos;
using eShopWcf.Parity;
using Grpc.Net.Client;

namespace eShopWcf.Parity;

internal static class ParityProgram
{
    private static async Task<int> Main(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Length; i += 2)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
            {
                Console.Error.WriteLine("Usage: --legacy-url URL --modern-url URL --output DIR --phase full|reads-after-restart");
                return 2;
            }

            options.Add(args[i], args[i + 1]);
        }

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        using var channel = GrpcChannel.ForAddress(options.GetValueOrDefault("--modern-url", "http://localhost:5200"));
        var legacy = new SoapClient(http, new Uri(options.GetValueOrDefault("--legacy-url", "http://localhost:5100/CatalogService.svc")));
        var modern = new ModernClient(new Catalog.CatalogClient(channel));
        var run = new ParityRun(legacy, modern, options.GetValueOrDefault("--output", "artifacts"));
        return await run.ExecuteAsync(options.GetValueOrDefault("--phase", "full")).ConfigureAwait(false);
    }
}
