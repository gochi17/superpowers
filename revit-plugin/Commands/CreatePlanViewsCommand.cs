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
                    "Either select rooms / generic model instances before running, " +
                    "or ensure at least one room is placed in the model.");
                return Result.Cancelled;
            }

            int count = 0;

            try
            {
                using var t = new Transaction(doc, "Create Plan Views");
                t.Start();
                count = new ViewCreationService(doc).CreatePlanViews(targets);
                t.Commit();
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            TaskDialog.Show("Done", $"Created {count} plan view(s).");
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

            // Fall back to all placed rooms.
            // OfClass(typeof(Room)) uses the concrete Room class — reliable in all Revit versions.
            // Area > 0 filters out unplaced room tags that carry no geometry.
            return new FilteredElementCollector(doc)
                .OfClass(typeof(Room))
                .Cast<Room>()
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
