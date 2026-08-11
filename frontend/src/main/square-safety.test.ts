// @vitest-environment node
import { describe, expect, it, vi } from "vitest";

vi.mock("electron", () => ({
	app: { isPackaged: false, getVersion: () => "0.0.0" },
	BrowserWindow: { getAllWindows: () => [] },
	dialog: { showMessageBox: vi.fn() },
}));
import { SQUARE_UPDATER_ENABLED, getUpdateStatus, startAutoUpdates, checkForUpdatesNow, downloadUpdateNow, quitAndInstallUpdate } from "./auto-updater";

describe("Square SA00 safety defaults", () => {
	it("keeps telemetry project defaults empty", async () => {
		const config = await import("../shared/posthog-config");
		expect(config.DEFAULT_POSTHOG_PROJECT_KEY).toBe("");
		expect(config.DEFAULT_POSTHOG_HOST).toBe("");
	});

	it("hard-disables every updater entry point", async () => {
		expect(SQUARE_UPDATER_ENABLED).toBe(false);
		expect(getUpdateStatus()).toMatchObject({ state: "unsupported" });
		await expect(startAutoUpdates("C:/square")).resolves.toBeUndefined();
		await expect(checkForUpdatesNow("C:/square")).resolves.toBeUndefined();
		await expect(downloadUpdateNow()).resolves.toBeUndefined();
		expect(() => quitAndInstallUpdate()).not.toThrow();
	});
});
