using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenReels
{
    /// <summary>
    /// A single reel. The outcome is decided by the machine *before* the
    /// animation starts, so the spin is purely presentational and can never
    /// influence fairness. The strip scrolls with an ease-out curve so the
    /// reel decelerates into place like a physical cabinet.
    ///
    /// Scene setup: an empty "Reel" RectTransform with a Mask + this script,
    /// and a child "Strip" RectTransform holding <see cref="cellPrefab"/> instances.
    /// </summary>
    public class Reel : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Container that gets scrolled. Cells are spawned as its children.")]
        [SerializeField] private RectTransform strip;

        [Tooltip("Prefab with an Image component used to render one symbol cell.")]
        [SerializeField] private Image cellPrefab;

        [Tooltip("Optional glow/frame shown when this reel is part of a win.")]
        [SerializeField] private GameObject winHighlight;

        [Header("Animation")]
        [Tooltip("Height of one symbol cell in UI units. Must match the prefab.")]
        [SerializeField] private float cellHeight = 160f;

        [Tooltip("How many blur symbols scroll past before the reel stops.")]
        [SerializeField] private int blurSymbols = 18;

        [Tooltip("Base spin duration. The machine adds a stagger per reel index.")]
        [SerializeField] private float baseDuration = 1.15f;

        [Tooltip("Extra seconds added per reel index so reels stop left to right.")]
        [SerializeField] private float staggerPerReel = 0.45f;

        private readonly List<Image> _cells = new List<Image>();
        private SlotSymbol _settled;

        public bool IsSpinning { get; private set; }

        /// <summary>Places a symbol instantly, without animating (initial state).</summary>
        public void SetImmediate(SlotSymbol symbol)
        {
            _settled = symbol;
            BuildStrip(new List<SlotSymbol> { symbol });
            strip.anchoredPosition = Vector2.zero;
            SetHighlight(false);
        }

        public void SetHighlight(bool on)
        {
            if (winHighlight != null) winHighlight.SetActive(on);
        }

        /// <summary>
        /// Spins to <paramref name="target"/>. <paramref name="index"/> is the reel's
        /// position (0-based) and drives the staggered stop.
        /// </summary>
        public IEnumerator Spin(SlotSymbol target, int index, RNGService rng, IReadOnlyList<SlotSymbol> pool)
        {
            IsSpinning = true;
            SetHighlight(false);

            // Strip = currently visible symbol, random blur symbols, then the outcome.
            var sequence = new List<SlotSymbol> { _settled != null ? _settled : target };
            for (var i = 0; i < blurSymbols; i++) sequence.Add(rng.PickWeighted(pool));
            sequence.Add(target);

            BuildStrip(sequence);

            var distance = (sequence.Count - 1) * cellHeight;
            var duration = baseDuration + index * staggerPerReel;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = EaseOutCubic(t);
                strip.anchoredPosition = new Vector2(0f, eased * distance);
                yield return null;
            }

            strip.anchoredPosition = new Vector2(0f, distance);
            _settled = target;
            IsSpinning = false;
        }

        private static float EaseOutCubic(float t) => 1f - Mathf.Pow(1f - t, 3f);

        /// <summary>Rebuilds the visible cells, reusing pooled Image instances.</summary>
        private void BuildStrip(IReadOnlyList<SlotSymbol> sequence)
        {
            while (_cells.Count < sequence.Count)
            {
                var cell = Instantiate(cellPrefab, strip);
                _cells.Add(cell);
            }

            for (var i = 0; i < _cells.Count; i++)
            {
                var active = i < sequence.Count;
                _cells[i].gameObject.SetActive(active);
                if (!active) continue;

                _cells[i].sprite = sequence[i].sprite;
                var rt = _cells[i].rectTransform;
                // Cells stack downward; scrolling the strip up reveals later cells.
                rt.anchoredPosition = new Vector2(0f, -i * cellHeight);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, cellHeight);
            }

            strip.anchoredPosition = Vector2.zero;
        }
    }
}
