using ChesterMeatMatrix.Data;
using ChesterMeatMatrix.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Globalization;

namespace ChesterMeatMatrix.Controllers
{
    public class StaffManagerController : Controller
    {
        private bool IsStaffManager()
        {
            var role = Session["Role"] as string;
            return role == "Admin" || role == "WarehouseManager";
        }

        // ========================================
        // DASHBOARD (Manager only)
        // ========================================

        [HttpGet]
        public ActionResult Dashboard()
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.PendingTasks = InMemoryDatabase.WorkTasks.Count(t => t.Status == WorkTaskStatus.Pending);
            ViewBag.AssignedTasks = InMemoryDatabase.WorkTasks.Count(t => t.Status == WorkTaskStatus.Assigned);
            ViewBag.InProgressTasks = InMemoryDatabase.WorkTasks.Count(t => t.Status == WorkTaskStatus.InProgress);
            ViewBag.CompletedTasks = InMemoryDatabase.WorkTasks.Count(t => t.Status == WorkTaskStatus.Completed);
            ViewBag.TotalStaff = InMemoryDatabase.Users.Count(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver");
            ViewBag.FullName = Session["FullName"] as string;

            var recentTasks = InMemoryDatabase.WorkTasks
                .OrderByDescending(t => t.CreatedDate)
                .Take(10)
                .ToList();

            foreach (var task in recentTasks)
            {
                if (task.AssignedToId.HasValue)
                {
                    task.AssignedTo = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == task.AssignedToId.Value);
                }
            }

            return View(recentTasks);
        }

        // ========================================
        // ASSIGN TASK (Manager only)
        // ========================================

        [HttpGet]
        public ActionResult AssignTask()
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Staff = InMemoryDatabase.Users
                .Where(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver")
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.TaskTypes = Enum.GetValues(typeof(WorkTaskType))
                .Cast<WorkTaskType>()
                .Select(t => new SelectListItem { Value = t.ToString(), Text = t.ToString() })
                .ToList();

            ViewBag.Priorities = Enum.GetValues(typeof(WorkTaskPriority))
                .Cast<WorkTaskPriority>()
                .Select(p => new SelectListItem { Value = p.ToString(), Text = p.ToString() })
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignTask(string title, string description, string taskType, string priority, int staffId, DateTime? dueDate, string notes)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrWhiteSpace(title) || staffId <= 0)
            {
                TempData["Error"] = "Please provide a task title and assign to a staff member.";
                return RedirectToAction("AssignTask");
            }

            var assignedBy = Session["FullName"] as string ?? Session["Username"] as string;

            if (!Enum.TryParse<WorkTaskType>(taskType, true, out var taskTypeEnum))
            {
                taskTypeEnum = WorkTaskType.OrderPicking;
            }

            if (!Enum.TryParse<WorkTaskPriority>(priority, true, out var taskPriorityEnum))
            {
                taskPriorityEnum = WorkTaskPriority.Medium;
            }

            var nextTaskId = InMemoryDatabase.WorkTasks.Any() ? InMemoryDatabase.WorkTasks.Max(t => t.WorkTaskId) + 1 : 1;

            var task = new WorkTask
            {
                WorkTaskId = nextTaskId,
                Title = title,
                Description = description,
                Type = taskTypeEnum,
                Priority = taskPriorityEnum,
                Status = WorkTaskStatus.Assigned,
                CreatedDate = DateTime.Now,
                DueDate = dueDate,
                CreatedBy = assignedBy,
                AssignedToId = staffId
            };

            InMemoryDatabase.WorkTasks.Add(task);

