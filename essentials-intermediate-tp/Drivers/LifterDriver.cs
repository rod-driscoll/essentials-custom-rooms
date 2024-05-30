using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using essentials_basic_tp_epi.joins;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Shades;
using PepperDash.Essentials.Room.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace essentials_basic_tp.Drivers
{
    public class LifterDriver : PanelDriverBase, IBasicRoomSetup, IShadesFeedback
    {
        public string ClassName { get { return "LifterDriver"; } }
        public uint LogLevel { get; set; }

        public uint UpPressJoin { get; private set; }
        public uint DownPressJoin { get; private set; }
        public uint StopPressJoin { get; private set; }
        public uint PageJoin { get; private set; }

        public IShadesOpenCloseStop CurrentDefaultDevice { get; private set; }

        public IntFeedback PositionFeedback { get; private set; }
        public BoolFeedback IsStoppedFeedback { get; private set; }

        public LifterDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
                    : base(parent.TriList)
        {
            try
            {
                Debug.Console(LogLevel, "{0} constructor", ClassName);
                LogLevel = 2;
                //Parent = parent;
                UpPressJoin = UiBoolJoin.ScreenUpPress;
                DownPressJoin = UiBoolJoin.ScreenDownPress;
                StopPressJoin = UiBoolJoin.ScreenStopPress;
                Debug.Console(LogLevel, "{0} constructor Register", ClassName);
                Register(); // the driver is always available so register here rather than on popupinterlock
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} constructor ERROR: {1}", ClassName, e.Message);
            }
            Debug.Console(LogLevel, "{0} constructor done", ClassName);
        }

        /// <summary>
        /// Called when room changes
        /// </summary>
        /// <param name="roomConf"></param>
        public void Setup(IBasicRoom room)
        {
            Debug.Console(LogLevel, "{0} ****************************** Setup, {1}", ClassName, room == null ? "== null" : room.Key);
            //EssentialsRoomPropertiesConfig roomConf = room.PropertiesConfig;
            IShadesOpenCloseStop device_;
            if (CurrentDefaultDevice != null) // Disconnect current room 
            {
            }

            var room_ = room as IHasDisplayFunction;
            Debug.Console(LogLevel, "{0} Setup, IHasDisplayFunction {1}", ClassName, room_ == null ? "== null" : room.Key);
            if (room_ != null)
            {
                Debug.Console(LogLevel, "{0} Setup, Driver {1}", ClassName, room_.Display == null ? "== null" : "exists");
                CurrentDefaultDevice = room_.Display.DefaultLifter;
                Debug.Console(LogLevel, "{0} Setup, Driver.DefaultLifter {1}", ClassName, room_.Display.DefaultLifter == null ? "== null" : room_.Display.DefaultLifter.Key);
                //device_ = CurrentDefaultDevice as IShadesOpenCloseStop;
                //Debug.Console(LogLevel, "{0} Setup, IShadesOpenCloseStop {1}", ClassName, device_ == null ? "== null" : "exists");
            }
            Debug.Console(LogLevel, "{0} Setup done, {1}", ClassName, room == null ? "== null" : room.Key);
        }

        public void Register()
        {
            try
            {
                Debug.Console(LogLevel, "{0} Register", ClassName);
                TriList.SetSigFalseAction(UpPressJoin, Close);
                TriList.SetSigFalseAction(DownPressJoin, Open);
                TriList.SetSigFalseAction(StopPressJoin, Stop);
            }
            catch (Exception e)
            {
                Debug.Console(LogLevel, "{0} Register ERROR: {1}", ClassName, e.Message);
            }
        }
        public void Unregister()
        {
            Debug.Console(LogLevel, "{0} Unregister", ClassName);
            TriList.ClearBoolSigAction(UpPressJoin);
            TriList.ClearBoolSigAction(DownPressJoin);
            TriList.ClearBoolSigAction(StopPressJoin);
        }

        public void SetPosition(ushort value)
        {
            // TODO: make timer and keep track of position 
            if (value > ushort.MaxValue / 2)
                Open();
            else
                Close();
        }

        public void Open()
        {
            Debug.Console(LogLevel, "{0} Open", ClassName);
            CurrentDefaultDevice?.Open();
        }
        public void Close()
        {
            Debug.Console(LogLevel, "{0} Close", ClassName);
            CurrentDefaultDevice?.Close();
        }
        public void Stop()
        {
            Debug.Console(LogLevel, "{0} Stop", ClassName);
            CurrentDefaultDevice?.Stop();
        }
    }
}
