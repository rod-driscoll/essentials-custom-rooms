using Crestron.SimplSharp;
using essentials_advanced_room;
using essentials_advanced_room.Functions;
using essentials_advanced_tp.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Shades;
using System.Linq;
using joins = essentials_advanced_tp.joins;

namespace essentials_basic_tp.Drivers
{
    public class ScreenDriver : PanelDriverBase, IAdvancedRoomSetup//, IShadesFeedback
    {
        public string ClassName { get { return "ScreenDriver"; } }
        public uint LogLevel { get; set; }

        private BasicPanelMainInterfaceDriver Parent;
        NotificationRibbonDriver ribbonDriver;
        CTimer SecondTimer;

        public uint UpPressJoin { get; private set; }
        public uint DownPressJoin { get; private set; }
        public uint StopPressJoin { get; private set; }
        public uint TogglePressJoin { get; private set; }
        public uint SecondsRemainingJoin { get; private set; }
        public uint PositionPercentJoin { get; private set; }
        public uint StatusTextJoin { get; private set; }
        public uint PageJoin { get; private set; }

        public ShadeBase CurrentDefaultDevice { get; private set; }
        public PowerStates CurrentDefaultDevicePowerState { get; private set; }

        public ScreenDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
                    : base(parent.TriList)
        {
            LogLevel =  2;
            Parent = parent;
            UpPressJoin = joins.UIBoolJoin.ScreenUpPress;
            DownPressJoin = joins.UIBoolJoin.ScreenDownPress;
            StopPressJoin = joins.UIBoolJoin.ScreenStopPress;
            TogglePressJoin = joins.UIBoolJoin.ScreenTogglePress;
            SecondsRemainingJoin = joins.UIUshortJoin.ScreenSecondsRemaining;
            PositionPercentJoin = joins.UIUshortJoin.ScreenPositionPercent;
            StatusTextJoin = joins.UIStringJoin.ScreenStatusTextJoin;

            var ribbon = Parent.ChildDrivers.First(x => x is NotificationRibbonDriver);
            if (ribbon != null)
                ribbonDriver = ribbon as NotificationRibbonDriver;

            Register(); // the driver is always available so register here rather than on popupinterlock
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        private void ConnectDevice(ShadeBase device)
        {
            CurrentDefaultDevice = device;
            Debug.Console(LogLevel, "{0} ConnectDevice, CurrentDefaultDevice {1}", ClassName, device == null ? "== null" : device.Key);

            var positionFeedback_ = device as IShadesFeedback;
            if (positionFeedback_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, registering PositionFeedback", ClassName);
                //positionFeedback_.PositionFeedback.OutputChange += PositionFeedback_OutputChange;
                positionFeedback_.PositionFeedback.LinkInputSig(TriList.UShortInput[PositionPercentJoin]);
            }

            var openCloseFeedback_ = device as IShadesOpenClosedFeedback;
            if (openCloseFeedback_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, registering OpenClosedFeedback", ClassName);
                openCloseFeedback_.ShadeIsClosedFeedback.OutputChange += IsClosedFeedback_OutputChange;
                openCloseFeedback_.ShadeIsOpenFeedback.OutputChange += IsOpenFeedback_OutputChange;
            }

            var stopFeedback_ = device as IShadesStopFeedback;
            if (stopFeedback_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, registering StopFeedback", ClassName);
                //stopFeedback_.IsStoppedFeedback.OutputChange += (o, a) =>
                //    Debug.Console(LogLevel, "{0} IsClosedFeedback {1}", ClassName, (o as IRelayControlledMotor).IsClosedFeedback.BoolValue);
                stopFeedback_.IsStoppedFeedback.LinkInputSig(TriList.BooleanInput[StopPressJoin]);
            }

            var movingFeedback_ = device as IShadesRaiseLowerFeedback;
            if (movingFeedback_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, registering OpeningClosingFeedback", ClassName);
                movingFeedback_.ShadeIsLoweringFeedback.OutputChange += IsLoweringFeedback_OutputChange;
                movingFeedback_.ShadeIsRaisingFeedback.OutputChange += IsRaisingFeedback_OutputChange;
            }
        }
        private void DisconnectDevice(ShadeBase device)
        {
            Debug.Console(LogLevel, "{0} DisconnectDevice, CurrentDefaultDevice {1}", ClassName, device == null ? "== null" : device.Key);

            var positionFeedback_ = device as IShadesFeedback;
            if (positionFeedback_ != null)
            {
                positionFeedback_.PositionFeedback.OutputChange -= PositionFeedback_OutputChange;
                positionFeedback_.PositionFeedback.UnlinkInputSig(TriList.UShortInput[PositionPercentJoin]);
            }

            var openCloseFeedback_ = device as IShadesOpenClosedFeedback;
            if (openCloseFeedback_ != null)
            {
                openCloseFeedback_.ShadeIsClosedFeedback.OutputChange -= IsClosedFeedback_OutputChange;
                openCloseFeedback_.ShadeIsOpenFeedback.OutputChange -= IsOpenFeedback_OutputChange;
            }

            var stopFeedback_ = device as IShadesStopFeedback;
            if (stopFeedback_ != null)
            {
                stopFeedback_.IsStoppedFeedback.UnlinkInputSig(TriList.BooleanInput[StopPressJoin]);
            }

            var movingFeedback_ = device as IShadesRaiseLowerFeedback;
            if (movingFeedback_ != null)
            {
                movingFeedback_.ShadeIsLoweringFeedback.OutputChange -= IsLoweringFeedback_OutputChange;
                movingFeedback_.ShadeIsRaisingFeedback.OutputChange -= IsRaisingFeedback_OutputChange;
            }
            CurrentDefaultDevice = null;
        }

        private void IsOpenClosedFeedback_OutputChange(object sender, FeedbackEventArgs e)
        { 
            var openCloseFeedback_ = CurrentDefaultDevice as IShadesOpenClosedFeedback;
            if (openCloseFeedback_ != null)
            {
                if (openCloseFeedback_.ShadeIsClosedFeedback.BoolValue)
                {
                    TriList.SetBool(DownPressJoin, false);
                    TriList.SetBool(UpPressJoin, true);
                }
                else if (openCloseFeedback_.ShadeIsOpenFeedback.BoolValue)
                {
                    TriList.SetBool(DownPressJoin, true);
                    TriList.SetBool(UpPressJoin, false);
                }
            }
        }
        private void IsOpenFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} IsOpenFeedback {1}", ClassName, e.BoolValue);
            IsOpenClosedFeedback_OutputChange(sender, e);
        }
        private void IsClosedFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} IsClosedFeedback {1}", ClassName, e.BoolValue);
            IsOpenClosedFeedback_OutputChange(sender, e);
        }

        private void IsMovingFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            var movingFeedback_ = CurrentDefaultDevice as IShadesRaiseLowerFeedback;
            if (movingFeedback_ != null)
            {
                if (movingFeedback_.ShadeIsRaisingFeedback.BoolValue)
                    TriList.SetBool(DownPressJoin, false);
                else if (movingFeedback_.ShadeIsLoweringFeedback.BoolValue)
                    TriList.SetBool(UpPressJoin, false);
                StartSecondTimer(movingFeedback_.ShadeIsLoweringFeedback.BoolValue || movingFeedback_.ShadeIsRaisingFeedback.BoolValue);
            }
        }
        private void IsLoweringFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} IsOpeningFeedback {1}", ClassName, e.BoolValue);
            IsMovingFeedback_OutputChange(sender, e);
        }
        private void IsRaisingFeedback_OutputChange(object sender, FeedbackEventArgs e)
        {
            Debug.Console(LogLevel, "{0} IsClosingFeedback {1}", ClassName, e.BoolValue);
            IsMovingFeedback_OutputChange(sender, e);
        }

        
        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IAdvancedRoom room)
        {
            Debug.Console(LogLevel, "{0} Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            //EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            if (CurrentDefaultDevice != null) // Disconnect current room 
                DisconnectDevice(CurrentDefaultDevice);

            var room_ = room as IHasDisplayFunction;
            Debug.Console(LogLevel, "{0} Setup, IHasDisplayFunction {1}", ClassName, room_ == null ? "== null" : room.Key);
            if (room_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, Driver {1}", ClassName, room_.Display == null ? "== null" : "exists");
                ConnectDevice(room_.Display.DefaultScreen);
            }
            Debug.Console(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        private void PositionFeedback_OutputChange(object sender, FeedbackEventArgs a)
        {
            Debug.Console(LogLevel, "{0} PositionFeedback_OutputChange {1}", ClassName, a.IntValue);
            //TriList.SetUshort(PositionPercentJoin, (ushort)(a.IntValue));
        }

        public void Register()
        {
            Debug.Console(LogLevel, "{0} Register", ClassName);
            TriList.SetSigFalseAction(UpPressJoin, Close);
            TriList.SetSigFalseAction(DownPressJoin, Open);
            TriList.SetSigFalseAction(StopPressJoin, Stop);
            TriList.SetSigFalseAction(TogglePressJoin, Toggle);
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
            TriList.ClearBoolSigAction(UpPressJoin);
            TriList.ClearBoolSigAction(DownPressJoin);
            TriList.ClearBoolSigAction(StopPressJoin);
            TriList.ClearBoolSigAction(TogglePressJoin);
        }

        public void SetPosition(ushort percent)
        {
            // TODO: make timer and keep track of position 
            if (percent >= 100)
                Open();
            else if (percent == 0)
                Close();
            else
                (CurrentDefaultDevice as IShadesPosition)?.SetPosition(percent);
        }

        public void Open()
        {
            Debug.Console(LogLevel, "{0} Open: CurrentDefaultDevice {1}", ClassName, CurrentDefaultDevice == null ? " == null" : "exists");
            CurrentDefaultDevice.Open();
        }
        public void Close()
        {
            Debug.Console(LogLevel, "{0} Close: CurrentDefaultDevice {1}", ClassName, CurrentDefaultDevice == null ? " == null" : "exists");
            CurrentDefaultDevice.Close();
        }
        public void Stop()
        {
            CurrentDefaultDevice.Stop();
        }

        private void StartSecondTimer(bool enable)
        {
            Debug.Console(0, "{0} StartSecondTimer: {1}", ClassName, enable);
            if (!enable)
            {
                Dispose();
            }
            else if (SecondTimer == null)
            {
                Debug.Console(0, "{0} StartSecondTimer creating new PowerTimer", ClassName);
                SecondTimer = new CTimer(SecondTimerExpired, this, 1000, 1000);
            }
            //Debug.Console(0, "{0} StartSecondTimer end", ClassName);
        }
        private void SecondTimerExpired(object userSpecific)
        {
            if (CurrentDefaultDevice != null) // make the button flash when moving
            {
                var device_ = CurrentDefaultDevice as IShadesRaiseLowerFeedback;
                Debug.Console(LogLevel, "{0} SecondTimerExpired dispWarmCool {1}", ClassName, device_ == null ? "== null" : (CurrentDefaultDevice as ShadeBase).Key);
                if (device_ != null)
                {
                    if (device_.ShadeIsRaisingFeedback.BoolValue)
                    {
                        TriList.SetBool(UpPressJoin, !TriList.BooleanInput[UpPressJoin].BoolValue);
                        TriList.SetBool(TogglePressJoin, !TriList.BooleanInput[TogglePressJoin].BoolValue);
                    }
                    else if (device_.ShadeIsLoweringFeedback.BoolValue)
                    {
                        TriList.SetBool(DownPressJoin, !TriList.BooleanInput[DownPressJoin].BoolValue);
                        TriList.SetBool(TogglePressJoin, !TriList.BooleanInput[TogglePressJoin].BoolValue);
                    }
                    else
                        Dispose();
                }
            }
        }
        public void Dispose()
        {
            if (SecondTimer != null)
            {
                SecondTimer.Stop();
                SecondTimer = null;
            }
        }
    }
}
