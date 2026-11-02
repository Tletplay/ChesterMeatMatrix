using System.Text;

namespace ChesterMeatMatrix.Helpers
{
    public static class DeliveryAddressFormatter
    {
        public static string BuildAddress(string addressLine1, string addressLine2, string suburb, string city, string province, string postalCode)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(addressLine1))
                sb.Append(addressLine1.Trim());

            if (!string.IsNullOrWhiteSpace(addressLine2))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(addressLine2.Trim());
            }

            if (!string.IsNullOrWhiteSpace(suburb))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(suburb.Trim());
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(city.Trim());
            }

            if (!string.IsNullOrWhiteSpace(province))
            {
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(province.Trim());
            }

            if (!string.IsNullOrWhiteSpace(postalCode))
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(postalCode.Trim());
            }

            return sb.ToString();
        }

        public static string BuildSpecialInstructions(string contactName, string contactEmail, string specialInstructions)
        {
            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(contactName))
                sb.AppendLine($"Contact: {contactName.Trim()}");

            if (!string.IsNullOrWhiteSpace(contactEmail))
                sb.AppendLine($"Email: {contactEmail.Trim()}");

            if (!string.IsNullOrWhiteSpace(specialInstructions))
                sb.AppendLine($"Notes: {specialInstructions.Trim()}");

            return sb.ToString().TrimEnd();
        }
    }
}