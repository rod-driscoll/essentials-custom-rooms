using Crestron.SimplSharpPro.DeviceSupport;
using essentials_basic_room.Functions;
using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;

namespace essentials_basic_tp.Drivers
{
    public class BasicAudioDriverControls
    {
        public BasicAudioDriverJoins Joins { get; private set; }
        public string Key { get; private set; }
        public BasicAudioDriverControls(string key, BasicAudioDriverJoins joins)
        {
            this.Joins = joins;
            this.Key = key;
        }
    }
    public class BasicAudioDriverJoins
    {
        public uint VolumeGaugeVisible { get; set; }
        public uint VolumeUpPress { get; set; }
        public uint VolumeDownPress { get; set; }
        public uint VolumeMutePressAndFb { get; set; }
        public uint VolumeButtonPopupPress { get; set; }
        public uint VolumeSlider1 { get; set; }
    }
    public class BasicAudioDriver: PanelDriverBase, IBasicRoomSetup//, IHasCurrentVolumeControls
    {
        public string ClassName { get { return String.Format("[AudioDriver-{0}]", controls.Key); } }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;

        private IHasCurrentVolumeControls CurrentVolumeDevice;
        /// <summary>
        /// Whether volume ramping from this panel will show the volume
        /// gauge popup.
        /// </summary>
        public bool ShowVolumeGauge { get; set; }

        /// <summary>
        /// The amount of time that the volume gauge stays on screen, in ms
        /// </summary>
        public uint VolumeGaugePopupTimeout
        {
            get { return VolumeGaugeFeedback.TimeoutMs; }
            set { VolumeGaugeFeedback.TimeoutMs = value; }
        }

        public bool ZeroVolumeWhenSwtichingVolumeDevices { get; }

        /// <summary>
        /// Controls the extended period that the volume gauge shows on-screen,
        /// as triggered by Volume up/down operations
        /// </summary>
        BoolFeedbackPulseExtender VolumeGaugeFeedback;

        /// <summary>
        /// Controls the period that the volume buttons show on non-hard-button
        /// interfaces
        /// </summary>
        //BoolFeedbackPulseExtender VolumeButtonsPopupFeedback;
        /// <summary>
        /// The amount of time that the volume buttons stays on screen, in ms
        /// </summary>
        //public uint VolumeButtonPopupTimeout
        //{
        //    get { return VolumeButtonsPopupFeedback.TimeoutMs; }
        //    set { VolumeButtonsPopupFeedback.TimeoutMs = value; }
        //}
        
        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;

        BasicAudioDriverControls controls;

