namespace SmartBillingSystem.Models.ViewModels;

public class CreateBillViewModel
{
    public List<BillItem> Items { get; set; } = new()
    {
        new BillItem { Id = 0, ProductName = "", UnitPrice = 0, Quantity = 1 }
    };
}
