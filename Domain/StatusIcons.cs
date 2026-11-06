using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Domain.IconPaint;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// The statuses' icons, painted when first needed in the buttons' style. Each shows a spawner (a mossy root nest, a red
// glow in its mouth) with an emblem in front:
// - Protecting spawner: in the blue shield bubble protected spawners get, the Protect button's shield, glowing amber:
//   you are what keeps it safe.
// - Protected spawner nearby: in the bubble, a stone keep, its window glowing blue: the server keeps it safe.
// - Highlighting spawner: no bubble, the nest outlined in the highlight's amber glow, the Highlight button's eye.
internal static class StatusIcons
{
    private static readonly Color Ward = new(0.4f, 0.72f, 1f);

    private static Sprite? _protecting;
    private static Sprite? _serverProtected;
    private static Sprite? _highlighting;

    internal static Sprite Protecting() =>
        _protecting ? _protecting! : _protecting = ProtectingPicture().ToSprite("ProtectSpawnerFarms_Protecting");

    internal static Sprite ServerProtected() =>
        _serverProtected ? _serverProtected! : _serverProtected = ServerProtectedPicture().ToSprite("ProtectSpawnerFarms_ServerProtected");

    internal static Sprite Highlighting() =>
        _highlighting ? _highlighting! : _highlighting = HighlightingPicture().ToSprite("ProtectSpawnerFarms_Highlighting");

    // The pictures' 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderProtecting() => ProtectingPicture().Colors;

    internal static Color[] RenderServerProtected() => ServerProtectedPicture().Colors;

    internal static Color[] RenderHighlighting() => HighlightingPicture().Colors;

    private static IconCanvas ProtectingPicture()
    {
        IconCanvas canvas = BubbledSpawner();
        Glow(canvas, 47f, 47f, 17f, ButtonIcons.Amber, 0.55f);
        ButtonIcons.PaintShield(canvas, 47f, 47f, 13.5f);
        return canvas;
    }

    private static IconCanvas ServerProtectedPicture()
    {
        IconCanvas canvas = BubbledSpawner();
        Glow(canvas, 47f, 47f, 17f, Ward, 0.55f);
        Keep(canvas);
        return canvas;
    }

    private static IconCanvas HighlightingPicture()
    {
        IconCanvas canvas = new();
        Glow(canvas, BubbleX, BubbleY, BubbleRadius + 6f, ButtonIcons.Amber, 0.3f);
        Reticle(canvas);
        Nest(canvas);
        // The highlight's glowing edge around the nest, as it shows in the world.
        GlowLine(canvas, (x, y) => Mathf.Abs(NestShape(x, y)) - 0.6f, ButtonIcons.Amber, new Color(1f, 0.8f, 0.4f), 4f, 0.7f);
        Glow(canvas, 46f, 50f, 16f, ButtonIcons.Amber, 0.55f);
        ButtonIcons.PaintEye(canvas, 46f, 50f, 0.48f);
        return canvas;
    }

    // A target ring where the bubble would be: four arcs, broken at the top, bottom and sides by ticks pointing in.
    private static void Reticle(IconCanvas canvas)
    {
        Color core = new(1f, 0.8f, 0.4f);
        GlowLine(canvas, (x, y) =>
        {
            float r = Circle(x, y, BubbleX, BubbleY, 0f);
            float angle = Mathf.Repeat(Mathf.Atan2(y - BubbleY, x - BubbleX) * Mathf.Rad2Deg, 90f);
            float gap = Mathf.Abs(angle - 45f) - 31f;
            float arc = Mathf.Max(Mathf.Abs(r - (BubbleRadius - 1f)) - 0.9f, gap * Mathf.Deg2Rad * BubbleRadius);
            float ticks = Mathf.Min(
                Mathf.Min(Capsule(x, y, BubbleX, BubbleY - BubbleRadius - 1f, BubbleX, BubbleY - BubbleRadius + 6f, 1f),
                    Capsule(x, y, BubbleX - BubbleRadius - 1f, BubbleY, BubbleX - BubbleRadius + 6f, BubbleY, 1f)),
                Mathf.Min(Capsule(x, y, BubbleX + BubbleRadius + 1f, BubbleY, BubbleX + BubbleRadius - 6f, BubbleY, 1f),
                    Capsule(x, y, BubbleX, BubbleY + BubbleRadius + 1f, BubbleX, BubbleY + BubbleRadius - 6f, 1f)));
            return Mathf.Min(arc, ticks);
        }, ButtonIcons.Amber, core, 3.5f, 0.6f);
    }

