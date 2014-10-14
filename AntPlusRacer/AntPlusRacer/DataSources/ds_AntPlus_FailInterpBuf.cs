/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ANT_Managed_Library;

namespace AntPlusRacer.DataSources
{
    public abstract class ds_AntPlus_RxFailInterpBuf : ds_AntPlus
    {
        double interpolatedDistanceDiff = 0;
        double averageSpeed = 0;
        DateTime lastEventTime;

        public ds_AntPlus_RxFailInterpBuf(String sourceName, racerSportType sportType)
            :base(sourceName, sportType)
        {
        }

        //Override base class to manage buffering
        new public void incrementDistanceAndUpdate(double distToAdd, double speedMs, double cadence = -1, byte heartRate = 0xFF, ushort powerW = 0xFFFF)
        {
            //NOTE!! This assumes the distToAdd field is calculated from an accumulated value and will include all distance since the last non-rx fail message
            //If we have been interpolating, synch the results
            if (interpolatedDistanceDiff > 0)
            {
                double correctedDist = distToAdd - interpolatedDistanceDiff;
                interpolatedDistanceDiff = 0;   //We are done with this difference

                //If we were correct or under-interpolated we will add whatever is left and we are caught up 
                //but If we over-interpolated, too bad, don't subtract, just keep going from here
                if (correctedDist < 0) 
                    correctedDist = 0; 

                //now forward corrected value to base class
                lastEventTime = DateTime.Now;
                base.incrementDistanceAndUpdate(correctedDist, speedMs, cadence, heartRate, powerW);
            }
            else //Nothing to correct, just forward
            {
                averageSpeed = speedMs; //Use last speed instead of average so it doesn't jump around every drop out
                //if (speedMs == 0)   //If we get a zero, we are stopped, so immediately zero the average
                //    averageSpeed = 0;
                //else
                //    averageSpeed = (averageSpeed * 3 + speedMs) / 4;
                Console.WriteLine("spdavg: averageSpeed: " + averageSpeed);

                lastEventTime = DateTime.Now;
                base.incrementDistanceAndUpdate(distToAdd, speedMs, cadence, heartRate, powerW);
            }            
        }

        public override void reset()
        {
            interpolatedDistanceDiff = 0;
            averageSpeed = 0;
            base.reset();
        }

        public sealed override void handleChannelResponse(ANT_Response response)
        {
            //Watch for collisions or failures, if we miss a message, interpolate from the last known speed
            if (response.responseID == (byte)ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40
                && response.messageContents[1] == (byte)ANT_ReferenceLibrary.ANTMessageID.EVENT_0x01
                && (response.messageContents[2] == (byte)ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_0x02
                    || response.messageContents[2] == (byte)ANT_ReferenceLibrary.ANTEventID.EVENT_CHANNEL_COLLISION_0x09))
            {
                DataSourcePacket lastData = getLastDataRcvd();
                if (lastData != null)
                {
                    DateTime curEventTime = DateTime.Now;
                    double timeMissed_s = curEventTime.Subtract(lastEventTime).TotalSeconds;
                    double distMissed_m = timeMissed_s * averageSpeed; //Use the recent average speed so we get a closer estimate
                    interpolatedDistanceDiff += distMissed_m;

                    lastEventTime = curEventTime;
                    base.incrementDistanceAndUpdate(distMissed_m, averageSpeed, lastData.cadence, lastData.heartRate, lastData.power);

                    ////DEBUG
                    //System.Console.Out.WriteLine((ANT_ReferenceLibrary.ANTEventID)response.messageContents[2] + " interpolated " + distMissed_m);
                }
            }
            if (response.messageContents[2] == (byte)ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_GO_TO_SEARCH_0x08)
            {
                //Sorry, if we drop to search there is not much we can do to recover, let the race display know we are dead
                base.incrementDistanceAndUpdate(0, 0, 0, 0, 0);
            }
            else //If it is not an rx failure, let the derived class handle it
            {
                //Don't worry about isInitialized=false on rx_drop_to_search here, because we hope the broadcast
                //comes back soon enough for it to not matter, if we have to wait long enough for it to matter, 
                //we have waited too long anyway

                ////DEBUG
                //if (response.responseID == (byte)ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40)
                //    System.Console.Out.WriteLine((ANT_ReferenceLibrary.ANTEventID)response.messageContents[2]);
                handleNonRxFailChannelResponse(response);
            }
        }

        protected abstract void handleNonRxFailChannelResponse(ANT_Response response);

    }
}
