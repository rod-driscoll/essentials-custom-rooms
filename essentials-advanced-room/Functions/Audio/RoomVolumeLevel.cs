using essentials_advanced_room;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using System;
using static PepperDash.Essentials.Devices.Common.VideoCodec.Cisco.CiscoCodecConfiguration;
using static PepperDash.Essentials.Devices.Common.VideoCodec.CiscoCodecBookings;

namespace essentials_advanced_room.Functions.Audio
{
    public class RoomVolumeLevel : IHasCurrentVolumeControls, ILogClassDetails
    {
        public string ClassName { get { return String.Format("[RoomVolumeLevel-{0}]", CurrentDevice.Key); } }

        public uint LogLevel { get; set; }
        /// <summary>
        /// Current audio device shouldn't change for a room in most scenarios, but code is here in case
        /// </summary>
        public event EventHandler<VolumeDeviceChangeEventArgs> CurrentVolumeDeviceChange;
        public IBasicVolumeControls DefaultControls { get; private set; }
        public IBasicVolumeControls CurrentVolumeControls
        {
            get { return _CurrentDevice; }
            set
            {
                if (value == _CurrentDevice) return;

                var oldDev = _CurrentDevice;
                // derigister this room from the device, if it can
                if (oldDev is IInUseTracking)
                    (oldDev as IInUseTracking).InUseTracker.RemoveUser(this, "volume");
                var handler = CurrentVolumeDeviceChange;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.WillChange));
                _CurrentDevice = value;
                if (handler != null)
                    CurrentVolumeDeviceChange(this, new VolumeDeviceChangeEventArgs(oldDev, value, ChangeType.DidChange));
                // register this room with new device, if it can
                if (_CurrentDevice is IInUseTracking)
                    (_CurrentDevice as IInUseTracking).InUseTracker.AddUser(this, "volume");
            }
        }
        IBasicVolumeControls _CurrentDevice;
        public IKeyed CurrentDevice { get; private set; }

        bool IHasCurrentVolumeControls.ZeroVolumeWhenSwtichingVolumeDevices { get; }

        private bool hasDefaultVolume;
        private ushort defaultVolume;
        public ushort DefaultVolume
        {
            get { return defaultVolume; }
            set 
            {
                defaultVolume = value;
                hasDefaultVolume = true;
            } 
        }
        public string Label { get; set; }

        public RoomVolumeLevel(IKeyed device, string label)
        {
            Debug.Console(2, "RoomVolumeLevel constructor [{0}] {1}", label, device == null ? "== null" : device.Key);
            LogLevel = 2;
            Label = label;
            CurrentDevice = device;
            DefaultControls = CurrentDevice as IBasicVolumeControls;
            Debug.Console(LogLevel, "{0} CurrentDevice {1}", ClassName, CurrentDevice == null ? "== null" : CurrentDevice.Key);
            if(device == null)
                Debug.Console(LogLevel, "{0} device {1}", ClassName, device == null ? "== null" : device.Key);
            else
            {
                Initialize();
                CustomActivate();
            }
        }

        void Initialize()
        {
            Debug.Console(LogLevel, CurrentDevice, "{0} Initialize", ClassName);
            CurrentVolumeControls = DefaultControls;
        }

        public void CustomActivate()
        {
            Debug.Console(LogLevel, CurrentDevice, "{0} CustomActivate", ClassName);
        }

        public void SetDefaultLevels()
        {
            Debug.Console(1, CurrentDevice, "Restoring default levels");
            var vol_ = CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (vol_ != null && hasDefaultVolume)
                vol_.SetVolume(DefaultVolume);
         }
    }
}
