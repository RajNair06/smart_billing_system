namespace SmartBillingSystem.Models.ViewModels;

public class BillResultViewModel
{
    public Bill Bill { get; set; } = new();
    public bool ShowRecommendations { get; set; } = true;
}
