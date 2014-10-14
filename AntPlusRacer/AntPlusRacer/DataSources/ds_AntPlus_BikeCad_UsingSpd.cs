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
    class ds_AntPlus_BikeCad_UsingSpd : ds_AntPlus
    {
        public const int MAX_NO_EVENT_STOP_COUNT = 12;

        byte noCadenceEventCount;

        double calculatedCadence;

        ushort lastCadTime_1024;
        ushort lastCadEventNum;

        double lastDistance = 0;

        ds_AntPlus_BikeSpd speedSensor;

        public ds_AntPlus_BikeCad_UsingSpd(ds_AntPlus_BikeSpd speedSensor)
            :base("Ant+ Cad w/Spd", racerSportType.Biking)
        {
            if (speedSensor.isInUse)
                throw new ArgumentException("Can't use a Speed Sensor that is already in use.");

            speedSensor.customSourceName = "Spd used by Cad w/Spd";
            speedSensor.isInUse = true;
            this.speedSensor = speedSensor;
            speedSensor.start(newSpeedSensorPacket);    //Ensure speed sensor is always running so we can always get the values
        }

        public override void start(Action<DataSourcePacket> distanceUpdateHandler)
        {
            //Reset the speed sensor first so we get back to a zero distance 
            speedSensor.stop();
            lastDistance = 0;
            speedSensor.reset();            
            speedSensor.start(newSpeedSensorPacket);

            base.start(distanceUpdateHandler);
        }

        public void newSpeedSensorPacket(DataSourcePacket pckt)
        {
            double distDiff = pckt.distance - lastDistance;
            if(distDiff > 0)
                incrementDistanceAndUpdate(distDiff, speedMs: pckt.speed_ms, cadence: calculatedCadence);
            lastDistance = pckt.distance;
        }

        protected override AntChannelProfile getDefaultSearchProfile()
        {
            return new AntChannelProfile()
            {
                rfOffset = 57,
                transType = 0,
                deviceType = 122,
                deviceNumber = 0,
                messagePeriod = 8102,
                pairingEnabled = false,
            };
        }

        public override void handleChannelResponse(ANT_Managed_Library.ANT_Response response)
        {
            if (response.responseID == (byte)ANT_Managed_Library.ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E)
            {
                //In this decode we ignore page change toggle and page type, since the info we need is transmitted on every page


                int eventDiff = 0;
                int timeDiff_1024 = 0;

                // Decode data page
                ushort curCadTime_1024 = (ushort)(response.messageContents[5] + (response.messageContents[6] << 8));
                ushort curCadEventNum = (ushort)(response.messageContents[7] + (response.messageContents[8] << 8));

                // Initialize previous values on first message received
                if (isInitialized) //Otherwise just init values below
                {
                    //Calculate Cadence
                    eventDiff = (int)curCadEventNum - (int)lastCadEventNum;
                    if (eventDiff != 0)
                    {
                        if (eventDiff < 0) //check for rollover
                            eventDiff = 65536 + eventDiff;

                        noCadenceEventCount = 0; //reset counter

                        timeDiff_1024 = (int)curCadTime_1024 - (int)lastCadTime_1024;
                        if (timeDiff_1024 != 0) //Ignore cadence if time hasn't changed
                        {
                            if (timeDiff_1024 < 0)//check for rollover
                                timeDiff_1024 = 65536 + timeDiff_1024;

                            calculatedCadence = (double)eventDiff / ((double)timeDiff_1024 / 61440); //events / time/61440 ticks/min = rpm
                        }

                    }
                    else //Cadence event unchanged
                    {
                        ++noCadenceEventCount;
                        if (noCadenceEventCount >= MAX_NO_EVENT_STOP_COUNT) //Are we coasting?
                        {
                            noCadenceEventCount = MAX_NO_EVENT_STOP_COUNT; //Ensure we don't roll over
                            calculatedCadence = 0;
                        }
                    }
                }
                else
                    isInitialized = true;

                // Update previous values
                lastCadTime_1024 = curCadTime_1024;
                lastCadEventNum = curCadEventNum;

                //Ensure speed sensor is always running so we can always get the values
                if (!speedSensor.isInitialized)  //If speed sensor is not receiving data, we need to push our data
                    incrementDistanceAndUpdate(0, cadence: calculatedCadence);
                else if (!speedSensor.isRunning()) //If the speed sensor is receiving data, start it to push data to us
                    speedSensor.start(newSpeedSensorPacket);

                

            }
        }
    }
}
