using essentials_custom_rooms_epi;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System;

namespace essentials_basic_room.Functions
{
    public class RoomAudio: IHasCurrentVolumeControls
    {
        public string ClassName { get { return "RoomAudio"; } }

        /// <summary>
        /// Current audio device shouldn't change for a room in most scenarios, but code is here in case
        /// </summary>
        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;
        public IBasicVolumeControls DefaultAudioDevice { get; private set; }
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
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "audio");
                var handler = CurrentVolumeDeviceChange;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.WillChange));
                _CurrentAudioDevice = value;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentAudioDevice is IInUseTracking)
                    (_CurrentAudioDevice as IInUseTracking).InUseTracker.AddUser(this, "audio");
            }
        }
        IBasicVolumeControls _CurrentAudioDevice;
        public ushort DefaultVolume { get; set; }
        bool IHasCurrentVolumeControls.ZeroVolumeWhenSwtichingVolumeDevices { get; }

        public IKeyed CurrentDevice { get; private set; }
        public Config config { get; private set; }


        public RoomAudio(Config config)
        {
            Debug.Console(2, "{0} RoomAudio constructor", ClassName);
            this.config = config;
            CurrentDevice = DeviceManager.GetDeviceForKey(config.DefaultAudioKey);
            Debug.Console(2, "{0} CurrentDevice {1}", ClassName, CurrentDevice==null ? "== null": CurrentDevice.Key);
            DefaultAudioDevice = CurrentDevice as IBasicVolumeControls;
            Initialize();
            CustomActivate();
        }

        void Initialize()
        {
            Debug.Console(2, CurrentDevice, "{0} Initialize", ClassName);
            // Audio
            Debug.Console(2, CurrentDevice, "{0} DefaultAudioDevice == {1}", ClassName, DefaultAudioDevice == null ? "null" : DefaultAudioDevice.GetType().Name);
            if (DefaultAudioDevice is IBasicVolumeControls)
            {
                Debug.Console(2, CurrentDevice, "{0} DefaultAudioDevice is IBasicVolumeControls", ClassName);
                DefaultVolumeControls = DefaultAudioDevice as IBasicVolumeControls;
            }
            else
            {
                Debug.Console(2, CurrentDevice, "{0} DefaultAudioDevice is not IBasicVolumeWithFeedback", ClassName);
            }
            CurrentVolumeControls = DefaultVolumeControls;
        }

        public void CustomActivate()
        {
            Debug.Console(2, CurrentDevice, "{0} CustomActivate", ClassName);
            if (this.config.Volumes != null)
            {
                Debug.Console(2, CurrentDevice, "{0} CustomActivate loading PropertiesConfig.Volumes.Master.Level", ClassName);
                this.DefaultVolume = (ushort)((this.config?.Volumes?.Master?.Level ?? 0) * 65535 / 100);
            }
        }

        public void SetDefaultLevels()
        {
            Debug.Console(1, CurrentDevice, "Restoring default levels");
            var vol_ = CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (vol_ != null)
                vol_.SetVolume(DefaultVolume);
            else
                Debug.Console(1, CurrentDevice, "CurrentVolumeControls == null");
        }
    }
}
