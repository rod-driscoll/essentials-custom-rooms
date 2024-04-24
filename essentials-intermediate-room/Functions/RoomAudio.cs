using essentials_custom_rooms_epi;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace essentials_basic_room.Functions
{
    public enum eVolumeKey { Volume, MicLevel };

    public class RoomVolume: IHasCurrentVolumeControls
    {
        public string ClassName { get { return "RoomVolume"; } }
        /// <summary>
        /// Current audio device shouldn't change for a room in most scenarios, but code is here in case
        /// </summary>
        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;
        //public IBasicVolumeControls DefaultAudioDevice { get; private set; }
        public IBasicVolumeControls DefaultVolumeControls { get; private set; }
        public IBasicVolumeControls CurrentVolumeControls
        {
            get { return _CurrentAudioDevice; }
            set
            {
                if (value == _CurrentAudioDevice) return;

                var oldDev = _CurrentAudioDevice;
                // derigister this room from the device, if it can
                if (oldDev is IInUseTracking)
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "volume");
                var handler = CurrentVolumeDeviceChange;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.WillChange));
                _CurrentAudioDevice = value;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentAudioDevice is IInUseTracking)
                    (_CurrentAudioDevice as IInUseTracking).InUseTracker.AddUser(this, "volume");
            }
        }
        IBasicVolumeControls _CurrentAudioDevice;
        public IKeyed CurrentVolumeDevice { get; private set; }

        bool IHasCurrentVolumeControls.ZeroVolumeWhenSwtichingVolumeDevices { get; }

        public ushort DefaultVolume { get; set; }
        public string Label { get; set; }

        public RoomVolume(IKeyed device, string label)
        {
            Label = label;
            CurrentVolumeDevice = device;
            DefaultVolumeControls = CurrentVolumeDevice as IBasicVolumeControls;
            Debug.Console(2, "{0} CurrentVolumeDevice {1}", ClassName, CurrentVolumeDevice == null ? "== null" : CurrentVolumeDevice.Key);
            
            Initialize();
            CustomActivate();
        }

        void Initialize()
        {
            Debug.Console(2, CurrentVolumeDevice, "{0} Initialize", ClassName);
            CurrentVolumeControls = DefaultVolumeControls;
        }

        public void CustomActivate()
        {
            Debug.Console(2, CurrentVolumeDevice, "{0} CustomActivate", ClassName);
        }

        public void SetDefaultLevels()
        {
            Debug.Console(1, CurrentVolumeDevice, "Restoring default levels");
            var vol_ = CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (vol_ != null)
                vol_.SetVolume(DefaultVolume);
            else
                Debug.Console(1, CurrentVolumeDevice, "CurrentVolumeControls == null");
        }
    }

    public class RoomAudio
    {
        public string ClassName { get { return "RoomAudio"; } }

        public Dictionary<string, RoomVolume> Levels; // Master volume and mic level for the room
        //public Dictionary<string, RoomVolume> Faders; // custom levels for the room

        public Config config { get; private set; }

        public RoomAudio(Config config)
        {
            Debug.Console(2, "{0} RoomAudio constructor", ClassName);
            this.config = config;

            // master volume and mic faders
            Levels = new Dictionary<string, RoomVolume>();
            if (!String.IsNullOrEmpty(config.DefaultAudioKey))
                Levels.Add(eVolumeKey.Volume.ToString(), new RoomVolume(DeviceManager.GetDeviceForKey(config.DefaultAudioKey), eVolumeKey.Volume.ToString()));
            if (!String.IsNullOrEmpty(config.DefaultMicKey))
                Levels.Add(eVolumeKey.MicLevel.ToString(), new RoomVolume(DeviceManager.GetDeviceForKey(config.DefaultMicKey), eVolumeKey.MicLevel.ToString()));
            
            // list of faders for tech page
            foreach (var fader_ in config.Faders)
            {
                Debug.Console(2, "{0} fader: {1}, label: {2}", ClassName, fader_.Key, fader_.Value.Label);
                var level_ = new RoomVolume(DeviceManager.GetDeviceForKey(fader_.Value.DeviceKey), fader_.Value.Label);
                Levels.Add(fader_.Key, level_);
                level_.DefaultVolume = (ushort)fader_.Value.Level;
            }
            //Faders = new Dictionary<string, RoomVolume>();

            CustomActivate();
        }

        public void CustomActivate()
        {
            Debug.Console(2, "{0} CustomActivate", ClassName);
            if (this.config.Volumes != null)
            {
                Debug.Console(2, "{0} CustomActivate loading PropertiesConfig.Volumes.Master.Level", ClassName);
                var defaultVolume_ = (ushort)((this.config?.Volumes?.Master?.Level ?? 0) * 65535 / 100);
                foreach(var level in Levels)
                    level.Value.DefaultVolume = defaultVolume_;
            }
        }

        public void SetDefaultLevels()
        {
            Debug.Console(1, "Restoring default levels");
            foreach (var level in Levels)
                level.Value.SetDefaultLevels();
        }
    }
}
