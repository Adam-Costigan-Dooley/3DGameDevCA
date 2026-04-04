# CA2 Test Matrix


# Scenario->Expected Outcome->Actual result
1. Host starts session
    Session created, host player spawned, ScoreManager spawned
        Session created successfully, cube visible, scoreboard displayed
2. Client joins session
    Client connects, second player spawned, both cubes visible
        Client joined, both cubes visible on both windows
3. Host clicks Score Point
    ScorePlayer1 increments on both clients
        Score updated to 1 on both windows simultaneously
4. Client clicks Score Point
    ScorePlayer2 increments on both clients
        Score updated to 1 on both windows simultaneously
5. Both players score multiple times
    Scores accumulate independently and correctly
        P1: 3, P2: 5 displayScores retained on host, client sees current scores on rejoined correctly on both clients
6. Client disconnects and rejoins
    Scores persist (maintained by host StateAuthority)
        Scores retained on host, client sees current scores on rejoin
7. "(You)" label displays correctly
    Host sees "(You)" next to Player 1, client sees it next to Player 2
        Labels display correctly on each respective client