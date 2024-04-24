using Crestron.SimplSharp;
using essentials_basic_room_epi;
using essentials_basic_tp_epi.Drivers;
using PepperDash.Core;
using PepperDash.Essentials;
using PepperDash.Essentials.Core;

namespace essentials_basic_tp.Drivers
{
    internal class NotificationRibbonDriver : PanelDriverBase, IBasicRoomSetup
    {
        public string ClassName { get { return "NotificationRibbonDriver"; } }

        /// <summary>
        /// Controls timeout of notification ribbon timer
        /// </summary>
        CTimer RibbonTimer;

        /// <summary>
        /// The parent driver for this
        /// </summary>
        private BasicPanelMainInterfaceDriver Parent;
        public NotificationRibbonDriver(BasicPanelMainInterfaceDriver parent, CrestronTouchpanelPropertiesConfig config)
            : base(parent.TriList)
        {
            Parent = parent;
            Debug.Console(2, "{0} constructor done", ClassName);
        }


        /// <summary>
        /// Reveals a message on the notification ribbon until cleared
        /// </summary>
        /// <param name="message">Text to display</param>
        /// <param name="timeout">Time in ms to display. 0 to keep on screen</param>
        public void ShowNotificationRibbon(string message, int timeout)
        {
            TriList.SetString(UIStringJoin.NotificationRibbonText, message);
            TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, true);
            if (timeout > 0)
            {
                if (RibbonTimer != null)
                    RibbonTimer.Stop();
                RibbonTimer = new CTimer(o =>
                {
                    TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, false);
                    RibbonTimer = null;
                }, timeout);
            }
        }

        /// <summary>
        /// Hides the notification ribbon
        /// </summary>
        public void HideNotificationRibbon()
        {
            TriList.SetBool(UIBoolJoin.NotificationRibbonVisible, false);
            if (RibbonTimer != null)
            {
                RibbonTimer.Stop();
                RibbonTimer = null;
            }
        }

        public void Setup(IBasicRoom room)
        { 
            //Debug.Console(2, "{0} Setup", ClassName);
            Debug.Console(2, "{0} Setup done", ClassName);
        }
    }
}
