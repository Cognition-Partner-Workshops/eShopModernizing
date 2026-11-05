using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace eShopModernizedWebForms.Tests
{
    public sealed class WebFormsAppFactory : WebApplicationFactory<Program>
    {
        private readonly Dictionary<string, string> settings;

        public WebFormsAppFactory(Dictionary<string, string> settings = null)
        {
            this.settings = settings ?? new Dictionary<string, string> { ["UseMockData"] = "true" };
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            foreach (var setting in settings)
            {
                builder.UseSetting(setting.Key, setting.Value);
            }
        }

        public HttpClient CreateBrowser() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    internal static class FormPost
    {
        private static readonly Regex TokenRegex = new("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");

        public static async Task<HttpResponseMessage> SubmitAsync(HttpClient client, string url, IDictionary<string, string> fields)
        {
            var page = await client.GetStringAsync(new Uri(url, UriKind.Relative));
            var values = new Dictionary<string, string>(fields)
            {
                ["__RequestVerificationToken"] = TokenRegex.Match(page).Groups[1].Value,
            };
            using var content = new FormUrlEncodedContent(values);
            return await client.PostAsync(new Uri(url, UriKind.Relative), content);
        }

        public static Dictionary<string, string> ValidItem(string name) => new()
        {
            ["Name"] = name,
            ["Description"] = name + " description",
            ["Brand"] = "3",
            ["Type"] = "4",
            ["Price"] = "21.75",
            ["Stock"] = "7",
            ["Restock"] = "2",
            ["Maxstock"] = "40",
        };

        public static void AssertRedirectToRoot(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/", response.Headers.Location?.OriginalString);
        }
    }
}
