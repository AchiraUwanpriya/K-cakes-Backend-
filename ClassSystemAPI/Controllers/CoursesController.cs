using System.Linq;
using System.Net;
using System.Web.Http;
using System.Data.Entity;
using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;
using System.Collections.Generic;

namespace ClassSystemAPI.Controllers
{
    public class CoursesController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();
        public CoursesController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/Courses
        public IHttpActionResult GetCourses()
        {
            var courses = db.Courses
                .Include(c => c.Teacher)
                .Include(c => c.CourseSubjects.Select(cs => cs.Subject))
                .Select(c => new
                {
                    c.CourseID,
                    c.CourseName,
                    c.CourseCode,
                    c.AcademicYear,
                    c.Description,
                    c.IsActive,
                    Subjects = c.CourseSubjects.Select(cs => new
                    {
                        cs.Subject.SubjectID,
                        cs.Subject.SubjectName,
                        cs.Subject.SubjectCode,
                        cs.Subject.IsActive
                    }).ToList(),
                    Teacher = c.Teacher == null ? null : new
                    {
                        c.Teacher.TeacherID,
                        User = c.Teacher.User == null ? null : new
                        {
                            c.Teacher.User.UserID,
                            c.Teacher.User.Username,
                            c.Teacher.User.FirstName,
                            c.Teacher.User.LastName
                        }
                    }
                })
                .ToList();

            return Ok(courses);
        }

        // GET: api/Courses/5
        public IHttpActionResult GetCourse(int id)
        {
            var course = db.Courses
                .Where(c => c.CourseID == id)
                .Include(c => c.Teacher)
                .Include(c => c.CourseSubjects.Select(cs => cs.Subject))
                .Select(c => new
                {
                    c.CourseID,
                    c.CourseName,
                    c.CourseCode,
                    c.AcademicYear,
                    c.Description,
                    c.IsActive,
                    Subjects = c.CourseSubjects.Select(cs => new
                    {
                        cs.Subject.SubjectID,
                        cs.Subject.SubjectName,
                        cs.Subject.SubjectCode,
                        cs.Subject.IsActive,
                        cs.Subject.Duration_In_Months,
                        cs.Subject.MonthlyFee,
                        cs.Subject.TotalFee
                    }).ToList(),
                    Teacher = c.Teacher == null ? null : new
                    {
                        c.Teacher.TeacherID,
                        User = c.Teacher.User == null ? null : new
                        {
                            c.Teacher.User.UserID,
                            c.Teacher.User.Username,
                            c.Teacher.User.FirstName,
                            c.Teacher.User.LastName
                        }
                    }
                })
                .FirstOrDefault();

            if (course == null)
                return NotFound();

            return Ok(course);
        }

        // POST: api/Courses
        public IHttpActionResult PostCourse(CourseCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var subjectIds = (dto.SubjectIDs ?? new List<int>()).Distinct().ToList();

            // Validate provided subject IDs exist
            if (subjectIds.Any())
            {
                var existingSubjectIds = db.Subjects
                    .Where(s => subjectIds.Contains(s.SubjectID))
                    .Select(s => s.SubjectID)
                    .ToList();

                var missing = subjectIds.Except(existingSubjectIds).ToList();
                if (missing.Any())
                    return BadRequest($"Invalid SubjectIDs: {string.Join(",", missing)}");
            }

            // Optional: validate Teacher exists if a non-zero TeacherID was supplied
            if (dto.TeacherID != 0 && !db.Teachers.Any(t => t.TeacherID == dto.TeacherID))
                return BadRequest("Invalid TeacherID.");

            using (var tx = db.Database.BeginTransaction())
            {
                var course = new Course
                {
                    CourseName = dto.CourseName,
                    CourseCode = dto.CourseCode,
                    TeacherID = dto.TeacherID,
                    AcademicYear = dto.AcademicYear,
                    Description = dto.Description,
                    IsActive = dto.IsActive
                };

                db.Courses.Add(course);
                db.SaveChanges();

                if (subjectIds.Any())
                {
                    foreach (var sid in subjectIds)
                    {
                        db.CourseSubjects.Add(new CourseSubjects { CourseID = course.CourseID, SubjectID = sid });
                    }
                    db.SaveChanges();
                }

                tx.Commit();

                // Build response similar to GET
                var created = db.Courses
                    .Where(c => c.CourseID == course.CourseID)
                    .Include(c => c.CourseSubjects.Select(cs => cs.Subject))
                    .Include(c => c.Teacher)
                    .Select(c => new
                    {
                        c.CourseID,
                        c.CourseName,
                        c.CourseCode,
                        c.AcademicYear,
                        c.Description,
                        c.IsActive,
                        Subjects = c.CourseSubjects.Select(cs => new
                        {
                            cs.Subject.SubjectID,
                            cs.Subject.SubjectName,
                            cs.Subject.SubjectCode
                        }).ToList(),
                    })
                    .FirstOrDefault();

                return CreatedAtRoute("DefaultApi", new { id = course.CourseID }, created);
            }
        }

        // PUT: api/Courses/5
        public IHttpActionResult PutCourse(int id, CourseCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var course = db.Courses.Find(id);
            if (course == null)
                return NotFound();

            var subjectIds = (dto.SubjectIDs ?? new List<int>()).Distinct().ToList();

            // Validate provided subject IDs exist before mutating existing links
            if (subjectIds.Any())
            {
                var existingSubjectIds = db.Subjects
                    .Where(s => subjectIds.Contains(s.SubjectID))
                    .Select(s => s.SubjectID)
                    .ToList();

                var missing = subjectIds.Except(existingSubjectIds).ToList();
                if (missing.Any())
                    return BadRequest($"Invalid SubjectIDs: {string.Join(",", missing)}");
            }

            // Optional: validate Teacher exists if a non-zero TeacherID was supplied
            if (dto.TeacherID != 0 && !db.Teachers.Any(t => t.TeacherID == dto.TeacherID))
                return BadRequest("Invalid TeacherID.");

            using (var tx = db.Database.BeginTransaction())
            {
                course.CourseName = dto.CourseName;
                course.CourseCode = dto.CourseCode;
                course.TeacherID = dto.TeacherID;
                course.AcademicYear = dto.AcademicYear;
                course.Description = dto.Description;
                course.IsActive = dto.IsActive;

                // Update join table: remove existing and add new
                var existing = db.CourseSubjects.Where(cs => cs.CourseID == id).ToList();
                if (existing.Any())
                {
                    db.CourseSubjects.RemoveRange(existing);
                }

                if (subjectIds.Any())
                {
                    foreach (var sid in subjectIds)
                    {
                        db.CourseSubjects.Add(new CourseSubjects { CourseID = id, SubjectID = sid });
                    }
                }

                db.Entry(course).State = EntityState.Modified;
                db.SaveChanges();

                tx.Commit();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Courses/5
        public IHttpActionResult DeleteCourse(int id)
        {
            var course = db.Courses.Find(id);
            if (course == null)
                return NotFound();

            // remove join rows first
            var links = db.CourseSubjects.Where(cs => cs.CourseID == id).ToList();
            if (links.Any())
            {
                db.CourseSubjects.RemoveRange(links);
            }

            db.Courses.Remove(course);
            db.SaveChanges();

            return Ok(course);
        }
    }
}
