using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using essentials_basic_room_epi;
using essentials_basic_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;

namespace essentials_basic_tp_epi.Drivers
{
    public class BasicPanelMainInterfaceDriver : PanelDriverBase, IDisposable
    {
        public string ClassName { get { return "BasicPanelMainInterfaceDriver"; } }
        public uint LogLevel { get; set; }
        //public EssentialsPanelMainInterfaceDriver EssentialsDriver { get; private set; }

        Config Config;
        public PanelDriverBase CurrentChildDriver { get; private set; }
        public List<PanelDriverBase> ChildDrivers = new List<PanelDriverBase>();
        public JoinedSigInterlock PopupInterlock { get; private set; }
        public List<PanelDriverBase> PopupInterlockDrivers = new List<PanelDriverBase>();

        public BasicPanelMainInterfaceDriver(BasicTriListWithSmartObject trilist,
            Config config)
            : base(trilist)
        {
            LogLevel = 2;
            Debug.Console(0, "{0} config {1}", ClassName, config == null ? "== null" : "exists");
            //Debug.Console(0, "{0} trilist {1}", ClassName, trilist == null ? "== null" : "exists");
            this.Config = config;
            AddReservedSigs(trilist);
            PopupInterlock = new JoinedSigInterlock(TriList);
            ChildDrivers.Add(new NotificationRibbonDriver(this, config));
            ChildDrivers.Add(new PowerDriver(this, config));
            ChildDrivers.Add(new PinDriver(this, config));
            ChildDrivers.Add(new DisplayDriver(this, config));
            ChildDrivers.Add(new ScreenDriver(this, config));
            ChildDrivers.Add(new LifterDriver(this, config));


            PopupInterlockDrivers.Add(new BasicAudioDriver(this));
            PopupInterlockDrivers.Add(new HelpButtonDriver(this, config));
            PopupInterlockDrivers.Add(new InfoButtonDriver(this, config));

            // suppress excess logging on classes
            Debug.Console(0, "{0} suppressing excess logging on drivers, ChildDrivers {1}", ClassName, ChildDrivers == null ? "== null" : "exists");
            foreach (var driver in ChildDrivers)
            {
                var driver_ = driver as ILogClassDetails;
                if (driver_ != null)
                    driver_.LogLevel = (uint)(driver_ is ScreenDriver ? 1: 255); // 255 means they won't log
            }
            Debug.Console(0, "{0} suppressing excess logging on drivers, PopupInterlockDrivers {1}", ClassName, PopupInterlockDrivers == null ? "== null" : "exists");
            foreach (var driver in PopupInterlockDrivers)
            {
                var driver_ = driver as ILogClassDetails;
                Debug.Console(0, "{0} driver is {1}", ClassName, driver.GetType().Name);
                if (driver_ != null)
                    driver_.LogLevel = 255;
            }
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// called from Device when a room is assigned
        /// </summary>
        /// <param name="room"></param>
        public void SetupChildDrivers(IBasicRoom room)
        {
            foreach (var driver in ChildDrivers)
            {
                var roomDriver_ = driver as IBasicRoomSetup;
                roomDriver_?.Setup(room);
            }

            foreach (var driver in PopupInterlockDrivers)
            {
                var roomDriver_ = driver as IBasicRoomSetup;
                roomDriver_?.Setup(room);
            }
        }

        private void AddReservedSigs(BasicTriListWithSmartObject trilist)
        {
            //Debug.Console(LogLevel, "{0} testing for tsx52or60: type: {1}", ClassName, trilist.GetType().Name);
            Tswx52ButtonVoiceControl tswx52ButtonVoiceControl = trilist as Tswx52ButtonVoiceControl;
            if (tswx52ButtonVoiceControl != null)
            {
                //Debug.Console(LogLevel, "{0} is Tswx52ButtonVoiceControl. ExtenderTouchDetectionReservedSigs {1}= null", ClassName, tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs == null ? "=" : "!");
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.Use();
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.DeviceExtenderSigChange += ExtenderTouchDetectionReservedSigs_DeviceExtenderSigChange;
                tswx52ButtonVoiceControl.ExtenderTouchDetectionReservedSigs.Time.UShortValue = 1;
            }
            else
            {
                //Debug.Console(LogLevel, "{0} as TswX70Base", ClassName);
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
                Debug.Console(0, "{0} DeviceExtenderSigChange", ClassName);
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
