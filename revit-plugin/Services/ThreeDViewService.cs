using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace SuperpowersRevit.Services
{
    /// <summary>
    /// Creates isometric 3D views with a section box fitted to each element's bounding box.
    /// Must be called inside an open Transaction.
    /// </summary>
    public class ThreeDViewService(Document doc)
    {
        // Padding in feet added to each side of the section box
        private const double SectionBoxPaddingFt = 1.0;

        public int Create3DViews(IEnumerable<Element> elements)
        {
            ViewFamilyType? vft = GetViewFamilyType(ViewFamily.ThreeDimensional);
            if (vft is null)
                throw new InvalidOperationException("No 3D view family type found in document.");

            int count = 0;

            foreach (Element el in elements)
            {
                BoundingBoxXYZ? bbox = el.get_BoundingBox(null);
                if (bbox is null) continue;

                View3D view = View3D.CreateIsometric(doc, vft.Id);
                view.Name = UniqueViewName($"3D - {ElementLabel(el)}");

                // Expand bounding box for section box
                BoundingBoxXYZ sectionBox = new()
                {
                    Min = new XYZ(
                        bbox.Min.X - SectionBoxPaddingFt,
                        bbox.Min.Y - SectionBoxPaddingFt,
                        bbox.Min.Z - SectionBoxPaddingFt),
                    Max = new XYZ(
                        bbox.Max.X + SectionBoxPaddingFt,
                        bbox.Max.Y + SectionBoxPaddingFt,
                        bbox.Max.Z + SectionBoxPaddingFt)
                };

                view.SetSectionBox(sectionBox);
                view.IsSectionBoxActive = true;

                // Orient to a standard isometric angle (from upper-right-front)
                var orientation = new ViewOrientation3D(
                    eye: new XYZ(
                        sectionBox.Max.X + 10,
                        sectionBox.Min.Y - 10,
                        sectionBox.Max.Z + 10),
                    up: XYZ.BasisZ,
                    forward: new XYZ(-1, 1, -1).Normalize());

                view.SetOrientation(orientation);

                count++;
            }

            return count;
        }

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
