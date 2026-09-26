"""Generates the item, buff and projectile sprites for the mod.

Run from the repository root:  python3 tools/generate_sprites.py
Requires Pillow (pip install pillow). Most sprites are drawn at half size and
scaled 2x with nearest-neighbour, which matches Terraria's pixel style.
Replace any PNG with hand-made art at the same size whenever you like.

The suit itself isn't a PNG: it is drawn in-game from the player's design
(Common/Suits/SuitRenderer.cs). The mod icon is made from a render of the
classic suit; see tools/README.md.
"""
from PIL import Image, ImageDraw

CLEAR = (0, 0, 0, 0)
OUTLINE = (20, 22, 30, 255)
RED = (170, 30, 30, 255)
RED_D = (110, 15, 20, 255)
RED_L = (220, 70, 60, 255)
GOLD = (235, 180, 50, 255)
GOLD_D = (170, 120, 30, 255)
STEEL = (160, 168, 180, 255)
STEEL_D = (95, 102, 115, 255)
STEEL_L = (215, 222, 230, 255)
CYAN = (140, 240, 255, 255)
CYAN_L = (225, 255, 255, 255)
CYAN_D = (60, 160, 200, 255)
DARK = (40, 44, 52, 255)
ORANGE = (255, 140, 40, 255)


def canvas(w, h):
    img = Image.new("RGBA", (w, h), CLEAR)
    return img, ImageDraw.Draw(img)


def save(img, path, scale=2):
    if scale != 1:
        img = img.resize((img.width * scale, img.height * scale), Image.NEAREST)
    img.save(path)
    print("wrote", path, img.width, "x", img.height)


def arc_reactor(tier):
    # 14x14 -> 28x28. Ring colour and core size grow with the tier.
    ring, ring_d = {1: (STEEL, STEEL_D), 2: (GOLD, GOLD_D), 3: (RED_L, RED_D)}[tier]
    img, d = canvas(14, 14)
    d.ellipse([0, 0, 13, 13], fill=OUTLINE)
    d.ellipse([1, 1, 12, 12], fill=ring)
    d.arc([1, 1, 12, 12], 20, 200, fill=ring_d)
    d.ellipse([3, 3, 10, 10], fill=DARK)
    if tier == 1:
        d.ellipse([4, 4, 9, 9], fill=CYAN_D)
        d.ellipse([5, 5, 8, 8], fill=CYAN)
    elif tier == 2:
        d.polygon([(6, 4), (7, 4), (10, 9), (3, 9)], fill=CYAN)  # triangle core
        d.point((6, 6), fill=CYAN_L); d.point((7, 6), fill=CYAN_L)
    else:
        d.ellipse([3, 3, 10, 10], fill=CYAN)
        d.ellipse([5, 5, 8, 8], fill=CYAN_L)
    # coil segments
    for x, y in [(6, 1), (7, 1), (1, 6), (1, 7), (12, 6), (12, 7), (6, 12), (7, 12)]:
        d.point((x, y), fill=ring_d)
    save(img, f"Content/Items/ArcReactorMk{tier}.png")


def stark_tablet():
    # 13x15 -> 26x30
    img, d = canvas(13, 15)
    d.rectangle([0, 0, 12, 14], fill=OUTLINE)
    d.rectangle([1, 1, 11, 13], fill=STEEL_D)
    d.rectangle([2, 2, 10, 11], fill=(20, 60, 90, 255))
    # holographic suit outline on the screen
    d.rectangle([5, 3, 7, 4], fill=CYAN)            # head
    d.rectangle([4, 5, 8, 8], fill=CYAN_D)          # body
    d.point((6, 6), fill=CYAN_L)                    # reactor
    d.rectangle([4, 9, 5, 10], fill=CYAN_D)         # legs
    d.rectangle([7, 9, 8, 10], fill=CYAN_D)
    d.point((6, 12), fill=STEEL_L)                  # button
    save(img, "Content/Items/StarkTablet.png")


