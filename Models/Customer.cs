using System;

namespace ChesterMeatMatrix.Models
{
    // Add this enum at the top of the file
    public enum CustomerTypeEnum
    {
        Standard = 0,   // Online customers - Card/EFT only
        Instore = 1,    // Can use Cash on Delivery
        Enterprise = 2  // B2B - Monthly billing
    }

    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }

        // Keep existing for backward compatibility
        public bool IsEnterprise { get; set; }

        // NEW: Explicit customer type
        public CustomerTypeEnum CustomerType { get; set; } = CustomerTypeEnum.Standard;

        public string ContactPhone { get; set; }
        public string Address { get; set; }

        // NEW: For EFT and Enterprise tracking
        public decimal OutstandingBalance { get; set; } = 0m;
        public decimal MonthlyCreditLimit { get; set; } = 50000m;
        public decimal CurrentMonthDebt { get; set; } = 0m;
    }
}