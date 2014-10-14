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
using System.Windows.Controls;

namespace AntPlusRacer
{
    class RacerRemoteControl
    {
        static RacerRemoteControl instance;
        static Label statusReportLabel = new Label() { Content = "Remote Control Channel has not been started" };

        enum RemoteControlMsgId:byte
        {
            RacerCmdId = 0x32,
            RequestDataSourceList = 0x50,
            RaceControl = 0x10,
            ScreenDisplayControl = 0x01,
        }

        enum RemoteControlReturnCodes:byte
        {
            Success = 0x00,
            Error_CmdFormatWrong = 0x01,
            Error_RacersNotConfigured = 0x10,
            Error_RaceNotRunning = 0x11,
            Error_PanelDoesntExist = 0x12,
            Error_DataSourceInUse = 0x14,
            Error_RacerDoesntExist = 0x15,
            Error_PanelInWrongState = 0x16,
            Error_DataSourceDoesntExist = 0x17,
            Error_DataSourceNotConnected = 0x18,
            Error_MaxPanelLimitReached = 0x19,
            Error_ClosingLastPanel = 0x20
        }
        
        ANT_Channel channel;        

        String statusReport
        {
            set
            {
                statusReportLabel.Dispatcher.Invoke(new Action(() => statusReportLabel.Content = String.Format("Remote: d#{0} - {1}", deviceNum, value)));
            }
        }
        //string lastCmd = "";
        int deviceNum = AntPlusRacerConfig.getInstance().remoteControlDevNum_negativeIsOff;

        byte[] lastMsgBytes;
        
        static public void startRemoteControl(ANT_Channel channel)
        {
            if (instance != null)
            {
                instance.stopChannel();
            }
            instance = new RacerRemoteControl(channel);
        }

        static public Label getRemoteStatus()
        {
            return statusReportLabel;
        }

        private RacerRemoteControl(ANT_Channel channel)
        {
            this.channel = channel;
            startChannel();
            channel.channelResponse += new dChannelResponseHandler(channel_channelResponse);
        }

        private void startChannel()
        {
            //Just try this in case we get an active channel for some reason
            channel.closeChannel(500);
            channel.unassignChannel(500);

            if (!channel.assignChannel(ANT_ReferenceLibrary.ChannelType.BASE_Master_Transmit_0x10, 0, 500))
                statusReport = "Failed to start, assign() failed";
            else if (!channel.setChannelID((ushort)deviceNum, false, 9, 5, 500))
                statusReport = "Failed to start, setChannelId() failed";
            else if (!channel.setChannelFreq(57, 500))
                statusReport = "Failed to start, setChannelFreq() failed";
            else if (!channel.setChannelPeriod(32768, 500))
                statusReport = "Failed to start, setChannelPeriod() failed";
            else if (!channel.setChannelTransmitPower(ANT_ReferenceLibrary.TransmitPower.RADIO_TX_POWER_0DB_0x03, 500))
                statusReport = "Failed to start, setChannelTransmitPower() failed";
            else if (!channel.openChannel(500))
                statusReport = "Failed to start, openChannel() failed";
            else
                statusReport = "Remote Control Channel is open";
        }

        private void stopChannel()
        {
            channel.closeChannel();
            channel.channelResponse -= channel_channelResponse;
            statusReport = "Remote Control Channel has been closed";
        }

