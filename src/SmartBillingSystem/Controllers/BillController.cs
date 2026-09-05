using Microsoft.AspNetCore.Mvc;
using SmartBillingSystem.Models;
using SmartBillingSystem.Models.ViewModels;
using SmartBillingSystem.Services;

namespace SmartBillingSystem.Controllers;

public class BillController : Controller
{
    private readonly IBillingService _billingService;
    private readonly IGeminiService _geminiService;
    private readonly IPdfGenerationService _pdfService;

    public BillController(
        IBillingService billingService,
        IGeminiService geminiService,
        IPdfGenerationService pdfService)
    {
        _billingService = billingService;
        _geminiService = geminiService;
        _pdfService = pdfService;
    }

    public IActionResult Create()
    {
        return View(new CreateBillViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(List<BillItem>? items)
    {
        if (items == null || !items.Any())
        {
            ModelState.AddModelError("", "No items were submitted. Please add at least one product.");
            return View(new CreateBillViewModel());
        }

        var bill = _billingService.CreateBill(items);

        if (!bill.Items.Any())
        {
            ModelState.AddModelError("", "Please add at least one valid product with name, price, and quantity.");
            return View(new CreateBillViewModel { Items = items });
        }

        var productNames = bill.Items.Select(i => i.ProductName).ToList();
        try
        {
            bill.Recommendations = await _geminiService.GetBillRecommendationsAsync(productNames);
        }
        catch (Exception)
        {
            bill.Recommendations = new List<string>();
        }

        return View("Result", new BillResultViewModel { Bill = bill });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendation(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return Json(new { recommendations = new List<string>() });

        var result = await _geminiService.GetProductRecommendationAsync(productName);
        return Json(new { recommendations = result.Recommendations });
    }

    public IActionResult Result(string? billId)
    {
        return View(new BillResultViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DownloadPdf(List<BillItem> items)
    {
        var bill = _billingService.CreateBill(items);
        var pdfBytes = _pdfService.GenerateBillPdf(bill);
        return File(pdfBytes, "application/pdf", $"Bill-{bill.BillId}.pdf");
    }
}
