using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Places an ElevationMarker at the plan centroid of each element's bounding box
    /// and generates four ViewSection interior elevations.
    ///
    /// Marker index → camera look direction (0° rotation, North-up plan view):
    ///   0 = East  (+X)   1 = North (+Y)   2 = West  (-X)   3 = South (-Y)
    ///
    /// All public methods must be called inside an open Transaction.
    /// </summary>
    public class ElevationService(Document doc)
    {
        private const int    DefaultScale     = 50;   // 1 : 50
        private const double FarClipPaddingFt = 1.0;  // extra depth past the far wall

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

                // Place the marker at the plan centroid (XY mid-point) at floor level (Min.Z).
                // Using Min.Z rather than the 3D centre ensures the marker sits on the floor
                // plane of the element, which is where Revit expects it relative to the plan
                // view's cut plane. Using the vertical midpoint can put the marker above the
                // plan view's cut height, causing Revit to silently skip marker creation.
                XYZ markerPosition = new(
                    (bbox.Min.X + bbox.Max.X) / 2.0,
                    (bbox.Min.Y + bbox.Max.Y) / 2.0,
                    bbox.Min.Z);

                ElevationMarker marker = ElevationMarker.CreateElevationMarker(
                    doc, vft.Id, markerPosition, DefaultScale);

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

        // ── Internals ─────────────────────────────────────────────────────────

        /// <summary>
        /// Extends the far-clip plane so the full room depth is visible.
        /// East/West elevations look along X → depth = X extent.
        /// North/South elevations look along Y → depth = Y extent.
        /// </summary>
        private static void AdjustFarClip(ViewSection view, BoundingBoxXYZ bbox, int dirIndex)
        {
            double depth = dirIndex switch
            {
                0 or 2 => Math.Abs(bbox.Max.X - bbox.Min.X) + FarClipPaddingFt,
                _      => Math.Abs(bbox.Max.Y - bbox.Min.Y) + FarClipPaddingFt
            };

            view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR)?.Set(depth);
        }

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
