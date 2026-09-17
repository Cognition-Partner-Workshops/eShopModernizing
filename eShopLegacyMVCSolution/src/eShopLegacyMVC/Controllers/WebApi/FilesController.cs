using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http;

namespace eShopLegacyMVC.Controllers.WebApi
{
    public class FilesController : ApiController
    {
        private ICatalogService _service;

        public FilesController(ICatalogService service)
        {
            _service = service;
        }

        // GET api/<controller>
        public HttpResponseMessage Get()
        {
            var brands = _service.GetCatalogBrands()
                .Select(b => new BrandDTO
                {
                    Id = b.Id,
                    Brand = b.Brand
                }).ToList();
            var serializer = new Serializing();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(serializer.SerializeJson(brands))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return response;
        }

        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}