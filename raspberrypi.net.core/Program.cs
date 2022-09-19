using System;
using System.Threading;
using Iot.Device.CpuTemperature;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using raspberrypi.net.core.Models;
using System.Text;

namespace raspberrypi.net.core;

class Program
{
    private static CpuTemperature _rpiCpuTemp = new CpuTemperature();
    private const string _deviceConnectionString = "HostName=Reihax-IoT-Hub-2.azure-devices.net;DeviceId=rpihome;SharedAccessKey=kULhrzx+ySUI9LmpuF/6UQkGyRB/ZpzVCQ/uwCm8FkM=";
    private static int _messageId = 0;
    private static DeviceClient _deviceClient;
    private const double _temperatureThreshold = 40.0;
    public const string DeviceId = "rpihome";

    static async Task Main(string[] args)
    {
        _deviceClient = DeviceClient.CreateFromConnectionString(
            _deviceConnectionString, TransportType.Mqtt);
        // Create a handler for the direct method call
        _deviceClient.SetMethodHandlerAsync(nameof(TurnOnLight), TurnOnLight, null).Wait();
        while (true)
        {
            if (_rpiCpuTemp.IsAvailable)
            {
                Console.WriteLine($"{_messageId}:The CPU temperature at {DateTime.Now} is {_rpiCpuTemp.Temperature}");
                await SendToIoTHub(_rpiCpuTemp.Temperature.DegreesCelsius);
                Console.WriteLine("The device data has been sent");
            }
            Thread.Sleep(5000); // Sleep for 5 seconds
        }
    }

    private static Task<MethodResponse> TurnOnLight(MethodRequest methodRequest, object userContext)
    {
        var messageText = methodRequest.DataAsJson == "5" ? 
                "Here is the direct method call from backend to turn on the light!" : 
                "Here is the method call from cloud to turn on the light!";
        Console.WriteLine(messageText);
        var result = "{\"result\":\"Executed direct method:" + methodRequest.Name + "\"}";
        return Task.FromResult(new MethodResponse(Encoding.
        UTF8.GetBytes(result), 200));
    }
    
    private static async Task SendToIoTHub(double tempCelsius)
    {
        string jsonData = JsonConvert.SerializeObject(
            new DeviceData(){
                MessageId = _messageId++,
                Temperature = tempCelsius
            });
        var messageToSend = new Message(Encoding.UTF8.GetBytes(jsonData));
        messageToSend.Properties.Add("TemperatureAlert", 
                    (tempCelsius > _temperatureThreshold) ? "true" : "false");
        await _deviceClient.SendEventAsync(messageToSend).ConfigureAwait(false);
    }
}
