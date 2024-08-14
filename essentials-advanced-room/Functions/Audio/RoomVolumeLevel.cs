using essentials_advanced_room;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Serilog.Events;
using System;
using WebSocketSharp;

namespace essentials_advanced_room.Functions.Audio
{
    public class RoomVolumeLevel : IHasCurrentVolumeControls, ILogClassDetails
    {
        public string ClassName { get { return String.Format("[RoomVolumeLevel-{0}]", CurrentDevice.Key); } }
        public LogEventLevel LogLevel { get; set; }
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
            Debug.LogMessage(LogEventLevel.Debug, "RoomVolumeLevel constructor [{0}] {1}", label, device == null ? "== null" : device.Key);
            LogLevel = LogEventLevel.Information;
            Label = label;
            CurrentDevice = device;
            DefaultControls = CurrentDevice as IBasicVolumeControls;
            Debug.LogMessage(LogLevel, "{0} CurrentDevice {1}", ClassName, CurrentDevice == null ? "== null" : CurrentDevice.Key);
            if(device == null)
                Debug.LogMessage(LogLevel, "{0} device {1}", ClassName, device == null ? "== null" : device.Key);
            else
            {
                Initialize();
                CustomActivate();
            }
        }

        void Initialize()
        {
            Debug.LogMessage(LogLevel, CurrentDevice, "{0} Initialize", ClassName);
            CurrentVolumeControls = DefaultControls;
        }

        public void CustomActivate()
        {
            Debug.LogMessage(LogLevel, CurrentDevice, "{0} CustomActivate", ClassName);
        }

        public void SetDefaultLevels()
        {
            Debug.LogMessage(LogEventLevel.Debug, CurrentDevice, "Restoring default levels");
            var vol_ = CurrentVolumeControls as IBasicVolumeWithFeedback;
            if (vol_ != null && hasDefaultVolume)
                vol_.SetVolume(DefaultVolume);
         }
    }
}
