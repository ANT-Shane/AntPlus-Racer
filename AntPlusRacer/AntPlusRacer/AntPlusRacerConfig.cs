/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AntPlusRacer
{
    public class AntPlusRacerConfig
    {
        public enum RacerInputType
        {
            ant_BikeCad_UsingSpd,
            ant_BikeCadAndSpd,
            ant_BikeSpd,
            ant_FitEqpmt,
            ant_StrideSDM
        }

        public struct RacerInput
        {
            public String displayName;
            public RacerInputType type;
            public UInt16 defaultDeviceId;
        }

        public struct RaceTrack
        {
            public racerSportType sportType;
            public double distance;
        }

        static AntPlusRacerConfig instance;

        public const string DatabaseName = "AntPlusRacerConfig";

        public bool autoLoadAvailableRaceSourcesForRace = true;

        public bool keepRecords = true;

        public bool displayMph = false;

        public bool quickStart_noCountdown = false;

        public bool fastRecords_noNames = false;

        public int RecordScreenSaverTimeout_negativeIsOff = 30;

        public int remoteControlDevNum_negativeIsOff = -1;

        public List<RacerInput> enabledRacerInputs = new List<RacerInput>();  
        public List<RaceTrack> enabledRaceTracks = new List<RaceTrack>();


        public List<RacerInput> availableRacerInputList = new List<RacerInput>();

        public AntPlusRacerConfig()
        {
        }

        public RaceTrack getDefaultRaceTrack(racerSportType sportType)
        {
            foreach (RaceTrack i in enabledRaceTracks)
                if (i.sportType == sportType)
                    return i;
            return new RaceTrack() { sportType = racerSportType.Unknown, distance = 499 };
        }

        public static AntPlusRacerConfig getInstance()
        {
            if (instance == null)
            {
                try
                {
                    instance = XmlDatabaser.loadDatabase<AntPlusRacerConfig>(DatabaseName,false);
                }
                catch (System.IO.FileNotFoundException) //If instance does not exist, create default
                {
                    //initialized in default constructor to show up in xml to show what is available
                    List<RacerInput> AvailableInputs;
                    List<RaceTrack> AvailableSubMinuteRaceTracks;

                    //Create default
                    instance = new AntPlusRacerConfig();

                    //Allow all available types
                    AvailableInputs = new List<RacerInput>();
                    foreach (RacerInputType i in Enum.GetValues(typeof(RacerInputType)))
                        AvailableInputs.Add(new RacerInput() { displayName = "", type = i, defaultDeviceId = 0});
                    instance.enabledRacerInputs = AvailableInputs;
                    instance.availableRacerInputList = AvailableInputs.ToList();
                    instance.availableRacerInputList[0] = new RacerInput() { displayName = "Sample Display Name, blank for default", type = instance.availableRacerInputList[0].type, defaultDeviceId = instance.availableRacerInputList[0].defaultDeviceId };

                    //Initialize several standardized tracks
                    AvailableSubMinuteRaceTracks = new List<RaceTrack>();
                    AvailableSubMinuteRaceTracks.Add(new RaceTrack() { sportType = racerSportType.Biking, distance = 500 });    //World record 1000m just under a minute in high altitude velodrome //58.875 seconds, Arnaud Tournant
                    AvailableSubMinuteRaceTracks.Add(new RaceTrack() { sportType = racerSportType.Rowing, distance = 200 });    //World record 500m 1:16 on rowing machine //75.9 seconds, Rob Smith
                    AvailableSubMinuteRaceTracks.Add(new RaceTrack() { sportType = racerSportType.Running, distance = 250 });   //Running World record 400m under 45s on track //43.18 seconds, Michael Johnson
                    AvailableSubMinuteRaceTracks.Add(new RaceTrack() { sportType = racerSportType.Skiing, distance = 200 });   //No idea //TODO pick a good distance here
                    instance.enabledRaceTracks = AvailableSubMinuteRaceTracks;

                    String err = XmlDatabaser.saveDatabase(DatabaseName, instance);
                    if (err != null)
                        System.Windows.MessageBox.Show("Error canot save created config file, the program will continue using default values");
                }                    
            }
            return instance;
        }

    }
}
