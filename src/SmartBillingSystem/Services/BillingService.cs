using SmartBillingSystem.Models;

namespace SmartBillingSystem.Services;

public class BillingService : IBillingService
{
    public decimal DefaultTaxRate { get; set; } = 0.18m;
    public string CurrencySymbol { get; set; } = "₹";

    public decimal CalculateTotal(decimal price, int quantity)
    {
        return price * quantity;
    }

    public decimal CalculateTotal(decimal price, int quantity, decimal discountPercent)
    {
        decimal subtotal = price * quantity;
        return subtotal - ApplyDiscount(subtotal, discountPercent);
    }

    public decimal CalculateTotal(List<BillItem> items)
    {
        return CalculateTotal(items, DefaultTaxRate);
    }

    public decimal CalculateTotal(List<BillItem> items, decimal taxRate)
    {
        decimal subtotal = items.Sum(i => i.UnitPrice * i.Quantity);
        decimal tax = subtotal * taxRate;
        return subtotal + tax;
    }

    public Bill CreateBill(List<BillItem> items)
    {
        return CreateBill(items, DefaultTaxRate);
    }

    public Bill CreateBill(List<BillItem> items, decimal taxRate)
    {
        return CreateBill(items, string.Empty, string.Empty, taxRate);
    }

    public Bill CreateBill(List<BillItem> items, string customerName, string customerContact)
    {
        return CreateBill(items, customerName, customerContact, DefaultTaxRate);
    }

    public Bill CreateBill(List<BillItem> items, string customerName, string customerContact, decimal taxRate)
    {
        var bill = new Bill
        {
            BillId = Guid.NewGuid().ToString("N")[..8].ToUpper(),
            DateCreated = DateTime.Now,
            CustomerName = customerName ?? string.Empty,
            CustomerContact = customerContact ?? string.Empty,
            TaxRate = taxRate
        };

        foreach (var item in items)
        {
            if (!string.IsNullOrWhiteSpace(item.ProductName) && item.Quantity > 0 && item.UnitPrice > 0)
                bill.AddItem(item);
        }

        return bill;
    }

    public decimal ApplyDiscount(decimal amount, decimal discountPercent)
    {
        return amount * (discountPercent / 100m);
    }

    public string FormatCurrency(decimal amount)
    {
        return $"{CurrencySymbol}{amount:N2}";
    }
}
