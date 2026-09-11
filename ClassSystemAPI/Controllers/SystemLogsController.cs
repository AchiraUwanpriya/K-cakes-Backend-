// Controllers/SystemLogsController.cs
using ClassSystemAPI.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace ClassSystemAPI.Controllers
{
    //[Authorize(Roles = "Admin")]  // Restrict to admin only
    public class SystemLogsController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public SystemLogsController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }

        // GET: api/SystemLogs
        public IHttpActionResult GetSystemLogs()
        {
            var logs = db.SystemLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.LogTime)
                .ToList();
            return Ok(logs);
        }

        // GET: api/SystemLogs/5
        public IHttpActionResult GetSystemLog(int id)
        {
            SystemLog systemLog = db.SystemLogs
                .Include(l => l.User)
                .FirstOrDefault(l => l.LogID == id);
            if (systemLog == null)
            {
                return NotFound();
            }

            return Ok(systemLog);
        }

        // GET: api/SystemLogs/ByUser/5
        [Route("api/SystemLogs/ByUser/{userId}")]
        public IHttpActionResult GetLogsByUser(int userId)
        {
            var logs = db.SystemLogs
                .Where(l => l.UserID == userId)
                .OrderByDescending(l => l.LogTime)
                .ToList();
            return Ok(logs);
        }

        // GET: api/SystemLogs/Search?action=login
        [Route("api/SystemLogs/Search")]
        public IHttpActionResult GetLogsByAction(string action)
        {
            var logs = db.SystemLogs
                .Where(l => l.Action.Contains(action))
                .OrderByDescending(l => l.LogTime)
                .ToList();
            return Ok(logs);
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