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

namespace AntPlusRacer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow instance = new MainWindow();

        const int maxRaces = 3;
        List<RacePanel> racePanelList = new List<RacePanel>();

        private MainWindow()
        {
            InitializeComponent();

            addRacePanel();
        }

        static public MainWindow getInstance()
        {
            return instance;
        }

        public RacePanel getRacePanel(int number)
        {
            if (racePanelList.Count > number)
            {
                return racePanelList[number];
            }
            else
            {
                return null;
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if ((e.Key == Key.Add || e.Key == Key.OemPlus) && (Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftShift)))
            {
                
                    addRacePanel();
            }
            else if (e.Key == Key.F1)
            {
                if (racePanelList.Count > 0)
                    racePanelList[0].toggleMenu();
            }
            else if (e.Key == Key.F2)
            {
                if (racePanelList.Count > 1)
                    racePanelList[1].toggleMenu();
            }
            else if (e.Key == Key.F3)
            {
                if (racePanelList.Count > 2)
                    racePanelList[2].toggleMenu();
            }
        }

        public RacePanel addRacePanel()
        {
            if (racePanelList.Count >= maxRaces)
                return null;

            RacePanel newRace = new RacePanel(closeRacePanel);
            racePanelList.Add(newRace);

            newRace.Border.BorderBrush = getPanelColour(racePanelList.Count);

            Grid_Races.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            Grid.SetRow(newRace, Grid_Races.RowDefinitions.Count - 1);
            Grid_Races.Children.Add(newRace);

            return newRace;
        }

        bool closeRacePanel(RacePanel panelClosing)
        {
            if (Grid_Races.Children.Count == 1)
                return false; //Don't close the last panel

            //Take out the list entry and its grid row
            racePanelList.Remove(panelClosing);
            Grid_Races.RowDefinitions.RemoveAt(0);


            //Refill the list
            Grid_Races.Children.Clear();
            for (int i = 0; i < racePanelList.Count; ++i)
            {
                Grid.SetRow(racePanelList[i], i);

                racePanelList[i].Border.BorderBrush = getPanelColour(i + 1);
                
                Grid_Races.Children.Add(racePanelList[i]);
            }

            return true;
        }

        SolidColorBrush getPanelColour(int panelNum)
        {
            switch (panelNum)
            {
                case 1:
                    return Brushes.Black;
                case 2:
                    return Brushes.Gray;
                case 3:
                    return Brushes.Black;
            }
            return null;
        }

    }
}
