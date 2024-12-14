using Microsoft.Extensions.Caching.Memory;
using OnePay.NetCore.Web.Controllers;
using System.Text.Json;

namespace OnePay.NetCore.Web.Services
{
    public class TestOnePayProcessor : IOnePayProcessor
    {
        public const string TYPE = nameof(TestOnePayProcessor);

        private readonly ILogger<OnePayTestController> _logger;
        private readonly IMemoryCache _cache;

        public string Type => TYPE;

        public TestOnePayProcessor(ILogger<OnePayTestController> logger, IMemoryCache cache)
        {
            _logger = logger;
            _cache = cache;
        }

        public Task ProcessReturnURL(OnePayResponse response)
        {
            _cache.Set(response.RequestCode, response);

            var data = JsonSerializer.Serialize(response);
            _logger.LogInformation("Process ReturnURL, response: {0}", data);
            return Task.CompletedTask;
        }
    }
}
