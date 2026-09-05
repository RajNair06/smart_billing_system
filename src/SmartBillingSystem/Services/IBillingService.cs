using SmartBillingSystem.Models;

namespace SmartBillingSystem.Services;

public interface IBillingService
{
    decimal CalculateTotal(decimal price, int quantity);
    decimal CalculateTotal(decimal price, int quantity, decimal discountPercent);
    decimal CalculateTotal(List<BillItem> items);
    decimal CalculateTotal(List<BillItem> items, decimal taxRate);
    Bill CreateBill(List<BillItem> items);
    Bill CreateBill(List<BillItem> items, decimal taxRate);
    decimal ApplyDiscount(decimal amount, decimal discountPercent);
    string FormatCurrency(decimal amount);
}
