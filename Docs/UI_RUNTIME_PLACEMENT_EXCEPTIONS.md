# Runtime UI Placement Exceptions

The production rule is that permanent screen hierarchy and transforms are serialized. The remaining runtime placement/instantiation is intentional and data- or event-dependent:

- Passive connection segments follow assigned authored node/junction RectTransforms in Edit and Play modes.
- Item, currency, relic, and status tooltips instantiate authored prefabs and move temporarily to the hovered control/screen-safe position.
- Inventory entries, stat/mod rows, relic inventory entries, and active-status badges vary with live data; their containers and reusable appearance are authored.
- Item/equipment icon, element-marker, rarity/corruption-border, and filter-highlight children are data-driven overlays whose presence depends on the bound item. Their sprite/color comes from the visual library; their host slot size and layout remain prefab-authored.
- Corruption button-frame overlays are state-dependent decoration attached to authored buttons. They do not create or position a screen/panel root.
- The crafting cursor follows pointer interaction and is transient.
- Damage-number digit/accent/critical children and skill-projectile visuals are transient combat presentation.
- Damage popups, projectiles, loot drops, enemies, and encounter/world objects are transient gameplay presentation, not persistent screen layout.

Any new permanent panel, fixed button, fixed slot, or screen root must be added to a prefab/scene and bound through a serialized View component. It must not be created or positioned in `Awake`, `Start`, or `Update`.
