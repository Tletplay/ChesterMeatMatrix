using System.Collections.Generic;

namespace ChesterMeatMatrix.Models
{
	public class RouteAssignmentViewModel
	{
		public List<RouteAssignmentStopViewModel> Stops { get; set; } = new List<RouteAssignmentStopViewModel>();
		public string OrderIds { get; set; }
		public string SuggestedAreaLabel { get; set; }
		public string RestrictionMessage { get; set; }
		public int SelectedStopCount { get; set; }
		public int GeocodedStopCount { get; set; }
		public decimal TotalWeightKg { get; set; }
		public int TotalPackages { get; set; }
		public double EstimatedDistanceKm { get; set; }
		public int DistinctAreaCount { get; set; }
	}

	public class RouteAssignmentStopViewModel
	{
		public int OrderId { get; set; }
		public string OrderNumber { get; set; }
		public string CustomerName { get; set; }
		public string DeliveryAddress { get; set; }
		public string AreaLabel { get; set; }
		public bool IsDurbanArea { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
	}
}