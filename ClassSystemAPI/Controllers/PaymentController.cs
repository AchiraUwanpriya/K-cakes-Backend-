using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    public class PaymentAccessInfo
    {
        public int EnrollmentID { get; set; }
        public int CourseID { get; set; }
        public int SubjectID { get; set; }
        public String CourseName { get; set; }
        public String SubjectName { get; set; }
        public bool Blocked { get; set; }
        public bool ShowReminder { get; set; }
        public int WeeksDue { get; set; }
        public int WeeksPaid { get; set; }
        public int DueDay { get; set; }
        public DateTime? NextDueDate { get; set; }
        public String Message { get; set; }
    }

    public static class PaymentAccess
    {
        // The MonthlyFee column holds the WEEKLY fee.
        public static PaymentAccessInfo Check(ClassSystemContext db, Enrollment enr, DateTime now)
        {
            var subject = db.Subjects.Find(enr.SubjectID);
            var course = db.Courses.Find(enr.CourseID);
            var payment = db.Payment.FirstOrDefault(p => p.EnrollmentID == enr.EnrollmentID);

            var info = new PaymentAccessInfo
            {
                EnrollmentID = enr.EnrollmentID,
                CourseID = enr.CourseID,
                SubjectID = enr.SubjectID,
                CourseName = course != null ? course.CourseName : null,
                SubjectName = subject != null ? subject.SubjectName : null,
                DueDay = 6
            };

            decimal weeklyFee = subject != null && subject.MonthlyFee.HasValue ? subject.MonthlyFee.Value : 0m;
            if (subject == null || payment == null || weeklyFee <= 0) return info;   // nothing to enforce
            if (enr.EnrollmentDate.Year < 2000) return info;

            int dueDay = subject.PaymentDueDay.HasValue ? subject.PaymentDueDay.Value : 6;
            if (dueDay < 0 || dueDay > 6) dueDay = 6;
            info.DueDay = dueDay;

            // Fully paid enrollments are never blocked
            if (payment.TotalAmount > 0 && payment.PaidAmount >= payment.TotalAmount) return info;

            var today = now.Date;
            var joined = enr.EnrollmentDate.Date;

            // First due date = next due weekday on or after the join date (days before it are free)
            var firstDue = joined.AddDays(((dueDay - (int)joined.DayOfWeek) + 7) % 7);
            int weeksDue = today < firstDue ? 0 : 1 + (int)((today - firstDue).TotalDays / 7);
            int weeksPaid = (int)Math.Floor(payment.PaidAmount / weeklyFee);
            var nextDue = firstDue.AddDays(7 * weeksDue);

            info.WeeksDue = weeksDue;
            info.WeeksPaid = weeksPaid;
            info.NextDueDate = nextDue;
            info.Blocked = weeksPaid < weeksDue;
            info.ShowReminder = !info.Blocked && (nextDue - today).TotalDays <= 2 && weeksPaid < weeksDue + 1;

            var name = string.IsNullOrEmpty(info.SubjectName) ? "this class" : info.SubjectName;
            if (info.Blocked)
                info.Message = "Your weekly payment for " + name + " is overdue. Please pay your teacher to continue.";
            else if (info.ShowReminder)
                info.Message = "Reminder: the weekly payment for " + name + " is due on " + nextDue.ToString("dddd, dd MMM") + ".";

            return info;
        }
    }

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

            if (dto.AmountPaid <= 0)
                return BadRequest("Please enter a valid amount greater than 0.");

            var balanceLeft = payment.TotalAmount - payment.PaidAmount;
            if (dto.AmountPaid > balanceLeft)
                return BadRequest("Amount exceeds the remaining balance.");

            string weeksNote = null;
            var enrollment = db.Enrollments.Find(payment.EnrollmentID);
            var subject = enrollment == null ? null : db.Subjects.Find(enrollment.SubjectID);
            var weeklyFee = subject != null && subject.MonthlyFee.HasValue ? subject.MonthlyFee.Value : 0m;

            if(weeklyFee > 0)
            {
                var isFinalPayment = dto.AmountPaid == balanceLeft;
                if (!isFinalPayment && dto.AmountPaid % weeklyFee != 0)
                {
                    return BadRequest("Partial payments are not accepted. The weekly fee is"
                        + weeklyFee.ToString("N2") + "LKR. Please Pay."
                        + weeklyFee.ToString("N2") + "for one week,"
                        + (weeklyFee * 2).ToString("N2") + "for two weeks, and so on.");
                }

                var weeksPaid = (int)Math.Floor(dto.AmountPaid / weeklyFee);
                if(weeksPaid > 0)
                    weeksNote = "Paid for" + weeksPaid + "week" + (weeksPaid > 1 ? "s" : "");
            }

            var history = new PaymentHistory
            {
                PaymentID = dto.PaymentID,
                AmountPaid = dto.AmountPaid,
                PaymentMethod = dto.PaymentMethod,
                ReferenceNo = dto.ReferenceNo,
                Remarks = string.IsNullOrWhiteSpace(dto.Remarks) ? weeksNote : (weeksNote == null ? dto.Remarks : dto.Remarks + " | " + weeksNote),
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
        [Route("student/{studentId}/access")]
        public IHttpActionResult GetAccessByStudent(int studentId)
        {
            var now = DateTime.Now;
            var enrollments = db.Enrollments
                .Where(e => e.StudentID == studentId && e.IsActive)
                .ToList();
            var result = enrollments.Select(e => PaymentAccess.Check(db, e, now)).ToList();
            return Ok(result);
        }

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
