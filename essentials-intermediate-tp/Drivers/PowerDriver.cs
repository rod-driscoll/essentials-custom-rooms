﻿using essentials_basic_room.Functions;
using essentials_basic_room;
using essentials_advanced_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using System;
using System.Linq;
using joins = essentials_advanced_tp.joins;

namespace essentials_basic_tp.Drivers
{
    public class PowerDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "PowerDriver"; } }
        public uint LogLevel { get; set; }
        public uint PressJoin { get; private set; }
        public uint PageJoin { get; private set; }

        public uint PowerToggleJoin { get; private set; }

        IBasicRoom CurrentRoom;

        public uint PowerOffTimeout { get; set; }
        /// <summary>
        /// Will auto-timeout a power off
        /// </summary>
        //CTimer PowerOffTimer; // could use a SecondsCountdownTimer if i had known it existed before i implemented this
        ModalDialog PowerDownModal;

        NotificationRibbonDriver ribbonDriver;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public PowerDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {            
            LogLevel = 2;
            Parent = parent;
            // may need to change this from GenericModalVisible if we use the GenericModal dialog for anything else such as room combine
            PressJoin = UIBoolJoin.GenericModalVisible;
            PageJoin = UIBoolJoin.GenericModalVisible;
            PowerToggleJoin = joins.UIBoolJoin.PowerTogglePress;

            TriList.SetSigFalseAction(PressJoin, () =>
                Parent.PopupInterlock.ShowInterlockedWithToggle(PageJoin));

            Parent.PopupInterlock.StatusChanged += PopupInterlock_StatusChanged;

            PowerOffTimeout = 30000;

            var ribbon = Parent.ChildDrivers.First(x => x is NotificationRibbonDriver);
            if(ribbon != null)
                ribbonDriver = ribbon as NotificationRibbonDriver;

            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="room"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, disconnect, room {1}", ClassName, CurrentRoom == null ? "== null" : CurrentRoom.Key);

            if (CurrentRoom != null)// Disconnect current room
            {
                CurrentRoom.ShutdownPromptTimer.HasStarted -= ShutdownPromptTimer_HasStarted;
                CurrentRoom.ShutdownPromptTimer.HasFinished -= ShutdownPromptTimer_HasFinished;
                CurrentRoom.ShutdownPromptTimer.WasCancelled -= ShutdownPromptTimer_WasCancelled;

                CurrentRoom.OnFeedback.OutputChange -= CurrentRoom_OnFeedback_OutputChange;
                CurrentRoom.IsWarmingUpFeedback.OutputChange -= CurrentRoom_IsWarmingFeedback_OutputChange;
                CurrentRoom.IsCoolingDownFeedback.OutputChange -= IsCoolingDownFeedback_OutputChange;
 
                var roomPower_ = CurrentRoom as IHasPowerFunction;
                if (roomPower_ != null)
                    roomPower_.Power.PowerChange -= Power_PowerChange;
            }

            CurrentRoom = room;

            Debug.Console(LogLevel, "{0} Setup, connect, room {1}", ClassName, CurrentRoom == null ? "== null" : CurrentRoom.Key);

            if (CurrentRoom != null)// Connect current room
            {
                 // power toggle button
                TriList.SetSigFalseAction(PowerToggleJoin, PowerButtonPressed);
                CurrentRoom.OnFeedback.OutputChange += (o, a) =>
                {
                    Debug.Console(LogLevel, "{0}  CurrentRoom.OnFeedback.OutputChange: {1}", ClassName, a.BoolValue);
                    TriList.SetBool(PowerToggleJoin, a.BoolValue);
                };
            
                // Default to showing rooms/sources now.
                if (CurrentRoom.OnFeedback.BoolValue)
                    TriList.SetBool(UIBoolJoin.TapToBeginVisible, false);
                else
                    //TriList.SetBool(StartPageVisibleJoin, true);
                    TriList.SetBool(UIBoolJoin.TapToBeginVisible, true);
                TriList.SetSigFalseAction(UIBoolJoin.ShowPowerOffPress, PowerOffPress);

                Debug.Console(LogLevel, "{0} Setup, subscribing to ShutdownPromptTimer", ClassName);
                // Shutdown timer
                CurrentRoom.ShutdownPromptTimer.HasStarted += ShutdownPromptTimer_HasStarted;
                CurrentRoom.ShutdownPromptTimer.HasFinished += ShutdownPromptTimer_HasFinished;
                CurrentRoom.ShutdownPromptTimer.WasCancelled += ShutdownPromptTimer_WasCancelled;

                Debug.Console(LogLevel, "{0} Setup, subscribing to OnFeedback", ClassName);
                // Link up all the change events from the room
                CurrentRoom.OnFeedback.OutputChange += CurrentRoom_OnFeedback_OutputChange;
                CurrentRoom_SyncOnFeedback();
                CurrentRoom.IsWarmingUpFeedback.OutputChange += CurrentRoom_IsWarmingFeedback_OutputChange;
                CurrentRoom.IsCoolingDownFeedback.OutputChange += IsCoolingDownFeedback_OutputChange;

                Debug.Console(LogLevel, "{0} Setup, subscribing to Power.PowerChange", ClassName);
                var roomPower_ = CurrentRoom as IHasPowerFunction;
                Debug.Console(LogLevel, "{0} CurrentRoom as IHasPowerFunction {1}", ClassName, roomPower_ == null ? " == null" : "exists");
                if (roomPower_ != null)
                {
                    Debug.Console(LogLevel, "{0} Registering Power_PowerChange", ClassName);
                    roomPower_.Power.PowerChange += Power_PowerChange;
                }
            }

            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        /// <summary>
        /// Helper for property setter. Sets the panel to the given room, latching up all functionality
        /// </summary>
        public void RefreshCurrentRoom(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} RefreshCurrentRoom", ClassName);
            Setup(room);
        }


        //void SetCurrentRoom(IBasicRoom room)
        //{
        //    Debug.Console(LogLevel, "{0} SetCurrentRoom", ClassName);
        //    if (CurrentRoom == room) return;
        //    // Disconnect current (probably never called)

        //    if (CurrentRoom != null)
        //        CurrentRoom.ConfigChanged -= room_ConfigChanged;

        //    room.ConfigChanged -= room_ConfigChanged;
        //    room.ConfigChanged += room_ConfigChanged;

        //    RefreshCurrentRoom(room);
        //}

        /// <summary>
        /// Fires when room config of current room has changed.  Meant to refresh room values to propegate any updates to UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void room_ConfigChanged(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} room_ConfigChanged", ClassName);
            RefreshCurrentRoom(CurrentRoom);
        }

        public override void Hide()
        {
            Debug.Console(LogLevel, "{0} Hide", ClassName);
            //TriList.BooleanInput[StartPageVisibleJoin].BoolValue = false;
            TriList.BooleanInput[UIBoolJoin.TapToBeginVisible].BoolValue = false;
            //TriList.BooleanInput[UIBoolJoin.StagingPageVisible].BoolValue = false;
            //CancelPowerOff();
            base.Hide();
        }

        void CurrentRoom_SyncOnFeedback()
        {
            Debug.Console(LogLevel, "{0} CurrentRoom_SyncOnFeedback", ClassName);
            var value = CurrentRoom.OnFeedback.BoolValue;
            //Debug.Console(LogLevel, CurrentRoom, "UI: Is on event={0}", value);
            TriList.BooleanInput[UIBoolJoin.RoomIsOn].BoolValue = value;
        }
        /// <summary>
        /// For room on/off changes
        /// </summary>
        void CurrentRoom_OnFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} CurrentRoom_OnFeedback_OutputChange", ClassName);
            CurrentRoom_SyncOnFeedback();
        }
        void CurrentRoom_IsWarmingFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} IsWarmingFeedback: {1}-{2}", ClassName, CurrentRoom.IsWarmingUpFeedback.BoolValue, CurrentRoom.IsWarmingUpFeedback.IntValue);
            if (CurrentRoom.IsWarmingUpFeedback.BoolValue)
                ribbonDriver?.ShowNotificationRibbon("Room is powering on. Please wait...", 0);
            else
                ribbonDriver?.ShowNotificationRibbon("Room is powered on. Welcome.", 2000);
        }
        void IsCoolingDownFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} IsCoolingFeedback: {1}-{2}", ClassName, CurrentRoom.IsCoolingDownFeedback.BoolValue, CurrentRoom.IsCoolingDownFeedback.IntValue);
            if (CurrentRoom.IsCoolingDownFeedback.BoolValue)
                ribbonDriver?.ShowNotificationRibbon("Room is powering off. Please wait.", 0);
            else
                ribbonDriver?.HideNotificationRibbon();
        }

        public void PowerOffPress()
        {
            Debug.Console(LogLevel, "{0} PowerOffPress", ClassName);
            if (!CurrentRoom.OnFeedback.BoolValue
                || CurrentRoom.ShutdownPromptTimer.IsRunningFeedback.BoolValue)
            {
                Debug.Console(LogLevel, "{0} PowerOffPress cancelled, system is off or powering", ClassName);
                return;
            }
            CurrentRoom.StartShutdown(eShutdownType.Manual); // CurrentRoom.StartShutdown is in EssentialsRoomBase, which starts ShutdownPromptTimer
        }
        public void PowerOnPress()
        {
            Debug.Console(LogLevel, "{0} PowerOnPress", ClassName);
            //CurrentRoom.PowerOnToDefaultOrLastSource();
            //CurrentRoom.RunDefaultPresentRoute();
            CurrentRoom.StartUp();
        }
        public void PowerButtonPressed()
        {
            Debug.Console(LogLevel, "{0} PowerButtonPressed, OnFeedback: {1}", ClassName, CurrentRoom.OnFeedback.BoolValue);

            if (CurrentRoom.OnFeedback.BoolValue)
                PowerOffPress();
            else
                PowerOnPress();
        }

        //void CancelPowerOffTimer()
        //      {
        //          Debug.Console(LogLevel, "{0} CancelPowerOffTimer", ClassName);
        //          Dispose();
        //      }

        /// <summary>
        /// Triggered by CurrentRoom.Power
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Power_PowerChange(object sender, PowerEventArgs args)
        {
            Debug.Console(LogLevel, "{0} Power_PowerChange, current: {1}, {2} seconds remaining", ClassName, args.Current.ToString(), args.SecondsRemaining.ToString());
            try
            {
                if (ribbonDriver != null)
                {
                    Debug.Console(LogLevel, "{0} Power_PowerChange ribbonDriver != null", ClassName);
                    if (args.SecondsRemaining == 0)
                        ribbonDriver.HideNotificationRibbon();
                    else
                    {
                        string msg_ = String.Format("System is {0}, {1} seconds remaining", args.Current.ToString(), args.SecondsRemaining.ToString());
                        Debug.Console(LogLevel, "{0} Power_PowerChange msg_: {1}", ClassName, msg_);
                        ribbonDriver.ShowNotificationRibbon(msg_, (int)(args.SecondsRemaining * 1000));
                    }
                }
                Debug.Console(LogLevel, "{0} Power_PowerChange ribbonDriver done", ClassName);
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} Power_PowerChange ERROR: {1}", ClassName, e.Message);
            }
            Debug.Console(LogLevel, "{0} Power_PowerChange done", ClassName);
        }

        #region ShutdownPromptTimer

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_HasStarted(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} ShutdownPromptTimer_HasStarted: {1}", ClassName, CurrentRoom.ShutdownType);
            // Do we need to check where the UI is? No?
            var timer = CurrentRoom.ShutdownPromptTimer;
            //EndMeetingButtonSig.BoolValue = true;
            //ShareButtonSig.BoolValue = false;

            if (CurrentRoom.ShutdownType == eShutdownType.Manual || CurrentRoom.ShutdownType == eShutdownType.Vacancy)
            {
                PowerDownModal = new ModalDialog(TriList);
                var message = string.Format("Room will power down in {0} seconds", CurrentRoom.ShutdownPromptSeconds);

                // Attach timer things to modal
                CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange += ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
                CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange += ShutdownPromptTimer_PercentFeedback_OutputChange;

                // respond to offs by cancelling dialog
                var onFb = CurrentRoom.OnFeedback;
                EventHandler<FeedbackEventArgs> offHandler = null;
                offHandler = (o, a) =>
                {
                    Debug.Console(LogLevel, "{0} offHandler: {1}", ClassName, onFb.BoolValue);
                    if (!onFb.BoolValue)
                    {
                        //EndMeetingButtonSig.BoolValue = false;
                        PowerDownModal.HideDialog();
                        onFb.OutputChange -= offHandler;
                        //gauge.OutputChange -= gaugeHandler;
                    }
                };
                onFb.OutputChange += offHandler;

                PowerDownModal.PresentModalDialog(2, "System Power", "Power", message, "Cancel", "Confirm", true, true,
                    but =>
                    {
                        if (but != 2) // any button except for End cancels
                        {
                            Debug.Console(LogLevel, "{0} PresentModalDialog Cancel button: {1}", ClassName, but);
                            timer.Cancel(); // this will invoke ShutdownPromptTimer_WasCancelled
                        }
                        else
                        {
                            Debug.Console(LogLevel, "{0} PresentModalDialog Finish button: {1}", ClassName, but);
                            timer.Finish(); // this will invoke ShutdownPromptTimer_HasFinished
                        }
                    });
            }
        }

        /// <summary>
        /// Called when the timer has finished or confirmed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_HasFinished(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} ShutdownPromptTimer_HasFinished", ClassName);
            //EndMeetingButtonSig.BoolValue = false;
            CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange -= ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange -= ShutdownPromptTimer_PercentFeedback_OutputChange;
            //CurrentRoom.ShutdownPromptTimer.Finish(); // this causes a recursive loop
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ShutdownPromptTimer_WasCancelled(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} ShutdownPromptTimer_WasCancelled", ClassName);
            if (PowerDownModal != null)
                PowerDownModal.HideDialog();
            //EndMeetingButtonSig.BoolValue = false;
            //ShareButtonSig.BoolValue = CurrentRoom.OnFeedback.BoolValue;

            CurrentRoom.ShutdownPromptTimer.TimeRemainingFeedback.OutputChange -= ShutdownPromptTimer_TimeRemainingFeedback_OutputChange;
            CurrentRoom.ShutdownPromptTimer.PercentFeedback.OutputChange -= ShutdownPromptTimer_PercentFeedback_OutputChange;
        }

        void ShutdownPromptTimer_TimeRemainingFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} ShutdownPromptTimer_TimeRemaining: {1}", ClassName, (sender as StringFeedback).StringValue);
            var message = string.Format("System will power off in {0} seconds", (sender as StringFeedback).StringValue);
            TriList.StringInput[ModalDialog.MessageTextJoin].StringValue = message;
        }

        void ShutdownPromptTimer_PercentFeedback_OutputChange(object sender, EventArgs e)
        {
            Debug.Console(LogLevel, "{0} ShutdownPromptTimer_PercentFeedback: {1}%", ClassName, (sender as IntFeedback).UShortValue);
            var value = (ushort)((sender as IntFeedback).UShortValue * 65535 / 100);
            TriList.UShortInput[ModalDialog.TimerGaugeJoin].UShortValue = value;
        }

        #endregion ShutdownPromptTimer

        #region register when visible

        private void PopupInterlock_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            Debug.Console(LogLevel, "{0} PopupInterlock_StatusChanged", ClassName);
            if (e.NewJoin == PageJoin)
                Register();
            else if (e.PreviousJoin == PageJoin)
                Unregister();
        }
        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
        }
        void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
            //TriList.ClearBoolSigAction(joins.UiBoolJoin.ToggleButtonPress);
        }

        #endregion register when visible

    }
}
