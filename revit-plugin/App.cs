using Autodesk.Revit.UI;
using System.Reflection;

namespace SuperpowersRevit
{
    public class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication app)
        {
            const string tab = "Superpowers";

            try { app.CreateRibbonTab(tab); }
            catch { /* tab already registered */ }

            string dll = Assembly.GetExecutingAssembly().Location;

            // ── Views panel ─────────────────────────────────────────────────
            RibbonPanel viewPanel = app.CreateRibbonPanel(tab, "Views");

            viewPanel.AddItem(new PushButtonData(
                name:        "CreatePlanViews",
                text:        "Plan\nViews",
                assemblyName: dll,
                className:   "SuperpowersRevit.Commands.CreatePlanViewsCommand")
            {
                ToolTip = "Create a cropped floor-plan view for each selected room or generic model family.",
                LongDescription =
                    "Select rooms or generic model instances before running. " +
                    "With nothing selected, all placed rooms are processed. " +
                    "Each element receives a dedicated floor-plan view cropped to its bounding box."
            });

            viewPanel.AddItem(new PushButtonData(
                name:        "CreateElevations",
                text:        "Elevations",
                assemblyName: dll,
                className:   "SuperpowersRevit.Commands.CreateElevationsCommand")
            {
                ToolTip = "Create four interior elevations (N / S / E / W) for each selected room or family.",
                LongDescription =
                    "Must be run while a floor-plan view is active. " +
                    "An ElevationMarker is placed at the centre of each element's bounding box " +
                    "and four ViewSection elevations are generated."
            });

            viewPanel.AddItem(new PushButtonData(
                name:        "Create3DViews",
                text:        "3D Views",
                assemblyName: dll,
                className:   "SuperpowersRevit.Commands.Create3DViewsCommand")
            {
                ToolTip = "Create an isometric 3D view with a fitted section box for each selected element."
            });

            // ── Drofus panel ─────────────────────────────────────────────────
            RibbonPanel drofusPanel = app.CreateRibbonPanel(tab, "Drofus");

            drofusPanel.AddItem(new PushButtonData(
                name:        "DrofusData",
                text:        "Drofus\nData",
                assemblyName: dll,
                className:   "SuperpowersRevit.Commands.DrofusDataCommand")
            {
                ToolTip = "Log in to Drofus, browse project rooms and items, and write data back to the model."
            });

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication app) => Result.Succeeded;
    }
}
