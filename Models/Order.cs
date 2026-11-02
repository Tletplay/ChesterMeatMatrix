using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ChesterMeatMatrix.Models
{
    // Add these enums
    public enum OrderStatusEnum
    {
        Pending = 0,      // Waiting for payment confirmation
        Confirmed = 1,    // Payment confirmed (Card/EFT verified)
        Processing = 2,   // Being picked/packed
        OutForDelivery = 3,
        Delivered = 4,
        Cancelled = 5
    }

    public enum PaymentMethodEnum
    {
        Card = 0,      // Stripe - automatic confirmation
        EFT = 1,       // Bank transfer - manual admin approval
        Cash = 2,      // On delivery - instore customers only
        Enterprise = 3 // Monthly billing - no immediate payment
    }

    public class Order
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsInstore { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal Total { get; set; }

        // NEW: Order workflow status
        public OrderStatusEnum Status { get; set; } = OrderStatusEnum.Pending;

        // NEW: Payment method selected
        public PaymentMethodEnum PaymentMethod { get; set; }

        // NEW: For EFT orders - admin approval
        public bool IsPaymentConfirmed { get; set; } = false;
        public DateTime? PaymentConfirmedAt { get; set; }
        public string ConfirmedBy { get; set; }

        // Stripe session ID for webhook
        public string StripeSessionId { get; set; }

        // Keep existing for compatibility
        public bool IsInvoiced { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string PaymentMethodString { get; set; }

        [Display(Name = "Total Weight (kg)")]
        public decimal TotalWeightKg { get; set; }

        // Navigation property for claims (ADD THIS)
        public virtual ICollection<Claim> Claims { get; set; }

        // Constructor to initialize collections
        public Order()
        {
            Items = new List<OrderItem>();
            Claims = new List<Claim>();
        }
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order Order { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // ADD THIS PROPERTY - Comma-separated list of assigned item codes from QR scanning
        [System.ComponentModel.DataAnnotations.StringLength(1000)]
        public string AssignedItemCodes { get; set; }
    }
}