// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Gaming.AgentInterfaces
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public class RegionConfiguration
    {
        public RegionConfiguration(string vmSize = null)
        {
            Location = null;
            MaxSessions = 0;
            StandingBySessions = 0;
            DynamicStandbySettings = new DynamicStandbySettings();
            ScheduledStandbySettings = new ScheduledStandbySettings();
            Status = string.Empty;
            CurrentActive = 0;
            CurrentStandby = 0;
            CurrentPropping = 0;
            VmSize = vmSize;
            IsVmOverriden = false;
        }

        public string Location { get; set; }

        [Range(0, int.MaxValue)]
        public int MaxSessions { get; set; }

        [Range(0, int.MaxValue)]
        public int StandingBySessions { get; set; }

        public bool IsVmOverriden { get; set; }

        public int? ServersPerVm { get; set; }

        public int? VmCoreCount { get; set; }

        public string VmSize { get; set; }
        public string VmFamily { get; set; }

        public DynamicStandbySettings DynamicStandbySettings { get; set; }

        public ScheduledStandbySettings ScheduledStandbySettings { get; set; }

        public string Status { get; set; }

        public int CurrentActive { get; set; }

        public int CurrentStandby { get; set; }

        public int CurrentPropping { get; set; }
    }

    public class DynamicStandbySettings
    {
        public bool IsEnabled { get; set; }
        public int? MinStandby { get; set; }
        public int? MinActiveAsMultipleOfStandby { get; set; }
        public int? RampDownSeconds { get; set; }
        public List<DynamicStandbyThreshold> DynamicFloorMultiplierThresholds { get; set; }
    }

    public class DynamicStandbyThreshold
    {
        public double TriggerThresholdPercentage { get; set; }
        public double Multiplier { get; set; }
    }

    public class ScheduledStandbySettings
    {
        public bool IsEnabled { get; set; }
        public List<StandbySchedule> Schedules { get; set; } = new List<StandbySchedule>();
    }

    public class StandbySchedule
    {
        public string Description { get; set; }
        public DateTime EndTime { get; set; }
        public bool? IsDisabled { get; set; }
        public bool? IsRecurringWeekly { get; set; }
        public DateTime StartTime { get; set; }
        public int TargetStandby { get; set; }
    }
}