            var assignedByUser = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == Session["Username"] as string);
            var nextAssignmentId = InMemoryDatabase.TaskAssignments.Any() ? InMemoryDatabase.TaskAssignments.Max(a => a.AssignmentId) + 1 : 1;

            var assignment = new TaskAssignment
            {
                AssignmentId = nextAssignmentId,
                WorkTaskId = task.WorkTaskId,
                StaffId = staffId,
                AssignedById = assignedByUser?.Id ?? 1,
                AssignedDate = DateTime.Now,
                Status = AssignmentStatus.Pending,
                Notes = notes
            };

            InMemoryDatabase.TaskAssignments.Add(assignment);

            TempData["Success"] = $"Task '{title}' assigned successfully.";
            return RedirectToAction("Dashboard");
        }

        // ========================================
        // VIEW ALL TASKS (Manager only)
        // ========================================

        [HttpGet]
        public ActionResult Tasks(string status = null)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            var tasks = InMemoryDatabase.WorkTasks.AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<WorkTaskStatus>(status, true, out var taskStatus))
            {
                tasks = tasks.Where(t => t.Status == taskStatus);
            }

            var taskList = tasks.OrderByDescending(t => t.CreatedDate).ToList();

            foreach (var task in taskList)
            {
                if (task.AssignedToId.HasValue)
                {
                    task.AssignedTo = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == task.AssignedToId.Value);
                }
            }

            ViewBag.CurrentStatus = status;

            return View(taskList);
        }

        // ========================================
        // UPDATE TASK STATUS (Manager only)
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateTaskStatus(int taskId, string status)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please login first." });
            }

            var task = InMemoryDatabase.WorkTasks.FirstOrDefault(t => t.WorkTaskId == taskId);
            if (task == null)
            {
                return Json(new { success = false, message = "Task not found" });
            }

            // The staff member the task is assigned to can update it, or a manager can override
            bool isOwner = task.AssignedToId.HasValue && task.AssignedToId.Value == userId;
            if (!isOwner && !IsStaffManager())
            {
                return Json(new { success = false, message = "You are not authorized to update this task." });
            }

            if (Enum.TryParse<WorkTaskStatus>(status, true, out var taskStatus))
            {
                task.Status = taskStatus;

                if (taskStatus == WorkTaskStatus.Completed)
                {
                    task.CompletedDate = DateTime.Now;

                    var assignment = InMemoryDatabase.TaskAssignments.FirstOrDefault(a => a.WorkTaskId == taskId);
                    if (assignment != null)
                    {
                        assignment.Status = AssignmentStatus.Completed;
                        assignment.CompletedDate = DateTime.Now;
                    }
                }

                return Json(new { success = true, message = "Task status updated" });
            }

            return Json(new { success = false, message = "Invalid status" });
        }

        // ========================================
        // MY TASKS (Any logged-in staff can view)
        // ========================================

        [HttpGet]
        public ActionResult MyTasks()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                TempData["Error"] = "Please login first.";
                return RedirectToAction("Login", "Account");
            }

            var tasks = InMemoryDatabase.WorkTasks
                .Where(t => t.AssignedToId == userId)
                .OrderByDescending(t => t.CreatedDate)
                .ToList();

            return View(tasks);
        }

        private int GetCurrentUserId()
        {
            var username = Session["Username"] as string;
            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == username);
            return user?.Id ?? 0;
        }

        // ========================================
        // SCHEDULE MANAGEMENT
        // ========================================

        [HttpGet]
        public ActionResult Schedule(DateTime? startDate)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Staff = InMemoryDatabase.Users
                .Where(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver")
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.ShiftTypes = Enum.GetValues(typeof(ShiftType))
                .Cast<ShiftType>()
                .Select(s => new SelectListItem { Value = s.ToString(), Text = s.ToString() })
                .ToList();

            // Use passed date if available, otherwise default to Today
            var baseDate = startDate ?? DateTime.Today;
            var startOfWeek = baseDate.AddDays(-(int)baseDate.DayOfWeek + (baseDate.DayOfWeek == DayOfWeek.Sunday ? -6 : 1)); // Monday start
            var endOfWeek = startOfWeek.AddDays(6);

            var shifts = InMemoryDatabase.Shifts
                .Where(s => s.ShiftDate.Date >= startOfWeek.Date && s.ShiftDate.Date <= endOfWeek.Date)
                .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.Type)
                .ToList();

            foreach (var shift in shifts)
            {
                shift.ShiftAssignments = InMemoryDatabase.ShiftAssignments
                    .Where(a => a.ShiftId == shift.ShiftId)
                    .ToList();
            }

            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;

            return View(shifts);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateShift(string shiftDate, string shiftType, string shiftName, string notes)
        {
            if (!IsStaffManager())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!DateTime.TryParse(shiftDate, out var date) || string.IsNullOrEmpty(shiftType))
            {
                TempData["Error"] = "Please provide a valid date and shift type.";
                return RedirectToAction("Schedule");
            }

            if (!Enum.TryParse<ShiftType>(shiftType, true, out var shiftTypeEnum))
            {
                shiftTypeEnum = ShiftType.Morning;
            }

            var nextId = InMemoryDatabase.Shifts.Any() ? InMemoryDatabase.Shifts.Max(s => s.ShiftId) + 1 : 1;

            var shift = new Shift
            {
                ShiftId = nextId,
                Type = shiftTypeEnum,
                ShiftDate = date,
                Name = shiftName ?? $"{shiftTypeEnum} Shift - {date:dd MMM yyyy}",
                Status = ShiftStatus.Draft,
                CreatedBy = Session["FullName"] as string ?? Session["Username"] as string,
                CreatedDate = DateTime.Now,
                Notes = notes
            };

            InMemoryDatabase.Shifts.Add(shift);

            TempData["Success"] = $"Shift created successfully.";
            return RedirectToAction("Schedule");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignStaffToShift(int shiftId, int staffId)
        {
            if (!IsStaffManager())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var shift = InMemoryDatabase.Shifts.FirstOrDefault(s => s.ShiftId == shiftId);
            if (shift == null)
            {
                return Json(new { success = false, message = "Shift not found" });
            }

            var staff = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == staffId);
            if (staff == null)
            {
                return Json(new { success = false, message = "Staff not found" });
            }

            var existing = InMemoryDatabase.ShiftAssignments.FirstOrDefault(a => a.ShiftId == shiftId && a.StaffId == staffId);
            if (existing != null)
            {
                return Json(new { success = false, message = "Staff already assigned to this shift" });
            }

            var nextId = InMemoryDatabase.ShiftAssignments.Any() ? InMemoryDatabase.ShiftAssignments.Max(a => a.ShiftAssignmentId) + 1 : 1;
            var assignedBy = InMemoryDatabase.Users.FirstOrDefault(u => u.Username == Session["Username"] as string);

            var assignment = new ShiftAssignment
            {
                ShiftAssignmentId = nextId,
                ShiftId = shiftId,
                StaffId = staffId,
                AssignedById = assignedBy?.Id ?? 1,
                AssignedDate = DateTime.Now,
                Status = ShiftAssignmentStatus.Pending
            };

            InMemoryDatabase.ShiftAssignments.Add(assignment);

            // Store in TempData so toast shows post-reload with full animation
            TempData["Success"] = $"{staff.FullName} assigned to {shift.Type} shift successfully.";

            return Json(new { success = true, message = $"Staff assigned to {shift.Type} shift." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PublishSchedule()
        {
            if (!IsStaffManager())
            {
                return RedirectToAction("Login", "Account");
            }

            var draftShifts = InMemoryDatabase.Shifts.Where(s => s.Status == ShiftStatus.Draft).ToList();
            foreach (var shift in draftShifts)
            {
                shift.Status = ShiftStatus.Published;
                shift.PublishedDate = DateTime.Now;
            }

            TempData["Success"] = $"Schedule published. {draftShifts.Count} shifts published.";
            return RedirectToAction("Schedule");
        }

        [HttpGet]
        public ActionResult MySchedule()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + 1);
            var endOfWeek = startOfWeek.AddDays(6);

            var assignments = InMemoryDatabase.ShiftAssignments
                .Where(a => a.StaffId == userId)
                .ToList();

            var shiftIds = assignments.Select(a => a.ShiftId).ToList();
            var shifts = InMemoryDatabase.Shifts
                .Where(s => shiftIds.Contains(s.ShiftId) && s.ShiftDate >= startOfWeek && s.ShiftDate <= endOfWeek)
                .OrderBy(s => s.ShiftDate)
                .ThenBy(s => s.Type)
                .ToList();

            return View(shifts);
        }

        // ========================================
        // ATTENDANCE MANAGEMENT
        // ========================================


        // ========================================
        // CLOCK IN / OUT PAGE (GET)
        // ========================================

        [HttpGet]
        public ActionResult ClockInOut()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var today = DateTime.Today;
            var todayAttendance = InMemoryDatabase.Attendances
                .FirstOrDefault(a => a.StaffId == userId && a.Date == today);

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == userId);

            ViewBag.IsClockedIn = todayAttendance != null && todayAttendance.ClockInTime.HasValue;
            ViewBag.IsClockedOut = todayAttendance != null && todayAttendance.ClockOutTime.HasValue;
            ViewBag.TodayAttendance = todayAttendance;
            ViewBag.HasFaceRegistered = user != null && !string.IsNullOrEmpty(user.FaceData);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ClockIn()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please login first." });
            }

            var today = DateTime.Today;
            var existing = InMemoryDatabase.Attendances
                .FirstOrDefault(a => a.StaffId == userId && a.Date == today);

            if (existing != null && existing.ClockInTime.HasValue)
            {
                return Json(new { success = false, message = "You are already clocked in today." });
            }

            var nextId = InMemoryDatabase.Attendances.Any()
                ? InMemoryDatabase.Attendances.Max(a => a.AttendanceId) + 1
                : 1;

            var attendance = new Attendance
            {
                AttendanceId = nextId,
                StaffId = userId,
                Date = today,
                ClockInTime = DateTime.Now,
                Status = AttendanceStatus.Present,
                CreatedDate = DateTime.Now
            };

            InMemoryDatabase.Attendances.Add(attendance);

            return Json(new { success = true, message = "Clocked in successfully at " + DateTime.Now.ToString("HH:mm") });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ClockOut()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Json(new { success = false, message = "Please login first." });
            }

            var today = DateTime.Today;
            var attendance = InMemoryDatabase.Attendances
                .FirstOrDefault(a => a.StaffId == userId && a.Date == today);

            if (attendance == null || !attendance.ClockInTime.HasValue)
            {
                return Json(new { success = false, message = "You have not clocked in yet." });
            }

            if (attendance.ClockOutTime.HasValue)
            {
                return Json(new { success = false, message = "You have already clocked out today." });
            }

            attendance.ClockOutTime = DateTime.Now;

            // Calculate total hours
            var duration = attendance.ClockOutTime.Value - attendance.ClockInTime.Value;
            attendance.TotalHours = (decimal)duration.TotalHours;

            // Calculate overtime (if more than 8 hours)
            if (attendance.TotalHours > 8)
            {
                attendance.OvertimeHours = attendance.TotalHours - 8;
            }

            return Json(new
            {
                success = true,
                message = "Clocked out successfully at " + DateTime.Now.ToString("HH:mm") +
                          " | Hours: " + attendance.TotalHours.ToString("F2")
            });
        }

        

        [HttpGet]
        public ActionResult Attendance(DateTime? date, string status = null)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            var targetDate = date ?? DateTime.Today;
            var query = InMemoryDatabase.Attendances
                .Where(a => a.Date == targetDate)
                .ToList();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<AttendanceStatus>(status, true, out var statusEnum))
            {
                query = query.Where(a => a.Status == statusEnum).ToList();
            }

            // Load staff details
            foreach (var att in query)
            {
                att.Staff = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == att.StaffId);
            }

            // Sort after loading staff
            query = query.OrderBy(a => a.Staff?.FullName ?? "").ToList();

            // All staff for dropdown
            ViewBag.AllStaff = InMemoryDatabase.Users
                .Where(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver")
                .OrderBy(u => u.FullName)
                .ToList();

            ViewBag.TargetDate = targetDate;
            ViewBag.CurrentStatus = status;
            ViewBag.StatusOptions = new[] { "Present", "Absent", "Late", "HalfDay", "Leave" };

            // Summary stats
            ViewBag.TotalStaff = InMemoryDatabase.Users.Count(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver");
            ViewBag.TotalPresent = query.Count(a => a.Status == AttendanceStatus.Present);
            ViewBag.TotalAbsent = query.Count(a => a.Status == AttendanceStatus.Absent);
            ViewBag.TotalLate = query.Count(a => a.Status == AttendanceStatus.Late);

            return View(query);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult UpdateAttendanceStatus(int attendanceId, string status, string notes)
        {
            if (!IsStaffManager())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var attendance = InMemoryDatabase.Attendances.FirstOrDefault(a => a.AttendanceId == attendanceId);
            if (attendance == null)
            {
                return Json(new { success = false, message = "Attendance record not found" });
            }

            if (Enum.TryParse<AttendanceStatus>(status, true, out var statusEnum))
            {
                attendance.Status = statusEnum;
                attendance.Notes = notes;
                return Json(new { success = true, message = "Attendance status updated" });
            }

            return Json(new { success = false, message = "Invalid status" });
        }

        // ========================================
        // PERFORMANCE MANAGEMENT
        // ========================================

        [HttpGet]
        public ActionResult PerformanceDashboard()
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            // Get all staff (Warehouse, Checker, Driver)
            var staff = InMemoryDatabase.Users
                .Where(u => u.Role == "Warehouse" || u.Role == "Checker" || u.Role == "Driver")
                .ToList();

            var performanceData = new List<Performance>();

            foreach (var s in staff)
            {
                // ----- TASKS COMPLETED (REAL) -----
                var tasks = InMemoryDatabase.WorkTasks
                    .Where(t => t.AssignedToId == s.Id)
                    .ToList();

                var completedTasks = tasks.Count(t => t.Status == WorkTaskStatus.Completed);
                var totalTasks = tasks.Count;

                // ----- ACCURACY RATE (REAL - based on returns/errors) -----
                // Count returns linked to this staff member
                var returnsCount = 0;
                if (s.Role == "Driver")
                {
                    // For drivers: count returns from their routes
                    var driverRoutes = InMemoryDatabase.Routes
                        .Where(r => r.AssignedDriverId == s.Id)
                        .Select(r => r.OrderIds)
                        .SelectMany(ids => ids ?? new List<int>())
                        .ToList();

                    returnsCount = InMemoryDatabase.Returns
                        .Count(r => driverRoutes.Contains(r.OrderId));
                }
                else if (s.Role == "Checker")
                {
                    // For checkers: count returns from orders they picked
                    var checkerOrders = InMemoryDatabase.Orders
                        .Where(o => o.ConfirmedBy == s.Username)
                        .Select(o => o.Id)
                        .ToList();

                    returnsCount = InMemoryDatabase.Returns
                        .Count(r => checkerOrders.Contains(r.OrderId));
                }
                else if (s.Role == "Warehouse")
                {
                    // For warehouse: count returns from stock they handled
                    // Use a simpler approach - count total returns
                    returnsCount = InMemoryDatabase.Returns.Count;
                }

                // Accuracy = 100% minus error rate (each return reduces accuracy)
                var accuracyRate = totalTasks > 0
                    ? Math.Max(70, 100 - (returnsCount * 2))
                    : 100;

                // ----- EFFICIENCY (REAL - tasks per hour) -----
                var attendance = InMemoryDatabase.Attendances
                    .Where(a => a.StaffId == s.Id && a.ClockInTime.HasValue && a.ClockOutTime.HasValue)
                    .ToList();

                var totalHours = attendance.Sum(a => a.TotalHours);
                var efficiency = totalHours > 0 && totalTasks > 0
                    ? Math.Round((decimal)totalTasks / totalHours, 2)
                    : 0;

                // ----- PUNCTUALITY (REAL) -----
                var lateCount = attendance.Count(a => a.Status == AttendanceStatus.Late);
                var punctuality = attendance.Any()
                    ? Math.Max(70, 100 - ((decimal)lateCount / attendance.Count * 15))
                    : 85;

                // ----- CUSTOMER FEEDBACK (REAL - from claims) -----
                // Count claims against this staff member
                var claimsCount = 0;
                if (s.Role == "Driver")
                {
                    var driverRoutes = InMemoryDatabase.Routes
                        .Where(r => r.AssignedDriverId == s.Id)
                        .Select(r => r.OrderIds)
                        .SelectMany(ids => ids ?? new List<int>())
                        .ToList();

                    claimsCount = InMemoryDatabase.Claims
                        .Count(c => driverRoutes.Contains(c.OrderId));
                }
                else if (s.Role == "Checker")
                {
                    var checkerOrders = InMemoryDatabase.Orders
                        .Where(o => o.ConfirmedBy == s.Username)
                        .Select(o => o.Id)
                        .ToList();

                    claimsCount = InMemoryDatabase.Claims
                        .Count(c => checkerOrders.Contains(c.OrderId));
                }

                // Customer feedback = 100% minus claims impact
                var customerFeedback = claimsCount > 0
                    ? Math.Max(70, 100 - (claimsCount * 3))
                    : 92;

                // ----- OVERALL SCORE (REAL - weighted average) -----
                // Weight: Accuracy 30%, Efficiency 25%, Punctuality 25%, Customer Feedback 20%
                var overallScore = (accuracyRate * 0.30m) +
                                   (Math.Min(efficiency * 20, 100) * 0.25m) +
                                   (punctuality * 0.25m) +
                                   (customerFeedback * 0.20m);

                if (overallScore > 100) overallScore = 100;

                // Get existing performance review or create new
                var existing = InMemoryDatabase.Performances
                    .FirstOrDefault(p => p.StaffId == s.Id && p.ReviewPeriod == DateTime.Now.ToString("MMMM yyyy"));

                if (existing != null)
                {
                    // Update existing metrics
                    existing.TasksCompleted = completedTasks;
                    existing.AccuracyRate = accuracyRate;
                    existing.Efficiency = efficiency;
                    existing.Punctuality = punctuality;
                    existing.CustomerFeedback = customerFeedback;
                    existing.OverallScore = overallScore;
                    performanceData.Add(existing);
                }
                else
                {
                    // Create new performance record
                    var nextId = InMemoryDatabase.Performances.Any()
                        ? InMemoryDatabase.Performances.Max(p => p.PerformanceId) + 1
                        : 1;

                    var performance = new Performance
                    {
                        PerformanceId = nextId,
                        StaffId = s.Id,
                        Staff = s,
                        ReviewPeriod = DateTime.Now.ToString("MMMM yyyy"),
                        ReviewDate = DateTime.Now,
                        TasksCompleted = completedTasks,
                        AccuracyRate = accuracyRate,
                        Efficiency = efficiency,
                        Punctuality = punctuality,
                        CustomerFeedback = customerFeedback,
                        OverallScore = overallScore,
                        OverallRating = GetPerformanceRating(overallScore),
                        CreatedDate = DateTime.Now
                    };

                    InMemoryDatabase.Performances.Add(performance);
                    performanceData.Add(performance);
                }
            }

            // Sort by overall score descending
            performanceData = performanceData.OrderByDescending(p => p.OverallScore).ToList();

            ViewBag.TotalStaff = staff.Count;
            ViewBag.AverageScore = performanceData.Any() ? performanceData.Average(p => p.OverallScore) : 0;
            ViewBag.TopPerformer = performanceData.FirstOrDefault()?.Staff?.FullName ?? "N/A";

            return View(performanceData);
        }

        // Helper method to get rating from score
        private PerformanceRating GetPerformanceRating(decimal score)
        {
            if (score >= 90) return PerformanceRating.Excellent;
            if (score >= 75) return PerformanceRating.Good;
            if (score >= 60) return PerformanceRating.Satisfactory;
            if (score >= 40) return PerformanceRating.NeedsImprovement;
            return PerformanceRating.Poor;
        }

        [HttpGet]
        public ActionResult PerformanceReview(int id)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            var performance = InMemoryDatabase.Performances
                .FirstOrDefault(p => p.PerformanceId == id);

            if (performance == null)
            {
                TempData["Error"] = "Performance record not found.";
                return RedirectToAction("PerformanceDashboard");
            }

            performance.Staff = InMemoryDatabase.Users
                .FirstOrDefault(u => u.Id == performance.StaffId);

            return View(performance);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SavePerformanceReview(int performanceId, string strengths, string areasForImprovement, string managerComments, string overallRating)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            var performance = InMemoryDatabase.Performances
                .FirstOrDefault(p => p.PerformanceId == performanceId);

            if (performance == null)
            {
                TempData["Error"] = "Performance record not found.";
                return RedirectToAction("PerformanceDashboard");
            }

            if (!string.IsNullOrEmpty(strengths))
                performance.Strengths = strengths;

            if (!string.IsNullOrEmpty(areasForImprovement))
                performance.AreasForImprovement = areasForImprovement;

            if (!string.IsNullOrEmpty(managerComments))
                performance.ManagerComments = managerComments;

            if (!string.IsNullOrEmpty(overallRating) &&
                Enum.TryParse<PerformanceRating>(overallRating, true, out var rating))
            {
                performance.OverallRating = rating;
            }

            performance.ReviewedBy = Session["FullName"] as string ?? Session["Username"] as string;
            performance.ReviewDate = DateTime.Now;

            TempData["Success"] = $"Performance review for {performance.Staff?.FullName} saved.";
            return RedirectToAction("PerformanceDashboard");
        }

        // ========================================
        // LEAVE MANAGEMENT
        // ========================================

        [HttpGet]
        public ActionResult MyLeave()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var leaveRequests = InMemoryDatabase.LeaveRequests
                .Where(l => l.StaffId == userId)
                .OrderByDescending(l => l.RequestDate)
                .ToList();

            // Calculate available leave days (simplified)
            var totalTaken = leaveRequests
                .Where(l => l.Status == LeaveStatus.Approved)
                .Sum(l => l.TotalDays);

            ViewBag.LeaveBalance = 20 - totalTaken; // 20 days annual leave
            ViewBag.TotalTaken = totalTaken;
            ViewBag.PendingCount = leaveRequests.Count(l => l.Status == LeaveStatus.Pending);

            return View(leaveRequests);
        }

        [HttpGet]
        public ActionResult RequestLeave()
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.LeaveTypes = Enum.GetValues(typeof(LeaveType))
                .Cast<LeaveType>()
                .Select(l => new SelectListItem { Value = l.ToString(), Text = l.ToString() })
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RequestLeave(string leaveType, DateTime startDate, DateTime endDate, string reason)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            if (string.IsNullOrEmpty(leaveType) || startDate == default || endDate == default)
            {
                TempData["Error"] = "Please provide all required fields.";
                return RedirectToAction("RequestLeave");
            }

            if (startDate > endDate)
            {
                TempData["Error"] = "Start date must be before end date.";
                return RedirectToAction("RequestLeave");
            }

            if (!Enum.TryParse<LeaveType>(leaveType, true, out var leaveTypeEnum))
            {
                leaveTypeEnum = LeaveType.Annual;
            }

            var totalDays = (endDate - startDate).Days + 1;

            var nextId = InMemoryDatabase.LeaveRequests.Any()
                ? InMemoryDatabase.LeaveRequests.Max(l => l.LeaveRequestId) + 1
                : 1;

            var leaveRequest = new LeaveRequest
            {
                LeaveRequestId = nextId,
                StaffId = userId,
                Type = leaveTypeEnum,
                StartDate = startDate,
                EndDate = endDate,
                TotalDays = totalDays,
                Status = LeaveStatus.Pending,
                RequestDate = DateTime.Now,
                Reason = reason
            };

            InMemoryDatabase.LeaveRequests.Add(leaveRequest);

            TempData["Success"] = $"Leave request submitted. Waiting for approval.";
            return RedirectToAction("MyLeave");
        }

        [HttpGet]
        public ActionResult ManageLeave(string status = null)
        {
            if (!IsStaffManager())
            {
                TempData["Error"] = "Access denied. Staff Manager only.";
                return RedirectToAction("Login", "Account");
            }

            var query = InMemoryDatabase.LeaveRequests.AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LeaveStatus>(status, true, out var statusEnum))
            {
                query = query.Where(l => l.Status == statusEnum);
            }

            var leaveRequests = query
                .OrderByDescending(l => l.RequestDate)
                .ToList();

            // Load staff details
            foreach (var leave in leaveRequests)
            {
                leave.Staff = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == leave.StaffId);
            }

            ViewBag.CurrentStatus = status;
            ViewBag.StatusOptions = new[] { "Pending", "Approved", "Rejected", "Cancelled" };

            // Summary stats
            ViewBag.TotalRequests = leaveRequests.Count;
            ViewBag.PendingCount = leaveRequests.Count(l => l.Status == LeaveStatus.Pending);
            ViewBag.ApprovedCount = leaveRequests.Count(l => l.Status == LeaveStatus.Approved);
            ViewBag.RejectedCount = leaveRequests.Count(l => l.Status == LeaveStatus.Rejected);

            return View(leaveRequests);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ProcessLeaveRequest(int leaveRequestId, string decision, string managerNotes)
        {
            if (!IsStaffManager())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var leaveRequest = InMemoryDatabase.LeaveRequests
                .FirstOrDefault(l => l.LeaveRequestId == leaveRequestId);

            if (leaveRequest == null)
            {
                return Json(new { success = false, message = "Leave request not found" });
            }

            if (leaveRequest.Status != LeaveStatus.Pending)
            {
                return Json(new { success = false, message = "This request has already been processed" });
            }

            if (decision == "Approve")
            {
                leaveRequest.Status = LeaveStatus.Approved;
                leaveRequest.ManagerNotes = managerNotes;
                leaveRequest.DecisionDate = DateTime.Now;
                leaveRequest.ReviewedBy = Session["FullName"] as string ?? Session["Username"] as string;
            }
            else if (decision == "Reject")
            {
                leaveRequest.Status = LeaveStatus.Rejected;
                leaveRequest.ManagerNotes = managerNotes;
                leaveRequest.DecisionDate = DateTime.Now;
                leaveRequest.ReviewedBy = Session["FullName"] as string ?? Session["Username"] as string;
            }
            else
            {
                return Json(new { success = false, message = "Invalid decision" });
            }

            return Json(new { success = true, message = $"Leave request {decision.ToLower()}d." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult RegisterFace(string faceDescriptor)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Json(new { success = false, message = "Please login first." });

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrEmpty(faceDescriptor))
                return Json(new { success = false, message = "No face data received. Please try again." });

            user.FaceData = faceDescriptor; // 128-value face descriptor, comma-separated
            return Json(new { success = true, message = "✅ Face registered successfully. You can now use Face Clock-In." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult ClockInWithFace(string faceDescriptor)
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Json(new { success = false, message = "Please login first." });

            var user = InMemoryDatabase.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrEmpty(user.FaceData))
                return Json(new { success = false, message = "No face registered yet. Please register your face first." });

            if (string.IsNullOrEmpty(faceDescriptor))
                return Json(new { success = false, message = "No face data received. Please try again." });

            var today = DateTime.Today;
            var existing = InMemoryDatabase.Attendances.FirstOrDefault(a => a.StaffId == userId && a.Date == today);
            if (existing != null && existing.ClockInTime.HasValue)
                return Json(new { success = false, message = "You are already clocked in today." });

            double distance = GetFaceDistance(user.FaceData, faceDescriptor);
            const double MATCH_THRESHOLD = 0.55; // lower = stricter match. 0.5-0.6 is the standard range for face-api.js

            if (distance > MATCH_THRESHOLD)
            {
                var confidence = Math.Max(0, (1 - distance) * 100);
                return Json(new { success = false, message = $"❌ Face does not match. Please try again or use ID method. (Match: {confidence:F0}%)" });
            }

            var matchScore = Math.Max(0, (1 - distance) * 100);
            var nextId = InMemoryDatabase.Attendances.Any() ? InMemoryDatabase.Attendances.Max(a => a.AttendanceId) + 1 : 1;

            var attendance = new Attendance
            {
                AttendanceId = nextId,
                StaffId = userId,
                Date = today,
                ClockInTime = DateTime.Now,
                Status = AttendanceStatus.Present,
                CreatedDate = DateTime.Now
            };
            InMemoryDatabase.Attendances.Add(attendance);

            return Json(new { success = true, message = $"✅ Face verified! Clocked in at {DateTime.Now:HH:mm} (Match: {matchScore:F0}%)" });
        }

        // Euclidean distance between two 128-dimension face descriptors — the real face-recognition math
        private double GetFaceDistance(string storedDescriptor, string capturedDescriptor)
        {
            try
            {
                var stored = storedDescriptor.Split(',').Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                var captured = capturedDescriptor.Split(',').Select(v => double.Parse(v, System.Globalization.CultureInfo.InvariantCulture)).ToArray();

                if (stored.Length != captured.Length || stored.Length == 0)
                    return double.MaxValue;

                double sumSquares = 0;
                for (int i = 0; i < stored.Length; i++)
                {
                    double diff = stored[i] - captured[i];
                    sumSquares += diff * diff;
                }
                return Math.Sqrt(sumSquares);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Face distance error: " + ex.Message);
                return double.MaxValue;
            }
        }
    }
}