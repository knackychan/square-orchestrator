import { FitAddon } from "@xterm/addon-fit";
import { Terminal, type ITheme } from "@xterm/xterm";
import type { ProofTheme } from "../shared/protocol.js";

export interface XtermFactory {
  readonly Terminal: typeof Terminal;
  readonly FitAddon: typeof FitAddon;
}

export function loadXtermFactory(): XtermFactory {
  return Object.freeze({ Terminal, FitAddon });
}

export function xtermTheme(theme: ProofTheme): ITheme {
  if (theme === "light") {
    return Object.freeze({ background: "#ffffff", foreground: "#1b1b1d", cursor: "#005fb8", selectionBackground: "#bfdfff" });
  }
  if (theme === "high-contrast") {
    return Object.freeze({ background: "#000000", foreground: "#ffffff", cursor: "#ffff00", selectionBackground: "#1aebff" });
  }
  return Object.freeze({ background: "#0c0e12", foreground: "#eef1f6", cursor: "#80bfff", selectionBackground: "#294f73" });
}
