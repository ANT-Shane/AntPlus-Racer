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
    public class DataSourcePacket
    {
        public readonly double distance; //m
        public readonly double cadence; //rpm
        public readonly double speed_ms; //m/s
        public readonly byte heartRate; //bpm
        public readonly ushort power; //W

        public DataSourcePacket(double dist, double spdMs, double cad, byte hr, ushort pwr)
        {
            distance = dist;
            cadence = cad;
            speed_ms = spdMs;
            heartRate = hr;
            power = pwr;
        }
    }

    public abstract class DataSourceBase
    {
        double distanceTravelled = 0;
        DataSourcePacket lastPacketRcvd = null;
        protected racerSportType sportType;

        const double defaultRunStrideLength = 2.5;
        const double defaultRowStrideLength = 5;
        const double defaultBikeStrideLength = 8;
        const double defaultSkiStrideLength = 4.5;

        public string customSourceName;

        public bool isInUse = false;
        public bool isHuman;

        static byte uidCount = 0;
        public readonly byte uid;
            

        public DataSourceBase(racerSportType sportType, bool isHuman)
        {
            this.sportType = sportType;
            this.isHuman = isHuman;
            uid = ++uidCount;
            if (uidCount == byte.MaxValue)
                uidCount = byte.MaxValue - 1; //If we overflow just stay at max instead of colliding with earlier values
        }

        Action<DataSourcePacket> distanceUpdate;
        bool running = false;

        public racerSportType getSportType()
        {
            return sportType;
        }

        public double getCurrentDistance()
        {
            return distanceTravelled;
        }

        public void incrementDistanceAndUpdate(double distToAdd, double speedMs = 0xFFFF, double cadence = -1, byte heartRate = 0xFF, ushort powerW = 0xFFFF)
        {
            if (running)
            {
                if (cadence == -1)
                    cadence = calculateCadence(speedMs);

                distanceTravelled += distToAdd;
                lastPacketRcvd = new DataSourcePacket(distanceTravelled, speedMs, cadence, heartRate, powerW);
                distanceUpdate(lastPacketRcvd);
            }
            else //Just save the last packet for monitoring purposes
            {
                if(lastPacketRcvd == null)
                    lastPacketRcvd = new DataSourcePacket(distToAdd, speedMs, cadence, heartRate, powerW);
                else
                    lastPacketRcvd = new DataSourcePacket(lastPacketRcvd.distance + distToAdd, speedMs, cadence, heartRate, powerW);
            }
        }

        public virtual void reset()
        {
            stop();
            distanceTravelled = 0;
            lastPacketRcvd = null;
        }

        public virtual void start(Action<DataSourcePacket> distanceUpdateHandler)
        {
            distanceUpdate = distanceUpdateHandler;
            running = true;            
        }

        public virtual void stop()
        {
            running = false;
            distanceUpdate = null;
        }

        public bool isRunning()
        {
            return running;
        }

        public abstract string getDefaultSourceName();

        public string getSourceName()
        {
            if (String.IsNullOrWhiteSpace(customSourceName))
                return getDefaultSourceName();
            else
                return customSourceName;
        }

        public DataSourcePacket getLastDataRcvd()
        {
            return lastPacketRcvd;
        }

        protected double calculateCadence(double speed_Ms)
        {
            switch (sportType)
            {
                case racerSportType.Running:
                    return (speed_Ms * 60) / DataSourceBase.defaultRunStrideLength; //[m/min] / [m/str] = str/min
                case racerSportType.Rowing:
                    return (speed_Ms * 60) / DataSourceBase.defaultRowStrideLength;
                case racerSportType.Biking:
                    return (speed_Ms * 60) / DataSourceBase.defaultBikeStrideLength;
                case racerSportType.Skiing:
                    return (speed_Ms * 60) / DataSourceBase.defaultSkiStrideLength;
                default:
                    return 0;
            }
        }
    }
}