def repulsors():
    # 14x14 -> 28x28: gauntlet palm with a glowing repulsor
    img, d = canvas(14, 14)
    d.rectangle([3, 5, 10, 13], fill=OUTLINE)
    d.rectangle([4, 6, 9, 12], fill=RED)
    d.rectangle([4, 10, 9, 12], fill=GOLD)
    for x in (3, 5, 7, 9):  # fingers
        d.rectangle([x, 1, x + 1, 5], fill=OUTLINE)
        d.rectangle([x, 2, x + 1, 5], fill=RED_L if x != 3 else RED)
    d.ellipse([5, 6, 8, 9], fill=CYAN)
    d.point((6, 7), fill=CYAN_L); d.point((7, 7), fill=CYAN_L)
    save(img, "Content/Abilities/Repulsors.png")


def shoulder_weapon():
    # 14x14 -> 28x28: shoulder pod with missiles
    img, d = canvas(14, 14)
    d.rectangle([1, 5, 12, 12], fill=OUTLINE)
    d.rectangle([2, 6, 11, 11], fill=RED)
    d.rectangle([2, 10, 11, 11], fill=RED_D)
    for x in (3, 6, 9):
        d.rectangle([x, 1, x + 1, 6], fill=STEEL)
        d.point((x, 0), fill=RED_L); d.point((x + 1, 0), fill=RED_L)
        d.point((x, 7), fill=DARK); d.point((x + 1, 7), fill=DARK)
    save(img, "Content/Abilities/ShoulderWeapon.png")


def unibeam():
    # 14x14 -> 28x28: reactor firing a beam
    img, d = canvas(14, 14)
    d.ellipse([0, 4, 7, 11], fill=OUTLINE)
    d.ellipse([1, 5, 6, 10], fill=GOLD)
    d.ellipse([2, 6, 5, 9], fill=CYAN_L)
    d.rectangle([6, 6, 13, 9], fill=CYAN)
    d.rectangle([6, 7, 13, 8], fill=CYAN_L)
    save(img, "Content/Abilities/Unibeam.png")


def buff_icon():
    # 16x16 -> 32x32: helmet face
    img, d = canvas(16, 16)
    d.rectangle([0, 0, 15, 15], fill=OUTLINE)
    d.rectangle([1, 1, 14, 14], fill=(30, 40, 70, 255))
    d.rectangle([4, 2, 11, 13], fill=RED)
    d.rectangle([3, 4, 12, 11], fill=RED)
    d.rectangle([5, 5, 10, 12], fill=GOLD)
    d.rectangle([5, 7, 6, 7], fill=CYAN_L)
    d.rectangle([9, 7, 10, 7], fill=CYAN_L)
    d.line([6, 11, 9, 11], fill=GOLD_D)
    save(img, "Content/Buffs/IronManSuit.png")


def suit_shot():
    # 16x16 at full size: soft white orb, tinted in code
    img = Image.new("RGBA", (16, 16), CLEAR)
    for y in range(16):
        for x in range(16):
            dx, dy = x - 7.5, y - 7.5
            dist = (dx * dx + dy * dy) ** 0.5 / 8.0
            if dist < 1.0:
                a = int(255 * (1.0 - dist) ** 1.5)
                img.putpixel((x, y), (255, 255, 255, a))
    save(img, "Content/Projectiles/SuitShot.png", scale=1)


def suit_missile():
    # 4x10 -> 8x20, pointing up
    img, d = canvas(4, 10)
    d.rectangle([1, 0, 2, 0], fill=RED_L)
    d.rectangle([0, 1, 3, 7], fill=STEEL)
    d.rectangle([0, 1, 0, 7], fill=STEEL_L)
    d.rectangle([3, 1, 3, 7], fill=STEEL_D)
    d.rectangle([1, 2, 2, 2], fill=RED)
    d.point((0, 8), fill=DARK); d.point((3, 8), fill=DARK)
    d.rectangle([1, 8, 2, 9], fill=ORANGE)
    save(img, "Content/Projectiles/SuitMissile.png")


