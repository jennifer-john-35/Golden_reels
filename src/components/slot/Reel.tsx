import { useEffect, useRef, useState } from "react";
import { spinSymbol, type SlotSymbol } from "@/lib/slot-engine";

const CELL_HEIGHT = 112; // px — must match the cell class below
const BLUR_SYMBOLS = 18; // how many filler symbols scroll past before the stop

interface ReelProps {
  /** Final symbol this reel should land on. */
  symbol: SlotSymbol;
  spinning: boolean;
  /** Stagger index — later reels stop later, like a real cabinet. */
  index: number;
  highlight: boolean;
}

/**
 * A single reel. While spinning we build a strip of random filler symbols
 * ending in the target symbol, then transition the strip upward with an
 * ease-out curve so the reel decelerates into place.
 */
export function Reel({ symbol, spinning, index, highlight }: ReelProps) {
  const [strip, setStrip] = useState<SlotSymbol[]>([symbol]);
  const [offset, setOffset] = useState(0);
  const [animating, setAnimating] = useState(false);
  const settled = useRef(symbol);

  useEffect(() => {
    if (!spinning) return;

    // Strip: current symbol, random blur symbols, then the outcome.
    const filler = Array.from({ length: BLUR_SYMBOLS }, spinSymbol);
    const next = [settled.current, ...filler, symbol];

    setStrip(next);
    setOffset(0);
    setAnimating(false);

    // Next frame: enable the transition and scroll to the last cell.
    const raf = requestAnimationFrame(() => {
      setAnimating(true);
      setOffset((next.length - 1) * CELL_HEIGHT);
    });
    settled.current = symbol;
    return () => cancelAnimationFrame(raf);
  }, [spinning, symbol]);

  const duration = 1.15 + index * 0.45;

  return (
    <div
      className={`relative h-28 w-[5.5rem] overflow-hidden rounded-xl border-2 bg-gradient-to-b from-secondary to-card transition-colors sm:w-28 ${
        highlight ? "animate-win-pulse border-gold" : "border-border"
      }`}
    >
      <div
        className="will-change-transform"
        style={{
          transform: `translateY(-${offset}px)`,
          transition: animating
            ? `transform ${duration}s cubic-bezier(0.16, 0.85, 0.22, 1)`
            : "none",
        }}
      >
        {strip.map((s, i) => (
          <div
            key={`${s.id}-${i}`}
            className="flex h-28 items-center justify-center text-5xl select-none sm:text-6xl"
            aria-hidden={i !== strip.length - 1}
          >
            {s.glyph}
          </div>
        ))}
      </div>
      {/* Glass sheen over the reel window */}
      <div className="pointer-events-none absolute inset-0 bg-gradient-to-b from-background/70 via-transparent to-background/70" />
    </div>
  );
}
