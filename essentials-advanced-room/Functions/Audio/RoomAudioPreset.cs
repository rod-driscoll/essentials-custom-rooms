using essentials_advanced_room;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace essentials_advanced_room.Functions.Audio
{
    public interface IAudioPreset : IDspPreset // IDspPreset is just 'Name'
    {
        void RunPresetNumber(uint presetNumber); // Essentials DSP presets uses this
        void RunPreset(string id);               // Essentials DSP presets uses this
        void RunPreset();                       //  Need to store the preset ID in the preset

        void SavePreset(uint presetNumber);
        string StopOrPresetButtonLabel { get; }
        event EventHandler PresetSaved;
    }
    //public interface IHasAudioPresets
    //{
    //    List<IAudioPreset> Presets { get; }
    //}
    public interface IHasCurrentAudioPresetControls
    {
        IAudioPreset CurrentControls { get; }
        event EventHandler<AudioPresetDeviceChangeEventArgs> CurrentDeviceChange;
    }
    public class AudioPresetDeviceChangeEventArgs : EventArgs
    {
        public IAudioPreset OldDev { get; private set; }
        public IAudioPreset NewDev { get; private set; }
        public ChangeType Type { get; private set; }
        public AudioPresetDeviceChangeEventArgs(IAudioPreset oldDev, IAudioPreset newDev, ChangeType type)
        {
            OldDev = oldDev;
            NewDev = newDev;
            Type = type;
        }
    }
    public class RoomAudioPreset : IHasCurrentAudioPresetControls, ILogClassDetails
    {
        public string ClassName { get { return "RoomAudioPreset"; } }
        public uint LogLevel { get; set; }
        /// <summary>
        /// Current audio device shouldn't change for a room in most scenarios, but code is here in case
        /// </summary>
        public event EventHandler<AudioPresetDeviceChangeEventArgs> CurrentDeviceChange;
        public event EventHandler PresetSaved;

        public IAudioPreset DefaultControls { get; private set; }
        public IAudioPreset CurrentControls
        {
            get { return _CurrentDevice; }
            set
            {
                if (value == _CurrentDevice) return;

                var oldDev = _CurrentDevice;
                // derigister this room from the device, if it can
                if (oldDev is IInUseTracking)
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "preset");
                var handler = CurrentDeviceChange;
                if (handler != null)
                    CurrentDeviceChange(this, new AudioPresetDeviceChangeEventArgs(oldDev, value, ChangeType.WillChange));
                _CurrentDevice = value;
                if (handler != null)
                    CurrentDeviceChange(this, new AudioPresetDeviceChangeEventArgs(oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentDevice is IInUseTracking)
                    (_CurrentDevice as IInUseTracking).InUseTracker.AddUser(this, "preset");
            }
        }
        IAudioPreset _CurrentDevice;
        public IKeyed CurrentDevice { get; private set; }

        public string Name { get; set; }
        public string Key { get; private set; }
        public string Function { get; private set; }
        public string StopOrPresetButtonLabel { get; private set; }
        private ushort presetNumber;

        public RoomVolumeLevel Level { get; private set; }

        public RoomAudioPreset(BasicAudioPresetConfig config)
        {
            LogLevel = 2;
            Name = config.Label;
            Key = config.DeviceKey;
            Function = config.Function; // e.g. "system-on", the room device looks for Function="system-on" to call on system power

            var match = Regex.Match(Key, @"([-_\w]+)--(.+)"); // Key = "deviceKey--presetName"
            // add device defined in config device "presets", this doesn't get feedback though
            if (match.Success)
            {
                var devKey = match.Groups[1].Value; // e.g. "dsp-1"
                var device = DeviceManager.GetDeviceForKey(devKey);
                if (device != null)
                {
                    CurrentDevice = device;
                    DefaultControls = CurrentDevice as IAudioPreset;
                    Debug.Console(LogLevel, "{0} CurrentDevice {1}", ClassName, CurrentDevice == null ? "== null" : CurrentDevice.Key);

                    var presetKey_ = match.Groups[2].Value; // e.g. "preset-1"
                    //Debug.Console(LogLevel, "{0} presetKey_: {1}", ClassName, presetKey_);
                    var m1_ = Regex.Match(presetKey_, @"(\d)"); // e.g. "1"
                    if (m1_.Success)
                    {
                        //Debug.Console(LogLevel, "{0} preset Index: {1}", ClassName, m1_.Groups[1].Value);
                        presetNumber = Convert.ToUInt16(m1_.Groups[1].Value);
                        if (presetNumber > 0) presetNumber--; // 0 indexed array
                        Debug.Console(LogLevel, "{0} presetNumber: {1}", ClassName, presetNumber);
                    }
                    Initialize();
                    CustomActivate();
                }
                // add device defined in config device "levelControlBlocks", this gets feedback, but must be defined in qsys as a NamedControl
                var levelDevKey = match.Groups[1].Value + "-" + match.Groups[2].Value; // e.g. "dsp-1-preset-1]"
                var levelDevice_ = (DeviceManager.GetDeviceForKey(levelDevKey));
                Debug.Console(LogLevel, "{0} levelDevice {1}", ClassName, levelDevice_ == null ? "== null" : levelDevice_.Key);
                if (levelDevice_ != null)
                    Level = new RoomVolumeLevel(levelDevice_, Name);
            }
        }

        void Initialize()
        {
            Debug.Console(LogLevel, CurrentDevice, "{0} Initialize", ClassName);
            CurrentControls = DefaultControls;
        }

        public void CustomActivate()
        {
            Debug.Console(LogLevel, CurrentDevice, "{0} CustomActivate", ClassName);
        }

        /// <summary>
        /// The audio DSP plugins don't use an interface for presets so we have to use reflection to use them.
        /// q-sys plugin has this method, we'll probabay find other models use different methods
        /// </summary>
        public void RecallPreset()
        {
            Debug.Console(LogLevel, CurrentDevice, "{0} RecallPreset: {1}", ClassName, presetNumber);
            //Debug.Console(LogLevel, CurrentDevice, "{0} RunPresetNumber CurrentDevice {1}", ClassName, CurrentDevice == null ? "== null" : "exists");
            var method = CurrentDevice.GetType().GetMethod("RunPresetNumber",
                                                    BindingFlags.Public | BindingFlags.Instance,
                                                    null,
                                                    CallingConventions.Any,
                                                    new Type[] { typeof(ushort) },
                                                    null);
            //Debug.Console(LogLevel, CurrentDevice, "{0} RunPresetNumber method {1}", ClassName, method == null ? "== null" : "exists");
            if (method != null)
            {
                Debug.Console(LogLevel, CurrentDevice, "{0} Invoke RunPresetNumber({1}): {2}", ClassName, presetNumber, Key);
                try
                {
                    method.Invoke(CurrentDevice, new object[] { presetNumber }); //setdevicestreamdebug dsp-1-tcp both                    
                }
                catch (Exception e)
                {
                    Debug.Console(LogLevel, CurrentDevice, "{0} RecallPreset({1}) ERROR: {2}", ClassName, presetNumber, e.Message);
                }
            }
        }
    }
}
