namespace ServiceMonitoringAPI.Hub
{
    public interface IHubService
    {
       
            Task SendManagementCommand(string machineUniqueId, string action, string serviceName);
        
    }
}
