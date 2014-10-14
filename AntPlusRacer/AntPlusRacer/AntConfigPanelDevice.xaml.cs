/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using AntPlusRacer.DataSources;

namespace AntPlusRacer
{
    /// <summary>
    /// Interaction logic for Fit_ConfigPanelDevice.xaml
    /// </summary>
    public partial class Fit_ConfigPanelDevice : UserControl
    {
        private AntPlusDevMgr.AntPlus_Connection antPlusConnection;

        public Fit_ConfigPanelDevice(AntPlusDevMgr.AntPlus_Connection antPlusConnection)
        {
            InitializeComponent();
            this.antPlusConnection = antPlusConnection;
            TextBox_SearchDeviceId.Text = antPlusConnection.dataSource.searchProfile.deviceNumber.ToString();
            Label_Description.Content = antPlusConnection.dataSource.getSourceName();
            Label_Description2.Content = antPlusConnection.dataSource.getSportType();
        }

        public void refreshView()
        {
            Dispatcher.Invoke(new Action(() =>
            {
                DataSourcePacket lastSeenData = antPlusConnection.dataSource.getLastDataRcvd();
                Label_Description2.Content = antPlusConnection.dataSource.getSportType();
                Label_DevID.Content = antPlusConnection.dataSource.searchProfile.deviceNumber;
                Label_Status.Content = antPlusConnection.getConnStatus().ToString();
                if (lastSeenData == null)
                {
                    Label_Dist.Content = "---";
                    Label_Speed.Content = "---";
                    Label_Cadence.Content = "---";
                }
                else
                {
                    Label_Dist.Content = Math.Round(lastSeenData.distance).ToString();
                    Label_Speed.Content = Math.Round(lastSeenData.speed_ms, 2).ToString();
                    Label_Cadence.Content = Math.Round(lastSeenData.cadence).ToString();
                }
            }));
        }

        private void Button_Reset_Click(object sender, RoutedEventArgs e)
        {
            ushort searchForDeviceNum;
            try
            {
                searchForDeviceNum = ushort.Parse(TextBox_SearchDeviceId.Text);
                AntConfigPanel.accessInstance().antMgr.resetConnection(antPlusConnection, searchForDeviceNum);
            }
            catch (Exception)
            {
                TextBox_SearchDeviceId.Text = "error";
            }
        }
    }
}
