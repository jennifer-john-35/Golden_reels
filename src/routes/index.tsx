import { createFileRoute } from "@tanstack/react-router";
import { useCallback, useRef, useState } from "react";
import { Reel } from "@/components/slot/Reel";
import {
  FREE_SPINS_AWARDED,
  SYMBOLS,
  evaluateSpin,
  spinReels,
  symbolById,
  type SlotSymbol,
  type SpinResult,
} from "@/lib/slot-engine";

export const Route = createFileRoute("/")({
  head: () => ({
    meta: [
      { title: "Golden Reels — Play the 3-Reel Slot Machine" },
      {
        name: "description",
        content:
          "Spin Golden Reels, a browser slot machine with weighted RNG, wild substitutions, scatter free spins and a full paytable.",
      },
      { property: "og:title", content: "Golden Reels — Play the 3-Reel Slot Machine" },
      {
        property: "og:description",
        content:
          "Spin Golden Reels, a browser slot machine with weighted RNG, wilds, scatters and free spins.",
      },
    ],
    links: [
      { rel: "preconnect", href: "https://fonts.googleapis.com" },
      { rel: "preconnect", href: "https://fonts.gstatic.com", crossOrigin: "anonymous" },
      {
        rel: "stylesheet",
        href: "https://fonts.googleapis.com/css2?family=Bungee&family=Space+Grotesk:wght@400;500;700&display=swap",
      },
    ],
  }),
  component: SlotGame,
});

const STARTING_CREDITS = 500;
const BET_STEPS = [5, 10, 25, 50];

