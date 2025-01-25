using LeaseERP.Shared.Enums;

namespace LeaseERP.Shared.DTOs
{
    public class BaseRequest
    {
        public OperationType Mode { get; set; }
        public string? ActionBy { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }
}
