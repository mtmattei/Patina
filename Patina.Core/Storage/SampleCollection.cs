using System.Collections.Immutable;
using Patina.Core.Model;
using Patina.Core.Rules;

namespace Patina.Core.Storage;

/// <summary>
/// A fictional collection placed at real Montréal sites. Works, artists and records are invented.
/// Survey dates are relative to the first run, so every condition and due state is represented whenever the app starts.
/// </summary>
public static class SampleCollection
{
    private const string Lead = "C. Lavoie";
    private const string Tech = "M. Haddad";

    public static PatinaDocument Create(DateOnly today)
    {
        Artwork A(string id, string acc, string title, string artist, int year, ArtMaterial m, string district, string site, double lat, double lon, string description) =>
            new(id, acc, title, artist, year, m, district, site, lat, lon, description);

        var artworks = ImmutableList.Create(
            A("a01", "PA-1931-004", "The Reader at Rest", "Joseph Taillefer", 1931, ArtMaterial.Bronze, "Ville-Marie", "Square Phillips", 45.5040, -73.5687,
                "Seated figure reading on a granite plinth. Lost-wax bronze with a brown patina last waxed in 2019."),
            A("a02", "PA-1987-112", "Tidewater Gate", "Hélène Marchand", 1987, ArtMaterial.Steel, "Ville-Marie", "Quai de l'Horloge", 45.5106, -73.5466,
                "Two painted steel arches framing the river. Marine exposure; coating renewed on a five-year cycle."),
            A("a03", "PA-2016-031", "Crossing Lines", "Collectif Aube", 2016, ArtMaterial.Mural, "Le Plateau-Mont-Royal", "Boulevard Saint-Laurent, south wall", 45.5175, -73.5885,
                "Acrylic mural over a brick party wall, 14 m wide. Anti-graffiti sacrificial coating applied in 2021."),
            A("a04", "PA-1974-058", "Seven Winds", "Marguerite Olande", 1974, ArtMaterial.Concrete, "Rosemont–La Petite-Patrie", "Parc Maisonneuve", 45.5560, -73.5630,
                "Seven cast concrete vanes on a radial base. Exposed aggregate finish."),
            A("a05", "PA-1962-020", "Heron Column", "Paul-Évariste Roy", 1962, ArtMaterial.Bronze, "Le Plateau-Mont-Royal", "Mount Royal, Parc Avenue entrance", 45.5060, -73.5880,
                "Heron at rest on a fluted bronze column, 5 m. Green corrosion run-off stains the limestone base."),
            A("a06", "PA-1999-077", "Granite Ledger", "Anika Sørbø", 1999, ArtMaterial.Stone, "Le Sud-Ouest", "Lachine Canal, Atwater lock", 45.4840, -73.5800,
                "Stacked granite slabs engraved with canal tonnage records."),
            A("a07", "PA-2004-015", "Sunday Market", "Atelier Pirès", 2004, ArtMaterial.Mosaic, "Villeray–Saint-Michel–Parc-Extension", "Place du marché, north façade", 45.5363, -73.6145,
                "Glass and ceramic tesserae on a concrete wall, 6 × 3 m."),
            A("a08", "PA-2019-044", "Night Garden", "Inés Calderón", 2019, ArtMaterial.Mural, "Mercier–Hochelaga-Maisonneuve", "Rue Ontario Est at Darling", 45.5469, -73.5420,
                "Exterior mural in silicate paint on stucco. Lower 1.5 m tagged repeatedly."),
            A("a09", "PA-2011-090", "Bench for Two Rivers", "Thomas Ouellet", 2011, ArtMaterial.Wood, "Verdun", "Berges de Verdun, beach path", 45.4600, -73.5680,
                "Carved white-oak bench seat on steel legs. Oiled twice a year."),
            A("a10", "PA-1989-063", "Signal", "Kenji Arai", 1989, ArtMaterial.Steel, "Ville-Marie", "Rue Jeanne-Mance at Président-Kennedy", 45.5085, -73.5662,
                "Red painted steel tower with a kinetic top element (fixed since 2008)."),
            A("a11", "PA-1925-001", "The Ferryman", "Edmond Lacasse", 1925, ArtMaterial.Bronze, "Ville-Marie", "Place Jacques-Cartier", 45.5079, -73.5530,
                "Standing figure with oar on a granite pedestal. The collection's oldest work outdoors."),
            A("a12", "PA-2008-027", "Fold", "Simone Achebe-Grant", 2008, ArtMaterial.Steel, "Outremont", "Parc Saint-Viateur", 45.5230, -73.6060,
                "Weathering steel sheet folded into a pavilion. Intended to rust; watch for perforation at seams."),
            A("a13", "PA-1984-102", "Limestone Choir", "Gabrielle Forest", 1984, ArtMaterial.Stone, "Mercier–Hochelaga-Maisonneuve", "Parc Morgan", 45.5530, -73.5460,
                "Nine carved Indiana limestone heads in a semicircle."),
            A("a14", "PA-2021-008", "Weathervane Children", "Studio Oculus", 2021, ArtMaterial.Mural, "Villeray–Saint-Michel–Parc-Extension", "Rue Jarry Est, library wall", 45.5600, -73.6200,
                "Mural painted with the neighbourhood school. Varnished at completion."),
            A("a15", "PA-1996-049", "Harbour Wall", "Raffaele Conti", 1996, ArtMaterial.Mosaic, "Le Sud-Ouest", "Rue Notre-Dame Ouest, Saint-Henri", 45.4770, -73.5850,
                "Ceramic mosaic band along a retaining wall, 40 m. Freeze-thaw losses at drain outlets."),
            A("a16", "PA-2014-066", "Totem of Small Things", "Odile Brassard", 2014, ArtMaterial.Wood, "Ahuntsic-Cartierville", "Parc Ahuntsic", 45.5540, -73.6600,
                "Carved cedar column with small cast-bronze inserts collected from residents."),
            A("a17", "PA-1978-033", "Equinox Ring", "Dario Belmonte", 1978, ArtMaterial.Concrete, "Côte-des-Neiges–Notre-Dame-de-Grâce", "Parc Jean-Brillant", 45.4960, -73.6230,
                "Precast concrete ring, 4 m, aligned to the equinox sunrise."),
            A("a18", "PA-1912-002", "Founders' Fountain", "Henri Beaulac", 1912, ArtMaterial.Bronze, "Ville-Marie", "Square Dorchester", 45.5000, -73.5710,
                "Bronze basin and three figures. Water off since 2017; basin used as a planter."));

        var surveys = ImmutableList.CreateBuilder<Survey>();
        var treatments = ImmutableList.CreateBuilder<Treatment>();
        var serial = 0;

        Finding F(FindingType type, string zone, Severity severity, string note = "") =>
            new($"f{++serial:000}", type, zone, severity, note, []);

        Survey S(string artworkId, int daysAgo, string surveyor, Weather weather, string notes, params Finding[] findings)
        {
            var survey = new Survey($"s{++serial:000}", artworkId, SurveyStatus.Submitted, today.AddDays(-daysAgo), surveyor, weather, notes,
                [.. findings], [], ConditionRules.Grade(findings));
            surveys.Add(survey);
            return survey;
        }

        void T(Survey survey, int findingIndex, TreatmentStatus status, int? scheduledInDays, string assignee, params (int DaysAgo, LogKind Kind, string Text, TreatmentStatus? Status)[] log)
        {
            var finding = survey.Findings[findingIndex];
            var created = survey.Date;
            treatments.Add(new Treatment(
                $"t{++serial:000}",
                survey.ArtworkId,
                survey.Id,
                finding.Id,
                TreatmentTitles.For(finding),
                finding.Severity == Severity.Urgent ? TreatmentPriority.Urgent : TreatmentPriority.High,
                status,
                scheduledInDays is { } d ? today.AddDays(d) : null,
                assignee,
                [
                    new TreatmentLogEntry(At(created), survey.Surveyor, LogKind.Proposed),
                    .. log.Select(l => new TreatmentLogEntry(At(today.AddDays(-l.DaysAgo)), assignee.Length > 0 ? assignee : Lead, l.Kind, l.Text, l.Status)),
                ],
                created,
                status == TreatmentStatus.Done ? today.AddDays(-log.Min(l => l.DaysAgo)) : null));
        }

        // The Reader at Rest: good, current.
        S("a01", 760, Lead, Weather.Overcast, "Wax layer intact.", F(FindingType.Soiling, "Book and hands", Severity.Minor));
        S("a01", 120, Lead, Weather.Dry, "Rewaxing holding well.", F(FindingType.Soiling, "Plinth top", Severity.Minor));

        // Tidewater Gate: fair, due soon.
        S("a02", 345, Tech, Weather.Wet, "Coating chalking on river side.",
            F(FindingType.CoatingFailure, "East arch, river face", Severity.Moderate, "Chalking, no bare metal."),
            F(FindingType.Graffiti, "West arch, base", Severity.Moderate));

        // Crossing Lines: critical.
        var crossing = S("a03", 40, Lead, Weather.Dry, "Large flake loss after spring freeze.",
            F(FindingType.Flaking, "Upper left, figure's face", Severity.Urgent, "About 0.6 m² lifting; loose flakes at street level."),
            F(FindingType.Graffiti, "Lower third, full width", Severity.Serious),
            F(FindingType.Fading, "Blue passages", Severity.Moderate));
        T(crossing, 0, TreatmentStatus.Scheduled, 3, Tech, (20, LogKind.StatusChanged, "Lift booked with borough.", TreatmentStatus.Scheduled));
        T(crossing, 1, TreatmentStatus.Proposed, null, string.Empty);

        // Seven Winds: never surveyed (new transfer to the collection).

        // Heron Column: poor, overdue.
        var heron = S("a05", 300, Lead, Weather.Overcast, "Run-off staining progressing on the limestone base.",
            F(FindingType.Corrosion, "Column flutes, north side", Severity.Serious, "Active green corrosion, powdery."),
            F(FindingType.Soiling, "Limestone base", Severity.Moderate, "Copper run-off stains."));
        T(heron, 0, TreatmentStatus.InProgress, -4, Tech,
            (30, LogKind.StatusChanged, "", TreatmentStatus.Scheduled),
            (5, LogKind.StatusChanged, "Cleaning started; wax to follow once dry.", TreatmentStatus.InProgress));

        // Granite Ledger: good, current.
        S("a06", 410, Tech, Weather.Dry, "Sound.", F(FindingType.BiologicalGrowth, "Shaded north face", Severity.Minor));

        // Sunday Market: fair, overdue.
        S("a07", 420, Lead, Weather.Freezing, "Some tesserae loose near downpipe.",
            F(FindingType.MaterialLoss, "Lower right by downpipe", Severity.Moderate, "11 tesserae missing."),
            F(FindingType.Efflorescence, "Lower edge", Severity.Minor));

        // Night Garden: poor, current; graffiti removal done.
        var night = S("a08", 70, Tech, Weather.Dry, "Tagging again on the lower band.",
            F(FindingType.Graffiti, "Lower band, east half", Severity.Serious),
            F(FindingType.Graffiti, "Lower band, west half", Severity.Moderate));
        T(night, 0, TreatmentStatus.Done, 55, Tech,
            (60, LogKind.StatusChanged, "", TreatmentStatus.Scheduled),
            (56, LogKind.StatusChanged, "", TreatmentStatus.InProgress),
            (55, LogKind.StatusChanged, "Removed with poultice; sacrificial coating renewed.", TreatmentStatus.Done));

        // Bench for Two Rivers: fair, due soon.
        S("a09", 165, Tech, Weather.Wet, "Oil schedule slipped.",
            F(FindingType.Cracking, "Seat, end grain", Severity.Moderate),
            F(FindingType.BiologicalGrowth, "Underside", Severity.Minor));

        // Signal: good, current.
        S("a10", 200, Lead, Weather.Dry, "Recoated 2025; good.", F(FindingType.Soiling, "Base plate", Severity.Minor));

        // The Ferryman: critical, current; urgent treatment proposed.
        var ferryman = S("a11", 25, Lead, Weather.Overcast, "Oar attachment loose; area cordoned.",
            F(FindingType.StructuralMovement, "Oar to right hand joint", Severity.Urgent, "Movement of 4 mm by hand. Cordon placed."),
            F(FindingType.Corrosion, "Pedestal fixings", Severity.Serious));
        T(ferryman, 0, TreatmentStatus.Proposed, null, string.Empty);
        T(ferryman, 1, TreatmentStatus.Scheduled, 12, Tech, (10, LogKind.StatusChanged, "", TreatmentStatus.Scheduled));

        // Fold: fair, current.
        S("a12", 150, Tech, Weather.Wet, "Seams holding.", F(FindingType.Corrosion, "Lower seam, south", Severity.Moderate, "Expected weathering; check for perforation."));

        // Limestone Choir: poor, overdue (two years ago).
        S("a13", 800, Lead, Weather.Overcast, "Sugaring on several faces.",
            F(FindingType.MaterialLoss, "Heads 3 and 4, noses", Severity.Serious),
            F(FindingType.BiologicalGrowth, "Back of heads", Severity.Moderate));

        // Weathervane Children: never surveyed.

        // Harbour Wall: fair escalated to poor (three moderate findings), current.
        var harbour = S("a15", 95, Tech, Weather.Freezing, "Freeze-thaw losses at three drains.",
            F(FindingType.MaterialLoss, "Drain 2", Severity.Moderate),
            F(FindingType.MaterialLoss, "Drain 5", Severity.Moderate),
            F(FindingType.MaterialLoss, "Drain 9", Severity.Moderate));
        _ = harbour;

        // Totem of Small Things: good, due soon.
        S("a16", 160, Tech, Weather.Dry, "Inserts all present.", F(FindingType.Soiling, "Lower carving", Severity.Minor));

        // Equinox Ring: fair, current.
        S("a17", 300, Lead, Weather.Dry, "Hairline cracks stable since last survey.", F(FindingType.Cracking, "Inner face, east", Severity.Moderate));

        // Founders' Fountain: poor, current, treatment done earlier.
        var fountain = S("a18", 210, Lead, Weather.Overcast, "Basin leaking into planter bed; figures sound.",
            F(FindingType.Corrosion, "Basin floor", Severity.Serious),
            F(FindingType.Graffiti, "North figure base", Severity.Minor));
        T(fountain, 0, TreatmentStatus.Done, 180, Tech,
            (200, LogKind.StatusChanged, "", TreatmentStatus.Scheduled),
            (185, LogKind.StatusChanged, "", TreatmentStatus.InProgress),
            (180, LogKind.StatusChanged, "Basin sealed and planter liner installed.", TreatmentStatus.Done));

        return new PatinaDocument(PatinaDocument.CurrentVersion, artworks, surveys.ToImmutable(), treatments.ToImmutable(), []);
    }

    private static DateTimeOffset At(DateOnly date) => new(date.ToDateTime(new TimeOnly(10, 0)), TimeSpan.Zero);
}
