/** Visitor preferences (language, theme) kept in localStorage; every access is guarded. */
export const LANG_KEY = "modernizer.lang";
export const THEME_KEY = "modernizer.theme";

export type Theme = "light" | "dark";

export function isTheme(value: unknown): value is Theme {
  return value === "light" || value === "dark";
}

/** A stored theme wins; otherwise the system preference; dark when nothing is known. */
export function resolveTheme(stored: string | null, prefersLight: boolean): Theme {
  if (isTheme(stored)) return stored;
  return prefersLight ? "light" : "dark";
}

export function readPref(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

export function writePref(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    /* private mode or blocked storage: the choice lives for this page only */
  }
}
