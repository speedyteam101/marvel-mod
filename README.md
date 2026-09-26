# Marvel: Iron Man — a tModLoader mod for Terraria

An unofficial, fan-made Marvel mod for **tModLoader** (Terraria 1.4.4). Become Iron Man and build your own suit in the Suit Workshop.

![Preset and randomly generated suits](docs/suits.png)

*The ten preset suits followed by eight random designs, rendered by the mod's own suit renderer.*

## Getting started

1. Craft a **Stark Tablet** and an **Arc Reactor Mk I** (recipes below).
2. Equip the Arc Reactor as an accessory.
3. Press **I** (Suit Up) or right-click with the Stark Tablet to suit up. Press it again to suit down.
4. Press **O** (Suit Workshop) or left-click with the Stark Tablet to design your suit.

Both keys can be changed in *Settings → Controls → Mod Controls*. If another mod already uses I or O, rebind them there.

## Items

| Item | What it does | Recipe |
| --- | --- | --- |
| **Stark Tablet** | Left click: open the Suit Workshop. Right click: suit up / down. | 5 Iron/Lead Bar, 10 Glass, 1 Fallen Star @ Anvil |
| **Arc Reactor Mk I** (accessory) | Lets you suit up. Grants Repulsors and the Shoulder Weapon while suited. | 12 Iron/Lead Bar, 3 Fallen Star, 5 Glass @ Anvil |
| **Arc Reactor Mk II** (accessory) | Stronger suit, no knockback, and adds the Unibeam. | Arc Reactor Mk I, 10 Hallowed Bar, 5 Soul of Light, 5 Soul of Might @ Mythril/Orichalcum Anvil |
| **Arc Reactor Mk III** (accessory) | Strongest suit, and it's immune to lava. | Arc Reactor Mk II, 10 Luminite Bar, 10 Vortex Fragment @ Ancient Manipulator |

Only one Arc Reactor can be worn at a time.

## The suit

While suited up you get:

- **Flight**: hold Jump in the air to fly up, hold Down to dive, left/right to steer. Flying resets fall damage.
- **Stats** (before the Systems options below):

  | Reactor | Defense | Damage reduction | Damage (all) | Crit | Move speed | Regen | Flight speed |
  | --- | --- | --- | --- | --- | --- | --- | --- |
  | Mk I | +10 | 4% | +10% | +4% | +10% | 1 HP/s | 7 |
  | Mk II | +28 | 10% | +25% | +8% | +20% | 3 HP/s | 10 |
  | Mk III | +50 | 16% | +45% | +12% | +30% | 6 HP/s | 13 |

- Water breathing, no fall damage, glowing eyes and reactor. No knockback from Mk II. Lava immunity at Mk III.
- **Ability items** put into your inventory automatically. They disappear when you suit down or drop them.

| Ability | Reactor | What it does |
| --- | --- | --- |
| **Repulsors** | Mk I+ | Palm blasts (28 base damage). Changes with *Repulsor Mode*. |
| **Shoulder Weapon** | Mk I+ | Fires the weapon chosen in *Shoulder Weapon*. |
| **Unibeam** | Mk II+ | Hold to fire a chest beam that stops at walls. It overheats after a few seconds, then cools down. Changes with *Unibeam Mode*. |

Ability damage grows after you beat Skeletron, the Wall of Flesh, any mechanical boss, Plantera and the Moon Lord (+10/+20/+20/+25/+50 base damage). It is then multiplied by 1x / 1.6x / 2.4x for the Mk I / II / III reactor.

## Suit Workshop

**1,262 options in 34 categories.** Every category is independent, so they combine into about 6.6 × 10⁴² different suits. The workshop shows both numbers, calculated from the catalogue in `Common/Suits/SuitCatalog.cs`.

| Group | Categories (number of options) |
| --- | --- |
| Armour | Helmet (16), Faceplate (10), Eyes (12), Chest (16), Arc Reactor (10), Shoulders (10), Gauntlets (12), Belt (8), Legs (12), Boots (10), Back Module (10) |
| Paint Job | Pattern (14), Emblem (12), Finish (6: Metallic, Matte, Chrome, Gloss, Stealth, Battle-Damaged) |
| Colours | 11 colour slots with 96 colours each: Primary, Secondary, Accent, Trim, Undersuit, Pattern, Emblem, Eye Glow, Reactor Glow, Repulsor Glow, Thrusters |
| Effects | Glow Strength (5), Glow Pulse (4), Thruster Trail (8) |
| Systems | Repulsor Mode (6), Shoulder Weapon (6), Unibeam Mode (4), Armour Plating (5), Thrusters (5), Power Core (5) |

The **Systems** options change how the suit plays:

- **Repulsor Mode**: Standard, Rapid (2x fire rate, weaker), Spread (3 shots), Heavy (slow, explosive), Piercing (goes through 5 enemies), Twin (2 parallel shots).
- **Shoulder Weapon**: Micro-Missiles (3 homing missiles), Minigun, Flare Pods (5 burning flares), Shoulder Laser (piercing), Heavy Rocket (big explosion), Cluster Bombs (split into 6 bomblets).
- **Unibeam Mode**: Focused, Wide (wider, weaker), Pulse (flickers on and off, stronger), Overcharge (much stronger, short, long cooldown).
- **Armour Plating**: Light, Standard, Reinforced, Heavy, Vibranium Alloy (defense against speed).
- **Thrusters**: Standard, Racing, Heavy-Lift, Hover (holds altitude when you let go), Afterburner.
- **Power Core**: Balanced, Overclocked, Efficient, Regenerative, Unstable (damage, attack speed and regeneration trade-offs).

The workshop has a live animated preview, the suit's stats for your current reactor, **Random Armour / Random Colours / Random Everything** buttons, **10 presets**, and **5 save slots** per character. Your design is saved with the character and synced to other players in multiplayer.

### How the suit is drawn

The suit isn't a fixed sprite sheet. `Common/Suits/SuitRenderer.cs` builds it from small ASCII pixel masks for each part (`Common/Suits/SuitParts.cs`). It adds patterns, emblem, finish shading and an outline, then caches the result as a texture. To add a new helmet, chest or other part, add a mask to `SuitParts.cs`. It shows up in the workshop automatically, and the option count updates too.

## Installing from source

1. Copy this repository into your tModLoader `ModSources` folder, **in a folder named `MarvelMod`**. The folder name must match the mod's internal name, so rename it if you cloned it as `marvel-mod`.
2. In tModLoader, go to *Workshop → Develop Mods* and click **Build + Reload** next to *Marvel: Iron Man*.

`tools/generate_sprites.py` regenerates the item, buff and projectile sprites (needs Pillow).

This is an unofficial fan project and is not affiliated with or endorsed by Marvel or Disney.
