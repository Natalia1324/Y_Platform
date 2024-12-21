namespace Y_Platform.Models
{
    public class ErrorViewModel
    {
        /// <summary>
        /// Model widoku błędu
        /// </summary>
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
