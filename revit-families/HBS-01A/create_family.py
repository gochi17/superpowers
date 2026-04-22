"""
HBS-01A Accessible Washbasin — Revit Family Generator
======================================================
Generates a parametric wall-hosted Plumbing Fixture family from the
HBS-01A sanitary set drawing.

Run inside Revit via:
  - pyRevit (add as a pushbutton script)
  - Revit Python Shell (paste and execute)

All dimensions in millimetres; the script converts to Revit internal
feet units automatically via the `mm` helper below.

Default dimensions (all overridable via family parameters at load time):
  Width        460 mm   — overall counter width
  Depth        520 mm   — projection from wall
  Height       840 mm   — finished floor to counter top (840 max per drawing)
  Rim          150 mm   — counter slab thickness
  Basin_W      435 mm   — inner bowl width
  Basin_D      435 mm   — inner bowl depth
  Basin_Depth   80 mm   — bowl depth below counter surface
  Knee_Clear   680 mm   — minimum knee clearance height
  Toe_H        230 mm   — minimum toe space height
  Toe_D        230 mm   — minimum toe space depth
"""

import clr
clr.AddReference("RevitAPI")
clr.AddReference("RevitAPIUI")

from Autodesk.Revit.DB import (
    Transaction, Family, FamilyManager, Extrusion, CurveArrArray, CurveArray,
    Line, Arc, XYZ, Plane, SketchPlane, ElementId, BuiltInCategory,
    BuiltInParameter, FamilyParameter, ParameterType, UnitType, UnitUtils,
    DisplayUnitType, FamilySymbol, FilteredElementCollector, Document,
    ReferencePlane, FamilyParameterGroup,
)

# ── helpers ──────────────────────────────────────────────────────────────────

def mm(value):
    """Convert millimetres to Revit internal units (decimal feet)."""
    return UnitUtils.ConvertToInternalUnits(value, DisplayUnitType.DUT_MILLIMETERS)

def make_rect_curve_loop(doc, origin_x, origin_y, width, depth, sketch_plane):
    """Return a CurveArray forming a closed rectangle on the XY sketch plane."""
    p0 = XYZ(origin_x, origin_y, 0)
    p1 = XYZ(origin_x + width, origin_y, 0)
    p2 = XYZ(origin_x + width, origin_y + depth, 0)
    p3 = XYZ(origin_x, origin_y + depth, 0)
    loop = CurveArray()
    loop.Append(Line.CreateBound(p0, p1))
    loop.Append(Line.CreateBound(p1, p2))
    loop.Append(Line.CreateBound(p2, p3))
    loop.Append(Line.CreateBound(p3, p0))
    return loop

def add_length_param(fm, name, group, value_mm, is_instance=False):
    """Add a length family parameter and set its default value."""
    fp = fm.AddParameter(name, group, ParameterType.Length, is_instance)
    fm.Set(fp, mm(value_mm))
    return fp

# ── main ─────────────────────────────────────────────────────────────────────

