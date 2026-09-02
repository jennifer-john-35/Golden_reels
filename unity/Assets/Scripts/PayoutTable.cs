using System.Collections.Generic;
using UnityEngine;

namespace GoldenReels
{
    /// <summary>Outcome of evaluating one payline.</summary>
    public struct SpinResult
    {
        public int Payout;        // credits won
        public int FreeSpins;     // free spins awarded
        public bool IsWin;
        public string Message;    // human-readable line description
    }

    /// <summary>
    /// Pure, UI-free win evaluation for a 3-reel single-payline machine.
    /// Kept separate from presentation so the rules can be unit tested.
    ///
    /// Priority order:
    ///   1. Three scatters  -> free spins
    ///   2. Three wilds     -> top jackpot
    ///   3. Wild-assisted / exact three of a kind -> payout x bet
    ///   4. Any pair        -> half the bet back
    /// </summary>
    [CreateAssetMenu(fileName = "PayoutTable", menuName = "Golden Reels/Payout Table")]
    public class PayoutTable : ScriptableObject
    {
        [Tooltip("Free spins granted when the payline is all scatters.")]
        [Min(0)] public int freeSpinsAwarded = 3;

        [Tooltip("Return half the bet when exactly two symbols match.")]
        public bool payPairs = true;

        public SpinResult Evaluate(IReadOnlyList<SlotSymbol> line, int bet)
        {
            var reelCount = line.Count;
            var scatters = 0;
            var wilds = 0;
            var nonWild = new List<SlotSymbol>();

            foreach (var s in line)
            {
                if (s.isScatter) scatters++;
                else if (s.isWild) wilds++;
                else nonWild.Add(s);
            }

            // 1. Scatter bonus.
            if (scatters == reelCount)
            {
                return new SpinResult
                {
                    Payout = 0,
                    FreeSpins = freeSpinsAwarded,
                    IsWin = true,
                    Message = $"Scatter bonus! {freeSpinsAwarded} free spins awarded."
                };
            }

            // 2. All wilds — top prize.
            if (wilds == reelCount)
            {
                var wild = line[0];
                return new SpinResult
                {
                    Payout = wild.payout * bet,
                    FreeSpins = 0,
                    IsWin = true,
                    Message = $"Three Wilds! {wild.payout}x jackpot."
                };
            }

            // 3. Wilds substitute: win when every non-wild symbol matches
            //    and no scatter is blocking the line.
            var allMatch = nonWild.Count > 0 && nonWild.Count + wilds == reelCount;
            if (allMatch)
            {
                for (var i = 1; i < nonWild.Count; i++)
                {
                    if (nonWild[i].id != nonWild[0].id) { allMatch = false; break; }
                }
            }

            if (allMatch)
            {
                var baseSymbol = nonWild[0];
                var message = wilds > 0
                    ? $"{baseSymbol.label} line with {wilds} wild{(wilds > 1 ? "s" : "")} — {baseSymbol.payout}x!"
                    : $"Three {baseSymbol.label}s — {baseSymbol.payout}x!";

                return new SpinResult
                {
                    Payout = baseSymbol.payout * bet,
                    FreeSpins = 0,
                    IsWin = true,
                    Message = message
                };
            }

            // 4. Consolation pair (scatters excluded).
            if (payPairs)
            {
                var counts = new Dictionary<string, int>();
                foreach (var s in line)
                {
                    if (s.isScatter) continue;
                    counts.TryGetValue(s.id, out var n);
                    counts[s.id] = n + 1;
                }

                foreach (var pair in counts)
                {
                    if (pair.Value >= 2)
                    {
                        return new SpinResult
                        {
                            Payout = bet / 2,
                            FreeSpins = 0,
                            IsWin = true,
                            Message = "Matching pair — half your bet back."
                        };
                    }
                }
            }

            return new SpinResult { Payout = 0, FreeSpins = 0, IsWin = false, Message = "No win. Spin again." };
        }
    }
}
