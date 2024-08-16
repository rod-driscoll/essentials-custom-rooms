using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using essentials_advanced_room;
using essentials_basic_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;
using DisplayBase = PepperDash.Essentials.Devices.Common.Displays.DisplayBase;
using Serilog.Events;
using System;
using System.Collections.Generic;

namespace essentials_advanced_tp.Drivers
{
    public class BasicPanelMainInterfaceDriver : PanelDriverBase, IDisposable
    {
        public string ClassName { get { return "MainInterfaceDriver"; } }
        public LogEventLevel LogLevel { get; set; }
        //public EssentialsPanelMainInterfaceDriver EssentialsDriver { get; private set; }

        Config Config;
        public PanelDriverBase CurrentChildDriver { get; private set; }
        public List<PanelDriverBase> ChildDrivers = new List<PanelDriverBase>();
        public JoinedSigInterlock PopupInterlock { get; private set; }
        public List<PanelDriverBase> PopupInterlockDrivers = new List<PanelDriverBase>();

        bool loadDisplay;
        bool loadScreen;
        bool loadLifter;
        bool loadAudio;
        bool loadRoomCombiner;
        bool loadSetTopBox;
        bool loadHelp;
        bool loadInfo;

        public BasicPanelMainInterfaceDriver(BasicTriListWithSmartObject trilist,
            Config config)
            : base(trilist)
        {
            LogLevel = LogEventLevel.Information;
            Debug.LogMessage(LogLevel, "{0} config {1}", ClassName, config == null ? "== null" : "exists");
            //Debug.LogMessage(LogLevel, "{0} trilist {1}", ClassName, trilist == null ? "== null" : "exists");
            this.Config = config;
            AddReservedSigs(trilist);
            PopupInterlock = new JoinedSigInterlock(TriList);
            ChildDrivers.Add(new NotificationRibbonDriver(this, config));
            ChildDrivers.Add(new PowerDriver(this, config));

            // load drivers only if associated devices or config exists
            if (!String.IsNullOrEmpty(config.Password))
                ChildDrivers.Add(new PinDriver(this, config));
            // because neither method is always accurate so picking the best for each
            /* I could use this at some point to get the config from a device
            * var config_ = ConfigReader.ConfigObject.Devices.Find(x => x.Key == device_.Key);
            * var config_ = ConfigReader.ConfigObject.Devices.Find(x => x.Group.StartsWith("audio"));
            */
            foreach (var dev in DeviceManager.GetDevices())
            {
                //if (dev.Group.Equals("dipslays") && !loadDisplay)
                if (dev is DisplayBase && !loadDisplay)
                {
                    Debug.LogMessage(LogLevel, "{0} Loading DisplayDriver", ClassName);
                    ChildDrivers.Add(new DisplayDriver(this, config));
                    loadDisplay = true;
                }
                else if (dev is IEssentialsRoomCombiner && !loadRoomCombiner)
                //else if (dev.Group.Equals("room-combiner") && !loadDisplay)
                {
                    Debug.LogMessage(LogLevel, "{0} Loading RoomCombineDriver", ClassName);
                    ChildDrivers.Add(new RoomCombineDriver(this, config));
                    loadRoomCombiner = true;
                }
                //else if (dev.Group.StartsWith("settopbox") && !loadSetTopBox)
                else if (dev is ISetTopBoxControls && !loadSetTopBox)
                {
                    Debug.LogMessage(LogLevel, "{0} Loading SetTopBoxDriver", ClassName);
                    PopupInterlockDrivers.Add(new SetTopBoxDriver(this, config));
                    loadSetTopBox = true;
                }
            }
            foreach (var dev in ConfigReader.ConfigObject.Devices)
            {
                //if (dev is ShadeBase && !loadScreen)
                if (dev.Group.StartsWith("screen") && !loadScreen)
                {
                    Debug.LogMessage(LogLevel, "{0} Loading ScreenDriver", ClassName);
                    ChildDrivers.Add(new ScreenDriver(this, config));
                    loadScreen = true;
                }
                //if (dev is ShadeBase && !loadLifter)
                if (dev.Group.StartsWith("lifter") && !loadLifter)
                {
                    Debug.LogMessage(LogLevel, "{0} Loading LifterDriver", ClassName);
                    ChildDrivers.Add(new LifterDriver(this, config));
                    loadLifter = true;
                }
                //else if (dev is IAdvancedRoomSetup && !loadAudio)
                else if (dev.Group.StartsWith("audio") && !loadAudio)
                {
                    Debug.LogMessage(LogLevel, "{0} Loading BasicAudioDriver", ClassName);
                    PopupInterlockDrivers.Add(new BasicAudioDriver(this));
                    loadAudio = true;
                }
                else if (dev.Group.Equals("room"))
                //else if (dev is EssentialsRoomBase)
                {
                    if (!loadHelp && (dev as IAdvancedRoom)?.PropertiesConfig?.Help?.Message != null)
                    {
                        Debug.LogMessage(LogLevel, "{0} Loading HelpButtonDriver", ClassName);
                        PopupInterlockDrivers.Add(new HelpButtonDriver(this, config)); // roomConf.Help.Message
                        loadHelp = true;
                    }
                    if (!loadInfo && (dev as IAdvancedRoom)?.PropertiesConfig?.Addresses != null)
                    {
                        Debug.LogMessage(LogLevel, "{0} Loading InfoButtonDriver", ClassName);
                        PopupInterlockDrivers.Add(new InfoButtonDriver(this, config));
                        loadInfo = true;
                    }
                }
            }

            // suppress excess logging on classes
            //Debug.LogMessage(2, "{0} suppressing excess logging on drivers, ChildDrivers {1}", ClassName, ChildDrivers == null ? "== null" : "exists");
            foreach (var driver in ChildDrivers)
            {
                var driver_ = driver as ILogClassDetails;
                if (driver_ != null)
                    //driver_.LogLevel = LogEventLevel.Debug;
                    driver_.LogLevel = driver_ is DisplayDriver ? LogEventLevel.Verbose : LogEventLevel.Debug;
            }
            //Debug.LogMessage(2, "{0} suppressing excess logging on drivers, PopupInterlockDrivers {1}", ClassName, PopupInterlockDrivers == null ? "== null" : "exists");
            foreach (var driver in PopupInterlockDrivers)
            {
                var driver_ = driver as ILogClassDetails;
                Debug.LogMessage(LogLevel, "{0} driver is {1}", ClassName, driver.GetType().Name);
                if (driver_ != null)
                    driver_.LogLevel = LogEventLevel.Debug;
            }
            Debug.LogMessage(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// called from DefaultSetTopBox when a room is assigned
        /// </summary>
        /// <param name="room"></param>
        public void SetupChildDrivers(IAdvancedRoom room)
        {
            try
            {
                Debug.LogMessage(LogLevel, "{0} SetupChildDrivers", ClassName);
                foreach (var driver in ChildDrivers)
                {
                    var roomDriver_ = driver as IAdvancedRoomSetup;
                    //Debug.LogMessage(LogLevel, "{0} Setup roomDriver_ {1}", ClassName, roomDriver_==null? "== null": roomDriver_.ClassName);
                    roomDriver_?.Setup(room);
                }
                Debug.LogMessage(LogLevel, "{0} PopupInterlockDrivers", ClassName);
                foreach (var driver in PopupInterlockDrivers)
                {
                    var roomDriver_ = driver as IAdvancedRoomSetup;
                    //Debug.LogMessage(LogLevel, "{0} Setup roomDriver_ {1}", ClassName, roomDriver_ == null ? "== null" : roomDriver_.ClassName);
                    roomDriver_?.Setup(room);
                }
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogLevel, "{0} SetupChildDrivers ERROR: {1}", ClassName, e.Message);
            }
            //Debug.LogMessage(LogLevel, "{0} SetupChildDrivers done", ClassName);
        }

        private void AddReservedSigs(BasicTriListWithSmartObject trilist)
        {
            //Debug.LogMessage(LogLevel, "{0} testing for tsx52or60: type: {1}", ClassName, trilist.GetType().Name);
            Tswx52ButtonVoiceControl tswx52ButtonVoiceControl = trilist as Tswx52ButtonVoiceControl;
            if (tswx52ButtonVoiceControl != null)
            {
                //Debug.LogMessage(LogLevel, "{0} is Tswx52ButtonVoiceControl. ExtenderTouchDetectionReservedSigs {1}= null", ClassName, tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs == null ? "=" : "!");
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.Use();
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.DeviceExtenderSigChange += ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange;
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.Time.UShortValue = 1;
            }
            else
            {
                //Debug.LogMessage(LogLevel, "{0} as TswX70Base", ClassName);
                TswX70Base tswX70Base = trilist as TswX70Base;
                if (tswX70Base != null)
                {
                    tswX70Base.ExtenderTouchDetectionReservedSigs.Use();
                    tswX70Base.ExtenderTouchDetectionReservedSigs.DeviceExtenderSigChange += ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange;
                    tswX70Base.ExtenderTouchDetectionReservedSigs.Time.UShortValue = 1;
                }
            }
        }
        void ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange(Crestron.SimplSharpPro.DeviceExtender currentDeviceExtender, Crestron.SimplSharpPro.SigEventArgs args)
        {
            if (args.Sig.BoolValue)
            {
                Debug.LogMessage(LogLevel, "{0} DeviceExtenderSigChange", ClassName);
            }
        }

        public override void Show()
        {
            CurrentChildDriver = null;
            TriList.SetSigFalseAction(UIBoolJoin.InterlockedModalClosePress, PopupInterlock.HideAndClear);
            base.Show();
        }
        public override void Hide()
        {
            PopupInterlock.HideAndClear();
            base.Hide();
        }
        void ShowSubDriver(PanelDriverBase driver)
        {
            CurrentChildDriver = driver;
            if (driver == null)
                return;
            this.Hide();
            driver.Show();
        }
        public override void BackButtonPressed()
        {
            if (CurrentChildDriver != null)
                CurrentChildDriver.BackButtonPressed();
        }

        public void Dispose()
        {
            foreach(var driver in ChildDrivers)
            {
                driver?.Hide();
                (driver as IDisposable)?.Dispose();
            }
            // dispose or hide all sub drivers
        }
    }
}
