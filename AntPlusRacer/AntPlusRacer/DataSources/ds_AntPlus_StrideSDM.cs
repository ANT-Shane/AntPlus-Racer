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
    class ds_AntPlus_StrideSDM : ds_AntPlus_RxFailInterpBuf
    {
        bool mIsInitialized = false;

        double lastCadence = -1;
        int ucPreviousStrideCount;
        int usPreviousDistance16;

        int ulAcumStrideCount = 0;

        public ds_AntPlus_StrideSDM()
            : base("Ant+ StrideSDM", racerSportType.Running)
        {
        }

        protected override AntChannelProfile getDefaultSearchProfile()
        {
            return new AntChannelProfile()
            {
                rfOffset = 57,
                transType = 0,
                deviceType = 124,
                deviceNumber = 0,
                messagePeriod = 8134,
                pairingEnabled = false,
            };
        }

        protected override void handleNonRxFailChannelResponse(ANT_Managed_Library.ANT_Response response)
        {
            if (response.responseID == (byte)ANT_Managed_Library.ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E)
            {
                int usDistance16;
                int usCadence;
                int usSpeed256;
                int ucStrideCount;

                switch (response.messageContents[1])   //Switch on pageNumber
                {
                    case 1: //Main page
                        usDistance16 = 0xFF0 & (response.messageContents[4] << 4);        // Distance, integer portion, in 1/16 seconds
                        usDistance16 += 0x00F & ((response.messageContents[5] & 0xF0) >> 4);  // Distance, fractional part, in 1/16 seconds
                        usSpeed256 = 0xF00 & ((response.messageContents[5] & 0x0F) << 8); // Speed, in 1/256 seconds
                        usSpeed256 += 0x0FF & (response.messageContents[6]);              // Speed, fractional part, in 1/256 seconds
                        ucStrideCount = 0xFF & (response.messageContents[7]);            // Stride count
                        if (!mIsInitialized)
                        {   // Initialize previous values for calculation of cumulative values
                            ucPreviousStrideCount = ucStrideCount;
                            usPreviousDistance16 = usDistance16;
                            mIsInitialized = true;
                        }
                        else if (ucStrideCount != ucPreviousStrideCount)  // Update data if dealing with a new event
                        {
                            // Update cumulative stride count
                            if (ucStrideCount > ucPreviousStrideCount)
                                ulAcumStrideCount += (ucStrideCount - ucPreviousStrideCount);
                            else
                                ulAcumStrideCount += 0xFF & (0xFF - ucPreviousStrideCount + ucStrideCount + 1);


                            // Determine increase in distance
                            double distIncrease;
                            if (usDistance16 > usPreviousDistance16)
                                distIncrease = (double)(usDistance16 - usPreviousDistance16) / 16;
                            else
                                distIncrease = (double)((0xFFF - usPreviousDistance16 + usDistance16 + 1) & 0x0FFF) / 16;


                            ucPreviousStrideCount = ucStrideCount;
                            usPreviousDistance16 = usDistance16;

                            incrementDistanceAndUpdate(distIncrease, (double)usSpeed256 / 256, lastCadence);
                        }

                        break;
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                    case 14:
                    case 15:    // Intentional fall thru (pages 2 - 15 should have bytes 3,4,5, and 7 of the template on Page 2).
                        usCadence = (response.messageContents[4] << 4);				// Cadence (1/16 seconds)
                        usCadence += (response.messageContents[5] & 0xF0) >> 4;
                        usSpeed256 = 0xF00 & ((response.messageContents[5] & 0x0F) << 8); // Speed, in 1/256 seconds
                        usSpeed256 += 0x0FF & (response.messageContents[6]);              // Speed, fractional part, in 1/256 seconds

                        //Use cadence value if it is valid
                        if (usCadence == 0)
                            lastCadence = -1;
                        else
                            lastCadence = (double)usCadence / 16;
                        break;

                    default:
                        return;
                }
            }
        }
    }
}
