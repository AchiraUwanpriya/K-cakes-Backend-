//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.Http;
//using ClassSystemAPI.Models;
//using System.Data.Entity; // added
//using System.Net;
//using System.Data.Entity.Infrastructure;

//namespace ClassSystemAPI.Controllers
//{

//    public class TeachersController : ApiController
//    {
//        private ClassSystemContext db = new ClassSystemContext();

//        //GET: api/Teachers
//        [HttpGet]
//        public IHttpActionResult GetTeachers()
//        {
//            var teachers = db.Teachers.Include("User").ToList();
//            return Ok(teachers);
//        }

//        //GET: api/Teachers/5
//        [HttpGet]
//        public IHttpActionResult GetTeacher(int id)
//        {
//            var teacher = db.Teachers.Include("User").FirstOrDefault(t => t.TeacherID == id);
//            if (teacher == null) return NotFound();
//            return Ok(teacher);
//        }
//        //POST: api/Teachers
//        [HttpPost]
//        public IHttpActionResult PostTeacher(Teacher teacher)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            db.Teachers.Add(teacher);
//            db.SaveChanges();
//            return Ok(teacher);
//        }

//        // PUT: api/Teachers/5
//        [HttpPut]
//        public IHttpActionResult PutTeacher(int id, Teacher teacher)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            if (id != teacher.TeacherID)
//                return BadRequest("ID mismatch");

//            var existing = db.Teachers.Find(id);
//            if (existing == null) return NotFound();

//            // Update only teacher-specific fields (do not overwrite navigation User here)
//            existing.EmployeeID = teacher.EmployeeID;
//            existing.Department = teacher.Department;
//            existing.Qualification = teacher.Qualification;
//            existing.JoiningDate = teacher.JoiningDate;
//            existing.Bio = teacher.Bio;

//            db.Entry(existing).State = EntityState.Modified;

//            try
//            {
//                db.SaveChanges();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                if (!TeacherExists(id))
//                    return NotFound();
//                throw;
//            }

//            return StatusCode(HttpStatusCode.NoContent);
//        }

//        // DELETE: api/Teachers/5
//        [HttpDelete]
//        public IHttpActionResult DeleteTeacher(int id)
//        {
//            var teacher = db.Teachers.Find(id);
//            if (teacher == null)
//                return NotFound();

//            // Note: This endpoint deletes only the teacher record (not the associated User).
//            db.Teachers.Remove(teacher);
//            db.SaveChanges();

//            return Ok(new { Message = "Teacher record deleted successfully" });
//        }

//        // GET: api/Teachers/{teacherId}/Courses/Students
//        // Returns all courses for the teacher, each with enrolled students
//        [HttpGet]
//        [Route("api/Teachers/{teacherId}/Courses/Students")]
//        public IHttpActionResult GetTeacherCoursesWithStudents(int teacherId)
//        {
//            var teacherExists = db.Teachers.Any(t => t.TeacherID == teacherId);
//            if (!teacherExists) return NotFound();

//            var data = db.Courses
//                .Where(c => c.TeacherID == teacherId /* && c.IsActive */)
//                .Select(c => new
//                {
//                    c.CourseID,
//                    c.CourseName,
//                    c.CourseCode,
//                    c.AcademicYear,
//                    c.Description,
//                    Students = db.Enrollments
//                        .Where(e => e.CourseID == c.CourseID && e.IsActive)
//                        .Select(e => new
//                        {
//                            e.EnrollmentID,
//                            e.EnrollmentDate,
//                            Student = new
//                            {
//                                e.Student.StudentID,
//                                e.Student.RollNumber,
//                                User = new
//                                {
//                                    e.Student.User.UserID,
//                                    e.Student.User.Username,
//                                    e.Student.User.FirstName,
//                                    e.Student.User.LastName,
//                                    e.Student.User.Email,
//                                    e.Student.User.IsActive
//                                }
//                            }
//                        })
//                        .ToList()
//                })
//                .ToList();

//            return Ok(data);
//        }

//        // GET: api/Teachers/{teacherId}/Courses/{courseId}/Students
//        // Returns enrolled students for a specific course ensuring it belongs to the teacher
//        [HttpGet]
//        [Route("api/Teachers/{teacherId}/Courses/{courseId}/Students")]
//        public IHttpActionResult GetCourseStudentsForTeacher(int teacherId, int courseId)
//        {
//            var course = db.Courses.FirstOrDefault(c => c.CourseID == courseId && c.TeacherID == teacherId);
//            if (course == null) return NotFound();

