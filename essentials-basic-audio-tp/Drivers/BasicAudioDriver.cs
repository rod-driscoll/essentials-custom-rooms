using Crestron.SimplSharpPro.DeviceSupport;
using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;

namespace essentials_basic_tp.Drivers
{
    public class BasicAudioDriver: PanelDriverBase, IBasicRoomSetup//, IHasCurrentVolumeControls
    {
        public string ClassName { get { return "AudioDriver"; } }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;

        //private IHasAudioDevice CurrentRoom;
        private IHasCurrentVolumeControls CurrentVolumeDevice;
        //private IBasicVolumeControls CurrentVolumeControls;
        /// <summary>
        /// Whether volume ramping from this panel will show the volume
        /// gauge popup.
        /// </summary>
        public bool ShowVolumeGauge { get; set; }
        /// <summary>
        /// The amount of time that the volume buttons stays on screen, in ms
        /// </summary>
        public uint VolumeButtonPopupTimeout
        {
            get { return VolumeButtonsPopupFeedback.TimeoutMs; }
            set { VolumeButtonsPopupFeedback.TimeoutMs = value; }
        }     
        /// <summary>
        /// The amount of time that the volume gauge stays on screen, in ms
        /// </summary>
        public uint VolumeGaugePopupTimeout
        {
            get { return VolumeGaugeFeedback.TimeoutMs; }
            set { VolumeGaugeFeedback.TimeoutMs = value; }
        }

        public bool ZeroVolumeWhenSwtichingVolumeDevices => throw new NotImplementedException();

        /// <summary>
        /// Controls the extended period that the volume gauge shows on-screen,
        /// as triggered by Volume up/down operations
        /// </summary>
        BoolFeedbackPulseExtender VolumeGaugeFeedback;
        /// <summary>
        /// Controls the period that the volume buttons show on non-hard-button
        /// interfaces
        /// </summary>
        BoolFeedbackPulseExtender VolumeButtonsPopupFeedback;

        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;

        public BasicAudioDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
                    : base(parent.TriList)
        {
            Parent = parent;

            ShowVolumeGauge = true;
            
            // One-second pulse extender for volume gauge
            VolumeGaugeFeedback = new BoolFeedbackPulseExtender(1500);
            VolumeGaugeFeedback.Feedback
                .LinkInputSig(TriList.BooleanInput[UIBoolJoin.VolumeGaugePopupVisible]);

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

            var CurrentRoom_ = room as IHasAudioDevice;

            if (CurrentRoom_ != null)
            {
                CurrentVolumeDevice = CurrentRoom_.Audio;  
                //CurrentVolumeControls = CurrentVolumeDevice.CurrentVolumeControls;
                Debug.Console(2, "{0} RefreshCurrentRoom, CurrentVolumeDevice {1}= null", ClassName, CurrentVolumeDevice == null ? "=" : "!");
                if (CurrentVolumeDevice != null) // Connect current room 
                {
                    CurrentVolumeDevice.CurrentVolumeDeviceChange += CurrentRoom_CurrentAudioDeviceChange;
                    RefreshAudioDeviceConnections();
                }
            }

            Debug.Console(2, "{0} RefreshAudioDeviceConnections done", ClassName);

            if (TriList is TswFt5ButtonSystem)
            {
                var tsw = TriList as TswFt5ButtonSystem;
                // Wire up hard keys
                Debug.Console(0, "{0} Wire up hard keys", ClassName);
                tsw.Up.UserObject = new Action<bool>(VolumeUpPress);
                tsw.Down.UserObject = new Action<bool>(VolumeDownPress);
            }
            TriList.SetSigFalseAction(UIBoolJoin.VolumeButtonPopupPress, VolumeButtonsTogglePress);
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

        void VolumeButtonsTogglePress()
        {
            Debug.Console(1, "{0} VolumeButtonsTogglePress", ClassName);
            if (VolumeButtonsPopupFeedback.BoolValue)
                VolumeButtonsPopupFeedback.ClearNow();
            else // Trigger the popup
            {
                VolumeButtonsPopupFeedback.BoolValue = true;
                VolumeButtonsPopupFeedback.BoolValue = false;
            }
        }
        public void VolumeUpPress(bool state)
        {
            Debug.Console(1, "VolumeUpPress");
            // extend timeouts
            if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            VolumeButtonsPopupFeedback.BoolValue = state;
            Debug.Console(2, "{0} VolumeUpPress CurrentVolumeDevice {1}= null", ClassName, CurrentVolumeDevice == null ? "=" : "!");
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeUp(state);
        }
        public void VolumeDownPress(bool state)
        {
            Debug.Console(1, "VolumeDownPress");
            // extend timeouts
            if (ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state;
            VolumeButtonsPopupFeedback.BoolValue = state;
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeDown(state);
        }


        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        void RefreshAudioDeviceConnections()
        {
            Debug.Console(2, "{0} RefreshAudioDeviceConnections", ClassName);

            var dev = CurrentVolumeDevice.CurrentVolumeControls;
            Debug.Console(2, "{0} RefreshAudioDeviceConnections CurrentVolumeDevice {1}= null", ClassName, dev == null ? "=" : "!");
            if (dev != null) // connect buttons
            {
                TriList.SetBoolSigAction(UIBoolJoin.VolumeUpPress, VolumeUpPress);
                TriList.SetBoolSigAction(UIBoolJoin.VolumeDownPress, VolumeDownPress);
                TriList.SetSigFalseAction(UIBoolJoin.Volume1ProgramMutePressAndFB, dev.MuteToggle);
            }

            var fbDev = dev as IBasicVolumeWithFeedback;
            if (fbDev == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
                TriList.UShortInput[UIUshortJoin.VolumeSlider1Value].UShortValue = 0;
            else
            {
                // slider
                TriList.SetUShortSigAction(UIUshortJoin.VolumeSlider1Value, fbDev.SetVolume);
                // feedbacks
                fbDev.MuteFeedback.LinkInputSig(TriList.BooleanInput[UIBoolJoin.Volume1ProgramMutePressAndFB]);
                fbDev.VolumeLevelFeedback.LinkInputSig(
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
            }
        }
        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        void ClearAudioDeviceConnections()
        {
            TriList.ClearBoolSigAction(UIBoolJoin.VolumeUpPress);
            TriList.ClearBoolSigAction(UIBoolJoin.VolumeDownPress);
            TriList.ClearBoolSigAction(UIBoolJoin.Volume1ProgramMutePressAndFB);

            var fDev = CurrentVolumeDevice.CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (fDev != null)
            {
                TriList.ClearUShortSigAction(UIUshortJoin.VolumeSlider1Value);
                fDev.VolumeLevelFeedback.UnlinkInputSig(
                    TriList.UShortInput[UIUshortJoin.VolumeSlider1Value]);
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