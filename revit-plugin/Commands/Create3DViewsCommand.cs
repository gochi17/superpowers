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
            UIDocument? uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc is null)
            {
                message = "No active document.";
                return Result.Failed;
            }

            Document doc = uidoc.Document;

            List<Element> targets = ResolveTargets(uidoc, doc);

            if (targets.Count == 0)
            {
                TaskDialog.Show("No Elements",
                    "No rooms or generic model families were found.\n\n" +
                    "Select rooms / generic model instances before running.");
                return Result.Cancelled;
            }

            int count = 0;

            try
            {
                using var t = new Transaction(doc, "Create 3D Views");
                t.Start();
                count = new ThreeDViewService(doc).Create3DViews(targets);
                t.Commit();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            TaskDialog.Show("Done", $"Created {count} 3D view(s).");
            return Result.Succeeded;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static List<Element> ResolveTargets(UIDocument uidoc, Document doc)
        {
            List<Element> selection = uidoc.Selection
                .GetElementIds()
                .Select(id => doc.GetElement(id))
                .Where(IsSupported)
                .ToList();

            if (selection.Count > 0)
                return selection;

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
