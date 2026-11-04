using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Http;
using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;

namespace ClassSystemAPI.Controllers
{
    public class SubjectsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();
        public SubjectsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/Subjects
        public IHttpActionResult GetSubjects()
        {
            //return db.Subjects.ToList();

            var subjects = db.Subjects.Select(s => new
            {
                s.SubjectID,
                s.SubjectName,
                s.SubjectCode,
                s.Description,
                s.TotalFee,
                s.Duration_In_Months,
                s.MonthlyFee,
                s.PaymentDueDay,
                s.IsActive,

                Courses = s.CourseSubjects
                    .Where(cs => cs.IsActive)
                    .Select(cs => new
                    {
                        cs.Course.CourseID,
                        cs.Course.CourseName,
                        cs.Course.CourseCode,
                        cs.Course.TeacherID,
                        cs.Course.AcademicYear,
                        cs.Course.Description,
                        cs.Course.IsActive
                    })
                    .ToList()
            })
            .ToList();

            return Ok(subjects);
        }

        [HttpGet]
        [Route("api/Subjects/All")]
        public IHttpActionResult GetAllSubjects()
        {
            var subjects = db.Subjects
                .Select(s => new
                {
                    s.SubjectID,
                    s.SubjectName,
                    s.SubjectCode,
                    s.Description,
                    s.TotalFee,
                    s.Duration_In_Months,
                    s.MonthlyFee,
                    s.PaymentDueDay,
                    s.IsActive
                })
                .ToList();

            return Ok(subjects);
        }

        [HttpPut]
        [Route("api/Subjects/ChangeStatus/{id}")]
        public IHttpActionResult ChangeSubjectStatus(int id, bool isActive)
        {
            var subject = db.Subjects.Find(id);

            if (subject == null)
                return NotFound();


            subject.IsActive = isActive;

            db.SaveChanges();

            return Ok(new
            {
                message = isActive
                    ? "Subject activated successfully"
                    : "Subject deactivated successfully"
            });
        }

        // GET: api/Subjects/LatestId
        [HttpGet]
        [Route("api/Subjects/LatestId")]
        public IHttpActionResult GetLatestSubjectId()
        {
            // Order by SubjectID descending and take the first ID.
            // If there are no subjects, return an object with a null value so the frontend can handle it gracefully.
            var latestId = db.Subjects
                             .OrderByDescending(s => s.SubjectID)
                             .Select(s => s.SubjectID)
                             .FirstOrDefault();

            // FirstOrDefault returns 0 when sequence is empty (SubjectID is assumed to be positive).
            if (latestId == 0)
                return Ok(new { latestSubjectId = (int?)null });

            return Ok(new { latestSubjectId = latestId });
        }

        // GET: api/Subjects/5
        public IHttpActionResult GetSubject(int id)
        {
            var subject = db.Subjects.Find(id);
            if (subject == null)
                return NotFound();

            return Ok(subject);
        }

        [HttpGet]
        [Route("api/Subjects/NotEnrolled")]
        public IHttpActionResult GetSubjectsNotEnrolledInAnyCourse()
        {
            var result = db.Subjects
                //.Where(s => !db.CourseSubjects
                //    .Any(cs => cs.SubjectID == s.SubjectID))

                .Where(s => s.IsActive &&
           !db.CourseSubjects.Any(cs => cs.SubjectID == s.SubjectID))
                .Select(s => new
                {
                    s.SubjectID,
                    s.SubjectName,
                    s.SubjectCode,
                    s.Description,
                    s.TotalFee,
                    s.Duration_In_Months,
                    s.MonthlyFee,
                    s.PaymentDueDay
                })
                .ToList();
            return Ok(result);
        }
        // POST: api/Subjects


        //public IHttpActionResult PostSubject(Subject subject)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    db.Subjects.Add(subject);
        //    db.SaveChanges();

        //    return CreatedAtRoute("DefaultApi", new { id = subject.SubjectID }, subject);
        //}


        // Achira

        //public IHttpActionResult PostSubject(Subject subject)
        //{
        //    if (!ModelState.IsValid)
        //        return BadRequest(ModelState);

        //    subject.IsActive = true; // force default to true on creation

        //    db.Subjects.Add(subject);
        //    db.SaveChanges();
        //    return CreatedAtRoute("DefaultApi", new { id = subject.SubjectID }, subject);
        //}