def create_hbs01a_family(doc):
    """
    Build the HBS-01A washbasin family geometry and parameters.
    Call this from an open Revit Family document (.rft template).
    """
    fm = doc.FamilyManager

    with Transaction(doc, "HBS-01A: Add Parameters") as t:
        t.Start()

        # ── Type parameters (drive geometry) ─────────────────────────────────
        p_width     = add_length_param(fm, "Width",       FamilyParameterGroup.PG_GEOMETRY, 460)
        p_depth     = add_length_param(fm, "Depth",       FamilyParameterGroup.PG_GEOMETRY, 520)
        p_height    = add_length_param(fm, "Height",      FamilyParameterGroup.PG_GEOMETRY, 840)
        p_rim       = add_length_param(fm, "Rim_Thickness",FamilyParameterGroup.PG_GEOMETRY, 150)
        p_basin_w   = add_length_param(fm, "Basin_Width", FamilyParameterGroup.PG_GEOMETRY, 435)
        p_basin_d   = add_length_param(fm, "Basin_Depth_Bowl", FamilyParameterGroup.PG_GEOMETRY, 80)
        p_knee      = add_length_param(fm, "Knee_Clearance",   FamilyParameterGroup.PG_CONSTRAINTS, 680)
        p_toe_h     = add_length_param(fm, "Toe_Space_Height", FamilyParameterGroup.PG_CONSTRAINTS, 230)
        p_toe_d     = add_length_param(fm, "Toe_Space_Depth",  FamilyParameterGroup.PG_CONSTRAINTS, 230)

        t.Commit()

    # ── Retrieve sketch planes ────────────────────────────────────────────────
    # In a wall-hosted template the reference planes "Left", "Right",
    # "Front", "Back", "Top", "Bottom" are already present.
    # We build geometry on the horizontal plane at counter-top level.

    W   = mm(460)   # Width
    D   = mm(520)   # Depth (projection from wall)
    H   = mm(840)   # Counter top Z
    RIM = mm(150)   # Slab thickness
    BW  = mm(435)   # Basin inner width
    BD  = mm(80)    # Basin bowl depth

    with Transaction(doc, "HBS-01A: Create Geometry") as t:
        t.Start()

        # ── Counter slab ─────────────────────────────────────────────────────
        # Sketch plane at Z = H - RIM (bottom face of slab)
        slab_origin = XYZ(0, 0, H - RIM)
        slab_plane  = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, slab_origin)
        slab_sp     = SketchPlane.Create(doc, slab_plane)

        outer_loop = CurveArrArray()
        outer_loop.Append(make_rect_curve_loop(doc, 0, 0, W, D, slab_sp))

        # Basin cutout — centred in width, 25 mm rim from front/back
        basin_offset_x = (W - BW) / 2.0
        basin_offset_y = mm(25)
        inner_loop = CurveArray()
        inner_loop.Append(
            Line.CreateBound(
                XYZ(basin_offset_x,          basin_offset_y, 0),
                XYZ(basin_offset_x + BW,     basin_offset_y, 0),
            )
        )
        inner_loop.Append(
            Line.CreateBound(
                XYZ(basin_offset_x + BW, basin_offset_y, 0),
                XYZ(basin_offset_x + BW, basin_offset_y + BW, 0),
            )
        )
        inner_loop.Append(
            Line.CreateBound(
                XYZ(basin_offset_x + BW, basin_offset_y + BW, 0),
                XYZ(basin_offset_x,       basin_offset_y + BW, 0),
            )
        )
        inner_loop.Append(
            Line.CreateBound(
                XYZ(basin_offset_x, basin_offset_y + BW, 0),
                XYZ(basin_offset_x, basin_offset_y,      0),
            )
        )
        outer_loop.Append(inner_loop)

        slab_extrusion = doc.FamilyCreate.NewExtrusion(
            True, outer_loop, slab_sp, RIM
        )
        slab_extrusion.get_Parameter(BuiltInParameter.ELEMENT_IS_CUTTING).Set(0)

        # ── Basin bowl (void extrusion cuts into slab) ────────────────────────
        bowl_origin = XYZ(0, 0, H - RIM)
        bowl_plane  = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, bowl_origin)
        bowl_sp     = SketchPlane.Create(doc, bowl_plane)

        bowl_profile = CurveArrArray()
        bowl_profile.Append(inner_loop)   # reuse same rect outline

        bowl_void = doc.FamilyCreate.NewExtrusion(
            False,          # isSolid = False → void
            bowl_profile,
            bowl_sp,
            -BD,            # cut downward
        )

        doc.FamilyCreate.NewCutGeometry(slab_extrusion, bowl_void)

        # ── Under-counter apron / modesty panel (solid) ───────────────────────
        # Thin panel from Knee clearance height to counter bottom, wall-flush
        apron_z     = mm(680)          # knee clearance min
        apron_thick = mm(25)
        apron_h     = (H - RIM) - apron_z

        apron_plane_origin = XYZ(0, 0, apron_z)
        apron_plane = Plane.CreateByNormalAndOrigin(XYZ.BasisZ, apron_plane_origin)
        apron_sp    = SketchPlane.Create(doc, apron_plane)

        apron_profile = CurveArrArray()
        apron_loop    = CurveArray()
        apron_loop.Append(Line.CreateBound(XYZ(0, 0, 0),           XYZ(W, 0, 0)))
        apron_loop.Append(Line.CreateBound(XYZ(W, 0, 0),           XYZ(W, apron_thick, 0)))
        apron_loop.Append(Line.CreateBound(XYZ(W, apron_thick, 0), XYZ(0, apron_thick, 0)))
        apron_loop.Append(Line.CreateBound(XYZ(0, apron_thick, 0), XYZ(0, 0, 0)))
        apron_profile.Append(apron_loop)

        doc.FamilyCreate.NewExtrusion(True, apron_profile, apron_sp, apron_h)

        t.Commit()

    print("HBS-01A family geometry and parameters created successfully.")
    print("Next steps:")
    print("  1. Assign subcategory visibility (Revit → Manage → Object Styles).")
    print("  2. Add connector for cold-water supply if required.")
    print("  3. Save as HBS-01A.rfa (File → Save).")


# ── entry point ───────────────────────────────────────────────────────────────

if __name__ == "__main__":
    # When run from pyRevit or Revit Python Shell, `doc` is injected
    # into the global namespace automatically.  If running standalone
    # (e.g. via Dynamo Player), supply the active family document here.
    try:
        active_doc = __revit__.ActiveUIDocument.Document  # pyRevit / RPS
    except NameError:
        raise RuntimeError(
            "Open a Revit Family document (File > New > Family, choose a "
            "Wall-hosted Plumbing Fixture template) before running this script."
        )

    if not active_doc.IsFamilyDocument:
        raise RuntimeError(
            "Active document is not a Family document. "
            "Open the correct .rft template first."
        )

    create_hbs01a_family(active_doc)
