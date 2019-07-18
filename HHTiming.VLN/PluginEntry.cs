using HHDev.ProjectFramework.Definitions;
using HHTiming.Core.Definitions.UIUpdate.Interfaces;
using HHTiming.DAL;
using HHTiming.Desktop.Definitions.PlugInFramework;
using HHTiming.VLN.PitStopScoreboard;
using HHTiming.VLN.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HHTiming.VLN
{
    public class PluginEntry : IHHTimingPlugin
    {
        public string Name => "VLN Plugin";

        public Guid PluginID => Guid.Parse("045b6783-9385-4903-835b-3ccbf10a5dfe");

        public IOptionsObject Options => null;

        public Func<IHHTimingContext> HHTimingContextFactory { set { } }
        public Func<string, IProjectObject> OpenProjectObject { set { } }

        public bool LoadSuccessful => true;

        public List<LapNumericTagItemDefinition> LapNumericTagsDefinitions => null;

        public event EventHandler<NewWorksheetEventArgs> AddNewWorksheet;
        public event EventHandler<CreateNewProjectObjectEventArgs> AddNewProjectItem;

        public PluginEntry()
        {
            BuildRibbonBar();
        }

        public List<IUIUpdateControl> GetAllBackgroundUIUpdateControls()
        {
            return null;
        }

        public List<Type> GetDataImporters()
        {
            return null;
        }

        public List<MessageParserDefinition> GetMessageParsers()
        {
            return null;
        }

        public IProjectObjectManager GetProjectObjectManager()
        {
            return null;
        }

        public List<HHRibbonTab> GetRibbonTabs() => _ribbonTabs;

        public IWorksheetControlManager GetWorksheetControlManager()
        {
            return null;
        }

        public void SoftwareClosing()
        {
            
        }

        private List<HHRibbonTab> _ribbonTabs;
        private void BuildRibbonBar()
        {
            _ribbonTabs = new List<HHRibbonTab>();

            var tab1 = new HHRibbonTab("VLN");
            _ribbonTabs.Add(tab1);

            var bar1 = new HHRibbonBar("Scoreboards");
            tab1.Bars.Add(bar1);

            var addPitStopScoreboardButton = new HHRibbonButton("Pit Stop Scoreboard", Resources.Scoreboard_48x48, (x) =>
            {
                AddNewWorksheet?.Invoke(this, new NewWorksheetEventArgs() { NewWorksheet = new PitStopScoreboardDisplay(), TargetWorkbook = x });
            });
            bar1.Buttons.Add(addPitStopScoreboardButton);
        }
    }
}
