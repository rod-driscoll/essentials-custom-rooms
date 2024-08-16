using Crestron.SimplSharpPro;
using essentials_advanced_room;
using essentials_advanced_room.Functions.Audio;
using essentials_advanced_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using Serilog.Events;
using System;
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
    public class BasicAudioDriver : PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "AudioDriver"; } }
        private LogEventLevel _logLevel;
        public LogEventLevel LogLevel { 
            get { return _logLevel; } 
            set {
                try
                {
                    _logLevel = value;
                    //Debug.LogMessage(2, "{0} Setting LogLevel {1}", ClassName, _logLevel);
                    //Debug.LogMessage(2, "{0} Setting LogLevel ChildDrivers {1}", ClassName, ChildDrivers == null ? " = null" : "exists");
                    foreach (var driver in ChildDrivers)
                    {
                        var driver_ = driver as ILogClassDetails;
                        Debug.LogMessage(LogEventLevel.Information, "{0} Setting driver standard LogLevel {1}, {2} {3}", ClassName, _logLevel, driver.GetType().Name, driver_ == null ? " = null" : "exists");
                        if (driver_ != null)
                            driver_.LogLevel = _logLevel;
                    }
                    Debug.LogMessage(LogEventLevel.Information, "{0} Setting LogLevel {1} done", ClassName, _logLevel);
                }
                catch (Exception e)
                {
                    Debug.LogMessage(LogLevel, "{0} Setting LogLevel ERROR: {1}", ClassName, e.Message);
                }
            } 
        }
        
        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public List<PanelDriverBase> ChildDrivers = new List<PanelDriverBase>();

        public BasicAudioDriver(BasicPanelMainInterfaceDriver parent)
            : base(parent.TriList)
        {
            LogLevel = LogEventLevel.Information;
            Debug.LogMessage(LogEventLevel.Verbose, "{0} loading ", ClassName);
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
            Debug.LogMessage(LogLevel, "{0} {1} loaded ", ClassName, VolumeKey.Volume.ToString());
            // mic level driver
            ChildDrivers.Add(new BasicAudioLevelDriver(parent,
                new BasicAudioDriverControls(VolumeKey.MicLevel.ToString(),
                    new BasicAudioDriverSigs {
                        MutePress         = TriList.BooleanOutput[UIBoolJoin.Volume1SpeechMutePressAndFB],
                        MuteFb            = TriList.BooleanInput [UIBoolJoin.Volume1SpeechMutePressAndFB],
                   })
            ));
            Debug.LogMessage(LogLevel, "{0} {1} loaded ", ClassName, VolumeKey.MicLevel.ToString());
            // Load faders on SRL
            ChildDrivers.Add(new AudioListDriver(parent));
            // Load presets in SmartObject DynamicList
            ChildDrivers.Add(new AudioPresetListDriver(parent));
            // toggle audio page
            TriList.SetSigFalseAction(UIBoolJoin.VolumeButtonPopupPress, () =>
                parent.PopupInterlock.ShowInterlockedWithToggle(UIBoolJoin.VolumeButtonPopupPress));

            //Register();
            Debug.LogMessage(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes,
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            Debug.LogMessage(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            foreach (var driver in ChildDrivers)
            {
                //Debug.LogMessage(LogLevel, "{0} Setup {1}", ClassName, driver.GetType().Name);
                var roomDriver_ = driver as IAdvancedRoomSetup;
                Debug.LogMessage(LogLevel, "{0} Setup {1}, driver {2}", ClassName, driver.GetType().Name, roomDriver_==null?"== null":"exists");
                roomDriver_?.Setup(room);
            }
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
    }
}
