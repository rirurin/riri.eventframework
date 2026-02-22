using System.ComponentModel;
using p3rpc.eventframework.Template.Configuration;
using RyoTune.Reloaded;

namespace p3rpc.eventframework.Configuration
{
    public class Config : Configurable<Config>
    {
        [Category("Debug")]
        [DisplayName("Log Level")]
        [DefaultValue(LogLevel.Information)]
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        
        [Category("Debug")]
        [DisplayName("Can Skip any Event")]
        [DefaultValue(false)]
        public bool CanSkipAnyEvent { get; set; } = false;
    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
