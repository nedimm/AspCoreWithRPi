using System;
using System.Threading;
using Iot.Device.CpuTemperature;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using raspberrypi.net.core.Models;
using System.Text;
using Microsoft.Azure.Devices.Shared;

namespace raspberrypi.net.core;

class Program
{
    private static CpuTemperature _rpiCpuTemp = new CpuTemperature();
    private const string _deviceConnectionString = "HostName=Reihax-IoT-Hub-2.azure-devices.net;DeviceId=rpihome;SharedAccessKey=kULhrzx+ySUI9LmpuF/6UQkGyRB/ZpzVCQ/uwCm8FkM=";
    private static int _messageId = 0;
    private static DeviceClient _deviceClient;
    private const double _temperatureThreshold = 40.0;
    public const string DeviceId = "rpihome";
    static bool _enableDirectMethodInvocation = true;
    static bool _sendCpuTemp = true;
    static bool _updateDeviceTwin = true;
    static bool _receiveCloudToDeviceMessages = false;

    static async Task Main(string[] args)
    {
        try
        {
            _deviceClient = DeviceClient.CreateFromConnectionString(_deviceConnectionString, TransportType.Mqtt);
            EnableDirectMethodInvocation();
            await UpdateDeviceTwin().ConfigureAwait(false);
            await SendCpuTemperature();
            await ReceiveCloudToDeviceMessageAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static void EnableDirectMethodInvocation()
    {
        if (_enableDirectMethodInvocation)
            _deviceClient.SetMethodHandlerAsync(nameof(TurnOnLight), TurnOnLight, null).Wait();
    }

    private static async Task UpdateDeviceTwin()
    {
        if (!_updateDeviceTwin)return;
        await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null).ConfigureAwait(false);
        var twin = await _deviceClient.GetTwinAsync();
        Console.WriteLine($"Initial Twin: {twin.ToJson()}");

        var telemetryConfig = new TwinCollection();
        telemetryConfig["sendFrequency"] = "5m";
        var reportedProperties = new TwinCollection();
        reportedProperties["telemetryConfig"] = telemetryConfig;

        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
        Console.WriteLine("Waiting 30 seconds for IoT Hub Twin updates...");
        await Task.Delay(3 * 1000);
    }

    private static async Task SendCpuTemperature()
    {
        if (!_sendCpuTemp) return;
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
        messageToSend.Properties.Add("TemperatureAlert", (tempCelsius > _temperatureThreshold) ? "true" : "false");
        await _deviceClient.SendEventAsync(messageToSend).ConfigureAwait(false);
    }

    private static async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
    {
        Console.WriteLine($"New desired property is {desiredProperties.ToJson()}");
        var reportedProperties = new TwinCollection();
        var telemetryConfig = new TwinCollection();
        telemetryConfig["status"] = "success";
        reportedProperties["telemetryConfig"] = telemetryConfig;
        await _deviceClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
    }

    private static async Task ReceiveCloudToDeviceMessageAsync()
    {
        if (!_receiveCloudToDeviceMessages)return;
        while (true)
        {
            var cloudMessage = await _deviceClient.ReceiveAsync();
            if (cloudMessage == null) continue;
            Console.WriteLine($"The received message is:{Encoding.ASCII.GetString(cloudMessage.GetBytes())}");
            await _deviceClient.CompleteAsync(cloudMessage); // Send feedback
        }
    }
}
