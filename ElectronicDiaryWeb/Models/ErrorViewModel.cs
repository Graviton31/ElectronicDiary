namespace ElectronicDiaryWeb.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public string? Message { get; set; }

        public string? Details { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
