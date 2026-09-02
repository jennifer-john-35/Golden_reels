# Golden Reels — Slot Game

A playable 3-reel, single-payline slot machine built with React + TypeScript (TanStack Start, Tailwind CSS).

## Game Overview

- 3 independent reels, one payline.
- Start with 500 credits; bet 5, 10, 25 or 50 per spin.
- Reels spin with staggered, decelerating animation and settle left to right.
- Credits, current bet, last win and a spin history are shown live.

## How to Run

```sh
npm install
npm run dev      # http://localhost:8080
npm run build    # production build
```

## Game Rules

| Symbol | Payout (3 of a kind) |
| --- | --- |
| 🍒 Cherry | 3× bet |
| 🍋 Lemon | 5× bet |
| 🔔 Bell | 10× bet |
| 💎 Diamond | 25× bet |
| 7️⃣ Lucky Seven | 60× bet |
| ⭐ Wild | 100× bet |
| 🎰 Scatter | 3 free spins |

- **Wild** substitutes for any paying symbol to complete a line.
- **Any pair** returns half the bet (consolation).
- **Three scatters** award 3 free spins (spins that cost no credits).

## Bonus Features

- Wild substitution.
- Scatter-triggered free spins with a dedicated spin button state.
- Half-bet return on pairs to smooth the credit curve.
- Win pulse/glow on the reel window, spin history log, credit reload when broke.

## Structure

```
src/
  lib/slot-engine.ts        # Pure game logic: symbols, weighted RNG, payout evaluation
  components/slot/Reel.tsx  # Single reel: strip building + deceleration animation
  routes/index.tsx          # Game screen: state, controls, meters, paytable, history
  styles.css                # Design tokens (colors, fonts, shadows, keyframes)
```

## Approach

The game logic is deliberately separated from rendering. `slot-engine.ts` is pure and
UI-free: it owns the symbol table (each symbol carries a reel **weight** and a **payout
multiplier**), a `crypto.getRandomValues`-backed RNG, and `evaluateSpin()` which resolves
scatters, all-wild, wild-assisted lines, exact matches and pairs in that priority order.
Weighted selection means rare symbols (Wild, Seven, Scatter) genuinely pay more without
special-casing anywhere in the UI.

The reel animation avoids frame-by-frame scripting: on each spin a strip is built from the
previously settled symbol, ~18 random filler symbols and the already-decided outcome, then
translated upward with a single ease-out cubic-bezier transition. Each reel uses a longer
duration than the one before it, which produces the staggered left-to-right stop of a real
cabinet. Because the outcome is decided by the RNG *before* the animation starts, the
animation is purely presentational and can never influence fairness.
