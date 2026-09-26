using EnglishLearning.Application.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnglishLearning.Api.Controllers;

[ApiController, Authorize, Route("api/v1/billing")]
public sealed class BillingController : ControllerBase
{
    [HttpPost("google-play/verify")]
    public ActionResult VerifyGooglePlayPurchase(VerifyGooglePurchaseRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProductId) || string.IsNullOrWhiteSpace(request.PurchaseToken))
            return BadRequest(new { message = "ProductId and purchaseToken are required." });
        return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Google Play server verification is not configured yet." });
    }
}
