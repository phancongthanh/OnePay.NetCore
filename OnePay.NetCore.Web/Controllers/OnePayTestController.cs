using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using OnePay.NetCore.Web.Services;

namespace OnePay.NetCore.Web.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OnePayTestController : ControllerBase
    {
        private readonly ILogger<OnePayTestController> _logger;
        private readonly IOnePayService _onePayService;
        private readonly IMemoryCache _cache;

        public OnePayTestController(ILogger<OnePayTestController> logger, IOnePayService onePayService, IMemoryCache cache)
        {
            _logger = logger;
            _onePayService = onePayService;
            _cache = cache;
        }

        [HttpPost]
        public async Task<string> CreatePaymentUrl([FromQuery] OnePayRequest request, [FromQuery] string? returnUrl = null)
        {
            if (string.IsNullOrEmpty(request.RequestCode)) return string.Empty;
            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Url.Action(nameof(GetResponse), new { code = request.RequestCode });
            }
            var url = await _onePayService.CreatePaymentLink(TestOnePayProcessor.TYPE, request, returnUrl);
            return url;
        }

        [HttpGet]
        public OnePayResponse? GetResponse(string code)
        {
            var response = _cache.Get<OnePayResponse>(code);
            return response;
        }
    }
}
