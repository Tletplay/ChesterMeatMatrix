namespace ChesterMeatMatrix.Models
{
    public class RouterDashboardViewModel
    {
        public int PendingCount { get; set; }
        public int ActiveCount { get; set; }
        public int DurbanPendingCount { get; set; }
        public int OutsidePendingCount { get; set; }
        public decimal PendingWeightKg { get; set; }
        public int AvailableDrivers { get; set; }
        public int AvailableTrucks { get; set; }
    }
}