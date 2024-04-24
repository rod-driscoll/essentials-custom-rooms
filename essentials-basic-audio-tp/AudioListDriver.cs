using Crestron.SimplSharpPro;
using Crestron.SimplSharpPro.DeviceSupport;
using essentials_basic_room.Functions;
using essentials_basic_room_epi;
using essentials_basic_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;
using SubpageReferenceList = essentials_basic_tp.Drivers.SubpageReferenceList;

namespace essentials_basic_tp_epi.Drivers
{
    public class SingleFaderDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return String.Format("[SingleFaderDriver-{0}]", controls.Key); } }

        private BasicPanelMainInterfaceDriver Parent;
        private IHasCurrentVolumeControls CurrentVolumeDevice;
        BoolFeedbackPulseExtender VolumeGaugeFeedback;
        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;
        BasicAudioDriverControls controls;

        public SingleFaderDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config,
            BasicAudioDriverControls controls)
            : base(parent.TriList)
        {
            this.controls = controls;
            Parent = parent;
            // One-second pulse extender for volume gauge
            if (controls.Sigs.GaugeVisible != null)
            {
                VolumeGaugeFeedback = new BoolFeedbackPulseExtender(1500);
                VolumeGaugeFeedback.Feedback
                    .LinkInputSig(controls.Sigs.GaugeVisible);
            }
            else
                Debug.Console(2, "{0} GaugeVisible join not deined", ClassName);
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
                if (CurrentRoom_.Audio.Levels.ContainsKey(controls.Key))
                {
                    CurrentVolumeDevice = CurrentRoom_.Audio.Levels[controls.Key];
                    Debug.Console(2, "{0} CurrentRoom_.Audio.Levels.ContainsKey: {1}", ClassName, controls.Key);
                }
                else
                    foreach (var item_ in CurrentRoom_.Audio.Levels)
                        Debug.Console(2, "{0} CurrentRoom_.Audio.Levels[{1}] exists", ClassName, item_.Key);
                Debug.Console(2, "{0} RefreshCurrentRoom, CurrentDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : "exists");
                if (CurrentVolumeDevice != null) // Connect current room 
                {
                    CurrentVolumeDevice.CurrentVolumeDeviceChange += CurrentRoom_CurrentAudioDeviceChange;
                    RefreshAudioDeviceConnections();
                }

            }

            Debug.Console(2, "{0} RefreshAudioDeviceConnections done", ClassName);

            if (controls.Key == VolumeKey.Volume.ToString())
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

        public void VolumeUpPress(bool state)
        {
            Debug.Console(1, "UpPress");
            Debug.Console(2, "{0} UpPress CurrentDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : "exists");
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeUp(state);
        }
        public void VolumeDownPress(bool state)
        {
            Debug.Console(1, "DownPress");
            if (CurrentVolumeDevice?.CurrentVolumeControls != null)
                CurrentVolumeDevice.CurrentVolumeControls.VolumeDown(state);
        }
        private void RefreshAudioDeviceConnections()
        {
            throw new NotImplementedException();
        }
        private void ClearAudioDeviceConnections()
        {
            throw new NotImplementedException();
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
    public class AudioListDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "AudioListDriver"; } }
        private BasicPanelMainInterfaceDriver Parent;

        SubpageReferenceList srl { get; set; }

        uint SmartObjectId = 1;
        uint dig_offset = 4;
        uint ana_offset = 1;
        uint ser_offset = 1;

        public List<SingleFaderDriver> Faders;
        public List<BasicAudioLevelDriver> ChildDrivers = new List<BasicAudioLevelDriver>();

        public BoolOutputSig VolumeButtonPopupPress { get; set; }
        public BoolInputSig VolumeButtonPopupFb { get; set; }

        public AudioListDriver(BasicPanelMainInterfaceDriver parent)
            : base(parent.TriList)
        {
            Debug.Console(0, "{0} loading", ClassName);
            this.Parent = parent;

            srl = new SubpageReferenceList(parent.TriList, SmartObjectId, dig_offset, ana_offset, ser_offset);

            Debug.Console(0, "{0} srl.Count: {1}", ClassName, srl.Count);
            
            for (uint i = 1;i <= srl.MaxDefinedItems; i++) {
                ChildDrivers.Add(new BasicAudioLevelDriver(Parent,
                    new BasicAudioDriverControls(i.ToString(),
                        new BasicAudioDriverSigs
                        {
                            UpPress   = srl.GetBoolFeedbackSig(i, (uint)1),
                            DownPress = srl.GetBoolFeedbackSig(i, (uint)2),
                            MutePress = srl.GetBoolFeedbackSig(i, (uint)3),
                            MuteFb    = srl.BoolInputSig(i, (uint)3),
                            Slider1   = srl.UShortInputSig(i, (ushort)1),
                            Slider1Fb = srl.GetUShortOutputSig(i, (ushort)1),
                            Label     = srl.StringInputSig(i, (ushort)1),
                        })
                ));
            }

            //Register();
            Debug.Console(2, "{0} constructor done", ClassName);
        }

        public void Setup(IBasicRoom room)
        {
            //srl.Clear();
            // need to get list of room levels
            bool[] isVisible = new bool[srl.MaxDefinedItems];
            var CurrentRoom_ = room as IHasAudioDevice; // implements this class
            if (CurrentRoom_ != null)
            {
                foreach (var driver in ChildDrivers)
                {
                    driver.Setup(room);
                    var key_ = driver.controls.Key; // number, not name
                    //Debug.Console(2, "{0} Setup key: {1} {2}", ClassName, key_, driver.CurrentDevice==null?"== null":"exists");
                    if (driver.CurrentVolumeDevice != null) 
                    {
                        var i = Convert.ToInt16(key_);
                        if (i > 0 && i <= srl.MaxDefinedItems)
                            isVisible[i - 1] = true;
                    }
                }
            }
            for (uint i = 0; i < isVisible.Length; i++)
            {
                //Debug.Console(2, "{0} SetInputVisible: {1}:{2}", ClassName, i, isVisible[i]);
                srl.SetInputVisible(i+1, isVisible[i]);
            }
         
            Debug.Console(2, "{0} Setup done", ClassName);
        }
    }
}
