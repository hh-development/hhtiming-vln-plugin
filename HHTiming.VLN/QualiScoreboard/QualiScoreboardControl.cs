using HHDev.Core.NETStandard.Extensions;
using HHTiming.Core.Definitions;
using HHTiming.Core.Definitions.Enums;
using HHTiming.Core.Definitions.UIUpdate.Implementations.Messages;
using HHTiming.Core.Definitions.UIUpdate.Interfaces;
using HHTiming.DataCache;
using HHTiming.WinFormsControls.ElementConfiguration;
using HHTiming.WinFormsControls.Filtering;
using HHTiming.WinFormsControls.Scoreboards;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HHTiming.VLN.QualiScoreboard
{
    public class QualiScoreboardControl :
        BaseScoreboardControl<QualiScoreboardDataContainer>
    {
        private AdvBindingList<QualiScoreboardDataContainer> myList;
        private ScoreboardDGV<QualiScoreboardDataContainer> myDGV;
        private UICarManager<QualiScoreboardDataContainer> myUICarManager;

        protected object _updateLock = new object();
        protected Timer _updateTimer = new Timer();
        protected List<string> _updatedCars = new List<string>();

        private double _sessionLength = 21600;

        protected override QualiScoreboardDataContainer GetNewDataContainer() => null;

        public QualiScoreboardControl()
        {
            myList = new AdvBindingList<QualiScoreboardDataContainer>(new DefaultScoreboardSorter(ListSortDirection.Ascending), nameof(QualiScoreboardDataContainer.Position), new QualiScoreboardDataContainer("", Color.White, ""));
            myUICarManager = new UICarManager<QualiScoreboardDataContainer>(Color.White, myList, (x) => new QualiScoreboardDataContainer(x.ItemID, x.CarColor, x.CategoryString));
            myDGV = new ScoreboardDGV<QualiScoreboardDataContainer>(myList, "\\g", (x) => myUICarManager.GetCategoryColour(x), true, false, false);

            base.SetListAndDGV(myList, myDGV);
            base.SetAppearanceOptions();
            base.InitializeFiltering();

            _updateTimer = new Timer();
            _updateTimer.Tick += _updateTimer_Tick;
            _updateTimer.Interval = 5000;
            _updateTimer.Start();
        }

        private void _updateTimer_Tick(object sender, EventArgs e)
        {
            _updateTimer.Stop();

            lock (_updateLock)
            {
                if (_updatedCars.Count > 0)
                {
                    foreach (string car in _updatedCars)
                    {
                        HandleCarUpated(car);
                    }
                }
                
                _updatedCars.Clear();
            }

            _updateTimer.Start();
        }

        private void HandleCarUpated(string aCarId)
        {
            if (DataManager.Instance.Cars.ContainsKey(aCarId))
            {
                var sectorTimes = new Dictionary<int, List<double>>();

                var car = DataManager.Instance.Cars[aCarId];
                var stints = new List<Stint>();

                foreach (var stint in car.Stints)
                {
                    if (stint.Value.TotalNumberOfLaps > 5)
                    {
                        foreach (var lap in stint.Value.Laps)
                        {
                            foreach (var sector in lap.Value.Sectors)
                            {
                                if (!sectorTimes.ContainsKey(sector.Value.Index))
                                {
                                    sectorTimes.Add(sector.Value.Index, new List<double>());
                                }

                                if (sector.Value.Time == -1 || sector.Value.Time == double.MaxValue)
                                {
                                    continue;
                                }
                                else
                                {
                                    sectorTimes[sector.Value.Index].Add(sector.Value.Time);
                                }
                            }
                        }
                    }
                }

                var row = myList.OriginalList.FirstOrDefault(x => x.CarID == aCarId);
                if (row == null) return;

                double runningLapTime = 0.0;

                foreach (var item in sectorTimes)
                {
                    if (item.Value.Count >= 5)
                    {
                        var sortedSectors = item.Value.OrderBy(x => x);
                        var top5Sectors = sortedSectors.Take(5);
                        var average = top5Sectors.Average();

                        runningLapTime += average;
                    }
                    else
                    {
                        runningLapTime = 0;
                        break;
                    }
                }

                if (runningLapTime > 0)
                {
                    UpdateTimeDataTypeValue(row.QualifyingTime, row, nameof(QualiScoreboardDataContainer.QualifyingTime), runningLapTime);
                }
            }
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
            else if (anUpdateMessage is DataManagerUIUpdateMessage dmMsg)
            {
                HandleDataManagerUIUpdateMessage(dmMsg);
            }
            
        }

        private void HandleDataManagerUIUpdateMessage(DataManagerUIUpdateMessage dmUpdateMsg)
        {
            lock (_updateLock)
            {
                foreach (var carId in dmUpdateMsg.AffectedCars)
                {
                    if (!_updatedCars.Contains(carId)) _updatedCars.Add(carId);
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
