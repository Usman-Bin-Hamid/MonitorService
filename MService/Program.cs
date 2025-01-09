using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using SharedModels;
using Timer = System.Timers.Timer;

namespace ServiceMonitor
{
    class Program
    {
        private static HubConnection _hubConnection;
        private static Timer _timer;
        private static Dictionary<string, ServiceStatus> _previousServiceStatus;

        static void Main(string[] args)
        {
            // Read configuration
            var config = JsonConvert.DeserializeObject<Config>(File.ReadAllText("appsettings.json"));

            // Initialize SignalR connection
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(config.SignalRUrl)
                .Build();

            // Start the connection
            _hubConnection.StartAsync().GetAwaiter().GetResult();

            // Register for commands from SignalR
            _hubConnection.On<string, string>("ReceiveMessage", ManageService);

            // Setup timer to monitor services every 10 seconds
            _timer = new Timer(10000);
            _timer.Elapsed += OnTimedEvent;
            _timer.Start();

            // Send initial service list
            SendServiceStatus();

            Console.WriteLine("Service monitor started. Press Enter to exit.");
            Console.ReadLine();

            // Stop timer and hub connection
            _timer.Stop();
            _hubConnection.StopAsync().GetAwaiter().GetResult();
        }

        private static void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            //SendServiceStatus();
        }

        private static void SendServiceStatus()
        {
            var currentServiceStatus = GetServicesStatus();
            if (_previousServiceStatus != null && AreDictionariesEqual(_previousServiceStatus, currentServiceStatus))
            {
                // No changes detected
                return;
            }

            try
            {
                var serviceStatusJson = JsonConvert.SerializeObject(currentServiceStatus);
                _hubConnection.InvokeAsync("SendServiceStatus","static", serviceStatusJson).GetAwaiter().GetResult();
                _previousServiceStatus = currentServiceStatus;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending service status: {ex.Message}");
            }
        }

        private static Dictionary<string, ServiceStatus> GetServicesStatus()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsServicesStatus();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxServicesStatus();
            }
            else
            {
                return new Dictionary<string, ServiceStatus>();
            }
        }

        private static Dictionary<string, ServiceStatus> GetWindowsServicesStatus()
        {
            var services = ServiceController.GetServices();
            var serviceStatus = new Dictionary<string, ServiceStatus>();
            foreach (var service in services)
            {
                serviceStatus[service.DisplayName] = new ServiceStatus
                {

                    StandardizedStatus = service.Status.ToString(),
                    OriginalStatus = service.Status.ToString(),
                    DisplayName = service.DisplayName
                };
            }
            return serviceStatus;
        }

        private static Dictionary<string, ServiceStatus> GetLinuxServicesStatus()
        {
            var output = RunShellCommand("systemctl list-units --type=service --all --no-pager --plain --no-legend");
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var serviceStatus = new Dictionary<string, ServiceStatus>();

            foreach (var line in lines)
            {
                var parts = Regex.Split(line.Trim(), @"\s+");
                if (parts.Length >= 4)
                {
                    var serviceName = parts[0];
                    var activeState = parts[2];
                    var displayNameParts = parts.Skip(4).ToList();
                    var displayName = string.Join(" ", displayNameParts);
                    var standardizedStatus = activeState switch
                    {
                        "active" => "Running",
                        "inactive" => "Stopped",
                        "failed" => "Failed",
                        _ => "Unknown"
                    };
                    serviceStatus[serviceName] = new ServiceStatus
                    {
                        StandardizedStatus = standardizedStatus,
                        OriginalStatus = activeState,
                        DisplayName = displayName
                    };
                }
            }

            return serviceStatus;
        }

        //private static Dictionary<string, ServiceStatus> GetLinuxServicesStatus()
        //{
        //    var output = RunShellCommand("systemctl list-units --type=service --all --no-pager");
        //    // Parse output and populate service status dictionary
        //    // Placeholder for actual implementation
        //    return new Dictionary<string, ServiceStatus>();
        //}

        private static void ManageService(string machine, string task)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ManageWindowsService(task, machine);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ManageLinuxService(task, machine);
            }
            else
            {
                Console.WriteLine("Unsupported OS");
            }
        }

        private static void ManageWindowsService(string action, string serviceName)
        {
            try
            {
                var service = new ServiceController(serviceName);
                switch (action.ToLower())
                {
                    case "start":
                        service.Start();
                        break;
                    case "stop":
                        service.Stop();
                        break;
                    case "restart":
                        service.Stop();
                        service.Start();
                        break;
                    default:
                        Console.WriteLine("Invalid action");
                        break;
                }
                Console.WriteLine($"Service {serviceName} {action}ed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error managing service: {ex.Message}");
            }
        }

        private static void ManageLinuxService(string action, string serviceName)
        {
            try
            {
                var command = $"systemctl {action} {serviceName}";
                var output = RunShellCommand(command);
                Console.WriteLine(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error managing service: {ex.Message}");
            }
        }

        private static string RunShellCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private static bool AreDictionariesEqual<TKey, TValue>(Dictionary<TKey, TValue> dict1, Dictionary<TKey, TValue> dict2)
        {
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var pair in dict1)
            {
                TValue value;
                if (!dict2.TryGetValue(pair.Key, out value))
                    return false;

                if (!EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                    return false;
            }
            return true;
        }
    }

   
}