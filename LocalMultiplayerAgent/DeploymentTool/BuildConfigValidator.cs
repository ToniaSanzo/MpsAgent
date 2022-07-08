/*// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Gaming.LocalMultiplayerAgent.DeploymentTool
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using AgentInterfaces;
    using VmAgent.Core.Interfaces;
    using System.Linq;
    using System.IO;
    using PlayFab.MultiplayerModels;

    public class BuildConfigValidator
    {
        private readonly ISystemOperations _systemOperations;
        private readonly DeploymentSettings _settings;

        public BuildConfigValidator(DeploymentSettings settings, ISystemOperations systemOperations = null)
        {
            if (settings == null)
            {
                throw new ArgumentException("Settings cannot be null");
            }
            _settings = settings;

            _systemOperations = systemOperations ?? SystemOperations.Default;
        }

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(_settings.OutputFolder) || string.IsNullOrWhiteSpace(_settings.TitleId))
            {
                throw new Exception("OutputFolder or TitleId not found. Call SetDefaultsIfNotSpecified() before this method");
            }

            if (!_systemOperations.DirectoryExists(_settings.OutputFolder))
            {
                Console.WriteLine($"OutputFolder '{_settings.OutputFolder}' does not exist. Check your MultiplayerSettings.json file");
                return false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("Running LocalMultiplayerAgent is not yet supported on MacOS. We would be happy to accept PRs to make it work!");
                return false;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && _settings.RunContainer)
            {
                Console.WriteLine("Running LocalMultiplayerAgent as container mode is not yet supported on Linux. Please set RunContainer to false in MultiplayerSettings.json");
                return false;
            }

            if (Globals.GameServerEnvironment == GameServerEnvironment.Linux && !_settings.RunContainer)
            {
                Console.WriteLine("The specified settings are invalid. Using Linux Game Servers requires running in a container.");
                return false;
            }

            string startGameCommand;

            if (_settings.RunContainer)
            {
                if (_settings.ContainerStartParameters == null)
                {
                    Console.WriteLine("No ContainerStartParameters were specified (and RunContainer is true).");
                    return false;
                }
                startGameCommand = _settings.ContainerStartParameters.StartGameCommand;
            }
            else
            {
                if (_settings.ProcessStartParameters == null)
                {
                    Console.WriteLine("No ProcessStartParameters were specified (and RunContainer is false).");
                    return false;
                }
                startGameCommand = _settings.ProcessStartParameters.StartGameCommand;
            }

            bool isSuccess = true;

            // StartGameCommand is optional on Linux
            if (string.IsNullOrWhiteSpace(startGameCommand))
            {
                if (Globals.GameServerEnvironment == GameServerEnvironment.Windows)
                {
                    Console.WriteLine("StartGameCommand must be specified.");
                    isSuccess = false;
                }
            }
            else if (startGameCommand.Contains("<your_game_server_exe>"))
            {
                Console.WriteLine($"StartGameCommand '{startGameCommand}' is invalid");
                isSuccess = false;
            }
            else if (_settings.AssetDetails != null && _settings.RunContainer && (Globals.GameServerEnvironment == GameServerEnvironment.Windows))
            {
                if ((!_settings.AssetDetails.Any(x => startGameCommand.Contains(x.MountPath, StringComparison.InvariantCultureIgnoreCase))))
                {
                    Console.WriteLine($"StartGameCommand '{startGameCommand}' is invalid and does not contain the mount path. This should look like: C:\\Assets\\GameServer.exe for example.");
                    isSuccess = false;
                }
            }

            if (_settings.RegionConfigurations?.Count() > 0)
            {
                HashSet<string> names = new HashSet<string>();
                HashSet<string> paths = new HashSet<string>();

                foreach (BuildRegionParams regionParams in _settings.RegionConfigurations)
                {
                    if (string.IsNullOrEmpty(regionParams.Region))
                    {
                        Console.WriteLine($"Certificate cannot have an empty name");
                        isSuccess = false;
                        continue;
                    }
                    if (string.IsNullOrEmpty(regionParams.)
                    {
                        Console.WriteLine($"Certificate with filename path '{certDetails.Path}' is not a pfx file");
                        isSuccess = false;
                        continue;
                    }
                    
                }
            }

            if (string.IsNullOrWhiteSpace(_settings.VmSize))
            {
                Console.WriteLine("VM size must be specified.");
                isSuccess = false;
            }

            if (string.IsNullOrWhiteSpace(_settings.BuildName))
            {
                Console.WriteLine("Build name must be specified.");
                isSuccess = false;
            }

            

            return isSuccess;

        }

        private bool AreAssetsValid(AssetDetail[] assetDetails)
        {
            if (assetDetails?.Length > 0)
            {
                foreach (AssetDetail detail in assetDetails)
                {
                    if (string.IsNullOrEmpty(detail.LocalFilePath))
                    {
                        Console.WriteLine("Asset details must contain local file path for each asset.");
                        return false;
                    }

                    if (_settings.RunContainer && string.IsNullOrEmpty(detail.MountPath))
                    {
                        Console.WriteLine("Asset details must contain mount path when running as container.");
                        return false;
                    }

                    if (!_systemOperations.FileExists(detail.LocalFilePath))
                    {
                        Console.WriteLine($"Asset {detail.LocalFilePath} was not found. Please specify path to a local zip file.");
                        return false;
                    }
                }

                return true;
            }

            if (Globals.GameServerEnvironment == GameServerEnvironment.Linux)
            {
                return true; // Assets are optional in Linux, since we're packing the entire game onto a container image
            }

            Console.WriteLine("Assets must be specified for game servers running on Windows.");
            return false;

        }
    }
}
*/