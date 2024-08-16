﻿using Crestron.SimplSharpPro.DeviceSupport;
using essentials_advanced_room;
using essentials_advanced_room.Functions.Audio;
using essentials_advanced_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using Serilog.Events;
using System;

namespace essentials_basic_tp.Drivers
{
    public class BasicAudioLevelDriver: PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return String.Format("[AudioLevelDriver-{0}]", controls.Key); } }
        public LogEventLevel LogLevel { get; set; }

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;

        public IHasCurrentVolumeControls CurrentVolumeDevice { get; set; }
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

        //public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;
        public BasicAudioDriverControls controls { get; private set; }

        public BasicAudioLevelDriver(BasicPanelMainInterfaceDriver parent, BasicAudioDriverControls controls)
                    : base(parent.TriList)
        {
            LogLevel = LogEventLevel.Information;
            this.controls = controls;
            Parent = parent;
            ShowVolumeGauge = true;
            // One-second pulse extender for volume gauge
            if(controls.Sigs.GaugeVisible != null)
            {
                VolumeGaugeFeedback = new BoolFeedbackPulseExtender(3000);
                VolumeGaugeFeedback.Feedback
                    .LinkInputSig(controls.Sigs.GaugeVisible);
            }
            //Debug.LogMessage(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes,
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            //Debug.LogMessage(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);

            if (CurrentVolumeDevice != null) // Disconnect current room 
            {
                CurrentVolumeDevice.CurrentVolumeDeviceChange -= this.CurrentRoom_CurrentAudioDeviceChange;
                ClearAudioDeviceConnections();
            }
           // Debug.LogMessage(LogLevel, "{0} ClearAudioDeviceConnections done", ClassName);

            var CurrentRoom_ = room as IHasAudioDevice; // implements this class
            if (CurrentRoom_ != null)
            {
                //CurrentRoom_.Audio.Levels.TryGetValue(controls.Key, out CurrentDevice);
                if (CurrentRoom_.Audio.Levels.ContainsKey(controls.Key))
                {
                    CurrentVolumeDevice = CurrentRoom_.Audio.Levels[controls.Key];
                    Debug.LogMessage(LogLevel, "{0} CurrentRoom_.Audio.Levels.ContainsKey: {1}", ClassName, controls.Key);
                }
                else
                { 
                    //foreach(var item_ in CurrentRoom_.Audio.Levels)
                    //    Debug.LogMessage(LogLevel, "{0} CurrentRoom_.Audio.Levels[{1}] exists", ClassName, item_.Key);
                }
                //Debug.LogMessage(LogLevel, "{0} RefreshCurrentRoom, CurrentDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : "exists");
                if (CurrentVolumeDevice != null) // Connect current room 
                {
                    CurrentVolumeDevice.CurrentVolumeDeviceChange += CurrentRoom_CurrentAudioDeviceChange;
                    RefreshAudioDeviceConnections();
                }
            }

            //Debug.LogMessage(LogLevel, "{0} RefreshAudioDeviceConnections done", ClassName);

            if (controls.Key == VolumeKey.Volume.ToString())
            {
                if (TriList is TswFt5ButtonSystem)
                {
                    var tsw = TriList as TswFt5ButtonSystem;
                    // Wire up hard keys
                    Debug.LogMessage(LogLevel, "{0} Wire up hard keys", ClassName);
                    tsw.Up.UserObject = new Action<bool>(VolumeUpPress);
                    tsw.Down.UserObject = new Action<bool>(VolumeDownPress);
                }
            }

            if (controls.Sigs.ButtonPopupPress != null)
                controls.Sigs.ButtonPopupPress.SetSigFalseAction(() => TriggerVolumePopup(true));
            Debug.LogMessage(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void Register()
        {
            Debug.LogMessage(LogLevel, "{0} Register", ClassName);
        }
        public void Unregister()
        {
            Debug.LogMessage(LogLevel, "{0} Unregister", ClassName);
        }

        private void TriggerVolumePopup(bool state)
        {
            if (VolumeGaugeFeedback != null && ShowVolumeGauge)
                VolumeGaugeFeedback.BoolValue = state; // extend timeouts
        }
        public void VolumeUpPress(bool state)
        {
            Debug.LogMessage(LogEventLevel.Debug, "{0} UpPress({1})", ClassName, state);
            TriggerVolumePopup(state);
            Debug.LogMessage(LogLevel, "{0} UpPress CurrentDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : "exists");
            Debug.LogMessage(LogLevel, "{0} UpPress CurrentVolumeControls {1}", ClassName, CurrentVolumeDevice.CurrentVolumeControls == null ? "== null" : "exists");
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeUp(state);
        }
        public void VolumeDownPress(bool state)
        {
            Debug.LogMessage(LogEventLevel.Debug, "{0} DownPress({1})", ClassName, state);
            TriggerVolumePopup(state);
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeDown(state);
        }


        /// <summary>
        /// Attaches the buttons and feedback to the room's current audio device
        /// </summary>
        void RefreshAudioDeviceConnections()
        {
            Debug.LogMessage(LogLevel, "{0} RefreshAudioDeviceConnections", ClassName);

            // Volume control
            var dev = CurrentVolumeDevice?.CurrentVolumeControls;
            Debug.LogMessage(LogLevel, "{0} RefreshAudioDeviceConnections CurrentDevice {1}", ClassName, dev == null ? "== null" : "exists");
            if (dev != null) // connect buttons
            {
                if(controls.Sigs.UpPress != null)
                    controls.Sigs.UpPress.SetBoolSigAction(VolumeUpPress);
                //Debug.LogMessage(LogLevel, "{0} UpPress registered on join {1}", ClassName, controls.Joins.UpPress);
                if (controls.Sigs.DownPress != null)
                    controls.Sigs.DownPress.SetBoolSigAction(VolumeDownPress);
                if (controls.Sigs.MutePress != null)
                    controls.Sigs.MutePress.SetSigFalseAction(() => {
                        Debug.LogMessage(LogEventLevel.Debug, "{0} MutePress", ClassName);
                        dev.MuteToggle();
                        TriggerVolumePopup(true);
                        TriggerVolumePopup(false);
                    });
            }
            var fbDev = dev as IBasicVolumeWithFeedback;
            if (fbDev == null) // this should catch both IBasicVolume and IBasicVolumeWithFeeback
            {
                if (controls.Sigs.Slider1 != null)
                    controls.Sigs.Slider1.UShortValue = 0;
            }
            else
            {
                if (controls.Sigs.Slider1Fb != null)
                    controls.Sigs.Slider1Fb.SetUShortSigAction((a) => {
                        Debug.LogMessage(LogEventLevel.Debug, "{0} SetVolume({1})", ClassName, a);
                        fbDev.SetVolume(a);
                        TriggerVolumePopup(true);
                        TriggerVolumePopup(false);
                    }); // slider
                // feedbacks
                if (controls.Sigs.MuteFb != null)
                    fbDev.MuteFeedback.LinkInputSig(controls.Sigs.MuteFb);
                if (controls.Sigs.Slider1 != null)
                    fbDev.VolumeLevelFeedback.LinkInputSig(controls.Sigs.Slider1);
            }
            if (controls.Sigs.Label != null)
            {
                var roomVol_ = CurrentVolumeDevice as RoomVolumeLevel;
                Debug.LogMessage(LogLevel, "{0} RefreshAudioDeviceConnections roomVol_ {1}", ClassName, roomVol_ == null ? "== null" : roomVol_.Label);
                if(roomVol_ != null)
                    controls.Sigs.Label.StringValue = roomVol_.Label;

                //foreach(var item_ in CurrentRoom_.Audio.Levels)
                //    Debug.LogMessage(LogLevel, "{0} CurrentRoom_.Audio.Levels[{1}] exists", ClassName, item_.Key);

           }
        }

        /// <summary>
        /// Detaches the buttons and feedback from the room's current audio device
        /// </summary>
        void ClearAudioDeviceConnections()
        {
            if (controls.Sigs.UpPress != null)
                controls.Sigs.UpPress.ClearSigAction();
            if (controls.Sigs.DownPress != null)
                controls.Sigs.DownPress.ClearSigAction();
            if (controls.Sigs.MutePress != null)
                controls.Sigs.MutePress.ClearSigAction();

            var fDev = CurrentVolumeDevice.CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (fDev != null)
            {
                if (controls.Sigs.Slider1Fb != null)
                    controls.Sigs.Slider1Fb.ClearSigAction();
                if (controls.Sigs.Slider1 != null)
                    fDev.VolumeLevelFeedback.UnlinkInputSig(controls.Sigs.Slider1);
            }
            if (controls.Sigs.Label != null)
                controls.Sigs.Label.StringValue = "";
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
            Debug.LogMessage(LogLevel, "{0} SetDefaultLevels notimplemented", ClassName);
        }
    }
}