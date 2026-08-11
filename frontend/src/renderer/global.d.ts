import type { AoBridge } from "../preload";

declare global {
	interface Window {
		ao?: AoBridge;
	}

	interface ImportMetaEnv {
		readonly VITE_SQUARE_POSTHOG_KEY?: string;
		readonly VITE_SQUARE_POSTHOG_HOST?: string;
	}
}

export {};
