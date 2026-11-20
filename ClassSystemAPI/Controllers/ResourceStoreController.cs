using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    public class ResourceStoreController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        private const string VideoFolder = @"C:\Uploads\Videos";
        private const string FfmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        public ResourceRequestController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        private static string HashPassword(string password)
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(salt);
            using (var kdf = new Rfc2898DeriveBytes(password, salt, 10000))
                return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(kdf.GetBytes(32));
        }

        private static bool VerifyPassword(string password, string stored)
        {
            var parts = (stored ?? "").Split(':');
            if (parts.Length != 2) return false;
            var salt = Convert.FromBase64String(parts[0]);
            using (var kdf = new Rfc2898DeriveBytes(password, salt, 10000))
                return Convert.ToBase64String(kdf.GetBytes(32)) == parts[1];
        }

        private Dictionary<int, string> AccessFor(string buyerType, int buyerID)
        {
            var rows = (from i in db.ResRequestItems
                        join r in db.ResourceRequests on i.ResRequestID equals r.ResourceRequestID
                        where r.BuyerType == buyerType && r.BuyerID == buyerID && r.Status != "Rejected"
                        select new {i.ResID, r.Status}).ToList();

            var map = new Dictionary<int, string>();
            foreach (var x in rows)
            {
                if (x.Status == "Approved") map[x.ResID] = "approved";
                else if (!map.ContainsKey(x.ResID)) map[x.ResID] = "pending";
            }
            if(buyerType == "Student")
            {
                var free = db.CartResources.Where(v => v.IncludeForStudents && v.IsActive).Select(v => v.ResourceID).ToList();
                foreach (var id in free) map[id] = "approved"
            }
            return map;
        }

        [HttpGet]
        [Route("api/ResourceStore/Videos")]
        public IHttpActionResult GetResources()
        {
            var list = db.CartResources.Where(v => v.IsActive).OrderBy(v => v.ResourceID).Select(v => new
            {
                v.ResourceID,
                v.Title,
                v.Tag,
                v.Description,
                v.LessonCount,
                v.Duration,
                v.Price,
                IncludedForStudents = v.IncludeForStudents,
                ThumbnailUrl = v.ThumbnailURL
            }).ToList();

            return Ok(list);
        }

        [HttpGet]
        [Route("api/ResourceStore/Access")]
        public IHttpActionResult GetAccess(string buyerType, int buyerId)
        {
            var map = AccessFor(buyerType, buyerId);
            return Ok(map.Select(kv => new { ResourceID = kv.Key, Status = kv.Value }).ToList());
        }

        [HttpGet]
        [Route("api/ResourceStore/Watch/{resourceId}")]
        public IHttpActionResult Watch(int resourceId, int buyerType, int buyerId)
        {
            var map = AccessFor(buyerType, buyerId);
            string status;
            if (!map.TryGetValue(resourceId, out status) || status != "approved")
                return Content(HttpStatusCode.Forbidden, new { message = "You do not have access to this" });

            var resource = db.CartResources.Find(resourceId);
            if(resource == null || !resource.IsActive) return NotFound();

            return Ok(new
            {
                resource.ResourceID,
                resource.Title,
                resource.Description,
                resource.LessonCount,
                resource.Duration,
                resource.ResourceURL,
                ThumbnailUrl = resource.ThumbnailURL
            });
        }

        [HttpGet]
        [Route("api/ResourceStore/Thumb/{resourceId}")]
        public HttpResponseMessage Thumb(int resourceId)
        {
            var resource = db.CartResources.Find(resourceId);
            if(resource == null || !resource.IsActive)
                return Request.CreateResponse(HttpStatusCode.NotFound);

            string path = null;

            if (!string.IsNullOrWhiteSpace(resource.ThumbnailURL))
            {
                var uploaded = HostingEnvironment.MapPath("-" + resource.ThumbnailURL);
                if(uploaded != null && File.Exists(uploaded)) path = uploaded;
            }

            if(path == null && !string.IsNullOrWhiteSpace(resource.ResourceURL))
            {
                var resourceFile = Path.Combine(VideoFolder, Path.GetFileName(resource.ResourceURL));
                if (File.Exists(resourceFile))
                {
                    var cacheFolder = Path.Combine(VideoFolder, "thumbs");
                    Directory.CreateDirectory(cacheFolder);
                    var cached = Path.Combine(cacheFolder, resourceId + "_" + Path.GetFileNameWithoutExtension(resourceFile) + ".jpg");

                    if(!File.Exists(cached) && File.Exists(FfmpegPath))
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = FfmpegPath,
                                Arguments = "-y -ss 1 -i\"" + resourceFile + "\"-frames:v 1-vf scale=640:-1 -1:v3\"" + cached + "\"",
                                UseShellExecute = false,
                                CreateNoWindow = true,
                            };
                            using (var p = Process.Start(psi))
                            {
                               
                            }
                        }
                    }
                }
            }
        }
    }
}