        public BasicAudioDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config, BasicAudioDriverControls controls)
                    : base(parent.TriList)
        {
            this.controls = controls;
            Parent = parent;
            ShowVolumeGauge = true;
            // One-second pulse extender for volume gauge
            if(controls.Joins.VolumeGaugeVisible > 0)
            {
                VolumeGaugeFeedback = new BoolFeedbackPulseExtender(1500);
                VolumeGaugeFeedback.Feedback
                    .LinkInputSig(TriList.BooleanInput[controls.Joins.VolumeGaugeVisible]);
            }
            else
                Debug.Console(2, "{0} VolumeGaugeVisible join not deined", ClassName);


            //VolumeButtonsPopupFeedback = new BoolFeedbackPulseExtender(4000);
            //VolumeButtonsPopupFeedback.Feedback
            //    .LinkInputSig(TriList.BooleanInput[UIBoolJoin.VolumeButtonPopupVisible]);

            Register();
            Debug.Console(2, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes,
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(2, "{0} Setup", ClassName);

            if (CurrentVolumeDevice != null) // Disconnect current room 
            {
                CurrentVolumeDevice.CurrentVolumeDeviceChange -= this.CurrentRoom_CurrentAudioDeviceChange;
                ClearAudioDeviceConnections();
            }
            Debug.Console(2, "{0} ClearAudioDeviceConnections done", ClassName);

            var CurrentRoom_ = room as IHasAudioDevice; // implements this class
            if (CurrentRoom_ != null)
            {
                //CurrentRoom_.Audio.Levels.TryGetValue(controls.Key, out CurrentVolumeDevice);
                if (CurrentRoom_.Audio.Levels.ContainsKey(controls.Key))
                {
                    CurrentVolumeDevice = CurrentRoom_.Audio.Levels[controls.Key];
                    Debug.Console(2, "{0} CurrentRoom_.Audio.Levels.ContainsKey: {1}", ClassName, controls.Key);
                }
                else
                    foreach(var item_ in CurrentRoom_.Audio.Levels)
                        Debug.Console(2, "{0} CurrentRoom_.Audio.Levels[{1}] exists", ClassName, item_.Key);
                Debug.Console(2, "{0} RefreshCurrentRoom, CurrentVolumeDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : "exists");
                if (CurrentVolumeDevice != null) // Connect current room 
                {
                    CurrentVolumeDevice.CurrentVolumeDeviceChange += CurrentRoom_CurrentAudioDeviceChange;
                    RefreshAudioDeviceConnections();
                }
            }

            Debug.Console(2, "{0} RefreshAudioDeviceConnections done", ClassName);

            if (controls.Key == eVolumeKey.Volume.ToString())
            {
                if (TriList is TswFt5ButtonSystem)
                {
                    var tsw = TriList as TswFt5ButtonSystem;
                    // Wire up hard keys
                    Debug.Console(0, "{0} Wire up hard keys", ClassName);
                    tsw.Up.UserObject = new Action<bool>(VolumeUpPress);
                    tsw.Down.UserObject = new Action<bool>(VolumeDownPress);
                }
            }

            if (controls?.Joins?.VolumeButtonPopupPress > 0)
                TriList.SetSigFalseAction(controls.Joins.VolumeButtonPopupPress, VolumeButtonsTogglePress);
            Debug.Console(2, "{0} Setup done", ClassName);
        }

        public void Register()
        {
            Debug.Console(2, "{0} Register", ClassName);
        }
        public void Unregister()
        {
            Debug.Console(2, "{0} Unregister", ClassName);
        }

        private void TriggerVolumePopup(bool state)
        {
            // extend timeouts
            if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            //if(VolumeButtonsPopupFeedback != null)
            //    VolumeButtonsPopupFeedback.BoolValue = state;
        }
        void VolumeButtonsTogglePress()
        {
            Debug.Console(1, "{0} VolumeButtonsTogglePress", ClassName);
            //if (VolumeButtonsPopupFeedback != null)
            //{
            //    if (VolumeButtonsPopupFeedback.BoolValue)
            //        VolumeButtonsPopupFeedback.ClearNow();
            //    else // Trigger the popup
            //    {
            //        VolumeButtonsPopupFeedback.BoolValue = true;
            //        VolumeButtonsPopupFeedback.BoolValue = false;
            //    }
            //}
        }
        public void VolumeUpPress(bool state)
        {
            Debug.Console(1, "VolumeUpPress");
            TriggerVolumePopup(state);
            Debug.Console(2, "{0} VolumeUpPress CurrentVolumeDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : "exists");
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeUp(state);
        }
        public void VolumeDownPress(bool state)
        {
            Debug.Console(1, "VolumeDownPress");
            TriggerVolumePopup(state);
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeDown(state);
        }


        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        void RefreshAudioDeviceConnections()
        {
            Debug.Console(2, "{0} RefreshAudioDeviceConnections", ClassName);

            // Volume control
            var dev = CurrentVolumeDevice?.CurrentVolumeControls;
            Debug.Console(2, "{0} RefreshAudioDeviceConnections CurrentVolumeDevice {1}", ClassName, dev == null ? "== null" : "exists");
            if (dev != null) // connect buttons
            {
                if(controls?.Joins?.VolumeUpPress > 0)
                {
                    TriList.SetBoolSigAction(controls.Joins.VolumeUpPress, VolumeUpPress);
                    //Debug.Console(2, "{0} VolumeUpPress registered on join {1}", ClassName, controls.Joins.VolumeUpPress);
                }
                if (controls?.Joins?.VolumeDownPress > 0)
                    TriList.SetBoolSigAction(controls.Joins.VolumeDownPress, VolumeDownPress);
                if (controls?.Joins?.VolumeMutePressAndFb > 0)
                    TriList.SetSigFalseAction(controls.Joins.VolumeMutePressAndFb, dev.MuteToggle);
            }
            var fbDev = dev as IBasicVolumeWithFeedback;
            if (fbDev == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
            {
                if (controls?.Joins?.VolumeSlider1 > 0)
                    TriList.UShortInput[controls.Joins.VolumeSlider1].UShortValue = 0;
            }
            else
            {
                if (controls?.Joins?.VolumeSlider1 > 0)
                    TriList.SetUShortSigAction(controls.Joins.VolumeSlider1, fbDev.SetVolume); // slider
                // feedbacks
                if (controls?.Joins?.VolumeMutePressAndFb > 0)
                    fbDev.MuteFeedback.LinkInputSig(TriList.BooleanInput[controls.Joins.VolumeMutePressAndFb]);
                if (controls?.Joins?.VolumeSlider1 > 0)
                    fbDev.VolumeLevelFeedback.LinkInputSig(TriList.UShortInput[controls.Joins.VolumeSlider1]);
            }    
        }
        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        void ClearAudioDeviceConnections()
        {
            if (controls?.Joins?.VolumeUpPress > 0)
                TriList.ClearBoolSigAction(controls.Joins.VolumeUpPress);
            if (controls?.Joins?.VolumeDownPress > 0)
                TriList.ClearBoolSigAction(controls.Joins.VolumeDownPress);
            if (controls?.Joins?.VolumeMutePressAndFb > 0)
                TriList.ClearBoolSigAction(controls.Joins.VolumeMutePressAndFb);

            var fDev = CurrentVolumeDevice.CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (fDev != null)
            {
                if (controls?.Joins?.VolumeSlider1 > 0)
                {
                    TriList.ClearUShortSigAction(controls.Joins.VolumeSlider1);
                    fDev.VolumeLevelFeedback.UnlinkInputSig(TriList.UShortInput[controls.Joins.VolumeSlider1]);
                }
            }
        }
        /// <summary>
        /// Handler for when the room's volume control device changes
        /// </summary>
        void CurrentRoom_CurrentAudioDeviceChange(object sender, VolumeDeviceChangeEventArgs args)
        {
            if (args.Type == ChangeType.WillChange)
                ClearAudioDeviceConnections();
            else // did change
                RefreshAudioDeviceConnections();
        }

        public void SetDefaultLevels()
        {
            Debug.Console(2, "{0} SetDefaultLevels notimplemented", ClassName);
        }
    }
}