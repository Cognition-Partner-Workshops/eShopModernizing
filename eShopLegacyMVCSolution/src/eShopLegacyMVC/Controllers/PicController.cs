using eShopLegacyMVC.Services;
using log4net;
using System;
using System.IO;
using System.Net;
using System.Web.Mvc;

namespace eShopLegacyMVC.Controllers
{
    public class PicController : Controller
    {
        private static readonly ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public const string GetPicRouteName = "GetPicRouteTemplate";

        private ICatalogService service;

        public PicController(ICatalogService service)
        {
            this.service = service;
        }

        // GET: Pic/5.png
        [HttpGet]
        [Route("items/{catalogItemId:int}/pic", Name = GetPicRouteName)]
        public ActionResult Index(int catalogItemId)
        {
            _log.Info($"Now loading... /items/Index?{catalogItemId}/pic");

            if (catalogItemId <= 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var item = service.FindCatalogItem(catalogItemId);

            if (item == null)
            {
                return HttpNotFound();
            }

            string fileName;
            string mimetype;
            if (!TryGetSafeFileName(item.PictureFileName, out fileName, out mimetype))
            {
                return HttpNotFound();
            }

            var webRoot = Path.GetFullPath(Server.MapPath("~/Pics"));
            var webRootPrefix = webRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(webRoot, fileName));

            if (!path.StartsWith(webRootPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return HttpNotFound();
            }

            if (!System.IO.File.Exists(path))
            {
                return HttpNotFound();
            }

            var buffer = System.IO.File.ReadAllBytes(path);

            Response.AddHeader("X-Content-Type-Options", "nosniff");

            return File(buffer, mimetype);
        }

        /// <summary>
        /// Accepts only a bare file name (no directory components, no invalid characters)
        /// with a whitelisted image extension. Returns the file name and its MIME type.
        /// </summary>
        internal static bool TryGetSafeFileName(string pictureFileName, out string fileName, out string mimetype)
        {
            fileName = null;
            mimetype = null;

            if (string.IsNullOrWhiteSpace(pictureFileName))
            {
                return false;
            }

            if (pictureFileName.IndexOf('/') >= 0 || pictureFileName.IndexOf('\\') >= 0)
            {
                return false;
            }

            if (pictureFileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            string candidate;
            try
            {
                candidate = Path.GetFileName(pictureFileName);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (!string.Equals(candidate, pictureFileName, StringComparison.Ordinal))
            {
                return false;
            }

            mimetype = GetImageMimeTypeFromImageFileExtension(Path.GetExtension(candidate));
            if (mimetype == null)
            {
                return false;
            }

            fileName = candidate;
            return true;
        }

        internal static string GetImageMimeTypeFromImageFileExtension(string extension)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return null;
            }

            switch (extension.ToLowerInvariant())
            {
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".bmp":
                    return "image/bmp";
                default:
                    return null;
            }
        }
    }
}
