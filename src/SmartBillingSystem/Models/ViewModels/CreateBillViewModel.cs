namespace SmartBillingSystem.Models.ViewModels;

public class CreateBillViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerContact { get; set; } = string.Empty;
    public List<BillItem> Items { get; set; } = new()
    {
        new BillItem { Id = 0, ProductName = "", UnitPrice = 0, Quantity = 1 }
    };
}
