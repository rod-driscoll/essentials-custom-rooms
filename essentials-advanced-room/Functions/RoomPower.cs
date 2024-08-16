using Crestron.SimplSharp;
using essentials_advanced_room;
using PepperDash.Core;
using Serilog.Events;
using System;
using Device = essentials_advanced_room.Device;

namespace essentials_advanced_room.Functions
{
    public enum PowerStates
    {
        unknown = 0,
        on = 1,
        standby = 2,
        off = 3,
        warming = 4,
        cooling = 5,
        toggle = 6,
        error = 7
    }

    public class PowerEventArgs : EventArgs
    {
        public PowerStates Current { get; set; } // must declare get; set; so it can be used in S+
        public PowerStates Pending { get; set; } // must declare get; set; so it can be used in S+
        public uint SecondsRemaining { get; set; } // must declare get; set; so it can be used in S+
        public uint id { get; set; }
        //[System.Obsolete("Empty ctor only exists to allow class to be used in S+", true)]
        public PowerEventArgs() { } // must declare an empty ctor so it can be used in S+
        public PowerEventArgs(PowerStates current, PowerStates pending, uint SecondsRemaining)
        {
            this.Current = current;
            this.Pending = pending;
            this.SecondsRemaining = SecondsRemaining;
        }
    }
    public class RoomPower: IDisposable, ILogClassDetails
    {
        public string ClassName { get { return "RoomPower"; } }
        public LogEventLevel LogLevel { get; set; }
        public Config config { get; private set; }
        public Device parent { get; private set; }

        CTimer PowerTimer;
        public event EventHandler<PowerEventArgs> PowerChange;

        public uint WarmSeconds { get; private set; }
        public uint CoolSeconds { get; private set; }
        public uint CurrentSeconds { get; private set; }

        public PowerStates PowerStatus { get; private set; }
        public PowerStates PendingPowerStatus { get; private set; }

        public RoomPower(Config config)
        {
            LogLevel = LogEventLevel.Information;
            Debug.LogMessage(LogLevel, "{0} constructor", ClassName);
            this.config = config;
            this.parent = parent;
            if (WarmSeconds < 1) WarmSeconds = 5;
            if (CoolSeconds < 1) CoolSeconds = 5;

            CustomActivate();
        }
        public void CustomActivate()
        {
            //Debug.LogMessage(LogLevel, "{0} CustomActivate", ClassName);
        }

        public void Dispose()
        {
            Debug.LogMessage(LogLevel, "{0} Dispose", ClassName);
            if (PowerTimer != null)
            {
                PowerTimer.Stop();
                PowerTimer.Dispose();
                PowerTimer = null;
            }
        }
        private void OnPower(PowerEventArgs args)
        {
            Debug.LogMessage(LogLevel, "{0} OnPower start", ClassName);
            if (PowerChange != null)
                PowerChange(this, args);
            Debug.LogMessage(LogLevel, "{0} OnPower done", ClassName);
        }
        public void SetPowerOn()
        {
            Debug.LogMessage(LogLevel, "{0} SetPowerOn, current: {1}, {2} seconds remaining", ClassName, PowerStatus.ToString(), CurrentSeconds.ToString());
            PendingPowerStatus = PowerStates.on;
            if (PowerStatus != PowerStates.on
                && PowerStatus != PowerStates.warming
                && PowerStatus != PowerStates.cooling)
            {
                if (CurrentSeconds > 0)
                    Debug.LogMessage(LogLevel, "{0} WARMING already running", ClassName);
                else
                {
                    CurrentSeconds = WarmSeconds;
                    PowerStatus = PowerStates.warming;
                    StartPowerTimer();
                }
                OnPower(new PowerEventArgs(PowerStatus, PendingPowerStatus, CurrentSeconds));
            }
        }

