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
using AntPlusRacer.DataSources;
namespace AntPlusRacer
{
    public class AntPlusDevMgr
    {
        private byte[] ANTPLUS_NETWORK_KEY = new byte[] 
            {
                //Insert the ANT+ network key here:
                0x, 0x, 0x, 0x, 0x, 0x, 0x, 0x
                //Distribution of source code containing the ANT+ Network Key is prohibited. 
                //You may not add the ANT+ Network Key to this source code and republish it. 
                //The ANT+ Network Key is available to ANT+ Adopters. 
                //Please refer to http://thisisant.com to become an ANT+ Adopter and access the key.
            };
        

        public class AntPlus_Connection
        {
            public enum ConnState
            {
                Closed = 0x00,
                InSrchQueue = 0x01,
                Searching = 0x02,
                Connected = 0x03,
                DrpdToSrch = 0x04                
            }

            public ANT_Channel connectedChannel = null;
            public ds_AntPlus dataSource;

            private ConnState curStatus = ConnState.Closed;

            public AntPlus_Connection(ds_AntPlus profileDecoder)
            {
                dataSource = profileDecoder;
            }

            public void setConnStatus(ConnState newStatus)
            {
                curStatus = newStatus;
            }

            public ConnState getConnStatus()
            {
                return curStatus;
            }

            public void antChannel_channelResponse_DataFetch(ANT_Response response)
            {
                if (curStatus == ConnState.Connected)
                {
                    //If the channel closes we need to know
                    if (response.responseID == (byte)ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40
                    && response.messageContents[1] == (byte)ANT_ReferenceLibrary.ANTMessageID.EVENT_0x01)
                    {
                        if (response.messageContents[2] == (byte)ANT_ReferenceLibrary.ANTEventID.EVENT_RX_FAIL_GO_TO_SEARCH_0x08)
                        {
                            dataSource.isInitialized = false;
                            curStatus = ConnState.DrpdToSrch;
                        }
                        else if (response.messageContents[2] == (byte)ANT_ReferenceLibrary.ANTEventID.EVENT_CHANNEL_CLOSED_0x07)
                        {
                            curStatus = ConnState.Closed;   //TODO: qc Ensure search channel is cycling, or we will never get this back
                        }
                    }
                }
                else if(curStatus == ConnState.DrpdToSrch)  //If we are in search and see a new broadcast
                {
                    if (response.responseID == (byte)ANT_Managed_Library.ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E)
                        curStatus = ConnState.Connected;
                }

                dataSource.handleChannelResponse(response);
            }
        }

        ANT_Device antStick = null;
        ANT_Channel searchChannel;
        int numChannelsForDevices;
        int searchingDeviceIndex = -1;

        public List<AntPlus_Connection> deviceList = new List<AntPlus_Connection>();

        public AntPlusDevMgr()
        {
            if (antStick == null)
            {
                ANT_Common.enableDebugLogs();
                findUsableAntDevice();
            }

            if (AntPlusRacerConfig.getInstance().remoteControlDevNum_negativeIsOff > 0)
            {
                numChannelsForDevices = antStick.getNumChannels() - 1;
                RacerRemoteControl.startRemoteControl(antStick.getChannel(numChannelsForDevices));
            }
            else
            {
                numChannelsForDevices = antStick.getNumChannels();
            }

            //Add all devices from config
            foreach (AntPlusRacerConfig.RacerInput i in AntPlusRacerConfig.getInstance().enabledRacerInputs)
            {
                switch (i.type)
                {
                    case AntPlusRacerConfig.RacerInputType.ant_BikeCad_UsingSpd:
                        ds_AntPlus_BikeSpd spdOnly = new ds_AntPlus_BikeSpd();
                        deviceList.Add(new AntPlus_Connection(spdOnly));
                        deviceList.Add(new AntPlus_Connection(new ds_AntPlus_BikeCad_UsingSpd(spdOnly)));
                        break;
                    case AntPlusRacerConfig.RacerInputType.ant_BikeCadAndSpd:
                        deviceList.Add(new AntPlus_Connection(new ds_AntPlus_BikeSpdCad()));
                        break;
                    case AntPlusRacerConfig.RacerInputType.ant_BikeSpd:
                        deviceList.Add(new AntPlus_Connection(new ds_AntPlus_BikeSpd()));
                        break;
                    case AntPlusRacerConfig.RacerInputType.ant_FitEqpmt:
                        deviceList.Add(new AntPlus_Connection(new ds_AntPlus_Fit()));
                        break;
                    case AntPlusRacerConfig.RacerInputType.ant_StrideSDM:
                        deviceList.Add(new AntPlus_Connection(new ds_AntPlus_StrideSDM()));
                        break;
                    default:    //Not one of the ant types, ignore
                        break;
                }

                if (i.defaultDeviceId != 0)
                    deviceList.Last().dataSource.searchProfile.deviceNumber = i.defaultDeviceId;

                if (!String.IsNullOrWhiteSpace(i.displayName))
                    deviceList.Last().dataSource.customSourceName = i.displayName;
            }

            startNextSearch();
        }

