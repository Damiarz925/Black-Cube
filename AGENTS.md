# Repository UI authoring rule

- Treat serialized prefab and scene layout as designer-authored source of truth.
- Do not change UI `RectTransform` anchors, pivots, positions, sizes, sibling order, layout-group spacing, scrollbar placement, or assigned artwork unless the user explicitly requests those exact placement changes.
- For UI requests, implement behavior, components, serialized bindings, and clearly named scaffolding only. Leave final placement and sizing to the user in the Unity Editor.
- Do not run an authoring or migration command that rebuilds or normalizes existing UI layout. UI editor utilities must preserve existing transforms and sprites.
- If required UI objects are absent, create neutral, clearly named placeholders without arranging existing layout, then document what the user should place in the Inspector.
