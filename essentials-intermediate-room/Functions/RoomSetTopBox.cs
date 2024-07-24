using essentials_basic_room_epi;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Devices.Common;
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
            LogLevel = 0; // 0 == log everything, 2 == logs almost nothing
            Debug.Console(LogLevel, "{0} constructor starting", ClassName);
            this.config = config;
            IKeyed device_;
            try
            {
                if (!String.IsNullOrEmpty(config.DefaultSetTopBoxKey))
                {
                    Debug.Console(LogLevel, "{0} DefaultSetTopBoxKey: {1}", ClassName, config.DefaultSetTopBoxKey);
                    //Debug.Console(LogLevel, "{0} SetTopBoxPresetsURL: {1}", ClassName, config.SetTopBoxPresetsURL == null ? "== null" : config.SetTopBoxPresetsURL);
                    device_ = DeviceManager.GetDeviceForKey(config.DefaultSetTopBoxKey);
                    if (device_ != null)
                    {
                        Debug.Console(LogLevel, "{0} DefaultSetTopBox is {1}", ClassName, device_.GetType().Name);
                        DefaultSetTopBox = device_ as ISetTopBoxControls;
                        //if (DefaultSetTopBox != null)
                        //    Debug.Console(LogLevel, "{0} ISetTopBoxControls exists", ClassName);
                        IRSetTopBoxBase stb_ = device_ as IRSetTopBoxBase;
                        if (stb_ != null)
                        {
                            if (stb_.IrPort != null)
                                stb_.IrPort.DriverLoaded.OutputChange += DriverLoaded_OutputChange;
                            if (stb_.TvPresets != null && config.SetTopBoxPresetsURL != null)
                            {
                                stb_.TvPresets.ImagesLocalHostPrefix = config.SetTopBoxPresetsURL;
                                Debug.Console(LogLevel, "{0} ISetTopBoxControls, setting path: '{1}'", ClassName, config.SetTopBoxPresetsURL);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} RoomSetTopBox constructor ERROR: {1}", ClassName, e.Message);
            }
        }
                            
        private void DriverLoaded_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} DriverLoaded, DefaultSetTopBox {1}", ClassName, DefaultSetTopBox==null?"== null": DefaultSetTopBox.GetType().Name);
            if (DefaultSetTopBox != null)
            {
                IRSetTopBoxBase stb_ = DefaultSetTopBox as IRSetTopBoxBase;
                if (stb_ != null)
                {
                    if (stb_.IrPort != null)
                    {
                        IrOutputPortController irPort_ = stb_.IrPort;
                        //Debug.Console(LogLevel, "{0} IrPort {1}", ClassName, irPort_ == null ? "== null" : "exists");
                        if (irPort_ != null)
                        {
                            Debug.Console(LogLevel, "{0} DefaultSetTopBox IrPort: '{1}', path: '{2}', loaded: {3}", ClassName, irPort_.Name, irPort_.DriverFilepath, irPort_.DriverIsLoaded);
                            //irPort_.PrintAvailableCommands();
                            //foreach (var command in irPort_.IrFileCommands)
                            //    Debug.Console(LogLevel, "{0} command: {1}", ClassName, command);
                        }
                    }
                    Debug.Console(LogLevel, "{0} TvPresets: '{1}'", ClassName, stb_.TvPresets == null ? "== null" : stb_.TvPresets.Name);
                    Debug.Console(LogLevel, "{0} stb_.TvPresets.ImagesLocalHostPrefix: {1}", ClassName, stb_.TvPresets.ImagesLocalHostPrefix == null ? "== null" : stb_.TvPresets.ImagesLocalHostPrefix);
                    /*if (stb_.TvPresets != null && config.SetTopBoxPresetsURL != null)
                    {
                        stb_.TvPresets.ImagesLocalHostPrefix = config.SetTopBoxPresetsURL;
                        Debug.Console(LogLevel, "{0} ISetTopBoxControls, setting path: '{1}'", ClassName, config.SetTopBoxPresetsURL);
                    }*/
                }
            }
        }
    }
}
