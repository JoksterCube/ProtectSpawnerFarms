# Builds the ProtectSpawnerFarms icon on top of the shared base (Thunderstore/icon_base.png): a corner badge with a shield.
# The badge frame matches FairCraftingLuck's clover badge so the mod icons read as one family.
# Usage (needs Pillow): python mod_icon.py <icon_base.png> <output icon.png> [preview.png]
import sys
from PIL import Image, ImageDraw, ImageFilter

W, SS = 256, 4
DARK = (24, 20, 16, 255)
GOLD = (176, 132, 62, 255)
GOLD_HI = (214, 172, 92, 255)
GREEN = (92, 150, 62, 255)
GREEN_HI = (128, 184, 86, 255)

def badge_frame(d, X, Y, R):
    # dark rim, gold ring, dark line, cream face
    d.ellipse([X - R, Y - R, X + R, Y + R], fill=DARK)
    R2 = R - 3.5 * SS
    d.ellipse([X - R2, Y - R2, X + R2, Y + R2], fill=GOLD)
    R3 = R2 - 5 * SS
    d.ellipse([X - R3, Y - R3, X + R3, Y + R3], fill=DARK)
    R4 = R3 - 2.5 * SS
    d.ellipse([X - R4, Y - R4, X + R4, Y + R4], fill=(238, 226, 190, 255))

def shield_outline(X, Y, half_w, top, bottom, pad=0.0):
    # heater shield: flat top with soft corners, sides curving into a point
    hw, t, b = half_w + pad, top - pad, bottom + pad * 1.6
    pts = []
    steps = 24
    pts.append((X - hw + 3 * SS, t))
    pts.append((X + hw - 3 * SS, t))
    pts.append((X + hw, t + 3 * SS))
    for i in range(steps + 1):
        u = i / steps  # right side, from the shoulder down to the tip
        x = X + hw * (1 - u ** 2.6)
        y = t + 3 * SS + (b - t - 3 * SS) * u
        pts.append((x, y))
    for i in range(steps, -1, -1):
        u = i / steps
        x = X - hw * (1 - u ** 2.6)
        y = t + 3 * SS + (b - t - 3 * SS) * u
        pts.append((x, y))
    pts.append((X - hw, t + 3 * SS))
    return pts

def shield(layer, X, Y):
    d = ImageDraw.Draw(layer)
    half_w, top, bottom = 19 * SS, Y - 20 * SS, Y + 23 * SS
    # dark outline, gold rim, two-tone green field split down the middle
    d.polygon(shield_outline(X, Y, half_w, top, bottom, 2.2 * SS), fill=DARK)
    d.polygon(shield_outline(X, Y, half_w, top, bottom), fill=GOLD)
    inner = shield_outline(X, Y, half_w - 3.6 * SS, top + 3.6 * SS, bottom - 4.4 * SS)
    d.polygon(inner, fill=GREEN)
    field = Image.new("L", layer.size, 0)
    ImageDraw.Draw(field).polygon(inner, fill=255)
    left = Image.new("L", layer.size, 0)
    ImageDraw.Draw(left).rectangle([0, 0, X, layer.size[1]], fill=255)
    hi = Image.new("RGBA", layer.size, GREEN_HI)
    hi.putalpha(Image.composite(field, Image.new("L", layer.size, 0), left))
    layer.alpha_composite(hi)
    d = ImageDraw.Draw(layer)
    # gold cross band with a dark edge, and a round boss in the middle
    band = 2.6 * SS
    for pad, col in [(1.4 * SS, DARK), (0, GOLD_HI)]:
        d.rectangle([X - band - pad, top + 3.6 * SS, X + band + pad, bottom - 6 * SS], fill=col)
        d.rectangle([X - half_w + 3.6 * SS, Y - 4 * SS - band - pad, X + half_w - 3.6 * SS, Y - 4 * SS + band + pad], fill=col)
    # repaint the green past the band ends so the cross stays inside the field
    clip = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(clip).polygon(shield_outline(X, Y, half_w, top, bottom), fill=GOLD)
    ImageDraw.Draw(clip).polygon(inner, fill=(0, 0, 0, 0))
    layer.alpha_composite(clip)
    br = 6.4 * SS
    bx, by = X, Y - 4 * SS
    d.ellipse([bx - br - 1.6 * SS, by - br - 1.6 * SS, bx + br + 1.6 * SS, by + br + 1.6 * SS], fill=DARK)
    d.ellipse([bx - br, by - br, bx + br, by + br], fill=GOLD_HI)
    d.chord([bx - br * 0.7, by - br * 0.7, bx + br * 0.7, by + br * 0.7], 200, 290, fill=(244, 218, 150, 255))

def shield_badge(img, cx=204, cy=204, r=40):
    layer = Image.new("RGBA", (W * SS, W * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    X, Y, R = cx * SS, cy * SS, r * SS
    sh = Image.new("RGBA", layer.size, (0, 0, 0, 0))
    ImageDraw.Draw(sh).ellipse([X - R, Y - R + 3 * SS, X + R, Y + R + 3 * SS], fill=(0, 0, 0, 120))
    sh = sh.filter(ImageFilter.GaussianBlur(4 * SS))
    badge_frame(d, X, Y, R)
    shield(layer, X, Y)
    combined = Image.alpha_composite(sh, layer).resize((W, W), Image.LANCZOS)
    return Image.alpha_composite(img, combined)

base = Image.open(sys.argv[1]).convert("RGBA")
icon = shield_badge(base)
icon.save(sys.argv[2], optimize=True)

if len(sys.argv) > 3:
    sheet = Image.new("RGBA", (W * 2, W * 2), (255, 255, 255, 255))
    sheet.paste((28, 30, 34, 255), (0, W, W * 2, W * 2))
    for row in range(2):
        for i, im in enumerate([base, icon]):
            sheet.alpha_composite(im, (i * W, row * W))
    sheet.convert("RGB").save(sys.argv[3])