        void channel_channelResponse(ANT_Response response)
        {
            switch ((ANT_ReferenceLibrary.ANTMessageID)response.responseID)
            {
                case ANT_ReferenceLibrary.ANTMessageID.ACKNOWLEDGED_DATA_0x4F:
                    statusReport = String.Format("Open - Last Activity = {0}:{1}", DateTime.Now.ToLongTimeString(), "Rcvd Ack");
                    lastMsgBytes = response.messageContents.Skip(1).ToArray();
                    decodeCmd(lastMsgBytes);
                    break;
                case ANT_ReferenceLibrary.ANTMessageID.BURST_DATA_0x50:
                    statusReport = String.Format("Open - Last Activity = {0}:{1}", DateTime.Now.ToLongTimeString(), "Rcvd Burst");
                    if ((response.messageContents[0] & 0xE0) == 0)  //first packet
                    {
                        lastMsgBytes = response.messageContents.Skip(1).ToArray();
                    }
                    else
                    {
                        lastMsgBytes = lastMsgBytes.Concat(response.messageContents.Skip(1)).ToArray();
                        if((response.messageContents[0] & 0x80) == 0x80) //last packet
                            decodeCmd(lastMsgBytes);
                    }
                    break;
                case ANT_ReferenceLibrary.ANTMessageID.RESPONSE_EVENT_0x40:
                    //Print all messages except EVENT_TX to status
                    if ((ANT_ReferenceLibrary.ANTEventID)response.messageContents[2] != ANT_ReferenceLibrary.ANTEventID.EVENT_TX_0x03)
                        statusReport = String.Format("Open - Last Activity = {0}:{1}", DateTime.Now.ToLongTimeString(), (ANT_ReferenceLibrary.ANTEventID)response.messageContents[2]);
                    break;

                default:
                    statusReport = String.Format("Open - Last Activity = {0}:{1}", DateTime.Now.ToLongTimeString(), (ANT_ReferenceLibrary.ANTMessageID)response.responseID);
                    break;
            }
        }

