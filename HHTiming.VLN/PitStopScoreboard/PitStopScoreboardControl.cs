using HHDev.Core.NETStandard.Extensions;
using HHTiming.Core.Definitions;
using HHTiming.Core.Definitions.Enums;
using HHTiming.Core.Definitions.UIUpdate.Implementations.Messages;
using HHTiming.Core.Definitions.UIUpdate.Interfaces;
using HHTiming.WinFormsControls.ElementConfiguration;
using HHTiming.WinFormsControls.Filtering;
using HHTiming.WinFormsControls.Scoreboards;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHTiming.VLN.PitStopScoreboard
{
    public class PitStopScoreboardControl :
        BaseScoreboardControl<PitStopScoreboardDataContainer>
    {
        private AdvBindingList<PitStopScoreboardDataContainer> myList;
        private ScoreboardDGV<PitStopScoreboardDataContainer> myDGV;
        private UICarManager<PitStopScoreboardDataContainer> myUICarManager;

        private double _sessionLength = 21600;

        protected override PitStopScoreboardDataContainer GetNewDataContainer() => null;

        public PitStopScoreboardControl()
        {
            myList = new AdvBindingList<PitStopScoreboardDataContainer>(new DefaultScoreboardSorter(ListSortDirection.Ascending), nameof(PitStopScoreboardDataContainer.Position), new PitStopScoreboardDataContainer("", Color.White, ""));
            myUICarManager = new UICarManager<PitStopScoreboardDataContainer>(Color.White, myList, (x) => new PitStopScoreboardDataContainer(x.ItemID, x.CarColor, x.CategoryString));
            myDGV = new ScoreboardDGV<PitStopScoreboardDataContainer>(myList, "\\g", (x) => myUICarManager.GetCategoryColour(x), true, false, false);

            base.SetListAndDGV(myList, myDGV);
            base.SetAppearanceOptions();
            base.InitializeFiltering();
        }

        #region Message Handling

        public void HandleUIUpdateMessage(IUIUpdateMessage anUpdateMessage)
        {
            if (anUpdateMessage is BulkRefreshDataUIUpdateMessage)
            {
                var b = (BulkRefreshDataUIUpdateMessage)anUpdateMessage;

                foreach (var item in b.ListOfUIUpdateMessages)
                {
                    HandleUIUpdateMessage(item, false);
                }

                myDGV.AutoResizeRows();
                ResetBS();

            }
            else
            {
                HandleUIUpdateMessage(anUpdateMessage, true);
            }
        }

        public void HandleUIUpdateMessage(IUIUpdateMessage anUpdateMessage, bool anAllowRefresh)
        {
            if (anUpdateMessage is CarUIUpdateMessage)
            {
                myUICarManager.HandleCarUIUpdateMessage((CarUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is TrackUIUpdateMessage)
            {
                base.HandleTrackUIUpdateMessage((TrackUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is PositionUIUpdateMessage)
            {
                PositionUIUpdateMessage m = (PositionUIUpdateMessage)anUpdateMessage;

                foreach (PitStopScoreboardDataContainer item in myList.OriginalList)
                {
                    if (item.CarID == m.ItemID)
                    {
                        if (m.PosType == PositionUIUpdateMessage.PositionType.Overall)
                        {
                            UpdateTimeDataTypeValue(item.Position, item, nameof(item.Position), m.Position);
                        }
                        else
                        {
                            item.PositionInClass = m.Position;
                        }

                        return;

                    }

                }

            }
            else if (anUpdateMessage is CategoryUIUpdateMessage)
            {
                HandleCategoryUIUpdateMessage((CategoryUIUpdateMessage)anUpdateMessage);
            }
            else if (anUpdateMessage is ResetUIUpdateMessage)
            {
                base.HandleResetUIUpdateMessage();
                myUICarManager.Reset();
            }
            else if (anUpdateMessage is UserOptionsUIUpdateMessage)
            {
                HandleOptionsUIMessage((UserOptionsUIUpdateMessage)anUpdateMessage);
            }

            else if (anUpdateMessage is LapUIUpdateMessage lapMsg)
                HandleLapUIUpdateMessage(lapMsg);

            else if (anUpdateMessage is SectorUIUpdateMessage sectorMsg)
                HandleSectorUIUpdateMessage(sectorMsg);

            else if (anUpdateMessage is SessionStatusUIUpdateMessage sessionMsg)
                HandleSessionStatusUIUpdateMessage(sessionMsg);

            else if (anUpdateMessage is UserDefinedSessionLengthUIUpdateMessage sessionLengthMsg)
                HandleUserDefinedSessionLengthUIUpdateMessage(sessionLengthMsg);

            else if (anUpdateMessage is CarStatusUIUpdateMessage carStatusMsg)
                HandleCarStatusUIUpdateMessage(carStatusMsg);
        }

        private void HandleCarStatusUIUpdateMessage(CarStatusUIUpdateMessage carStatusMsg)
        {
            if (carStatusMsg.CarStatus == eCarStatus.PitIn)
            {
                var row = myList.OriginalList.FirstOrDefault(x => x.ItemID == carStatusMsg.ItemID);
                if (row == null) return;
                row.BlockUpdates = true;
            }
            else if (carStatusMsg.CarStatus == eCarStatus.PitOut)
            {
                var row = myList.OriginalList.FirstOrDefault(x => x.ItemID == carStatusMsg.ItemID);
                if (row == null) return;
                row.BlockUpdates = false;
                row.StintLength = 0;
                UpdateTimeDataTypeValue(row.MinimumPitStopTime, row, nameof(PitStopScoreboardDataContainer.MinimumPitStopTime), double.MaxValue);
            }
        }

        private void HandleUserDefinedSessionLengthUIUpdateMessage(UserDefinedSessionLengthUIUpdateMessage sessionLengthMsg)
        {
            _sessionLength = sessionLengthMsg.SessionLengthHours * 3600;
        }

        private void HandleSectorUIUpdateMessage(SectorUIUpdateMessage sectorMsg)
        {
            if (sectorMsg.StintNumber < 0) return;
            if (!_mostRecentSessionTime.IsValid()) return;

            var stopTime = GetMinimumPitStopLength(sectorMsg.StintLapNumber, sectorMsg.StintNumber == 0, _mostRecentSessionTime, _sessionLength);

            var row = myList.OriginalList.FirstOrDefault(x => x.ItemID == sectorMsg.ItemID);
            if (row == null) return;
            if (row.BlockUpdates) return;

            base.UpdateTimeDataTypeValue(row.MinimumPitStopTime, row, nameof(PitStopScoreboardDataContainer.MinimumPitStopTime), stopTime);
            row.StintLength = sectorMsg.StintLapNumber;
        }

        private double _mostRecentSessionTime = double.MaxValue;

        private void HandleSessionStatusUIUpdateMessage(SessionStatusUIUpdateMessage sessionMsg)
        {
            _mostRecentSessionTime = sessionMsg.SessionTime;

            var timeRemaining = _sessionLength - _mostRecentSessionTime;
            if (timeRemaining > 0 && timeRemaining < 4200)
            {
                var pitStopTime = GetMinimumPitStopLength(0, false, _mostRecentSessionTime, _sessionLength);

                // for each car we should update the pit stop time AS LONG as it is not in the pits
                foreach (var item in myList.OriginalList)
                    if (!item.BlockUpdates)
                        base.UpdateTimeDataTypeValue(item.MinimumPitStopTime, item, nameof(PitStopScoreboardDataContainer.MinimumPitStopTime), pitStopTime);
            }
        }

        private void HandleLapUIUpdateMessage(LapUIUpdateMessage lapMsg)
        {
            if (lapMsg.StintNumber < 0) return;
            if (!_mostRecentSessionTime.IsValid()) return;

            var stopTime = GetMinimumPitStopLength(lapMsg.StintLapNumber + 1, lapMsg.StintNumber == 0, _mostRecentSessionTime, _sessionLength);

            var row = myList.OriginalList.FirstOrDefault(x => x.ItemID == lapMsg.ItemID);
            if (row == null) return;
            if (row.BlockUpdates) return;

            base.UpdateTimeDataTypeValue(row.MinimumPitStopTime, row, nameof(PitStopScoreboardDataContainer.MinimumPitStopTime), stopTime);
            row.StintLength = lapMsg.StintLapNumber + 1;
        }

        private double GetMinimumPitStopLength(int aNumberOfLapsInStint, bool anIsFirstStint, double aSessionTime, double aSessionLength)
        {
            var sessionTimeRemaining = aSessionLength - aSessionTime;
            if (sessionTimeRemaining < 4200)
            {
                // in this case we need to use table 2
                var sessionTimeRemainingWholeMinutes = (int)Math.Floor(sessionTimeRemaining / 60);
                switch (sessionTimeRemainingWholeMinutes)
                {
                    case 69: return 190;
                    case 68: return 188;
                    case 67: return 186;
                    case 66: return 183;
                    case 65: return 181;
                    case 64: return 178;
                    case 63: return 176;
                    case 62: return 174;
                    case 61: return 171;
                    case 60: return 169;
                    case 59: return 166;
                    case 58: return 164;
                    case 57: return 162;
                    case 56: return 159;
                    case 55: return 157;
                    case 54: return 154;
                    case 53: return 152;
                    case 52: return 150;
                    case 51: return 147;
                    case 50: return 145;
                    case 49: return 143;
                    case 48: return 140;
                    case 47: return 138;
                    case 46: return 135;
                    case 45: return 133;
                    case 44: return 131;
                    case 43: return 128;
                    case 42: return 126;
                    case 41: return 123;
                    case 40: return 121;
                    case 39: return 119;
                    case 38: return 116;
                    case 37: return 114;
                    case 36: return 111;
                    case 35: return 109;
                    case 34: return 107;
                    case 33: return 104;
                    case 32: return 102;
                    case 31: return 100;
                    case 30: return 97;
                    case 29: return 95;
                    case 28: return 92;
                    case 27: return 90;
                    case 26: return 88;
                    case 25: return 85;
                    case 24: return 83;
                    case 23: return 80;
                    case 22: return 78;
                    case 21: return 76;
                    case 20: return 73;
                    case 19: return 71;
                    case 18: return 68;
                    case 17: return 66;
                    case 16: return 64;
                    case 15: return 61;
                    case 14: return 59;
                    case 13: return 57;
                    case 12: return 54;
                    case 11: return 52;
                    case 10: return 49;
                    case 9: return 47;
                    case 8: return 45;
                    case 7: return 42;
                    case 6: return 40;
                    case 5: return 37;
                    case 4: return 35;
                    case 3: return 33;
                    case 2: return 30;
                    case 1: return 28;
                    default: return double.MaxValue;
                }
            }
            else
            {
                if (anIsFirstStint)
                {
                    switch (aNumberOfLapsInStint)
                    {
                        case 11: return 258;
                        case 10: return 238;
                        case 9: return 218;
                        case 8: return 196;
                        case 7: return 175;
                        case 6: return 156;
                        case 5: return 136;
                        case 4: return 116;
                        case 3: return 97;
                        case 2: return 77;
                        case 1: return 57;
                        //case 11: return 334;
                        //case 10: return 312;
                        //case 9: return 289;
                        //case 8: return 264;
                        //case 7: return 241;
                        //case 6: return 218;
                        //case 5: return 196;
                        //case 4: return 174;
                        //case 3: return 151;
                        //case 2: return 129;
                        //case 1: return 106;
                        default: return double.MaxValue;
                    }
                }
                else
                {
                    switch (aNumberOfLapsInStint)
                    {
                        case 11: return 249;
                        case 10: return 229;
                        case 9: return 210;
                        case 8: return 187;
                        case 7: return 166;
                        case 6: return 147;
                        case 5: return 127;
                        case 4: return 108;
                        case 3: return 88;
                        case 2: return 68;
                        case 1: return 49;
                        default: return double.MaxValue;
                    }
                }
            }
        }

        public void HandleCategoryUIUpdateMessage(CategoryUIUpdateMessage message)
        {
            myUICarManager.SetCategoryColour(message.ItemID, message.CategoryColour);
        }

        public void HandleOptionsUIMessage(UserOptionsUIUpdateMessage anOptionsUIMessage)
        {
            myDGV.HandleOptionsUIMessage(anOptionsUIMessage);
        }

        public override void HandleResetUIUpdateMessage()
        {
            base.HandleResetUIUpdateMessage();
            myUICarManager.Reset();
        }

        #endregion

    }
}
