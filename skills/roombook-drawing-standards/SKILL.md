---
name: roombook-drawing-standards
description: Use when reviewing, generating, or quality-checking Roombook drawings for hospital projects; when validating room data sheets, checking drawing completeness against healthcare facility standards, or identifying missing information in room schedules
---

# Roombook Drawing Standards — Hospital Projects

## Overview

A Roombook (Room Data Sheet / RDS) is the authoritative source of truth for every room in a hospital project. Every drawing derived from it must be complete, consistent, and compliant.

**Core principle:** A drawing without a fully populated Roombook entry is an unverified drawing.

## When to Use

- Generating or reviewing hospital room drawings
- Auditing Roombook completeness before issue for construction
- Cross-checking drawings against Room Data Sheets
- Quality-checking BIM models or 2D drawings for room compliance
- Identifying discrepancies between Roombook data and issued drawings

**Do NOT skip when:**
- Drawings look "almost complete" (missing data causes RFIs and delays)
- Under programme pressure (incomplete drawings cost more to fix in construction)
- Room type is repeated (repetition ≠ verified compliance)

---

## Standard Roombook Entry — Required Fields

Every room record must contain all of the following before a drawing is issued:

### Identity
| Field | Requirement |
|-------|-------------|
| Room Number | Unique per floor/department, follows facility numbering protocol |
| Room Name | Matches design brief and department schedule |
| Department / Zone | Linked to organisational hierarchy |
| Functional Use | Primary clinical or support function |
| Occupancy Type | Patient room / staff / public / restricted |

### Spatial
| Field | Requirement |
|-------|-------------|
| Net Floor Area (m²) | Meets minimum per FGI Guidelines or local code |
| Clear Ceiling Height (mm) | ≥ 2700 mm clinical; ≥ 2400 mm non-clinical |
| Room Orientation | North arrow confirmed on plan |
| Adjacency Requirements | Key adjacencies noted (e.g., clean utility next to patient bay) |

### Finishes
| Surface | Required Data |
|---------|---------------|
| Floor | Material, finish, slip rating, coved skirting Y/N |
| Wall | Material, paint/tile zone heights, impact protection Y/N |
| Ceiling | System type, tile/board specification, access Y/N |
| Skirting | Material, height |

### MEP (Mechanical, Electrical, Plumbing)
| System | Minimum Roombook Data |
|--------|----------------------|
| HVAC | Ventilation class (ASHRAE 170 / HTM 03-01), ACH, pressure regime (positive/negative/neutral), temperature range |
| Electrical | Lighting level (lux), emergency lighting Y/N, circuit type, socket outlet count and type |
| Plumbing | Hot/cold outlets count, isolation Y/N, bedpan washer Y/N, hand-wash basin type |
| Medical Gas | Oxygen, vacuum, compressed air — point count and type (Schrader/NIST) |
| Nurse Call | Type, priority zone |

### Equipment & Furniture
| Field | Requirement |
|-------|-------------|
| Fixed Equipment Schedule | Item code, manufacturer reference, installation note |
| Loose/Movable Equipment | Count and type, clearance zone confirmed on plan |
| Furniture Schedule | Items keyed to plan, compliance with infection control gaps |

### Compliance Tags
| Tag | Standard |
|-----|---------|
| Accessibility | ADA / BS 8300 / local equivalent — manoeuvring clearances shown |
| Infection Control | Room classification (e.g., AIIR, PE room, standard) |
| Privacy | Visual and acoustic requirements noted |
| Fire | Compartment boundary confirmed, door rating if on boundary |

---

## Drawing Quality Checklist

Run this checklist on every room drawing before issue:

### Plan View
- [ ] Room number and name match Roombook exactly
- [ ] Net area annotated and within tolerance (≤ 2% of Roombook value)
- [ ] All doors: swing shown, clear opening width annotated
- [ ] All windows: sill height and head height annotated
- [ ] Handrail and grab-bar positions shown (if applicable)
- [ ] Fixed equipment keyed to schedule
- [ ] Loose equipment envelope shown with clearance zones
- [ ] Hand-wash basin position complies with point-of-care requirement
- [ ] North arrow present

### Elevations (minimum 2 per room)
- [ ] Wall finish zones shown and annotated
- [ ] Socket outlet heights confirmed
- [ ] Medical gas panel heights confirmed (standard: 1200 mm AFFL unless noted)
- [ ] Tile / splash-back heights shown