        void findUsableAntDevice()
        {
            List<ANT_Device> unusableDevices = new List<ANT_Device>();
            try
            {
                antStick = new ANT_Device();
                
                //Get new devices until we 
                //lowpri and prox search is enough now to ensure we get a device that behave as we expected
                while (antStick.getDeviceCapabilities().lowPrioritySearch == false || antStick.getDeviceCapabilities().ProximitySearch == false)
                {
                    unusableDevices.Add(antStick); //keep the last device ref so we don't see it again
                    antStick = new ANT_Device();    //this will throw an exception when there are no devices left
                }

                if (!antStick.setNetworkKey(0, ANTPLUS_NETWORK_KEY , 500))
                    throw new ApplicationException("Failed to set network key");
            }
            catch (Exception ex)
            {
                ANT_Device.shutdownDeviceInstance(ref antStick);    //Don't leave here with an invalid device ref
                throw new Exception("Could not connect to valid USB2: " + ex.Message);   //forward the exception
            }
            finally
            {
                //Release all the unusable devices
                foreach (ANT_Device i in unusableDevices)
                    i.Dispose();
            }
        }

        void startNextSearch()
        {
            if (searchChannel != null || antStick == null)  //Check if device is present and the channel is valid
            {
                //Ensure we are still connected
                try
                {
                    searchChannel.requestStatus(1000); //Check if we get an exception...means we are disconnected, otherwise continue
                }
                catch (Exception)
                {
                    try
                    {
                        //We get to this code almost always because the device is dead, so try to restart it
                        ANT_Device.shutdownDeviceInstance(ref antStick);
                        searchChannel = null;
                        foreach (AntPlus_Connection i in deviceList)
                            i.connectedChannel = null;

                        findUsableAntDevice();
                        //Now fall through and attempt to restart search
                    }
                    catch (Exception)
                    {
                        System.Windows.MessageBox.Show("Opening Device Failed. Try removing then re-inserting the stick, then try again.");
                        return;
                    }
                }
            }   //end check if device and search channel (if there is one) are valid

            //Check if we still need to search or have all the equipment already
            List<int> usedChannels = new List<int>();
            foreach (AntPlus_Connection i in deviceList)
            {
                switch (i.getConnStatus())
	            {
		            case AntPlus_Connection.ConnState.Closed:
                    case AntPlus_Connection.ConnState.Searching:
                        i.setConnStatus(AntPlus_Connection.ConnState.InSrchQueue);
                        break;
                    case AntPlus_Connection.ConnState.InSrchQueue:
                        break;
                    case AntPlus_Connection.ConnState.Connected:
                    case AntPlus_Connection.ConnState.DrpdToSrch:
                        usedChannels.Add(i.connectedChannel.getChannelNum());
                        break;
	            }                    
            }

            if (usedChannels.Count == deviceList.Count)
                return;     //we have all the equipment already

            //Get new search channel if neccesary
            if (searchChannel == null)   
            {
                if (usedChannels.Count >= numChannelsForDevices)
                    return;     //no free channels
                
                //Find the first free channel and start the search
                for (int i = 0; i < numChannelsForDevices; ++i)
                {
                    if (!usedChannels.Contains(i))
                    {
                        searchChannel = antStick.getChannel(i);
                        searchChannel.channelResponse += new dChannelResponseHandler(antChannel_channelResponse_FeSearch);
                        break;
                    }
                }
            }                

            //Search for a search period for given device parameters
            //Find the next device to search for

            while (true)  //We know there is at least one device we need to search for, because of the check above, so this will never loop infinitely
            {
                ++searchingDeviceIndex;
                if (searchingDeviceIndex >= deviceList.Count)
                    searchingDeviceIndex = 0;
                if (deviceList[searchingDeviceIndex].connectedChannel == null)
                    break;
            }

            //Now set the channel parameters to start the next search
            try
            {
                if (searchChannel == null)
                    throw new ApplicationException("Couldn't allocate a channel for search");

                ds_AntPlus.AntChannelProfile srch = deviceList[searchingDeviceIndex].dataSource.searchProfile;
                deviceList[searchingDeviceIndex].setConnStatus(AntPlus_Connection.ConnState.Searching);

                if (!searchChannel.assignChannel(ANT_ReferenceLibrary.ChannelType.BASE_Slave_Receive_0x00, 0, 500))
                {
                    //Usually because the channel is in wrong state
                    searchChannel.closeChannel(500);
                    searchChannel.unassignChannel(500);
                    if (!searchChannel.assignChannel(ANT_ReferenceLibrary.ChannelType.BASE_Slave_Receive_0x00, 0, 500))
                        throw new ApplicationException("Failed to assign channel");
                }

                //Handle setting the search timeout
                byte timeout = 4; //default 4*2.5=10 seconds for each device
                if (deviceList.Count - usedChannels.Count == 1)
                    timeout = 255;  //search forever if we only have one device to find; If one of the other devices resets it will startNextSearch again so we won't get stuck
                if (!searchChannel.setLowPrioritySearchTimeout(timeout, 500))
                    throw new ApplicationException("Failed to set low-pri search timeout");

                if (!searchChannel.setChannelSearchTimeout(0, 500))
                    throw new ApplicationException("Failed to set search timeout");

                if (!searchChannel.setChannelFreq(srch.rfOffset, 500))
                    throw new ApplicationException("Failed to set channel frequency");

                if (!searchChannel.setChannelPeriod(srch.messagePeriod, 500))
                    throw new ApplicationException("Failed to set channel period");

                if (!searchChannel.setChannelID(srch.deviceNumber, srch.pairingEnabled, srch.deviceType, srch.transType, 500))
                    throw new ApplicationException("Failed to set channel ID");

                if (!searchChannel.openChannel(500))
                    throw new ApplicationException("Failed to open channel");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Search Channel Open Failed: " + ex.Message +". If you still need to connect other fitness equipment, you may need to restart the application.");
            }
        }

