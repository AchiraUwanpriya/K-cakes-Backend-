using ClassSystemAPI.Models;
using ClassSystemAPI.Models.DTOs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Hosting;
using System.Web.Http;
using System.Web.Http.Cors;

namespace ClassSystemAPI.Controllers
{
    [EnableCors(origins: "http://localhost:3000,https://sweetofkcake.dtselife.com", headers: "*", methods: "*")]
    public class UsersController : ApiController
    {
        private ClassSystemContext db = new ClassSystemContext();

        public UsersController()
        {
            db.Configuration.LazyLoadingEnabled = false;
            db.Configuration.ProxyCreationEnabled = false;
        }



        // Helper method to add logs
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

            var addresses = ipAddress.Split(',');
            if (addresses.Length != 0)
                return addresses[0];

            return request.ServerVariables["REMOTE_ADDR"];
        }

        // login
        [HttpPost]
        [Route("api/Users/Login")]
        public IHttpActionResult Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                AddLog(null, "LoginFailed", "Invalid request model");
                return BadRequest(ModelState);
            }

            var user = db.Users
                         .Include(u => u.UserType)
                         .FirstOrDefault(u => u.Username == request.Username && u.PasswordHash == request.Password);

            if (user == null)
            {
                AddLog(null, "LoginFailed", $"Invalid credentials for username: {request.Username}");
                return Unauthorized();
            }

            // Prevent login if user is inactive — return structured JSON + 403
            if (!user.IsActive)
            {
                AddLog(user.UserID, "LoginBlockedInactive", $"Attempt to login by inactive user: {request.Username}");

                var responseObj = new
                {
                    error = "AccountInactive",
                    message = "Your account is inactive. Please contact the administrator to reactivate your account."
                };

                return Content(HttpStatusCode.Forbidden, responseObj);
            }

            // Update last login time
            user.LastLogin = DateTime.Now;
            db.SaveChanges();

            AddLog(user.UserID, "LoginSuccess", $"User '{request.Username}' logged in successfully");

            return Ok(new
            {
                user.UserID,
                user.Username,
                user.UserTypeID,
                UserTypeName = user.UserType?.TypeName
            });
        }
        // GET: api/Users
        public IHttpActionResult GetUsers()
        {
            var users = db.Users.Include("UserType").ToList();
            AddLog(null, "GetAllUsers", $"Retrieved {users.Count} users");
            return Ok(users);
        }

        // GET: api/Users/5
        //public IHttpActionResult GetUser(int id)
        //{
        //    var user = db.Users.Find(id);
        //    if (user == null)
        //    {
        //        AddLog(null, "GetUserFailed", $"User with ID {id} not found");
        //        return NotFound();
        //    }

        //    AddLog(null, "GetUser", $"Retrieved user with ID {id}");
        //    return Ok(user);
        //}



        // GET: api/Users/5
        //public IHttpActionResult GetUser(int id)
        //{
        //    // Find the user
        //    var user = db.Users.Find(id);
        //    if (user == null)
        //    {
        //        AddLog(null, "GetUserFailed", $"User with ID {id} not found");
        //        return NotFound();
        //    }

        //    // Get only active enrollments for this user
        //    var activeEnrollments = db.Enrollments
        //                              .Where(e => e.StudentID == id && e.IsActive )
        //                              .ToList(); // materialize

        //    if (!activeEnrollments.Any())
        //    {
        //        return Ok(new
        //        {
        //            userId = user.UserID,
        //            username = user.Username,
        //            courses = new List<object>()
        //        });
        //    }

        //    // Get course IDs from active enrollments
        //    var courseIds = activeEnrollments.Select(e => e.CourseID).Distinct().ToList();

        //    // Get course details for these IDs
        //    var courses = db.Courses
        //                    .Where(c => courseIds.Contains(c.CourseID) && c.IsActive) // optional course active filter
        //                    .ToList() // materialize
        //                    .Select(c => new
        //                    {
        //                        c.CourseID,
        //                        c.CourseName,
        //                        c.CourseCode,
        //                        c.TeacherID,
        //                        c.AcademicYear,
        //                        c.Description,
        //                        CourseIsActive = c.IsActive,
        //                        EnrollmentIsActive = activeEnrollments
        //                                             .Where(e => e.CourseID == c.CourseID)
        //                                             .Select(e => e.IsActive)
        //                                             .FirstOrDefault(),
        //                        SubjectID = activeEnrollments
        //                                    .Where(e => e.CourseID == c.CourseID)
        //                                    .Select(e => e.SubjectID)
        //                                    .FirstOrDefault()
        //                    })
        //                    .ToList();

        //    AddLog(null, "GetUser", $"Retrieved active courses for user with ID {id}");

        //    return Ok(new
        //    {
        //        userId = user.UserID,
        //        username = user.Username,
        //        courses = courses
        //    });
        //}

        //public IHttpActionResult GetUser(int id)
        //{
        //    var user = db.Users.Find(id);
        //    if (user == null)
        //    {
        //        AddLog(null, "GetUserFailed", $"User with ID {id} not found");
        //        return NotFound();
        //    }

        //    var activeEnrollments = db.Enrollments
        //                              .Where(e => e.StudentID == id && e.IsActive)
        //                              .ToList();

        //    if (!activeEnrollments.Any())
        //    {
        //        return Ok(new
        //        {
        //            userId = user.UserID,
        //            username = user.Username,
        //            firstName = user.FirstName,
        //            lastName = user.LastName,
        //            email = user.Email,
        //            courses = new List<object>()
        //        });
        //    }

        //    var courseIds = activeEnrollments.Select(e => e.CourseID).Distinct().ToList();

        //    var courses = db.Courses
        //                    .Where(c => courseIds.Contains(c.CourseID) && c.IsActive)
        //                    .ToList()
        //                    .Select(c =>
        //                    {
        //                        var enrollment = activeEnrollments
        //                                         .First(e => e.CourseID == c.CourseID);

        //                        // TeacherID = UserID (as per your confirmation)
        //                        var teacherUser = db.Users.FirstOrDefault(u => u.UserID == c.TeacherID);

        //                        string teacherName = teacherUser != null
        //                            ? $"{teacherUser.FirstName} {teacherUser.LastName}".Trim()
        //                            : null;

        //                        return new
        //                        {
        //                            c.CourseID,
        //                            c.CourseName,
        //                            c.CourseCode,
        //                            c.TeacherID,
        //                            TeacherName = teacherName,
        //                            c.AcademicYear,
        //                            c.Description,
        //                            CourseIsActive = c.IsActive,
        //                            EnrollmentIsActive = enrollment.IsActive,
        //                            enrollment.SubjectID
        //                        };
        //                    })
        //                    .ToList();

        //    AddLog(null, "GetUser", $"Retrieved active courses for user with ID {id}");

        //    return Ok(new
        //    {
        //        userId = user.UserID,
        //        username = user.Username,
        //        courses = courses
        //    });
        //}


        //public IHttpActionResult GetUser(int id)
        //{
        //    var user = db.Users.Find(id);
        //    if (user == null)
        //    {
        //        AddLog(null, "GetUserFailed", $"User with ID {id} not found");
        //        return NotFound();
        //    }

        //    var activeEnrollments = db.Enrollments
        //                              .Where(e => e.StudentID == id && e.IsActive)
        //                              .ToList();

        //    if (!activeEnrollments.Any())
        //    {
        //        return Ok(new
        //        {
        //            userId = user.UserID,
        //            username = user.Username,
        //            firstName = user.FirstName,
        //            lastName = user.LastName,
        //            email = user.Email,
        //            userTypeID = user.UserTypeID,
        //            profilePicture = user.ProfilePicture,
        //            courses = new List<object>()
        //        });
        //    }

        //    var courseIds = activeEnrollments.Select(e => e.CourseID).Distinct().ToList();

        //    var courses = db.Courses
        //                    .Where(c => courseIds.Contains(c.CourseID) && c.IsActive)
        //                    .ToList()
        //                    .Select(c =>
        //                    {
        //                        var enrollment = activeEnrollments.First(e => e.CourseID == c.CourseID);

        //                        // TeacherID = UserID
        //                        var teacherUser = db.Users.FirstOrDefault(u => u.UserID == c.TeacherID);

        //                        string teacherName = teacherUser != null
        //                            ? $"{teacherUser.FirstName} {teacherUser.LastName}".Trim()
        //                            : null;

        //                        return new
        //                        {
        //                            c.CourseID,
        //                            c.CourseName,
        //                            c.CourseCode,
        //                            c.TeacherID,
        //                            TeacherName = teacherName,
        //                            c.AcademicYear,
        //                            c.Description,
        //                            CourseIsActive = c.IsActive,
        //                            EnrollmentIsActive = enrollment.IsActive,
        //                            enrollment.SubjectID
        //                        };
        //                    })
        //                    .ToList();

        //    AddLog(null, "GetUser", $"Retrieved active courses for user with ID {id}");

        //    return Ok(new
        //    {
        //        userId = user.UserID,
        //        username = user.Username,
        //        firstName = user.FirstName,
        //        lastName = user.LastName,
        //        email = user.Email,
        //        userTypeID = user.UserTypeID,
        //        profilePicture = user.ProfilePicture,
        //        courses = courses
        //    });
        //}

        public IHttpActionResult GetUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                AddLog(null, "GetUserFailed", $"User with ID {id} not found");
                return NotFound();
            }

            var activeEnrollments = db.Enrollments
                                      .Where(e => e.StudentID == id && e.IsActive)
                                      .ToList();

            if (!activeEnrollments.Any())
            {
                return Ok(new
                {
                    userId = user.UserID,
                    username = user.Username,
                    firstName = user.FirstName,
                    lastName = user.LastName,
                    email = user.Email,
                    hedMobileNo = user.HED_MOBILE_NO,
                    userTypeID = user.UserTypeID,
                    profilePicture = user.ProfilePicture,
                    courses = new List<object>()
                });
            }

            var courseIds = activeEnrollments.Select(e => e.CourseID).Distinct().ToList();
            var courses = db.Courses
                            .Where(c => courseIds.Contains(c.CourseID) && c.IsActive)
                            .ToList()
                            .Select(c =>
                            {
                                var enrollment = activeEnrollments.First(e => e.CourseID == c.CourseID);
                                // TeacherID = UserID
                                var teacherUser = db.Users.FirstOrDefault(u => u.UserID == c.TeacherID);
                                string teacherName = teacherUser != null
                                    ? $"{teacherUser.FirstName} {teacherUser.LastName}".Trim()
                                    : null;

                                return new
                                {
                                    enrollmentID = enrollment.EnrollmentID,  // Added this line
                                    c.CourseID,
                                    c.CourseName,
                                    c.CourseCode,
                                    c.TeacherID,
                                    TeacherName = teacherName,
                                    c.AcademicYear,
                                    c.Description,
                                    CourseIsActive = c.IsActive,
                                    EnrollmentIsActive = enrollment.IsActive,
                                    enrollment.SubjectID
                                };
                            })
                            .ToList();

            AddLog(null, "GetUser", $"Retrieved active courses for user with ID {id}");

            return Ok(new
            {
                userId = user.UserID,
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                email = user.Email,
                userTypeID = user.UserTypeID,
                profilePicture = user.ProfilePicture,
                courses = courses
            });
        }



        // POST: api/Users
        public IHttpActionResult PostUser(User user)
        {
            if (!ModelState.IsValid)
            {
                AddLog(null, "CreateUserFailed", "Invalid model state");
                return BadRequest(ModelState);
            }

            db.Users.Add(user);
            db.SaveChanges();

            AddLog(null, "UserCreated", $"Created new user with ID {user.UserID}");
            return CreatedAtRoute("DefaultApi", new { id = user.UserID }, user);
        }

        // POST: api/Users/UploadProfile
        [HttpPost]
        [Route("api/Users/UploadProfile")]
        public IHttpActionResult UploadProfilePhoto()
        {
            var httpRequest = HttpContext.Current.Request;

            if (httpRequest == null)
                return BadRequest("Invalid request");

            // Read userId from form
            var userIdStr = httpRequest.Form["userId"] ?? httpRequest.Form["UserID"];
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return BadRequest("'userId' form field is required.");

            var user = db.Users.Find(userId);
            if (user == null)
                return NotFound();

            if (httpRequest.Files == null || httpRequest.Files.Count == 0)
                return BadRequest("No file uploaded");

            var postedFile = httpRequest.Files[0];

            // Allowed extensions
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var fileExt = Path.GetExtension(postedFile.FileName)?.ToLowerInvariant();

            if (string.IsNullOrEmpty(fileExt) || !allowedExtensions.Contains(fileExt))
                return BadRequest("Invalid file type. Allowed: .jpg, .jpeg, .png, .gif");

            // Max 5 MB
            const int maxBytes = 5 * 1024 * 1024;
            if (postedFile.ContentLength > maxBytes)
                return BadRequest("File size exceeds limit (5 MB).");

            // Upload path
            var uploadsVirtualFolder = "/Uploads/ProfilePhotos";
            var uploadsPath = HostingEnvironment.MapPath("~" + uploadsVirtualFolder);

            if (uploadsPath == null)
                return InternalServerError(new Exception("Unable to resolve uploads path"));

            Directory.CreateDirectory(uploadsPath);


            var fileName = userId + fileExt;
            var physicalFilePath = Path.Combine(uploadsPath, fileName);

            try
            {
                // Overwrite existing file
                if (File.Exists(physicalFilePath))
                    File.Delete(physicalFilePath);

                postedFile.SaveAs(physicalFilePath);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            // Save path to database
            user.ProfilePicture = $"{uploadsVirtualFolder}/{fileName}";

            try
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            return Ok(new { filePath = user.ProfilePicture });
        }


        // PUT: api/Users/UpdateProfilePhoto
        [HttpPut]
        [Route("api/Users/UpdateProfilePhoto")]
        public IHttpActionResult UpdateProfilePhoto()
        {
            var httpRequest = HttpContext.Current.Request;

            if (httpRequest == null)
                return BadRequest("Invalid request");

            // Read userId from form-data
            var userIdStr = httpRequest.Form["userId"] ?? httpRequest.Form["UserID"];
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return BadRequest("'userId' is required in form-data.");

            var user = db.Users.Find(userId);
            if (user == null)
                return NotFound();

            if (httpRequest.Files == null || httpRequest.Files.Count == 0)
                return BadRequest("No file uploaded.");

            var newFile = httpRequest.Files[0];

            // Allowed extensions
            var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var ext = Path.GetExtension(newFile.FileName)?.ToLowerInvariant();

            if (!allowedExt.Contains(ext))
                return BadRequest("Only .jpg, .jpeg, .png, .gif allowed.");

            if (newFile.ContentLength > 5 * 1024 * 1024)
                return BadRequest("File size exceeds limit (5 MB).");

            // Upload directory
            var folderVirtual = "/Uploads/ProfilePhotos";
            var folderPhysical = HostingEnvironment.MapPath("~" + folderVirtual);
            Directory.CreateDirectory(folderPhysical);

            // New filename
            var newFileName = userId + ext;
           //ar newFileName = $"{userId}_{Guid.NewGuid()}{ext}";
            var newFilePath = Path.Combine(folderPhysical, newFileName);

            try
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(user.ProfilePicture))
                {
                    var oldPhysicalPath = HostingEnvironment.MapPath("~" + user.ProfilePicture);

                    if (File.Exists(oldPhysicalPath))
                        File.Delete(oldPhysicalPath);
                }

                // Save new photo
                newFile.SaveAs(newFilePath);

                // Update database
                user.ProfilePicture = $"{folderVirtual}/{newFileName}";
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            return Ok(new
            {
                message = "Profile photo updated successfully.",
                filePath = user.ProfilePicture
            });
        }


        // DELETE: api/Users/DeleteProfile/5
        [HttpDelete]
        [Route("api/Users/DeleteProfile/{userId}")]
        public IHttpActionResult DeleteProfilePhoto(int userId)
        {
            var user = db.Users.Find(userId);
            if (user == null)
                return NotFound();

            if (string.IsNullOrEmpty(user.ProfilePicture))
                return BadRequest("This user has no profile picture.");

            // Map file path
            var physicalFilePath = HostingEnvironment.MapPath("~" + user.ProfilePicture);

            try
            {
                // Delete from disk
                if (File.Exists(physicalFilePath))
                    File.Delete(physicalFilePath);

                // Remove from database
                user.ProfilePicture = null;
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            return Ok("Profile photo deleted successfully.");
        }


        // PUT: api/Users/5
        //public IHttpActionResult PutUser(int id, User user)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        AddLog(null, "UpdateUserFailed", "Invalid model state");
        //        return BadRequest(ModelState);
        //    }

        //    if (id != user.UserID)
        //    {
        //        AddLog(null, "UpdateUserFailed", "ID mismatch");
        //        return BadRequest();
        //    }

        //    var existingUser = db.Users.Find(id);
        //    if (existingUser == null)
        //    {
        //        AddLog(null, "UpdateUserFailed", $"User with ID {id} not found");
        //        return NotFound();
        //    }

        //    // Preserve values that should not be cleared when client doesn't send them
        //    var previousProfilePicture = existingUser.ProfilePicture;
        //    var previousDateCreated = existingUser.DateCreated;

        //    // Apply incoming values onto the existing entity
        //    db.Entry(existingUser).CurrentValues.SetValues(user);

        //    // --- Profile picture handling ---
        //    // If client explicitly requests removal via RemoveProfilePicture flag, delete file and clear DB field.
        //    // If client provided a new ProfilePicture path (non-empty and different), replace it and optionally delete previous file.
        //    // If client omitted ProfilePicture (null or empty) and didn't request remove, keep previousProfilePicture.

        //    bool clientRequestedRemove = false;

        //    // Reflection-safe check for an optional RemoveProfilePicture property on the incoming model.
        //    var prop = user?.GetType().GetProperty("RemoveProfilePicture");
        //    if (prop != null)
        //    {
        //        try
        //        {
        //            var val = prop.GetValue(user);
        //            if (val is bool b) clientRequestedRemove = b;
        //            else if (val is bool?) clientRequestedRemove = ((bool?)val) == true;
        //        }
        //        catch
        //        {
        //            clientRequestedRemove = false;
        //        }
        //    }

        //    // Helper to safely delete a file when it appears to be inside our uploads area.
        //    void TryDeleteIfUploadedPath(string virtualPath)
        //    {
        //        try
        //        {
        //            if (string.IsNullOrWhiteSpace(virtualPath)) return;

        //            // Only delete files that look like they belong to the app uploads folder
        //            const string uploadsVirtualFolder = "/Uploads/ProfilePhotos";
        //            // Accept both "/Uploads/..." and "Uploads/..." shapes
        //            if (!virtualPath.StartsWith(uploadsVirtualFolder, StringComparison.OrdinalIgnoreCase) &&
        //                !virtualPath.StartsWith("/" + uploadsVirtualFolder.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
        //            {
        //                // Do not attempt to delete paths outside the uploads folder
        //                return;
        //            }

        //            var physicalPath = HostingEnvironment.MapPath("~" + virtualPath);
        //            if (string.IsNullOrEmpty(physicalPath)) return;
        //            if (File.Exists(physicalPath))
        //            {
        //                File.Delete(physicalPath);
        //                AddLog(null, "ProfilePhotoDeleted", $"Deleted profile photo file: {physicalPath}");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            // Log but don't fail the whole request for file-system errors
        //            AddLog(existingUser.UserID, "ProfilePhotoDeleteFailed", ex.Message);
        //        }
        //    }

        //    if (clientRequestedRemove)
        //    {
        //        // Delete previous file if present and clear DB field
        //        TryDeleteIfUploadedPath(previousProfilePicture);
        //        existingUser.ProfilePicture = null;
        //    }
        //    else
        //    {
        //        // If incoming model provided a non-empty ProfilePicture value, treat it as an update (replace)
        //        var incomingProfilePicture = user.ProfilePicture;
        //        if (!string.IsNullOrEmpty(incomingProfilePicture))
        //        {
        //            // If the incoming path is different from previous, remove the old file (optional)
        //            if (!string.Equals(previousProfilePicture, incomingProfilePicture, StringComparison.OrdinalIgnoreCase))
        //            {
        //                TryDeleteIfUploadedPath(previousProfilePicture);
        //            }
        //            existingUser.ProfilePicture = incomingProfilePicture;
        //        }
        //        else
        //        {
        //            // Client did not include a profile picture; preserve existing value
        //            existingUser.ProfilePicture = previousProfilePicture;
        //        }
        //    }

        //    // Preserve DateCreated to avoid accidental updates from client
        //    existingUser.DateCreated = previousDateCreated;

        //    db.Entry(existingUser).State = EntityState.Modified;

        //    try
        //    {
        //        db.SaveChanges();
        //        AddLog(existingUser.UserID, "UserUpdated", $"User with ID {id} updated");
        //    }
        //    catch (Exception ex)
        //    {
        //        if (!UserExists(id))
        //        {
        //            AddLog(null, "UpdateUserFailed", $"User with ID {id} not found");
        //            return NotFound();
        //        }
        //        else
        //        {
        //            AddLog(null, "UpdateUserError", ex.Message);
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}
        public IHttpActionResult PutUser(int id, UpdateUserDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is missing.");

            if (id != dto.UserID)
                return BadRequest("ID mismatch.");

            var existingUser = db.Users.Find(id);
            if (existingUser == null)
                return NotFound();

            // Preserve protected fields
            var previousPasswordHash = existingUser.PasswordHash;
            var previousProfilePicture = existingUser.ProfilePicture;
            var previousDateCreated = existingUser.DateCreated;
            var previousUserTypeId = existingUser.UserTypeID;

            //  Update ONLY allowed fields
            existingUser.Username = dto.Username ?? existingUser.Username;
            existingUser.Email = dto.Email ?? existingUser.Email;
            existingUser.FirstName = dto.FirstName ?? existingUser.FirstName;
            existingUser.LastName = dto.LastName ?? existingUser.LastName;
            existingUser.HED_MOBILE_NO = dto.HED_MOBILE_NO ?? existingUser.HED_MOBILE_NO;

            //  Password logic
            if (!string.IsNullOrWhiteSpace(dto.PasswordHash))
            {
                existingUser.PasswordHash = dto.PasswordHash;
            }
            else
            {
                existingUser.PasswordHash = previousPasswordHash;
            }

            // Profile picture handling
            void TryDeleteIfUploadedPath(string virtualPath)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(virtualPath)) return;

                    const string uploadsVirtualFolder = "/Uploads/ProfilePhotos";
                    if (!virtualPath.StartsWith(uploadsVirtualFolder, StringComparison.OrdinalIgnoreCase))
                        return;

                    var physicalPath = HostingEnvironment.MapPath("~" + virtualPath);
                    if (!string.IsNullOrEmpty(physicalPath) && File.Exists(physicalPath))
                    {
                        File.Delete(physicalPath);
                        AddLog(existingUser.UserID, "ProfilePhotoDeleted", physicalPath);
                    }
                }
                catch (Exception ex)
                {
                    AddLog(existingUser.UserID, "ProfilePhotoDeleteFailed", ex.Message);
                }
            }

            if (dto.RemoveProfilePicture)
            {
                TryDeleteIfUploadedPath(previousProfilePicture);
                existingUser.ProfilePicture = null;
            }
            else if (!string.IsNullOrWhiteSpace(dto.ProfilePicture))
            {
                if (!string.Equals(previousProfilePicture, dto.ProfilePicture, StringComparison.OrdinalIgnoreCase))
                {
                    TryDeleteIfUploadedPath(previousProfilePicture);
                }
                existingUser.ProfilePicture = dto.ProfilePicture;
            }

            //  Restore protected fields
            existingUser.DateCreated = previousDateCreated;
            existingUser.UserTypeID = previousUserTypeId;

            db.Entry(existingUser).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
                AddLog(existingUser.UserID, "UserUpdated", $"User {id} updated");
            }
            catch (DbUpdateException ex)
            {
                var sqlEx = ex.InnerException?.InnerException as SqlException;
                return Content(HttpStatusCode.BadRequest, sqlEx?.Message ?? ex.Message);
            }

            return StatusCode(HttpStatusCode.NoContent);
        }


        //// DELETE: api/Users/5
        //public IHttpActionResult DeleteUser(int id)
        //{
        //    var user = db.Users.Find(id);
        //    if (user == null) return NotFound();

        //    var student = db.Students.Find(id);
        //    if (student != null)
        //    {
        //        var enrollments = db.Enrollments.Where(e => e.StudentID == id).ToList();
        //        if (enrollments.Any())
        //            db.Enrollments.RemoveRange(enrollments);

        //        db.Students.Remove(student);
        //    }

        //    var teacher = db.Teachers.Find(id);
        //    if (teacher != null) db.Teachers.Remove(teacher);

        //    db.SystemLogs.Where(l => l.UserID == id).ToList().ForEach(l => l.UserID = null);
        //    db.SaveChanges();

        //    db.Users.Remove(user);
        //    db.SaveChanges();

        //    AddLog(null, "UserDeleted", $"User with ID {id} deleted");
        //    return Ok(user);
        //}

        [HttpDelete]
        public IHttpActionResult DeleteUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return NotFound();

            user.IsActive = false; // soft delete
            db.SaveChanges();

            AddLog(null, "UserDeactivated", $"User with ID {id} deactivated");

            return Ok(new { message = "User moved to inactive" });
        }

        [HttpPut]
        [Route("api/Users/Activate/{id}")]
        public IHttpActionResult ActivateUser(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return NotFound();

            user.IsActive = true;
            db.SaveChanges();

            AddLog(null, "UserActivated", $"User with ID {id} activated");

            return Ok(new { message = "User activated successfully" });
        }




        private bool UserExists(int id)
        {
            return db.Users.Count(e => e.UserID == id) > 0;
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}
        // GET: api/Users/BasicInfo/5
        [HttpGet]
        [Route("api/Users/BasicInfo/{id:int}")]
        public IHttpActionResult GetBasicUserInfo(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
            {
                AddLog(null, "GetBasicUserInfoFailed", $"User with ID {id} not found");
                return NotFound();
            }

            var fullName = $"{user.FirstName} {user.LastName}".Trim();

            var result = new
            {
                UserID = user.UserID,
                FullName = fullName,
                Email = user.Email,
                HED_MOBILE_NO = user.HED_MOBILE_NO
            };

            AddLog(user.UserID, "GetBasicUserInfo", $"Fetched basic info for user ID {id}");

            return Ok(result);
        }
        // GET: api/Users/ProfilePicture/5
        [HttpGet]
        [Route("api/Users/ProfilePicture/{id:int}")]
        public IHttpActionResult GetProfilePicture(int id)
        {
            var user = db.Users.Find(id);
            if (user == null)
                return NotFound();

            if (string.IsNullOrEmpty(user.ProfilePicture))
                return Ok(new { hasPicture = false, filePath = "" });

            return Ok(new { hasPicture = true, filePath = user.ProfilePicture });
        }


    }
}