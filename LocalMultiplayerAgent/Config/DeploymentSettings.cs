// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Gaming.LocalMultiplayerAgent.Config
{
    using System;
    using System.Collections.Generic;
    using AgentInterfaces;

    public class DeploymentSettings
    {
        public string BuildName { get; set; }

        public string VmSize { get; set; }

        public int ServersPerVm { get; set; }

        public string Platform { get; set; }

        public IEnumerable<RegionConfiguration> RegionConfigurations { get; set; }


        /*public ContainerImageDetails ImageDetails { get; set; }

        public string StartGameCommand { get; set; }*/
    }
}
