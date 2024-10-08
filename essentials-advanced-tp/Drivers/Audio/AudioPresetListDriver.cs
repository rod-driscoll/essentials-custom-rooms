﻿using essentials_advanced_room;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using SmartObjectDynamicList = essentials_basic_tp.Drivers.SmartObjectDynamicList;

namespace essentials_advanced_tp.Drivers
{
    public class AudioPresetListDriver : PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "AudioPresetListDriver"; } }
        public uint LogLevel { get; set; }
        //private BasicPanelMainInterfaceDriver Parent;

        uint SmartObjectId = joins.UISmartObjectJoin.AudioPresetList;
        uint nameSigOffset = 10; // not sure what this is

        SmartObjectDynamicList sol { get; set; }

        public AudioPresetListDriver(BasicPanelMainInterfaceDriver parent)
            : base(parent.TriList)
        {
            LogLevel = 2;
            Debug.Console(0, "{0} loading", ClassName);
            //this.Parent = parent;
            var so = parent.TriList.SmartObjects[SmartObjectId];
            sol = new SmartObjectDynamicList(so, true, nameSigOffset); // sol.Count = 0
            Debug.Console(0, "{0} sol.MaxCount: {1}", ClassName, sol.MaxCount);
            //Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        public void Setup(IAdvancedRoom room)
        {
            Debug.Console(0, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            // need to get list of room presets
            var CurrentRoom_ = room as IHasAudioDevice; // implements this class
            //Debug.Console(LogLevel, "{0} Setup CurrentRoom_ {1}", ClassName, CurrentRoom_ == null ? " = null":"exists");
            uint i = 1;
            if (CurrentRoom_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup CurrentRoom.Audio.Presets {1}", ClassName, CurrentRoom_.Audio.Presets == null ? " = null" : "exists");
                foreach (var preset in CurrentRoom_.Audio.Presets)
                {
                    Debug.Console(LogLevel, "{0} Setup Presets {1}, {2}", ClassName, preset.Key, preset.Value.Name);
                    sol.SetItemVisible(i, true);
                    sol.SetItemMainText(i, preset.Value.Name);
                    var buttonActionAssigned = false;
                    Debug.Console(LogLevel, "{0} Setup preset.Value.Level {1}", ClassName, preset.Value.Level == null ? " = null" : "exists");
                    if (preset.Value.Level != null)
                    {
                        Debug.Console(LogLevel, "{0} Setup preset.Value.Level.CurrentDevice {1}", ClassName, preset.Value.Level.CurrentDevice == null ? " = null" : preset.Value.Level.CurrentDevice.Key);
                        var fbDev = preset.Value.Level.CurrentDevice as IBasicVolumeWithFeedback;
                        Debug.Console(LogLevel, "{0} Setup fbDev {1}", ClassName, fbDev == null ? " = null" : "exists");
                        if (fbDev != null)
                        {
                            sol.SetItemButtonAction(i, (o) => {
                                Debug.Console(LogLevel, "{0} RecallPreset (level) {1} SmartObject pressed: {2}", ClassName, i.ToString(), preset.Value.Level.CurrentDevice.Key);
                                fbDev.SetVolume(1);
                            });
                            buttonActionAssigned = true;

                            var ItemFeedback = sol.GetBoolFeedbackSig(i);
                            Debug.Console(LogLevel, "{0} Setup ItemFeedback {1}", ClassName, ItemFeedback == null ? " = null" : "exists");
                            if (ItemFeedback != null)
                            {
                                EventHandler<FeedbackEventArgs> Feedback_OutputChange = (object o, FeedbackEventArgs e) =>
                                {
                                    Debug.Console(LogLevel, "{0} VolumeLevelFeedback.OutputChange[{1}]: {2}", ClassName, i, e.IntValue);
                                    ItemFeedback.BoolValue = e.IntValue > 0;
                                };
                                fbDev.VolumeLevelFeedback.OutputChange -= Feedback_OutputChange;
                                fbDev.VolumeLevelFeedback.OutputChange += Feedback_OutputChange;
                                Debug.Console(LogLevel, "{0} Setup VolumeLevelFeedback", ClassName);
                            }
                        }
                    }
                    if (!buttonActionAssigned)
                        sol.SetItemButtonAction(i, (o) => {
                            Debug.Console(LogLevel, "{0} RecallPreset {1} SmartObject pressed: {2}", ClassName, i.ToString(), preset.Value.Level.CurrentDevice.Key);
                            preset.Value.RecallPreset(); // setting 
                        });
                    i++;
                }
            }
            for (; i < sol.MaxCount; i++)
            {
                Debug.Console(LogLevel, "{0} Setup SetItemInvisible: {1}", ClassName, i);
                sol.SetItemVisible(i, false);
            }
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }
    }
}

