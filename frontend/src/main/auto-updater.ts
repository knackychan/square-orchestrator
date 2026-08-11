import type { UpdateChannel, UpdateSettings, UpdateStatus } from "./update-settings";

/** Square updater activation is prohibited through SA14. */
export const SQUARE_UPDATER_ENABLED = false;

export type UpdateCheckOptions = {
	settings?: UpdateSettings;
	requestId?: string;
};

/** Retained as a stable import seam; it never configures a release feed. */
export function configureFeed(_settings: Pick<UpdateSettings, "channel" | "feature">): void {}

export function getUpdateStatus(): UpdateStatus {
	return { state: "unsupported", message: "Square updates are disabled until SA14." };
}

export async function startAutoUpdates(_stateDir: string): Promise<void> {}

export async function setUpdateSettings(_stateDir: string, _settings: UpdateSettings): Promise<void> {}

export async function checkForUpdatesNow(
	_stateDir: string,
	_options: UpdateCheckOptions = {},
): Promise<void> {}

export async function returnToHome(_stateDir: string, _requestId?: string): Promise<void> {}

export async function downloadUpdateNow(_requestId?: string): Promise<void> {}

export function quitAndInstallUpdate(): void {}

export async function ensureUpdatePrefs(_stateDir: string): Promise<void> {}

// Keep the channel type referenced at this boundary for downstream callers
// that import the updater facade while the feed remains disabled.
export type DisabledUpdateChannel = UpdateChannel;
