using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Places an ElevationMarker at the centroid of each element's bounding box and creates
    /// four interior elevation views (North, South, East, West — indices 0-3).
    /// Must be called inside an open Transaction.
    /// The host plan view must be passed in; Revit requires it when creating elevations.
    /// </summary>
    public class ElevationService(Document doc)
    {
        // Default drawing scale denominator (1 : Scale)
        private const int DefaultScale = 50;

        // Depth of the elevation view beyond the opposite wall (feet)
        private const double FarClipOffsetFt = 0.5;

        public int CreateElevations(IEnumerable<Element> elements, ViewPlan hostPlanView)
        {
            ViewFamilyType? vft = GetViewFamilyType(ViewFamily.Elevation);
            if (vft is null)
                throw new InvalidOperationException("No Elevation view family type found in document.");

            int count = 0;

            foreach (Element el in elements)
            {
                BoundingBoxXYZ? bbox = el.get_BoundingBox(null);
                if (bbox is null) continue;

                XYZ center = BBoxCenter(bbox);

                ElevationMarker marker = ElevationMarker.CreateElevationMarker(
                    doc, vft.Id, center, DefaultScale);

                string label = ElementLabel(el);

                // Indices 0-3 correspond to the four cardinal directions as placed by Revit.
                // The actual compass direction depends on True North rotation; labels here use
                // the conventional marker indices (Right / Top / Left / Bottom in plan).
                string[] dirNames = ["East", "North", "West", "South"];

                for (int i = 0; i < 4; i++)
                {
                    ViewSection elev = marker.CreateElevation(doc, hostPlanView.Id, i);
                    elev.Name = UniqueViewName($"Elev - {label} - {dirNames[i]}");

                    // Extend far clip to reach the opposite side of the bounding box
                    SetFarClip(elev, bbox, i);
                }

                count++;
            }

            return count;
        }

        private static void SetFarClip(ViewSection elev, BoundingBoxXYZ bbox, int dirIndex)
        {
            // Estimate depth from bounding box extents
            double depth = dirIndex switch
            {
                0 or 2 => Math.Abs(bbox.Max.X - bbox.Min.X) + FarClipOffsetFt, // East / West
                _ => Math.Abs(bbox.Max.Y - bbox.Min.Y) + FarClipOffsetFt        // North / South
            };

            Parameter? farClip = elev.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
            farClip?.Set(depth);
        }

        private static XYZ BBoxCenter(BoundingBoxXYZ b) =>
            new((b.Min.X + b.Max.X) / 2.0,
                (b.Min.Y + b.Max.Y) / 2.0,
                (b.Min.Z + b.Max.Z) / 2.0);

        private ViewFamilyType? GetViewFamilyType(ViewFamily family) =>
            new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(vft => vft.ViewFamily == family);

        private static string ElementLabel(Element el) =>
            el is Room room
                ? $"{room.Name} {room.Number}".Trim()
                : string.IsNullOrWhiteSpace(el.Name) ? el.Id.ToString() : el.Name;

        private string UniqueViewName(string baseName)
        {
            var existing = new FilteredElementCollector(doc)
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
