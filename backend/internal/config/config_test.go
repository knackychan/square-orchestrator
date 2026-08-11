package config

import (
	"os"
	"path/filepath"
	"testing"
	"time"
)

func TestLoadDefaults(t *testing.T) {
	// Clear every recognised var so we observe pure defaults regardless of the
	// surrounding environment.
	for _, k := range []string{"SQUARE_PORT", "SQUARE_REQUEST_TIMEOUT", "SQUARE_SHUTDOWN_TIMEOUT", "SQUARE_RUN_FILE", "SQUARE_DATA_DIR", "SQUARE_AGENT", "SQUARE_ALLOWED_ORIGINS", "SQUARE_TELEMETRY_EVENTS", "SQUARE_TELEMETRY_METRICS", "SQUARE_TELEMETRY_REMOTE", "SQUARE_TELEMETRY_POSTHOG_KEY", "SQUARE_TELEMETRY_POSTHOG_HOST", "SQUARE_TELEMETRY_DISABLED_EVENTS", "SQUARE_TELEMETRY_APP_VERSION"} {
		t.Setenv(k, "")
	}

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load: %v", err)
	}
	if cfg.Host != LoopbackHost {
		t.Errorf("Host = %q, want %q", cfg.Host, LoopbackHost)
	}
	if cfg.Port != DefaultPort {
		t.Errorf("Port = %d, want %d", cfg.Port, DefaultPort)
	}
	if cfg.RequestTimeout != DefaultRequestTimeout {
		t.Errorf("RequestTimeout = %s, want %s", cfg.RequestTimeout, DefaultRequestTimeout)
	}
	if cfg.ShutdownTimeout != DefaultShutdownTimeout {
		t.Errorf("ShutdownTimeout = %s, want %s", cfg.ShutdownTimeout, DefaultShutdownTimeout)
	}
	if cfg.RunFilePath == "" {
		t.Error("RunFilePath is empty, want a resolved default path")
	}
	homeDir, err := os.UserHomeDir()
	if err != nil {
		t.Fatalf("UserHomeDir: %v", err)
	}
	wantRunFilePath := filepath.Join(homeDir, ".square", "running.json")
	if cfg.RunFilePath != wantRunFilePath {
		t.Errorf("RunFilePath = %q, want %q", cfg.RunFilePath, wantRunFilePath)
	}
	if cfg.DataDir == "" {
		t.Error("DataDir is empty, want a resolved default path")
	}
	wantDataDir := filepath.Join(homeDir, ".square", "data")
	if cfg.DataDir != wantDataDir {
		t.Errorf("DataDir = %q, want %q", cfg.DataDir, wantDataDir)
	}
	if cfg.Telemetry.Remote != TelemetryRemoteOff || cfg.Telemetry.PostHogHost != DefaultTelemetryPostHogHost {
		t.Fatalf("Telemetry defaults = %+v", cfg.Telemetry)
	}
}

func TestLoadAbsolutizesRelativeOverrides(t *testing.T) {
	// A relative override must be resolved to absolute at Load time. The daemon
	// chdir's into its data dir at startup, so a relative path left as-is would
	// be re-resolved against the new cwd and double-nest state.
	t.Setenv("SQUARE_RUN_FILE", "rel-running.json")
	t.Setenv("SQUARE_DATA_DIR", "rel-data")

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load: %v", err)
	}
	if !filepath.IsAbs(cfg.RunFilePath) {
		t.Errorf("RunFilePath = %q, want absolute", cfg.RunFilePath)
	}
	if !filepath.IsAbs(cfg.DataDir) {
		t.Errorf("DataDir = %q, want absolute", cfg.DataDir)
	}
	cwd, err := os.Getwd()
	if err != nil {
		t.Fatal(err)
	}
	if want := filepath.Join(cwd, "rel-data"); cfg.DataDir != want {
		t.Errorf("DataDir = %q, want %q", cfg.DataDir, want)
	}
	if want := filepath.Join(cwd, "rel-running.json"); cfg.RunFilePath != want {
		t.Errorf("RunFilePath = %q, want %q", cfg.RunFilePath, want)
	}
}

func TestLoadOverrides(t *testing.T) {
	overrideDir := t.TempDir()
	runFilePath := filepath.Join(overrideDir, "ao-test-running.json")
	dataDir := filepath.Join(overrideDir, "ao-test-data")

	t.Setenv("SQUARE_PORT", "4002")
	t.Setenv("SQUARE_REQUEST_TIMEOUT", "5s")
	t.Setenv("SQUARE_SHUTDOWN_TIMEOUT", "3s")
	t.Setenv("SQUARE_RUN_FILE", runFilePath)
	t.Setenv("SQUARE_DATA_DIR", dataDir)
	// These inherited values must not enable Square telemetry.
	t.Setenv("SQUARE_TELEMETRY_EVENTS", "on")
	t.Setenv("SQUARE_TELEMETRY_METRICS", "on")
	t.Setenv("SQUARE_TELEMETRY_REMOTE", "posthog")
	t.Setenv("SQUARE_TELEMETRY_POSTHOG_KEY", "phc_test")
	t.Setenv("SQUARE_TELEMETRY_POSTHOG_HOST", "https://example.invalid")

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load: %v", err)
	}
	if cfg.Addr() != "127.0.0.1:4002" {
		t.Errorf("Addr() = %q, want 127.0.0.1:4002", cfg.Addr())
	}
	if cfg.RequestTimeout != 5*time.Second {
		t.Errorf("RequestTimeout = %s, want 5s", cfg.RequestTimeout)
	}
	if cfg.ShutdownTimeout != 3*time.Second {
		t.Errorf("ShutdownTimeout = %s, want 3s", cfg.ShutdownTimeout)
	}
	if cfg.RunFilePath != runFilePath {
		t.Errorf("RunFilePath = %q, want %q", cfg.RunFilePath, runFilePath)
	}
	if cfg.DataDir != dataDir {
		t.Errorf("DataDir = %q, want %q", cfg.DataDir, dataDir)
	}
	if cfg.Telemetry.Events || cfg.Telemetry.Metrics {
		t.Fatalf("Telemetry toggles = %+v", cfg.Telemetry)
	}
	if cfg.Telemetry.Remote != TelemetryRemoteOff || cfg.Telemetry.PostHogKey != "" || cfg.Telemetry.PostHogHost != "" {
		t.Fatalf("Telemetry remote = %+v", cfg.Telemetry)
	}
}

