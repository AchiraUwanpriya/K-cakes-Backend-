using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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

        // Resource files live here, outside the website folder (the IIS app pool user needs Read access)
        private const string ResourceFolder = @"C:\Uploads\Resources";
        private const string FfmpegPath = @"C:\Tools\ffmpeg\bin\ffmpeg.exe";

        public ResourceStoreController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // ---------- helpers ----------
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

        // ResourceID -> "pending" | "approved" for one buyer
        private Dictionary<int, string> AccessFor(string buyerType, int buyerId)
        {
            var rows = (from i in db.ResRequestItems
                        join r in db.ResourceRequests on i.ResRequestID equals r.ResourceRequestID
                        where r.BuyerType == buyerType && r.BuyerID == buyerId && r.Status != "Rejected"
                        select new { i.ResID, r.Status }).ToList();

            var map = new Dictionary<int, string>();
            foreach (var x in rows)
            {
                if (x.Status == "Approved") map[x.ResID] = "approved";
                else if (!map.ContainsKey(x.ResID)) map[x.ResID] = "pending";
            }

            if (buyerType == "Student")
            {
                var free = db.CartResources.Where(v => v.IncludeForStudents && v.IsActive).Select(v => v.ResourceID).ToList();
                foreach (var id in free) map[id] = "approved";
            }
            return map;
        }

        // ---------- resources ----------
        [HttpGet]
        [Route("api/ResourceStore/Resources")]
        public IHttpActionResult GetResources()
        {
            var list = db.CartResources.Where(v => v.IsActive).OrderBy(v => v.ResourceID)
                .Select(v => new
                {
                    v.ResourceID,
                    v.Title,
                    v.Tag,
                    v.Description,
                    v.LessonCount,
                    v.Duration,
                    v.Price,
                    v.IncludeForStudents,
                    v.ThumbnailURL
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

        // Only returns the resource details when the buyer has an approved purchase
        [HttpGet]
        [Route("api/ResourceStore/Watch/{resourceId}")]
        public IHttpActionResult Watch(int resourceId, string buyerType, int buyerId)
        {
            var map = AccessFor(buyerType, buyerId);
            string status;
            if (!map.TryGetValue(resourceId, out status) || status != "approved")
                return Content(HttpStatusCode.Forbidden, new { message = "You do not have access to this resource." });

            var resource = db.CartResources.Find(resourceId);
            if (resource == null || !resource.IsActive) return NotFound();

            return Ok(new
            {
                resource.ResourceID,
                resource.Title,
                resource.Description,
                resource.LessonCount,
                resource.Duration,
                resource.ResourceURL,
                resource.ThumbnailURL
            });
        }

        // Public thumbnail: the uploaded one if there is one, otherwise the first frame of the file
        [HttpGet]
        [Route("api/ResourceStore/Thumb/{resourceId}")]
        public HttpResponseMessage Thumb(int resourceId)
        {
            var resource = db.CartResources.Find(resourceId);
            if (resource == null || !resource.IsActive)
                return Request.CreateResponse(HttpStatusCode.NotFound);

            string path = null;

            // 1. uploaded thumbnail
            if (!string.IsNullOrWhiteSpace(resource.ThumbnailURL))
            {
                var uploaded = HostingEnvironment.MapPath("~" + resource.ThumbnailURL);
                if (uploaded != null && File.Exists(uploaded)) path = uploaded;
            }

            // 2. frame taken from the file (made once, then reused)
            if (path == null && !string.IsNullOrWhiteSpace(resource.ResourceURL))
            {
                var resourceFile = Path.Combine(ResourceFolder, Path.GetFileName(resource.ResourceURL));
                if (File.Exists(resourceFile))
                {
                    var cacheFolder = Path.Combine(ResourceFolder, "thumbs");
                    Directory.CreateDirectory(cacheFolder);
                    var cached = Path.Combine(cacheFolder,
                        resourceId + "_" + Path.GetFileNameWithoutExtension(resourceFile) + ".jpg");

                    if (!File.Exists(cached) && File.Exists(FfmpegPath))
                    {
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = FfmpegPath,
                                Arguments = "-y -ss 1 -i \"" + resourceFile + "\" -frames:v 1 -vf scale=640:-1 -q:v 3 \"" + cached + "\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using (var p = Process.Start(psi))
                            {
                                if (!p.WaitForExit(20000)) { try { p.Kill(); } catch { } }
                            }
                        }
                        catch { }
                    }
                    if (File.Exists(cached)) path = cached;
                }
            }

            if (path == null)
                return Request.CreateResponse(HttpStatusCode.NotFound);

            var ext = Path.GetExtension(path).ToLower();
            var mime = ext == ".png" ? "image/png" : "image/jpeg";

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
            response.Headers.CacheControl = new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromHours(1) };
            return response;
        }

        // Streams the resource file, but only for buyers with an approved purchase
        [HttpGet]
        [Route("api/ResourceStore/Stream/{resourceId}")]
        public HttpResponseMessage Stream(int resourceId, string buyerType, int buyerId)
        {
            var map = AccessFor(buyerType, buyerId);
            string status;
            if (!map.TryGetValue(resourceId, out status) || status != "approved")
                return Request.CreateResponse(HttpStatusCode.Forbidden);

            var resource = db.CartResources.Find(resourceId);
            if (resource == null || string.IsNullOrWhiteSpace(resource.ResourceURL))
                return Request.CreateResponse(HttpStatusCode.NotFound);

            // Path.GetFileName stops anyone using ..\ tricks to reach other files
            var path = Path.Combine(ResourceFolder, Path.GetFileName(resource.ResourceURL));
            if (!File.Exists(path))
                return Request.CreateResponse(HttpStatusCode.NotFound);

            var ext = Path.GetExtension(path).ToLower();
            var mime = ext == ".mp4" ? "video/mp4" : ext == ".webm" ? "video/webm" : "application/octet-stream";

            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var response = new HttpResponseMessage();
            try
            {
                if (Request.Headers.Range != null)
                {
                    response.StatusCode = HttpStatusCode.PartialContent;
                    response.Content = new ByteRangeStreamContent(stream, Request.Headers.Range, mime);
                }
                else
                {
                    response.Content = new StreamContent(stream);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);
                }
            }
            catch (InvalidByteRangeException ex)
            {
                stream.Dispose();
                return Request.CreateErrorResponse(HttpStatusCode.RequestedRangeNotSatisfiable, ex);
            }
            response.Headers.AcceptRanges.Add("bytes");
            return response;
        }

        // ---------- admin: manage resources ----------
        private void ClearCachedThumbs(int resourceId)
        {
            try
            {
                var cacheFolder = Path.Combine(ResourceFolder, "thumbs");
                if (!Directory.Exists(cacheFolder)) return;
                foreach (var f in Directory.GetFiles(cacheFolder, resourceId + "_*.jpg")) File.Delete(f);
            }
            catch { }
        }

        private void DeleteUploadedThumb(CartResource item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.ThumbnailURL)) return;
                var old = HostingEnvironment.MapPath("~" + item.ThumbnailURL);
                if (old != null && File.Exists(old)) File.Delete(old);
            }
            catch { }
        }

        // Reads the form + files sent by the admin page. Returns an error message, or null when saved into 'item'.
        private string ApplyForm(CartResource item, bool isNew)
        {
            var req = HttpContext.Current.Request;

            var title = (req.Form["Title"] ?? "").Trim();
            if (title == "") return "Title is required.";

            decimal price;
            if (!decimal.TryParse(req.Form["Price"], NumberStyles.Any, CultureInfo.InvariantCulture, out price) || price < 0)
                return "Enter a valid price.";

            int lessons;
            int.TryParse(req.Form["LessonCount"], out lessons);

            var file = req.Files["File"];
            var hasFile = file != null && file.ContentLength > 0;
            if (isNew && !hasFile) return "Please choose the resource file.";

            string fileExt = null;
            if (hasFile)
            {
                fileExt = Path.GetExtension(file.FileName).ToLower();
                if (fileExt != ".mp4" && fileExt != ".webm") return "Only MP4 or WebM files are allowed.";
            }

            var thumb = req.Files["Thumbnail"];
            var hasThumb = thumb != null && thumb.ContentLength > 0;
            string thumbExt = null;
            if (hasThumb)
            {
                thumbExt = Path.GetExtension(thumb.FileName).ToLower();
                if (thumbExt != ".jpg" && thumbExt != ".jpeg" && thumbExt != ".png")
                    return "Thumbnail must be a JPG or PNG image.";
                if (thumb.ContentLength > 5 * 1024 * 1024)
                    return "Thumbnail size exceeds limit (5 MB).";
            }

            // all checks passed, now save
            item.Title = title;
            item.Tag = (req.Form["Tag"] ?? "").Trim();
            item.Description = (req.Form["Description"] ?? "").Trim();
            item.LessonCount = lessons;
            item.Duration = (req.Form["Duration"] ?? "").Trim();
            item.Price = price;
            item.IncludeForStudents = req.Form["IncludeForStudents"] == "true";
            item.IsActive = req.Form["IsActive"] == "true";

            if (hasFile)
            {
                Directory.CreateDirectory(ResourceFolder);
                var name = Guid.NewGuid().ToString("N") + fileExt;
                file.SaveAs(Path.Combine(ResourceFolder, name));

                if (!isNew && !string.IsNullOrWhiteSpace(item.ResourceURL))
                {
                    var old = Path.Combine(ResourceFolder, Path.GetFileName(item.ResourceURL));
                    try { if (File.Exists(old)) File.Delete(old); } catch { }
                    ClearCachedThumbs(item.ResourceID);
                }
                item.ResourceURL = name;
            }

            if (hasThumb)
            {
                var folder = HostingEnvironment.MapPath("~/Uploads/ResourceThumbnails");
                Directory.CreateDirectory(folder);
                var tname = Guid.NewGuid().ToString("N") + thumbExt;
                thumb.SaveAs(Path.Combine(folder, tname));

                DeleteUploadedThumb(item);
                item.ThumbnailURL = "/Uploads/ResourceThumbnails/" + tname;
            }
            else if (req.Form["RemoveThumbnail"] == "true")
            {
                DeleteUploadedThumb(item);
                item.ThumbnailURL = null;
            }

            return null;
        }

        // all resources, including hidden ones
        [HttpGet]
        [Route("api/ResourceStore/Resources/All")]
        public IHttpActionResult GetAllResources()
        {
            return Ok(db.CartResources.OrderBy(v => v.ResourceID).ToList());
        }

        [HttpPost]
        [Route("api/ResourceStore/Resources")]
        public IHttpActionResult CreateResource()
        {
            var item = new CartResource { CreatedDate = DateTime.Now };
            var error = ApplyForm(item, true);
            if (error != null) return BadRequest(error);

            db.CartResources.Add(item);
            db.SaveChanges();
            return Ok(new { item.ResourceID });
        }

        [HttpPost]
        [Route("api/ResourceStore/Resources/{id:int}/Update")]
        public IHttpActionResult UpdateResource(int id)
        {
            var item = db.CartResources.Find(id);
            if (item == null) return NotFound();

            var error = ApplyForm(item, false);
            if (error != null) return BadRequest(error);

            db.SaveChanges();
            return Ok(new { item.ResourceID });
        }

        // ---------- visitors ----------
        [HttpPost]
        [Route("api/ResourceStore/Visitors/Register")]
        public IHttpActionResult RegisterVisitor(VisitorRegisterDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email)
                || string.IsNullOrWhiteSpace(dto.Phone) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Please fill in all fields.");
            if (dto.Password.Length < 6)
                return BadRequest("Password must be at least 6 characters.");

            var email = dto.Email.Trim().ToLower();
            if (db.Visitors.Any(x => x.Email == email))
                return BadRequest("An account with this email already exists. Please log in.");

            var visitor = new Visitor
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                Phone = dto.Phone.Trim(),
                PasswordHash = HashPassword(dto.Password),
                CreatedDate = DateTime.Now
            };
            db.Visitors.Add(visitor);
            db.SaveChanges();

            return Ok(new { visitor.VisitorID, visitor.FullName, visitor.Email, visitor.Phone });
        }

        [HttpPost]
        [Route("api/ResourceStore/Visitors/Login")]
        public IHttpActionResult LoginVisitor(VisitorLoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrEmpty(dto.Password))
                return BadRequest("Enter your email and password.");

            var email = dto.Email.Trim().ToLower();
            var visitor = db.Visitors.FirstOrDefault(x => x.Email == email);
            if (visitor == null || !VerifyPassword(dto.Password, visitor.PasswordHash))
                return BadRequest("Wrong email or password.");

            return Ok(new { visitor.VisitorID, visitor.FullName, visitor.Email, visitor.Phone });
        }

        // ---------- requests ----------
        [HttpPost]
        [Route("api/ResourceStore/Requests")]
        public IHttpActionResult CreateRequest(ResourceRequestCreateDto dto)
        {
            if (dto == null || dto.ResourceIDs == null || dto.ResourceIDs.Count == 0)
                return BadRequest("Select at least one resource.");

            string name, phone, email;
            if (dto.BuyerType == "Student")
            {
                var u = db.Users.FirstOrDefault(x => x.UserID == dto.BuyerID && x.IsActive);
                if (u == null) return BadRequest("Student not found.");
                name = u.FirstName + " " + u.LastName;
                phone = u.HED_MOBILE_NO;
                email = u.Email;
            }
            else if (dto.BuyerType == "Visitor")
            {
                var visitorRow = db.Visitors.Find(dto.BuyerID);
                if (visitorRow == null) return BadRequest("Visitor not found.");
                name = visitorRow.FullName;
                phone = visitorRow.Phone;
                email = visitorRow.Email;
            }
            else return BadRequest("Invalid buyer.");

            // skip resources already owned or already waiting for approval
            var owned = AccessFor(dto.BuyerType, dto.BuyerID);
            var ids = dto.ResourceIDs.Distinct().Where(id => !owned.ContainsKey(id)).ToList();
            var resources = db.CartResources.Where(x => ids.Contains(x.ResourceID) && x.IsActive).ToList();
            if (resources.Count == 0)
                return BadRequest("These resources are already purchased or waiting for approval.");

            var request = new ResourceRequest
            {
                BuyerType = dto.BuyerType,
                BuyerID = dto.BuyerID,
                BuyerName = name,
                Phone = phone,
                Email = email,
                TotalAmount = resources.Sum(x => x.Price),   // price always comes from the database
                Status = "Pending",
                CreatedDate = DateTime.Now
            };
            db.ResourceRequests.Add(request);
            db.SaveChanges();

            foreach (var resource in resources)
            {
                db.ResRequestItems.Add(new ResRequestItem
                {
                    ResRequestID = request.ResourceRequestID,
                    ResID = resource.ResourceID,
                    Price = resource.Price
                });
            }
            db.SaveChanges();

            return Ok(new { request.ResourceRequestID, request.TotalAmount, request.Status });
        }

        [HttpPost]
        [Route("api/ResourceStore/Requests/{id}/Slip")]
        public IHttpActionResult UploadSlip(int id)
        {
            var request = db.ResourceRequests.Find(id);
            if (request == null) return NotFound();
            if (request.Status != "Pending") return BadRequest("This request is already closed.");

            var files = HttpContext.Current.Request.Files;
            if (files.Count == 0) return BadRequest("No file was sent.");

            var file = files[0];
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".pdf")
                return BadRequest("Only JPG, PNG or PDF files are allowed.");
            if (file.ContentLength > 5 * 1024 * 1024)
                return BadRequest("File size exceeds limit (5 MB).");

            var folder = HostingEnvironment.MapPath("~/Uploads/ResourceSlips");
            Directory.CreateDirectory(folder);
            var fileName = Guid.NewGuid().ToString("N") + ext;
            file.SaveAs(Path.Combine(folder, fileName));

            request.SlipPath = "/Uploads/ResourceSlips/" + fileName;
            db.SaveChanges();

            return Ok(new { slipPath = request.SlipPath });
        }

        // admin list
        [HttpGet]
        [Route("api/ResourceStore/Requests")]
        public IHttpActionResult GetRequests()
        {
            var requests = db.ResourceRequests.OrderByDescending(r => r.CreatedDate).ToList();
            var items = (from i in db.ResRequestItems
                         join v in db.CartResources on i.ResID equals v.ResourceID
                         select new { i.ResRequestID, v.ResourceID, v.Title, i.Price }).ToList();

            var result = requests.Select(r => new
            {
                r.ResourceRequestID,
                r.BuyerType,
                r.BuyerID,
                r.BuyerName,
                r.Phone,
                r.Email,
                r.TotalAmount,
                r.SlipPath,
                r.Status,
                r.CreatedDate,
                Items = items.Where(i => i.ResRequestID == r.ResourceRequestID)
                             .Select(i => new { i.ResourceID, i.Title, i.Price }).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpPut]
        [Route("api/ResourceStore/Requests/{id}/Decision")]
        public IHttpActionResult Decide(int id, ResourceDecisionDto dto)
        {
            if (dto == null || (dto.Status != "Approved" && dto.Status != "Rejected"))
                return BadRequest("Status must be Approved or Rejected.");

            var request = db.ResourceRequests.Find(id);
            if (request == null) return NotFound();

            request.Status = dto.Status;
            request.DecidedDate = DateTime.Now;
            db.SaveChanges();
            return Ok(new { request.ResourceRequestID, request.Status });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}