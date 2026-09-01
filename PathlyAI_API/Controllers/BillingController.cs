using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    /// <summary>
    /// Billing endpoints: the logged-in user's live entitlement state, checkout initiation,
    /// and the payment provider webhook (public, authenticated by signature instead).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class BillingController : ControllerBase
    {
        private readonly IBillingServiceInterface _Billing;
        private readonly IPaymentGatewayInterface _Gateway;

        public BillingController(IBillingServiceInterface billing,
                                 IPaymentGatewayInterface gateway)
        {
            _Billing = billing ?? throw new ArgumentNullException(nameof(billing));
            _Gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        /// <summary>The caller's current plan, quotas and month-to-date usage.</summary>
        [Authorize]
        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            return Ok(await _Billing.GetUsageSummaryAsync(userId));
        }

        /// <summary>Starts a hosted checkout for the given plan code and returns the payment URL.</summary>
        [Authorize]
        [HttpPost("checkout/{planCode}")]
        public async Task<IActionResult> StartCheckout(string planCode)
        {
            var userId = User.FindFirstValue("extension_userId");

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { message = "A valid access token is required." });
            }

            try
            {
                var result = await _Billing.InitiateCheckoutAsync(userId, planCode);

                return result.Success ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
            }
            catch (PlanNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Payment provider webhook. Public by necessity — authenticity comes from the
        /// signature header, never from trusting the payload.
        /// </summary>
        [AllowAnonymous]
        [HttpPost("webhooks/paystack")]
        public async Task<IActionResult> PaystackWebhook()
        {
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            var signature = Request.Headers["x-paystack-signature"].FirstOrDefault() ?? string.Empty;

            if (!_Gateway.VerifyWebhookSignature(rawBody, signature))
            {
                return Unauthorized(new { message = "Invalid webhook signature." });
            }

            using var doc = System.Text.Json.JsonDocument.Parse(rawBody);

            var eventName = doc.RootElement.GetProperty("event").GetString();
            var data = doc.RootElement.GetProperty("data");

            if (eventName == "charge.success")
            {
                var reference = data.TryGetProperty("reference", out var r) ? r.GetString() : null;

                if (!string.IsNullOrWhiteSpace(reference))
                {
                    await _Billing.MarkPaymentSucceededAsync(reference, null);
                }
            }

            return Ok();
        }
    }
}
