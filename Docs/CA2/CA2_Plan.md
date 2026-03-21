# CA2 Plan — Networked Score / Game State

## Chosen Scope
Option C — Networked Score / Game State

## Why This Scope
A shared score system is a well-defined, testable feature that can be completed and polished within the two-week networking sprint. It avoids the complexity of physics synchronisation (movement, projectiles) while still demonstrating core Fusion concepts: session management, networked properties, RPCs, and authority checks.

I explicitly decided not to attempt networked movement or projectiles, as client-side prediction and lag compensation would have added significant complexity that I feel would be better suited to a more in depth project rather then a feeler into networking as a whole.

## Synchronisation Approach

### Networked Properties
- `ScorePlayer1` and `ScorePlayer2` will be `[Networked]` int properties on the ScoreManager
- Fusion automatically replicates these to all clients whenever the StateAuthority modifies them

### RPC
- `RpcRequestScore` is used for clients to request a score change
- The RPC is sent from any client (`RpcSources.All`) to the StateAuthority (`RpcTargets.StateAuthority`)
- The StateAuthority validates the request and modifies the `[Networked]` properties
- This ensures only the authority can write state, preventing desync