### Reflected Ceiling Plan
- [ ] Ceiling system type annotated
- [ ] Luminaire type and quantity keyed to electrical schedule
- [ ] Emergency luminaire positions shown
- [ ] HVAC supply / return grille positions shown with airflow arrow
- [ ] Sprinkler head positions (if applicable)
- [ ] Access hatch positions

### Schedules / Tags on Drawing
- [ ] Finish schedule references match Roombook
- [ ] Door/window tag numbers match door schedule
- [ ] Equipment tag numbers match equipment schedule
- [ ] Drawing revision cloud and revision triangle present if not first issue

---

## Common Failures

| Failure | Impact | Fix |
|---------|--------|-----|
| Net area not annotated | RFI, potential non-compliance | Annotate from Roombook; verify against minimum code area |
| HVAC pressure regime missing | Infection control risk | Confirm with M&E engineer; add to Roombook and RCP note |
| Hand-wash basin missing or mislocated | CQC / regulatory finding | Check FGI / HTM 00 point-of-care rules; relocate before issue |
| Medical gas outlet count mismatch | Construction error | Cross-check Roombook against HTM 02-01 / NFPA 99 table |
| Door clear width not annotated | Accessibility non-compliance | Check ADA / BS 8300 minimum (≥ 900 mm clear) |
| Equipment clearance zones omitted | Equipment cannot function post-build | Add 600 mm maintenance zone each side of fixed equipment |
| Ceiling height below minimum | Code violation | Escalate to architect; update structural/services coordination |
| Finish spec references wrong revision | Wrong materials ordered | Check finish schedule revision date against current Roombook |

---

## Hospital Room Type Reference

Quick minimum-standard reference (FGI 2022 / HTM baseline):

| Room Type | Min Net Area | ACH (total/OA) | Pressure | Hand-wash Basin |
|-----------|-------------|----------------|----------|-----------------|
| Single Patient Room (acute) | 28 m² | 6 / 2 | Neutral | 1 (point of care) |
| ICU/Critical Care Bed | 23 m² (clear floor) | 15 / 3 | Positive or neutral | 1 (point of care) |
| Airborne Infection Isolation Room | 23 m² | 12 / 2 | Negative (2.5 Pa) | 1 inside + 1 ante |
| Operating Theatre (general) | 37 m² | 20 / 5 | Positive (8 Pa min) | Scrub sink outside |
| Procedure Room | 23 m² | 15 / 3 | Positive | 1 |
| Clean Utility / Medication | 9 m² min | 10 / 2 | Positive | 1 |
| Dirty Utility / Soiled Holding | 9 m² min | 10 / 2 | Negative | 1 + BPW/BPD |
| Toilet / Shower (patient) | 4.5 m² (accessible) | 10 / 0 (exhaust) | Negative | 1 |
| Staff Base / Nursing Station | 1.4 m² per workstation | Natural / 6 mech | Neutral | — |

*Always verify minimums against project-specific brief and local authority requirements.*

---

## Quality Gate — Before Drawing Issue

```
STOP. Answer these before marking a drawing "Ready for Issue":

1. Is every Roombook field complete for this room?          YES / NO
2. Does the plan area match the Roombook (≤ 2%)?            YES / NO
3. Do finish references match the current Roombook rev?     YES / NO
4. Is HVAC pressure regime shown on RCP?                    YES / NO
5. Are all hand-wash basins shown at correct positions?     YES / NO
6. Is medical gas outlet count confirmed against schedule?  YES / NO
7. Are all accessibility clearances shown?                  YES / NO
8. Has M&E engineer signed off MEP data in Roombook?        YES / NO

Any NO = do not issue. Resolve first.
```

---

## Red Flags — STOP Before Issuing

- "Room is the same as adjacent — just copy it" (rooms differ; verify each)
- Roombook cell is blank but drawing has been issued before
- Area annotation is absent or says "TBC"
- Medical gas schedule not co-ordinated with Roombook
- Ceiling height annotated as "as existing" without measurement
- Finish specification refers to an earlier Roombook revision
- Any field marked "NTS" (Not To Scale) on a construction-issue drawing

---

## Key Standards Reference

| Standard | Scope |
|----------|-------|
| FGI Guidelines for Design and Construction (2022) | Space and functional minimums (US baseline) |
| NHS HTM 00 / HTM 03-01 / HTM 02-01 | UK hospital design and engineering guidance |
| ASHRAE 170-2021 | Ventilation of Health Care Facilities |
| NFPA 99 (2021) | Health Care Facilities Code (medical gas) |
| ADA Standards for Accessible Design | Accessibility (US) |
| BS 8300:2018 | Accessibility (UK) |
| ISO 14644 | Cleanroom classification (theatres/pharmacy) |