        public virtual void SetPowerOff()
        {
            Debug.LogMessage(LogLevel, "{0} SetPowerOff, current: {1}, {2} seconds remaining, pending: {3}", ClassName, PowerStatus.ToString(), CurrentSeconds.ToString(), PendingPowerStatus.ToString());
            PendingPowerStatus = PowerStates.standby;
            if (PowerStatus != PowerStates.off
                && PowerStatus != PowerStates.standby
                && PowerStatus != PowerStates.warming
                && PowerStatus != PowerStates.cooling)
            {
                if (CurrentSeconds > 0)
                    Debug.LogMessage(LogLevel, "{0} SetPowerOff {1} already running", ClassName, PendingPowerStatus);
                else
                {
                    Debug.LogMessage(LogLevel, "{0} SetPowerOff starting", ClassName);
                    CurrentSeconds = CoolSeconds;
                    PowerStatus = PowerStates.cooling;
                    StartPowerTimer();
                }
                OnPower(new PowerEventArgs(PowerStatus, PendingPowerStatus, CurrentSeconds));
            }
        }
        public virtual void SetPowerToggle()
        {
            SetPower(PowerStates.toggle);
        }
        public virtual void SetPower(PowerStates state)
        {
            Debug.LogMessage(LogLevel, "{0} SetPower {1}", ClassName, state);
            switch (state)
            {
                case PowerStates.off:
                case PowerStates.standby: SetPowerOff(); break;
                case PowerStates.on: SetPowerOn(); break;
                case PowerStates.toggle:
                    if (PowerStatus == PowerStates.on)
                        SetPowerOff();
                    else
                        SetPowerOn();
                    break;
            }
        }
        public void ForcePowerOn()
        {
            PowerStatus = PowerStates.off;
            SetPowerOn();
        }
        public void ForcePowerOff()
        {
            Debug.LogMessage(LogLevel, "{0} ForcePowerOff {1}", ClassName, PowerStatus);
            PowerStatus = PowerStates.on;
            SetPowerOff();
        }

        public virtual void SetPowerFeedback(PowerStates state)
        {
            //Debug.LogMessage(LogLevel, "{0} SetPowerFeedback {1}", ClassName, state);
            if (PowerStatus != state || CurrentSeconds > 0)
            {
                PowerStatus = state;
                OnPower(new PowerEventArgs(PowerStatus, PendingPowerStatus, CurrentSeconds));
            }
        }
        void PowerTimerExpired(object obj)
        {
            Debug.LogMessage(LogLevel, "{0} PowerTimerExpired, pending: {1}, {2} seconds remaining", ClassName, PendingPowerStatus.ToString(), CurrentSeconds.ToString());
            try
            {
                if (PowerTimer != null)
                {
                    Debug.LogMessage(LogLevel, "{0} PowerTimerExpired {1}", ClassName, CurrentSeconds);
                    if (CurrentSeconds < 1)
                    {
                        Debug.LogMessage(LogLevel, "{0} PowerTimerExpired, unsubscribing", ClassName);
                        CurrentSeconds = 0;
                        if (PowerStatus == PowerStates.cooling)
                        {
                            if(PendingPowerStatus == PowerStates.off || PendingPowerStatus == PowerStates.standby)
                                PowerStatus = PendingPowerStatus;
                        }
                        else if (PowerStatus == PowerStates.warming)
                            PowerStatus = PowerStates.on;

                        if (PowerStatus == PendingPowerStatus)
                        {
                            Debug.LogMessage(LogLevel, "{0} PowerTimerExpired, PowerStatus == PendingPowerStatus", ClassName);
                            Dispose();
                        }
                        else
                            SetPower(PendingPowerStatus);
                    }
                    else
                    {
                        CurrentSeconds--;
                        if (PowerStatus == PowerStates.cooling)
                        {
                            Debug.LogMessage(LogLevel, "{0} {1} resend {2} {3}", ClassName, PowerStatus.ToString(), PendingPowerStatus.ToString(), CurrentSeconds.ToString());
                            SetPower(PendingPowerStatus);
                        }
                    }
                    OnPower(new PowerEventArgs(PowerStatus, PendingPowerStatus, CurrentSeconds));
                    Debug.LogMessage(LogLevel, "{0} PowerTimerExpired, OnPower done", ClassName);
                }
                else
                    Debug.LogMessage(LogLevel, "{0} PowerTimerExpired, PowerTimer == null", ClassName);

                //Debug.LogMessage(LogLevel, "{0} PowerTimerExpired done", ClassName);
            }
            catch (Exception e)
            {
                Debug.LogMessage(LogLevel, "{0} PowerTimer ERROR: {1}", ClassName, e.Message);
            }
        }

        private void StartPowerTimer()
        {
            if (PowerTimer != null)
            {
                Debug.LogMessage(LogLevel, "{0} StartPowerTimer resetting", ClassName);
                PowerTimer.Reset(1000, 1000);
            }
            else
            {
                Debug.LogMessage(LogLevel, "{0} StartPowerTimer creating new PowerTimer", ClassName);
                PowerTimer = new CTimer(PowerTimerExpired, this, 1000, 1000);
            }
            Debug.LogMessage(LogLevel, "{0} StartPowerTimer end", ClassName);
        }
    }
}
