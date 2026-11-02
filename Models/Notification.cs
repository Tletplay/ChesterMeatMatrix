using System;

namespace ChesterMeatMatrix.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Driver's User Id
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; } // "Route", "Alert", "Info"
        public int? ReferenceId { get; set; } // Route Id
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }

        public Notification()
        {
            IsRead = false;
            CreatedAt = DateTime.UtcNow;
        }
    }
}