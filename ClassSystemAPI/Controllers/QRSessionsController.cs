using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Net;
using System.Web.Http;
using ClassSystemAPI.Models;

namespace ClassSystemAPI.Controllers
{
    public class QRSessionsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public QRSessionsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/QRSessions
        public IHttpActionResult GetQRSessions()
        {
            var sessions = db.QRSessions
                .Include(q => q.Course)
                .Include(q => q.Teacher)
                .ToList();
            return Ok(sessions);
        }

        // GET: api/QRSessions/5
        public IHttpActionResult GetQRSession(int id)
        {
            var qrSession = db.QRSessions
                .Include(q => q.Course)
                .Include(q => q.Teacher)
                .FirstOrDefault(q => q.SessionID == id);

            if (qrSession == null)
            {
                return NotFound();
            }

            return Ok(qrSession);
        }

        // POST: api/QRSessions
        [HttpPost]
        public IHttpActionResult PostQRSession(QRSession qrSession)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate relationships exist
                if (!db.Courses.Any(c => c.CourseID == qrSession.CourseID))
                    return BadRequest("Invalid CourseID specified");

                if (!db.Teachers.Any(t => t.TeacherID == qrSession.TeacherID))
                    return BadRequest("Invalid TeacherID specified");

                // Validate dates
                if (qrSession.EndTime <= qrSession.StartTime)
                    return BadRequest("EndTime must be after StartTime");

                if (qrSession.ExpiryTime <= qrSession.EndTime)
                    return BadRequest("ExpiryTime must be after EndTime");

                // Generate QR code data if not provided
                if (string.IsNullOrEmpty(qrSession.QRCodeData))
                {
                    qrSession.QRCodeData = GenerateQRCodeData(qrSession);
                }

                // Ensure CreatedAt is set
                qrSession.CreatedAt = DateTime.Now;

                db.QRSessions.Add(qrSession);
                db.SaveChanges();

                return CreatedAtRoute("DefaultApi", new { id = qrSession.SessionID }, qrSession);
            }
            catch (DbUpdateException dbEx)
            {
                return BadRequest($"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private string GenerateQRCodeData(QRSession qrSession)
        {
            throw new NotImplementedException();
        }

        // PUT: api/QRSessions/5
        public IHttpActionResult PutQRSession(int id, QRSession qrSession)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != qrSession.SessionID)
            {
                return BadRequest();
            }

            db.Entry(qrSession).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (Exception)
            {
                if (!QRSessionExists(id))
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

        // DELETE: api/QRSessions/5
        public IHttpActionResult DeleteQRSession(int id)
        {
            QRSession qrSession = db.QRSessions.Find(id);
            if (qrSession == null)
            {
                return NotFound();
            }

            db.QRSessions.Remove(qrSession);
            db.SaveChanges();

            return Ok(qrSession);
        }

        // GET: api/QRSessions/Course/5 - Get sessions by course
        [Route("api/QRSessions/Course/{courseId}")]
        public IHttpActionResult GetQRSessionsByCourse(int courseId)
        {
            var sessions = db.QRSessions
                .Where(q => q.CourseID == courseId)
                .Include(q => q.Course)
                .Include(q => q.Teacher)
                .ToList();

            return Ok(sessions);
        }

        // GET: api/QRSessions/Active - Get active sessions
        [Route("api/QRSessions/Active")]
        public IHttpActionResult GetActiveQRSessions()
        {
            var now = DateTime.Now;
            var sessions = db.QRSessions
                .Where(q => q.IsActive && q.StartTime <= now && q.ExpiryTime >= now)
                .Include(q => q.Course)
                .Include(q => q.Teacher)
                .ToList();

            return Ok(sessions);
        }

        // POST: api/QRSessions/GenerateForClass - Generate QR session for a class
        [Route("api/QRSessions/GenerateForClass")]
        public IHttpActionResult GenerateQRSessionForClass(GenerateSessionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var session = new QRSession
                {
                    CourseID = request.CourseID,
                    TeacherID = request.TeacherID,
                    SessionDate = DateTime.Today,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now.AddMinutes(request.DurationMinutes),
                    ExpiryTime = DateTime.Now.AddMinutes(request.DurationMinutes + 5),
                    IsActive = true,
                    QRCodeData = GenerateQRCodeData(request.CourseID, request.TeacherID)
                };

                db.QRSessions.Add(session);
                db.SaveChanges();

                return CreatedAtRoute("DefaultApi", new { id = session.SessionID }, session);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        private string GenerateQRCodeData(int courseId, int teacherId)
        {
            return $"CLASS-{courseId}-{teacherId}-{Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        private bool QRSessionExists(int id)
        {
            return db.QRSessions.Count(e => e.SessionID == id) > 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }

    // Request model for generating a session
    public class GenerateSessionRequest
    {
        public int CourseID { get; set; }
        public int TeacherID { get; set; }
        public int DurationMinutes { get; set; }
        public DbGeography Location { get; set; }
    }
}