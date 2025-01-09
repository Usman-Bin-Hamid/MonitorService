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

        public async Task SendManagementCommand(string machineUniqueId, string machine, string task)
        {
        
                await _hubContext.Clients.All.SendAsync("ReceiveMessage", machine, task);
        }
    }
}
