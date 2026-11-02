using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChesterMeatMatrix.Helpers
{
    public static class RoutingZoneHelper
    {
        // Delivery zones configuration
        private static readonly List<DeliveryZone> DeliveryZones = new List<DeliveryZone>
        {
            new DeliveryZone { Label = "Durban North / Umhlanga", Keywords = new[] { "umhlanga", "durban north", "la lucia", "umhlali", "glen anil" } },
            new DeliveryZone { Label = "Central Durban", Keywords = new[] { "durban", "ethekwini", "mobeni", "point", "south beach", "north beach" } },
            new DeliveryZone { Label = "Berea", Keywords = new[] { "berea", "morningside", "musgrave", "glenwood" } },
            new DeliveryZone { Label = "Westville / Pinetown / Hillcrest", Keywords = new[] { "westville", "pinetown", "hillcrest", "new germany", "kloof" } },
            new DeliveryZone { Label = "Chatsworth / Umlazi / Isipingo / Amanzimtoti", Keywords = new[] { "chatsworth", "umlazi", "isipingo", "amanzimtoti", "toti" } },
            new DeliveryZone { Label = "Phoenix / Verulam / Tongaat", Keywords = new[] { "phoenix", "verulam", "tongaat", "kwamashu" } }
        };

        public static bool IsDurbanArea(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return false;
            var lowerAddress = address.ToLowerInvariant();

            // Check if address contains any Durban-related keyword
            return DeliveryZones.Any(zone =>
                zone.Keywords.Any(keyword => lowerAddress.Contains(keyword)));
        }

        public static string GetAreaLabel(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return "Durban Area (Default)";  // Changed from "Unknown"

            var lowerAddress = address.ToLowerInvariant();

            var matchingZone = DeliveryZones.FirstOrDefault(zone =>
                zone.Keywords.Any(keyword => lowerAddress.Contains(keyword)));

            return matchingZone?.Label ?? "Durban Area (Default)";  // Changed from "Outside Durban Area"
        }

        public static List<string> GetAreaSequence()
        {
            return DeliveryZones.Select(z => z.Label).ToList();
        }

        private class DeliveryZone
        {
            public string Label { get; set; }
            public string[] Keywords { get; set; }
        }
    }
}