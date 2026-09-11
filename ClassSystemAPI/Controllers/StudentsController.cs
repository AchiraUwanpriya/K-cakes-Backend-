//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Data.Entity.Infrastructure;
//using System.Linq;
//using System.Net;
//using System.Web;
//using System.Web.Http;
//using ClassSystemAPI.Models;
//using ClassSystemAPI.Models.DTOs;

//namespace ClassSystemAPI.Controllers
//{
//    public class StudentsController : ApiController
//    {
//        private ClassSystemContext db = new ClassSystemContext();

//        // GET: api/Students
//        public IHttpActionResult GetStudents()
//        {
//            var students = db.Students
//                .Include(s => s.User)
//                .Select(s => new StudentDto
//                {
//                    StudentID = s.StudentID,
//                    RollNumber = s.RollNumber,
//                    EnrollmentDate = s.EnrollmentDate,
//                    CurrentGrade = s.CurrentGrade,
//                    ParentName = s.ParentName,
//                    ParentContact = s.ParentContact,
//                    UserID = s.User.UserID,
//                    Username = s.User.Username
//                })
//                .ToList();

//            return Ok(students);
//        }


//        // GET: api/Students/5
//        public IHttpActionResult GetStudent(int id)
//        {
//            Student student = db.Students.Find(id);
//            if (student == null)
//                return NotFound();

//            return Ok(student);
//        }

//        // POST: api/Students
//        public IHttpActionResult PostStudent(Student student)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            db.Students.Add(student);
//            db.SaveChanges();

//            return CreatedAtRoute("DefaultApi", new { id = student.StudentID }, student);
//        }

//        // PUT: api/Students/5
//        public IHttpActionResult PutStudent(int id, Student student)
//        {
//            if (!ModelState.IsValid)
//                return BadRequest(ModelState);

//            if (id != student.StudentID)
//                return BadRequest();

//            db.Entry(student).State = EntityState.Modified;

//            try
//            {
//                db.SaveChanges();
//            }
//            catch (DbUpdateConcurrencyException)
//            {
//                if (!StudentExists(id))
//                    return NotFound();
//                else
//                    throw;
//            }

//            return StatusCode(HttpStatusCode.NoContent);
//        }

//        // DELETE: api/Students/5
//        public IHttpActionResult DeleteStudent(int id)
//        {
//            Student student = db.Students.Find(id);
//            if (student == null)
//                return NotFound();

//            db.Students.Remove(student);
//            db.SaveChanges();

//            return Ok(student);
//        }

//        private bool StudentExists(int id)
//        {
//            return db.Students.Count(e => e.StudentID == id) > 0;
//        }
//    }

//}





using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web.Http;
using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;

namespace ClassSystemAPI.Controllers
{
    public class StudentsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public StudentsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/Students
        public IHttpActionResult GetStudents()
        {
            var students = db.Students
                .Include(s => s.User)
                .Select(s => new StudentDetailsDto
                {
                    StudentID = s.StudentID,
                    RollNumber = s.RollNumber,
                    EnrollmentDate = s.EnrollmentDate,
                    CurrentGrade = s.CurrentGrade,
                    ParentName = s.ParentName,
                    ParentContact = s.ParentContact,
                    UserDetails = new UserDto
                    {
                        UserID = s.User.UserID,
                        Username = s.User.Username,
                        Email = s.User.Email,
                        FirstName = s.User.FirstName,
                        LastName = s.User.LastName,
                        HED_MOBILE_NO = s.User.HED_MOBILE_NO,
                        IsActive = s.User.IsActive
                    }
                })
                .ToList();

            return Ok(students);
        }

        // GET: api/Students/5

        public IHttpActionResult GetStudent(int id)
        {
            var student = db.Students
                .Include(s => s.User)
                .Where(s => s.StudentID == id)
                .Select(s => new StudentDetailsDto
                {
                    StudentID = s.StudentID,
                    RollNumber = s.RollNumber,
                    EnrollmentDate = s.EnrollmentDate,
                    CurrentGrade = s.CurrentGrade,
                    ParentName = s.ParentName,
                    ParentContact = s.ParentContact,
                    UserDetails = new UserDto
                    {
                        UserID = s.User.UserID,
                        Username = s.User.Username,
                        Email = s.User.Email,
                        FirstName = s.User.FirstName,
                        LastName = s.User.LastName,
                        HED_MOBILE_NO = s.User.HED_MOBILE_NO,
                        IsActive = s.User.IsActive
                    }
                })
                .FirstOrDefault();

            if (student == null)
                return NotFound();

            return Ok(student);
        }
        
        // POST: api/Students
        public IHttpActionResult PostStudent(StudentCreateDto studentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify the user exists and is a student
            var user = db.Users.Find(studentDto.UserID);

            if (user == null)
                return BadRequest("User not found");

            if (user.UserTypeID != 3) // Assuming 3 is the Student type
                return BadRequest("User is not registered as a student");

            // Check if student already exists for this user
            if (db.Students.Any(s => s.StudentID == studentDto.UserID))
                return BadRequest("Student record already exists for this user");

            var student = new Student
            {
                StudentID = studentDto.UserID,
                RollNumber = studentDto.RollNumber,
                EnrollmentDate = studentDto.EnrollmentDate ?? DateTime.Today,
                CurrentGrade = studentDto.CurrentGrade,
                ParentName = studentDto.ParentName,
                ParentContact = studentDto.ParentContact
            };

            db.Students.Add(student);

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                if (StudentExists(student.StudentID))
                    return Conflict();
                throw;
            }

            return CreatedAtRoute("DefaultApi", new { id = student.StudentID }, student);
        }

        // PUT: api/Students/5
        public IHttpActionResult PutStudent(int id, StudentUpdateDto studentDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != studentDto.StudentID)
                return BadRequest("ID mismatch");

            var student = db.Students.Find(id);
            if (student == null)
                return NotFound();

            // Update only the student-specific fields
            student.RollNumber = studentDto.RollNumber;
            student.CurrentGrade = studentDto.CurrentGrade;
            student.ParentName = studentDto.ParentName;
            student.ParentContact = studentDto.ParentContact;

            db.Entry(student).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                    return NotFound();
                throw;
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/Students/5
        public IHttpActionResult DeleteStudent(int id)
        {
            Student student = db.Students.Find(id);
            if (student == null)
                return NotFound();

            // Note: We're not deleting the user, just the student record
            db.Students.Remove(student);
            db.SaveChanges();

            return Ok(new { Message = "Student record deleted successfully" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool StudentExists(int id)
        {
            return db.Students.Count(e => e.StudentID == id) > 0;
        }
    }
}