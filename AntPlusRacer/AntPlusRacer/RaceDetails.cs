/*
This software is subject to the license described in the license.txt file included with this software distribution.You may not use this file except in compliance with this license. 
Copyright © Dynastream Innovations Inc. 2012
All rights reserved.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AntPlusRacer.DataSources;

namespace AntPlusRacer
{
    public class RaceDetails
    {
        public List<RacerDetails> racerDetails;
        public targetType target_type;

        public System.Timers.Timer raceTock;
        public System.Diagnostics.Stopwatch raceTimer;

        public bool isRaceFinished = false;

        int notFinishedHumanRacers = 0;
        int finishOrder = 0;

        RacePanel OwningDisplayPanel;

        System.Threading.CancellationTokenSource cancelAsyncToken;
        System.Threading.Tasks.Task asyncWorker;

        public RaceDetails(List<RacerDetails> racerList, targetType targetType)
        {
            //Note: Some of the framework was set up to handle races with a timed target,
            // but as development progressed, only distance races were implemented. Enabling timed
            // races would require a thorough code review.
            if(targetType != AntPlusRacer.targetType.Meters)
                throw new NotImplementedException("Support for timed races is disabled! See notes in code.");

            racerDetails = racerList.ToList();         

            this.target_type = targetType;
            raceTock = new System.Timers.Timer(750);
            raceTock.Elapsed += new System.Timers.ElapsedEventHandler(raceTock_Elapsed);

            if (target_type == targetType.Meters)
                raceTimer = new System.Diagnostics.Stopwatch();
        }

        void startAsync(Action asyncTask)
        {
            if (asyncWorker != null)
            {
                try
                {
                    asyncWorker.Wait();
                }
                catch (AggregateException)
                {
                    if(cancelAsyncToken.IsCancellationRequested)
                        return; //If the last task was cancelled we are too
                }
            }
            else
            {
                cancelAsyncToken = new System.Threading.CancellationTokenSource();
            }

            if (!cancelAsyncToken.IsCancellationRequested)
                asyncWorker = System.Threading.Tasks.Task.Factory.StartNew(asyncTask, cancelAsyncToken.Token);
        }

        public void sleepCheckStop(int ms)
        {
            cancelAsyncToken.Token.ThrowIfCancellationRequested();
            while (ms > 0)
            {
                if (ms > 100)
                    System.Threading.Thread.Sleep(100);
                else
                    System.Threading.Thread.Sleep(ms);

                cancelAsyncToken.Token.ThrowIfCancellationRequested();
                ms -= 100;
            }
            
        }

        public void disposeRace()
        {
            //Make sure any async ops are stopped
            if (asyncWorker != null)
            {
                try
                {
                    cancelAsyncToken.Cancel();
                    asyncWorker.Wait();
                }
                catch (AggregateException ex)
                {
                    if (!cancelAsyncToken.IsCancellationRequested)
                        throw ex;   //If this is exception is not from cancelling, rethrow it
                }
            }
            
            //Turn off all the recurring timers
            raceTock.Stop();
            if (target_type == targetType.Meters)
                raceTimer.Stop();
            foreach (RacerDetails i in racerDetails)
            {
                i.dataSource.stop();
                i.dataSource.isInUse = false;
            }            
        }

        public void startRace(RacePanel displayPanel)
        {
            OwningDisplayPanel = displayPanel;
            startAsync(startRaceAsync);
        }
    
        private void startRaceAsync()
        {
            notFinishedHumanRacers = 0;
            foreach (RacerDetails i in racerDetails)
            {
                i.dataSource.reset();
                OwningDisplayPanel.Dispatcher.Invoke((Action)i.racePanel.resetDisplay, System.Windows.Threading.DispatcherPriority.Loaded);

                if (i.dataSource.isHuman)
                    ++notFinishedHumanRacers;
            }

            if (!AntPlusRacerConfig.getInstance().quickStart_noCountdown)
            {
                //Display names
                
                for (int i = 0; i < racerDetails.Count; ++i)
                {
                    racerDetails[i].racePanel.makeAnnouncement(racerDetails[i].racer_displayname);
                    sleepCheckStop(1000);                    
                }
                sleepCheckStop(1000);

                //Display target values
                for (int i = 0; i < racerDetails.Count; ++i)
                {
                    racerDetails[i].racePanel.makeAnnouncement(" ");
                    racerDetails[i].racePanel.makeAnnouncement(racerDetails[i].targetValue.ToString() + " " + target_type);
                }
                sleepCheckStop(2000);

                //Set Displays to Blank
                for (int i = 0; i < racerDetails.Count; ++i)
                {
                    racerDetails[i].racePanel.makeAnnouncement(" ");
                }

                //Countdown (only on top screen)
                racerDetails[0].racePanel.makeAnnouncement("Ready?");
                sleepCheckStop(2000);
                racerDetails[0].racePanel.makeAnnouncement("3");
                sleepCheckStop(1000);
                racerDetails[0].racePanel.makeAnnouncement("2");
                sleepCheckStop(1000);
                racerDetails[0].racePanel.makeAnnouncement("1");
                sleepCheckStop(1000);
            }

            //TODO: qc could check speed of fit equip to make sure no one takes a running start
            racerDetails[0].racePanel.makeAnnouncement("GO!");

            for (int i = 0; i < racerDetails.Count; ++i)
            {
                int a = i;
                racerDetails[i].dataSource.start((Action<DataSourcePacket>)((x) => dataSourceUpdating(a, x)));
            }

            //Start the timer(s)
            raceTock.Start();
            
            if(target_type == targetType.Meters)
                raceTimer.Start();

            //Clear the countdown display
            sleepCheckStop(1000);

            //Clear Displays
            for (int i = 0; i < racerDetails.Count; ++i)
            {
                racerDetails[i].racePanel.makeAnnouncement("");
            } 
        }

        private void finishRaceAsync()
        {
            isRaceFinished = true;

            raceTock.Stop();
            if (target_type == targetType.Meters)
                raceTimer.Stop(); 

            if (!AntPlusRacerConfig.getInstance().keepRecords)
            {
                sleepCheckStop(6000);    //wait longer without records because they won't see the scores again 
            }
            else
            {
                //Allow time to view the result before we switch screens
                if(AntPlusRacerConfig.getInstance().fastRecords_noNames)
                    sleepCheckStop(1000);
                else
                    sleepCheckStop(3000);

                //Sort racers by finish result so that the saved indexes from the post racepanel are consistent
                racerDetails.Sort((x, y) => Comparer<Double>.Default.Compare(x.finishResult, y.finishResult));

                //Save the results of any human racers
                List<KeyValuePair<TrackRecords.TrackRecordList, int>> savedRecords = new List<KeyValuePair<TrackRecords.TrackRecordList, int>>();
                System.Threading.AutoResetEvent recordSaveDone = new System.Threading.AutoResetEvent(false);
                foreach (RacerDetails i in racerDetails)
                {
                    if (i.dataSource.isHuman)
                    {
                        if (AntPlusRacerConfig.getInstance().fastRecords_noNames)
                        {
                            i.racerRecordInfo = new TrackRecords.RecordData() { FirstName = i.dataSource.getSourceName() };
                        }

                        if (i.racerRecordInfo != null) //If we have preset data, save the record automatically
                        {
                            i.racerRecordInfo.DataSourceName = i.dataSource.getSourceName();
                            i.racerRecordInfo.recordValue = i.finishResult;
                            TrackRecords.TrackRecordList list = TrackRecords.RecordDatabase.getInstance().getTrackRecordList(i.dataSource.getSportType(), i.targetValue);
                            int pos = TrackRecords.PostRacePanel.saveRacerRecord(list, i.racerRecordInfo);                 
                            savedRecords.Add(new KeyValuePair<TrackRecords.TrackRecordList, int>(list, pos));
                        }
                        else
                        {
                            OwningDisplayPanel.Dispatcher.Invoke((Action<RacerDetails, Action<TrackRecords.TrackRecordList, int>>)OwningDisplayPanel.showPostRacePanel,
                                                                         i, new Action<TrackRecords.TrackRecordList, int>(
                                                                            (list, pos) =>
                                                                            {
                                                                                savedRecords.Add(new KeyValuePair<TrackRecords.TrackRecordList, int>(list, pos));
                                                                                recordSaveDone.Set();
                                                                            }));

                            //Sleep until post race panel is done
                            System.Threading.WaitHandle.WaitAny(new System.Threading.WaitHandle[] { cancelAsyncToken.Token.WaitHandle, recordSaveDone });
                        }
                        cancelAsyncToken.Token.ThrowIfCancellationRequested();
                    }
                }

                //Yes, there are simpler ways to correlate the lists. But why not try and learn how to use new things?
                Lookup<TrackRecords.TrackRecordList, int> recordLists = (Lookup<TrackRecords.TrackRecordList, int>)savedRecords.ToLookup(k => k.Key, e => e.Value);
                foreach (IGrouping<TrackRecords.TrackRecordList, int> trackList in recordLists)
                {
                    OwningDisplayPanel.Dispatcher.Invoke((Action<TrackRecords.TrackRecordList, List<int>>)OwningDisplayPanel.showResult, trackList.Key, trackList.ToList());
                    //Wait to view this list for a moment
                    if (AntPlusRacerConfig.getInstance().fastRecords_noNames)
                        sleepCheckStop(1000);
                    else
                        sleepCheckStop(5000);
                }
            }

            OwningDisplayPanel.Dispatcher.Invoke((Action)OwningDisplayPanel.showRaceFactory);   //Now, back to start a new race
        }

        void raceTock_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            updateRaceAnimations();

            //Update all the timers if in time race
            //if (target_type == targetType.Seconds)
            //{
            //    foreach (RacerDetails i in racerDetails)
            //    {
            //        ++i.targetProgress;
            //        if (i.targetProgress >= i.targetValue)
            //        {
            //            i.finish(++finishOrder, targetType.Meters, i.dataSource.getCurrentDistance());

            //            //If a human racer, handle and check human not finished count
            //            if(i.dataSource.isHuman)
            //            {
            //                //If everyone is done, stop the timer
            //                if (--notFinishedHumanRacers == 0)
            //                {
            //                    finishRace();
            //                }
            //            }
            //        }
            //    }
            //}
        }

        void dataSourceUpdating(int racerIndex, DataSourcePacket data)
        {
            RacerDetails racer = racerDetails[racerIndex];

            //Handle targets if this is a distance race
            if (target_type == targetType.Meters)
            {
                if (data.distance >= racer.targetValue)
                {
                    double finishTime = raceTimer.Elapsed.TotalMilliseconds/1000;

                    //Since the finish distance is usually a little past the target when we get it, we need to interpolate to get an accurate value for the actual finish
                    //TODO qc Another issue with accuracy is that depending on the timing of the first update packets between racers, one racer can have up to one packet period advantage.
                    racer.finish(++finishOrder, targetType.Seconds, finishTime - ((data.distance - racer.targetValue) / data.speed_ms));

                    //If everyone is done, stop the timer, don't wait for simulator results
                    if (racer.dataSource.isHuman)
                    {
                        if (--notFinishedHumanRacers == 0)
                        {
                            //Make sure all non-humans are closed too
                            foreach (RacerDetails i in racerDetails)
                            {
                                i.dataSource.stop();
                                i.dataSource.isInUse = false;
                            }

                            startAsync(finishRaceAsync);
                        }
                    }                  
                }
                else
                {
                    if(data.speed_ms != 0)
                        racer.racePanel.updateProgressDisplay(data.distance, (int)(1000 * (racer.targetValue - data.distance) / data.speed_ms));
                    else
                        racer.racePanel.updateProgressDisplay(data.distance, 0);
                }
            }

            racer.racePanel.updateStats(data);
        }

        private void updateRaceAnimations()
        {
            RacerDetails leadingRacer = racerDetails[0];
            double leadingRaceProgress = leadingRacer.dataSource.getCurrentDistance() / leadingRacer.targetValue;
            List<RacerDetails> trailingRacers = new List<RacerDetails>();
            List<double> trailerRaceProgress = new List<double>();

            for(int i=1; i<racerDetails.Count; ++i)
            {
                double currentRacePosition = racerDetails[i].dataSource.getCurrentDistance() / racerDetails[i].targetValue;
                if(currentRacePosition > leadingRaceProgress)
                {
                    //Push the old highest into the trailing list
                    trailingRacers.Add(leadingRacer);
                    trailerRaceProgress.Add(leadingRaceProgress);
                    //Save the new highest
                    leadingRacer = racerDetails[i];
                    leadingRaceProgress = currentRacePosition;
                }
                else
                {
                    trailingRacers.Add(racerDetails[i]);
                    trailerRaceProgress.Add(currentRacePosition);
                }
            }

            if (leadingRaceProgress == 0)   //Updating when the leading racer is 0 causes divide by 0 errors
                return;

            //Take care of leader's positioning first            
            double leadingDisplayPosition;
            if (leadingRacer.dataSource.isRunning())
            {
                if (leadingRaceProgress < 0.2)
                {
                    leadingDisplayPosition = leadingRaceProgress * 2; // ~ pos/0.2 * 0.4
                }
                else if (leadingRaceProgress < 0.8)
                {
                    leadingDisplayPosition = (((leadingRaceProgress - 0.2) / 0.6) * 0.4) + 0.4;
                }
                else
                {
                    leadingDisplayPosition = leadingRaceProgress;
                }
                //System.Console.Write("f-{0}, {1:0.00}, ", leadingRacer.dataSource.getSourceName(), leadingRaceProgress);
                leadingRacer.racePanel.moveDistance(leadingDisplayPosition, 1);
            }
            else
            {
                leadingRaceProgress = 1;
                leadingDisplayPosition = 1;
                //System.Console.WriteLine("f-{0}, {1:0.00}, Finished.", leadingRacer.dataSource.getSourceName(), leadingRaceProgress);
            }


            //Now take care of trailers
            for(int i=0; i<trailingRacers.Count; ++i)
            {
                if (trailingRacers[i].dataSource.isRunning())
                {
                    double fractionOfLead = trailerRaceProgress[i] / leadingRaceProgress;
                    double displayPosition = 0;
                    double trailingScale = 1;
                    if (fractionOfLead > 0.5)
                        displayPosition = (fractionOfLead - 0.5) * 2;
                   // if (fractionOfLead > 0.7)
                   //     displayPosition = fractionOfLead / 0.6; // ~ 1 - ((x / 0.3) * 0.5)
                   // else if (fractionOfLead > 0.5)
                   //     displayPosition = (fractionOfLead - 0.3) / 0.4; // ~((x - 0.3) / 0.2) * 0.5
                    else
                        if (raceTimer.Elapsed.TotalSeconds > 1) //Don't start scaling at very start of race or it swings back and forth a bunch
                            trailingScale = Math.Max(fractionOfLead / 0.5, 0.2);    //Max at 20%, we don't want racer to get too small to see
                    //System.Console.Write("t-{0}, {1:0.00}, ", trailingRacers[i].dataSource.getSourceName(), trailerRaceProgress[i]);
                    trailingRacers[i].racePanel.moveDistance(displayPosition * leadingDisplayPosition, trailingScale);
                }
            }
        }
    }

    public class RacerDetails
    {
        public string racer_displayname
        {
            get
            {
                if (racerRecordInfo == null || String.IsNullOrWhiteSpace(racerRecordInfo.FirstName))
                    return dataSource.getSourceName();
                else
                    return String.Format("{0} on {1}", racerRecordInfo.FirstName, dataSource.getSourceName());
            }
        }

        public TrackRecords.RecordData racerRecordInfo = null;

        DataSourceBase _dataSource;
        public DataSourceBase dataSource
        {
            get
            {
                return _dataSource;
            }
        }

        public double targetValue;
        public double finishResult;

        public RacerInfoPanel racePanel;

        public RacerDetails(DataSourceBase dataSource, double targetValue)
        {
            changeRacerSource(dataSource, targetValue);            
        }

        public bool changeRacerSource(DataSourceBase dsb, double targetVal)
        {
            if (!dsb.Equals(dataSource))
            {
                if (dataSource != null && dataSource.isRunning())
                    return false;

                if (dsb.isInUse)
                    return false;

                if(dataSource != null)
                    dataSource.isInUse = false; //Make sure the old one is marked as free

                _dataSource = dsb;
                dsb.isInUse = true;
            }

            targetValue = targetVal;
            racePanel = new RacerInfoPanel(dataSource.getSportType(), targetVal);
            return true;
        }

        internal void finish(int order, targetType resultType, double resultValue)
        {
            dataSource.stop();
            dataSource.isInUse = false;

            //round the value to 0.1s since our update resolution from most data sources is at least 250ms
            finishResult = Math.Round(resultValue, 1);            

            //Display finish 
            racePanel.Dispatcher.Invoke(new Action(() =>
                {
                    racePanel.finishRace();
                    racePanel.makeAnnouncement(String.Format("Finished! - {0:0.0} {1}", finishResult, resultType));
                }));
        }
    }

    public enum racerSportType:byte
    {
        Biking = 0x00,
        Running = 0x01,
        Rowing = 0x02,
        Skiing = 0x03,
        Unknown = 0xFF,
    }

    public enum racerSourceType
    {
        TrackRecord,
        WorldRecordPace,
        FitEquip,
    }

    public enum targetType
    {
        Seconds,
        Meters,
    }
}
