using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using SuperpowersRevit.Services;

namespace SuperpowersRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreateElevationsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            if (uidoc.ActiveView is not ViewPlan planView)
            {
                TaskDialog.Show("Wrong View",
                    "Elevations must be created from a floor plan view.\n" +
                    "Open a floor plan view and run this command again.");
                return Result.Cancelled;
            }

            var targets = ResolveTargets(uidoc, doc);

            if (targets.Count == 0)
            {
                TaskDialog.Show("No Elements",
                    "No rooms or generic model families found.\n" +
                    "Select rooms / generic model instances in a plan view before running.");
                return Result.Cancelled;
            }

            var service = new ElevationService(doc);
            int count = 0;

            using (var t = new Transaction(doc, "Create Elevations"))
            {
                t.Start();
                count = service.CreateElevations(targets, planView);
                t.Commit();
            }

            TaskDialog.Show("Done", $"Created {count * 4} elevation view(s) ({count} set(s) of 4).");
            return Result.Succeeded;
        }

        private static List<Element> ResolveTargets(UIDocument uidoc, Document doc)
        {
            var selected = uidoc.Selection.GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(IsSupported)
                .ToList();

            if (selected.Count > 0)
                return selected;

            return new FilteredElementCollector(doc)
                .OfClass(typeof(SpatialElement))
                .Cast<SpatialElement>()
                .OfType<Room>()
                .Where(r => r.Area > 0)
                .Cast<Element>()
                .ToList();
        }

        private static bool IsSupported(Element el) =>
            el is Room ||
            (el is FamilyInstance fi &&
             fi.Category?.BuiltInCategory == BuiltInCategory.OST_GenericModel);
    }
}
