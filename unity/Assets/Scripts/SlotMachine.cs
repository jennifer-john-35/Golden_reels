using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GoldenReels
{
    /// <summary>
    /// Game controller: owns credits, bet, free spins, and orchestrates
    /// RNG -> reel animation -> payout evaluation -> UI update.
    /// All rules live in <see cref="PayoutTable"/>; all randomness in
    /// <see cref="RNGService"/>. This class only coordinates them.
    /// </summary>
    public class SlotMachine : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private List<SlotSymbol> symbols = new List<SlotSymbol>();
        [SerializeField] private PayoutTable payoutTable;
        [SerializeField] private List<Reel> reels = new List<Reel>();

        [Header("Economy")]
        [SerializeField] private int startingCredits = 500;
        [SerializeField] private int[] betSteps = { 5, 10, 25, 50 };

        [Header("UI")]
        [SerializeField] private Text creditsLabel;
        [SerializeField] private Text betLabel;
        [SerializeField] private Text lastWinLabel;
        [SerializeField] private Text messageLabel;
        [SerializeField] private Button spinButton;
        [SerializeField] private Text spinButtonLabel;
        [SerializeField] private Button reloadButton;

        [Header("Audio (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip spinClip;
        [SerializeField] private AudioClip winClip;

        private RNGService _rng;
        private int _credits;
        private int _betIndex = 1;
        private int _freeSpins;
        private int _lastWin;
        private bool _spinning;

        private int Bet => betSteps[Mathf.Clamp(_betIndex, 0, betSteps.Length - 1)];

        private void Awake()
        {
            _rng = new RNGService();
            _credits = startingCredits;

            if (spinButton != null) spinButton.onClick.AddListener(OnSpinPressed);
            if (reloadButton != null) reloadButton.onClick.AddListener(Reload);
        }

        private void Start()
        {
            // Give each reel a starting face so the cabinet isn't blank.
            for (var i = 0; i < reels.Count; i++) reels[i].SetImmediate(_rng.PickWeighted(symbols));
            SetMessage("Place your bet and pull the lever.");
            RefreshUI();
        }

        private void OnDestroy() => _rng?.Dispose();

        /// <summary>Hook this to bet +/- buttons, or call SetBet(index) directly.</summary>
        public void CycleBet(int delta)
        {
            if (_spinning) return;
            _betIndex = (_betIndex + delta + betSteps.Length) % betSteps.Length;
            RefreshUI();
        }

        public void SetBet(int index)
        {
            if (_spinning) return;
            _betIndex = Mathf.Clamp(index, 0, betSteps.Length - 1);
            RefreshUI();
        }

        public void Reload()
        {
            if (_spinning) return;
            _credits = startingCredits;
            SetMessage("Credits reloaded. Good luck!");
            RefreshUI();
        }

        public void OnSpinPressed()
        {
            if (_spinning) return;

            var usingFreeSpin = _freeSpins > 0;
            if (!usingFreeSpin && _credits < Bet)
            {
                SetMessage("Not enough credits — lower your bet or reload.");
                return;
            }

            if (usingFreeSpin) _freeSpins--;
            else _credits -= Bet;

            StartCoroutine(SpinRoutine());
        }

        private IEnumerator SpinRoutine()
        {
            _spinning = true;
            _lastWin = 0;
            SetMessage("Spinning…");
            RefreshUI();
            Play(spinClip);

            // 1. Decide the outcome up front — animation never affects fairness.
            var outcome = new List<SlotSymbol>(reels.Count);
            for (var i = 0; i < reels.Count; i++) outcome.Add(_rng.PickWeighted(symbols));

            // 2. Launch every reel; they stop staggered left to right.
            for (var i = 0; i < reels.Count; i++)
            {
                StartCoroutine(reels[i].Spin(outcome[i], i, _rng, symbols));
            }

            // 3. Wait for the last reel to settle.
            var stillSpinning = true;
            while (stillSpinning)
            {
                stillSpinning = false;
                for (var i = 0; i < reels.Count; i++)
                {
                    if (reels[i].IsSpinning) { stillSpinning = true; break; }
                }
                yield return null;
            }

            // 4. Resolve.
            var result = payoutTable.Evaluate(outcome, Bet);
            _credits += result.Payout;
            _lastWin = result.Payout;
            _freeSpins += result.FreeSpins;

            if (result.IsWin)
            {
                Play(winClip);
                for (var i = 0; i < reels.Count; i++) reels[i].SetHighlight(true);
            }

            SetMessage(result.Message);
            _spinning = false;
            RefreshUI();
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }

        private void SetMessage(string text)
        {
            if (messageLabel != null) messageLabel.text = text;
        }

        private void RefreshUI()
        {
            if (creditsLabel != null) creditsLabel.text = _credits.ToString();
            if (betLabel != null) betLabel.text = Bet.ToString();
            if (lastWinLabel != null) lastWinLabel.text = _lastWin.ToString();

            if (spinButtonLabel != null)
            {
                spinButtonLabel.text = _spinning
                    ? "Spinning"
                    : _freeSpins > 0 ? $"Free Spin ({_freeSpins})" : "Spin";
            }

            if (spinButton != null)
            {
                spinButton.interactable = !_spinning && (_freeSpins > 0 || _credits >= Bet);
            }

            if (reloadButton != null)
            {
                reloadButton.gameObject.SetActive(!_spinning && _freeSpins == 0 && _credits < Bet);
            }
        }
    }
}
