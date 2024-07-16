using essentials_basic_room_epi;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System;

namespace essentials_basic_room.Functions
{
    public interface IHasDefaultSetTopBox
    {
        ISetTopBoxControls DefaultSetTopBox { get; }
    }

    public class RoomSetTopBox: IHasDefaultSetTopBox, ILogClassDetails
    {
        public string ClassName { get { return "RoomSetTopBox"; } }
        public uint LogLevel { get; set; }
        public Config config { get; private set; }

        public ISetTopBoxControls DefaultSetTopBox { get; private set; }

        public RoomSetTopBox(Config config)
        {
            LogLevel = 2;
            Debug.Console(LogLevel, "{0} constructor", ClassName);
            this.config = config;
            IKeyed device_;
            if (!String.IsNullOrEmpty(config.DefaultSetTopBoxKey))
            {
                Debug.Console(LogLevel, "{0} DefaultSetTopBoxKey: {1}", ClassName, config.DefaultSetTopBoxKey);
                device_ = DeviceManager.GetDeviceForKey(config.DefaultSetTopBoxKey);
                if (device_ != null)
                {
                    Debug.Console(LogLevel, "{0} DefaultSetTopBox loaded: {1}", ClassName, device_.GetType().Name);
                    DefaultSetTopBox = device_ as ISetTopBoxControls;
                    if (DefaultSetTopBox != null)
                        Debug.Console(LogLevel, "{0} ISetTopBoxControls loaded", ClassName);
                }
            }
        }

    }
}
