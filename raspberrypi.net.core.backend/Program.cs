using System;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;

namespace raspberrypi.net.core.backend;

class Program
{
    private static ServiceClient _serviceClient;
    private const string _deviceId = "rpihome";
    private const string methodName = "TurnOnLight";
    private const string _deviceConnectionString = "HostName=Reihax-IoT-Hub.azure-devices.net;DeviceId=rpihome;SharedAccessKey=KmxifXkLoU+ZbgG9IlzjkkiaUqfMOMrTMAlW8HUCe4U=";

    static async Task Main(string[] args)
    {
        _serviceClient = ServiceClient.CreateFromConnectionString(_deviceConnectionString);
        await InvokeDirectMethod(methodName);
        Console.WriteLine("Hello World!");
    }

    private static async Task InvokeDirectMethod(string methodName)
    {
        var invocation = new CloudToDeviceMethod(methodName)
        {
            ResponseTimeout = TimeSpan.FromSeconds(45)
        };
        invocation.SetPayloadJson("5");
        var response = await _serviceClient.
        InvokeDeviceMethodAsync(_deviceId, invocation);
        Console.WriteLine(response.GetPayloadAsJson());
    }
}