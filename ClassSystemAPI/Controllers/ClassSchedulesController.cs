using ClassSystemAPI.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    public class ClassSchedulesController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public ClassSchedulesController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/ClassSchedules
        public IQueryable<ClassSchedule> GetClassSchedules()
        {
            // Now include both Course and Subject
            return db.ClassSchedules
                     .Include(cs => cs.Course)
                     .Include(cs => cs.Subject);
        }

        // GET: api/ClassSchedules/5
        public IHttpActionResult GetClassSchedule(int id)
        {
            // Use Include here as well so Subject & Course come in one shot
            var schedule = db.ClassSchedules
                             .Include(cs => cs.Course)
                             .Include(cs => cs.Subject)
                             .FirstOrDefault(cs => cs.ScheduleID == id);

            if (schedule == null)
                return NotFound();

            return Ok(schedule);
        }

        [HttpGet]
        [Route("api/ClassSchedules/GetClassScheduleByDate")]
        public IHttpActionResult GetClassScheduleByDate(DateTime classDate)
        {
            // Filter schedules by ClassDate only
            var schedules = db.ClassSchedules
                              .Include(cs => cs.Course)
                              .Include(cs => cs.Subject)
                              .Where(cs => DbFunctions.TruncateTime(cs.ClassDate) == DbFunctions.TruncateTime(classDate))
                              .ToList();

            if (!schedules.Any())
                return NotFound();

            return Ok(schedules);
        }

        // POST: api/ClassSchedules
        public IHttpActionResult PostClassSchedule(ClassSchedule classSchedule)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // classSchedule now can contain CourseID and SubjectID
            db.ClassSchedules.Add(classSchedule);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = classSchedule.ScheduleID }, classSchedule);
        }

        // PUT: api/ClassSchedules/5
        public IHttpActionResult PutClassSchedule(int id, ClassSchedule classSchedule)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != classSchedule.ScheduleID)
                return BadRequest();

            db.Entry(classSchedule).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch
            {
                return NotFound();
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // DELETE: api/ClassSchedules/5
        public IHttpActionResult DeleteClassSchedule(int id)
        {
            var schedule = db.ClassSchedules.Find(id);
            if (schedule == null)
                return NotFound();

            db.ClassSchedules.Remove(schedule);
            db.SaveChanges();

            return Ok(schedule);
        }
    }
}