        public void resetConnection(AntPlus_Connection connToReset, ushort searchForDeviceNum = 0)
        {
            lock (connToReset)
            {
                if (connToReset.connectedChannel != null)
                {
                    connToReset.connectedChannel.channelResponse -= connToReset.antChannel_channelResponse_DataFetch;
                    connToReset.connectedChannel.closeChannel(500);
                    connToReset.connectedChannel.unassignChannel(500);
                    connToReset.connectedChannel = null;
                    connToReset.dataSource.isInitialized = false;   //Invalidate the buffers
                    connToReset.dataSource.reset();
                }
                connToReset.dataSource.searchProfile.deviceNumber = searchForDeviceNum;
                connToReset.setConnStatus(AntPlus_Connection.ConnState.Closed);
            }

            startNextSearch();
        }

        void antChannel_channelResponse_FeSearch(ANT_Response response)
        {
            switch ((ANT_ReferenceLibrary.ANTMessageID)response.responseID)
            {
                case ANT_ReferenceLibrary.ANTMessageID.BROADCAST_DATA_0x4E:
                    AntPlus_Connection newConn = deviceList[searchingDeviceIndex];
                    ANT_Response idResp = null;
                    try
                    {
                        idResp = antStick.requestMessageAndResponse(searchChannel.getChannelNum(), ANT_ReferenceLibrary.RequestMessageID.CHANNEL_ID_0x51, 1000);
                    }
                    catch (Exception)
                    {
                        //Don't know what to do if we can't get id...could retry somewhere...
                        break;
                    }
                    

                    lock (newConn)
                    {
                        searchChannel.channelResponse -= antChannel_channelResponse_FeSearch;
                        newConn.dataSource.searchProfile.deviceNumber = (ushort)(idResp.messageContents[1] + ((ushort)idResp.messageContents[2] << 8)); //Save to the search profile so we keep this id after dropouts
                        newConn.connectedChannel = searchChannel;
                        //Note: the low pri search happens before the high pri, so this isn't even doing anything. 
                        //newConn.connectedChannel.setChannelSearchTimeout(2, 500); //If we drop, we really want it back soon because it may be in use in a race, but we can't afford to ruin other channels staying in high priority. With the default search waveform, 5s is supposed to give us a really good rate of acquisition
                        newConn.connectedChannel.setLowPrioritySearchTimeout(255, 500); //Search indefinitely 
                        newConn.antChannel_channelResponse_DataFetch(response);
                        newConn.connectedChannel.channelResponse += newConn.antChannel_channelResponse_DataFetch;
                        newConn.setConnStatus(AntPlus_Connection.ConnState.Connected);
                    }

                    searchChannel = null;   //invalidate this channel as a search channel
                    startNextSearch();
                    break;

                case ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40:
                    if (response.messageContents[1] == (byte)ANT_ReferenceLibrary.ANTMessageID.EVENT_0x01)
                    {
                        //if(response.messageContents[2] == 0x01)  //Search timeout causes close channel, so wait for that
                        if (response.messageContents[2] == (byte)ANT_ReferenceLibrary.ANTEventID.EVENT_CHANNEL_CLOSED_0x07) //Closed channel 
                            startNextSearch();
                    }
                    break;
            }
        }
    }
}
