using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServiceMonitoringAPI.Context;
using SharedModels;
using System.Threading.Tasks;

namespace ServiceMonitoringAPI.Hubs
{
    public class ServiceHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly ApplicationDbContext _context;

        public ServiceHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public async override Task OnConnectedAsync()
        {
            // Assuming the client sends the machine unique ID during connection
            var machineUniqueId = Context.GetHttpContext().Request.Query["machineUniqueId"].ToString();
            if (!string.IsNullOrEmpty(machineUniqueId))
            {
                // Add the connection to the machine's group
                await Groups.AddToGroupAsync(Context.ConnectionId, machineUniqueId);
            }

            await base.OnConnectedAsync();
        }

        public async override Task OnDisconnectedAsync(Exception exception)
        {
            // Remove the connection from the machine's group
            var machineUniqueId = Context.GetHttpContext().Request.Query["machineUniqueId"].ToString();
            if (!string.IsNullOrEmpty(machineUniqueId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, machineUniqueId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendServiceStatus(string machineUniqueId, List<ServiceStatus> statuses)
        {
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.UniqueId == machineUniqueId);
            if (machine == null)
            {
                machine = new Machine
                {
                    UniqueId = machineUniqueId,
                    OsType = "Unknown"
                };
                _context.Machines.Add(machine);
                await _context.SaveChangesAsync();
            }

            foreach (var status in statuses)
            {
                _context.Services.Add(new Service
                {
                    MachineId = machine.MachineId,
                    ServiceName = status.DisplayName,
                    Status = status.StandardizedStatus,
                    LastChecked = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();

           
        }

        public async Task ManageService(string action, string serviceName)
        {

            await Clients.All.SendAsync("ReceiveServiceStatus", action, serviceName);
            // Logic to manage the service on the client machine
            // This could involve interacting with the operating system
            // Placeholder for actual implementation
            //Console.WriteLine($"Received command to {action} service {serviceName}");
        }
    }
}