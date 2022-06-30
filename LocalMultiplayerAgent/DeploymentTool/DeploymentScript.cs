using Microsoft.Azure.Gaming.LocalMultiplayerAgent.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.MultiplayerModels;
using System.IO;

namespace Microsoft.Azure.Gaming.LocalMultiplayerAgent.MPSDeploymentTool
{
    public class DeploymentScript
    {
        private readonly MultiplayerSettings settings;
        private readonly DeploymentSettings settingsDeployment;
        public DeploymentScript(MultiplayerSettings multiplayerSettings)
        {
            settings = multiplayerSettings;
            settingsDeployment = JsonConvert.DeserializeObject<DeploymentSettings>(File.ReadAllText("deployment.json"));
            Globals.deploymentSettings = settingsDeployment;
        }

        public async Task RunScriptAsync()
        {
            const string buildFriendlyName = "bbb";
            string buildName = $"{buildFriendlyName}_{Guid.NewGuid()}";

            IEnumerable<PlayFab.MultiplayerModels.Port> ports = null;

            foreach (var portList in settings.PortMappingsList)
            {
                ports = portList?.Select(x => new PlayFab.MultiplayerModels.Port()
                {
                    Name = x.GamePort.Name,
                    Num = x.GamePort.Number,
                    Protocol = (ProtocolType)Enum.Parse(typeof(ProtocolType), x.GamePort.Protocol)
                }).ToList();
            }

            // Clean up day old builds if for some reason they were not deleyted (test runs halfway and cancelled, test box was unhappy, etc.)
            //await Helpers.CleanupOldBuildsAsync(buildFriendlyName, _fixture);
            PlayFabApiSettings apiSettings = new PlayFabApiSettings();
            PlayFabSettings.staticSettings.TitleId = "59F84";
            PlayFabSettings.staticSettings.DeveloperSecretKey = "KGY76BMR3XJISEONHPU8US1FIUUZEUMYGUEZZMSEJ6E8QUQUBB";
            var req = new PlayFab.AuthenticationModels.GetEntityTokenRequest();
            var res = await PlayFabAuthenticationAPI.GetEntityTokenAsync(req);
            apiSettings.TitleId = "59F84";
            PlayFabAuthenticationContext context = new PlayFabAuthenticationContext(null,
                "NHxjcDhMdCtYaHJrTmxxUkJLalk2RDY4TXc1NGRZZ2NsV3cwTlpKcHozWXNNPXx7ImkiOiIyMDIyLTA2LTMwVDE5OjQ1OjA2LjM5MDcyNzJaIiwiaWRwIjoiVW5rbm93biIsImUiOiIyMDIyLTA3LTAxVDE5OjQ1OjA2LjM5MDcyNzJaIiwidGlkIjoiNzAxMmQ2YzJhMzcxNDdjYmFkMjFmYjJhMzkzZGZiMTAiLCJoIjoiRUQ1Njg0MDQyNDlEMjJGIiwiZWMiOiJ0aXRsZSE1RkVDOEU3N0I0RDYyM0YvNTlGODQvIiwiZWkiOiI1OUY4NCIsImV0IjoidGl0bGUifQ==", null, null, null);
            PlayFabMultiplayerInstanceAPI ss = new PlayFabMultiplayerInstanceAPI(apiSettings, context);

            CreateBuildWithProcessBasedServerRequest buildRequest = new()
            {
                VmSize = AzureVmSize.Standard_D1_v2,
                GameCertificateReferences = null,
                Ports = settings.PortMappingsList?.Select(x => new PlayFab.MultiplayerModels.Port()
                {
                    Name = "game_port"
                }).ToList(),
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
            };

            Console.WriteLine($"Starting deployment {buildName} for titleId, regions  {string.Join(", ", buildRequest.RegionConfigurations.Select(x => x.Region))}");

            var res2 = await PlayFabMultiplayerAPI.CreateBuildWithProcessBasedServerAsync(buildRequest);
            //CreateBuildWithProcessBasedServerResponse responseObj = await Helpers.GetActualResponseFromProcessRequestAsync(buildRequest, _fixture);
            Console.WriteLine("done");
            }
        }
    }

  
