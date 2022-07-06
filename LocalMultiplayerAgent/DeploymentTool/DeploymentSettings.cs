// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Gaming.LocalMultiplayerAgent.DeploymentTool
{
    using System;
    using System.Collections.Generic;
    using AgentInterfaces;

    public class DeploymentSettings
    {
        public string BuildName { get; set; }

        public string VmSize { get; set; }

        public int MultiplayerServerCountPerVm { get; set; }

        public string OSPlatform { get; set; }

        public List<RegionConfiguration> RegionConfigurations { get; set; }
    }
}
