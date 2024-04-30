using Crestron.SimplSharpPro;
using essentials_basic_room.Functions;
using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System.Collections.Generic;

namespace essentials_basic_tp.Drivers
{
    public class BasicAudioDriverControls
    {
        public BasicAudioDriverSigs Sigs { get; private set; }
        public string Key { get; private set; }
        public BasicAudioDriverControls(string key, BasicAudioDriverSigs sigs)
        {
            this.Sigs = sigs;
            this.Key = key;
        }
    }
    public class BasicAudioDriverSigs
    {
        public BoolInputSig GaugeVisible { get; set; }
        public BoolOutputSig UpPress { get; set; }
        public BoolOutputSig DownPress { get; set; }
        public BoolOutputSig MutePress { get; set; }
        public BoolInputSig MuteFb { get; set; }
        public BoolOutputSig ButtonPopupPress { get; set; }
        public UShortInputSig Slider1 { get; set; }
        public UShortOutputSig Slider1Fb { get; set; }
        public StringInputSig Label { get; set; }
    }
    public class BasicAudioDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "AudioDriver"; } }
        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public List<PanelDriverBase> ChildDrivers = new List<PanelDriverBase>();

        public BasicAudioDriver(BasicPanelMainInterfaceDriver parent)
            : base(parent.TriList)
        {
            Debug.Console(0, "{0} loading ", ClassName);
            Parent = parent;
            // main volume driver
            ChildDrivers.Add(new BasicAudioLevelDriver(parent,
                new BasicAudioDriverControls(VolumeKey.Volume.ToString(),
                    new BasicAudioDriverSigs
                    {
                        GaugeVisible      = TriList.BooleanInput [UIBoolJoin.VolumeGaugePopupVisible],
                        UpPress           = TriList.BooleanOutput[UIBoolJoin.VolumeUpPress],
                        DownPress         = TriList.BooleanOutput[UIBoolJoin.VolumeDownPress],
                        MutePress         = TriList.BooleanOutput[UIBoolJoin.Volume1ProgramMutePressAndFB],
                        MuteFb            = TriList.BooleanInput [UIBoolJoin.Volume1ProgramMutePressAndFB],
                        Slider1           = TriList.UShortInput  [UIUshortJoin.VolumeSlider1Value],
                        Slider1Fb         = TriList.UShortOutput [UIUshortJoin.VolumeSlider1Value],
                    })
            ));
            Debug.Console(0, "{0} {1} loaded ", ClassName, VolumeKey.Volume.ToString());
            // mic level driver
            ChildDrivers.Add(new BasicAudioLevelDriver(parent,
                new BasicAudioDriverControls(VolumeKey.MicLevel.ToString(),
                    new BasicAudioDriverSigs {
                        MutePress         = TriList.BooleanOutput[UIBoolJoin.Volume1SpeechMutePressAndFB],
                        MuteFb            = TriList.BooleanInput [UIBoolJoin.Volume1SpeechMutePressAndFB],
                   })
            ));
            Debug.Console(0, "{0} {1} loaded ", ClassName, VolumeKey.MicLevel.ToString());
            // Load faders on SRL
            ChildDrivers.Add(new AudioListDriver(parent));
            // Load presets in SmartObject DynamicList
            ChildDrivers.Add(new AudioPresetListDriver(parent));
            // toggle audio page
            TriList.SetSigFalseAction(UIBoolJoin.VolumeButtonPopupPress, () =>
                parent.PopupInterlock.ShowInterlockedWithToggle(UIBoolJoin.VolumeButtonPopupPress));

            //Register();
            Debug.Console(2, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes,
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(2, "{0} Setup, room {1}", ClassName, room == null ? "== null" : "exists");
            foreach (var driver in ChildDrivers)
            {
                //Debug.Console(2, "{0} Setup {1}", ClassName, driver.GetType().Name);
                var roomDriver_ = driver as IBasicRoomSetup;
                Debug.Console(2, "{0} Setup {1}, driver {2}", ClassName, driver.GetType().Name, roomDriver_==null?"== null":"exists");
                roomDriver_?.Setup(room);
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
    }
}
