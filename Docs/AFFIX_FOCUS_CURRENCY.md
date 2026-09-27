# Affix Focus currency

Affix Focus is an ordinary run currency. Its initial direct drop chance is 0.2% per enemy from combat level 50, before rarity additive and Enemy Power. It is appended to the stable currency enum; prior currency IDs are unchanged.

In the ordinary crafting panel, enable **Use Affix Focus**, choose **Prefix** or **Suffix**, arm a compatible ordinary currency, then apply it to equipment. One Affix Focus is spent **alongside** one ordinary Add Rare, Remove Rare, Magic Reroll, or Rare/Legendary Reroll. Normal→Magic and Magic→Rare upgrades cannot be focused. The chosen side is validated against the current item's rarity, open side capacity, eligible modifier list, and Crafting Potential before either currency is consumed. An invalid or unsuccessful operation must leave both balances unchanged.

Affix Focus changes the target side, not the roll's tier, value, affix family, or Crafting Potential cost. Production `EquipmentCrafting.CanApply` and `TryApply` are the authority; the Workbench should not implement a separate crafting rule. The current panel uses a fallback currency icon pending dedicated art.
