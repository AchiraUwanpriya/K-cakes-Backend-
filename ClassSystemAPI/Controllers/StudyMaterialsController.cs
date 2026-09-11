using ClassSystemAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using ClassSystemAPI.Models;

namespace ClassSystemAPI.Controllers
{
    [RoutePrefix("api/studymaterials")]
    public class StudyMaterialsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public StudyMaterialsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }


        // GET: api/studymaterials
        //[HttpGet]
        //[Route("")]
        //public IHttpActionResult GetAllMaterials()
        //{
        //    var materials = db.StudyMaterials
        //        .Include(m => m.Course)
        //        .Include(m => m.Teacher)
        //        .Where(m => m.IsVisible)
        //        .ToList();

        //    return Ok(materials);
        //}

        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllMaterials()
        {
            var result = (from a in db.StudyMaterials
                          join b in db.Users on a.TeacherID equals b.UserID
                          join c in db.Courses on a.CourseID equals c.CourseID
                          join e in db.Subjects on a.SubjectID equals e.SubjectID
                          where a.IsVisible == true
                          select new
                          {
                              a.MaterialID,
                              a.TeacherID,
                              b.FirstName,
                              b.LastName,
                              a.CourseID,
                              c.CourseName,
                              a.SubjectID,
                              e.SubjectName,
                              a.Title,
                              a.Description,
                              a.FilePath,
                              a.FileType,
                              a.UploadDate,
                              a.IsVisible
                          }).ToList();

            return Ok(result);
        }

        [HttpGet]
        [Route("GetMatbyTeacherid")]
        public IHttpActionResult GetMatbyTeacherid(int teacherID)
        {
            var result = (from a in db.StudyMaterials
                          join b in db.Users on a.TeacherID equals b.UserID
                          join c in db.Courses on a.CourseID equals c.CourseID
                          join e in db.Subjects on a.SubjectID equals e.SubjectID
                          where a.TeacherID == teacherID && a.IsVisible == true
                          select new
                          {
                              a.MaterialID,
                              a.TeacherID,
                              b.FirstName,
                              b.LastName,
                              a.CourseID,
                              c.CourseName,
                              a.SubjectID,
                              e.SubjectName,
                              a.Title,
                              a.Description,
                              a.FilePath,
                              a.FileType,
                              a.UploadDate,
                              a.IsVisible
                          }).ToList();

            return Ok(result);
        }

        [HttpGet]
        [Route("GetMatbyCourseid")]
        public IHttpActionResult GetMatbyTeacherid(int courseid , int subjectId)
        {
            var result = (from a in db.StudyMaterials
                          join b in db.Users on a.TeacherID equals b.UserID
                          join c in db.Courses on a.CourseID equals c.CourseID
                          join e in db.Subjects on a.SubjectID equals e.SubjectID
                          where a.CourseID == courseid && a.SubjectID == subjectId && a.IsVisible == true
                          select new
                          {
                              a.MaterialID,
                              a.TeacherID,
                              b.FirstName,
                              b.LastName,
                              a.CourseID,
                              c.CourseName,
                              a.SubjectID,
                              e.SubjectName,
                              a.Title,
                              a.Description,
                              a.FilePath,
                              a.FileType,
                              a.UploadDate,
                              a.IsVisible
                          }).ToList();

            return Ok(result);
        }


        // GET: api/studymaterials/course/5
        [HttpGet]
        [Route("course/{courseId}")]
        public IHttpActionResult GetMaterialsByCourse(int courseId)
        {
            var materials = db.StudyMaterials
                .Include(m => m.Course)
                .Include(m => m.Teacher)
                .Where(m => m.CourseID == courseId && m.IsVisible)
                .ToList();

            if (!materials.Any())
            {
                return NotFound();
            }

            return Ok(materials);
        }

        // NEW: api/studymaterials/student/{studentId}
        [HttpGet]
        [Route("student/{studentId}")]
        public IHttpActionResult GetMaterialsForStudent(int studentId)
        {
            var studentExists = db.Students.Any(s => s.StudentID == studentId);
            if (!studentExists) return NotFound();

            var courseIds = db.Enrollments
                .Where(e => e.StudentID == studentId && e.IsActive)
                .Select(e => e.CourseID);

            var materials = db.StudyMaterials
                .Where(m => m.IsVisible && courseIds.Contains(m.CourseID))
                .Include(m => m.Course)
                .Include(m => m.Teacher.User)
                .OrderByDescending(m => m.UploadDate)
                .Select(m => new
                {
                    m.MaterialID,
                    m.Title,
                    m.Description,
                    m.FilePath,
                    m.FileType,
                    m.UploadDate,
                    Course = new
                    {
                        m.Course.CourseID,
                        m.Course.CourseName,
                        m.Course.CourseCode
                    },
                    Teacher = m.Teacher == null ? null : new
                    {
                        m.Teacher.TeacherID,
                        FirstName = m.Teacher.User.FirstName,
                        LastName = m.Teacher.User.LastName,
                        Username = m.Teacher.User.Username
                    }
                })
                .ToList();

            return Ok(materials);
        }

        // GET: api/studymaterials/5
        // Named route so CreatedAtRoute in POST can resolve
        [HttpGet]
        [Route("{id}", Name = "GetStudyMaterialById")]
        public IHttpActionResult GetMaterial(int id)
        {
            // Return material details regardless of IsVisible flag.
            var material = db.StudyMaterials
                .Include(m => m.Course)
                .Include(m => m.Teacher)
                .FirstOrDefault(m => m.MaterialID == id);

            if (material == null)
            {
                return NotFound();
            }

            return Ok(material);
        }


        //[HttpGet]
        //[Route("{id}", Name = "GetStudyMaterialById")]
        //public async Task<IHttpActionResult> GetStudyMaterial(int id)
        //{
        //    var material = await db.StudyMaterials.FindAsync(id);
        //    if (material == null) return NotFound();
        //    return Ok(material);
        //}


        // POST: api/studymaterials
        [HttpPost]
        [Route("PostStudyMaterial")]
        public async Task<IHttpActionResult> PostStudyMaterial()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequest("Request must be multipart/form-data");
            }

            try
            {
                var provider = new MultipartFormDataStreamProvider(Path.GetTempPath());
                await Request.Content.ReadAsMultipartAsync(provider);

                var material = new StudyMaterial
                {
                    CourseID = Convert.ToInt32(provider.FormData["CourseID"]),
                    TeacherID = Convert.ToInt32(provider.FormData["TeacherID"]),
                    SubjectID = Convert.ToInt32(provider.FormData["SubjectID"]),
                    Title = provider.FormData["Title"],
                    Description = provider.FormData["Description"] ?? string.Empty,
                    UploadDate = DateTime.Now,
                    IsVisible = true
                };

                var teacher = await db.Teachers.FindAsync(material.TeacherID);
                if (teacher == null) return BadRequest("Invalid Teacher ID");

                var course = await db.Courses.FindAsync(material.CourseID);
                if (course == null) return BadRequest("Invalid Course ID");

                var subject = await db.Subjects.FindAsync(material.SubjectID);
                if (subject == null) return BadRequest("Invalid Subject ID");

                var file = provider.FileData.FirstOrDefault();
                if (file != null && new FileInfo(file.LocalFileName).Length > 0)
                {
                    var originalFileName = file.Headers.ContentDisposition.FileName.Trim('"');
                    var fileExtension = Path.GetExtension(originalFileName);

                    var tempUploadPath = Path.Combine(@"C:\Uploads\StudyMaterials",
                                                     material.TeacherID.ToString(),
                                                     $"{material.CourseID}_{material.SubjectID}");

                    if (!Directory.Exists(tempUploadPath))
                        Directory.CreateDirectory(tempUploadPath);

                    var tempFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var tempFilePath = Path.Combine(tempUploadPath, tempFileName);
                    File.Move(file.LocalFileName, tempFilePath);
                    material.FilePath = tempFilePath;
                    material.FileType = fileExtension.Replace(".", "").ToLower();

                    db.StudyMaterials.Add(material);
                    await db.SaveChangesAsync();

                    var finalFileName = $"({material.MaterialID})_{SanitizeFileName(material.Title)}{fileExtension}";
                    var finalFilePath = Path.Combine(tempUploadPath, finalFileName);

                    File.Move(tempFilePath, finalFilePath);
                    material.FilePath = finalFilePath;
                    await db.SaveChangesAsync();
                }
                else
                {
                    return BadRequest("File is required");
                }

                return CreatedAtRoute("GetStudyMaterialById", new { id = material.MaterialID }, material);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "file";

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            if (sanitized.Length > 100)
                sanitized = sanitized.Substring(0, 100);

            return sanitized;
        }


        [HttpGet]
        [Route("download/{id}")]
        public IHttpActionResult DownloadMaterial(int id)
        {
            // Find the material by ID
            var material = db.StudyMaterials.FirstOrDefault(m => m.MaterialID == id);
            if (material == null)
                return NotFound();

            if (string.IsNullOrEmpty(material.FilePath))
                return BadRequest("File path not found.");

            // Ensure the path is absolute
            string fullPath = material.FilePath;
            if (!Path.IsPathRooted(fullPath))
                fullPath = HttpContext.Current.Server.MapPath(fullPath);

            if (!File.Exists(fullPath))
                return BadRequest("File does not exist on server.");

            // Read the file bytes
            byte[] fileBytes = File.ReadAllBytes(fullPath);
            string fileName = Path.GetFileName(fullPath);

            // Create response
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(fileBytes)
            };

            response.Content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName
                };

            response.Content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            return ResponseMessage(response);
        }








        // PUT: api/studymaterials/5
        [HttpPut]
        [Route("{id}")]
        public async Task<IHttpActionResult> PutStudyMaterial(int id, StudyMaterial material)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != material.MaterialID)
            {
                return BadRequest();
            }

            db.Entry(material).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception)
            {
                if (!StudyMaterialExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }




        // DELETE: api/studymaterials/5
        [HttpDelete]
        [Route("{id}")]
        public async Task<IHttpActionResult> DeleteStudyMaterial(int id)
        {
            var material = await db.StudyMaterials.FindAsync(id);
            if (material == null)
            {
                return NotFound();
            }

            // Perform permanent delete (hard delete)
            db.StudyMaterials.Remove(material);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // If DB constraint prevents delete, return a helpful error.
                return InternalServerError(ex);
            }

            return Ok(new { Message = "Study material permanently deleted", materialId = id });
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        private bool StudyMaterialExists(int id)
        {
            return db.StudyMaterials.Count(e => e.MaterialID == id) > 0;
        }
    }
}