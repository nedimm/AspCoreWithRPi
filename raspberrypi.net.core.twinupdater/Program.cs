using System;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;

namespace raspberrypi.net.core.twinupdater;
class Program{
    static ServiceClient _serviceClient;
    const string _deviceId = "rpihome";
    const string _deviceConnectionString = "HostName=Reihax-IoT-Hub-2.azure-devices.net;SharedAccessKeyName=serviceRegistryRead;SharedAccessKey=RtVfaafpkIkmzRT+bZ8kJfAPCwK3L/pQNLIK+29Ix8A=";
    static RegistryManager _registryManager;  

    static async Task Main(string[] args){
        try{
            _serviceClient = ServiceClient.CreateFromConnectionString(_deviceConnectionString);
            _registryManager = RegistryManager.CreateFromConnectionString(_deviceConnectionString);
            await UpdateTwin();
            Console.WriteLine("Device twin updated");
        }catch(Exception ex){
            Console.WriteLine($"Could not update device twin: {ex.Message}");
        }
    }

    static async Task UpdateTwin()
    {
        var twin = await _registryManager.GetTwinAsync(_deviceId);
        var toUpdate = @"{
            tags:{
                location: {
                    region: 'DE'
                }
            },
            properties: {
                desired: {
                    telemetryConfig: {
                        sendFrequency: '5m'
                    },
                    $metadata: {
                        $lastUpdated: '2020-07-14T10:47:29.8590777Z'
                    },
                    $version: 1
                }
            }
        }";
        await _registryManager.UpdateTwinAsync(_deviceId, toUpdate, twin.ETag);
    }
}