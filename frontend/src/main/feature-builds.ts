import type { UpdateSettings } from "./update-settings";

/** Feature-build release feeds are disabled with the updater through SA14. */
export const SQUARE_FEATURE_BUILDS_ENABLED = false;

export interface FeatureBuild {
	pr: number;
	title: string;
	base: string;
	sha: string;
	slug: string;
	buildId: string;
	publishedAt: string;
}

export function parseFeatureBuild(version: string): { pr: number } | null {
	const match = version.match(/-pr(\d+)\.\d{12}/);
	if (!match) return null;
	const pr = Number.parseInt(match[1], 10);
	return Number.isFinite(pr) && pr > 0 ? { pr } : null;
}

export function getActiveFeatureBuild(): { pr: number } | null {
	return null;
}

export async function listFeatureBuilds(): Promise<FeatureBuild[]> {
	return [];
}

export async function reconcileFeaturePin(
	settings: UpdateSettings,
): Promise<{ settings: UpdateSettings; cleared: boolean }> {
	return { settings, cleared: false };
}
