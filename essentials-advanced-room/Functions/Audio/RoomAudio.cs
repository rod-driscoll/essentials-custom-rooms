using PepperDash.Core;
using PepperDash.Essentials.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace essentials_advanced_room.Functions.Audio
{
    public enum VolumeKey { Volume, MicLevel };

    public class RoomAudio: ILogClassDetails
    {
        public string ClassName { get { return "RoomAudio"; } }

        public Dictionary<string, RoomVolumeLevel> Levels; // Master volume and mic level for the room
        public Dictionary<string, RoomAudioPreset> Presets;

        public Config config { get; private set; }
        public uint LogLevel { get; set; }

        public RoomAudio(Config config)
        {
            LogLevel = 2;
            Debug.Console(LogLevel, "{0} constructor", ClassName);
            this.config = config;

            IKeyed device_;
            // master volume and mic faders
            Levels = new Dictionary<string, RoomVolumeLevel>();
            if (!String.IsNullOrEmpty(config.DefaultAudioKey))
            {
                Debug.Console(LogLevel, "{0} DefaultAudioKey: {1}", ClassName, config.DefaultAudioKey);
                device_ = DeviceManager.GetDeviceForKey(config.DefaultAudioKey);
                if (device_ != null) 
                    Levels.Add(VolumeKey.Volume.ToString(), new RoomVolumeLevel(device_, VolumeKey.Volume.ToString()));
            }
            if (!String.IsNullOrEmpty(config.DefaultMicKey))
            {
                Debug.Console(LogLevel, "{0} DefaultMicKey: {1}", ClassName, config.DefaultMicKey);
                device_ = DeviceManager.GetDeviceForKey(config.DefaultMicKey);
                if (device_ != null) 
                    Levels.Add(VolumeKey.MicLevel.ToString(), new RoomVolumeLevel(device_, VolumeKey.MicLevel.ToString()));
            }

            if (config.Faders != null) // list of faders for tech page
            {
                foreach (var item_ in config.Faders)
                {
                    Debug.Console(LogLevel, "{0} fader: {1}, name: {2}, key: {3}", ClassName, item_.Key, item_.Value.Label, item_.Value.DeviceKey);
                    device_ = DeviceManager.GetDeviceForKey(item_.Value.DeviceKey);
                    if (device_ != null)
                    {
                        var level_ = new RoomVolumeLevel(device_, item_.Value.Label);
                        //Debug.Console(LogLevel, "{0} fader: {1}, name: {2}{3}", ClassName, item_.Key, item_.Value.Label, level_ == null ? "== null" : "exists");
                        Levels.Add(item_.Key, level_);
                        //Debug.Console(LogLevel, "{0} Added", ClassName);
                        if (item_.Value.Level > 0)
                            level_.DefaultVolume = (ushort)item_.Value.Level;
                        //Debug.Console(LogLevel, "{0} DefaultVolume", ClassName);
                        //Debug.Console(LogLevel, "{0} DefaultVolume {1}", ClassName, level_.DefaultVolume);
                    }
                }
            }
            Debug.Console(LogLevel, "{0} faders loaded", ClassName);
            Debug.Console(LogLevel, "{0} Presets {1}", ClassName, config.AudioPresets == null ? "== null" : "exist");

            Presets = new Dictionary<string, RoomAudioPreset>();


            if (config.AudioPresets != null)
            {
                foreach (var item_ in config.AudioPresets)
                {
                    Debug.Console(LogLevel, "{0} preset: {1}, name: {2}, key: {3}, function: {4}", ClassName, item_.Key, item_.Value.Label, item_.Value.DeviceKey, item_.Value.Function);
                    // preset: system-on, name: System on , key: preset-1
                    var preset_ = new RoomAudioPreset(item_.Value);
                    //Debug.Console(LogLevel, "{0} preset {1} ", ClassName, preset_==null ? "== null":"exists");
                    Presets.Add(item_.Key, preset_);
                }
            }
            Debug.Console(LogLevel, "{0} presets loaded", ClassName);

            CustomActivate();
        }

        public void CustomActivate()
        {
            Debug.Console(LogLevel, "{0} CustomActivate", ClassName);
            if (this.config.Volumes != null)
            {
                Debug.Console(LogLevel, "{0} CustomActivate loading PropertiesConfig.Volumes.Master.Level", ClassName);
                var defaultVolume_ = (ushort)((this.config?.Volumes?.Master?.Level ?? 0) * 65535 / 100);
                foreach(var level in Levels)
                    level.Value.DefaultVolume = defaultVolume_;
            }
        }

        public void SetDefaultLevels()
        {
            Debug.Console(1, "{0} Restoring default levels", ClassName);
            Debug.Console(1, "{0} config.AudioPresets {1}", ClassName, Presets == null? "== null": Presets.Count.ToString());
            var hasDefaultPreset = PresetOnRecall();
            if (!hasDefaultPreset) // only set default levels if preset doesn't exist
            {
                // we could unmute levels if we wanted, best to do it in the presets though
                //if (!String.IsNullOrEmpty(config.DefaultAudioKey) && Levels.ContainsKey(config.DefaultAudioKey))
                //{
                //    var level_ = Levels[config.DefaultAudioKey].CurrentDevice as IBasicVolumeWithFeedback;
                //    Debug.Console(1, "{0} default level as IBasicVolumeWithFeedback {1}", ClassName, level_ == null ? "== null" : "exists");
                //    if (level_ != null)
                //        level_.MuteOff();
                //}
            } 
            foreach (var level in Levels)
                if(level.Value.DefaultVolume != 0)
                    level.Value.SetDefaultLevels();    
        }

        public bool PresetRecall(string name)
        { 
            Debug.Console(1, "{0} PresetRecall", ClassName);
            if (Presets?.Count > 0)
            {
                var kvp_ = Presets.First(x => x.Value.Function == name); //< string, RoomAudioPreset > where RoomAudioPreset.Function == "system-on"
                if (!String.IsNullOrEmpty(kvp_.Key))
                {
                    Debug.Console(1, "{0} kvp: {1}", ClassName, kvp_.Key);
                    kvp_.Value.RecallPreset();
                    return true;
                }
            }
            return false;
        }

        public bool PresetOffRecall()
        {
            return PresetRecall("system-off");
        }
        public bool PresetOnRecall()
        {
            return PresetRecall("system-on");
        }
    }
}
