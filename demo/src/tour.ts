import { MODES, type Mode } from "./routing.ts";

/** A guided walk through the four modes; captions are i18n keys resolved by the caller. */
export const TOUR_STEP_MS = 3500;

export interface TourState {
  index: number;
  running: boolean;
}

export const idle: TourState = { index: -1, running: false };

export function start(): TourState {
  return { index: 0, running: true };
}

export function advance(state: TourState): TourState {
  if (!state.running) return state;
  const next = state.index + 1;
  return next < MODES.length ? { index: next, running: true } : { index: MODES.length, running: false };
}

export function stop(): TourState {
  return idle;
}

export function modeAt(state: TourState): Mode | null {
  return state.index >= 0 && state.index < MODES.length ? (MODES[state.index] ?? null) : null;
}

export function finished(state: TourState): boolean {
  return state.index === MODES.length && !state.running;
}
