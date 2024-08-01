using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace essentials_advanced_room
{
    public class Factory : EssentialsPluginDeviceFactory<Device>
    {
        public Factory()
        {
            MinimumEssentialsFrameworkVersion = "1.6.4";
            TypeNames = new List<string>() { "basic-room" };
        }
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {
            Debug.Console(1, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);
            return new Device(dc);
        }
    }
}
