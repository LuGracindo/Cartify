using Cartify.Orders.Domain.Common;

namespace Cartify.Orders.Domain.Orders;

public sealed class Order
{
    private readonly List<OrderItem> _items = [];

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PaymentMethodId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public CancellationReason? CancellationReason { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
    public decimal TotalAmount => _items.Sum(item => item.Subtotal);

    private Order()
    {
    }

    public static Order Create(
        Guid customerId,
        string paymentMethodId,
        IEnumerable<OrderItem> items,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (customerId == Guid.Empty)
            throw new DomainException("Customer is required.");

        if (string.IsNullOrWhiteSpace(paymentMethodId))
            throw new DomainException("Payment method is required.");

        var itemList = items.ToList();

        if (itemList.Count == 0)
            throw new DomainException("An order must have at least one item.");

        var distinctProducts = itemList.Select(item => item.ProductId).Distinct().Count();
        if (distinctProducts != itemList.Count)
            throw new DomainException("The same product cannot appear twice in an order.");

        var order = new Order
        {
            Id = Guid.CreateVersion7(),
            CustomerId = customerId,
            PaymentMethodId = paymentMethodId,
            Status = OrderStatus.Pending,
            CreatedAt = now
        };

        order._items.AddRange(itemList);

        return order;
    }

    public void MarkStockReserved()
    {
        EnsureStatus(OrderStatus.Pending);
        Status = OrderStatus.StockReserved;
    }

    public void Confirm()
    {
        EnsureStatus(OrderStatus.StockReserved);
        Status = OrderStatus.Confirmed;
    }

    public void Cancel(CancellationReason reason)
    {
        EnsureStatus(OrderStatus.Pending, OrderStatus.StockReserved);
        Status = OrderStatus.Cancelled;
        CancellationReason = reason;
    }

    private void EnsureStatus(params OrderStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException($"Order is {Status} and cannot perform this operation.");
    }
}