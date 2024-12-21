
using System;

namespace MVirus.Client
{
    public class DataSpeedCalculator
    {
        private long lastTick;
        private long lastData;
        private float lastSpeed;

        public DataSpeedCalculator(long totalData)
        {
            lastData = totalData;
            lastTick = DateTime.Now.Ticks;
        }

        public void Update(long newData)
        {
            var thisTick = DateTime.Now.Ticks;

            var interval = (thisTick - lastTick) / TimeSpan.TicksPerMillisecond / 1000f;
            var dataPerInterval = lastData - newData;

            if (interval == 0)
                lastSpeed = 0;
            else
                lastSpeed = dataPerInterval / interval;

            lastData = newData;
            lastTick = thisTick;
        }

        public string GetUserFriendlyString()
        {
            float divBy;
            string valueName;
            if (lastSpeed < 1024)
            {
                divBy = 1;
                valueName = "Byte";
            }
            else if (lastSpeed < 1024 * 1024)
            {
                divBy = 1024f;
                valueName = "Kb";
            }
            else
            {
                divBy = 1024f * 1024f;
                valueName = "Mb";
            }
            return string.Format("{0:0.00} {1}/s", lastSpeed / divBy, valueName);
        }
    }
}
