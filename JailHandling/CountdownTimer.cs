using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Orpheus.JailHandling
{
    public class CountdownTimer
    {
        private int seconds;
        private bool isCheckTime;
        private bool end = false;

        public CountdownTimer(int seconds)
        {
            this.seconds = seconds;
            isCheckTime = false;
        }

        public CountdownTimer(int minutes, int seconds)
        {
            this.seconds = (minutes * 60) + seconds;
            isCheckTime = false;
        }

        public CountdownTimer(int hours, int minutes, int seconds)
        {
            this.seconds = (hours * 60 * 60) + (minutes * 60) + seconds;
            isCheckTime = false;
        }

        public async Task startCountDown()
        {
            await countdown();
        }

        private async Task countdown()
        {
            int waitedSeconds = 0;
            while (seconds > 0)
            {
                await Task.Delay(1000);
                waitedSeconds++;
                seconds--;
                if (waitedSeconds >= getTaskWaitSeconds())
                {
                    waitedSeconds = 0;
                    isCheckTime = true;
                }
                if (end)
                {
                    return;
                }
            }
        }

        public void lowerTimeBySeconds(int secondsToLowerBy)
        {
            seconds -= secondsToLowerBy;
        }

        public void lowerTimeByMinutes(int minutesToLowerBy)
        {
            lowerTimeBySeconds(minutesToLowerBy * 60);
        }

        public void lowerTimeByHours(int hoursToLowerBy)
        {
            lowerTimeByMinutes(hoursToLowerBy * 60);
        }

        public void lowerTimeBy(int hoursToLowerBy, int minutesToLowerBy, int secondsToLowerBy)
        {
            lowerTimeByHours(hoursToLowerBy);
            lowerTimeByMinutes(minutesToLowerBy);
            lowerTimeBySeconds(secondsToLowerBy);
        }

        public int getSecondsRemaining()
        {
            int temp = seconds;
            int hoursTill = temp / 3600;
            temp %= 3600;
            int minutesTill = temp / 60;
            temp %= 60;
            return temp;
        }

        public int getTotalSecondsRemaining()
        {
            return seconds;
        }

        public int getMinutesRemaining()
        {
            int temp = seconds;
            int hoursTill = temp / 3600;
            temp %= 3600;
            int minutesTill = temp / 60;
            return minutesTill;
        }

        public int getHoursRemaining()
        {
            int temp = seconds;
            int hoursTill = temp / 3600;
            return hoursTill;
        }

        public string toString()
        {
            int temp = seconds;
            int hoursTill = temp / 3600;
            temp %= 3600;
            int minutesTill = temp / 60;
            temp %= 60;

            return $"{hoursTill}:{minutesTill}:{temp}";
        }

        public string toQuickTime()
        {
            if (getHoursRemaining() > 0)
            {
                return $"{getHoursRemaining()} Hours";
            }
            else if (getMinutesRemaining() > 0)
            {
                return $"{getMinutesRemaining()} Minutes";
            }
            else if (getSecondsRemaining() >= 0)
            {
                return $"{getSecondsRemaining()} Seconds";
            }
            return "0 Seconds";
        }

        public int getTaskWaitSeconds()
        {
            if (getHoursRemaining() > 1)
            {
                //hours remaining
                return 3600;
            }
            else if (getMinutesRemaining() > 1)
            {
                //minutes remaining
                return 60;
            }
            else
            {
                return 5;
            }
        }

        public bool IsUpdateTime()
        {
            if (isCheckTime)
            {
                isCheckTime = false;
                return true;
            }
            return false;
        }

        public void endCountdown()
        {
            seconds = 0;
            end = true;
        }
    }
}
