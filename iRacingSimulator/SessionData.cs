﻿using System.Collections.Generic;
using System.Linq;
using iRacingSdkWrapper;
using iRacingSdkWrapper.Bitfields;
using iRacingSimulator.Drivers;

namespace iRacingSimulator
{
    public class SessionData
    {
        public SessionData()
        {
            this.ClassBestLaps = new Dictionary<int, BestLap>();
        }

        public Track Track { get; set; }
        public string EventType { get; set; }
        public int SubsessionId { get; set; }

        public double SessionTime { get; set; }
        public double TimeRemaining { get; set; }
        public int LeaderLap { get; set; }

        public bool TrackCleanup { get; set; }
        public bool DynamicTrack { get; set; }
        public TrackUsageTypes TrackUsage { get; set; }
        public string TrackUsageText { get; set; }

        public string RaceLaps { get; set; }
        public double RaceTime { get; set; }

        public Dictionary<int, BestLap> ClassBestLaps { get; set; }
        public BestLap OverallBestLap { get; set; }
        
        public SessionFlag Flags { get; set; }
        public SessionStates State { get; set; }
        public bool IsFinished { get; set; }

        public void Update(SessionInfo info)
        {
            this.Track = Track.FromSessionInfo(info);

            var weekend = info["WeekendInfo"];
            this.SubsessionId = Parser.ParseInt(weekend["SubSessionID"].GetValue());

            var session = info["SessionInfo"]["Sessions"]["SessionNum", Sim.Instance.CurrentSessionNumber];
            this.EventType = session["SessionType"].GetValue();

            this.TrackUsageText = session["SessionTrackRubberState"].GetValue();
            this.TrackUsage = TrackUsageFromString(this.TrackUsageText);

            this.TrackCleanup = weekend["TrackCleanup"].GetValue() == "1";
            this.DynamicTrack = weekend["TrackDynamicTrack"].GetValue() == "1";

            var laps = session["SessionLaps"].GetValue();
            var time = Parser.ParseSec(session["SessionTime"].GetValue());
            
            this.RaceLaps = laps;
            this.RaceTime = time;
        }

        public void Update(TelemetryInfo telemetry)
        {
            this.SessionTime = telemetry.SessionTime.Value;
            this.TimeRemaining = telemetry.SessionTimeRemain.Value;
            this.Flags = telemetry.SessionFlags.Value;
        }

        public void UpdateState(SessionStates state)
        {
            this.State = state;
            this.IsFinished = state == SessionStates.CoolDown;
        }

        public BestLap UpdateFastestLap(Laptime lap, Driver driver)
        {
            var classId = driver.Car.CarClassId;
            if (!this.ClassBestLaps.ContainsKey(classId))
            {
                this.ClassBestLaps.Add(classId, BestLap.Default);
            }

            if (lap.Value > 0 && this.ClassBestLaps[classId].Laptime.Value > lap.Value)
            {
                var bestlap = new BestLap(lap, driver);
                this.ClassBestLaps[classId] = bestlap;

                this.OverallBestLap =
                    this.ClassBestLaps.Values.Where(l => l.Laptime.Value > 0)
                        .OrderBy(l => l.Laptime.Value)
                        .FirstOrDefault();

                return bestlap;
            }
            return null;
        }

        public TrackUsageTypes TrackUsageFromString(string usage)
        {
            switch (usage.ToLower().Trim())
            {
                case "clean": return TrackUsageTypes.Clean;
                case "low usage": return TrackUsageTypes.Low;
                case "slight usage": return TrackUsageTypes.Slight;
                case "moderately low usage": return TrackUsageTypes.ModeratelyLow;
                case "moderate usage": return TrackUsageTypes.Moderate;
                case "moderately high usage": return TrackUsageTypes.ModeratelyHigh;
                case "high usage": return TrackUsageTypes.High;
                case "extensive usage": return TrackUsageTypes.Extensive;
                case "maximum usage": return TrackUsageTypes.Maximum;
            }
            return TrackUsageTypes.Unknown;
        }

        public enum TrackUsageTypes
        {
            Unknown,
            Clean,
            Slight,
            Low,
            ModeratelyLow,
            Moderate,
            ModeratelyHigh,
            High,
            Extensive,
            Maximum
        }
    }
}
