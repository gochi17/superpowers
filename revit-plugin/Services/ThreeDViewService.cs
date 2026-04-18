using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Creates isometric 3D views with a section box fitted to each element's bounding box.
    /// The camera is oriented to a standard SE-upper-front isometric angle with a
    /// mathematically correct perpendicular up vector.
    /// All public methods must be called inside an open Transaction.
    /// </summary>
    public class ThreeDViewService(Document doc)
    {
        private const double SectionBoxPaddingFt = 1.0; // feet of padding on every side

        public int Create3DViews(IEnumerable<Element> elements)
        {
            ViewFamilyType vft = GetViewFamilyType(ViewFamily.ThreeDimensional)
                ?? throw new InvalidOperationException(
                       "No 3D view family type found in the document.");

            int count = 0;

            foreach (Element el in elements)
            {
                BoundingBoxXYZ? bbox = el.get_BoundingBox(null);
                if (bbox is null) continue;

                View3D view = View3D.CreateIsometric(doc, vft.Id);
                view.Name = UniqueViewName($"3D - {ElementLabel(el)}");

                // Padded section box
                BoundingBoxXYZ sectionBox = new()
                {
                    Min = new XYZ(bbox.Min.X - SectionBoxPaddingFt,
                                  bbox.Min.Y - SectionBoxPaddingFt,
                                  bbox.Min.Z - SectionBoxPaddingFt),
                    Max = new XYZ(bbox.Max.X + SectionBoxPaddingFt,
                                  bbox.Max.Y + SectionBoxPaddingFt,
                                  bbox.Max.Z + SectionBoxPaddingFt)
                };

                view.SetSectionBox(sectionBox);
                view.IsSectionBoxActive = true;
                view.SetOrientation(BuildIsometricOrientation(sectionBox));

                count++;
            }

            return count;
        }

        // ── Orientation ──────────────────────────────────────────────────────

        /// <summary>
        /// Computes a ViewOrientation3D for a standard SE-upper-front isometric angle.
        ///
        /// The eye looks from the (+X, -Y, +Z) octant toward the bounding-box centre.
        /// Because the world Z-axis is not perpendicular to that look direction, the
        /// true camera-up vector is derived by projecting Z onto the plane perpendicular
        /// to the forward vector: up = normalize(Z − (Z·forward)·forward).
        /// This guarantees the required orthogonality constraint and avoids a Revit API
        /// ArgumentException at runtime.
        /// </summary>
        private static ViewOrientation3D BuildIsometricOrientation(BoundingBoxXYZ sectionBox)
        {
            XYZ center = new(
                (sectionBox.Min.X + sectionBox.Max.X) / 2.0,
                (sectionBox.Min.Y + sectionBox.Max.Y) / 2.0,
                (sectionBox.Min.Z + sectionBox.Max.Z) / 2.0);

            double size = Math.Max(
                Math.Max(sectionBox.Max.X - sectionBox.Min.X,
                         sectionBox.Max.Y - sectionBox.Min.Y),
                sectionBox.Max.Z - sectionBox.Min.Z);

            // Place the eye in the (+X, -Y, +Z) diagonal — standard "right-front-top"
            XYZ eyeDir  = new XYZ(1, -1, 1).Normalize();
            XYZ eye     = center + eyeDir * (size * 2.5 + 5.0);

            // Forward = from eye toward scene centre
            XYZ forward = (center - eye).Normalize();

            // Project world Z onto the plane perpendicular to forward
            XYZ worldZ  = XYZ.BasisZ;
            double dot  = worldZ.DotProduct(forward);
            XYZ upProj  = new XYZ(
                worldZ.X - dot * forward.X,
                worldZ.Y - dot * forward.Y,
                worldZ.Z - dot * forward.Z);

            XYZ up = upProj.Normalize();

            return new ViewOrientation3D(eye, up, forward);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

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