//            var students = db.Enrollments
//                .Where(e => e.CourseID == courseId && e.IsActive)
//                .Select(e => new
//                {
//                    e.EnrollmentID,
//                    e.EnrollmentDate,
//                    Student = new
//                    {
//                        e.Student.StudentID,
//                        e.Student.RollNumber,
//                        User = new
//                        {
//                            e.Student.User.UserID,
//                            e.Student.User.Username,
//                            e.Student.User.FirstName,
//                            e.Student.User.LastName,
//                            e.Student.User.Email
//                        }
//                    }
//                })
//                .ToList();

//            return Ok(new
//            {
//                course.CourseID,
//                course.CourseName,
//                course.CourseCode,
//                course.AcademicYear,
//                Students = students
//            });
//        }

//        private bool TeacherExists(int id)
//        {
//            return db.Teachers.Count(e => e.TeacherID == id) > 0;
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing)
//            {
//                db.Dispose();
//            }
//            base.Dispose(disposing);
//        }
//    }
//}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using ClassSystemAPI.Models;
using System.Data.Entity; // added
using System.Net;
using System.Data.Entity.Infrastructure;

namespace ClassSystemAPI.Controllers
{

    public class TeachersController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();


        public TeachersController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        //GET: api/Teachers
        [HttpGet]
        public IHttpActionResult GetTeachers()
        {
            var teachers = db.Teachers.Include("User").ToList();
            return Ok(teachers);
        }

