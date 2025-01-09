using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServiceMonitoringAPI.Context;
using SharedModels;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
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

        public async Task SendServiceStatus(string machineUniqueId, object statuses)
        {

            using JsonDocument doc = JsonDocument.Parse(statuses.ToString());
            JsonElement root = doc.RootElement;

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

            DateTime localTime = DateTime.Now; // Local time
            DateTime utcTime = localTime.ToUniversalTime(); // Converts to UTC



            foreach (JsonProperty serviceProperty in root.EnumerateObject())
            {
                var services = await _context.Services.FirstOrDefaultAsync(m => m.ServiceName == serviceProperty.Value.GetProperty("DisplayName").GetString());
                if (services is null)
                {
                    string serviceName = serviceProperty.Name;

                    _context.Services.Add(new Service
                    {
                        MachineId = machine.MachineId,
                        ServiceName = serviceProperty.Value.GetProperty("DisplayName").GetString(),
                        Status = serviceProperty.Value.GetProperty("OriginalStatus").GetString(),

                        LastChecked = "",
                    });
                }
                else
                {
                    string serviceName = serviceProperty.Name;
                    services.Status = serviceProperty.Value.GetProperty("OriginalStatus").GetString();
                    await _context.SaveChangesAsync();

                }




            }


            try
            {
                await _context.SaveChangesAsync();
            }catch(Exception e)
            {

            }


        }

        public async Task ManageService(string machine, string task)
        {

            await Clients.All.SendAsync("ReceiveServiceStatus", machine, task);
            // Logic to manage the service on the client machine
            // This could involve interacting with the operating system
            // Placeholder for actual implementation
            //Console.WriteLine($"Received command to {action} service {serviceName}");
        }
    }
}