        public IHttpActionResult PostSubject(SubjectCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var courseIds = (dto.CourseIDs ?? new List<int>()).Distinct().ToList();

            // Validate provided course IDs exist
            if (courseIds.Any())
            {
                var existingCourseIds = db.Courses
                    .Where(c => courseIds.Contains(c.CourseID))
                    .Select(c => c.CourseID)
                    .ToList();

                var missing = courseIds.Except(existingCourseIds).ToList();
                if (missing.Any())
                    return BadRequest($"Invalid CourseIDs: {string.Join(",", missing)}");
            }

            using (var tx = db.Database.BeginTransaction())
            {
                var subject = new Subject
                {
                    SubjectName = dto.SubjectName,
                    SubjectCode = dto.SubjectCode,
                    Description = dto.Description,
                    TotalFee = dto.TotalFee,
                    Duration_In_Months = dto.Duration_In_Months,
                    MonthlyFee = dto.MonthlyFee,
                    PaymentDueDay = dto.PaymentDueDay ?? 6,
                    IsActive = true // force default to true on creation
                };

                db.Subjects.Add(subject);
                db.SaveChanges();

                if (courseIds.Any())
                {
                    foreach (var cid in courseIds)
                    {
                        db.CourseSubjects.Add(new CourseSubjects { CourseID = cid, SubjectID = subject.SubjectID });
                    }
                    db.SaveChanges();
                }

                tx.Commit();

                // Build response similar to GET
                var created = db.Subjects
                    .Where(s => s.SubjectID == subject.SubjectID)
                    .Include(s => s.CourseSubjects.Select(cs => cs.Course))
                    .Select(s => new
                    {
                        s.SubjectID,
                        s.SubjectName,
                        s.SubjectCode,
                        s.Description,
                        s.TotalFee,
                        s.Duration_In_Months,
                        s.MonthlyFee,
                        s.PaymentDueDay,
                        s.IsActive,
                        Courses = s.CourseSubjects.Select(cs => new
                        {
                            cs.Course.CourseID,
                            cs.Course.CourseName,
                            cs.Course.CourseCode
                        }).ToList(),
                    })
                    .FirstOrDefault();

                return CreatedAtRoute("DefaultApi", new { id = subject.SubjectID }, created);
            }
        }


        // PUT: api/Subjects/5
        public IHttpActionResult PutSubject(int id, Subject subject)
        {
            if (!ModelState.IsValid || id != subject.SubjectID)
                return BadRequest();

            db.Entry(subject).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();

            return StatusCode(System.Net.HttpStatusCode.NoContent);
        }

       

        // DELETE: api/Subjects/5
        public IHttpActionResult DeleteSubject(int id)
        {
            var subject = db.Subjects.Find(id);
            if (subject == null)
                return NotFound();

            db.Subjects.Remove(subject);
            db.SaveChanges();

            return Ok(subject);
        }

        [HttpGet]
        [Route("api/Subjects/StudentsBySubject/{subjectId}")]
        public IHttpActionResult GetStudentsBySubject(int subjectId)
        {
            var result = (from a in db.Enrollments
                          join b in db.Courses on a.CourseID equals b.CourseID
                          join c in db.Subjects on a.SubjectID equals c.SubjectID
                          join d in db.Users on a.StudentID equals d.UserID
                          join e in db.Students on a.StudentID equals e.StudentID
                          where a.SubjectID == subjectId
                          orderby a.SubjectID ascending
                          select new
                          {                          
                              a.EnrollmentID,
                              CourseID =  a.CourseID,
                              b.CourseName,
                              SubjectID = a.SubjectID,
                              c.SubjectName,
                              a.StudentID,
                              d.Username,
                              d.FirstName,
                              d.LastName,
                              d.Email,
                              e.RollNumber,
                              a.EnrollmentDate,
                              a.IsActive
                          }).ToList();

          return Ok(result);
        }
        [HttpGet]
        [Route("api/Subjects/EnrolledByStudent/{studentId}")]
        public IHttpActionResult GetEnrolledSubjectsByStudent(int studentId)
        {
            var result = (from e in db.Enrollments
                          join c in db.Courses on e.CourseID equals c.CourseID
                          join s in db.Subjects on e.SubjectID equals s.SubjectID
                          where e.StudentID == studentId
                          group new { e, c, s } by new { c.CourseID, c.CourseName } into g
                          orderby g.Key.CourseID
                          select new
                          {
                              CourseID = g.Key.CourseID,
                              CourseName = g.Key.CourseName,
                              Subjects = g
                                  .Select(x => new
                                  {
                                      x.s.SubjectID,
                                      x.s.SubjectName
                                  })
                                  .Distinct()
                          }).ToList();
            return Ok(result);
        }
    }
}



