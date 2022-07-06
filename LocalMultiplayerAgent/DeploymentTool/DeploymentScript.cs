using Microsoft.Azure.Gaming.LocalMultiplayerAgent.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using System.IO;
using Microsoft.Azure.Gaming.LocalMultiplayerAgent.DeploymentTool;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Microsoft.Azure.Gaming.LocalMultiplayerAgent.MPSDeploymentTool
{
    public class DeploymentScript
    {
        private readonly MultiplayerSettings settings;
        private readonly DeploymentSettings settingsDeployment;
        public DeploymentScript(MultiplayerSettings multiplayerSettings)
        {
            settings = multiplayerSettings;
            settingsDeployment = JsonConvert.DeserializeObject<DeploymentSettings>(File.ReadAllText("DeploymentTool/deployment.json"));
        }

        public async Task RunScriptAsync()
        {
            const string buildFriendlyName = "bbb";
            string buildName = $"{buildFriendlyName}_{Guid.NewGuid()}";

            PlayFabSettings.staticSettings.TitleId = settings.TitleId;

            string secret = Environment.GetEnvironmentVariable("PF_SECRET");
            if (string.IsNullOrEmpty(secret))
            {
                Console.WriteLine("Enter developer secret key");
                PlayFabSettings.staticSettings.DeveloperSecretKey = Console.ReadLine();
            }

           var req = new PlayFab.AuthenticationModels.GetEntityTokenRequest();
            var res = await PlayFabAuthenticationAPI.GetEntityTokenAsync(req);

            List<PlayFab.MultiplayerModels.Port> ports = null;

            foreach (var portList in settings.PortMappingsList)
            {
                ports = portList?.Select(x => new PlayFab.MultiplayerModels.Port()
                {
                    Name = x.GamePort.Name,
                    Num = x.GamePort.Number,
                    Protocol = (ProtocolType)Enum.Parse(typeof(ProtocolType), x.GamePort.Protocol)
                }).ToList();
            }

            /*CreateBuildWithProcessBasedServerRequest processBuildRequest = new()
            {
                VmSize = AzureVmSize.Standard_D1_v2,
                GameCertificateReferences = null,
                Ports = ports,
                MultiplayerServerCountPerVm = 1,
                RegionConfigurations = settingsDeployment.RegionConfigurations?.Select(x => new BuildRegionParams()
                {
                    Region = "WestUS",
                    MaxServers = 1,
                    StandbyServers = 1,
                    MultiplayerServerCountPerVm = 1,
                    VmSize = AzureVmSize.Standard_D1_v2

                }).ToList(),
                BuildName = settingsDeployment.BuildName,
                GameAssetReferences = settings.AssetDetails?.Select(x => new AssetReferenceParams()
                {
                    FileName = "gameassets.zip"
                }).ToList(),
                StartMultiplayerServerCommand = @"wrapper.exe -g fakegame.exe arg1 arg2",
                //buildRequest.GameWorkingDirectory = @"C:\Assets";
                OsPlatform = "Windows"
            };*/

            CreateBuildWithManagedContainerRequest winConBuildRequest = new()
            {
                VmSize = AzureVmSize.Standard_D1_v2,
                GameCertificateReferences = null,
                Ports = ports,
                MultiplayerServerCountPerVm = 1,
                RegionConfigurations = settingsDeployment.RegionConfigurations?.Select(x => new BuildRegionParams()
                {
                    Region = "WestUS",
                    MaxServers = 1,
                    StandbyServers = 1,
                    MultiplayerServerCountPerVm = 1,
                    VmSize = AzureVmSize.Standard_D1_v2

                }).ToList(),
                BuildName = settingsDeployment.BuildName,
                GameAssetReferences = settings.AssetDetails?.Select(x => new AssetReferenceParams()
                {
                    FileName = "gameassets.zip",
                    MountPath = x.MountPath
                }).ToList(),
                StartMultiplayerServerCommand = settings.ContainerStartParameters.StartGameCommand,
                //buildRequest.GameWorkingDirectory = @"C:\Assets";
                ContainerFlavor = "Windows" == settingsDeployment.OSPlatform ? ContainerFlavor.ManagedWindowsServerCore : ContainerFlavor.CustomLinux
            };

            GetAssetDownloadUrlRequest downloadRequest = new() { FileName = "gamified.zip" };

            var uriDownResult = await PlayFabMultiplayerAPI.GetAssetDownloadUrlAsync(downloadRequest);

            if (uriDownResult == null)
            {
                GetAssetUploadUrlRequest request1 = new() { FileName = "gamified.zip" };

                var uriResult = await PlayFabMultiplayerAPI.GetAssetUploadUrlAsync(request1);

                //check for error
                if (uriResult.Error != null)
                {
                    Console.WriteLine(uriResult.Error.ErrorMessage);
                }
                var uri = new System.Uri(uriResult.Result.AssetUploadUrl);

                var blockBlob = new CloudBlockBlob(uri);
                var file = @"C:\\github\\PlayFab\\MpsSamples\\wrappingGsdk\\drop\\gameassets.zip";  //your file location
                await blockBlob.UploadFromFileAsync(file);
            }

            else if (uriDownResult.Error != null)
            {
                Console.WriteLine($"{uriDownResult.Error.ErrorMessage}");
            }

            

            Console.WriteLine($"Starting deployment {buildName} for titleId, regions  {string.Join(", ", winConBuildRequest.RegionConfigurations.Select(x => x.Region))}");

            var res2 = await PlayFabMultiplayerAPI.CreateBuildWithManagedContainerAsync(winConBuildRequest); 

            if (res2.Error != null)
            {
                foreach(var err in res2.Error.ErrorDetails)
                {
                    foreach (var mess in err.Value)
                    {
                        Console.WriteLine($"{mess}");
                    }
                    
                }
                
            }
            Console.WriteLine("done");
            }
        }
    }

  
