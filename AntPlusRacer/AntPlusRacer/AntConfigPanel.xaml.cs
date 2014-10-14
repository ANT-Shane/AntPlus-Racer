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
using System.Windows.Shapes;
using AntPlusRacer.DataSources;

namespace AntPlusRacer
{
    /// <summary>
    /// Interaction logic for FIT_ConfigPanel.xaml
    /// </summary>
    public partial class AntConfigPanel : UserControl
    {
        static AntConfigPanel instance;

        public AntPlusDevMgr antMgr;
        System.Timers.Timer updateTimer = new System.Timers.Timer(1000);

        private AntConfigPanel()
        {
            InitializeComponent();
            antMgr = new AntPlusDevMgr();

            ListBox_statusPanelHolder.Items.Clear();
            Binding widthBind = new Binding("ActualWidth");
            widthBind.Source = ListBox_statusPanelHolder;
            foreach (AntPlusDevMgr.AntPlus_Connection i in antMgr.deviceList)
            {
                Fit_ConfigPanelDevice pnl = new Fit_ConfigPanelDevice(i);
                pnl.SetBinding(Fit_ConfigPanelDevice.WidthProperty, widthBind);
                ListBox_statusPanelHolder.Items.Add(pnl);
            }


            if (AntPlusRacerConfig.getInstance().remoteControlDevNum_negativeIsOff > 0)
            {
                Grid.SetRow(RacerRemoteControl.getRemoteStatus(), 1);
                Grid_Main.Children.Add(RacerRemoteControl.getRemoteStatus());
            }
        }

        public static AntConfigPanel accessInstance()
        {
            if (instance == null)   //If the initializer failed we will have to rerun it each time
                instance = new AntConfigPanel();

            return instance;
        }

        public static UIElement getInstanceForDisplay()
        {
            if (accessInstance().antMgr.deviceList.Count == 0)
            {
                Viewbox vb = new Viewbox();
                vb.Child = new Label() { Content = "No devices selected for use. Exit and add devices to 'UsingDevices' in " + AntPlusRacerConfig.DatabaseName + ".db.xml" };
                return vb;
            }
            if (accessInstance().Parent != null)
            {
                Viewbox vb = new Viewbox();
                vb.Child = new Label() { Content = "FIT Config Panel Already Open" };
                return vb;
            }
            else
            {
                return instance;
            }
        }        

        private void Control_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            switch (e.NewValue as bool?)
            {
                case true:
                    EnableDisplayUpdate();
                    break;
                case false:
                    DisableDisplayUpdate();
                    break;
            }
        }

        private void EnableDisplayUpdate()
        {
            updateTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateTimer_Elapsed);
            updateTimer.Start();
        }

        private void DisableDisplayUpdate()
        {
            updateTimer.Stop();
            updateTimer.Elapsed -= updateTimer_Elapsed;
        }

        void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            foreach (Fit_ConfigPanelDevice i in ListBox_statusPanelHolder.Items)
                i.refreshView();
        }

        //Disable selection ability to prevent highlighting
        private void ListBox_statusPanelHolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox_statusPanelHolder.SelectionChanged -= ListBox_statusPanelHolder_SelectionChanged;
            ListBox_statusPanelHolder.SelectedIndex = -1;
            ListBox_statusPanelHolder.SelectionChanged += ListBox_statusPanelHolder_SelectionChanged;
        }


    }
}
