using ClassSystemAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    [RoutePrefix("api/Reports")]
    public class ReportsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public ReportsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        private void AddLog(int? userId, string action, string details = null)
        {
            var log = new SystemLog
            {
                UserID = userId,
                Action = action,
                Details = details,
                IPAddress = GetClientIpAddress(),
                LogTime = DateTime.Now
            };
            db.SystemLogs.Add(log);
            db.SaveChanges();
        }

        private string GetClientIpAddress()
        {
            if (System.Web.HttpContext.Current == null)
                return null;

            var request = System.Web.HttpContext.Current.Request;
            var ipAddress = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ipAddress))
                return request.ServerVariables["REMOTE_ADDR"];

            return ipAddress.Split(',')[0];
        }


        [HttpGet]
        [Route("student/{id:int}")]
        public IHttpActionResult GetStudentReport(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                AddLog(null, "GetStudentReportFailed", $"User with ID {id} not found");
                return NotFound();
            }

            var activeEnrollments = db.Enrollments
                                      .Where(e => e.StudentID == id && e.IsActive)
                                      .ToList();

            if (!activeEnrollments.Any())
            {
                return Ok(new
                {
                    studentId = user.UserID,
                    studentName = $"{user.FirstName} {user.LastName}".Trim(),
                    email = user.Email,
                    reports = new List<object>()
                });
            }

            var enrollmentIds = activeEnrollments.Select(e => e.EnrollmentID).ToList();

            var courses = db.Courses.ToList();
            var subjects = db.Subjects.ToList();

            var payments = db.Payment
                             .Where(p => enrollmentIds.Contains(p.EnrollmentID))
                             .ToList();

            // Fetch PaymentHistories for all payments
            var paymentIds = payments.Select(p => p.PaymentID).ToList();
            var paymentHistories = db.PaymentHistory
                                     .Where(ph => paymentIds.Contains(ph.PaymentID))
                                     .ToList();

            var report = activeEnrollments.Select(e =>
            {
                var course = courses.FirstOrDefault(c => c.CourseID == e.CourseID);
                var subject = subjects.FirstOrDefault(s => s.SubjectID == e.SubjectID);
                var payment = payments.FirstOrDefault(p => p.EnrollmentID == e.EnrollmentID);

                return new
                {
                    CourseName = course?.CourseName,
                    SubjectName = subject?.SubjectName,
                    StartDate = e.EnrollmentDate,
                    Payment = payment == null ? null : new
                    {
                        payment.PaymentID,
                        payment.TotalAmount,
                        payment.PaidAmount,
                        payment.BalanceAmount,
                        payment.Status,
                        payment.CreatedDate,

                        // Add PaymentHistories
                        PaymentHistory = paymentHistories
                            .Where(ph => ph.PaymentID == payment.PaymentID)
                            .Select(ph => new
                            {
                                ph.PaymentHistoryID,
                                ph.PaymentDate,
                                ph.AmountPaid,
                                ph.PaymentMethod,
                                ph.ReferenceNo,
                                ph.Remarks,
                                ph.CreatedDate
                            })
                            .OrderByDescending(ph => ph.PaymentDate)
                            .ToList()
                    }
                };
            }).ToList();

            AddLog(null, "GetStudentReport", $"Retrieved student report for ID {id}");

            return Ok(new
            {
                studentId = user.UserID,
                studentName = $"{user.FirstName} {user.LastName}".Trim(),
                email = user.Email,
                reports = report
            });
        }


        [HttpGet]
        [Route("students")]
        public IHttpActionResult GetAllStudentReports()
        {
            var students = (from u in db.Users
                            join s in db.Students on u.UserID equals s.StudentID
                            where u.UserTypeID == 3 && u.IsActive
                            select new
                            {
                                User = u,
                                StudentDetails = s
                            }).ToList();

            if (!students.Any())
            {
                AddLog(null, "GetAllStudentReports", "No students found");
                return Ok(new List<object>());
            }

            var studentIds = students.Select(s => s.User.UserID).ToList();

            var enrollments = db.Enrollments
                                .Where(e => studentIds.Contains(e.StudentID) && e.IsActive)
                                .ToList();

            var enrollmentIds = enrollments.Select(e => e.EnrollmentID).ToList();

            var courses = db.Courses.ToList();
            var subjects = db.Subjects.ToList();
            var payments = db.Payment
                             .Where(p => enrollmentIds.Contains(p.EnrollmentID))
                             .ToList();

            // Fetch PaymentHistories for all payments
            var paymentIds = payments.Select(p => p.PaymentID).ToList();
            var paymentHistories = db.PaymentHistory
                                     .Where(ph => paymentIds.Contains(ph.PaymentID))
                                     .ToList();

            var report = students.Select(student =>
            {
                var studentReports = enrollments
                    .Where(e => e.StudentID == student.User.UserID)
                    .Select(e =>
                    {
                        var course = courses.FirstOrDefault(c => c.CourseID == e.CourseID);
                        var subject = subjects.FirstOrDefault(s => s.SubjectID == e.SubjectID);
                        var payment = payments.FirstOrDefault(p => p.EnrollmentID == e.EnrollmentID);

                        return new
                        {
                            CourseName = course?.CourseName,
                            SubjectName = subject?.SubjectName,
                            StartDate = e.EnrollmentDate,

                            Payment = payment == null ? null : new
                            {
                                payment.TotalAmount,
                                payment.PaidAmount,
                                payment.BalanceAmount,
                                payment.Status,
                                payment.CreatedDate,

                                // PaymentHistories
                                PaymentHistory = paymentHistories
                                    .Where(ph => ph.PaymentID == payment.PaymentID)
                                    .Select(ph => new
                                    {
                                        ph.PaymentHistoryID,
                                        ph.PaymentDate,
                                        ph.AmountPaid,
                                        ph.PaymentMethod,
                                        ph.ReferenceNo,
                                        ph.Remarks,
                                        ph.CreatedDate
                                    })
                                    .OrderByDescending(ph => ph.PaymentDate)
                                    .ToList()
                            }
                        };
                    }).ToList();

                return new
                {
                    StudentID = student.User.UserID,
                    StudentName = $"{student.User.FirstName} {student.User.LastName}".Trim(),
                    student.User.Email,
                    student.StudentDetails.ParentName,
                    student.StudentDetails.ParentContact,
                    Reports = studentReports
                };
            }).ToList();

            AddLog(null, "GetAllStudentReports", "Retrieved all student reports");

            return Ok(report);
        }

    }
}