func TestLoadInvalid(t *testing.T) {
	tests := []struct {
		name string
		env  map[string]string
	}{
		{"non-numeric port", map[string]string{"SQUARE_PORT": "abc"}},
		{"port out of range", map[string]string{"SQUARE_PORT": "70000"}},
		{"bad request timeout", map[string]string{"SQUARE_REQUEST_TIMEOUT": "soon"}},
		{"bad shutdown timeout", map[string]string{"SQUARE_SHUTDOWN_TIMEOUT": "later"}},
		{"zero request timeout", map[string]string{"SQUARE_REQUEST_TIMEOUT": "0s"}},
		{"negative request timeout", map[string]string{"SQUARE_REQUEST_TIMEOUT": "-1s"}},
		{"zero shutdown timeout", map[string]string{"SQUARE_SHUTDOWN_TIMEOUT": "0s"}},
		{"negative shutdown timeout", map[string]string{"SQUARE_SHUTDOWN_TIMEOUT": "-5s"}},
		{"null origin", map[string]string{"SQUARE_ALLOWED_ORIGINS": "app://renderer,null"}},
		{"wildcard origin", map[string]string{"SQUARE_ALLOWED_ORIGINS": "*"}},
	}
	for _, tc := range tests {
		t.Run(tc.name, func(t *testing.T) {
			for k, v := range tc.env {
				t.Setenv(k, v)
			}
			if _, err := Load(); err == nil {
				t.Fatal("Load() = nil error, want error")
			}
		})
	}
}

func TestLoadAllowedOrigins(t *testing.T) {
	t.Run("default includes the packaged renderer origin", func(t *testing.T) {
		t.Setenv("SQUARE_ALLOWED_ORIGINS", "")
		cfg, err := Load()
		if err != nil {
			t.Fatalf("Load: %v", err)
		}
		found := false
		for _, origin := range cfg.AllowedOrigins {
			if origin == "app://renderer" {
				found = true
			}
		}
		if !found {
			t.Errorf("AllowedOrigins = %v, want app://renderer included", cfg.AllowedOrigins)
		}
	})

	t.Run("override replaces defaults and trims entries", func(t *testing.T) {
		t.Setenv("SQUARE_ALLOWED_ORIGINS", " app://renderer , http://localhost:9999 ,")
		cfg, err := Load()
		if err != nil {
			t.Fatalf("Load: %v", err)
		}
		want := []string{"app://renderer", "http://localhost:9999"}
		if len(cfg.AllowedOrigins) != len(want) {
			t.Fatalf("AllowedOrigins = %v, want %v", cfg.AllowedOrigins, want)
		}
		for i, origin := range want {
			if cfg.AllowedOrigins[i] != origin {
				t.Errorf("AllowedOrigins[%d] = %q, want %q", i, cfg.AllowedOrigins[i], origin)
			}
		}
	})
}

// Square ignores telemetry environment variables until an owner-approved
// policy is introduced. This prevents inherited AO settings from re-enabling
// a remote sink.
func TestLoadTelemetryDisabledEventsAndAppVersion(t *testing.T) {
	t.Setenv("SQUARE_TELEMETRY_DISABLED_EVENTS", " ao.v2.app.active , ao.renderer.* ,, ")
	t.Setenv("SQUARE_TELEMETRY_APP_VERSION", "  0.11.2  ")

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load: %v", err)
	}
	if len(cfg.Telemetry.DisabledEvents) != 0 || cfg.Telemetry.AppVersion != "" {
		t.Fatalf("Telemetry = %+v, want hard-off defaults", cfg.Telemetry)
	}
}

// An unparseable or blank list must never stop the daemon booting: the switch
// has to be usable in a hurry, so a bad entry is inert rather than fatal.
func TestLoadTelemetryDisabledEventsBlankIsInert(t *testing.T) {
	t.Setenv("SQUARE_TELEMETRY_DISABLED_EVENTS", " , , ")
	t.Setenv("SQUARE_TELEMETRY_APP_VERSION", "")

	cfg, err := Load()
	if err != nil {
		t.Fatalf("Load: %v", err)
	}
	if len(cfg.Telemetry.DisabledEvents) != 0 {
		t.Fatalf("DisabledEvents = %#v, want empty", cfg.Telemetry.DisabledEvents)
	}
	if cfg.Telemetry.AppVersion != "" {
		t.Fatalf("AppVersion = %q, want empty", cfg.Telemetry.AppVersion)
	}
}
