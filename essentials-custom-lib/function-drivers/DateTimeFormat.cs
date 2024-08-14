/*
 * DateTimeFormat
 * Update a clock display
 * 20240326 v1.0 Rod Driscoll<rod@theavitgroup.com.au>
 */

namespace avit_essentials_common.function_drivers
{
    using Crestron.SimplSharp;
    using PepperDash.Core;
    using PepperDash.Essentials.Core;
    using System;

    public class TimeDateFormatClass : IKeyed
    { 
        public string Key { get; private set; }
        bool _isRunning;

        public string Pattern;
        public bool AmPmLowerCase;

        public StringFeedback TimeDateFeedback { get; private set; }
        private CTimer _secondTimer;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key"></param>
        public TimeDateFormatClass(string key)
        {
            Key = key;
            Pattern = "ddd d'th' MMM | h:mmtt";
            TimeDateFeedback = new StringFeedback(() =>
            {
                var now = DateTime.Now;
                // set lowercase AM/PM
                var pattern_ = AmPmLowerCase ? Pattern.Replace("tt", string.Format("'{0}'", now.ToString("tt").ToLower())) : Pattern;
                var result_ = now.ToString(pattern_);
                // replace dayOfMonth 'th' suffix with correct suffix (e.g. '1st', '2nd', '3rd')
                result_ = result_.Replace("'th'",
                      now.Day % 10 == 1 && now.Day % 100 != 11 ? "'st'"
                    : now.Day % 10 == 2 && now.Day % 100 != 12 ? "'nd'"
                : now.Day % 10 == 3 && now.Day % 100 != 13 ? "'rd'"
                : "th");

                //Debug.LogMessage(2, this, $"TimeDateFeedback: {result_}");
                return result_;
            }); 
        }

        /// <summary>
        /// Starts the Timer
        /// </summary>
        public void Start()
        {
            if (_isRunning)
                return;
            if (_secondTimer != null)
                _secondTimer.Stop();
            _secondTimer = new CTimer(SecondElapsedTimerCallback, null, 0, 1000);
            _isRunning = true;
        }

        /// <summary>
        /// Restarts the timer
        /// </summary>
        public void Reset()
        {
            _isRunning = false;
            Start();
        }

        /// <summary>
        /// Cancels the timer (without triggering it to finish)
        /// </summary>
        public void Cancel()
        {
            StopHelper();
        }

        /// <summary>
        /// Called upon expiration, or calling this will force timer to finish.
        /// </summary>
        public void Finish()
        {
            StopHelper();
        }

        void StopHelper()
        {
            if (_secondTimer != null)
                _secondTimer.Stop();
            _isRunning = false;
        }

        void SecondElapsedTimerCallback(object o)
        {
            TimeDateFeedback.FireUpdate();
        }
    }
}