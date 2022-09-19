using System;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;

namespace raspberrypi.net.core.backend;

class Program
{
    private static ServiceClient _serviceClient;
    private const string _deviceId = "rpihome";
    private const string methodName = "TurnOnLight";
    private const string _deviceConnectionString = "HostName=Reihax-IoT-Hub-2.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=HZLOwfewOWrzdzTzjZaS2oBwRVhbYQIgEH5RXKzgJYg=";
    static async Task Main(string[] args)
    {
        try{
            _serviceClient = ServiceClient.CreateFromConnectionString(_deviceConnectionString);
            await InvokeDirectMethod(methodName);   
            Console.WriteLine("Direct method on Device called.");
        }catch(Exception ex){
            Console.WriteLine($"Could not invoke direct call: {ex.Message}");
        }
    }

    private static async Task InvokeDirectMethod(string methodName)
    {
        var invocation = new CloudToDeviceMethod(methodName)
        {
            ResponseTimeout = TimeSpan.FromSeconds(45)
        };
        invocation.SetPayloadJson("5");
        var response = await _serviceClient.InvokeDeviceMethodAsync(_deviceId, invocation);
        Console.WriteLine($"Response payload: {response.GetPayloadAsJson()}");
    }
}