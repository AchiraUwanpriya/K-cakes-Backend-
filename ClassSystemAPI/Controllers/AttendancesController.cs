using System;
using System.Data.Entity;
using System.Data.Entity.Spatial;
using System.Linq;
using System.Net;
using System.Web.Http;
using ClassSystemAPI.Models;

namespace ClassSystemAPI.Controllers
{
    public class AttendancesController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();
        public AttendancesController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/Attendances
        public IHttpActionResult GetAttendances()
        {
            var attendances = db.Attendances
            //.Include(a => a.QRSession)
                .Include(a => a.Student)
                .ToList();
            return Ok(attendances);
        }

        // GET: api/Attendances/5
        public IHttpActionResult GetAttendance(int id)
        {
            var attendance = db.Attendances
            // .Include(a => a.QRSession)
                .Include(a => a.Student)
                .FirstOrDefault(a => a.AttendanceID == id);

            if (attendance == null)
            {
                return NotFound();
            }

            return Ok(attendance);
        }

        //post attendance
        public IHttpActionResult PostAttendance(Attendance attendance)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int? courseId = attendance.CourseID;
            if (courseId == null)
            {
                var qr = db.QRSessions.FirstOrDefault(q => q.SessionID == attendance.SessionID);
                if (qr != null) courseId = qr.CourseID;
            }

            if(courseId != null)
            {
                var enrollments = db.Enrollments.Where(e=>e.StudentID == attendance.StudentID && e.CourseID == courseId.Value && e.IsActive).ToList();
                if(attendance.SubjectID != null)
                {
                    enrollments = enrollments.Where(e => e.SubjectID == attendance.SubjectID.Value).ToList();
                }
                foreach(var enr in enrollments)
                {
                    var access = PaymentAccess.Check(db, enr, DateTime.Now);
                    if (access.Blocked)
                    {
                        return Content(HttpStatusCode.Forbidden, new {message = access.Message});
                    }
                }
            }

            attendance.ScanTime = DateTime.Now;
            attendance.Status = "Present";

