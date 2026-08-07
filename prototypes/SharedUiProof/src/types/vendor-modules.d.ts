declare module "@xterm/xterm" {
  export interface IDisposable { dispose(): void; }
  export interface ITerminalAddon { activate(terminal: Terminal): void; dispose(): void; }
  export interface ITheme {
    background?: string;
    foreground?: string;
    cursor?: string;
    selectionBackground?: string;
  }
  export interface ITerminalOptions {
    allowProposedApi?: boolean;
    convertEol?: boolean;
    cursorBlink?: boolean;
    disableStdin?: boolean;
    screenReaderMode?: boolean;
    scrollback?: number;
    fontFamily?: string;
    fontSize?: number;
    lineHeight?: number;
    minimumContrastRatio?: number;
    theme?: ITheme;
  }
  export class Terminal {
    constructor(options?: ITerminalOptions);
    options: ITerminalOptions;
    readonly cols: number;
    readonly rows: number;
    open(element: HTMLElement): void;
    loadAddon(addon: ITerminalAddon): void;
    write(data: Uint8Array | string, callback?: () => void): void;
    focus(): void;
    clear(): void;
    dispose(): void;
    onData(listener: (data: string) => void): IDisposable;
  }
}

declare module "@xterm/addon-fit" {
  import type { ITerminalAddon } from "@xterm/xterm";
  export class FitAddon implements ITerminalAddon {
    activate(terminal: import("@xterm/xterm").Terminal): void;
    fit(): void;
    dispose(): void;
  }
}
