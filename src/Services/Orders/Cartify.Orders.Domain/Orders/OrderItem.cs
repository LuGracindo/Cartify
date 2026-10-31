using Cartify.Orders.Domain.Common;

namespace Cartify.Orders.Domain.Orders
{
    public sealed record OrderItem
    {
        public Guid ProductId { get; }
        public int Quantity { get; }
        public decimal UnitPrice { get; }

        public decimal Subtotal => Quantity * UnitPrice;

        public OrderItem(Guid productId, int quantity, decimal unitPrice)
        {
            if (productId == Guid.Empty)
                throw new DomainException("Product is required.");

            if (quantity <= 0)
                throw new DomainException("Quantity must be greater than zero.");

            if (unitPrice <= 0)
                throw new DomainException("Unit price must be greater than zero.");

            ProductId = productId;
            Quantity = quantity;
            UnitPrice = unitPrice;
        }
    }
}