# Attached weapons. Drawn in light greys pointing straight up with the grip at the bottom,
# because the game tints them with the suit's Weapon Colour and rotates them.
W_LIGHT = (245, 245, 245, 255)
W_MID = (200, 200, 205, 255)
W_DARK = (130, 130, 138, 255)
W_GRIP = (70, 70, 78, 255)


def weapon(name, w, h, draw):
    img, d = canvas(w, h)
    draw(d, w, h)
    save(img, f"Content/Weapons/{name}.png")


def energy_sword(d, w, h):  # 6x28
    d.rectangle([1, 0, 4, 20], fill=OUTLINE)
    d.rectangle([2, 1, 3, 20], fill=W_LIGHT)
    d.point((2, 0), fill=W_LIGHT)
    d.rectangle([0, 21, 5, 22], fill=W_DARK)   # guard
    d.rectangle([2, 23, 3, 27], fill=W_GRIP)


def katana(d, w, h):  # 5x32
    d.line([2, 0, 2, 22], fill=W_LIGHT)
    d.line([3, 1, 3, 22], fill=W_MID)
    d.line([1, 2, 1, 22], fill=OUTLINE)
    d.rectangle([0, 23, 4, 23], fill=W_DARK)   # tsuba
    d.rectangle([2, 24, 3, 31], fill=W_GRIP)
    for y in range(25, 31, 2):
        d.point((2, y), fill=W_DARK)


def battle_axe(d, w, h):  # 14x30
    d.rectangle([6, 2, 7, 29], fill=W_GRIP)
    d.polygon([(7, 1), (13, 0), (13, 11), (7, 9)], fill=W_MID)
    d.line([13, 0, 13, 11], fill=W_LIGHT)
    d.polygon([(6, 3), (2, 2), (2, 7), (6, 7)], fill=W_DARK)


def warhammer(d, w, h):  # 14x30
    d.rectangle([6, 8, 7, 29], fill=W_GRIP)
    d.rectangle([0, 0, 13, 8], fill=OUTLINE)
    d.rectangle([1, 1, 12, 7], fill=W_MID)
    d.rectangle([1, 1, 12, 2], fill=W_LIGHT)
    d.rectangle([5, 3, 8, 5], fill=W_DARK)


def reaper_scythe(d, w, h):  # 18x34
    d.rectangle([8, 3, 9, 33], fill=W_GRIP)
    d.polygon([(9, 1), (17, 3), (17, 6), (13, 4), (9, 5)], fill=W_LIGHT)
    d.line([0, 9, 4, 5], fill=W_MID)
    d.polygon([(8, 2), (2, 3), (0, 8), (3, 6), (8, 5)], fill=W_MID)
    d.line([0, 8, 2, 3], fill=W_LIGHT)


def throwing_spear(d, w, h):  # 5x36
    d.polygon([(2, 0), (4, 5), (2, 8), (0, 5)], fill=W_LIGHT)
    d.rectangle([2, 8, 2, 35], fill=W_GRIP)
    d.point((1, 9), fill=W_DARK); d.point((3, 9), fill=W_DARK)


def energy_lance(d, w, h):  # 7x40
    d.polygon([(3, 0), (6, 14), (3, 18), (0, 14)], fill=W_LIGHT)
    d.line([3, 2, 3, 16], fill=W_MID)
    d.rectangle([0, 19, 6, 20], fill=W_DARK)
    d.rectangle([2, 21, 4, 39], fill=W_GRIP)


def wrist_blade(d, w, h):  # 4x16
    d.polygon([(1, 0), (3, 3), (3, 11), (0, 11), (0, 2)], fill=W_LIGHT)
    d.line([3, 3, 3, 11], fill=W_MID)
    d.rectangle([0, 12, 3, 15], fill=W_GRIP)


