using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace eShopLegacyMVC.Services.Catalog
{
    /// <summary>Typed HttpClient for the Catalog API (registered via AddHttpClient in ApplicationModule).</summary>
    public class CatalogApiClient
    {
        private static readonly JsonSerializerOptions Json = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly HttpClient _http;

        public CatalogApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<PaginatedItemsDto<CatalogItemDto>> GetItemsAsync(int pageSize, int pageIndex, int? brandId = null, int? typeId = null)
        {
            var url = $"api/catalog/items?pageSize={pageSize}&pageIndex={pageIndex}";
            if (brandId.HasValue) url += $"&brandId={brandId.Value}";
            if (typeId.HasValue) url += $"&typeId={typeId.Value}";
            return await _http.GetFromJsonAsync<PaginatedItemsDto<CatalogItemDto>>(url, Json);
        }

        public async Task<CatalogItemDto> GetItemAsync(int id)
        {
            using var response = await _http.GetAsync($"api/catalog/items/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CatalogItemDto>(Json);
        }

        public Task<List<CatalogBrandDto>> GetBrandsAsync() =>
            _http.GetFromJsonAsync<List<CatalogBrandDto>>("api/catalog/brands", Json);

        public Task<List<CatalogTypeDto>> GetTypesAsync() =>
            _http.GetFromJsonAsync<List<CatalogTypeDto>>("api/catalog/types", Json);

        public async Task<CatalogItemDto> CreateItemAsync(CatalogItemWriteDto item)
        {
            using var response = await _http.PostAsJsonAsync("api/catalog/items", item, Json);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CatalogItemDto>(Json);
        }

        public async Task UpdateItemAsync(int id, CatalogItemWriteDto item)
        {
            using var response = await _http.PutAsJsonAsync($"api/catalog/items/{id}", item, Json);
            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteItemAsync(int id)
        {
            using var response = await _http.DeleteAsync($"api/catalog/items/{id}");
            if (response.StatusCode != HttpStatusCode.NotFound)
            {
                response.EnsureSuccessStatusCode();
            }
        }
    }
}
