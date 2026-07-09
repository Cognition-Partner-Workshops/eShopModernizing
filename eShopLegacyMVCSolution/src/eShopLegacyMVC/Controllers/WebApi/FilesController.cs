using eShopLegacy.Utilities;
using eShopLegacyMVC.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Hosting;
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
                Content = new StreamContent(serializer.SerializeBinary(brands))
            };

            return response;
        }

        // GET api/files/download?fileName=web.config
        [HttpGet]
        [Route("api/files/download")]
        public HttpResponseMessage Download(string fileName)
        {
            var basePath = HostingEnvironment.MapPath("~/");
            var fullPath = Path.Combine(basePath, fileName);
            var bytes = File.ReadAllBytes(fullPath);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(bytes)
            };
        }

        // POST api/files/import
        [HttpPost]
        [Route("api/files/import")]
        public IHttpActionResult Import()
        {
            var serializer = new Serializing();
            var bytes = Request.Content.ReadAsByteArrayAsync().Result;

            using (var stream = new MemoryStream(bytes))
            {
                var result = serializer.DeserializeBinary(stream);
                return Ok(result);
            }
        }

        [Serializable]
        public class BrandDTO
        {
            public int Id { get; set; }
            public string Brand { get; set; }
        }
    }
}