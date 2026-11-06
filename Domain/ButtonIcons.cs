using UnityEngine;
using static JoksterCube.ProtectSpawnerFarms.Domain.IconPaint;

namespace JoksterCube.ProtectSpawnerFarms.Domain;

// The inventory buttons' icons, painted when first needed in the same style as TempestRite's flap buttons (a glowing
// rune or emblem over a soft shadow), so the mod ships no image files:
// - Protect: a round wooden shield in an iron rim, its domed iron boss ringed with amber light.
// - Highlight: an eye glowing amber, the highlight's own color, light rays fanning out above and below it.
internal static class ButtonIcons
{
    internal static readonly Color Amber = new(1f, 0.5f, 0.12f);

    private static Sprite? _shield;
    private static Sprite? _eye;

    internal static Sprite Shield() => _shield ? _shield! : _shield = ShieldPicture().ToSprite("ProtectSpawnerFarms_Shield");

    internal static Sprite Eye() => _eye ? _eye! : _eye = EyePicture().ToSprite("ProtectSpawnerFarms_Eye");

    // The pictures' 64 x 64 colors, row 0 at the top (previews).
    internal static Color[] RenderShield() => ShieldPicture().Colors;

    internal static Color[] RenderEye() => EyePicture().Colors;

    // ---- Protect ----

    private static IconCanvas ShieldPicture()
    {
        IconCanvas canvas = new();
        Glow(canvas, 32f, 32f, 31f, Amber, 0.45f);
        PaintShield(canvas, 32f, 32f, 27f);
        return canvas;
    }

