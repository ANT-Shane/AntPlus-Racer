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
    /// Interaction logic for RaceFactoryPanel.xaml
    /// </summary>
    public partial class RaceFactoryPanel : UserControl
    {
        Action<RaceDetails> raceRcvr;

        public List<RacerDetails> configuredRacers = new List<RacerDetails>();

        bool inRacerConfigScreen = false;

        List<DataSourceBase> dsOnDisplay;

        int racerIndexUnderConfig = -1;

        public RaceFactoryPanel(Action<RaceDetails> raceOutputRcvr)
        {
            InitializeComponent();
            raceRcvr = raceOutputRcvr;

            if (AntPlusRacerConfig.getInstance().autoLoadAvailableRaceSourcesForRace)
            {
                AntPlusDevMgr antMgr = AntConfigPanel.accessInstance().antMgr;
                for (int i = 0; i < antMgr.deviceList.Count; ++i)
                {
                    AntPlusDevMgr.AntPlus_Connection dev = antMgr.deviceList[i];
                    //Skip devices in use
                    if (dev.dataSource.isInUse || dev.getConnStatus() != AntPlusDevMgr.AntPlus_Connection.ConnState.Connected || isDataSourceReserved(dev.dataSource))
                        continue;
                    else
                        configureRacer(0xFF, dev.dataSource, AntPlusRacerConfig.getInstance().getDefaultRaceTrack(dev.dataSource.getSportType()).distance);
                }
                if (configuredRacers.Count == 0)
                    DisplayRacerList();
            }
            else
            {
                DisplayRacerList();
            }

            ComboBox_RaceLength.SizeChanged += new SizeChangedEventHandler((sender, args) => comboBoxAutoLblFontSize(sender as ComboBox));
        }

        void comboBoxAutoLblFontSize(ComboBox cb)
        {
            List<Label> lbls = new List<Label>();
            foreach (Object i in cb.Items)
            {
                Label lbl = i as Label;
                if (lbl != null)
                    lbls.Add((Label)i);
            }
            if (lbls.Count > 0)
            {
                int biggestFontSize = getLargestFontSize(lbls, cb.ActualWidth - 20, cb.ActualHeight);
                cb.FontSize = biggestFontSize;
            }
        }

        private int getLargestFontSize(List<Label> lbls, double maxWidth, double maxHeight)
        {
            int maxLength = 0;
            Label maxLbl = null;
            foreach (Label l in lbls)
            {
                int strLen = ((String)l.Content).Length;
                if (strLen > maxLength)
                {
                    maxLength = strLen;
                    maxLbl = l;
                }
            }
            if(maxLength == 0)
                return -1;

            Typeface t = new Typeface(maxLbl.FontFamily,maxLbl.FontStyle,maxLbl.FontWeight,maxLbl.FontStretch, null);
            FormattedText ft = new FormattedText((String)maxLbl.Content, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight, t, 10, Brushes.Black);
            int i;
            for (i = 10; i < 120; ++i)
            {
                ft.SetFontSize(i);
                if (ft.Width > maxWidth || ft.Height > maxHeight)
                    break;
            }
            return i - 1;
        }

        private void DisplayRacerList()
        {
            inRacerConfigScreen = false;

            ListBox_RacerOrSources.Items.Clear();
            Button_AddRacer.Visibility = System.Windows.Visibility.Visible;
            ComboBox_RaceLength.Visibility = System.Windows.Visibility.Hidden;
            Button_Start.Visibility = System.Windows.Visibility.Visible;
            Label_DisplayTitle.Content = "Racer List:";

            Binding widthBind = new Binding("ActualWidth");
            widthBind.Source = ListBox_RacerOrSources;

            if (configuredRacers.Count > 0)
            {
                for (int i = 0; i < configuredRacers.Count; ++i)
                {
                    Viewbox vb = new Viewbox();
                    Label racerLbl = new Label();
                    vb.SetBinding(Viewbox.WidthProperty, widthBind);
                    vb.Margin = new Thickness(0, 1, 30, 1);
                    vb.Child = racerLbl;
                    if (configuredRacers[i].racerRecordInfo != null && !String.IsNullOrWhiteSpace(configuredRacers[i].racerRecordInfo.FirstName))
                        racerLbl.Content = String.Format("{0}: {1} {2}m-{3}", configuredRacers[i].racerRecordInfo.FirstName, configuredRacers[i].dataSource.getSportType(), configuredRacers[i].targetValue, configuredRacers[i].dataSource.getSourceName());
                    else if(configuredRacers[i].racerRecordInfo != null && !String.IsNullOrWhiteSpace(configuredRacers[i].racerRecordInfo.LastName))
                        racerLbl.Content = String.Format("{0}: {1} {2}m-{3}", configuredRacers[i].racerRecordInfo.LastName, configuredRacers[i].dataSource.getSportType(), configuredRacers[i].targetValue, configuredRacers[i].dataSource.getSourceName());
                    else
                        racerLbl.Content = String.Format("Racer {0}: {1} {2}m-{3}", i + 1, configuredRacers[i].dataSource.getSportType(), configuredRacers[i].targetValue, configuredRacers[i].dataSource.getSourceName());
                    ListBox_RacerOrSources.Items.Add(vb);
                }
                Button_Start.IsEnabled = true;
            }
            else
            {
                Label_DisplayTitle.Content = "Racer List:";
                Button_Start.IsEnabled = false;
            }

            Label lbl2 = new Label() { Content = "Change Racer" };
            Viewbox vb2 = new Viewbox() { Child = lbl2 };
            Button_RacerAddOrFinish.Content = vb2;
            Label lbl3 = new Label() { Content = "Remove Racer" };
            Viewbox vb3 = new Viewbox() { Child = lbl3 };
            Button_RacerRemoveOrCancel.Content = vb3;

            Button_RacerAddOrFinish.IsEnabled = false;
            Button_RacerRemoveOrCancel.IsEnabled = false;

            if (racerIndexUnderConfig >= 0)
            {
                ListBox_RacerOrSources.SelectedIndex = racerIndexUnderConfig;
            }
            else
            {
                ListBox_RacerOrSources.SelectedIndex = -1;
                Image_SelectionImage.Source = RacerInfoPanel.getRaceDisplayBitmap(racerSportType.Unknown);
            }
        }

        private bool isDataSourceReserved(DataSourceBase ds)
        {
            foreach (RacerDetails i in configuredRacers)
            {
                if (i != null && i.dataSource.Equals(ds))
                    return true;
            }
            return false;
        }

        private void DisplayRacerConfig()
        {
            inRacerConfigScreen = true;

            //TODOb this only shows ant plus sources
            ListBox_RacerOrSources.Items.Clear();
            Button_AddRacer.Visibility = System.Windows.Visibility.Hidden;
            ComboBox_RaceLength.Visibility = System.Windows.Visibility.Visible;
            Button_Start.Visibility = System.Windows.Visibility.Hidden;
            Label_DisplayTitle.Content = "Configure New Racer - Source and Track Length:";
            ComboBox_RaceLength.Items.Clear();

            AntPlusDevMgr antMgr = AntConfigPanel.accessInstance().antMgr;
            Binding widthBind = new Binding("ActualWidth");
            widthBind.Source = ListBox_RacerOrSources;

            bool select0 = false;
            dsOnDisplay = new List<DataSourceBase>();

            //Add current source
            if (racerIndexUnderConfig >= 0)
            {
                dsOnDisplay.Add(configuredRacers[racerIndexUnderConfig].dataSource);
                select0 = true;
            }

            for(int i=0; i<antMgr.deviceList.Count; ++i)
            {
                AntPlusDevMgr.AntPlus_Connection dev = antMgr.deviceList[i];
                //Skip devices in use
                if (dev.dataSource.isInUse || dev.getConnStatus() != AntPlusDevMgr.AntPlus_Connection.ConnState.Connected || isDataSourceReserved(dev.dataSource))
                    continue;
                else
                    dsOnDisplay.Add(dev.dataSource);
            }

            if (dsOnDisplay.Count > 0)
            {
                foreach (DataSourceBase i in dsOnDisplay)
                {
                    //Show as option in list
                    Label name = new Label();
                    name.Content = i.getSportType() + "-" + i.getSourceName();
                    Viewbox vb = new Viewbox();
                    vb.SetBinding(Viewbox.WidthProperty, widthBind);
                    vb.Margin = new Thickness(0, 1, 20, 1);
                    vb.Child = name;
                    ListBox_RacerOrSources.Items.Add(vb);

                    if (select0)
                        ListBox_RacerOrSources.SelectedIndex = 0;

                    Button_RacerAddOrFinish.IsEnabled = true;
                    Button_RacerRemoveOrCancel.IsEnabled = true;
                }
            }
            else
            {
                Label_DisplayTitle.Content = "...No Data Sources Available";
                Button_RacerAddOrFinish.IsEnabled = false;
                Button_RacerRemoveOrCancel.IsEnabled = true;
            }            

            Label lbl2 = new Label() { Content = "Save" };
            Viewbox vb2 = new Viewbox() { Child = lbl2 };
            Button_RacerAddOrFinish.Content = vb2;
            Label lbl3 = new Label() { Content = "Cancel" };
            Viewbox vb3 = new Viewbox() { Child = lbl3 };
            Button_RacerRemoveOrCancel.Content = vb3;            
        }

        public bool startRace()
        {
            //Need someone in the race!
            if (configuredRacers.Count < 1)
                return false;

            //If solo, might as well race the record
            if (configuredRacers.Count == 1)
                configuredRacers.Add(new RacerDetails(new ds_TrackRecord(configuredRacers[0].dataSource.getSportType(), configuredRacers[0].targetValue), configuredRacers[0].targetValue));

            //Send race setup to raceRcvr
            raceRcvr(new RaceDetails(configuredRacers, targetType.Meters));
            return true;
        }

        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            startRace();
        }

        private void Button_RacerConfigOrSave_Click(object sender, RoutedEventArgs e)
        {
            if (inRacerConfigScreen)   //SAVE CONFIG
            {
                //Save racer config and go back to racer list
                if (ListBox_RacerOrSources.SelectedIndex >= 0 && ComboBox_RaceLength.SelectedIndex >= 0)
                {
                    if (racerIndexUnderConfig < 0)
                        racerIndexUnderConfig = 0xFF;
                    string ds = (String)((Label)ComboBox_RaceLength.SelectedItem).Content;
                    double selectedTrackDist = double.Parse(ds.Remove(ds.IndexOf('m')));

                    if (-1 == configureRacer((byte)racerIndexUnderConfig, dsOnDisplay[ListBox_RacerOrSources.SelectedIndex], selectedTrackDist))
                    {
                        ((Label)((Viewbox)ListBox_RacerOrSources.SelectedItem).Child).Content = "ALREADY IN USE - " + (String)((Label)((Viewbox)ListBox_RacerOrSources.SelectedItem).Child).Content;
                        //ListBox_RacerOrSources.Items.Refresh();
                    }
                }
            }
            else   //CONFIG RACER
            {                
                //Go to racer config select screen
                if (ListBox_RacerOrSources.SelectedIndex >= 0)
                {
                    racerIndexUnderConfig = ListBox_RacerOrSources.SelectedIndex;
                    DisplayRacerConfig();
                }
            }
        }

        private void Button_AddRacer_Click(object sender, RoutedEventArgs e)
        {
            racerIndexUnderConfig = -1;
            DisplayRacerConfig();
        }

        private void Button_RacerRemoveOrCancel_Click(object sender, RoutedEventArgs e)
        {
            if (inRacerConfigScreen)   //CANCEL
            {
                //Don't save and go back to racer list
                DisplayRacerList();
            }
            else   //REMOVE RACER
            {
                //Set selected racer back to null
                if (ListBox_RacerOrSources.SelectedIndex >= 0)
                    removeRacer(ListBox_RacerOrSources.SelectedIndex);
            }
        }

        private void populateDistanceBox(racerSportType sport)
        {
            ComboBox_RaceLength.Items.Clear();

            RacerDetails selectedRacer = null;

            if (racerIndexUnderConfig >= 0)
            {
                selectedRacer = configuredRacers[racerIndexUnderConfig];
                ComboBox_RaceLength.Items.Add(new Label() { Content = selectedRacer.targetValue.ToString() + "m track" });
            }

            //Add all available values
            foreach (AntPlusRacerConfig.RaceTrack i in AntPlusRacerConfig.getInstance().enabledRaceTracks)
            {
                if (i.sportType == sport)
                {
                    //Skip the current value because we already added it
                    if (selectedRacer != null && selectedRacer.targetValue == i.distance)
                        continue;

                    ComboBox_RaceLength.Items.Add(new Label() { Content = i.distance.ToString() + "m track" });
                }
            }

            if (ComboBox_RaceLength.Items.Count == 0)
                ComboBox_RaceLength.Items.Add(new Label() { Content = "499m track" });

            ComboBox_RaceLength.SelectedIndex = 0;
            comboBoxAutoLblFontSize(ComboBox_RaceLength);
        }

        private void ListBox_RacerOrSources_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBox_RacerOrSources.SelectedIndex >= 0)
            {
                DataSourceBase ds = null;
                if (inRacerConfigScreen)
                {
                    if (dsOnDisplay.Count > 0)
                    {
                        ds = dsOnDisplay[ListBox_RacerOrSources.SelectedIndex];
                        populateDistanceBox(ds.getSportType());
                    }
                }
                else
                {
                    ds = configuredRacers[ListBox_RacerOrSources.SelectedIndex].dataSource;
                    Button_RacerAddOrFinish.IsEnabled = true;
                    Button_RacerRemoveOrCancel.IsEnabled = true;
                }
                
                Image_SelectionImage.Source = RacerInfoPanel.getRaceDisplayBitmap(ds.getSportType());
            }
            else
            {
                Image_SelectionImage.Source = RacerInfoPanel.getRaceDisplayBitmap(racerSportType.Unknown);
                ComboBox_RaceLength.Items.Clear();
            }
        }

        public bool removeRacer(int racerIndex)
        {
            if (racerIndex < configuredRacers.Count)
            {
                configuredRacers[racerIndex].dataSource.isInUse = false;
                configuredRacers.RemoveAt(racerIndex);

                if (inRacerConfigScreen)
                {
                    if (racerIndexUnderConfig == racerIndex)
                    {
                        racerIndexUnderConfig = -1;
                        DisplayRacerList();
                    }
                }
                else
                {
                    if (ListBox_RacerOrSources.SelectedIndex == racerIndex)
                        racerIndexUnderConfig = -1;
                    else
                        racerIndexUnderConfig = ListBox_RacerOrSources.SelectedIndex;
                    DisplayRacerList();
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public int configureRacer(byte racerNum, DataSourceBase dataSrc, double selectedTrackDist, string firstName = null, string lastname = null, string phoneNum = null, string emailAdr = null)
        {
            //Prerequisite: Callers have already ensured selected data source is connected (because not all data sources have the notion of connectivity)
            if (racerNum == 0xFF)
            {
                if (dataSrc.isInUse || isDataSourceReserved(dataSrc))
                    return -1;
                //if (dev.getConnStatus() != AntPlusDevMgr.AntPlus_Connection.ConnState.Connected)  //Both the remote and the factory list already check for this
                racerIndexUnderConfig = configuredRacers.Count;
                configuredRacers.Add(new RacerDetails(dataSrc, selectedTrackDist));               
            }
            else  
            {
                if (racerNum >= configuredRacers.Count)
                    return -2;
                if ( (!dataSrc.Equals(configuredRacers[racerNum].dataSource) && isDataSourceReserved(dataSrc))
                    || !configuredRacers[racerNum].changeRacerSource(dataSrc, selectedTrackDist))
                    return -1;
                racerIndexUnderConfig = racerNum;
            }

            if (configuredRacers[racerIndexUnderConfig].racerRecordInfo == null && (firstName != null || lastname != null || phoneNum != null || emailAdr != null))
            {
                configuredRacers[racerIndexUnderConfig].racerRecordInfo = new TrackRecords.RecordData();

                if (firstName != null)
                    configuredRacers[racerIndexUnderConfig].racerRecordInfo.FirstName = firstName;
                if (lastname != null)
                    configuredRacers[racerIndexUnderConfig].racerRecordInfo.LastName = lastname;
                if (phoneNum != null)
                    configuredRacers[racerIndexUnderConfig].racerRecordInfo.PhoneNumber = phoneNum;
                if (emailAdr != null)
                    configuredRacers[racerIndexUnderConfig].racerRecordInfo.Email = emailAdr;
            }

            DisplayRacerList();
            return 0;
        }
    }
}
