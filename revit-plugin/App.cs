using Autodesk.Revit.UI;
using System.Reflection;

namespace SuperpowersRevit
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            const string tabName = "Superpowers";

            try { app.CreateRibbonTab(tabName); }
            catch { /* tab already exists */ }

            string dll = Assembly.GetExecutingAssembly().Location;

            RibbonPanel viewPanel = app.CreateRibbonPanel(tabName, "Views");

            viewPanel.AddItem(new PushButtonData(
                "CreatePlanViews",
                "Plan\nViews",
                dll,
                "SuperpowersRevit.Commands.CreatePlanViewsCommand")
            {
                ToolTip = "Create plan views from selected rooms or generic model families.",
                LongDescription =
                    "Select rooms or generic model family instances before running. " +
                    "If nothing is selected all placed rooms are used. " +
                    "Each element gets a cropped floor-plan view sized to its bounding box."
            });

            viewPanel.AddItem(new PushButtonData(
                "CreateElevations",
                "Elevations",
                dll,
                "SuperpowersRevit.Commands.CreateElevationsCommand")
            {
                ToolTip = "Generate N/S/E/W interior elevations for selected rooms or families."
            });

            viewPanel.AddItem(new PushButtonData(
                "Create3DViews",
                "3D Views",
                dll,
                "SuperpowersRevit.Commands.Create3DViewsCommand")
            {
                ToolTip = "Create an isometric 3D view with a section box fitted to each selected element."
            });

            RibbonPanel drofusPanel = app.CreateRibbonPanel(tabName, "Drofus");

            drofusPanel.AddItem(new PushButtonData(
                "DrofusData",
                "Drofus\nData",
                dll,
                "SuperpowersRevit.Commands.DrofusDataCommand")
            {
                ToolTip = "Connect to a Drofus account and pull room/item data into the model."
            });

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }
}
