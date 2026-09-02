using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace GoldenReels
{
    /// <summary>
    /// Random number source for the game. Uses a cryptographic RNG so outcomes
    /// are not predictable from frame timing or a seeded Unity Random.
    /// A deterministic seed can be supplied for automated tests.
    /// </summary>
    public class RNGService : IDisposable
    {
        private readonly RandomNumberGenerator _crypto;
        private readonly Random _seeded;

        public RNGService()
        {
            _crypto = RandomNumberGenerator.Create();
        }

        /// <summary>Deterministic constructor — for unit tests / reproducible runs.</summary>
        public RNGService(int seed)
        {
            _seeded = new Random(seed);
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public double NextDouble()
        {
            if (_seeded != null) return _seeded.NextDouble();

            var buffer = new byte[4];
            _crypto.GetBytes(buffer);
            return BitConverter.ToUInt32(buffer, 0) / (double)uint.MaxValue;
        }

        /// <summary>
        /// Picks one symbol using the weighted distribution defined on the symbol assets.
        /// Rare symbols (wild, seven, scatter) simply carry a smaller weight.
        /// </summary>
        public SlotSymbol PickWeighted(IReadOnlyList<SlotSymbol> symbols)
        {
            var total = 0;
            for (var i = 0; i < symbols.Count; i++) total += symbols[i].weight;

            var roll = NextDouble() * total;
            for (var i = 0; i < symbols.Count; i++)
            {
                roll -= symbols[i].weight;
                if (roll < 0) return symbols[i];
            }

            return symbols[symbols.Count - 1];
        }

        public void Dispose() => _crypto?.Dispose();
    }
}