    // The shield with its shadow, centered on (cx, cy), radius pixels to the rim's outer edge. Drawn at 27 for the
    // button; the statuses draw it smaller.
    internal static void PaintShield(IconCanvas canvas, float cx, float cy, float radius)
    {
        float s = radius / 27f, rimWidth = 3.5f * s, face = radius - rimWidth;
        Shadow(canvas, (x, y) => Circle(x, y, cx, cy, radius), 0.6f);

        // The face: upright boards, each a little different, lit from the top and darker at their seams.
        float left = cx - 28f * s, top = cy - 24f * s, board = 9f * s;
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, cx, cy, face);
            if (d > 0.5f) return Color.clear;
            int index = Mathf.FloorToInt((x - left) / board);
            float seam = Mathf.Abs((x - left) - (index + 0.5f) * board) / (board / 2f);
            float tone = index % 2 == 0 ? 1f : 0.88f;
            float light = Mathf.Lerp(1f, 0.62f, Mathf.Clamp01((y - top) / (48f * s))) * tone;
            light *= Mathf.Lerp(1f, 0.6f, Mathf.Pow(seam, 6f));
            light *= 0.8f + 0.2f * Mathf.Clamp01(-d / (3f * s));
            return new Color(0.62f * light, 0.38f * light, 0.2f * light, Mathf.Clamp01(0.5f - d));
        });

        // The rim: iron, brighter on top, with a dark inner edge where it grips the boards.
        canvas.Paint((x, y) =>
        {
            float r = Circle(x, y, cx, cy, 0f);
            float d = Mathf.Abs(r - (radius - rimWidth / 2f)) - rimWidth / 2f;
            float lit = Mathf.Clamp01((cy - y) / radius * 0.5f + 0.5f);
            float bevel = Mathf.Clamp01(-d / (1.5f * s));
            float light = Mathf.Lerp(0.3f, 0.68f, lit) * (0.7f + 0.3f * bevel);
            return new Color(light, light * 0.98f, light * 0.95f, Mathf.Clamp01(0.5f - d));
        });
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Abs(Circle(x, y, cx, cy, face)) - 0.5f;
            return new Color(0.12f, 0.07f, 0.04f, Mathf.Clamp01(0.5f - d) * 0.7f);
        });

        PaintBoss(canvas, cx, cy, s);
    }

    // A domed iron boss in the middle, a ring of amber light glowing round it.
    private static void PaintBoss(IconCanvas canvas, float cx, float cy, float s)
    {
        float r = 9f * s;
        GlowLine(canvas, (x, y) => Mathf.Abs(Circle(x, y, cx, cy, r + 2.2f * s)) - Mathf.Max(0.9f * s, 0.6f),
            new Color(1f, 0.45f, 0.1f), new Color(1f, 0.82f, 0.45f), Mathf.Max(4.5f * s, 2.5f), 0.8f);
        Shadow(canvas, (x, y) => Circle(x, y, cx, cy, r), 0.5f);
        canvas.Paint((x, y) =>
        {
            float d = Circle(x, y, cx, cy, r);
            float nx = (x - cx) / r, ny = (y - cy) / r;
            float lit = Mathf.Clamp01(0.55f - 0.6f * (nx * 0.6f + ny * 0.8f) - 0.35f * (nx * nx + ny * ny));
            float light = Mathf.Lerp(0.22f, 0.8f, lit);
            return new Color(light, light * 0.97f, light * 0.93f, Mathf.Clamp01(0.5f - d));
        });
        canvas.Paint((x, y) => new Color(1f, 1f, 0.95f,
            Mathf.Clamp01(0.5f - Circle(x, y, cx - 3f * s, cy - 3.5f * s, Mathf.Max(1.8f * s, 0.8f))) * 0.7f));
    }

    // ---- Highlight ----

    private static readonly float[] RayAngles = [-60f, -30f, 0f, 30f, 60f];

    private static IconCanvas EyePicture()
    {
        IconCanvas canvas = new();
        Glow(canvas, 32f, 32f, 32f, Amber, 0.5f);

        // Rays fanning out above and below the eye.
        GlowLine(canvas, (x, y) =>
        {
            float d = float.MaxValue;
            foreach (float angle in RayAngles)
            {
                float a = angle * Mathf.Deg2Rad, sx = Mathf.Sin(a), sy = Mathf.Cos(a);
                float inner = 19f + 4f * Mathf.Abs(sx), outer = inner + 7f;
                d = Mathf.Min(d, Capsule(x, y, 32f + sx * inner, 32f - sy * inner, 32f + sx * outer, 32f - sy * outer, 1.3f));
                d = Mathf.Min(d, Capsule(x, y, 32f + sx * inner, 32f + sy * inner, 32f + sx * outer, 32f + sy * outer, 1.3f));
            }
            return d;
        }, Amber, new Color(1f, 0.85f, 0.5f), 3.5f, 0.6f);

        PaintEye(canvas, 32f, 32f, 1f);
        return canvas;
    }

    // The eye with its shadow, its middle at (cx, cy): where two circles overlap, 48 wide and 24 high at scale 1 (the
    // button); the status draws it smaller.
    internal static void PaintEye(IconCanvas canvas, float cx, float cy, float scale)
    {
        float s = scale, lid = Mathf.Max(2f * s, 1.2f);
        float EyeShape(float x, float y) => Mathf.Max(Circle(x, y, cx, cy + 18f * s, 30f * s), Circle(x, y, cx, cy - 18f * s, 30f * s));

        Shadow(canvas, (x, y) => EyeShape(x, y) - lid, 0.6f);

        // The lids: a dark leathery outline around the eye.
        canvas.Paint((x, y) =>
        {
            float d = EyeShape(x, y) - lid;
            return new Color(0.24f, 0.14f, 0.08f, Mathf.Clamp01(0.5f - d));
        });

        // The white, shaded toward the corners and under the upper lid.
        canvas.Paint((x, y) =>
        {
            float d = EyeShape(x, y);
            float shade = Mathf.Lerp(1f, 0.7f, Mathf.Clamp01(Mathf.Abs(x - cx) / (22f * s)))
                * Mathf.Lerp(0.82f, 1f, Mathf.Clamp01((y - (cy - 10f * s)) / (10f * s)));
            return new Color(0.95f * shade, 0.9f * shade, 0.8f * shade, Mathf.Clamp01(0.5f - d));
        });

        // The iris: glowing amber, brighter toward the pupil, clipped by the lids.
        float irisRadius = 10f * s;
        canvas.Paint((x, y) =>
        {
            float d = Mathf.Max(Circle(x, y, cx, cy, irisRadius), EyeShape(x, y));
            float depth = Mathf.Clamp01(-Circle(x, y, cx, cy, irisRadius) / irisRadius);
            Color iris = new(1f, Mathf.Lerp(0.45f, 0.82f, depth), Mathf.Lerp(0.08f, 0.35f, depth), Mathf.Clamp01(0.5f - d));
            float ring = Mathf.Clamp01(0.5f - Mathf.Max(Mathf.Abs(Circle(x, y, cx, cy, irisRadius - 0.6f * s)) - Mathf.Max(0.6f * s, 0.4f), EyeShape(x, y)));
            return IconCanvas.Over(iris, new Color(0.45f, 0.18f, 0.05f, ring * 0.8f));
        });

        // The pupil, and a glint.
        canvas.Paint((x, y) => new Color(0.08f, 0.04f, 0.02f, Mathf.Clamp01(0.5f - Circle(x, y, cx, cy, Mathf.Max(4.2f * s, 1.6f)))));
        canvas.Paint((x, y) => new Color(1f, 1f, 0.95f,
            Mathf.Clamp01(0.5f - Circle(x, y, cx - 3.5f * s, cy - 3.5f * s, Mathf.Max(2f * s, 0.9f))) * 0.9f));
    }
}
