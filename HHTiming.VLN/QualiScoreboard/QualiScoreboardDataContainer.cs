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

namespace HHTiming.VLN.QualiScoreboard
{
    public class QualiScoreboardDataContainer :
          BaseUICar,
        IIdentifiableDataContainer,
        IHighlightRow,
        IFilterableItem,
        INotifyColumnChanged
    {
        [Browsable(false)]
        public bool BlockUpdates { get; set; } = false;

        public TimeDataType Position { get; set; } = new TimeDataType(nameof(Position), true, false, false, false, "", 0, false, false, false, 1, true);

        public string CarNumber
        {
            get { return myCarID; }
            set { myCarID = value; }
        }

        public string TeamName { get; set; }
        public string CarMake { get; set; }

        public string Category
        {
            get { return myCategoryID; }
            set { myCategoryID = value; }
        }

        [DisplayName("Qualifying Time")]
        public TimeDataType QualifyingTime { get; set; } = new TimeDataType(nameof(QualifyingTime), false, true, true, true, "0.000", RefDisplayMode.Value, false, false, false, 1, false);

        [Browsable(false)]
        public string ItemID
        {
            get { return CarNumber; }
        }

        [Browsable(false)]
        public string SecondarySortID
        {
            get { return Position.ToString(); }
        }

        public QualiScoreboardDataContainer(string aCarID, Color aColor, string aCategory) : base(aCarID, aColor, aCategory, "")
        {

        }

        [Browsable(false)]
        public string CarID
        {
            get { return myCarID; }
        }

        [Browsable(false)]
        public string CategoryID
        {
            get { return myCategoryID; }
        }

        [Browsable(false)]
        public bool IsClassLeader
        {
            get { return false; }
        }

        [Browsable(false)]
        public bool IsRowHightLighted { get; set; }

        [Browsable(false)]
        public Color RowColour
        {
            get { return myCarColour; }

            set { }
        }

        public event NotifyColumnChangedEventEventHandler NotifyColumnChangedEvent;
        public void RaiseNotifyColumnChangedEvent(string aName)
        {
            NotifyColumnChangedEvent?.Invoke(aName);
        }

        [Browsable(false)]
        public double BestTheoBySector { get; set; } = double.MaxValue;
    }
    }