    // ---- The spawner in its bubble ----

    private const float BubbleX = 28f, BubbleY = 30f, BubbleRadius = 26f;
    private const float NestX = 26f, NestBase = 52f;

    private static IconCanvas BubbledSpawner()
    {
        IconCanvas canvas = new();
        Glow(canvas, BubbleX, BubbleY, BubbleRadius + 6f, Ward, 0.3f);
        Nest(canvas);
        Bubble(canvas);
        return canvas;
    }

    private static float NestShape(float x, float y)
    {
        float mound = Mathf.Max(Ellipse(x, y, NestX, NestBase, 18.5f, 22f), y - NestBase);
        // Roots splaying out at its foot.
        float roots = Mathf.Min(
            Mathf.Min(Capsule(x, y, NestX - 13f, NestBase - 3f, NestX - 21f, NestBase + 1.5f, 1.8f),
                Capsule(x, y, NestX + 13f, NestBase - 3f, NestX + 21f, NestBase + 1.5f, 1.8f)),
            Mathf.Min(Capsule(x, y, NestX - 6f, NestBase - 2f, NestX - 9.5f, NestBase + 2.5f, 1.5f),
                Capsule(x, y, NestX + 6f, NestBase - 2f, NestX + 9.5f, NestBase + 2.5f, 1.5f)));
        return Mathf.Min(mound, roots);
    }

    private static float NestMouth(float x, float y) => Ellipse(x, y, NestX, NestBase - 8f, 5f, 6.5f);

    private static void Nest(IconCanvas canvas)
    {
        // Its shadow on the ground.
        canvas.Paint((x, y) => new Color(0f, 0f, 0f, Mathf.Clamp01(0.5f - Ellipse(x, y, NestX, NestBase + 1f, 23f, 3f)) * 0.45f));
        Shadow(canvas, NestShape, 0.5f);

        // Twisted bark, lit from the top left, its ridges running up to the crown, moss on top.
        float crownY = NestBase - 30f;
        Fill(canvas, NestShape, (x, y, depth) =>
        {
            float angle = Mathf.Atan2(x - NestX, y - crownY) + Mathf.Sin(y * 0.45f) * 0.08f;
            float ridge = Mathf.Abs(Mathf.Sin(angle * 11f));
            float lit = Mathf.Sqrt((x - (NestX - 9f)) * (x - (NestX - 9f)) + (y - (NestBase - 19f)) * (y - (NestBase - 19f)));
            float light = Mathf.Lerp(1.2f, 0.55f, Mathf.Clamp01(lit / 28f));
            light *= Mathf.Lerp(0.55f, 1f, Mathf.SmoothStep(0f, 1f, ridge));
            light *= 0.8f + 0.2f * Mathf.Clamp01(depth / 1.5f);
            Color bark = new(0.44f * light, 0.29f * light, 0.17f * light, 1f);
            Color moss = new(0.36f * light, 0.46f * light, 0.18f * light, 1f);
            return Color.Lerp(bark, moss, Mathf.Clamp01((NestBase - 15f - y) / 4f) * 0.75f);
        });

        // The mouth: dark, glowing red deep inside, two eyes peering out.
        canvas.Paint((x, y) =>
        {
            float d = NestMouth(x, y);
            float rim = Mathf.Clamp01(0.5f - (Mathf.Abs(d) - 0.8f)) * 0.8f;
            Color hole = new(0.07f, 0.03f, 0.02f, Mathf.Clamp01(0.5f - d));
            float heat = Mathf.Pow(Mathf.Clamp01(1f - Circle(x, y, NestX, NestBase - 5f, 0f) / 7f), 2f);
            hole = IconCanvas.Over(hole, new Color(1f, 0.25f, 0.08f, heat * 0.7f * Mathf.Clamp01(0.5f - d)));
            return IconCanvas.Over(hole, new Color(0.16f, 0.09f, 0.05f, rim));
        });
        foreach (float side in new[] { -1.7f, 1.7f })
            canvas.Paint((x, y) => new Color(1f, 0.75f, 0.3f, Mathf.Clamp01(0.5f - Circle(x, y, NestX + side, NestBase - 10.5f, 0.95f))));
    }

