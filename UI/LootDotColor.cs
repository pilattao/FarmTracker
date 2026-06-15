using System.Numerics;

namespace FarmTracker.UI;

/// <summary>Maps a coarse item category/rarity key to a dot color for the loot log. Pure (no ImGui),
/// so it is unit-tested on Linux.</summary>
public static class LootDotColor
{
    public static Vector4 For(string category)
    {
        switch ((category ?? "").Trim().ToLowerInvariant())
        {
            case "currency": return new Vector4(1.00f, 0.55f, 0.23f, 1f);  // orange
            case "unique":   return new Vector4(0.78f, 0.48f, 0.23f, 1f);  // brown-gold
            case "rare":     return new Vector4(0.91f, 0.87f, 0.42f, 1f);  // yellow
            case "magic":    return new Vector4(0.45f, 0.55f, 0.95f, 1f);  // blue
            case "gem":      return new Vector4(0.36f, 0.80f, 0.66f, 1f);  // teal
            case "map":      return new Vector4(0.70f, 0.55f, 0.90f, 1f);  // purple
            default:         return new Vector4(0.66f, 0.72f, 0.80f, 1f);  // neutral grey
        }
    }
}
