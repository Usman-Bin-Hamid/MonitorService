using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ServiceMonitoringAPI.Context;
using ServiceMonitoringAPI.Hub;
using ServiceMonitoringAPI.Hubs;
using SharedModels;

namespace ServiceMonitoringAPI.Controllers
{
    public class MachinesController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubService _hubContext;


        public MachinesController(ApplicationDbContext context, IHubService hubContext)
        {
            _hubContext = hubContext;
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Machine>>> GetMachines()
        {
            return await _context.Machines.ToListAsync();
        }
        [HttpPost("ManageService")]
        public async Task<IActionResult> ManageService([FromBody] ServiceManagementRequest request)
        {
            var machine = await _context.Machines.FirstOrDefaultAsync(m => m.UniqueId == request.MachineUniqueId);
            if (machine == null)
            {
                return NotFound();
            }

            await _hubContext.SendManagementCommand(machine.UniqueId, request.Action, request.ServiceName);

            return Ok();
        }

        // POST: api/Machines
        //[HttpPost]
        //public async Task<ActionResult<Machine>> PostMachine(Machine machine)
        //{
        //    _context.Machines.Add(machine);
        //    await _context.SaveChangesAsync();
        //    return CreatedAtAction(nameof(GetMachines), new { id = machine.MachineId }, machine);
        //}
    }
    


}
