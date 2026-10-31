namespace Cartify.Orders.Domain.Orders
{
    public enum OrderStatus
    {
        Pending = 1,
        StockReserved = 2,
        Confirmed = 3,
        Cancelled = 4
    }
}
