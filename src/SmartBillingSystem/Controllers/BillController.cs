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
    private readonly ILogger<BillController> _logger;

    public BillController(
        IBillingService billingService,
        IGeminiService geminiService,
        IPdfGenerationService pdfService,
        ILogger<BillController> logger)
    {
        _billingService = billingService;
        _geminiService = geminiService;
        _pdfService = pdfService;
        _logger = logger;
    }

    public IActionResult Create()
    {
        return View(new CreateBillViewModel());
    }

    [HttpPost]
    public IActionResult Create(CreateBillViewModel model)
    {
        if (model.Items == null || !model.Items.Any())
        {
            ModelState.AddModelError("", "No items were submitted. Please add at least one product.");
            return View(new CreateBillViewModel());
        }

        var bill = _billingService.CreateBill(
            model.Items, 
            model.CustomerName ?? string.Empty, 
            model.CustomerContact ?? string.Empty
        );

        if (!bill.Items.Any())
        {
            ModelState.AddModelError("", "Please add at least one valid product with name, price, and quantity.");
            return View(model);
        }

        return View("Result", new BillResultViewModel { Bill = bill });
    }

    [HttpGet]
    public async Task<IActionResult> GetRecommendation(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return Json(new { recommendations = new List<string>() });

        try
        {
            var result = await _geminiService.GetProductRecommendationAsync(productName);
            return Json(new { recommendations = result.Recommendations });
        }
        catch
        {
            return Json(new { recommendations = new List<string>() });
        }
    }

    public IActionResult Result(string? billId)
    {
        return View(new BillResultViewModel());
    }

    [HttpPost]
    public IActionResult DownloadPdf(List<BillItem> items, string? customerName, string? customerContact)
    {
        var bill = _billingService.CreateBill(
            items, 
            customerName ?? string.Empty, 
            customerContact ?? string.Empty
        );
        var pdfBytes = _pdfService.GenerateBillPdf(bill);
        return File(pdfBytes, "application/pdf", $"Bill-{bill.BillId}.pdf");
    }
}
