// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Gaming.LocalMultiplayerAgent
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Reflection;
    using System.Threading.Tasks;
    using AgentInterfaces;
    using AspNetCore.Hosting;
    using Config;
    using Newtonsoft.Json;
    using VmAgent.Core;
    using VmAgent.Core.Interfaces;
    using PlayFab;
    using Microsoft.Azure.Gaming.VmAgent.Core.Dependencies;
    //using PlayFabAllSDK;
    using System.Collections.Generic;
    using PlayFab.MultiplayerModels;
    using PlayFab.ClientModels;
    using System.Threading;

    //using PlayFab.API.Entity.Models;

    public class Program
    {
        private static bool _running = true;
        public static async Task Main(string[] args)
        {

            string[] salutations =
            {
                "Have a nice day!",
                "Thank you for using PlayFab Multiplayer Servers",
                "Check out our docs at aka.ms/playfabdocs!",
                "Have a question? Check our community at community.playfab.com"
            };
            Console.WriteLine(salutations[new Random().Next(salutations.Length)]);

            string debuggingUrl = "https://github.com/PlayFab/gsdkSamples/blob/master/Debugging.md";
            Console.WriteLine($"Check this page for debugging tips: {debuggingUrl}");

            // lcow stands for Linux Containers On Windows => https://docs.microsoft.com/en-us/virtualization/windowscontainers/deploy-containers/linux-containers
            Globals.GameServerEnvironment = args.Contains("-lcow") && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? GameServerEnvironment.Linux : GameServerEnvironment.Windows; // LocalMultiplayerAgent is running only on Windows for the time being
            //process or container
            MultiplayerSettings settings = JsonConvert.DeserializeObject<MultiplayerSettings>(File.ReadAllText("MultiplayerSettings.json"));
            DeploymentSettings settingsDeployment = JsonConvert.DeserializeObject<DeploymentSettings>(File.ReadAllText("deployment.json"));

            settings.SetDefaultsIfNotSpecified();
            //validate .json
            MultiplayerSettingsValidator validator = new MultiplayerSettingsValidator(settings);

            if (!validator.IsValid())
            {
                Console.WriteLine("The specified settings are invalid. Please correct them and re-run the agent.");
                Environment.Exit(1);
            }

            string vmId =
                $"xcloudwusu4uyz5daouzl:{settings.Region}:{Guid.NewGuid()}:tvmps_{Guid.NewGuid():N}{Guid.NewGuid():N}_d";

            Console.WriteLine($"TitleId: {settings.TitleId}");
            Console.WriteLine($"BuildId: {settings.BuildId}");
            Console.WriteLine($"VmId: {vmId}");

            Globals.Settings = settings;
            Globals.deploymentSettings = settingsDeployment;
            string rootOutputFolder = Path.Combine(settings.OutputFolder, "PlayFabVmAgentOutput", DateTime.Now.ToString("s").Replace(':', '-'));
            Console.WriteLine($"Root output folder: {rootOutputFolder}");

            VmDirectories vmDirectories = new VmDirectories(rootOutputFolder);

            Globals.VmConfiguration = new VmConfiguration(settings.AgentListeningPort, vmId, vmDirectories, false);
            if (Globals.GameServerEnvironment == GameServerEnvironment.Linux && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                VmPathHelper.AdaptFolderPathsForLinuxContainersOnWindows(Globals.VmConfiguration);  // Linux Containers on Windows requires special folder mapping
            }

            Directory.CreateDirectory(rootOutputFolder);
            Directory.CreateDirectory(vmDirectories.GameLogsRootFolderVm);
            Directory.CreateDirectory(Globals.VmConfiguration.VmDirectories.CertificateRootFolderVm);
            IWebHost host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://*:{settings.AgentListeningPort}")
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();

            await host.StartAsync();

            Console.WriteLine($"Local Multiplayer Agent is listening on port {settings.AgentListeningPort}");

            Globals.SessionConfig = settings.SessionConfig ?? new SessionConfig() { SessionId = Guid.NewGuid() };
            Console.WriteLine($"{string.Join(", ", Globals.SessionConfig.InitialPlayers)}");
            await new MultiplayerServerManager(SystemOperations.Default, Globals.VmConfiguration, Globals.MultiLogger, SessionHostRunnerFactory.Instance)
                .CreateAndStartContainerWaitForExit(settings.ToSessionHostsStartInfo());

            await host.StopAsync();

            SessionHostsStartInfo startparams = settings.ToSessionHostsStartInfo();

            //check if everything looks good

            ConsoleKey response = 0;
            do
            {
                Console.Write("Do you want to go ahead and create a build? [y/n] ");
                response = Console.ReadKey(false).Key;
                
                
                
                
                
                
                if (response != ConsoleKey.Enter)
                    Console.WriteLine();
            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            if (response == ConsoleKey.Y)
            {
                //Console.WriteLine(startparams.ToJsonString());
                Console.WriteLine(settings.ToJsonString());

                const string buildFriendlyName = "bbb";
                string buildName = $"{buildFriendlyName}_{Guid.NewGuid()}";

                IEnumerable< PlayFab.MultiplayerModels.Port > ports = null;

                foreach (var portList in settings.PortMappingsList)
                {
                    ports = portList?.Select(x => new PlayFab.MultiplayerModels.Port()
                    {
                        Name = x.GamePort.Name, 
                        Num = x.GamePort.Number,
                        Protocol = (ProtocolType)Enum.Parse(typeof(ProtocolType), x.GamePort.Protocol)
                    }).ToList();
                }

                // Clean up day old builds if for some reason they were not deleted (test runs halfway and cancelled, test box was unhappy, etc.)
                //await Helpers.CleanupOldBuildsAsync(buildFriendlyName, _fixture);
                //PlayFabApiSettings apiSettings = new PlayFabApiSettings();
                PlayFabSettings.staticSettings.TitleId = settings.TitleId;
                //apiSettings.TitleId = settings.TitleId;
                PlayFabAuthenticationContext context = new PlayFabAuthenticationContext(null, "NHxKc2JHWjNLWlFKYm9zd2xsVVRkeWlxY1N2V2tPRTA0WHJobVJTdStGNkJBPXx7ImkiOiIyMDIyLTA2LTI5VDE4OjMzOjMwLjg1NDQ2OTdaIiwiaWRwIjoiVW5rbm93biIsImUiOiIyMDIyLTA2LTMwVDE4OjMzOjMwLjg1NDQ2OTdaIiwidGlkIjoiYjkwMjg4N2MzYmI3NDJjNjliOTg2OGIxNjk0ZTg1NmEiLCJoIjoiRUQ1Njg0MDQyNDlEMjJGIiwiZWMiOiJ0aXRsZSE1RkVDOEU3N0I0RDYyM0YvNTlGODQvIiwiZWkiOiI1OUY4NCIsImV0IjoidGl0bGUifQ==", null, null, null);
                //PlayFabMultiplayerInstanceAPI ss = new PlayFabMultiplayerInstanceAPI(apiSettings, context);
                //var request = new LoginWithCustomIDRequest { CustomId = "GettingStartedGuide", CreateAccount = true }; 
                //var loginTask = PlayFabClientAPI.LoginWithCustomIDAsync(request);

                /*while (_running)
                {
                    if (loginTask.IsCompleted) // You would probably want a more sophisticated way of tracking pending async API calls in a real game
                    {
                        OnLoginComplete(loginTask);
                    }

                    // Presumably this would be your main game loop, doing other things
                    Thread.Sleep(1);
                }*/

                CreateBuildWithProcessBasedServerRequest buildRequest = new()
                {
                    AuthenticationContext = context,
                    VmSize = AzureVmSize.Standard_D2a_v4,
                    GameCertificateReferences = null,
                    Ports = (List<PlayFab.MultiplayerModels.Port>)ports,
                    Metadata = (Dictionary<string, string>)settings.DeploymentMetadata,
                    MultiplayerServerCountPerVm = settingsDeployment.MultiplayerServerCountPerVm,
                    RegionConfigurations = settingsDeployment.RegionConfigurations?.Select(x => new BuildRegionParams()
                    {
                        Region = "EastUS",
                        MaxServers = 10,
                        StandbyServers = 5
                    }).ToList(),
                    BuildName = settingsDeployment.BuildName,
                    GameAssetReferences = settings.AssetDetails?.Select(x => new AssetReferenceParams()
                    {
                        MountPath = x.MountPath,
                        FileName = x.LocalFilePath
                    }).ToList(),
                    StartMultiplayerServerCommand = @"wrapper.exe -g fakegame.exe arg1 arg2",
                    //buildRequest.GameWorkingDirectory = @"C:\Assets";
                    OsPlatform = settingsDeployment.OSPlatform
                };

                Console.WriteLine($"Starting deployment {buildName} for titleId, regions  {string.Join(", ", buildRequest.RegionConfigurations.Select(x => x.Region))}");

                var res = await PlayFabMultiplayerAPI.CreateBuildWithProcessBasedServerAsync(buildRequest);
                //CreateBuildWithProcessBasedServerResponse responseObj = await Helpers.GetActualResponseFromProcessRequestAsync(buildRequest, _fixture);
                //Console.WriteLine($"{res.Exception.Message}");
                Console.WriteLine("done");
            }
        }

        /*private static void OnLoginComplete(Task<PlayFabResult<LoginResult>> taskResult)
        {
            var apiError = taskResult.Result.Error;
            var apiResult = taskResult.Result.Result;

            if (apiError != null)
            {
                Console.ForegroundColor = ConsoleColor.Red; // Make the error more visible
                Console.WriteLine("Something went wrong with your first API call.  :(");
                Console.WriteLine("Here's some debug information:");
                Console.WriteLine(PlayFabUtil.GenerateErrorReport(apiError));
                Console.ForegroundColor = ConsoleColor.Gray; // Reset to normal
            }
            else if (apiResult != null)
            {
                Console.WriteLine("Congratulations, you made your first successful API call!");
            }

            _running = false; // Because this is just an example, successful login triggers the end of the program
        }*/
    }


}