        private void decodeCmd(byte[] lastMsgBytes)
        {
            if (lastMsgBytes[0] == (byte)RemoteControlMsgId.RacerCmdId) //Racer CMD ID
            {
                switch ((RemoteControlMsgId)lastMsgBytes[1])
                {
                    case RemoteControlMsgId.RequestDataSourceList:
                        {
                            List<byte> response = new List<byte> { (byte)AntConfigPanel.accessInstance().antMgr.deviceList.Count };
                            foreach (AntPlusDevMgr.AntPlus_Connection i in AntConfigPanel.accessInstance().antMgr.deviceList)
                            {
                                response.AddRange(new byte[]{i.dataSource.uid, 
                                                    (byte)i.dataSource.getSportType(), 
                                                    (byte)i.getConnStatus(),
                                                    (byte)(i.dataSource.searchProfile.deviceNumber & 0x00FF),
                                                    (byte)((i.dataSource.searchProfile.deviceNumber & 0xFF00) >> 8),
                                                    i.dataSource.searchProfile.transType,
                                                    i.dataSource.searchProfile.deviceType,
                                                    Convert.ToByte(i.dataSource.isInUse),
                                                    (byte)i.dataSource.getSourceName().Length,
                                                });
                                response.AddRange(UTF8Encoding.ASCII.GetBytes(i.dataSource.getSourceName()));
                            }
                            sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes, response);
                            return;
                        }
                    case RemoteControlMsgId.RaceControl:
                        {
                            RacePanel selectedPanel = MainWindow.getInstance().getRacePanel(lastMsgBytes[2]);
                            if (selectedPanel == null)
                            {
                                sendResponse(RemoteControlReturnCodes.Error_PanelDoesntExist, lastMsgBytes);
                                return;
                            }
                            else
                            {
                                selectedPanel.Dispatcher.Invoke((Action<RacePanel, byte[]>)decodeRaceControlCmd, selectedPanel, lastMsgBytes);
                                return;
                            }
                        }
                    case RemoteControlMsgId.ScreenDisplayControl:
                        {
                            RacePanel selectedPanel;
                            if (lastMsgBytes[2] == 0xFF)    //Create new panel
                            {
                                MainWindow wdw = MainWindow.getInstance();
                                selectedPanel = (RacePanel)wdw.Dispatcher.Invoke((Func<RacePanel>)wdw.addRacePanel);
                                if (selectedPanel == null)
                                {
                                    sendResponse(RemoteControlReturnCodes.Error_MaxPanelLimitReached, lastMsgBytes);
                                    return;
                                }
                            }
                            else
                            {
                                selectedPanel = MainWindow.getInstance().getRacePanel(lastMsgBytes[2]);
                                if (selectedPanel == null)
                                {
                                    sendResponse(RemoteControlReturnCodes.Error_PanelDoesntExist, lastMsgBytes);
                                    return;
                                }
                            }

                            switch (lastMsgBytes[3])
                            {
                                case 0: //Show Config
                                    selectedPanel.Dispatcher.Invoke((Action)selectedPanel.showAntConfig);
                                    break;
                                case 1: //New Race
                                    selectedPanel.Dispatcher.Invoke((Action)selectedPanel.showRaces);
                                    break;
                                case 2: //Record Display
                                    selectedPanel.Dispatcher.Invoke((Action)selectedPanel.showRecords);
                                    break;
                                case 3:
                                    bool ret = (bool)selectedPanel.Dispatcher.Invoke((Func<bool>)selectedPanel.closePanel);
                                    if (!ret)
                                    {
                                        sendResponse(RemoteControlReturnCodes.Error_ClosingLastPanel, lastMsgBytes);
                                        return;
                                    }
                                    break;
                                default:
                                    sendResponse(RemoteControlReturnCodes.Error_CmdFormatWrong, lastMsgBytes);
                                    return;
                            }

                            selectedPanel.Dispatcher.Invoke((Action)selectedPanel.hideMenu);
                            sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes);
                            return;
                        }
                    default:
                        sendResponse(RemoteControlReturnCodes.Error_CmdFormatWrong, lastMsgBytes);
                        return;
                }

            }
            else //device is not a racer cmd
            {
                sendResponse(RemoteControlReturnCodes.Error_CmdFormatWrong, lastMsgBytes);
                return;
            }
        }

        private void decodeRaceControlCmd(RacePanel selectedPanel, byte[] lastMsgBytes)
        {
            switch (lastMsgBytes[3])
            {
                case 0: //start
                    {
                        if (selectedPanel.PanelState != RacePanel.panelState.RaceFactory)
                        {
                            sendResponse(RemoteControlReturnCodes.Error_PanelInWrongState, lastMsgBytes);
                            return;
                        }

                        RaceFactoryPanel factoryPanel = selectedPanel.Grid_Content.Children[0] as RaceFactoryPanel;
                        if (!factoryPanel.startRace())
                        {
                            sendResponse(RemoteControlReturnCodes.Error_RacersNotConfigured, lastMsgBytes);
                            return;
                        }
                        sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes);
                        return;
                    }
                case 1: //restart
                    {
                        if (selectedPanel.PanelState != RacePanel.panelState.Racing)
                        {
                            sendResponse(RemoteControlReturnCodes.Error_PanelInWrongState, lastMsgBytes);
                            return;
                        }

                        selectedPanel.raceConfig.disposeRace();
                        selectedPanel.raceConfig = new RaceDetails(selectedPanel.raceConfig.racerDetails, selectedPanel.raceConfig.target_type);
                        selectedPanel.raceConfig.startRace(selectedPanel);
                        sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes);
                        return;
                    }
                case 2: //cancel race
                    {
                        if (selectedPanel.PanelState != RacePanel.panelState.RaceFactory
                            && selectedPanel.PanelState != RacePanel.panelState.Racing
                            && selectedPanel.PanelState != RacePanel.panelState.PostRaceDisplay
                            && selectedPanel.PanelState != RacePanel.panelState.PostRaceResults)
                        {
                            sendResponse(RemoteControlReturnCodes.Error_PanelInWrongState, lastMsgBytes);
                            return;
                        }

                        selectedPanel.showRaceFactory();
                        sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes);
                        return;
                    }
                case 3: //get current racer list
                    {
                        if (selectedPanel.PanelState != RacePanel.panelState.RaceFactory
                            && selectedPanel.PanelState != RacePanel.panelState.Racing
                            && selectedPanel.PanelState != RacePanel.panelState.PostRaceDisplay
                            && selectedPanel.PanelState != RacePanel.panelState.PostRaceResults)
                        {
                            sendResponse(RemoteControlReturnCodes.Error_PanelInWrongState, lastMsgBytes);
                            return;
                        }


                        List<RacerDetails> racerList;
                        if (selectedPanel.PanelState == RacePanel.panelState.RaceFactory)
                            racerList = ((RaceFactoryPanel)selectedPanel.Grid_Content.Children[0]).configuredRacers;
                        else
                            racerList = selectedPanel.raceConfig.racerDetails;


                        List<byte> retMsg = new List<byte> { (byte)racerList.Count };
                        for (byte i = 0; i < racerList.Count; ++i)
                        {
                            RacerDetails racer = racerList[i];
                            retMsg.Add(i);
                            retMsg.Add(racer.dataSource.uid);
                            retMsg.Add((byte)((ushort)racer.targetValue & 0xFF));
                            retMsg.Add((byte)(((ushort)racer.targetValue & 0xFF00) >> 8));
                            ushort curDistance = (ushort)racer.dataSource.getCurrentDistance();
                            retMsg.Add((byte)((ushort)curDistance & 0xFF));
                            retMsg.Add((byte)(((ushort)curDistance & 0xFF00) >> 8));
                            if (racer.racerRecordInfo != null)
                            {
                                if (!String.IsNullOrWhiteSpace(racer.racerRecordInfo.FirstName))
                                {
                                    retMsg.Add(1);
                                    retMsg.Add((byte)racer.racerRecordInfo.FirstName.Length);
                                    retMsg.AddRange(UTF8Encoding.ASCII.GetBytes(racer.racerRecordInfo.FirstName));
                                }
                                if (!String.IsNullOrWhiteSpace(racer.racerRecordInfo.LastName))
                                {
                                    retMsg.Add(2);
                                    retMsg.Add((byte)racer.racerRecordInfo.LastName.Length);
                                    retMsg.AddRange(UTF8Encoding.ASCII.GetBytes(racer.racerRecordInfo.LastName));
                                }
                                if (!String.IsNullOrWhiteSpace(racer.racerRecordInfo.PhoneNumber))
                                {
                                    retMsg.Add(3);
                                    retMsg.Add((byte)racer.racerRecordInfo.PhoneNumber.Length);
                                    retMsg.AddRange(UTF8Encoding.ASCII.GetBytes(racer.racerRecordInfo.PhoneNumber));
                                }
                                if (!String.IsNullOrWhiteSpace(racer.racerRecordInfo.Email))
                                {
                                    retMsg.Add(4);
                                    retMsg.Add((byte)racer.racerRecordInfo.Email.Length);
                                    retMsg.AddRange(UTF8Encoding.ASCII.GetBytes(racer.racerRecordInfo.Email));
                                }
                            }
                            retMsg.Add(0);  //Null term options list
                        }
                        sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes, retMsg);
                        return;

                    }
                case 4: //Config race
                    {
                        if (selectedPanel.PanelState != RacePanel.panelState.RaceFactory)
                        {
                            if (selectedPanel.PanelState == RacePanel.panelState.ScreenSaverRecordDisplay)
                            {
                                selectedPanel.Dispatcher.Invoke((Action)selectedPanel.showRaceFactory);
                            }
                            else
                            {
                                sendResponse(RemoteControlReturnCodes.Error_PanelInWrongState, lastMsgBytes);
                                return;
                            }
                        }

                        selectedPanel.ss_lastActivity = DateTime.Now.Add(new TimeSpan(0, 0, 5));    //Give ourselves 5 seconds of extra time before screensaver to process ant messages
                        RaceFactoryPanel factoryPanel = selectedPanel.Grid_Content.Children[0] as RaceFactoryPanel;

                        if (lastMsgBytes[5] == 0xFF)
                        {
                            if (factoryPanel.removeRacer(lastMsgBytes[4]))
                            {
                                sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes);
                                return;
                            }
                            else
                            {
                                sendResponse(RemoteControlReturnCodes.Error_RacerDoesntExist, lastMsgBytes);
                                return;
                            }
                        }

                        byte sourceID = lastMsgBytes[5];

                        DataSources.DataSourceBase dataSrc = null;
                        foreach (AntPlusDevMgr.AntPlus_Connection i in AntConfigPanel.accessInstance().antMgr.deviceList)
                        {
                            if (i.dataSource.uid == sourceID)
                            {
                                dataSrc = i.dataSource;
                                if (i.getConnStatus() != AntPlusDevMgr.AntPlus_Connection.ConnState.Connected)
                                {
                                    sendResponse(RemoteControlReturnCodes.Error_DataSourceNotConnected, lastMsgBytes);
                                    return;
                                }
                                break;
                            }
                        }
                        if (dataSrc == null)
                        {
                            sendResponse(RemoteControlReturnCodes.Error_DataSourceDoesntExist, lastMsgBytes);
                            return;
                        }

                        double trackDist = 0;
                        //Get the default (first) track for the given sport type
                        foreach (AntPlusRacerConfig.RaceTrack i in AntPlusRacerConfig.getInstance().enabledRaceTracks)
                        {
                            if (i.sportType == dataSrc.getSportType())
                            {
                                trackDist = i.distance;
                                break;
                            }
                        }

                        //decode options
                        string firstName = null;
                        string lastName = null;
                        string phoneNum = null;
                        string emailAdr = null;
                        int curPos = 6;
                        while (curPos < lastMsgBytes.Length && lastMsgBytes[curPos] != 0) //0 is the end of list marker
                        {
                            int option = lastMsgBytes[curPos];
                            switch (option)
                            {
                                case 1:
                                case 2:
                                case 3:
                                case 4:
                                    int strLength = lastMsgBytes[curPos + 1];
                                    string str = UTF8Encoding.ASCII.GetString(lastMsgBytes.Skip(curPos + 2).Take(strLength).ToArray());
                                    switch (option)
                                    {
                                        case 1:
                                            firstName = str;
                                            break;
                                        case 2:
                                            lastName = str;
                                            break;
                                        case 3:
                                            phoneNum = str;
                                            break;
                                        case 4:
                                            emailAdr = str;
                                            break;
                                    }
                                    curPos += 2 + strLength;
                                    break;
                                default:
                                    sendResponse(RemoteControlReturnCodes.Error_CmdFormatWrong, lastMsgBytes);
                                    return;
                            }
                        }

                        if (curPos >= lastMsgBytes.Length)  //If we ended without hitting the end of the list
                        {
                            sendResponse(RemoteControlReturnCodes.Error_CmdFormatWrong, lastMsgBytes);
                            return;
                        }

                        //Finally create/change the racer
                        int ret = factoryPanel.configureRacer(lastMsgBytes[4], dataSrc, trackDist, firstName, lastName, phoneNum, emailAdr);

                        if (ret == 0)
                            sendResponse(RemoteControlReturnCodes.Success, lastMsgBytes);
                        else if (ret == -1)
                            sendResponse(RemoteControlReturnCodes.Error_DataSourceInUse, lastMsgBytes);
                        else if (ret == -2)
                            sendResponse(RemoteControlReturnCodes.Error_RacerDoesntExist, lastMsgBytes);
                        return;
                    }
                default:
                    sendResponse(RemoteControlReturnCodes.Error_CmdFormatWrong, lastMsgBytes);
                    return;
            }
        }

        private void sendResponse(RemoteControlReturnCodes remoteControlReturnCode, byte[] origCmd, IEnumerable<byte> retMsg = null)
        {
            byte[] msgToSend = new byte[] { origCmd[0], origCmd[1], (byte)remoteControlReturnCode };

            if (retMsg != null)
                msgToSend = msgToSend.Concat(retMsg).ToArray();

            //Just always send a burst
            //retry up to 10 times
            for (int i = 0; i < 10; ++i)
            {
                ANT_ReferenceLibrary.MessagingReturnCode ret = channel.sendBurstTransfer(msgToSend, (uint)msgToSend.Count() + 2000);
                if (ret == ANT_ReferenceLibrary.MessagingReturnCode.Pass)
                    return;
            }
            //System.Diagnostics.Debug.Fail("Sending response failed");
        }
    }
}
