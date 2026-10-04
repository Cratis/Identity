# Identity - AI Assistant Instructions

## CI Runners - cratis-arc, always

This is a **private repository**. Every job GitHub runs on a hosted runner
(`ubuntu-*`, `windows-*`, `macos-*`) bills Actions minutes against the
organization's fixed monthly quota - with a one-minute minimum per job, Windows
at 2x and macOS at 10x. In October 2026, overlooked hosted jobs drained the
entire quota in four days. Public repositories run free; this one does not.

When writing or touching workflows here:

1. Default every job to the self-hosted `cratis-arc` scale set:
   - build/test/publish jobs: `runs-on: ${{ vars.RUNNER_FORCE || 'cratis-arc' }}`
   - short guard/gate jobs: `runs-on: ${{ vars.RUNNER_GATE || 'cratis-arc' }}`
   Never write a hosted label - not even as the fallback default inside an
   expression. If cratis-arc is down, flip the repo variable instead of editing
   workflows: `gh variable set RUNNER_FORCE -R Cratis/<repo> --body ubuntu-latest`
   (auto-reverts with `RUNNER_FORCE_UNTIL`). The full runner selection and
   override technique lives in
   `Cratis/Infrastructure/.github/workflows/determine-runner.yml`, consumed via
   `fromJson(needs.determine-runner.outputs.runner)`.
2. Never add Windows or macOS build legs to this repository.
3. Reusable workflows and actions called from this repository must also resolve
   to cratis-arc - a called workflow's jobs bill the caller, and the caller here
   is private.
4. The `Runner Guard` workflow fails any pull request that reintroduces a
   hosted-runner reference, and audits the repository daily.
