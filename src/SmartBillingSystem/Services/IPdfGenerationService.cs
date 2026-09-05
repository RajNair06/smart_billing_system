using SmartBillingSystem.Models;

namespace SmartBillingSystem.Services;

public interface IPdfGenerationService
{
    byte[] GenerateBillPdf(Bill bill);
}
