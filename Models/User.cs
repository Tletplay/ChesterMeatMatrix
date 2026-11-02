using System;

namespace ChesterMeatMatrix.Models
{
    public class User
    {
        public int Id { get; set; }

        // login
        public string Username { get; set; }

        // plain for prototype only
        public string Password { get; set; }

        // Checker, Warehouse, SalesRep, Router, Driver, Admin, Receiver
        public string Role { get; set; }

        public string FullName { get; set; }

        // ADD THIS - Student/Employee ID for clock in/out
        public string EmployeeId { get; set; }

        public string FaceData { get; set; }  // Stores Base64 face image
    }
}