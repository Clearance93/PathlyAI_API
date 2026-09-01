using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pathly_DTOs;
using PathlyInterfaces.IService;

namespace PathlyAI_API.Controllers
{
    /// <summary>Public pricing catalogue.</summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class PlansController : ControllerBase
    {
        private readonly IBillingServiceInterface _Billing;

        public PlansController(IBillingServiceInterface billing)
        {
            _Billing = billing ?? throw new ArgumentNullException(nameof(billing));
        }

        [HttpGet]
        public async Task<IActionResult> GetPlans()
        {
            var plans = await _Billing.GetActivePlansAsync();

            return Ok(plans);
        }
    }
}
