using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Creates cropped floor-plan views sized to the bounding box of rooms or generic model families.
    /// All public methods must be called inside an open Transaction.
    /// </summary>
    public class ViewCreationService(Document doc)
    {
        private const double CropPaddingFt = 1.5; // feet of padding around each bounding box

        public int CreatePlanViews(IEnumerable<Element> elements)
        {
            ViewFamilyType vft = GetViewFamilyType(ViewFamily.FloorPlan)
                ?? throw new InvalidOperationException(
                       "No Floor Plan view family type found in the document.");

            int count = 0;

            foreach (Element el in elements)
            {
                BoundingBoxXYZ? bbox = el.get_BoundingBox(null);
                if (bbox is null) continue;

                ElementId levelId = ResolveLevelId(el);
                if (levelId == ElementId.InvalidElementId) continue;

                ViewPlan view = ViewPlan.Create(doc, vft.Id, levelId);
                view.Name = UniqueViewName($"Plan - {ElementLabel(el)}");

                // Padded crop box — Z extents follow the bounding box so the view
                // covers the element's full height in section; Revit ignores Z for
                // plan projection but the extents are still required to be valid.
                view.CropBox = new BoundingBoxXYZ
                {
                    Min = new XYZ(bbox.Min.X - CropPaddingFt, bbox.Min.Y - CropPaddingFt, bbox.Min.Z),
                    Max = new XYZ(bbox.Max.X + CropPaddingFt, bbox.Max.Y + CropPaddingFt, bbox.Max.Z)
                };

                view.CropBoxActive  = true;
                view.CropBoxVisible = true;

                count++;
            }

            return count;
        }

        // ── Internals ────────────────────────────────────────────────────────

        private ViewFamilyType? GetViewFamilyType(ViewFamily family) =>
            new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == family);

        private static ElementId ResolveLevelId(Element el)
        {
            if (el is Room room)
                return room.LevelId;

            if (el.LevelId != ElementId.InvalidElementId)
                return el.LevelId;

            // Family instances may store their level in either of these built-in parameters
            Parameter? p = el.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                          ?? el.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

            return p?.AsElementId() ?? ElementId.InvalidElementId;
        }

        private static string ElementLabel(Element el) =>
            el is Room room
                ? $"{room.Name} {room.Number}".Trim()
                : string.IsNullOrWhiteSpace(el.Name) ? el.Id.ToString() : el.Name;

        private string UniqueViewName(string baseName)
        {
            HashSet<string> existing = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Select(v => v.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (!existing.Contains(baseName))
                return baseName;

            for (int i = 2; ; i++)
            {
                string candidate = $"{baseName} ({i})";
                if (!existing.Contains(candidate))
                    return candidate;
            }
        }
    }
}
