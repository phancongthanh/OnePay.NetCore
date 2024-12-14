using System.Collections.Generic;

namespace OnePay.NetCore
{
    public class OnePayRequest
    {
        /// <summary>
        /// Mã giao dịch là duy nhất
        /// </summary>
        public string RequestCode { get; set; } = OnePayExtensions.GenerateRandomString();

        /// <summary>
        /// Mã sản phẩm
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Số tiền
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Các thông tin của vpc (không bao gồm vpc_ trong key)
        /// </summary>
        public IDictionary<string, string> VPCData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Các thông tin tuy chỉnh (không bao gồm user_ trong key)
        /// </summary>
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
