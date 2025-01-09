namespace SharedModels
{
    public class Config
    {
        public string SignalRUrl { get; set; }
    }

    public class ServiceStatus
    {
        public string StandardizedStatus { get; set; }
        public string OriginalStatus { get; set; }
        public string DisplayName { get; set; }
    }

    public class ServiceManagementRequest
    {
        public string MachineUniqueId { get; set; }
        public string Action { get; set; }
        public string ServiceName { get; set; }
    }
}
