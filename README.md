# OnePay.NetCore

OnePay.NetCore là một thư viện được thiết kế để tích hợp cổng thanh toán OnePay vào các ứng dụng .NET Core. Nó giúp đơn giản hóa quá trình tạo liên kết thanh toán, xử lý callback (ReturnURL, IPN) và xử lý phản hồi thanh toán.

## Tính năng

- **Tích hợp Dependency Injection**: Dễ dàng cấu hình `OnePayOptions` và `OnePayService` với Dependency Injection.
- **Dịch vụ Thanh toán Tùy chỉnh**: Cho phép sử dụng các triển khai tùy chỉnh của interface `IOnePayService` để xử lý thanh toán.
- **Xử lý Callback**: Hỗ trợ tự động cấu hình các endpoints để xử lý các callback từ OnePay (ReturnURL, IPN).
- **Tạo Liên kết Thanh toán**: Cho phép tạo liên kết thanh toán cho các giao dịch OnePay.
- **Tích hợp Processor**: Hỗ trợ thêm các processor tùy chỉnh để xử lý dữ liệu trả về từ OnePay.

## Cài đặt

Để cài đặt OnePay.NetCore, sử dụng NuGet Package Manager hoặc .NET CLI:

```bash
dotnet add package OnePay.NetCore
```

## Cấu hình và Thiết lập

### 1. Cấu hình OnePay trong `Startup.cs` hoặc `Program.cs`

Trong `Startup.cs` hoặc `Program.cs` của ứng dụng, đăng ký các dịch vụ của OnePay và cấu hình các tùy chọn.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddOnePay(options => Configuration.GetSection("OnePay").Bind(options));
    services.AddTransient<IOnePayProcessor, TestOnePayProcessor>();
}
```

### 2. Định tuyến các Endpoints của OnePay

Trong phương thức `Configure`, cấu hình các endpoint để xử lý các phản hồi ReturnURL và IPN từ OnePay.

```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    app.MapOnePay();
}
```

### 3. Tạo Liên kết Thanh toán

Sử dụng `OnePayService` để tạo một liên kết thanh toán cho giao dịch.

```csharp
public class SomeClass
{
    private readonly IOnePayService _onePayService;

    public SomeClass(IOnePayService onePayService)
    {
        _onePayService = onePayService;
    }

    public async Task CreateLink(OnePayRequest request, string returnUrl)
    {
        var url = await _onePayService.CreatePaymentLink(TestOnePayProcessor.TYPE, request, returnUrl);
    }
}
```

## Tùy chỉnh Dịch vụ Thanh toán

Bạn có thể ghi đè `OnePayService` hoặc triển khai dịch vụ `IOnePayService` của riêng mình và đăng ký nó trong DI container. Dưới đây là ví dụ:

```csharp
public class CustomOnePayService : OnePayService
{
    // Ghi đè các phương thức xử lý yêu cầu ở đây
}
```

Sau đó, đăng ký dịch vụ trong DI container:

```csharp
services.AddOnePay<CustomOnePayService>(options => Configuration.GetSection("OnePay").Bind(options));
```

## Xử lý Callback từ OnePay

Thư viện tự động cấu hình hai endpoint để xử lý callback URLs từ OnePay, bao gồm ReturnURL và IPN:

1. **ReturnURL**: Xử lý qua endpoint `MapGet(options.ReturnURL)`.
2. **IPN**: Xử lý qua endpoint `MapGet(options.IPNURL)`.

Bạn có thể tùy chỉnh cách xử lý phản hồi bằng cách triển khai interface `IOnePayProcessor` và đăng ký các processor tùy chỉnh.

Ví dụ về một processor đơn giản:

```csharp
public class TestOnePayProcessor : IOnePayProcessor
{
    public const string TYPE = nameof(TestOnePayProcessor);
    private readonly ILogger<TestOnePayProcessor> _logger;

    public string Type => TYPE;

    public TestOnePayProcessor(ILogger<TestOnePayProcessor> logger)
    {
        _logger = logger;
    }

    public Task ProcessReturnURL(OnePayResponse response)
    {
        _logger.LogInformation("Đang xử lý ReturnURL cho mã yêu cầu: {0}", response.RequestCode);
        return Task.CompletedTask;
    }

    public Task ProcessIPN(OnePayResponse response)
    {
        _logger.LogInformation("Đang xử lý IPN cho mã yêu cầu: {0}", response.RequestCode);
        return Task.CompletedTask;
    }
}
```

### Đăng ký Processor Tùy chỉnh

Đăng ký processor tùy chỉnh như sau:

```csharp
services.AddTransient<IOnePayProcessor, TestOnePayProcessor>();
```

## Cấu hình Ví dụ

```json
{
  "OnePay": {
    "ApiUrl": "https://mtf.onepay.vn/",
    "AccessCode": "6BEB2546",
    "Merchant": "TESTONEPAY",
    "HashKey": "6D0870CDE5F24F34F3915FB0045120DB"
  }
}
```

## Giấy phép

OnePay.NetCore là một thư viện mã nguồn mở và được cấp phép dưới [Giấy phép MIT](LICENSE). Bạn có thể tự do sử dụng, sửa đổi và phân phối thư viện trong các dự án của mình.

## Cảnh báo

- Để đảm bảo tính bảo mật, tác giả khuyến khích bạn tham khảo/tải về và tùy chỉnh mã nguồn của thư viện này để phù hợp với yêu cầu bảo mật cao hơn cho hệ thống của bạn trong môi trường sản xuất với các giao dịch thanh toán tiền **THẬT**.
- Mặc dù thư viện này đã được thiết kế để hỗ trợ tích hợp OnePay, bạn có thể tùy chỉnh và tối ưu mã nguồn để đáp ứng các yêu cầu bảo mật cao hơn, nếu cần thiết.
- Hướng dẫn chi tiết về quy trình tích hợp dịch vụ thanh toán OnePay có sẵn tại [**đây**](https://mtf.onepay.vn/developer/resource/documents/docx/quy_trinh_tich_hop-quocte.pdf).