    // The shield bubble: see-through, thicker toward its edge, a bright rim and a glint at the top left.
    private static void Bubble(IconCanvas canvas)
    {
        canvas.Paint((x, y) =>
        {
            float r = Circle(x, y, BubbleX, BubbleY, 0f);
            if (r > BubbleRadius + 0.5f) return Color.clear;
            float edge = Mathf.Pow(Mathf.Clamp01(r / BubbleRadius), 5f);
            return new Color(Ward.r, Ward.g, Ward.b, (0.1f + 0.4f * edge) * Mathf.Clamp01(BubbleRadius + 0.5f - r));
        });
        GlowLine(canvas, (x, y) => Mathf.Abs(Circle(x, y, BubbleX, BubbleY, BubbleRadius)) - 0.7f,
            Ward, new Color(0.8f, 0.92f, 1f), 3.5f, 0.6f);

        // The glint: an arc along the inside of the rim, fading at its ends.
        canvas.Paint((x, y) =>
        {
            float r = Circle(x, y, BubbleX, BubbleY, 0f);
            float angle = Mathf.Atan2(y - BubbleY, x - BubbleX) * Mathf.Rad2Deg;
            float along = Mathf.Clamp01(1f - Mathf.Abs(angle + 132f) / 30f);
            float d = Mathf.Abs(r - (BubbleRadius - 4.5f)) - 1.3f * along;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d) * Mathf.SmoothStep(0f, 1f, along) * 0.8f);
        });
    }

    // ---- The keep ----

    private static float KeepShape(float x, float y)
    {
        float tower = Box(x, y, 40.5f, 40f, 53.5f, 60f);
        float parapet = Box(x, y, 39.5f, 37f, 54.5f, 41f);
        float merlons = Mathf.Min(Box(x, y, 39.5f, 33.5f, 42.5f, 38f),
            Mathf.Min(Box(x, y, 45.5f, 33.5f, 48.5f, 38f), Box(x, y, 51.5f, 33.5f, 54.5f, 38f)));
        return Mathf.Min(Mathf.Min(tower, parapet), merlons);
    }

    private static void Keep(IconCanvas canvas)
    {
        Shadow(canvas, KeepShape, 0.6f);

        // Grey stone in courses of blocks, lit from the top left, the edges catching the light.
        Fill(canvas, KeepShape, (x, y, depth) =>
        {
            int course = Mathf.FloorToInt((y - 33.5f) / 4f);
            float rowEdge = Mathf.Abs((y - 33.5f) - (course + 0.5f) * 4f) / 2f;
            float shift = course % 2 == 0 ? 0f : 2.5f;
            float joint = Mathf.Abs(Mathf.Repeat(x - 40f + shift, 5f) - 2.5f) / 2.5f;
            float mortar = Mathf.Max(Mathf.Pow(rowEdge, 8f), Mathf.Pow(joint, 10f));
            float light = Mathf.Lerp(1.1f, 0.65f, Mathf.Clamp01((x - 39.5f) / 15f * 0.6f + (y - 33.5f) / 27f * 0.4f));
            light *= Mathf.Lerp(1f, 0.62f, mortar);
            light *= 0.78f + 0.22f * Mathf.Clamp01(depth / 1.2f);
            light *= 1f + 0.1f * ((course * 7 + Mathf.FloorToInt((x - 40f + shift) / 5f) * 3) % 3 - 1);
            return new Color(0.62f * light, 0.61f * light, 0.6f * light, 1f);
        });

        // An arched window, glowing ward-blue, and the dark doorway below it.
        GlowLine(canvas, (x, y) => Capsule(x, y, 47f, 45f, 47f, 49.5f, 1.7f), Ward, new Color(0.82f, 0.95f, 1f), 4f, 0.8f);
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Min(Box(x, y, 44.5f, 56f, 49.5f, 60f), Circle(x, y, 47f, 56f, 2.5f));
            return new Color(0.1f, 0.08f, 0.07f, Mathf.Clamp01(0.5f - d));
        });
    }
}
