using System;
using System.Reflection;

namespace Sample.Common
{
    public static class ApplicationInformation
    {
        static ApplicationInformation()
        {
            var assemblyName = Assembly.GetEntryAssembly().GetName();
            Name = assemblyName.Name.ToLowerInvariant();
            Version = assemblyName.Version;
        }

        public static string Name { get; }
        public static Version Version { get; }
    }
}
