using System;
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
        public IHttpActionResult GetAnnouncementForStudents(int studentId)
        {
            var studentExists = db.Students.Any(s=> s.StudentID == studentId);
            if(!studentExists) return NotFound();

            var now = DateTime.Now;

            var enrollments = db.Enrollments.Where(e => e.StudentID == studentId && e.IsActive).Select(e => new {e.CourseID, e.SubjectID}).ToList();

            var courseIds = enrollments.Select(e => e.CourseID).Distinct().ToList();
            var subjectIds = enrollments.Select(e => e.SubjectID).Distinct().ToList();

            var announcements = db.Announcements.Where(a => a.IsForAllStudents || (a.CourseID != null && courseIds.Contains(a.CourseID.Value) && (a.SubjectID == null || subjectIds.Contains(a.SubjectID.Value))))
                .Where(a => a.ExpiryDate == null || a.ExpiryDate > now).OrderByDescending(a => a.PostDate).Select(a => new
                {
                    a.AnnouncementID,
                    a.Title,
                    a.Content,
                    a.PostDate,
                    a.ExpiryDate,
                    a.IsImportant,
                    a.IsForAllStudents,
                    a.SubjectID,
                    SubjectName = db.Subjects.Where(s => s.SubjectID == a.SubjectID).Select(s => s.SubjectName).FirstOrDefault(),
                    Course = a.Course == null ? null : new
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
                        UserName = a.Teacher.User.Username
                    }
                })
                .ToList();

            return Ok(announcements);
        }

       //POST Announcement
       public IHttpActionResult PostAnnouncement(Announcement announcement)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            var targetError = NormalizeTarget(announcement);
            if(targetError != null)
                return BadRequest(targetError);

            db.Announcements.Add(announcement);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = announcement.AnnouncementID }, announcement);
        }

        //PUT Announcment
        public IHttpActionResult PutAnnoucement(int id, Announcement announcement)
        {
            if(!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != announcement.AnnouncementID)
                return BadRequest();

            var targetError = NormalizeTarget(announcement);
            if(targetError != null)
                return BadRequest(targetError);

            db.Entry(announcement).State = EntityState.Modified;
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        //A notice goes to all or a course
        private string NormalizeTarget(Announcement a)
        {
            if (a.IsForAllStudents)
            {
                a.CourseID = null;
                a.SubjectID = null;
                return null;
            }
            if (a.CourseID == null)
                return "Select a course, or send the notice to all students";
            return null;
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
