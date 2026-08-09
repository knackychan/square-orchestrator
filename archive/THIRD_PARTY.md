# Third-party dependency register

Dependencies are pinned and admitted only in the task that proves or implements their boundary. Prototype
packages remain isolated and are not referenced by production projects.

| Package | Exact version | Role | License / redistribution conclusion | Architectural owner |
|---|---:|---|---|---|
| `typescript` | `6.0.3` | Repository development compiler | Development-only; package lock committed | Repository tooling |
| `@xterm/xterm` | `6.0.0` | SP00-T04 terminal renderer | MIT; license bytes are copied beside launchable proof assets | `prototypes/SharedUiProof` |
| `@xterm/addon-fit` | `0.11.0` | SP00-T04 terminal-fit adapter | MIT; license bytes are copied beside launchable proof assets | `prototypes/SharedUiProof` |
| `Microsoft.Web.WebView2` | `1.0.4129.50` | SP00-T04 WPF/WebView2 SDK | NuGet restore only in the isolated proof. Production redistribution and runtime bootstrap remain subject to SP00-T05/SP06/SP13 review. | `prototypes/SharedUiProof` |

The source archive does not include restored npm/NuGet package payloads, generated `dist`, `bin`, `obj`, or
WebView2 user-data directories. A launchable Windows proof restores the pinned packages, copies the two xterm
license files into generated assets, and records exact runtime versions in evidence.

Every later dependency change must record:

- package and exact version;
- runtime, development, or optional role;
- license and redistribution conclusion;
- security/provenance review;
- architectural owner and dependency direction; and
- lockfile/generated-file changes.
