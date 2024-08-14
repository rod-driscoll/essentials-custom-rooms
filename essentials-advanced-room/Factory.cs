using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Serilog.Events;

namespace essentials_advanced_room
{
    public class Factory : EssentialsPluginDeviceFactory<Device>
    {
        public Factory()
        {
            MinimumEssentialsFrameworkVersion = "1.6.4";
            TypeNames = new List<string>() { "advanced-room" };
        }
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.LogMessage(LogEventLevel.Debug, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);
            return new Device(dc);
        }
    }
}
