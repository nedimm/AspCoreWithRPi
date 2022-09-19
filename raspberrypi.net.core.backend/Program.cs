using System;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;
using System.Text;

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
            await SendCloudToDeviceMessageAsync();
            await InvokeDirectMethod(methodName);   
            await ReceiveDeliveryFeedback();
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

    private static async Task SendCloudToDeviceMessageAsync()
    {
        var message = new Message(Encoding.ASCII.GetBytes("This is a message from cloud."));
        message.Ack = DeliveryAcknowledgement.Full; // This is to request the feedback
        await _serviceClient.SendAsync(_deviceId, message);
    }

    private static async Task ReceiveDeliveryFeedback()
    {
        var feedbackReceiver = _serviceClient.GetFeedbackReceiver();
        var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        while (true)
        {
            var feedback = await feedbackReceiver.ReceiveAsync(token);
            if (feedback == null) continue;
            Console.WriteLine($"The feedback status is:{string.Join(",", feedback.Records.Select(s =>s.StatusCode))}");
            await feedbackReceiver.CompleteAsync(feedback, token);
        }
    }
}