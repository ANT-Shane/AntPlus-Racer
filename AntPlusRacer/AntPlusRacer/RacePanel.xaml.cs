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
    /// Interaction logic for RacePanel.xaml
    /// </summary>
    public partial class RacePanel : UserControl
    {
        Func<RacePanel, bool> closePanelSignal;

        public RaceDetails raceConfig = null;
        
        System.Timers.Timer ss_ScreenSaverTimer = new System.Timers.Timer(1000);
        public DateTime ss_lastActivity;
        bool ss_isActive = false;
        bool ss_ignoreFirstMove;
        RaceFactoryPanel ss_savedPanel = null;

        public enum panelState
        {
            RecordDisplay,
            RaceFactory,
            Config,
            Racing,
            PostRaceDisplay,
            PostRaceResults,
            ScreenSaverRecordDisplay,
        }
        public panelState PanelState;

        public RacePanel(Func<RacePanel, bool> closeRacePnl)
        {
            InitializeComponent();

            this.closePanelSignal = closeRacePnl;

            ColumnDefinition_menuCol.Width = new GridLength(1, GridUnitType.Star);
            showAntConfig();

            ss_ScreenSaverTimer.Elapsed += new System.Timers.ElapsedEventHandler(recordDisplayScreenSaver_Elapsed);
        }

        public void toggleMenu()
        {
            if (ColumnDefinition_menuCol.Width.Value == 0)
                ColumnDefinition_menuCol.Width = new GridLength(1, GridUnitType.Star);
            else
                ColumnDefinition_menuCol.Width = new GridLength(0, GridUnitType.Star);
        }

        public void hideMenu()
        {
            if (ColumnDefinition_menuCol.Width.Value != 0)
                toggleMenu();
        }

        private void button_close_Click(object sender, RoutedEventArgs e)
        {
            if (!closePanel())
            {
                MessageBoxResult result = MessageBox.Show(MainWindow.getInstance(), "Exit the racer application?", "Ant+ Racer - Confirm Exit", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                    MainWindow.getInstance().Close();
            }
        }

        public bool closePanel()
        {
            if (!closePanelSignal(this))
                return false;

            prepareForPanelChange(true);
            return true;
        }

        public void factoryComplete(RaceDetails raceConfig)
        {
            this.raceConfig = raceConfig;

            hideMenu();
            showRaces();

            raceConfig.startRace(this);
        }

        public void showAntConfig()
        {
            prepareForPanelChange(true);

            UIElement antCfg;
            try
            {
                antCfg = AntConfigPanel.getInstanceForDisplay();
            }
            catch (Exception ex)
            {
                Viewbox vb = new Viewbox();
                vb.Child = new Label() { Content = "Error: " + ex.Message };
                antCfg = vb;
            }

            Grid_Content.Children.Add(antCfg);
            PanelState = panelState.Config;
        }

        public void showRaceFactory()
        {
            prepareForPanelChange(true);
            raceConfig = null;

            RaceFactoryPanel toAdd;
            if (ss_isActive && ss_savedPanel != null)
            {
                toAdd = ss_savedPanel;
                ss_isActive = false;
            }
            else
            {
                toAdd = new RaceFactoryPanel(factoryComplete);
            }

            Grid_Content.Children.Add(toAdd);
            PanelState = panelState.RaceFactory;

            ss_lastActivity = DateTime.Now;

            if(AntPlusRacerConfig.getInstance().RecordScreenSaverTimeout_negativeIsOff > 0)
                ss_ScreenSaverTimer.Start();
        }

        public void showRaces()
        {
            if (raceConfig == null || raceConfig.isRaceFinished)
            {
                showRaceFactory();
                return;
            }

            prepareForPanelChange(false);

            for (int i = 0; i < raceConfig.racerDetails.Count; ++i)
            {
                Grid_Content.RowDefinitions.Add(new RowDefinition(){Height = new GridLength(1, GridUnitType.Star)});
                Grid.SetRow(raceConfig.racerDetails[i].racePanel, i);
                Grid_Content.Children.Add(raceConfig.racerDetails[i].racePanel);
            }
            PanelState = panelState.Racing;
        }

        public void showPostRacePanel(RacerDetails racer, Action<TrackRecords.TrackRecordList, int> postRaceDoneHandler)
        {
            prepareForPanelChange(false);

            Grid_Content.Children.Add(new TrackRecords.PostRacePanel(postRaceDoneHandler, racer));
            PanelState = panelState.PostRaceDisplay;
        }

        public void showResult(TrackRecords.TrackRecordList recordListToShow, List<int> recordsToHighlight)
        {
            prepareForPanelChange(false);

            Grid_Content.Children.Add(new TrackRecords.RecordDisplayPanel(recordListToShow, recordsToHighlight));
            PanelState = panelState.PostRaceResults;
        }

        public void showRecords()
        {
            if(ss_isActive)
                ss_savedPanel = Grid_Content.Children[0] as RaceFactoryPanel;

            prepareForPanelChange(true);

            Grid_Content.Children.Add(new TrackRecords.RecordDisplayPanel());
            PanelState = panelState.RecordDisplay;
        }

        private void prepareForPanelChange(bool doneWithRace)
        {
            if (doneWithRace)
            {
                //If we are in the factory, we need to dispose the configured racers data sources
                if (PanelState == panelState.RaceFactory && !ss_isActive)
                {
                    foreach (RacerDetails i in ((RaceFactoryPanel)Grid_Content.Children[0]).configuredRacers)
                        i.dataSource.isInUse = false;
                }

                if (raceConfig != null)  //Ensure we dispose of the race
                {
                    if (!raceConfig.isRaceFinished)
                        raceConfig.disposeRace();   //If the race is still running, cancel it
                    raceConfig = null;
                }
            }

            ss_ScreenSaverTimer.Stop();

            Grid_Content.Children.Clear();
            Grid_Content.RowDefinitions.Clear();
        }

        private void button_AntConfig_Click(object sender, RoutedEventArgs e)
        {
            showAntConfig();
        }

        private void button_ViewRecords_Click(object sender, RoutedEventArgs e)
        {
            showRecords();
        }

        private void button_hide_Click(object sender, RoutedEventArgs e)
        {
            toggleMenu();
        }

        private void button_NewRace_Click(object sender, RoutedEventArgs e)
        {
            showRaceFactory();
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (ss_isActive)
            {
                lock (ss_ScreenSaverTimer)
                {
                    if (ss_isActive)
                    {
                        if(ss_ignoreFirstMove) //Ignore the first move since it is fired when the panel switches
                            ss_ignoreFirstMove = false;
                        else
                            showRaceFactory();
                    }
                }
            }
            else
            {
                ss_lastActivity = DateTime.Now;
            }
        }

        private void UserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (ss_isActive)
            {
                lock (ss_ScreenSaverTimer)
                {
                    if (ss_isActive)
                        showRaceFactory();
                }
            }
            else
            {
                ss_lastActivity = DateTime.Now;
            }
        }

        void recordDisplayScreenSaver_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (DateTime.Now.Subtract(ss_lastActivity).TotalSeconds > AntPlusRacerConfig.getInstance().RecordScreenSaverTimeout_negativeIsOff)
            {
                lock (ss_ScreenSaverTimer)
                {
                    if (!ss_isActive)
                    {
                        ss_lastActivity = DateTime.Now;
                        ss_ignoreFirstMove = true;
                        ss_isActive = true;
                        Dispatcher.Invoke((Action)showRecords);
                        PanelState = panelState.ScreenSaverRecordDisplay;
                    }
                }
            }
        }
    }
}
