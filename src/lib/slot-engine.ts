/**
 * Slot machine game engine.
 *
 * Pure, UI-free logic so the rules can be reasoned about (and tested)
 * independently from rendering: symbol table, weighted RNG, and payout
 * evaluation for a 3-reel / single-payline machine.
 */

export type SymbolId = "cherry" | "lemon" | "bell" | "diamond" | "seven" | "wild" | "scatter";

export interface SlotSymbol {
  id: SymbolId;
  glyph: string;
  label: string;
  /** Relative weight on the reel strip — higher means more frequent. */
  weight: number;
  /** Multiplier applied to the bet when three of this symbol land. */
  payout: number;
}

export const SYMBOLS: SlotSymbol[] = [
  { id: "cherry", glyph: "🍒", label: "Cherry", weight: 26, payout: 3 },
  { id: "lemon", glyph: "🍋", label: "Lemon", weight: 22, payout: 5 },
  { id: "bell", glyph: "🔔", label: "Bell", weight: 16, payout: 10 },
  { id: "diamond", glyph: "💎", label: "Diamond", weight: 10, payout: 25 },
  { id: "seven", glyph: "7️⃣", label: "Lucky Seven", weight: 6, payout: 60 },
  { id: "wild", glyph: "⭐", label: "Wild", weight: 5, payout: 100 },
  { id: "scatter", glyph: "🎰", label: "Scatter", weight: 5, payout: 0 },
];

export const REEL_COUNT = 3;
export const FREE_SPINS_AWARDED = 3;

const TOTAL_WEIGHT = SYMBOLS.reduce((sum, s) => sum + s.weight, 0);

export function symbolById(id: SymbolId): SlotSymbol {
  const found = SYMBOLS.find((s) => s.id === id);
  if (!found) throw new Error(`Unknown symbol: ${id}`);
  return found;
}

/**
 * Cryptographically-seeded random float in [0, 1).
 * Falls back to Math.random when crypto is unavailable (SSR/old runtimes).
 */
function random(): number {
  if (typeof crypto !== "undefined" && crypto.getRandomValues) {
    const buf = new Uint32Array(1);
    crypto.getRandomValues(buf);
    return buf[0]! / 2 ** 32;
  }
  return Math.random();
}

/** Picks one symbol using the weighted distribution above. */
export function spinSymbol(): SlotSymbol {
  let roll = random() * TOTAL_WEIGHT;
  for (const symbol of SYMBOLS) {
    roll -= symbol.weight;
    if (roll < 0) return symbol;
  }
  return SYMBOLS[SYMBOLS.length - 1]!;
}

/** Spins every reel independently — no reel influences another. */
export function spinReels(): SlotSymbol[] {
  return Array.from({ length: REEL_COUNT }, spinSymbol);
}

export interface SpinResult {
  symbols: SlotSymbol[];
  payout: number;
  /** Human-readable description of the winning (or losing) line. */
  message: string;
  freeSpins: number;
  isWin: boolean;
}

/**
 * Evaluates a spin.
 *
 * Rules:
 *  - Three matching symbols pay `payout * bet`.
 *  - Wilds substitute for any paying symbol; an all-wild line pays the top prize.
 *  - Three scatters award free spins instead of a cash payout.
 *  - Two matching symbols (wild-assisted included) return half the bet.
 */
export function evaluateSpin(symbols: SlotSymbol[], bet: number): SpinResult {
  const scatters = symbols.filter((s) => s.id === "scatter").length;
  if (scatters === REEL_COUNT) {
    return {
      symbols,
      payout: 0,
      message: `Scatter bonus! ${FREE_SPINS_AWARDED} free spins awarded.`,
      freeSpins: FREE_SPINS_AWARDED,
      isWin: true,
    };
  }

  const wilds = symbols.filter((s) => s.id === "wild").length;
  const nonWild = symbols.filter((s) => s.id !== "wild" && s.id !== "scatter");

  // All wilds — top prize.
  if (wilds === REEL_COUNT) {
    const wild = symbolById("wild");
    return {
      symbols,
      payout: wild.payout * bet,
      message: `Three Wilds! ${wild.payout}x jackpot.`,
      freeSpins: 0,
      isWin: true,
    };
  }

  // Wilds substitute: a line wins when every non-wild symbol matches.
  const allMatch =
    nonWild.length > 0 &&
    nonWild.every((s) => s.id === nonWild[0]!.id) &&
    nonWild.length + wilds === REEL_COUNT;

  if (allMatch) {
    const base = nonWild[0]!;
    const payout = base.payout * bet;
    return {
      symbols,
      payout,
      message: wilds
        ? `${base.label} line with ${wilds} wild${wilds > 1 ? "s" : ""} — ${base.payout}x!`
        : `Three ${base.label}s — ${base.payout}x!`,
      freeSpins: 0,
      isWin: true,
    };
  }

  // Consolation: any pair (wilds count toward it) returns half the bet.
  const counts = new Map<SymbolId, number>();
  for (const s of symbols) counts.set(s.id, (counts.get(s.id) ?? 0) + 1);
  const hasPair = [...counts.entries()].some(([id, n]) => id !== "scatter" && n >= 2);
  if (hasPair) {
    return {
      symbols,
      payout: Math.floor(bet / 2),
      message: "Matching pair — half your bet back.",
      freeSpins: 0,
      isWin: true,
    };
  }

  return { symbols, payout: 0, message: "No win. Spin again.", freeSpins: 0, isWin: false };
}
