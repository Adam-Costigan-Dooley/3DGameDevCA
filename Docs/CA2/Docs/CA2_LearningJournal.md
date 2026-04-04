# CA2 Learning Journal

## Section 1 — Weekly Evidence Log

| 8 | 16–22 Mar | Fusion session setup; NetworkRunner configured; Player prefab spawning on two clients; CA2 scope decided (Option C — score/game state) | net: fusion session setup | Fig TBD(two-client baseline) | Photon player IDs start at 2, not 1 — had to adjust score attribution logic |
| 9 | 23–29 Mar | Delayed, Minor catch up work on CA3 labs from weeks not done prior.
| 10 | 30–5 Mar-Apr | Score system: Networked properties, RPC with authority check, polished UI; test matrix; docs | net: networked score system, docs: CA2 plan, docs: test matrix | Fig TBD (score system, both clients) | RPC initially used RpcSources.InputAuthority on a shared object with no input authority — changed to RpcSources.All |


## Section 2 — Critical Discussion

### Why I Chose Option C (Score / Game State)

I chose a networked score system because it is a clearly scoped feature that could be fully implemented and tested within the networking sprint, and could easily serve as the basis for any other feature or be more appliccably imported to use as a baseline template in future projects. A smaller, fully correct feature was a better use of the time than a larger one that had less direct emphasis on the networking features being studied.

### Networked Property vs RPC

The score system uses both Networked properties and an RPC, each for a specific reason. The two score values (ScorePlayer1, ScorePlayer2) are stored as Networked int properties because they represent persistent game state that every client needs to see at all times. Fusion automatically synchronises Networked properties to all clients, including late joiners, if a new client connects mid-game, they immediately receive the current scores without any extra logic. 

The RPC (RpcRequestScore) is used as a request mechanism: when a client clicks "Score Point", the RPC sends a message to the StateAuthority asking it to increment the score.

### A Concrete Authority Model Decision

When I first implemented the ScoreManager, I attached it to individual player prefabs and used RpcSources.InputAuthority to send score requests. This failed because the ScoreManager needed to be a shared object (one instance tracking both players' scores), and a shared object has no InputAuthority assigned to any specific player. The RPC silently never fired from the client because RpcSources.InputAuthority requires the sender to have input authority over the object. Moving to RpcSources.All and spawning the ScoreManager as a standalone NetworkObject (not owned by any player) fixed the issue. The StateAuthority guard inside the RPC ensures only the host processes the score change, preventing double-counting.

### One Thing I Would Do Differently

I would set up my Git repository structure correctly from day one. I initially committed CA2 work into the wrong repository (the over arching folder the project is in) and had to rebuild my commit history in the correct project folder. If I were starting over, I would verify which .git directory I was committing to before making any commits, and I would establish the feature branch immediately rather than committing networking code directly to master. I also did not commit during the second week, and committed very late into the extension granted in the third week as I was putting off the project weekly labs to work on assignments.

## Section 3 — Lessons Learned

1. **Photon Fusion player IDs do not start at 1.** I assumed PlayerRef.RawEncoded would be 1 for the host and 2 for the client, but they are actually 2 and 3. Hardcoding player identification based on assumed IDs caused both players to score for the same team. Adding debug logging revealed the actual values and the fix was straightforward, but the assumption cost debugging time.

2. **Shared NetworkObjects need RpcSources.All, not RpcSources.InputAuthority.** When a NetworkObject is not owned by a specific player (like a shared ScoreManager), no client has InputAuthority over it. Using RpcSources.InputAuthority meant RPCs from non-host clients were silently dropped. Changing to RpcSources.All and relying on the HasStateAuthority guard inside the RPC resolved the issue.

3. **Always verify which Git repository you are committing to.** Having nested .git directories (one in the project root, one in a subfolder) led to commits going to the wrong repository. This was especially painful because it meant that when i did finally push to the right github, i had lost a "in between" step in my implementation progress. Simply doubly checking my repo would have caught this.
