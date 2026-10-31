using Cartify.Orders.Domain.Common;
using Cartify.Orders.Domain.Orders;

namespace Cartify.Orders.Domain.Tests.Orders;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CustomerId = Guid.NewGuid();
    private const string PaymentMethodId = "pm_test_123";

    private static OrderItem Item(decimal unitPrice = 10m, int quantity = 1) =>
        new(Guid.NewGuid(), quantity, unitPrice);

    private static Order CreateValidOrder() =>
        Order.Create(CustomerId, PaymentMethodId, [Item()], Now);

    // ---------- Create ----------

    [Fact]
    public void Create_ValidData_StartsPendingWithTotal()
    {
        var order = Order.Create(CustomerId, PaymentMethodId, [Item(10m, 2), Item(5m, 1)], Now);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(25m, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
        Assert.Equal(Now, order.CreatedAt);
        Assert.Null(order.CancellationReason);
    }

    [Fact]
    public void Create_NoItems_Throws()
    {
        Assert.Throws<DomainException>(() => Order.Create(CustomerId, PaymentMethodId, [], Now));
    }

    [Fact]
    public void Create_EmptyCustomerId_Throws()
    {
        Assert.Throws<DomainException>(() => Order.Create(Guid.Empty, PaymentMethodId, [Item()], Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyPaymentMethod_Throws(string paymentMethodId)
    {
        Assert.Throws<DomainException>(() => Order.Create(CustomerId, paymentMethodId, [Item()], Now));
    }

    [Fact]
    public void Create_DuplicateProduct_Throws()
    {
        var productId = Guid.NewGuid();
        OrderItem[] items = [new(productId, 1, 10m), new(productId, 2, 10m)];

        Assert.Throws<DomainException>(() => Order.Create(CustomerId, PaymentMethodId, items, Now));
    }

    [Fact]
    public void Create_TwoOrders_GenerateDifferentIds()
    {
        var first = CreateValidOrder();
        var second = CreateValidOrder();

        Assert.NotEqual(first.Id, second.Id);
    }

    // ---------- OrderItem ----------

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void OrderItem_InvalidQuantity_Throws(int quantity)
    {
        Assert.Throws<DomainException>(() => new OrderItem(Guid.NewGuid(), quantity, 10m));
    }

    [Fact]
    public void OrderItem_ZeroPrice_Throws()
    {
        Assert.Throws<DomainException>(() => new OrderItem(Guid.NewGuid(), 1, 0m));
    }

    [Fact]
    public void OrderItem_EmptyProductId_Throws()
    {
        Assert.Throws<DomainException>(() => new OrderItem(Guid.Empty, 1, 10m));
    }

    [Fact]
    public void OrderItem_SameValues_AreEqual()
    {
        var productId = Guid.NewGuid();

        var first = new OrderItem(productId, 2, 10m);
        var second = new OrderItem(productId, 2, 10m);

        Assert.Equal(first, second);
    }

    // ---------- Transições ----------

    [Fact]
    public void MarkStockReserved_FromPending_ChangesStatus()
    {
        var order = CreateValidOrder();

        order.MarkStockReserved();

        Assert.Equal(OrderStatus.StockReserved, order.Status);
    }

    [Fact]
    public void MarkStockReserved_Twice_Throws()
    {
        var order = CreateValidOrder();
        order.MarkStockReserved();

        Assert.Throws<DomainException>(order.MarkStockReserved);
    }

    [Fact]
    public void Confirm_FromPending_Throws()
    {
        var order = CreateValidOrder();

        Assert.Throws<DomainException>(order.Confirm);
    }

    [Fact]
    public void Confirm_FromStockReserved_ChangesStatus()
    {
        var order = CreateValidOrder();
        order.MarkStockReserved();

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
    }

    [Fact]
    public void Cancel_FromPending_SetsReason()
    {
        var order = CreateValidOrder();

        order.Cancel(CancellationReason.OutOfStock);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(CancellationReason.OutOfStock, order.CancellationReason);
    }

    [Fact]
    public void Cancel_FromStockReserved_SetsReason()
    {
        var order = CreateValidOrder();
        order.MarkStockReserved();

        order.Cancel(CancellationReason.PaymentRejected);

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(CancellationReason.PaymentRejected, order.CancellationReason);
    }

    [Fact]
    public void Cancel_FromConfirmed_Throws()
    {
        var order = CreateValidOrder();
        order.MarkStockReserved();
        order.Confirm();

        Assert.Throws<DomainException>(() => order.Cancel(CancellationReason.OutOfStock));
    }

    [Fact]
    public void Confirm_FromCancelled_Throws()
    {
        var order = CreateValidOrder();
        order.Cancel(CancellationReason.OutOfStock);

        Assert.Throws<DomainException>(order.Confirm);
    }
}
