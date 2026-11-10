using ClassSystemAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.RightsManagement;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    public class WebsiteCoursesController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public WebsiteCoursesController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        public IHttpActionResult GetWebsiteCourses()
        {
            var courses = db.WebsiteCourses.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.WebsiteCourseID).ToList();
            return Ok(courses);
        }

        [HttpGet]
        [Route("api/WebsiteCourses/All")]
        public IHttpActionResult GetAllWebsiteCourses()
        {
            var courses = db.WebsiteCourses.OrderBy(c => c.DisplayOrder).ThenBy(c => c.WebsiteCourseID).ToList();
            return Ok(courses); 
        }

        public IHttpActionResult PostWebsiteCourses(WebsiteCourse course)
        {
            if (course == null)
                return BadRequest("No Course data was sent");
            if(!ModelState.IsValid)
                return BadRequest(ModelState);
            course.WebsiteCourseID = 0;
            course.CreatedDate = DateTime.Now;  

            db.WebsiteCourses.Add(course);
            db.SaveChanges();

            return Ok();
        }

        public IHttpActionResult PutWebsiteCourse(int id, WebsiteCourse course)
        {
            if (course == null)
                return BadRequest("No course data was sent");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != course.WebsiteCourseID)
                return BadRequest("The id does not match the course");

            var existing = db.WebsiteCourses.Find(id);
            if (existing == null)
                return NotFound();

            existing.Name = course.Name;
            existing.Level = course.Level;
            existing.Description = course.Description;
            existing.Duration = course.Duration;
            existing.Schedule = course.Schedule;
            existing.TeacherName = course.TeacherName;
            existing.WeeklyFee = course.WeeklyFee;
            existing.DisplayOrder = course.DisplayOrder;
            existing.IsActive = course.IsActive;

            db.SaveChanges();

            return Ok(existing);
        }

        public IHttpActionResult DeleteWebsiteCourse(int id)
        {
            var existing = db.WebsiteCourses.Find(id);
            if (existing == null)
                return NotFound();

            db.WebsiteCourses.Remove(existing);
            db.SaveChanges();

            return Ok();
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
