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
    class ds_AntPlus_BikeSpd : ds_AntPlus_RxFailInterpBuf
    {
        public const int MAX_NO_EVENT_STOP_COUNT = 12;
        public const double DEFAULT_WHEEL_CIRCUMFERENCE_m = 2.095; //Average 700cx23mm road tire

        public readonly double wheelCircumfrence_m;

        byte noSpeedEventCount;

        double calculatedSpeed;
        double calculatedDistIncrease;

        ushort lastSpdTime_1024;
        ushort lastSpdEventNum;

        public ds_AntPlus_BikeSpd(double wheelCircumfrence_m = DEFAULT_WHEEL_CIRCUMFERENCE_m)
            :base("Ant+ Spd", racerSportType.Biking)
        {
            this.wheelCircumfrence_m = wheelCircumfrence_m;
        }

        protected override AntChannelProfile getDefaultSearchProfile()
        {
            return new AntChannelProfile()
            {
                rfOffset = 57,
                transType = 0,
                deviceType = 123,
                deviceNumber = 0,
                messagePeriod = 8118,
                pairingEnabled = false,
            };
        }

        protected override void handleNonRxFailChannelResponse(ANT_Managed_Library.ANT_Response response)
        {
            if (response.responseID == (byte)ANT_Managed_Library.ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E)
            {
                //In this decode we ignore page change toggle and page type, since the info we need is transmitted on every page

                int eventDiff = 0;
                int timeDiff_1024 = 0;

                // Decode data page
                ushort curSpdTime_1024 = (ushort)(response.messageContents[5] + (response.messageContents[6] << 8));
                ushort curSpdEventNum = (ushort)(response.messageContents[7] + (response.messageContents[8] << 8));

                // Initialize previous values on first message received
                if (isInitialized) //Otherwise just init values below
                {
                    //Calculate Speed
                    eventDiff = (int)curSpdEventNum - (int)lastSpdEventNum;
                    if (eventDiff != 0)
                    {
                        if (eventDiff < 0) //check for rollover
                            eventDiff = 65536 + eventDiff;

                        noSpeedEventCount = 0; //reset counter

                        //Distance change
                        calculatedDistIncrease = (double)eventDiff * wheelCircumfrence_m;

                        timeDiff_1024 = (int)curSpdTime_1024 - (int)lastSpdTime_1024;
                        if (timeDiff_1024 != 0) //Ignore speed if time hasn't changed
                        {
                            if (timeDiff_1024 < 0)//check for rollover
                                timeDiff_1024 = 65536 + timeDiff_1024;

                            calculatedSpeed = calculatedDistIncrease / ((double)timeDiff_1024 / 1024); // distInc / time/1024 ticks/sec = m/s
                        }
                    }
                    else //Speed event unchanged
                    {
                        ++noSpeedEventCount;
                        if (noSpeedEventCount >= MAX_NO_EVENT_STOP_COUNT) //Are we stopped?
                        {
                            noSpeedEventCount = MAX_NO_EVENT_STOP_COUNT; //Ensure we don't roll over
                            calculatedSpeed = 0;
                        }
                        calculatedDistIncrease = 0; //If the wheel hasn't rotated, we haven't gone anywhere
                    }

                    incrementDistanceAndUpdate(calculatedDistIncrease, speedMs: calculatedSpeed);
                }
                else
                {
                    isInitialized = true;
                }

                // Update previous values
                lastSpdTime_1024 = curSpdTime_1024;
                lastSpdEventNum = curSpdEventNum;
            }
        }
    }
}
