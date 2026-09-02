# Golden Reels — Unity Scripts

C# implementation of the 3-reel, single-payline slot machine: reels, RNG,
payline evaluation, wild substitution and scatter-triggered free spins.

```
unity/Assets/Scripts/
  SlotSymbol.cs   # ScriptableObject: sprite, weight, payout, isWild, isScatter
  RNGService.cs   # Cryptographic weighted RNG (+ seeded ctor for tests)
  PayoutTable.cs  # ScriptableObject: pure payline evaluation
  Reel.cs         # One reel: strip building + ease-out deceleration
  SlotMachine.cs  # Controller: credits, bet, free spins, spin orchestration
```

## Setup (Unity 2022.3 LTS or newer)

1. **Create the project** — Unity Hub → New Project → *2D (URP or Built-in)*.
2. **Copy the scripts** — drop `Assets/Scripts` from this folder into your
   project's `Assets/` folder. Wait for the compile to finish (no errors expected).
3. **Create the symbols** — `Assets > Create > Golden Reels > Slot Symbol`, once
   per symbol. Suggested values:

   | id | label | weight | payout | wild | scatter |
   | --- | --- | --- | --- | --- | --- |
   | cherry | Cherry | 30 | 3 | ☐ | ☐ |
   | lemon | Lemon | 25 | 5 | ☐ | ☐ |
   | bell | Bell | 18 | 10 | ☐ | ☐ |
   | diamond | Diamond | 10 | 25 | ☐ | ☐ |
   | seven | Lucky Seven | 6 | 60 | ☐ | ☐ |
   | wild | Wild | 4 | 100 | ☑ | ☐ |
   | scatter | Scatter | 5 | 0 | ☐ | ☑ |

   Assign a sprite to each (any 160×160 PNG set to *Sprite (2D and UI)*).
4. **Create the payout table** — `Assets > Create > Golden Reels > Payout Table`.
   Leave `freeSpinsAwarded = 3` and `payPairs = true`.

## Scene hierarchy

```
Canvas (Screen Space - Overlay, CanvasScaler: Scale With Screen Size 1920x1080)
├── Cabinet (Image)
│   ├── Reel0 (RectTransform 160x160 + Mask + Image + Reel.cs)
│   │   └── Strip (RectTransform, anchor/pivot top-center)
│   ├── Reel1 (same)
│   └── Reel2 (same)
├── CreditsLabel / BetLabel / LastWinLabel / MessageLabel (Text)
├── SpinButton (Button, child Text = SpinButtonLabel)
├── ReloadButton (Button)
└── GameController (empty GameObject + SlotMachine.cs)
```

- **Cell prefab**: a UI `Image`, 160×160, pivot top-center. Save as
  `Assets/Prefabs/Cell.prefab`.
- On each **Reel**: assign `Strip`, `Cell.prefab`, an optional `winHighlight`
  child, and keep `cellHeight = 160` so it matches the prefab.
- On **SlotMachine**: assign the 7 symbols, the PayoutTable asset, the 3 reels
  (in left-to-right order) and the UI labels/buttons. Optionally add an
  `AudioSource` plus spin/win clips.
- Bet buttons: hook `SlotMachine.CycleBet` with `+1` / `-1`, or
  `SlotMachine.SetBet(index)` for direct 5 / 10 / 25 / 50 selection.

## Run in the Editor

Press **Play**. Each reel gets a random starting face; press **Spin** to bet and
resolve a payline.

## WebGL build

1. `File > Build Settings > WebGL > Switch Platform`.
2. Add the scene via **Add Open Scenes**.
3. `Player Settings > Publishing Settings > Compression Format = Disabled`
   (or Gzip if your host sets the right headers).
4. **Build** → pick an output folder → serve it over HTTP
   (`python3 -m http.server` inside the build folder; `file://` will not work).

## Rules implemented

Evaluation priority in `PayoutTable.Evaluate`:

1. Three scatters → 3 free spins (no credit cost on the next spins).
2. Three wilds → 100× bet jackpot.
3. Wild-assisted or exact three of a kind → symbol payout × bet.
4. Any pair → half the bet back.

The outcome is drawn by `RNGService` **before** the animation starts, so the
reel animation is purely presentational and can never affect fairness.
