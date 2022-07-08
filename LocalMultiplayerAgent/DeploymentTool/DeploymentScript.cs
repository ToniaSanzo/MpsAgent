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

        public CreateBuildWithCustomContainerRequest GetCustomContainerRequest(List<PlayFab.MultiplayerModels.Port> ports)
        {
            return new CreateBuildWithCustomContainerRequest
            {
                BuildName = settingsDeployment.BuildName,
                VmSize = (AzureVmSize)Enum.Parse(typeof(AzureVmSize), settingsDeployment.VmSize),
                ContainerFlavor = ContainerFlavor.CustomLinux,
                ContainerImageReference = new ContainerImageReference()
                {
                    ImageName = settings.ContainerStartParameters.ImageDetails.ImageName,
                    Tag = settings.ContainerStartParameters.ImageDetails.ImageTag
                },
                Ports = ports,
                ContainerRunCommand = settings.ContainerStartParameters.StartGameCommand,
                RegionConfigurations = settingsDeployment.RegionConfigurations?.Select(x => new BuildRegionParams()
                {
                    Region = x.Region,
                    MaxServers = x.MaxServers,
                    StandbyServers = x.StandbyServers,
                    MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
                    VmSize = (AzureVmSize)Enum.Parse(typeof(AzureVmSize), settingsDeployment.VmSize)

                }).ToList(),
                MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
            };
        }

        public CreateBuildWithManagedContainerRequest GetManagedContainerRequest(List<PlayFab.MultiplayerModels.Port> ports)
        {
            return new CreateBuildWithManagedContainerRequest
            {
                VmSize = (AzureVmSize)Enum.Parse(typeof(AzureVmSize), settingsDeployment.VmSize),
                GameCertificateReferences = null,
                Ports = ports,
                MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
                RegionConfigurations = settingsDeployment.RegionConfigurations?.Select(x => new BuildRegionParams()
                {
                    Region = x.Region,
                    MaxServers = x.MaxServers,
                    StandbyServers = x.StandbyServers,
                    MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
                    VmSize = (AzureVmSize)Enum.Parse(typeof(AzureVmSize), settingsDeployment.VmSize)

                }).ToList(),
                BuildName = settingsDeployment.BuildName,
                GameAssetReferences = settings.AssetDetails?.Select(x => new AssetReferenceParams()
                {
                    FileName = x.LocalFilePath,
                    MountPath = x.MountPath
                }).ToList(),
                StartMultiplayerServerCommand = settings.ContainerStartParameters.StartGameCommand,
                ContainerFlavor = ContainerFlavor.ManagedWindowsServerCore
            };
        }

        public CreateBuildWithProcessBasedServerRequest GetProcessBasedServerRequest(List<PlayFab.MultiplayerModels.Port> ports)
        {
            return new CreateBuildWithProcessBasedServerRequest
            {
                VmSize = (AzureVmSize)Enum.Parse(typeof(AzureVmSize), settingsDeployment.VmSize),
                GameCertificateReferences = null,
                Ports = ports,
                MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
                RegionConfigurations = settingsDeployment.RegionConfigurations?.Select(x => new BuildRegionParams()
                {
                    Region = x.Region,
                    MaxServers = x.MaxServers,
                    StandbyServers = x.StandbyServers,
                    MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
                    VmSize = (AzureVmSize)Enum.Parse(typeof(AzureVmSize), settingsDeployment.VmSize)

                }).ToList(),
                BuildName = settingsDeployment.BuildName,
                GameAssetReferences = settings.AssetDetails?.Select(x => new AssetReferenceParams()
                {
                    FileName = x.LocalFilePath,
                    MountPath = x.MountPath
                }).ToList(),
                StartMultiplayerServerCommand = settings.ProcessStartParameters.StartGameCommand,
                OsPlatform = settingsDeployment.OSPlatform
            };
        }

        public async Task<PlayFabResult<CreateBuildWithProcessBasedServerResponse>> CreateBuildWithProcessBasedServer(CreateBuildWithProcessBasedServerRequest request)
        {
            Console.WriteLine($"Starting deployment {request.BuildName} for titleId, regions  {string.Join(", ", request.RegionConfigurations.Select(x => x.Region))}");

            return await PlayFabMultiplayerAPI.CreateBuildWithProcessBasedServerAsync(request);
        }

        public async Task<PlayFabResult<CreateBuildWithManagedContainerResponse>> CreateBuildWithManagedContainer(CreateBuildWithManagedContainerRequest request)
        {
            Console.WriteLine($"Starting deployment {request.BuildName} for titleId, regions  {string.Join(", ", request.RegionConfigurations.Select(x => x.Region))}");

            return await PlayFabMultiplayerAPI.CreateBuildWithManagedContainerAsync(request);
        }

        public async Task<PlayFabResult<CreateBuildWithCustomContainerResponse>> CreateBuildWithCustomContainer(CreateBuildWithCustomContainerRequest request)
        {
            Console.WriteLine($"Starting deployment {request.BuildName} for titleId, regions  {string.Join(", ", request.RegionConfigurations.Select(x => x.Region))}");

            return await PlayFabMultiplayerAPI.CreateBuildWithCustomContainerAsync(request);
        }

        public async Task<PlayFabResult<GetAssetDownloadUrlResponse>> FileExists(string filename)
        {
            GetAssetDownloadUrlRequest downloadRequest = new() { FileName = filename };

            return await PlayFabMultiplayerAPI.GetAssetDownloadUrlAsync(downloadRequest);
        }

        public async Task CheckAssetFiles(string filename)
        {
            var filevalidator = FileExists(filename);

            if (filevalidator.Result.Result == null)
            {
                GetAssetUploadUrlRequest request1 = new() { FileName = filename };

                var uriResult = await PlayFabMultiplayerAPI.GetAssetUploadUrlAsync(request1);

                //check for error
                if (uriResult.Error != null)
                {
                    Console.WriteLine(uriResult.Error.ErrorMessage);
                }
                var uri = new System.Uri(uriResult.Result.AssetUploadUrl);

                var blockBlob = new CloudBlockBlob(uri);
                await blockBlob.UploadFromFileAsync(filename);
            }

            else if (filevalidator.Result.Error != null)
            {
                Console.WriteLine($"{filevalidator.Result.Error.ErrorMessage}");
            }
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

            if (res.Error == null)
            {
                Console.WriteLine(res.Error.ErrorMessage);
                return;
            }


            // do validation checks

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

            // if Os is windows and Container mode is True
            // Call method A to get CreateBuildWithCustomContainerRequest
            // else you call method B to get CreateBuildWithProcessBasedServerRequest
            dynamic request = null;
            if(settings.RunContainer)
            {
                if(settingsDeployment.OSPlatform == "Windows")
                {
                    //run get managedcontainer request
                    request = GetManagedContainerRequest(ports);
                }
                else
                {
                    //ask user
                }
            }
            else
            {
                //run get processbased request
                request = GetProcessBasedServerRequest(ports);
            }

            //check asset files
            Console.WriteLine($"");
            foreach (var file in settingsDeployment.AssetFileNames)
            {
                await CheckAssetFiles(file);
            }
                

            //create build
            dynamic createBuild = null;
            if (settings.RunContainer)
            {
                if (settingsDeployment.OSPlatform == "Windows")
                {
                    //run get managedcontainer request
                    createBuild = CreateBuildWithManagedContainer(request);
                }
                else
                {
                    //ask user
                }
            }
            else
            {
                //run get processbased request
                createBuild = await CreateBuildWithProcessBasedServer(request);
            }

            if (createBuild.Error != null)
            {
                foreach (var err in createBuild.Error.ErrorDetails)
                {
                    foreach (var mess in err.Value)
                    {
                        Console.WriteLine($"{mess}");
                    }

                }
            }

            //Console.WriteLine("done");
        }
    }
 
}

  
