using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace OnePay.NetCore
{
    public static class OnePayExtensions
    {
        /// <summary>
        /// Cấu hình OnePayOptions và OnePayService vào Dependency Injection container.
        /// </summary>
        /// <param name="services">IServiceCollection của ứng dụng.</param>
        /// <param name="configureOptions">Hàm cấu hình OnePayOptions.</param>
        /// <returns>IServiceCollection sau khi thêm cấu hình.</returns>
        public static IServiceCollection AddOnePay(this IServiceCollection services, Action<OnePayOptions> configureOptions)
            => AddOnePay<OnePayService>(services, configureOptions);

        /// <summary>
        /// Cấu hình OnePayOptions và OnePayService vào Dependency Injection container.
        /// </summary>
        /// <typeparam name="T">Dịch vụ xử lý tùy chỉnh triển khai IOnePayService</typeparam>
        /// <param name="services">IServiceCollection của ứng dụng.</param>
        /// <param name="configureOptions">Hàm cấu hình OnePayOptions.</param>
        /// <returns>IServiceCollection sau khi thêm cấu hình.</returns>
        public static IServiceCollection AddOnePay<T>(this IServiceCollection services, Action<OnePayOptions> configureOptions)
            where T : class, IOnePayService
        {
            if (configureOptions == null)
            {
                throw new ArgumentNullException(nameof(configureOptions), "ConfigureOptions cannot be null.");
            }

            services.AddHttpContextAccessor();
            services.AddHttpClient<IOnePayService, T>();

            // Đăng ký OnePayOptions từ hàm cấu hình
            services.Configure(configureOptions);

            // Đăng ký IOnePayService với OnePayService
            services.AddScoped<IOnePayService, T>();

            return services;
        }

        /// <summary>
        /// Cấu hình 2 endpoints để xử lý phản hồi của OnePay
        /// </summary>
        public static IEndpointRouteBuilder MapOnePay(this IEndpointRouteBuilder endpoints)
        {
            // Lấy thông tin cấu hình từ OnePayOptions
            var options = endpoints.ServiceProvider.GetRequiredService<IOptions<OnePayOptions>>().Value;

            // Endpoint đầu tiên để xử lý callback (ReturnURL) từ OnePay
            endpoints.MapGet(options.ReturnURL, async context =>
            {
                // Lấy dịch vụ OnePayService từ container DI
                var onePayService = context.RequestServices.GetRequiredService<IOnePayService>();

                // Lấy danh sách các processor đã được đăng ký
                var onePayProcessors = context.RequestServices.GetServices<IOnePayProcessor>().ToArray();

                // Xử lý callback và lấy thông tin trả về
                var (type, response, returnUrl) = await onePayService.ProcessCallBack();

                // Lọc các processor theo loại (nếu có loại phù hợp)
                var processors = onePayProcessors.Where(x => string.IsNullOrEmpty(x.Type) || x.Type == type).ToArray();

                // Gọi phương thức ProcessURL của các processor phù hợp
                foreach (var processor in processors) await processor.ProcessReturnURL(response);

                // Chuyển hướng người dùng đến URL đã đăng ký trong CreatePaymentLink
                context.Response.Redirect(returnUrl);
            });

            // Endpoint thứ hai để xử lý IPN từ OnePay
            endpoints.MapGet(options.IPNURL, async context =>
            {
                // Lấy dịch vụ OnePayService từ container DI
                var onePayService = context.RequestServices.GetRequiredService<IOnePayService>();

                // Lấy danh sách các processor đã được đăng ký
                var onePayProcessors = context.RequestServices.GetServices<IOnePayProcessor>().ToArray();

                // Xử lý callback và lấy thông tin trả về
                var (type, response, returnUrl) = await onePayService.ProcessCallBack();

                // Lọc các processor theo loại (nếu có loại phù hợp)
                var processors = onePayProcessors.Where(x => string.IsNullOrEmpty(x.Type) || x.Type == type).ToArray();

                // Gọi phương thức ProcessIPN của các processor phù hợp
                foreach (var processor in processors) await processor.ProcessIPN(response);

                // Trả về phản hồi cho OnePay xác nhận đã xử lý
                await context.Response.WriteAsync("responsecode=1&desc=confirm-success");
            });

            // Trả về danh sách endpoints đã cấu hình
            return endpoints;
        }

        /// <summary>
        /// Sinh mã ngẫu nhiên cho giao dịch
        /// </summary>
        /// <param name="length">Độ dài chuỗi</param>
        /// <param name="chars">Các ký tự trong mã</param>
        /// <returns>Mã</returns>
        public static string GenerateRandomString(int length = 12, string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
        {
            var random = new Random();
            char[] stringChars = new char[length];

            for (int i = 0; i < length; i++)
                stringChars[i] = chars[random.Next(chars.Length)];

            return new string(stringChars);
        }
    }
}
