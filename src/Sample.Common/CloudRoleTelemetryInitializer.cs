using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Sample.Common
{
    internal class CloudRoleTelemetryInitializer : ITelemetryInitializer
    {
        private readonly string roleName;
        private readonly string roleInstance;
        private readonly string version;

        public CloudRoleTelemetryInitializer()
        {
            var name = Assembly.GetEntryAssembly().GetName();
            this.roleName = name.Name;
            this.roleInstance = Environment.MachineName;
            this.version = name.Version.ToString();
        }

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Cloud.RoleName = roleName;
            telemetry.Context.Cloud.RoleInstance = roleInstance;
            telemetry.Context.GlobalProperties["AppVersion"] = version;
        }
    }
}