def railgun(d, w, h):  # 10x42
    d.rectangle([3, 0, 6, 30], fill=OUTLINE)
    d.rectangle([4, 0, 5, 30], fill=W_MID)
    d.rectangle([2, 4, 7, 5], fill=W_DARK)     # rail rings
    d.rectangle([2, 12, 7, 13], fill=W_DARK)
    d.rectangle([2, 20, 7, 21], fill=W_DARK)
    d.rectangle([1, 26, 8, 36], fill=W_DARK)   # body
    d.rectangle([2, 27, 7, 35], fill=W_MID)
    d.rectangle([3, 28, 6, 29], fill=CYAN)
    d.rectangle([4, 37, 6, 41], fill=W_GRIP)   # stock


def disc(d, w, h, teeth):  # 14x14
    d.ellipse([0, 0, 13, 13], fill=W_MID)
    d.ellipse([1, 1, 12, 12], fill=W_LIGHT)
    d.ellipse([4, 4, 9, 9], fill=W_DARK)
    d.ellipse([5, 5, 8, 8], fill=CLEAR)
    for x, y in teeth:
        d.point((x, y), fill=W_LIGHT)


def chakram(d, w, h):
    disc(d, w, h, [])


def buzzsaw(d, w, h):
    d.ellipse([1, 1, 12, 12], fill=W_MID)
    for x, y in [(6, 0), (13, 6), (7, 13), (0, 7), (11, 1), (12, 11), (2, 12), (1, 2)]:
        d.point((x, y), fill=W_LIGHT)
    d.ellipse([5, 5, 8, 8], fill=W_GRIP)


def riot_shield(d, w, h):  # 14x22
    d.rectangle([0, 0, 13, 21], fill=OUTLINE)
    d.rectangle([1, 1, 12, 20], fill=W_MID)
    d.rectangle([1, 1, 12, 2], fill=W_LIGHT)
    d.rectangle([2, 5, 11, 7], fill=(120, 190, 230, 255))  # visor slot
    d.rectangle([6, 10, 7, 18], fill=W_DARK)


def weapon_icon(roman):
    # 14x14 -> 28x28: crossed blades with a numeral-like mark
    img, d = canvas(14, 14)
    d.line([1, 12, 12, 1], fill=STEEL_L)
    d.line([2, 12, 12, 2], fill=STEEL)
    d.line([1, 1, 12, 12], fill=STEEL_D)
    d.rectangle([5, 5, 8, 8], fill=RED)
    for i in range(roman):
        d.point((6 + i if roman == 1 else 5 + i * 2, 6), fill=GOLD)
        d.point((6 + i if roman == 1 else 5 + i * 2, 7), fill=GOLD)
    save(img, f"Content/Abilities/Weapon{'One' if roman == 1 else 'Two'}.png")


def weapons():
    import os
    os.makedirs("Content/Weapons", exist_ok=True)
    weapon("EnergySword", 6, 28, energy_sword)
    weapon("Katana", 5, 32, katana)
    weapon("BattleAxe", 14, 30, battle_axe)
    weapon("Warhammer", 14, 30, warhammer)
    weapon("ReaperScythe", 18, 34, reaper_scythe)
    weapon("ThrowingSpear", 5, 36, throwing_spear)
    weapon("EnergyLance", 7, 40, energy_lance)
    weapon("WristBlade", 4, 16, wrist_blade)
    weapon("Railgun", 10, 42, railgun)
    weapon("Chakram", 14, 14, chakram)
    weapon("Buzzsaw", 14, 14, buzzsaw)
    weapon("RiotShield", 14, 22, riot_shield)
    weapon_icon(1)
    weapon_icon(2)


if __name__ == "__main__":
    weapons()
    for tier in (1, 2, 3):
        arc_reactor(tier)
    stark_tablet()
    repulsors()
    shoulder_weapon()
    unibeam()
    buff_icon()
    suit_shot()
    suit_missile()
