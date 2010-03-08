using System;
using System.Collections.Generic;

namespace StorEvil.Configuration
{
    public enum ConsoleMode
    {
        Color,
        NoColor
    }

    public class ConfigSettings
    {
        public ConfigSettings()
        {
            AssemblyLocations = new string[0];
        }

        public IEnumerable<string> ScenarioExtensions { get; set; }
        public IEnumerable<string> AssemblyLocations { get; set; }

        public string StoryBasePath { get; set; }

        public string OutputFile { get; set; }

        public string OutputFileFormat { get; set; }

        public bool Quiet { get; set; }

        public ConsoleMode ConsoleMode { get; set; }

        public string OutputFileTemplate { get; set; }

        public static ConfigSettings Default()
        {
            return new ConfigSettings
                       {
                           ScenarioExtensions = new string[0],
                           AssemblyLocations = new string[0]
                       };
        }
    }
}