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
    public class ds_AntPlus_Fit : ds_AntPlus_RxFailInterpBuf
    {
        public const int MAX_NO_EVENT_STOP_COUNT = 12;

        byte lastElapsedTimeAcc;
        byte lastDistAcc;
        double calcSpeed = 0; //Averaged Speed over several msgs in case instSpeed is not available

        int lastInstCadence = -1;
        ushort lastInstPower = 0xFFFF;

        byte idleCount;

        public ds_AntPlus_Fit()
            :base("Ant+ FIT", racerSportType.Unknown)
        {
        }

        protected override AntChannelProfile getDefaultSearchProfile()
        {
            return new AntChannelProfile()
            {
                rfOffset = 57,
                transType = 0,
                deviceType = 17,
                deviceNumber = 0,
                messagePeriod = 8192,
                pairingEnabled = false,
            };
        }

        protected override void handleNonRxFailChannelResponse(ANT_Managed_Library.ANT_Response response)
        {
            if (response.responseID == (byte)ANT_Managed_Library.ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E)
            {
                switch (response.messageContents[1])
                {
                    case 16:   //Page 16 - General Page
                        {
                            //Get new values

                            byte curElapsedTimeAcc = response.messageContents[3];
                            byte curDistAcc = response.messageContents[4];
                            ushort curInstSpeed = (ushort)(response.messageContents[5] + ((ushort)response.messageContents[6] << 8));
                            //heartRate = response.messageContents[7],

                            //If we aren't initialized we can't do the calculations yet and we just save the values below
                            if (isInitialized) 
                            {
                                //Get the distance increase out of the accumulated field
                                int distIncrease = curDistAcc - lastDistAcc;
                                if (distIncrease < 0)
                                    distIncrease = 255 + distIncrease;

                                if (distIncrease == 0)  //Some devices like the rower don't reset the speed value when the rowing stops
                                {
                                    if (++idleCount >= MAX_NO_EVENT_STOP_COUNT)    //depending on page setup of machine, this is at least 1.5s, max 3s
                                    {
                                        curInstSpeed = 0;
                                        idleCount = MAX_NO_EVENT_STOP_COUNT;    //Always set, so we don't rollover
                                    }
                                }
                                else
                                {
                                    idleCount = 0;
                                }

                                //System.Console.Out.WriteLine(feDataSource.type + ", " + curData.distAcc + ", " + distIncrease);

                                //TODO: qc Keep checking if this is the same device as original connection?
                                //if((FitEquipType)(response.messageContents[2] & 0x1F) != feDataSource.type)

                                if (curInstSpeed == 0xFFFF) //If instantaneous speed is invalid, we have to manually calculate it
                                {
                                    //Average speed over last 16 msgs
                                    //System.Console.Write(String.Format("Pre Speed = {0}, ", calcSpeed));
                                    double calcTimeDiff = curElapsedTimeAcc - lastElapsedTimeAcc;
                                    
                                    if (calcTimeDiff > 0)   //If there is no time diff, it is probably because the broadcast data wasn't updated, so don't try and predict speed from it
                                    {
                                        calcSpeed = (calcSpeed * 7 / 8);
                                        calcSpeed += (((double)distIncrease * 4) / calcTimeDiff) / 8; //Add the fractional contributed by this msg. Time is in 0.25s units, so multiply dist by 4
                                    }                                  

                                    incrementDistanceAndUpdate(distIncrease, calcSpeed, lastInstCadence, powerW: lastInstPower);
                                }
                                else
                                {
                                    incrementDistanceAndUpdate(distIncrease, ((double)curInstSpeed) / 1000, lastInstCadence, powerW: lastInstPower);
                                }
                            }
                            else //When we are initializing we determine the sport type
                            {
                                switch (response.messageContents[2] & 0x1F)
                                {
                                    case 19: //Treadmill
                                        sportType = racerSportType.Running;
                                        break;
                                    case 22:    //Rower
                                        sportType = racerSportType.Rowing;
                                        break;
                                    case 21:    //Biking
                                        sportType = racerSportType.Biking;
                                        break;
                                    case 20:    //Elliptical
                                        sportType = racerSportType.Running;
                                        break;
                                    case 23:    //Climber
                                        sportType = racerSportType.Running;
                                        break;
                                    case 24:    //Nordic Ski machine
                                        sportType = racerSportType.Skiing;
                                        break;
                                    default:
                                        sportType = racerSportType.Unknown;
                                        break;
                                }

                                isInitialized = true;
                            }

                            //Save for next time
                            lastElapsedTimeAcc = curElapsedTimeAcc;
                            lastDistAcc = curDistAcc;                            
                        }
                        break;
                    //case 17: //general settings Page, not on bike, shows on rower, but not very useful
                    //    {
                    //        System.Console.Out.WriteLine("Cycle Length:" + response.messageContents[4] + ",Incline:" + (response.messageContents[5] + (response.messageContents[5] << 8)) + ",Resist:" + response.messageContents[7]);
                    //    }
                    //    break;
                    //case 18: //metabolic data, doesn't show on rower or bike
                    //    {
                    //        System.Console.Out.WriteLine("BurnRate:" + (response.messageContents[5] + (response.messageContents[6] << 8)) + ",Cal:" + response.messageContents[7]);
                    //    }
                    //    break;
                    case 19: //treadmill data
                        {
                            lastInstCadence = response.messageContents[5];
                        }
                        break;
                    case 21: //bike data
                    case 22: //Row data, same format as bike for cad and power
                    case 20: //Elliptical, same format as bike for cad and power
                    case 24: //Nordic Skier, same format as bike for cad and power
                        {
                            lastInstCadence = response.messageContents[5];
                            lastInstPower = (ushort)(response.messageContents[6] + (response.messageContents[7] << 8));
                            //System.Console.Out.WriteLine("Cadence:" + response.messageContents[5] + ",Power:" + (response.messageContents[6] + (response.messageContents[7] << 8)));
                            //System.Console.Out.WriteLine("SPM:"+response.messageContents[5]+"Power:"+(response.messageContents[6] + (response.messageContents[7] << 8)));
                        }
                        break;
                }
            }
        }
    }
}