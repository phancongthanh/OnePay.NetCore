namespace OnePay.NetCore
{
    /// <summary>
    /// Cấu hình OnePay
    /// </summary>
    public class OnePayOptions
    {
        /// <summary>
        /// Host của OnePay
        /// </summary>
        public string ApiUrl { get; set; } = "https://mtf.onepay.vn/";
        public string ReturnURL { get; set; } = "/onepay/return";
        public string IPNURL { get; set; } = "/onepay/ipn";

        /// <summary>
        /// Tài khoản do OnePay cấp
        /// </summary>
        public string User { get; set; } = string.Empty;
        /// <summary>
        /// Mật khẩu do OnePay cấp
        /// </summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// Mã truy cập do OnePay cấp
        /// </summary>
        public string AccessCode { get; set; } = string.Empty;
        /// <summary>
        /// MerchantId do OnePay cấp
        /// </summary>
        public string Merchant { get; set; } = string.Empty;
        /// <summary>
        /// Mã Hash do OnePay cấp
        /// </summary>
        public string HashKey { get; set; } = string.Empty;
    }
}
