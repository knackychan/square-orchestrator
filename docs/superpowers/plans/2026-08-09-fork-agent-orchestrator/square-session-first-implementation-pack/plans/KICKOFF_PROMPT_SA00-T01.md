# Kickoff Prompt — SA00-T01 Create and pin the downstream fork

Paste this prompt into a fresh coding-agent session. Replace every `<...>` placeholder before sending.

```text
You are executing Square task SA00-T01 — Create and pin the downstream fork.

Implementation pack:
<ABSOLUTE_PATH_TO_SQUARE_SESSION_FIRST_IMPLEMENTATION_PACK>

Destination repository to create:
<ABSOLUTE_EMPTY_OR_ABSENT_DESTINATION_PATH>

Optional owner remote:
<HTTPS_OR_SSH_GIT_URL, OR WRITE NONE>

Identity for evidence records:
<PERSON_OR_AGENT_IDENTITY>

Read these files completely before doing anything:

1. <PACK>/START_HERE.md
2. <PACK>/README.md
3. <PACK>/plans/MASTER_IMPLEMENTATION_PLAN.md
4. <PACK>/docs/ARCHITECTURE_AMENDMENT.md
5. <PACK>/docs/UPSTREAM_GOVERNANCE.md
6. <PACK>/docs/TEST_AND_RELEASE_STRATEGY.md
7. <PACK>/plans/tasks/SA00-T01.md
8. <PACK>/scripts/bootstrap-square-fork.ps1
9. <PACK>/scripts/capture-authority-hashes.ps1

Task authority is the SA00-T01 packet. Do not infer permission to start SA00-T02 or any Square product implementation.

Before changing the filesystem, return a preflight report containing:

- PowerShell version;
- Git version;
- requested destination and whether it exists/is empty;
- upstream URL;
- requested tag;
- expected commit prefix;
- origin URL or explicit NONE;
- implementation-pack path;
- exact task write scope;
- every already-triggered STOP condition;
- the exact commands you intend to run.

After the preflight, perform only SA00-T01.

Locked requirements:

- Clone the full Git repository/history from
  https://github.com/Untrivial-ai/agent-orchestrator.git.
- Pin exact tag v0.12.1.
- Require HEAD to begin with 1df40e9.
- Rename the official remote to upstream.
- Create branch square/main at the pinned commit.
- Create annotated tag square-base-v0.12.1 at the pinned commit.
- Add the owner remote as origin only when a real URL was supplied.
- Do not push.
- Do not rebase or rewrite history.
- Do not modify backend, frontend, dependencies, migrations, generated files,
  CI, branding, telemetry, updater, daemon behavior, or product behavior.
- Add only the downstream authority/evidence/upstream/receipt roots authorized
  by the task packet.
- Preserve the upstream Apache-2.0 license and notices.
- Record exact upstream commit/tree/parents/tag/signature-visible metadata.
- Copy only the curated starter overlay and preserve its bytes. Full authority activation belongs to SA00-T04.
- Produce SA00-T01 evidence and completion receipt.
- Do not claim a clean working tree before committing the authorized downstream
  files; report the exact intended additions instead.

Preferred execution helper, after reviewing it:

PowerShell:

  & "<PACK>/scripts/bootstrap-square-fork.ps1" `
    -Destination "<DESTINATION>" `
    -OriginUrl "<ORIGIN_OR_OMIT_PARAMETER>" `
    -AuthorityPackPath "<PACK>" `
    -CreatedBy "<IDENTITY>"

Do not pass -OriginUrl when no owner remote exists.

After the helper completes, independently verify every result with Git commands.
Do not trust the script output alone.

Mandatory STOP conditions include:

- destination exists and is non-empty;
- tag is not exactly v0.12.1;
- resolved commit does not begin with 1df40e9;
- tag and HEAD differ;
- an unexpected origin/upstream remote already exists;
- the downstream baseline tag already exists at another commit;
- the curated starter overlay cannot be identified;
- any required product/build file would need modification;
- Git reports corruption or unsafe ownership;
- the task would require pushing or rewriting history.

At completion return:

1. status: PASS, FAIL, BLOCKED, or STOPPED_FOR_OWNER_DECISION;
2. full upstream commit and tree SHA;
3. exact tag and branch;
4. remotes with fetch/push URLs;
5. added/modified/deleted files;
6. evidence directory;
7. evidence manifest SHA-256;
8. completion receipt path and SHA-256;
9. starter-overlay paths copied;
10. whether any product/build file changed;
11. whether any push occurred;
12. remaining risks;
13. whether SA00-T02 is ready for independent dispatch.

A PASS for SA00-T01 means only that the fork foundation is correct. It does not
accept the unchanged AO Windows baseline and does not authorize Square product
code.
```
