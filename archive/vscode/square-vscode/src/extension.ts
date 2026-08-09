import * as vscode from "vscode";

export function activate(context: vscode.ExtensionContext): void {
  context.subscriptions.push(vscode.commands.registerCommand("square.showStatus", async () => {
    await vscode.window.showInformationMessage("Square Orchestrator bootstrap: pipe activation begins in SP07-T01 after the protocol proof.");
  }));
}
export function deactivate(): void { /* The extension owns no workflow or terminal process. */ }