function SlotGame() {
  const [credits, setCredits] = useState(STARTING_CREDITS);
  const [bet, setBet] = useState(10);
  const [reels, setReels] = useState<SlotSymbol[]>([
    symbolById("cherry"),
    symbolById("bell"),
    symbolById("seven"),
  ]);
  const [spinning, setSpinning] = useState(false);
  const [result, setResult] = useState<SpinResult | null>(null);
  const [freeSpins, setFreeSpins] = useState(0);
  const [history, setHistory] = useState<string[]>([]);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const spin = useCallback(() => {
    if (spinning) return;
    const usingFreeSpin = freeSpins > 0;
    if (!usingFreeSpin && credits < bet) return;

    if (usingFreeSpin) setFreeSpins((n) => n - 1);
    else setCredits((c) => c - bet);

    setResult(null);
    setSpinning(true);

    const outcome = spinReels();
    setReels(outcome);

    // Resolve once the last reel has settled (matches the Reel stagger).
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => {
      const evaluated = evaluateSpin(outcome, bet);
      setSpinning(false);
      setResult(evaluated);
      if (evaluated.payout > 0) setCredits((c) => c + evaluated.payout);
      if (evaluated.freeSpins > 0) setFreeSpins((n) => n + evaluated.freeSpins);
      setHistory((h) =>
        [
          `${outcome.map((s) => s.glyph).join(" ")}  ${
            evaluated.payout > 0
              ? `+${evaluated.payout}`
              : evaluated.freeSpins > 0
                ? `+${evaluated.freeSpins} free`
                : `−${usingFreeSpin ? 0 : bet}`
          }`,
          ...h,
        ].slice(0, 8),
      );
    }, 2350);
  }, [spinning, credits, bet, freeSpins]);

  const canSpin = !spinning && (freeSpins > 0 || credits >= bet);
  const winningIndexes = result?.isWin ? [0, 1, 2] : [];

  return (
    <main className="mx-auto flex min-h-screen w-full max-w-3xl flex-col items-center gap-8 px-4 py-10">
      <header className="text-center">
        <p className="text-xs tracking-[0.4em] text-gold-soft/70 uppercase">Slot Assignment</p>
        <h1 className="mt-2 font-display text-4xl text-gold drop-shadow-[0_0_18px_oklch(0.83_0.16_85/0.4)] sm:text-6xl">
          Golden Reels
        </h1>
      </header>

      {/* Cabinet */}
      <section className="w-full rounded-3xl border-2 border-gold/40 bg-card p-5 shadow-cabinet sm:p-8">
        <div className="grid grid-cols-3 gap-3 rounded-2xl bg-background/70 p-3 sm:gap-5 sm:p-6">
          {reels.map((symbol, i) => (
            <div key={i} className="flex justify-center">
              <Reel
                symbol={symbol}
                spinning={spinning}
                index={i}
                highlight={winningIndexes.includes(i)}
              />
            </div>
          ))}
        </div>

        <p
          className={`mt-5 min-h-6 text-center text-sm font-medium ${
            result?.isWin ? "text-gold" : "text-muted-foreground"
          }`}
          role="status"
          aria-live="polite"
        >
          {spinning ? "Spinning…" : (result?.message ?? "Place your bet and pull the lever.")}
        </p>

        {/* Controls */}
        <div className="mt-6 flex flex-col gap-5">
          <div className="flex flex-wrap items-center justify-center gap-2">
            {BET_STEPS.map((amount) => (
              <button
                key={amount}
                type="button"
                onClick={() => setBet(amount)}
                disabled={spinning}
                className={`rounded-full border px-4 py-1.5 text-sm font-semibold transition-colors disabled:opacity-50 ${
                  bet === amount
                    ? "border-gold bg-gold text-primary-foreground"
                    : "border-border bg-secondary text-foreground hover:border-gold/60"
                }`}
              >
                {amount}
              </button>
            ))}
          </div>

          <button
            type="button"
            onClick={spin}
            disabled={!canSpin}
            className="mx-auto w-full max-w-xs rounded-2xl border-b-4 border-crimson bg-accent px-8 py-4 font-display text-xl tracking-wide text-accent-foreground shadow-glow transition-transform active:translate-y-0.5 disabled:cursor-not-allowed disabled:opacity-45 disabled:shadow-none sm:text-2xl"
          >
            {spinning ? "Spinning" : freeSpins > 0 ? `Free Spin (${freeSpins})` : "Spin"}
          </button>
        </div>
      </section>

      {/* Meters */}
      <section className="grid w-full grid-cols-3 gap-3">
        <Meter label="Credits" value={credits} />
        <Meter label="Bet" value={bet} />
        <Meter label="Last Win" value={result?.payout ?? 0} />
      </section>

      {credits < bet && freeSpins === 0 && !spinning && (
        <button
          type="button"
          onClick={() => setCredits(STARTING_CREDITS)}
          className="rounded-full border border-gold/60 px-5 py-2 text-sm font-semibold text-gold hover:bg-gold/10"
        >
          Out of credits — reload {STARTING_CREDITS}
        </button>
      )}

      {/* Paytable + history */}
      <section className="grid w-full gap-4 sm:grid-cols-2">
        <div className="rounded-2xl border border-border bg-card/70 p-5">
          <h2 className="font-display text-base text-gold-soft">Paytable</h2>
          <ul className="mt-3 space-y-2 text-sm">
            {SYMBOLS.map((s) => (
              <li key={s.id} className="flex items-center justify-between gap-2">
                <span className="flex items-center gap-2">
                  <span className="text-xl">{s.glyph}</span>
                  <span className="text-muted-foreground">{s.label}</span>
                </span>
                <span className="font-semibold text-foreground">
                  {s.id === "scatter" ? `${FREE_SPINS_AWARDED} free spins` : `${s.payout}× bet`}
                </span>
              </li>
            ))}
          </ul>
          <p className="mt-4 text-xs text-muted-foreground">
            Wilds substitute for any paying symbol. Any pair returns half the bet. Three scatters
            trigger free spins.
          </p>
        </div>

        <div className="rounded-2xl border border-border bg-card/70 p-5">
          <h2 className="font-display text-base text-gold-soft">Spin History</h2>
          {history.length === 0 ? (
            <p className="mt-3 text-sm text-muted-foreground">No spins yet.</p>
          ) : (
            <ul className="mt-3 space-y-2 font-mono text-sm">
              {history.map((line, i) => (
                <li key={i} className="flex justify-between border-b border-border/50 pb-1">
                  <span>{line}</span>
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    </main>
  );
}

function Meter({ label, value }: { label: string; value: number }) {
  return (
    <div className="rounded-2xl border border-gold/30 bg-card px-4 py-3 text-center">
      <p className="text-[0.65rem] tracking-[0.25em] text-muted-foreground uppercase">{label}</p>
      <p className="mt-1 font-display text-2xl text-gold tabular-nums">{value}</p>
    </div>
  );
}
