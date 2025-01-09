using Microsoft.AspNetCore.SignalR;
using ServiceMonitoringAPI.Hubs;

namespace ServiceMonitoringAPI.Hub
{
    public class HubService : IHubService
    {
        private readonly IHubContext<ServiceHub> _hubContext;

        public HubService(IHubContext<ServiceHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendManagementCommand(string machineUniqueId, string action, string serviceName)
        {
            await _hubContext.Clients.Group(machineUniqueId).SendAsync("ManageService", action, serviceName);
        }
    }
}
