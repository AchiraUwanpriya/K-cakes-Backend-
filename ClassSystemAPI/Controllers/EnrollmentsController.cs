using System.Linq;
using System.Net;
using System.Web.Http;
using System.Data.Entity;
using ClassSystemAPI.Models;

namespace ClassSystemAPI.Controllers
{
    public class EnrollmentsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public EnrollmentsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/Enrollments
        //public IQueryable<Enrollment> GetEnrollments()
        //{
        //    return db.Enrollments.Include(e => e.Course).Include(e => e.Student);
        //}

        public IHttpActionResult GetEnrollments()
        {
            var enrollments = db.Enrollments
                .Include(e => e.Course)
                .Include(e => e.Student)
                .Select(e => new
                {
                    e.EnrollmentID,
                    e.EnrollmentDate,
                    e.IsActive,
                })
                .ToList();

            return Ok(enrollments);
        }

        // GET: api/Enrollments/5
        public IHttpActionResult GetEnrollment(int id)
        {
            var enrollment = db.Enrollments
                .Include(e => e.Course.Teacher)
                .Include(e => e.Student)
                .Where(e => e.EnrollmentID == id)
                .Select(e => new
                {
                    e.EnrollmentID,
                    e.EnrollmentDate,
                    e.IsActive,

                    Course = new
                    {
                        e.Course.CourseID,
                        e.Course.CourseName,
                        e.Course.Description,
                        Teacher = new
                        {
                            e.Course.Teacher.TeacherID,
                            e.Course.Teacher.User.Username
                        }
                    },

                    Student = new
                    {
                        e.Student.StudentID,
                    }
                })
                .FirstOrDefault();

            if (enrollment == null)
                return NotFound();

            return Ok(enrollment);
        }

        // NEW: api/Enrollments/Student/{studentId} - Courses for a student
        [HttpGet]
        [Route("api/Enrollments/Student/{studentId}")]
        public IHttpActionResult GetStudentCourses(int studentId)
        {
            var studentExists = db.Students.Any(s => s.StudentID == studentId);
            if (!studentExists) return NotFound();

            var courses = db.Enrollments
                .Where(e => e.StudentID == studentId && e.IsActive)
                .Include(e => e.Course.CourseSubjects.Select(cs => cs.Subject))
                .Include(e => e.Course.Teacher.User)
                .Select(e => new
                {
                    e.EnrollmentID,
                    e.EnrollmentDate,
                    Course = new
                    {
                        e.Course.CourseID,
                        e.Course.CourseName,
                        e.Course.CourseCode,
                        e.Course.AcademicYear,
                        e.Course.Description,
                        Subjects = e.Course.CourseSubjects
                            .Select(cs => new
                            {
                                cs.Subject.SubjectID,
                                cs.Subject.Description
                            }),
                        Teacher = e.Course.Teacher == null ? null : new
                        {
                            e.Course.Teacher.TeacherID,
                            e.Course.Teacher.User.FirstName,
                            e.Course.Teacher.User.LastName,
                            e.Course.Teacher.User.Username
                        }
                    }
                })
                .ToList();

            return Ok(courses);
        }

        // POST: api/Enrollments
        public IHttpActionResult PostEnrollment(Enrollment enrollment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Enrollments.Add(enrollment);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = enrollment.EnrollmentID }, enrollment);
        }

        // PUT: api/Enrollments/5
        public IHttpActionResult PutEnrollment(int id, Enrollment enrollment)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != enrollment.EnrollmentID)
                return BadRequest();

            db.Entry(enrollment).State = EntityState.Modified;
            db.SaveChanges();

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Enrollments/5
        public IHttpActionResult DeleteEnrollment(int id)
        {
            var enrollment = db.Enrollments.Find(id);
            if (enrollment == null)
                return NotFound();

            db.Enrollments.Remove(enrollment);
            db.SaveChanges();

            return Ok(enrollment);
        }
    }
}
