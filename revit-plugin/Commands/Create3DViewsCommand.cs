using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using SuperpowersRevit.Services;

namespace SuperpowersRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Create3DViewsCommand : IExternalCommand
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
                    "Select rooms / generic model instances before running.");
                return Result.Cancelled;
            }

            var service = new ThreeDViewService(doc);
            int count = 0;

            using (var t = new Transaction(doc, "Create 3D Views"))
            {
                t.Start();
                count = service.Create3DViews(targets);
                t.Commit();
            }

            TaskDialog.Show("Done", $"Created {count} 3D view(s).");
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
