using System.Linq;
using System.Net;
using System.Web.Http;
using System.Data.Entity;
using ClassSystemAPI.Models;

namespace ClassSystemAPI.Controllers
{
    public class AnnouncementsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public AnnouncementsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }
        // GET: api/Announcements
        public IQueryable<Announcement> GetAnnouncements()
        {
            return db.Announcements.Include(a => a.Course).Include(a => a.Teacher);
        }

        // GET: api/Announcements/5
        public IHttpActionResult GetAnnouncement(int id)
        {
            var announcement = db.Announcements.Find(id);
            if (announcement == null)
                return NotFound();

            return Ok(announcement);
        }

        // GET: api/Announcements/Student/{studentId}
        // Returns announcements for all courses where the student is currently enrolled.
        [HttpGet]
        [Route("api/Announcements/Student/{studentId}")]
        public IHttpActionResult GetAnnouncementsForStudent(int studentId)
        {
            var studentExists = db.Students.Any(s => s.StudentID == studentId);
            if (!studentExists) return NotFound();

            var courseIds = db.Enrollments
                .Where(e => e.StudentID == studentId && e.IsActive)
                .Select(e => e.CourseID);

            var announcements = db.Announcements
                .Where(a => courseIds.Contains(a.CourseID))
                .Include(a => a.Course)
                .Include(a => a.Teacher.User)
                .OrderByDescending(a => a.PostDate)
                .Select(a => new
                {
                    a.AnnouncementID,
                    a.Title,
                    a.Content,
                    a.PostDate,
                    a.ExpiryDate,
                    a.IsImportant,
                    Course = new
                    {
                        a.Course.CourseID,
                        a.Course.CourseName,
                        a.Course.CourseCode
                    },
                    Teacher = a.Teacher == null ? null : new
                    {
                        a.Teacher.TeacherID,
                        FirstName = a.Teacher.User.FirstName,
                        LastName = a.Teacher.User.LastName,
                        Username = a.Teacher.User.Username
                    }
                })
                .ToList();

            return Ok(announcements);
        }

        // POST: api/Announcements
        public IHttpActionResult PostAnnouncement(Announcement announcement)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Announcements.Add(announcement);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = announcement.AnnouncementID }, announcement);
        }

        // PUT: api/Announcements/5
        public IHttpActionResult PutAnnouncement(int id, Announcement announcement)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != announcement.AnnouncementID)
                return BadRequest();

            db.Entry(announcement).State = EntityState.Modified;
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Announcements/5
        public IHttpActionResult DeleteAnnouncement(int id)
        {
            var announcement = db.Announcements.Find(id);
            if (announcement == null)
                return NotFound();

            db.Announcements.Remove(announcement);
            db.SaveChanges();

            return Ok(announcement);
        }
    }
}
