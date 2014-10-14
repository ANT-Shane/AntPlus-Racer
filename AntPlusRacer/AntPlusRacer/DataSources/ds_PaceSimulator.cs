/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntPlusRacer.DataSources
{
    class ds_PaceSimulator: DataSourceBase
    {
        private double speedMs;
        private double speedPerClock;
        private double cadence;
        protected ushort power = 0xFFFF;
        System.Timers.Timer constantSpeedTimer;


        public ds_PaceSimulator(racerSportType sportType)
            : base(sportType, false)
        {
            ////RowingPower
            //power = (ushort)Math.Round(2.8 / Math.Pow(1 / speedMs, 3)); //watts = 2.80/pace³ - source: www.concept2.com/us/interactive/calculators/watts.asp
        }

        protected void initPaceTimer(double speedMs)
        {
            this.speedMs = speedMs;
            double clocksPerSec = 4;
            speedPerClock = speedMs / clocksPerSec;

            cadence = calculateCadence(speedMs);

            constantSpeedTimer = new System.Timers.Timer((1 / clocksPerSec) * 1000 * 0.98); //0.98 is an adjustment to balance the inaccuracy of the timer so the resulting speed is accurate
            constantSpeedTimer.Elapsed += new System.Timers.ElapsedEventHandler(constantSpeedTimer_Elapsed);
        }

        public override void start(Action<DataSourcePacket> distanceUpdateHandler)
        {
            base.start(distanceUpdateHandler);
            constantSpeedTimer.Start();
        }

        public override void stop()
        {
            constantSpeedTimer.Stop();
            base.stop();
        }

        void constantSpeedTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock (constantSpeedTimer)
            {
                incrementDistanceAndUpdate(speedPerClock, speedMs, cadence, powerW: power);
            }
        }

        public override string getDefaultSourceName()
        {
            return "Pacer @ " + (speedMs*3.6).ToString("0.0") + "kph";
        }

        public static double convertPaceTo_mPerSec(int minPerKm, double secondsPerKm)
        {
            secondsPerKm += minPerKm * 60;
            return 1000 / secondsPerKm;
        }
    }
}
