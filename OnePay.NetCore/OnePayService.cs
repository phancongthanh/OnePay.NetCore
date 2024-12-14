using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace OnePay.NetCore
{
    public class OnePayService : IOnePayService
    {
        protected readonly OnePayOptions _options;
        protected readonly IHttpContextAccessor _httpContextAccessor;
        protected readonly HttpClient _httpClient;

        public OnePayService(IOptions<OnePayOptions> options, IHttpContextAccessor httpContextAccessor, HttpClient httpClient)
        {
            _httpContextAccessor = httpContextAccessor;
            _options = options.Value;
            _httpClient = httpClient;
        }

        public Task<string> CreatePaymentLink(string type, OnePayRequest request, string returnUrl)
        {
            // Tạo đối tượng VPCRequest với URL của API thanh toán
            var vpcRequest = new VPCRequest(_options.ApiUrl + "/paygate/vpcpay.op");
            // Cài đặt secret key dùng để tạo chữ ký bảo mật
            vpcRequest.SetSecureSecret(_options.HashKey);

            // Thêm các trường bắt buộc vào yêu cầu thanh toán
            vpcRequest.AddDigitalOrderField("vpc_Version", "2"); // Phiên bản API, mặc định là "2"
            vpcRequest.AddDigitalOrderField("vpc_Currency", "VND"); // Loại tiền tệ, ở đây là VNĐ
            vpcRequest.AddDigitalOrderField("vpc_Command", "pay"); // Lệnh thực hiện thanh toán
            vpcRequest.AddDigitalOrderField("vpc_AccessCode", _options.AccessCode); // Access Code cung cấp bởi OnePAY
            vpcRequest.AddDigitalOrderField("vpc_Merchant", _options.Merchant); // Merchant ID cung cấp bởi OnePAY
            vpcRequest.AddDigitalOrderField("vpc_Locale", "vn"); // Ngôn ngữ giao diện thanh toán, ở đây là tiếng Việt
            vpcRequest.AddDigitalOrderField("AgainLink", "http://onepay.vn");
            vpcRequest.AddDigitalOrderField("Title", "onepay paygate"); // Tiêu đề giao diện

            // Thêm URL callback để nhận kết quả trả về từ OnePAY sau khi thanh toán
            vpcRequest.AddDigitalOrderField("vpc_ReturnURL", GetAbsoluteUrl(_options.ReturnURL));

            // Thêm các thông tin giao dịch
            vpcRequest.AddDigitalOrderField("vpc_MerchTxnRef", request.RequestCode); // Mã giao dịch của Merchant
            vpcRequest.AddDigitalOrderField("vpc_OrderInfo", request.OrderCode); // Mã đơn hàng
            vpcRequest.AddDigitalOrderField("vpc_Amount", request.Amount.ToString("0", CultureInfo.InvariantCulture) + "00"); // Số tiền (thêm hai chữ số thập phân)

            // Thêm địa chỉ IP của khách hàng
            var ip = GetClientIp(); // Lấy địa chỉ IP của client
            vpcRequest.AddDigitalOrderField("vpc_TicketNo", ip);

            // Thêm thông tin xử lý
            vpcRequest.AddDigitalOrderField("user_Type", type); // Loại request
            vpcRequest.AddDigitalOrderField("user_returnUrl", returnUrl); // URL tùy chỉnh để quay lại sau khi xử lý

            // Thêm các dữ liệu VPC tuỳ chỉnh, nếu có
            if (request.VPCData != null)
                foreach (var item in request.Data)
                    vpcRequest.AddDigitalOrderField($"vpc_{item.Key}", item.Value);

            // Thêm các dữ liệu tuỳ chỉnh của người dùng, nếu có
            if (request.Data != null)
                foreach (var item in request.Data)
                    vpcRequest.AddDigitalOrderField($"user_{item.Key}", item.Value);

            // Tạo chuỗi truy vấn URL chứa toàn bộ thông tin giao dịch và chữ ký bảo mật
            var url = vpcRequest.Create3PartyQueryString();

            // Trả về đường dẫn thanh toán
            return Task.FromResult(url);
        }

        /// <summary>
        /// Lấy đường dẫn tuyệt đối cho returnUrl
        /// </summary>
        protected virtual string GetAbsoluteUrl(string relativePath)
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!");

            var request = httpContext.Request;
            var host = request.Host.HasValue ? request.Host.Value : throw new InvalidOperationException("Request host is missing.");
            var scheme = request.Scheme;

            return $"{scheme}://{host}{relativePath}";
        }

        /// <summary>
        /// Lấy Ip người dùng hiện tại
        /// </summary>
        protected virtual string? GetClientIp()
        {
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!");

            // Kiểm tra header X-Forwarded-For nếu ứng dụng chạy phía sau proxy hoặc load balancer
            var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].ToString();

            // Nếu header có giá trị, trả về địa chỉ IP đầu tiên (client thực sự)
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').FirstOrDefault();
            }

            // Nếu không có header X-Forwarded-For, lấy IP từ RemoteIpAddress
            return httpContext.Connection.RemoteIpAddress?.ToString();
        }

        public Task<(string type, OnePayResponse response, string returnUrl)> ProcessCallBack()
        {
            // Lấy thông tin HTTP context từ accessor
            var httpContext = _httpContextAccessor.HttpContext
                ?? throw new NullReferenceException("httpContext is null!"); // Kiểm tra null để đảm bảo context tồn tại

            // Tạo NameValueCollection để lưu trữ các tham số từ query string
            var nameValueCollection = new NameValueCollection();
            foreach (var key in httpContext.Request.Query.Keys) // Duyệt qua tất cả các key trong query string
            {
                nameValueCollection.Add(key, httpContext.Request.Query[key]); // Thêm key-value vào NameValueCollection
            }

            // Tạo đối tượng VPCRequest và thiết lập secure hash key
            var vpcRequest = new VPCRequest(_options.ApiUrl + "/paygate/vpcpay.op");
            vpcRequest.SetSecureSecret(_options.HashKey);

            // Xử lý phản hồi từ OnePay và kiểm tra chữ ký bảo mật (hash validation)
            var hashvalidateResult = vpcRequest.Process3PartyResponse(nameValueCollection);

            // Lấy mã phản hồi giao dịch (transaction response code)
            var vpc_TxnResponseCode = vpcRequest.GetTxnResponseCode();

            // Xác định trạng thái giao dịch dựa vào kết quả kiểm tra hash và mã phản hồi
            bool? result = hashvalidateResult == "CORRECTED" && vpc_TxnResponseCode.Trim() == "0" ? (bool?)true : // Giao dịch thành công
                           hashvalidateResult == "INVALIDATED" && vpc_TxnResponseCode.Trim() == "0" ? (bool?)null : // Giao dịch đang xử lý
                           false; // Giao dịch thất bại

            // Lấy các thông tin quan trọng từ phản hồi
            var requestCode = vpcRequest.GetResultField("vpc_MerchTxnRef"); // Mã giao dịch của merchant
            var type = vpcRequest.GetResultField("user_Type"); // Loại giao dịch (do user cung cấp)
            var code = vpcRequest.GetResultField("vpc_OrderInfo"); // Mã đơn hàng
            var returnUrl = vpcRequest.GetResultField("user_returnUrl"); // URL để người dùng quay lại

            // Chuyển đổi số tiền từ chuỗi sang kiểu decimal, nếu không chuyển được thì gán giá trị mặc định
            if (!decimal.TryParse(vpcRequest.GetResultField("vpc_Amount"), out var amount))
                amount = 0;
            amount /= 100; // Chia 100 để lấy giá trị thực (do API trả về số tiền nhân với 100)

            // Lấy các trường dữ liệu bắt đầu bằng "vpc_" và lưu vào dictionary
            var fields = vpcRequest.GetResult();
            var vpcData = fields
                .Where(x => x.Key.StartsWith("vpc_")) // Chỉ lấy các trường bắt đầu bằng "vpc_"
                .ToDictionary(x => x.Key.Substring(4), x => x.Value.ToString()); // Bỏ tiền tố "vpc_" trong key

            // Lấy các trường dữ liệu bắt đầu bằng "user_" (trừ một số trường cố định) và lưu vào dictionary
            var data = fields
                .Where(x => x.Key.StartsWith("user_")) // Chỉ lấy các trường bắt đầu bằng "user_"
                .Where(x => x.Key != "user_Type" && x.Key != "user_returnUrl") // Loại bỏ các trường "user_Type" và "user_returnUrl"
                .ToDictionary(x => x.Key.Substring(5), x => x.Value.ToString()); // Bỏ tiền tố "user_" trong key

            // Tạo đối tượng OnePayResponse chứa thông tin phản hồi
            var response = new OnePayResponse
            {
                Result = result, // Kết quả giao dịch
                RequestCode = requestCode, // Mã giao dịch
                OrderCode = code, // Mã đơn hàng
                Amount = amount, // Số tiền giao dịch
                VPCData = vpcData, // Các dữ liệu từ trường "vpc_"
                Data = data // Các dữ liệu từ trường "user_"
            };

            // Trả về tuple gồm loại giao dịch (type), phản hồi (response) và URL quay lại (returnUrl)
            return Task.FromResult((type, response, returnUrl));
        }

        public async Task<(bool? result, IDictionary<string, string> data)> QueryDR(string requestCode)
        {
            // URL endpoint của API OnePAY
            var url = $"{_options.ApiUrl}/msp/api/v1/vpc/invoices/queries";

            // Dữ liệu gửi trong body của yêu cầu POST
            var formData = new Dictionary<string, string>
            {
                { "vpc_Command", "queryDR" }, // Lệnh gọi API, mặc định là "queryDR"
                { "vpc_Version", "2" }, // Phiên bản API, mặc định là "2"
                { "vpc_MerchTxnRef", requestCode }, // Mã giao dịch cần truy vấn
                { "vpc_Merchant", _options.Merchant }, // Merchant ID cung cấp bởi OnePAY
                { "vpc_AccessCode", _options.AccessCode }, // Access Code cung cấp bởi OnePAY
                { "vpc_User", _options.User }, // Tên người dùng cung cấp bởi OnePAY
                { "vpc_Password", _options.Password } // Mật khẩu cung cấp bởi OnePAY
            };

            // Khởi tạo đối tượng VPCRequest để tạo hash bảo mật
            var vpcRequest = new VPCRequest(url);
            vpcRequest.SetSecureSecret(_options.HashKey); // Cài đặt secret key dùng để tạo hash

            // Thêm các trường dữ liệu vào đối tượng VPCRequest
            foreach (var field in formData)
                vpcRequest.AddDigitalOrderField(field.Key, field.Value);

            // Tạo chữ ký bảo mật (vpc_SecureHash) bằng thuật toán SHA-256
            var secureHash = vpcRequest.CreateSHA256Signature(true);
            formData.Add("vpc_SecureHash", secureHash); // Thêm chữ ký vào formData

            // Chuẩn bị nội dung gửi với định dạng application/x-www-form-urlencoded
            var content = new FormUrlEncodedContent(formData);

            // Gửi yêu cầu POST đến API
            var response = await _httpClient.PostAsync(url, content);

            // Kiểm tra nếu phản hồi thành công (HTTP 200)
            response.EnsureSuccessStatusCode();

            // Đọc phản hồi từ API dưới dạng Dictionary<string, string>
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>() ?? new Dictionary<string, string>();

            // Chuyển đổi dữ liệu nhận được thành NameValueCollection (dành cho xử lý sau)
            var nameValueCollection = new NameValueCollection();
            foreach (var key in data.Keys)
            {
                nameValueCollection.Add(key, data[key]);
            }

            // Kiểm tra tính toàn vẹn của hash trong phản hồi (vpc_SecureHash)
            var hashvalidateResult = vpcRequest.Process3PartyResponse(nameValueCollection);

            // Lấy mã phản hồi giao dịch (txnResponseCode)
            var vpc_TxnResponseCode = vpcRequest.GetTxnResponseCode();

            // Xác định kết quả giao dịch dựa trên hashvalidateResult và mã phản hồi
            bool? result = hashvalidateResult == "CORRECTED" && vpc_TxnResponseCode.Trim() == "0" ? (bool?)true :
                           hashvalidateResult == "INVALIDATED" && vpc_TxnResponseCode.Trim() == "0" ? (bool?)null : false;

            // Lấy danh sách các trường bắt đầu với "vpc_" từ kết quả trả về
            var fields = vpcRequest.GetResult();
            var vpcData = fields
                .Where(x => x.Key.StartsWith("vpc_")) // Lọc các trường có tiền tố "vpc_"
                .ToDictionary(x => x.Key.Substring(4), x => x.Value.ToString()); // Loại bỏ tiền tố "vpc_" khi tạo dictionary

            // Trả về kết quả giao dịch và dữ liệu vpc_
            return (result, vpcData);
        }
    }
}
