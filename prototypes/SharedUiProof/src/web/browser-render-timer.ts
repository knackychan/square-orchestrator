import type { RenderTimer, ScheduledWork } from "./render-queue.js";

export class BrowserRenderTimer implements RenderTimer {
  schedule(callback: () => void, delayMs: number): ScheduledWork {
    let cancelled = false;
    let timeoutId: number | null = null;
    let animationId: number | null = null;
    const animate = (): void => {
      if (cancelled) return;
      animationId = requestAnimationFrame(() => {
        animationId = null;
        if (!cancelled) callback();
      });
    };
    if (delayMs === 0) animate();
    else timeoutId = window.setTimeout(() => { timeoutId = null; animate(); }, delayMs);
    return Object.freeze({
      cancel(): void {
        cancelled = true;
        if (timeoutId !== null) window.clearTimeout(timeoutId);
        if (animationId !== null) cancelAnimationFrame(animationId);
      }
    });
  }
}
