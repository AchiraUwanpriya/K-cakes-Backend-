using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    [RoutePrefix("api/Payments")]
    public class PaymentsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public PaymentsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        [HttpPost]
        [Route("")]
        public IHttpActionResult PostPayment(PaymentCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var payment = new Payment
            {
                EnrollmentID = dto.EnrollmentID,
                TotalAmount = dto.TotalAmount,
                PaidAmount = dto.FirstPaidAmount,
                Status = dto.FirstPaidAmount == 0 ? "PENDING" :
                         dto.FirstPaidAmount < dto.TotalAmount ? "PARTIAL" : "PAID",
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            };

            db.Payment.Add(payment);
            db.SaveChanges(); // PaymentID generated

            // Save first payment into history if any
            if (dto.FirstPaidAmount > 0)
            {
                var history = new PaymentHistory
                {
                    PaymentID = payment.PaymentID,
                    AmountPaid = dto.FirstPaidAmount,
                    PaymentMethod = dto.PaymentMethod,
                    ReferenceNo = dto.ReferenceNo,
                    Remarks = dto.Remarks ?? "Initial Payment",
                    PaymentDate = DateTime.Now,
                    CreatedBy = dto.CreatedBy,
                    CreatedDate = DateTime.Now
                };

                db.PaymentHistory.Add(history);
                db.SaveChanges();
            }

            return Ok(payment);
        }


        [HttpPost]
        [Route("AddInstallment")]
        public IHttpActionResult AddInstallment(PaymentHistoryDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var payment = db.Payment.Find(dto.PaymentID);
            if (payment == null)
                return NotFound();

            // Add new installment to history
            var history = new PaymentHistory
            {
                PaymentID = dto.PaymentID,
                AmountPaid = dto.AmountPaid,
                PaymentMethod = dto.PaymentMethod,
                ReferenceNo = dto.ReferenceNo,
                Remarks = dto.Remarks,
                PaymentDate = DateTime.Now,
                CreatedBy = dto.CreatedBy,
                CreatedDate = DateTime.Now
            };

            db.PaymentHistory.Add(history);

            // Update master table
            payment.PaidAmount += dto.AmountPaid;
            payment.UpdatedDate = DateTime.Now;
            payment.UpdatedBy = dto.CreatedBy;

            // Update status
            payment.Status = payment.PaidAmount == 0 ? "PENDING" :
                             payment.PaidAmount < payment.TotalAmount ? "PARTIAL" : "PAID";

            db.SaveChanges();

            return Ok(history);
        }


        [HttpGet]
        [Route("{id}")]
        public IHttpActionResult GetPayment(int id)
        {
            var payment = db.Payment
                .Where(p => p.PaymentID == id)
                .Select(p => new
                {
                    p.PaymentID,
                    p.EnrollmentID,
                    p.TotalAmount,
                    p.PaidAmount,
                    p.BalanceAmount,
                    p.Status,
                    History = db.PaymentHistory
                                 .Where(h => h.PaymentID == p.PaymentID)
                                 .OrderByDescending(h => h.PaymentHistoryID)
                                 .ToList()
                })
                .FirstOrDefault();

            if (payment == null)
                return NotFound();

            return Ok(payment);
        }

        //[HttpGet]
        //[Route("All")]
        //public IHttpActionResult GetAllPayment()
        //{
        //    var payments = db.Payment
        //        .Select(p => new
        //        {
        //            p.PaymentID,
        //            p.EnrollmentID,
        //            p.TotalAmount,
        //            p.PaidAmount,
        //            p.BalanceAmount,
        //            p.Status,
        //            History = db.PaymentHistory
        //                         .Where(h => h.PaymentID == p.PaymentID)
        //                         .OrderByDescending(h => h.PaymentHistoryID)
        //                         .ToList()
        //        })
        //        .ToList();   // Get ALL records correctly

        //    if (!payments.Any())
        //        return NotFound();

        //    return Ok(payments);
        //}
        //[HttpGet]
        //[Route("All")]
        //public IHttpActionResult GetAllPayment()
        //{
        //    var payments = db.Payment
        //        .Select(p => new
        //        {
        //            p.PaymentID,
        //            p.EnrollmentID,
        //            p.TotalAmount,
        //            p.PaidAmount,
        //            p.BalanceAmount,
        //            p.Status,

        //            // Get enrollment row
        //            Enrollment = db.Enrollments
        //                           .Where(e => e.EnrollmentID == p.EnrollmentID)
        //                           .Select(e => new
        //                           {
        //                               e.EnrollmentID,
        //                               e.StudentID,

        //                               // Get student user details
        //                               Student = db.Users
        //                                           .Where(u => u.UserID == e.StudentID)
        //                                           .Select(u => new
        //                                           {
        //                                               u.UserID,
        //                                               u.Username,
        //                                               u.FirstName,
        //                                               u.LastName,
        //                                               u.Email,
        //                                               u.UserTypeID
        //                                           })
        //                                           .FirstOrDefault()
        //                           })
        //                           .FirstOrDefault(),

        //            // payment history
        //            History = db.PaymentHistory
        //                        .Where(h => h.PaymentID == p.PaymentID)
        //                        .OrderByDescending(h => h.PaymentHistoryID)
        //                        .ToList()
        //        })
        //        .ToList();

        //    if (!payments.Any())
        //        return NotFound();

        //    return Ok(payments);
        //}



        //achira

        [HttpGet]
        [Route("All")]
        public IHttpActionResult GetAllPayment()
        {
            var payments = db.Payment
                .Where(p => db.Enrollments
                              .Any(e => e.EnrollmentID == p.EnrollmentID && e.IsActive))
                .Select(p => new
                {
                    p.PaymentID,
                    p.EnrollmentID,
                    p.TotalAmount,
                    p.PaidAmount,
                    p.BalanceAmount,
                    p.Status,
                    // Get enrollment row
                    Enrollment = db.Enrollments
                                   .Where(e => e.EnrollmentID == p.EnrollmentID)
                                   .Select(e => new
                                   {
                                       e.EnrollmentID,
                                       e.StudentID,
                                       e.IsActive,
                                       // Get student user details
                                       Student = db.Users
                                                   .Where(u => u.UserID == e.StudentID)
                                                   .Select(u => new
                                                   {
                                                       u.UserID,
                                                       u.Username,
                                                       u.FirstName,
                                                       u.LastName,
                                                       u.Email,
                                                       u.UserTypeID,
                                                       u.IsActive
                                                   })
                                                   .FirstOrDefault(),
                                       // Get course name only
                                       CourseName = db.Courses
                                                      .Where(c => c.CourseID == e.CourseID)
                                                      .Select(c => c.CourseName)
                                                      .FirstOrDefault(),
                                       // Get subject name only
                                       SubjectName = db.Subjects
                                                       .Where(s => s.SubjectID == e.SubjectID)
                                                       .Select(s => s.SubjectName)
                                                       .FirstOrDefault()
                                   })
                                   .FirstOrDefault(),
                    // payment history
                    History = db.PaymentHistory
                                .Where(h => h.PaymentID == p.PaymentID)
                                .OrderByDescending(h => h.PaymentHistoryID)
                                .ToList()
                })
                .ToList();
            if (!payments.Any())
                return NotFound();
            return Ok(payments);
        }


        //[HttpGet]
        //[Route("teacher/{teacherId}")]
        //public IHttpActionResult GetPaymentsByTeacher(int teacherId)
        //{
        //    var payments =
        //        (from pay in db.Payment
        //         join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
        //         join course in db.Courses on enr.CourseID equals course.CourseID
        //         join teacher in db.Teachers on course.TeacherID equals teacher.TeacherID
        //         join userTeacher in db.Users on teacher.TeacherID equals userTeacher.UserID // Teacher name
        //         join student in db.Students on enr.StudentID equals student.StudentID
        //         join userStudent in db.Users on student.StudentID equals userStudent.UserID // Student name
        //         where course.TeacherID == teacherId && userTeacher.UserTypeID == 2 && userStudent.UserTypeID == 3
        //         select new
        //         {
        //             pay.PaymentID,
        //             pay.EnrollmentID,
        //             pay.TotalAmount,
        //             pay.PaidAmount,
        //             pay.BalanceAmount,
        //             pay.Status,

        //             TeacherID = course.TeacherID,
        //             TeacherName = userTeacher.FirstName + " " + userTeacher.LastName,

        //             StudentID = student.StudentID,
        //             StudentName = userStudent.FirstName + " " + userStudent.LastName,
        //             ParentName = student.ParentName,
        //             ParentContact = student.ParentContact,

        //             History = db.PaymentHistory
        //                       .Where(h => h.PaymentID == pay.PaymentID)
        //                       .OrderByDescending(h => h.PaymentHistoryID)
        //                       .ToList()
        //         })
        //        .ToList();

        //    return Ok(payments);
        //}



        //Achira

        //[HttpGet]
        //[Route("teacher/{teacherId}")]
        //public IHttpActionResult GetPaymentsByTeacher(int teacherId)
        //{
        //    var payments =
        //        (from pay in db.Payment
        //         join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
        //         join course in db.Courses on enr.CourseID equals course.CourseID
        //         join teacher in db.Teachers on course.TeacherID equals teacher.TeacherID
        //         join userTeacher in db.Users on teacher.TeacherID equals userTeacher.UserID
        //         join student in db.Students on enr.StudentID equals student.StudentID
        //         join userStudent in db.Users on student.StudentID equals userStudent.UserID
        //         where course.TeacherID == teacherId && userTeacher.UserTypeID == 2 && userStudent.UserTypeID == 3
        //         select new
        //         {
        //             pay.PaymentID,
        //             pay.EnrollmentID,
        //             pay.TotalAmount,
        //             pay.PaidAmount,
        //             pay.BalanceAmount,
        //             pay.Status,
        //             TeacherID = course.TeacherID,
        //             TeacherName = userTeacher.FirstName + " " + userTeacher.LastName,
        //             StudentID = student.StudentID,
        //             StudentName = userStudent.FirstName + " " + userStudent.LastName,
        //             StudentIsActive = userStudent.IsActive,
        //             ParentName = student.ParentName,
        //             ParentContact = student.ParentContact,
        //             CourseName = course.CourseName,
        //             SubjectName = db.Subjects
        //                             .Where(s => s.SubjectID == enr.SubjectID)
        //                             .Select(s => s.SubjectName)
        //                             .FirstOrDefault(),
        //             History = db.PaymentHistory
        //                       .Where(h => h.PaymentID == pay.PaymentID)
        //                       .OrderByDescending(h => h.PaymentHistoryID)
        //                       .ToList()
        //         })
        //        .ToList();
        //    return Ok(payments);
        //}

        [HttpGet]
        [Route("teacher/{teacherId}")]
        public IHttpActionResult GetPaymentsByTeacher(int teacherId)
        {
            var payments =
                (from pay in db.Payment
                 join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
                 join course in db.Courses on enr.CourseID equals course.CourseID
                 join teacher in db.Teachers on course.TeacherID equals teacher.TeacherID
                 join userTeacher in db.Users on teacher.TeacherID equals userTeacher.UserID
                 join student in db.Students on enr.StudentID equals student.StudentID
                 join userStudent in db.Users on student.StudentID equals userStudent.UserID
                 where course.TeacherID == teacherId
                       && enr.IsActive
                       && userTeacher.UserTypeID == 2
                       && userStudent.UserTypeID == 3
                 select new
                 {
                     pay.PaymentID,
                     pay.EnrollmentID,
                     pay.TotalAmount,
                     pay.PaidAmount,
                     pay.BalanceAmount,
                     pay.Status,
                     TeacherID = course.TeacherID,
                     TeacherName = userTeacher.FirstName + " " + userTeacher.LastName,
                     StudentID = student.StudentID,
                     StudentName = userStudent.FirstName + " " + userStudent.LastName,
                     StudentIsActive = userStudent.IsActive,
                     ParentName = student.ParentName,
                     ParentContact = student.ParentContact,
                     CourseName = course.CourseName,
                     SubjectName = db.Subjects
                                     .Where(s => s.SubjectID == enr.SubjectID)
                                     .Select(s => s.SubjectName)
                                     .FirstOrDefault(),
                     History = db.PaymentHistory
                               .Where(h => h.PaymentID == pay.PaymentID)
                               .OrderByDescending(h => h.PaymentHistoryID)
                               .ToList()
                 })
                .ToList();
            return Ok(payments);
        }

        //[HttpGet]
        //[Route("student/{studentId}")]
        //public IHttpActionResult GetPaymentsByStudent(int studentId)
        //{
        //    var payments =
        //        (from pay in db.Payment
        //         join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
        //         join student in db.Students on enr.StudentID equals student.StudentID
        //         join user in db.Users on student.StudentID equals user.UserID   // get student name
        //         where enr.StudentID == studentId && user.UserTypeID == 3
        //         select new
        //         {
        //             pay.PaymentID,
        //             pay.EnrollmentID,
        //             pay.TotalAmount,
        //             pay.PaidAmount,
        //             pay.BalanceAmount,
        //             pay.Status,
        //             StudentID = student.StudentID,
        //             StudentName = user.FirstName + " " + user.LastName,
        //             ParentName = student.ParentName,
        //             ParentContact = student.ParentContact,

        //             History = db.PaymentHistory
        //                       .Where(h => h.PaymentID == pay.PaymentID)
        //                       .OrderByDescending(h => h.PaymentHistoryID)
        //                       .ToList()
        //         })
        //        .ToList();

        //    return Ok(payments);
        //}

        //[HttpGet]
        //[Route("student/{studentId}")]
        //public IHttpActionResult GetPaymentsByStudent(int studentId)
        //{
        //    var payments =
        //        (from pay in db.Payment
        //         join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
        //         join student in db.Students on enr.StudentID equals student.StudentID
        //         join userStudent in db.Users on student.StudentID equals userStudent.UserID   // Student name
        //         join course in db.Courses on enr.CourseID equals course.CourseID
        //         join teacher in db.Teachers on course.TeacherID equals teacher.TeacherID
        //         join userTeacher in db.Users on teacher.TeacherID equals userTeacher.UserID  // Teacher name
        //         where enr.StudentID == studentId && userStudent.UserTypeID == 3 && userTeacher.UserTypeID == 2
        //         select new
        //         {
        //             pay.PaymentID,
        //             pay.EnrollmentID,
        //             pay.TotalAmount,
        //             pay.PaidAmount,
        //             pay.BalanceAmount,
        //             pay.Status,

        //             StudentID = student.StudentID,
        //             StudentName = userStudent.FirstName + " " + userStudent.LastName,
        //             ParentName = student.ParentName,
        //             ParentContact = student.ParentContact,

        //             TeacherID = course.TeacherID,
        //             TeacherName = userTeacher.FirstName + " " + userTeacher.LastName,

        //             History = db.PaymentHistory
        //                       .Where(h => h.PaymentID == pay.PaymentID)
        //                       .OrderByDescending(h => h.PaymentHistoryID)
        //                       .ToList()
        //         })
        //        .ToList();

        //    return Ok(payments);
        //}



        //achira

        //[HttpGet]
        //[Route("student/{studentId}")]
        //public IHttpActionResult GetPaymentsByStudent(int studentId)
        //{
        //    var payments =
        //        (from pay in db.Payment
        //         join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
        //         join student in db.Students on enr.StudentID equals student.StudentID
        //         join userStudent in db.Users on student.StudentID equals userStudent.UserID   // Student name
        //         join course in db.Courses on enr.CourseID equals course.CourseID
        //         join teacher in db.Teachers on course.TeacherID equals teacher.TeacherID
        //         join userTeacher in db.Users on teacher.TeacherID equals userTeacher.UserID  // Teacher name
        //         where enr.StudentID == studentId
        //               && enr.IsActive
        //               && userStudent.UserTypeID == 3
        //               && userTeacher.UserTypeID == 2
        //         select new
        //         {
        //             pay.PaymentID,
        //             pay.EnrollmentID,
        //             pay.TotalAmount,
        //             pay.PaidAmount,
        //             pay.BalanceAmount,
        //             pay.Status,
        //             StudentID = student.StudentID,
        //             StudentName = userStudent.FirstName + " " + userStudent.LastName,
        //             ParentName = student.ParentName,
        //             ParentContact = student.ParentContact,
        //             TeacherID = course.TeacherID,
        //             TeacherName = userTeacher.FirstName + " " + userTeacher.LastName,
        //             History = db.PaymentHistory
        //                       .Where(h => h.PaymentID == pay.PaymentID)
        //                       .OrderByDescending(h => h.PaymentHistoryID)
        //                       .ToList()
        //         })
        //        .ToList();
        //    return Ok(payments);
        //}

        [HttpGet]
        [Route("student/{studentId}")]
        public IHttpActionResult GetPaymentsByStudent(int studentId)
        {
            var payments =
                (from pay in db.Payment
                 join enr in db.Enrollments on pay.EnrollmentID equals enr.EnrollmentID
                 join student in db.Students on enr.StudentID equals student.StudentID
                 join userStudent in db.Users on student.StudentID equals userStudent.UserID   // Student name
                 join course in db.Courses on enr.CourseID equals course.CourseID
                 join teacher in db.Teachers on course.TeacherID equals teacher.TeacherID
                 join userTeacher in db.Users on teacher.TeacherID equals userTeacher.UserID  // Teacher name
                 where enr.StudentID == studentId
                       && enr.IsActive
                       && userStudent.UserTypeID == 3
                       && userTeacher.UserTypeID == 2
                 select new
                 {
                     pay.PaymentID,
                     pay.EnrollmentID,
                     pay.TotalAmount,
                     pay.PaidAmount,
                     pay.BalanceAmount,
                     pay.Status,
                     StudentID = student.StudentID,
                     StudentName = userStudent.FirstName + " " + userStudent.LastName,
                     ParentName = student.ParentName,
                     ParentContact = student.ParentContact,
                     TeacherID = course.TeacherID,
                     TeacherName = userTeacher.FirstName + " " + userTeacher.LastName,
                     CourseName = course.CourseName,          // tied to enr.CourseID via the course join
                     SubjectName = db.Subjects
                                     .Where(s => s.SubjectID == enr.SubjectID)   // tied to enr.SubjectID
                                     .Select(s => s.SubjectName)
                                     .FirstOrDefault(),
                     History = db.PaymentHistory
                               .Where(h => h.PaymentID == pay.PaymentID)
                               .OrderByDescending(h => h.PaymentHistoryID)
                               .ToList()
                 })
                .ToList();
            return Ok(payments);
        }






    }
}
