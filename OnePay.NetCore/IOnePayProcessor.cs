using System.Threading.Tasks;

namespace OnePay.NetCore
{
    /// <summary>
    /// Đăng ký 1 processor xử lý phản hồi của OnePay
    /// </summary>
    public interface IOnePayProcessor
    {
        /// <summary>
        /// Loại giao dịch đăng ký xử lý, string.Empty nếu muốn xử lý tất cả
        /// </summary>
        string Type => string.Empty;

        /// <summary>
        /// Xử lý trong trường hợp OnePay returnUrl
        /// </summary>
        /// <param name="response">Dữ liệu từ phản hồi của OnePay</param>
        Task ProcessReturnURL(OnePayResponse response) => Task.CompletedTask;

        /// <summary>
        /// Xử lý trong trường hợp OnePay IPN
        /// </summary>
        /// <param name="response">Dữ liệu từ phản hồi của OnePay</param>
        Task ProcessIPN(OnePayResponse response) => Task.CompletedTask;
    }
}
