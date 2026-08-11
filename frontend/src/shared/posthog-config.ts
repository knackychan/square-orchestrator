// Square has no telemetry project until the owner explicitly opts one in.
// Keep both values empty so a packaged build cannot inherit the upstream AO
// project merely by importing the renderer telemetry module.
export const DEFAULT_POSTHOG_PROJECT_KEY = "";
export const DEFAULT_POSTHOG_HOST = "";
