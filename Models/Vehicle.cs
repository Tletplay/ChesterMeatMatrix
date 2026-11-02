namespace ChesterMeatMatrix.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Registration { get; set; }
        public string Model { get; set; }
        public int Capacity { get; set; }

        // NEW: For truck weighing
        public decimal EmptyWeightKg { get; set; } = 0;
        public decimal MaxLoadCapacityKg { get; set; } = 0;
        public decimal CurrentLoadWeightKg { get; set; } = 0;
    }
}