using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedModels
{
    public class Machine
    {
        public int MachineId { get; set; }
        public string UniqueId { get; set; }
        public string OsType { get; set; }
        public ICollection<Service> Services { get; set; }
    }

    public class Service
    {
        public int ServiceId { get; set; }
        public int MachineId { get; set; }
        public string ServiceName { get; set; }
        public string Status { get; set; }
        public string LastChecked { get; set; }
        public Machine Machine { get; set; }
    }
}