        //GET: api/Teachers/5
        [HttpGet]
        public IHttpActionResult GetTeacher(int id)
        {
            var teacher = db.Teachers.Include("User").FirstOrDefault(t => t.TeacherID == id);
            if (teacher == null) return NotFound();
            return Ok(teacher);
        }
        //POST: api/Teachers
        [HttpPost]
        public IHttpActionResult PostTeacher(Teacher teacher)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            db.Teachers.Add(teacher);
            db.SaveChanges();
            return Ok(teacher);
        }

        // PUT: api/Teachers/5
        [HttpPut]
        public IHttpActionResult PutTeacher(int id, Teacher teacher)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != teacher.TeacherID)
                return BadRequest("ID mismatch");

            var existing = db.Teachers.Find(id);
            if (existing == null) return NotFound();

            // Update only teacher-specific fields (do not overwrite navigation User here)
            existing.EmployeeID = teacher.EmployeeID;
            existing.Department = teacher.Department;
            existing.Qualification = teacher.Qualification;
            existing.JoiningDate = teacher.JoiningDate;
            existing.Bio = teacher.Bio;

            db.Entry(existing).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TeacherExists(id))
                    return NotFound();
                throw;
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Teachers/5
        [HttpDelete]
        public IHttpActionResult DeleteTeacher(int id)
        {
            var teacher = db.Teachers.Find(id);
            if (teacher == null)
                return NotFound();

            // Note: This endpoint deletes only the teacher record (not the associated User).
            db.Teachers.Remove(teacher);
            db.SaveChanges();

            return Ok(new { Message = "Teacher record deleted successfully" });
        }

        // GET: api/Teachers/{teacherId}/Courses/Students
        // Returns all courses for the teacher, each with enrolled students
        //[HttpGet]
        //[Route("api/Teachers/{teacherId}/Courses/Students")]
        //public IHttpActionResult GetTeacherCoursesWithStudents(int teacherId)
        //{
        //    var teacherExists = db.Teachers.Any(t => t.TeacherID == teacherId);
        //    if (!teacherExists) return NotFound();

        //    var data = db.Courses
        //        .Where(c => c.TeacherID == teacherId /* && c.IsActive */)
        //        .Select(c => new
        //        {
        //            c.CourseID,
        //            c.CourseName,
        //            c.CourseCode,
        //            c.AcademicYear,
        //            c.Description,
        //            Students = db.Enrollments
        //                .Where(e => e.CourseID == c.CourseID && e.IsActive)
        //                .Select(e => new
        //                {
        //                    e.EnrollmentID,
        //                    e.EnrollmentDate,
        //                    Student = new
        //                    {
        //                        e.Student.StudentID,
        //                        e.Student.RollNumber,
        //                        User = new
        //                        {
        //                            e.Student.User.UserID,
        //                            e.Student.User.Username,
        //                            e.Student.User.FirstName,
        //                            e.Student.User.LastName,
        //                            e.Student.User.Email
        //                        }
        //                    }
        //                })
        //                .ToList()
        //        })
        //        .ToList();

        //    return Ok(data);
        //}


        //[HttpGet]
        //[Route("api/Teachers/{teacherId}/Courses/Students")]
        //public IHttpActionResult GetTeacherCoursesWithStudents(int teacherId)
        //{
        //    var teacherExists = db.Teachers.Any(t => t.TeacherID == teacherId);
        //    if (!teacherExists) return NotFound();

        //    var data = db.Courses
        //        .Where(c =>
        //            c.TeacherID == teacherId &&
        //            db.Enrollments.Any(e =>
        //                e.CourseID == c.CourseID &&
        //                e.IsActive
        //            )
        //        ) // <-- IMPORTANT FILTER
        //        .Select(c => new
        //        {
        //            c.CourseID,
        //            c.CourseName,
        //            c.CourseCode,
        //            c.AcademicYear,
        //            c.Description,
        //            Students = db.Enrollments
        //                .Where(e => e.CourseID == c.CourseID && e.IsActive)
        //                .Select(e => new
        //                {
        //                    e.EnrollmentID,
        //                    e.EnrollmentDate,
        //                    Student = new
        //                    {
        //                        e.Student.StudentID,
        //                        e.Student.RollNumber,
        //                        User = new
        //                        {
        //                            e.Student.User.UserID,
        //                            e.Student.User.Username,
        //                            e.Student.User.FirstName,
        //                            e.Student.User.LastName,
        //                            e.Student.User.Email
        //                        }
        //                    }
        //                })
        //                .ToList()
        //        })
        //        .ToList();

        //    return Ok(data);
        //}
        //[HttpGet]
        //[Route("api/Teachers/{teacherId}/Courses/Students")]
        //public IHttpActionResult GetTeacherCoursesWithStudents(int teacherId)
        //{
        //    var teacherExists = db.Teachers.Any(t => t.TeacherID == teacherId);
        //    if (!teacherExists) return NotFound();

        //    var data = db.Courses
        //        .Where(c => c.TeacherID == teacherId)
        //        .Select(c => new
        //        {
        //            c.CourseID,
        //            c.CourseName,
        //            c.CourseCode,
        //            c.AcademicYear,
        //            c.Description,

        //            Students =
        //                db.Students
        //                .Select(s => new
        //                {
        //                    s.StudentID,
        //                    s.RollNumber,

        //                    // LEFT JOIN enrollment
        //                    Enrollment = db.Enrollments
        //                        .Where(e => e.StudentID == s.StudentID && e.CourseID == c.CourseID)
        //                        .Select(e => new
        //                        {
        //                            e.EnrollmentID,
        //                            e.EnrollmentDate,
        //                            e.IsActive
        //                        })
        //                        .FirstOrDefault(),

        //                    User = new
        //                    {
        //                        s.User.UserID,
        //                        s.User.Username,
        //                        s.User.FirstName,
        //                        s.User.LastName,
        //                        s.User.Email
        //                    }
        //                })
        //                .ToList()
        //        })
        //        .ToList();

        //    return Ok(data);
        //}
        [HttpGet]
        [Route("api/Teachers/{teacherId}/Courses/Students")]
        public IHttpActionResult GetTeacherCoursesWithStudents(int teacherId)
        {
            var teacherExists = db.Teachers.Any(t => t.TeacherID == teacherId);
            if (!teacherExists) return NotFound();

            var data = db.Courses
                .Where(c => c.TeacherID == teacherId)
                .Select(c => new
                {
                    c.CourseID,
                    c.CourseName,
                    c.CourseCode,
                    c.AcademicYear,
                    c.Description,
                    Students = db.Students
                        .Select(s => new
                        {
                            s.StudentID,
                            s.RollNumber,
                            // LEFT JOIN enrollment
                            Enrollment = db.Enrollments
                                .Where(e => e.StudentID == s.StudentID && e.CourseID == c.CourseID)
                                .Select(e => new
                                {
                                    e.EnrollmentID,
                                    e.EnrollmentDate,
                                    e.IsActive
                                })
                                .FirstOrDefault(),
                            User = new
                            {
                                s.User.UserID,
                                s.User.Username,
                                s.User.FirstName,
                                s.User.LastName,
                                s.User.Email,
                                s.User.IsActive  // Add this line to include User's IsActive status
                            }
                        })
                        .ToList()
                })
                .ToList();

            return Ok(data);
        }


        [HttpGet]
        [Route("api/Teachers/{teacherId}/{studentId}/Courses/Students")]
        public IHttpActionResult GetTeacherCoursesWithStudent(int teacherId, int studentId)
        {
            var teacherExists = db.Teachers.Any(t => t.TeacherID == teacherId);
            if (!teacherExists) return NotFound();

            var studentExists = db.Students.Any(s => s.StudentID == studentId);
            if (!studentExists) return NotFound();

            var data = db.Courses
                .Where(c => c.TeacherID == teacherId)
                .Select(c => new
                {
                    c.CourseID,
                    c.CourseName,
                    c.CourseCode,
                    c.AcademicYear,
                    c.Description,
                    Students = db.Enrollments
                        .Where(e =>
                            e.CourseID == c.CourseID &&
                            e.IsActive &&
                            e.StudentID == studentId
                        )
                        .Select(e => new
                        {
                            e.EnrollmentID,
                            e.EnrollmentDate,
                            Student = new
                            {
                                e.Student.StudentID,
                                e.Student.RollNumber,
                                User = new
                                {
                                    e.Student.User.UserID,
                                    e.Student.User.Username,
                                    e.Student.User.FirstName,
                                    e.Student.User.LastName,
                                    e.Student.User.Email,
                                    e.Student.User.IsActive
                                }
                            }
                        })
                        .ToList()
                })
                .ToList();

            return Ok(data);
        }


        // GET: api/Teachers/{teacherId}/Courses/{courseId}/Students
        // Returns enrolled students for a specific course ensuring it belongs to the teacher
        [HttpGet]
        [Route("api/Teachers/{teacherId}/Courses/{courseId}/Students")]
        public IHttpActionResult GetCourseStudentsForTeacher(int teacherId, int courseId)
        {
            var course = db.Courses.FirstOrDefault(c => c.CourseID == courseId && c.TeacherID == teacherId);
            if (course == null) return NotFound();

            var students = db.Enrollments
                .Where(e => e.CourseID == courseId && e.IsActive)
                .Select(e => new
                {
                    e.EnrollmentID,
                    e.EnrollmentDate,
                    Student = new
                    {
                        e.Student.StudentID,
                        e.Student.RollNumber,
                        User = new
                        {
                            e.Student.User.UserID,
                            e.Student.User.Username,
                            e.Student.User.FirstName,
                            e.Student.User.LastName,
                            e.Student.User.Email,
                            e.Student.User.ProfilePicture
                        }
                    }
                })
                .ToList();

            return Ok(new
            {
                course.CourseID,
                course.CourseName,
                course.CourseCode,
                course.AcademicYear,
                Students = students
            });
        }

        [HttpDelete]
        [Route("api/Teachers/{teacherId}/Courses/{courseId}")]
        public IHttpActionResult DeleteCourse(int teacherId, int courseId)
        {
            // 1. Validate teacher
            if (!db.Teachers.Any(t => t.TeacherID == teacherId))
                return NotFound();

            // 2. Validate course
            var course = db.Courses.FirstOrDefault(c => c.CourseID == courseId && c.TeacherID == teacherId);
            if (course == null) return NotFound();

            // 3. Soft delete Enrollments
            var enrollments = db.Enrollments.Where(e => e.CourseID == courseId).ToList();
            foreach (var e in enrollments)
                e.IsActive = false;

            // 4. Soft delete ClassSchedules
            var schedules = db.ClassSchedules.Where(cs => cs.CourseID == courseId).ToList();
            foreach (var cs in schedules)
                cs.IsActive = false;

            // 5. Soft delete StudyMaterials (use IsVisible)
            var studyMaterials = db.StudyMaterials.Where(sm => sm.CourseID == courseId).ToList();
            foreach (var sm in studyMaterials)
                sm.IsVisible = false;

            // 6. Soft delete QR Sessions
            var qrSessions = db.QRSessions.Where(q => q.CourseID == courseId).ToList();
            foreach (var qr in qrSessions)
                qr.IsActive = false;

            // 7. Soft delete Attendances linked to QR Sessions
            var sessionIds = qrSessions.Select(q => q.SessionID).ToList();
            var attendances = db.Attendances.Where(a => sessionIds.Contains(a.SessionID)).ToList();
            foreach (var att in attendances)
                att.IsActive = false;

            // 8. Soft delete CourseSubjects
            var courseSubjects = db.CourseSubjects.Where(cs => cs.CourseID == courseId).ToList();
            foreach (var cs in courseSubjects)
                cs.IsActive = false;  // make sure CourseSubjects has IsActive column

            // 9. Soft delete the Course itself
            course.IsActive = false;

            // 10. Save all changes
            db.SaveChanges();

            return Ok(new
            {
                Message = "Course inactivated successfully",
                InactivatedCourseID = courseId
            });
        }

        //[HttpDelete]
        //[Route("api/Admin/Courses/{courseId}")]
        //public IHttpActionResult DeleteCourseByAdmin(int courseId)
        //{
        //    // 1. Validate course
        //    var course = db.Courses.FirstOrDefault(c => c.CourseID == courseId);
        //    if (course == null) return NotFound();

        //    // 2. Soft delete Enrollments
        //    var enrollments = db.Enrollments.Where(e => e.CourseID == courseId).ToList();
        //    foreach (var e in enrollments)
        //        e.IsActive = false;

        //    // 3. Soft delete ClassSchedules
        //    var schedules = db.ClassSchedules.Where(cs => cs.CourseID == courseId).ToList();
        //    foreach (var cs in schedules)
        //        cs.IsActive = false;

        //    // 4. Soft delete StudyMaterials (use IsVisible)
        //    var studyMaterials = db.StudyMaterials.Where(sm => sm.CourseID == courseId).ToList();
        //    foreach (var sm in studyMaterials)
        //        sm.IsVisible = false;

        //    // 5. Soft delete QR Sessions
        //    var qrSessions = db.QRSessions.Where(q => q.CourseID == courseId).ToList();
        //    foreach (var qr in qrSessions)
        //        qr.IsActive = false;

        //    // 6. Soft delete Attendances linked to QR Sessions
        //    var sessionIds = qrSessions.Select(q => q.SessionID).ToList();
        //    var attendances = db.Attendances.Where(a => sessionIds.Contains(a.SessionID)).ToList();
        //    foreach (var att in attendances)
        //        att.IsActive = false;

        //    // 7. Soft delete CourseSubjects
        //    var courseSubjects = db.CourseSubjects.Where(cs => cs.CourseID == courseId).ToList();
        //    foreach (var cs in courseSubjects)
        //        cs.IsActive = false;

        //    // 8. Soft delete the Course itself
        //    course.IsActive = false;

        //    // 9. Save all changes
        //    db.SaveChanges();

        //    return Ok(new
        //    {
        //        Message = "Course inactivated successfully",
        //        InactivatedCourseID = courseId
        //    });
        //}

        [HttpDelete]
        [Route("api/Admin/Students/{studentId}/Courses/{courseId}")]
        public IHttpActionResult DeleteStudentFromCourseByAdmin(int studentId, int courseId)
        {
            // 1. Validate student
            if (!db.Students.Any(s => s.StudentID == studentId))
                return NotFound();

            // 2. Validate course
            if (!db.Courses.Any(c => c.CourseID == courseId))
                return NotFound();

            // 3. Validate active enrollment for this student in this course
            var enrollment = db.Enrollments
                .Where(e => e.CourseID == courseId
                         && e.StudentID == studentId
                         && e.IsActive)
                .OrderByDescending(e => e.EnrollmentID) // guards against duplicate active rows
                .FirstOrDefault();

            if (enrollment == null)
                return NotFound();

            // 4. Soft delete this student's enrollment
            enrollment.IsActive = false;

            // 5. ClassSchedules are course-level (not per-student) — do NOT deactivate here,
            // as that would affect every other student still enrolled in this course.

            // 6. Soft delete this student's Attendance
            // NOTE: currently scoped by StudentID only — this will affect attendance
            // across ALL of the student's courses, not just this one, until we can
            // scope by SessionID -> QRSession -> ClassSchedule -> CourseID.
            var attendances = db.Attendances
                .Where(a => a.StudentID == studentId)
                .ToList();
            foreach (var att in attendances)
                att.IsActive = false;

            //// 7. Soft delete Payments for this student (if exists)
            //var payments = db.Payments
            //    .Where(p => p.CourseID == courseId && p.StudentID == studentId)
            //    .ToList();
            //foreach (var p in payments)
            //    p.IsActive = false;

            // 8. Save all changes
            db.SaveChanges();

            return Ok(new
            {
                Message = "Student removed from course successfully",
                CourseID = courseId,
                StudentID = studentId
            });
        }





        private bool TeacherExists(int id)
        {
            return db.Teachers.Count(e => e.TeacherID == id) > 0;
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
}