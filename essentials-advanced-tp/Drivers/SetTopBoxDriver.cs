using essentials_advanced_room;
using essentials_advanced_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.PageManagers;
using PepperDash.Essentials.Core.Presets;
using PepperDash.Essentials.Devices.Common;
using Serilog.Events;
using System;
using joins = essentials_advanced_tp.joins;

namespace essentials_basic_tp.Drivers
{
    public class SetTopBoxDriver : PanelDriverBase, IAdvancedRoomSetup
    {
        public string ClassName { get { return "SetTopBoxDriver"; } }
        public LogEventLevel LogLevel { get; set; }
        public uint PressJoin { get; private set; }
        public uint PageJoin { get; private set; }
        public ISetTopBoxControls CurrentDefaultDevice { get; private set; }

        SetTopBoxThreePanelPageManager pageManager;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public SetTopBoxDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            LogLevel = LogEventLevel.Information;
            Parent = parent;

            PressJoin = joins.UIBoolJoin.TvTunerButtonPress;
            PageJoin = joins.UIBoolJoin.TvTunerButtonPress;

            TriList.SetSigFalseAction(PressJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;
            Debug.LogMessage(LogLevel, "{0} constructor done", ClassName);
        }

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            Debug.LogMessage(LogLevel, "{0} PopupInterlock_StatusChanged, e.NewJoin: {1}, e.PreviousJoin: {2}", ClassName, e.NewJoin, e.PreviousJoin);
            if (e.PreviousJoin == PageJoin)
                Unregister();
            if (e.NewJoin == PageJoin)
                Register();
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            Debug.LogMessage(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            //EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            Unregister();
            var room_ = room as IHasSetTopBoxFunction;
            //Debug.LogMessage(LogLevel, "{0} Setup, IHasSetTopBoxFunction {1}", ClassName, room_ == null ? "== null" : room.Key);
            if (room_ != null)
            {
                //Debug.LogMessage(LogLevel, "{0} Setup, Driver {1}", ClassName, room_.SetTopBox == null ? "== null" : "exists");
                CurrentDefaultDevice = room_.SetTopBox.DefaultSetTopBox;
                //Register();
            }
            Debug.LogMessage(LogLevel, "{0} Setup, Driver.DefaultSetTopBox {1}", ClassName, CurrentDefaultDevice == null ? "== null" : CurrentDefaultDevice.Key);
            Debug.LogMessage(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        private void TvPresets_PresetsSaved(System.Collections.Generic.List<PepperDash.Essentials.Core.Presets.PresetChannel> presets)
        {
            Debug.LogMessage(LogLevel, "{0} TvPresets_PresetsSaved", ClassName);
        }
        private void TvPresets_PresetsLoaded(object sender, EventArgs e)
        {
            Debug.LogMessage(LogLevel, "{0} TvPresets_PresetsLoaded", ClassName);
        }
        private void TvPresets_PresetRecalled(ISetTopBoxNumericKeypad device, string channel)
        {
            PresetChannel preset_ = CurrentDefaultDevice.TvPresets.PresetsList.Find(x => x.Channel == channel);
            if (preset_ != null)
                Debug.LogMessage(LogLevel, "{0} TvPresets_PresetRecalled[{1}] '{2}', '{3}'", ClassName, preset_.Channel, preset_.Name, preset_.IconUrl);
            else
            {
                Debug.LogMessage(LogLevel, "{0} TvPresets_PresetRecalled: {1}", ClassName, channel);
                foreach(PresetChannel preset in CurrentDefaultDevice.TvPresets.PresetsList)
                    Debug.LogMessage(LogLevel, "{0} TvPreset[{1}] {2}, {3}", ClassName, preset.Channel, preset.Name, preset.IconUrl);
            }
            Debug.LogMessage(LogLevel, "{0} TvPresets_PresetRecalled enabled: {1}, UseLocalImageStorage: {2}, PresetsAreLoaded: {3}", 
                ClassName, CurrentDefaultDevice.TvPresets.Enabled, CurrentDefaultDevice.TvPresets.UseLocalImageStorage, CurrentDefaultDevice.TvPresets.PresetsAreLoaded);
        }

        public void Register()
        {
            Debug.LogMessage(LogLevel, "{0} Register", ClassName);
            try
            {
                if (TriList == null)
                    Debug.LogMessage(LogLevel, "{0} TriList == null", ClassName);

                    Debug.LogMessage(LogLevel, "{0} CurrentDefaultDevice {1}", ClassName, CurrentDefaultDevice == null?"== null":"exists");
                if (CurrentDefaultDevice != null)
                {
                    CurrentDefaultDevice.LinkButtons(TriList);
                    if (CurrentDefaultDevice is IChannel)
                        (CurrentDefaultDevice as IChannel).LinkButtons(TriList);
                    if (CurrentDefaultDevice is IColor)
                        (CurrentDefaultDevice as IColor).LinkButtons(TriList);
                    //Debug.LogMessage(LogLevel, "{0} Register IDPad {1}", ClassName, (CurrentDefaultDevice is IDPad) ? "exists" : "== null");
                    if (CurrentDefaultDevice is IDPad)
                        (CurrentDefaultDevice as IDPad).LinkButtons(TriList);
                    if (CurrentDefaultDevice is IDvr)
                        (CurrentDefaultDevice as IDvr).LinkButtons(TriList);
                    //Debug.LogMessage(LogLevel, "{0} Register INumericKeypad {1}", ClassName, (CurrentDefaultDevice is INumericKeypad) ? "exists":"== null");
                    if (CurrentDefaultDevice is INumericKeypad)
                        (CurrentDefaultDevice as INumericKeypad).LinkButtons(TriList);
                    if (CurrentDefaultDevice is ITransport)
                        (CurrentDefaultDevice as ITransport).LinkButtons(TriList);
                    if (CurrentDefaultDevice is IRSetTopBoxBase)
                    {
                        Debug.LogMessage(LogLevel, "{0} CurrentDefaultDevice is IRSetTopBoxBase", ClassName);
                        (CurrentDefaultDevice as IRSetTopBoxBase).KeypadAccessoryButton1Label = "OK";
                        (CurrentDefaultDevice as IRSetTopBoxBase).KeypadAccessoryButton1Command = "OK";
                        (CurrentDefaultDevice as IRSetTopBoxBase).KeypadAccessoryButton2Label = "Select";
                        (CurrentDefaultDevice as IRSetTopBoxBase).KeypadAccessoryButton2Command = "SELECT";
                    }
                    else
                        Debug.LogMessage(LogLevel, "{0} CurrentDefaultDevice is NOT IRSetTopBoxBase", ClassName);

                    pageManager = new SetTopBoxThreePanelPageManager(CurrentDefaultDevice, TriList);
                    pageManager.Show();
                    CurrentDefaultDevice.TvPresets.PresetRecalled += TvPresets_PresetRecalled;
                    CurrentDefaultDevice.TvPresets.PresetsLoaded += TvPresets_PresetsLoaded;
                    CurrentDefaultDevice.TvPresets.PresetsSaved += TvPresets_PresetsSaved;
                }
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogLevel, "{0} Register ERROR: {1}", ClassName, e.Message);
            }
        }
        public void Unregister()
        {
            Debug.LogMessage(LogLevel, "{0} Unregister", ClassName);
            try
            {        
                if(pageManager != null)
                    pageManager.Hide();
                if (CurrentDefaultDevice != null) // Disconnect current room 
                {
                    Debug.LogMessage(LogLevel, "{0} Unregister, TvPresets {1}", ClassName, CurrentDefaultDevice.TvPresets == null ? "== null" : "exist");
                    if (CurrentDefaultDevice.TvPresets != null)
                    {
                        CurrentDefaultDevice.TvPresets.PresetRecalled -= TvPresets_PresetRecalled;
                        CurrentDefaultDevice.TvPresets.PresetsLoaded -= TvPresets_PresetsLoaded;
                        CurrentDefaultDevice.TvPresets.PresetsSaved -= TvPresets_PresetsSaved;
                    }
                    Debug.LogMessage(LogLevel, "{0} Unregister, UnlinkButtons", ClassName);
                    CurrentDefaultDevice.UnlinkButtons(TriList);
                    Debug.LogMessage(LogLevel, "{0} Unregister, Unlink interfaces", ClassName);
                    if (CurrentDefaultDevice is IChannel)
                        (CurrentDefaultDevice as IChannel).UnlinkButtons(TriList);
                    if (CurrentDefaultDevice is IColor)
                        (CurrentDefaultDevice as IColor).UnlinkButtons(TriList);
                    if (CurrentDefaultDevice is IDPad)
                        (CurrentDefaultDevice as IDPad).UnlinkButtons(TriList);
                    if (CurrentDefaultDevice is IDvr)
                        (CurrentDefaultDevice as IDvr).UnlinkButtons(TriList);
                    if (CurrentDefaultDevice is INumericKeypad)
                        (CurrentDefaultDevice as INumericKeypad).UnlinkButtons(TriList);
                    if (CurrentDefaultDevice is ITransport)
                        (CurrentDefaultDevice as ITransport).UnlinkButtons(TriList);
                }
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogLevel, "{0} Unregister ERROR: {1}", ClassName, e.Message);
            }
        }
    }
}
