using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using SuperpowersRevit.Services;

namespace SuperpowersRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreatePlanViewsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            var targets = ResolveTargets(uidoc, doc);

            if (targets.Count == 0)
            {
                TaskDialog.Show("No Elements",
                    "No rooms or generic model families found.\n" +
                    "Place at least one room or select rooms / generic model instances before running.");
                return Result.Cancelled;
            }

            var service = new ViewCreationService(doc);
            int count = 0;

            using (var t = new Transaction(doc, "Create Plan Views"))
            {
                t.Start();
                count = service.CreatePlanViews(targets);
                t.Commit();
            }

            TaskDialog.Show("Done", $"Created {count} plan view(s).");
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

            // Fall back to all placed rooms
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
