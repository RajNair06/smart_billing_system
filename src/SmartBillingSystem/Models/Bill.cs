namespace SmartBillingSystem.Models;

public class Bill
{
    public string BillId { get; set; } = string.Empty;
    public DateTime DateCreated { get; set; } = DateTime.Now;
    public List<BillItem> Items { get; set; } = new();
    public decimal TaxRate { get; set; } = 0.18m;
    public List<string> Recommendations { get; set; } = new();

    public decimal Subtotal => Items.Sum(i => i.LineTotal);
    public decimal TaxAmount => Subtotal * TaxRate;
    public decimal GrandTotal => Subtotal + TaxAmount;

    public void AddItem(BillItem item)
    {
        item.Id = Items.Count + 1;
        Items.Add(item);
    }

    public void RemoveItem(int itemId)
    {
        var item = Items.FirstOrDefault(i => i.Id == itemId);
        if (item != null)
            Items.Remove(item);
    }

    public string GetSummary()
    {
        return $"Bill #{BillId} | {Items.Count} items | Total: {GrandTotal:C}";
    }
}
