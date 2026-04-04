# CA2 Branch Strategy

## Convention
- `master` — main stable branch, should always be in a working state
- `feature/ca2-network` — dedicated branch for CA2 networking work

## Workflow
1. Commits use prefixed messages: `net:` for networking code, `feat:` for features, `fix:` for bugs, `docs:` for documentation
4. Branches merged back to `master` once the feature was tested and stable
2. `ca2-baseline` tag applied to the first commit of project
2. `ca2-submit` tag applied to the final commit of project