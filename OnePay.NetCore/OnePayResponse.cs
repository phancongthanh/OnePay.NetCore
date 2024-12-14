using System.Collections.Generic;

namespace OnePay.NetCore
{
    /// <summary>
    /// Thông tin phản hồi của OnePay
    /// </summary>
    public class OnePayResponse
    {
        /// <summary>
        /// Kết quả thanh toán, null - đang xử lý, true - thành công, false - thất bại
        /// </summary>
        public bool? Result { get; set; } = null;

        /// <summary>
        /// Mã giao dịch gửi đi
        /// </summary>
        public string RequestCode { get; set; } = string.Empty;

        /// <summary>
        /// Mã sản phẩm gửi đi
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;

        /// <summary>
        /// Số tiền đã thanh toán
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Danh sách các trường dữ liệu OnePay trả về (không bao gồm vpc_ trong key)
        /// </summary>
        public IDictionary<string, string> VPCData { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Danh sách các trường dữ liệu tự tạo trong request gửi đi (không bao gồm user_ trong key)
        /// </summary>
        public IDictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