            db.Attendances.Add(attendance);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = attendance.AttendanceID }, attendance);
        }

        // POST: api/Attendances/Record - Record attendance via QR code
        [Route("api/Attendances/Record")]
        public IHttpActionResult RecordAttendance(RecordAttendanceRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find the active session
            var session = db.QRSessions
                .FirstOrDefault(q => q.QRCodeData == request.QRCodeData &&
                                   q.IsActive &&
                                   DateTime.Now >= q.StartTime &&
                                   DateTime.Now <= q.ExpiryTime);

            if (session == null)
            {
                return BadRequest("Invalid or expired QR session");
            }

            //weekly payment check(per class)
            foreach (var enr in db.Enrollments.Where(e => e.StudentID == request.StudentID && e.CourseID == session.CourseID && e.IsActive).ToList())
            {
                var access = PaymentAccess.Check(db, enr, DateTime.Now);
                if(access.Blocked)
                    return Content(HttpStatusCode.Forbidden, new {message = access.Message});
            }

            // Check if attendance already recorded
            var existingAttendance = db.Attendances
                .FirstOrDefault(a => a.SessionID == session.SessionID &&
                                   a.StudentID == request.StudentID);

            if (existingAttendance != null)
            {
                return BadRequest("Attendance already recorded for this session");
            }

            // Create new attendance record
            var attendance = new Attendance
            {
                SessionID = session.SessionID,
                StudentID = request.StudentID,
                ScanTime = DateTime.Now,
                DeviceInfo = request.DeviceInfo,
                IPAddress = request.IPAddress,
                Location = request.Location,
                Status = DateTime.Now > session.EndTime ? "Late" : "Present"
            };

            db.Attendances.Add(attendance);
            db.SaveChanges();

            return Ok(attendance);
        }

        // GET: api/Attendances/Session/5 - Get attendances for a session
        [Route("api/Attendances/Session/{sessionId}")]
        public IHttpActionResult GetAttendancesBySession(int sessionId)
        {
            var attendances = db.Attendances
                .Where(a => a.SessionID == sessionId)
                .Include(a => a.Student)
                .ToList();

            return Ok(attendances);
        }

        // GET: api/Attendances/Student/5 - Get attendances for a student
        //[Route("api/Attendances/Student/{studentId}")]
        //public IHttpActionResult GetAttendancesByStudent(int studentId)
        //{
        //    var attendances = db.Attendances
        //        .Where(a => a.StudentID == studentId)
        //        .Include(a => a.QRSession)
        //        .Include(a => a.QRSession.Course)
        //        .ToList();

        //    return Ok(attendances);
        //}

      [Route("api/GetAtendBySheduleId")]
        //create by kusal gamage
        public IHttpActionResult GetAtendBySheduleId(int sheduleid, DateTime date)
        {
            var rawData = db.Attendances
                .Where(a => a.SessionID == sheduleid
                    && DbFunctions.TruncateTime(a.ScanTime) == date.Date)
                .GroupBy(a => a.StudentID)
                .Select(g => new
        {
            StudentID = g.Key,
            ScanTime = g.Min(x => x.ScanTime)
        })
            .ToList();
                if (!rawData.Any())
                    return NotFound();
                var result = rawData.Select(x => new
                {
                    x.StudentID,
                    ScanTime = x.ScanTime.ToString("yyyy-MM-dd HH:mm:ss")
                });
            return Ok(result);
        }

        [Route("api/GetAtendBySubject")]
        [HttpGet]
        public IHttpActionResult GetAtendBySubject(int subjectId, DateTime date)
        {
            // Get all ScheduleIDs for this subject
            var scheduleIds = db.ClassSchedules
                                .Where(s => s.SubjectID == subjectId)
                                .Select(s => s.ScheduleID)
                                .ToList();

            // Get attendances for these schedule IDs on the given date
            var rawData = db.Attendances
                            .Where(a => scheduleIds.Contains(a.SessionID)
                                        && DbFunctions.TruncateTime(a.ScanTime) == date.Date)
                            .GroupBy(a => a.StudentID)
                            .Select(g => new
                            {
                                StudentID = g.Key,
                                ScanTime = g.Min(x => x.ScanTime)
                            })
                            .ToList()
                            .Select(x => new
                            {
                                x.StudentID,
                                ScanTime = x.ScanTime.ToString("yyyy-MM-dd HH:mm:ss")
                            });

            if (!rawData.Any())
                return NotFound();

            return Ok(rawData);
        }





        [Route("api/GetAtendByStudentId")]
        // created by kusal gamage
        public IHttpActionResult GetAtendByStudentId(int sheduleid, int studentid)
        {
            var dailyFirstScans = db.Attendances
                .Where(a => a.SessionID == sheduleid && a.StudentID == studentid)
                .GroupBy(a => DbFunctions.TruncateTime(a.ScanTime)) 
                .Select(g => new
                {
                    ScanDate = g.Key, 
                    FirstScanTime = g.Min(x => x.ScanTime) 
                })
                .OrderBy(x => x.ScanDate)
                .ToList();

            if (!dailyFirstScans.Any())
                return NotFound();

            var result = dailyFirstScans.Select(x => new
            {
                ScanDate = x.ScanDate.Value.ToString("yyyy-MM-dd"),
                FirstScanTime = x.FirstScanTime.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return Ok(result);
        }


        [Route("api/GetAtendByStudentAndSubject")]
        [HttpGet]
        public IHttpActionResult GetAtendByStudentAndSubject(int studentId, int subjectId)
        {
            //  Get all schedules for this subject
            var scheduleIds = db.ClassSchedules
                                .Where(s => s.SubjectID == subjectId)
                                .Select(s => s.ScheduleID)
                                .ToList();

            if (!scheduleIds.Any())
                return NotFound();

            // Get attendance for this student across all schedules for this subject
            var dailyFirstScans = db.Attendances
                .Where(a => scheduleIds.Contains(a.SessionID) && a.StudentID == studentId)
                .GroupBy(a => DbFunctions.TruncateTime(a.ScanTime))  // group by date
                .Select(g => new
                {
                    ScanDate = g.Key,
                    FirstScanTime = g.Min(x => x.ScanTime)
                })
                .OrderBy(x => x.ScanDate)
                .ToList();

            if (!dailyFirstScans.Any())
                return NotFound();

            //  Format result
            var result = dailyFirstScans.Select(x => new
            {
                ScanDate = x.ScanDate.Value.ToString("yyyy-MM-dd"),
                FirstScanTime = x.FirstScanTime.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return Ok(result);
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

    // Request model for recording attendance
    public class RecordAttendanceRequest
    {
        public string QRCodeData { get; set; }
        public int StudentID { get; set; }
        public string DeviceInfo { get; set; }
        public string IPAddress { get; set; }
        public DbGeography Location { get; set; }
    }
}