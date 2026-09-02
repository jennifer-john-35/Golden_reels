using UnityEngine;

namespace GoldenReels
{
    /// <summary>
    /// A single slot symbol. Created as a ScriptableObject so designers can
    /// tune weights and payouts from the Unity Inspector without touching code.
    /// Create via: Assets > Create > Golden Reels > Slot Symbol
    /// </summary>
    [CreateAssetMenu(fileName = "SlotSymbol", menuName = "Golden Reels/Slot Symbol")]
    public class SlotSymbol : ScriptableObject
    {
        [Tooltip("Stable identifier, e.g. cherry, bell, wild, scatter.")]
        public string id = "cherry";

        [Tooltip("Display name shown in the paytable.")]
        public string label = "Cherry";

        [Tooltip("Sprite drawn on the reel.")]
        public Sprite sprite;

        [Tooltip("Relative frequency on the reel strip. Higher = more common.")]
        [Min(0)] public int weight = 10;

        [Tooltip("Multiplier applied to the bet when three of these land on the payline.")]
        [Min(0)] public int payout = 3;

        [Tooltip("Substitutes for any paying symbol.")]
        public bool isWild;

        [Tooltip("Awards free spins instead of a cash payout.")]
        public bool isScatter;
    }
}
