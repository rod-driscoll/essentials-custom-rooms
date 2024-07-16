using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.PageManagers;
using PepperDash.Essentials.Core.Presets;
using PepperDash.Essentials.Devices.Common.VideoCodec.CiscoCodec;
using System;
using joins = essentials_basic_tp_epi.joins;

namespace essentials_basic_tp.Drivers
{
    public class SetTopBoxDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "SetTopBoxDriver"; } }
        public uint LogLevel { get; set; }
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
            LogLevel = 2;
            Parent = parent;

            PressJoin = joins.UIBoolJoin.TvTunerButtonPress;
            PageJoin = joins.UIBoolJoin.TvTunerButtonPress;

            TriList.SetSigFalseAction(PressJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.NewJoin == PageJoin)
                Register();
            else if (e.PreviousJoin == PageJoin)
                Unregister();
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            //EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            Unregister();
            var room_ = room as IHasSetTopBoxFunction;
            //Debug.Console(LogLevel, "{0} Setup, IHasSetTopBoxFunction {1}", ClassName, room_ == null ? "== null" : room.Key);
            if (room_ != null)
            {
                //Debug.Console(LogLevel, "{0} Setup, Driver {1}", ClassName, room_.SetTopBox == null ? "== null" : "exists");
                CurrentDefaultDevice = room_.SetTopBox.DefaultSetTopBox;
                //Register();
            }
            Debug.Console(LogLevel, "{0} Setup, Driver.DefaultSetTopBox {1}", ClassName, CurrentDefaultDevice == null ? "== null" : CurrentDefaultDevice.Key);
            Debug.Console(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        private void TvPresets_PresetsSaved(System.Collections.Generic.List<PepperDash.Essentials.Core.Presets.PresetChannel> presets)
        {
            Debug.Console(LogLevel, "{0} TvPresets_PresetsSaved", ClassName);
        }

        private void TvPresets_PresetsLoaded(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} TvPresets_PresetsLoaded", ClassName);
        }

        private void TvPresets_PresetRecalled(ISetTopBoxNumericKeypad device, string channel)
        {
            PresetChannel preset_ = CurrentDefaultDevice.TvPresets.PresetsList.Find(x => x.Channel == channel);
            if (preset_ != null)
                Debug.Console(LogLevel, "{0} TvPresets_PresetRecalled[{1}] '{2}', '{3}'", ClassName, preset_.Channel, preset_.Name, preset_.IconUrl);
            else
            {
                Debug.Console(LogLevel, "{0} TvPresets_PresetRecalled: {1}", ClassName, channel);
                foreach(PresetChannel preset in CurrentDefaultDevice.TvPresets.PresetsList)
                    Debug.Console(LogLevel, "{0} TvPreset[{1}] {2}, {3}", ClassName, preset.Channel, preset.Name, preset.IconUrl);
            }
            Debug.Console(LogLevel, "{0} TvPresets_PresetRecalled enabled: {1}, UseLocalImageStorage: {2}, PresetsAreLoaded: {3}", 
                ClassName, CurrentDefaultDevice.TvPresets.Enabled, CurrentDefaultDevice.TvPresets.UseLocalImageStorage, CurrentDefaultDevice.TvPresets.PresetsAreLoaded);
        }

        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
            CurrentDefaultDevice.LinkButtons(TriList);
            if (CurrentDefaultDevice is IChannel)
                (CurrentDefaultDevice as IChannel).LinkButtons(TriList);
            if (CurrentDefaultDevice is IColor)
                (CurrentDefaultDevice as IColor).LinkButtons(TriList);
            if (CurrentDefaultDevice is IDPad)
                (CurrentDefaultDevice as IDPad).LinkButtons(TriList);
            if (CurrentDefaultDevice is IDvr)
                (CurrentDefaultDevice as IDvr).LinkButtons(TriList);
            if (CurrentDefaultDevice is INumericKeypad)
                (CurrentDefaultDevice as INumericKeypad).LinkButtons(TriList);
            if (CurrentDefaultDevice is ITransport)
                (CurrentDefaultDevice as ITransport).LinkButtons(TriList);
            pageManager = new SetTopBoxThreePanelPageManager(CurrentDefaultDevice, TriList);
            pageManager.Show();
            CurrentDefaultDevice.TvPresets.PresetRecalled += TvPresets_PresetRecalled;
            CurrentDefaultDevice.TvPresets.PresetsLoaded += TvPresets_PresetsLoaded;
            CurrentDefaultDevice.TvPresets.PresetsSaved += TvPresets_PresetsSaved;
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);


            if(pageManager != null)
                pageManager.Hide();
            if (CurrentDefaultDevice != null) // Disconnect current room 
            {
                CurrentDefaultDevice.TvPresets.PresetRecalled -= TvPresets_PresetRecalled;
                CurrentDefaultDevice.TvPresets.PresetsLoaded -= TvPresets_PresetsLoaded;
                CurrentDefaultDevice.TvPresets.PresetsSaved -= TvPresets_PresetsSaved;

                CurrentDefaultDevice.UnlinkButtons(TriList);
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
    }
}
