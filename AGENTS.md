# HoneyDrunk.Vault agent instructions

Own secrets/configuration access and provider adapters. Consumers use the exported secret/configuration contracts; provider SDKs stay behind their boundary. Never log secret values. Keep cache isolation, expiry and failure behavior covered by tests.

Start with [README.md](README.md) and the relevant source/tests. Project files and lockfiles own SDK, framework and dependency versions.

Read the [shared engineering conventions](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md) and this repository's owning documentation before editing. Apply the parts relevant to this stack; preserve existing public contracts, dependency direction and repository-specific behavior. Verify shared capabilities in current code before reusing them; a catalog entry or scaffold is not an implemented integration.

Work within the selected request. Preserve unrelated changes and use a separate worktree when needed. Review the final diff, use Conventional Commits and ready-for-review PRs with exactly one accurate `Authorship:` line and a `Request:` line; include the authorship in commit trailers. Run meaningful checks for the affected behavior and report the reviewed/tested revision, failures and unrun checks. For documentation-only changes, check links, paths and instruction consistency. Preserve required checks and inspect actual latest-head Sonar new-code findings where analysis applies; do not suppress findings or weaken gates to obtain a pass. Legacy Grid Review is retired; do not restore its workers, queues or bypass labels. A configured replacement reviewer is not evidence of a completed review or enforcing merge check.

## Verification

From the repository root for code/build changes:

```sh
dotnet restore HoneyDrunk.Vault.slnx
dotnet build HoneyDrunk.Vault.slnx -c Release --no-restore
dotnet test HoneyDrunk.Vault.slnx -c Release --no-build
```

Use the checked-in workflow and relevant test documentation for additional integration prerequisites, coverage and consumer checks. Do not use live resources or credentials merely to make a local check pass.

Read the [engineering guide](docs/engineering-guide.md) for repository-specific contracts and patterns.

## Code Review Rules

Apply the [shared review criteria](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md#code-review) to changed behavior, using the repository boundaries above. Report actionable findings with the failing path, concrete impact and a small corrective action; disclose unavailable evidence. These rules grant no cross-repository access or merge authority.

- Keep exported secrets/configuration contracts provider-neutral and SDK behavior inside the owning adapter. Check concrete existing access/cache helpers before accepting duplicate behavior or bypasses.
- Trace cache keys, expiry, refresh and provider failure across tenant/environment boundaries and concurrent requests. Flag stale-secret success, credential exposure, unbounded retries or a fallback that broadens access; preserve cancellation and disposal.
- Require contract and isolation/failure tests for changed providers and caches, including concurrent refresh where affected. Doubles prove their tested contract only; never request or log real secret values to establish review coverage.
