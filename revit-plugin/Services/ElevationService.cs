using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Places an ElevationMarker at the centroid of each element's bounding box and generates
    /// four ViewSection elevations at indices 0-3 (East · North · West · South for an
    /// unrotated marker in a North-up plan view).
    /// All public methods must be called inside an open Transaction.
    /// </summary>
    public class ElevationService(Document doc)
    {
        private const int    DefaultScale       = 50;   // 1:50
        private const double FarClipPaddingFt   = 1.0;  // extra depth beyond opposite wall

        // Marker index → label.
        // Revit assigns indices clockwise starting from the right (East) for a 0° marker.
        private static readonly string[] DirectionLabels = ["East", "North", "West", "South"];

        public int CreateElevations(IEnumerable<Element> elements, ViewPlan hostPlanView)
        {
            ViewFamilyType vft = GetViewFamilyType(ViewFamily.Elevation)
                ?? throw new InvalidOperationException(
                       "No Elevation view family type found in the document.");

            int count = 0;

            foreach (Element el in elements)
            {
                BoundingBoxXYZ? bbox = el.get_BoundingBox(null);
                if (bbox is null) continue;

                XYZ center = BBoxCenter(bbox);

                ElevationMarker marker = ElevationMarker.CreateElevationMarker(
                    doc, vft.Id, center, DefaultScale);

                string label = ElementLabel(el);

                for (int i = 0; i < 4; i++)
                {
                    ViewSection elev = marker.CreateElevation(doc, hostPlanView.Id, i);
                    elev.Name = UniqueViewName($"Elev - {label} - {DirectionLabels[i]}");
                    AdjustFarClip(elev, bbox, i);
                }

                count++;
            }

            return count;
        }

        // ── Internals ────────────────────────────────────────────────────────

        /// <summary>
        /// Sets the far-clip offset to the width/depth of the bounding box in the
        /// camera's look direction so the entire room is visible in the elevation.
        /// </summary>
        private static void AdjustFarClip(ViewSection view, BoundingBoxXYZ bbox, int dirIndex)
        {
            // Indices 0/2 = East/West: depth measured along X axis
            // Indices 1/3 = North/South: depth measured along Y axis
            double depth = dirIndex switch
            {
                0 or 2 => Math.Abs(bbox.Max.X - bbox.Min.X) + FarClipPaddingFt,
                _      => Math.Abs(bbox.Max.Y - bbox.Min.Y) + FarClipPaddingFt
            };

            Parameter? far = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
            far?.Set(depth);
        }

        private static XYZ BBoxCenter(BoundingBoxXYZ b) =>
            new((b.Min.X + b.Max.X) / 2.0,
                (b.Min.Y + b.Max.Y) / 2.0,
                (b.Min.Z + b.Max.Z) / 2.0);

        private ViewFamilyType? GetViewFamilyType(ViewFamily family) =>
            new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .FirstOrDefault(v => v.ViewFamily == family);

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
