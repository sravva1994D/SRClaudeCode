# Sprint 1 Spike Results

**Audience**: tech lead and developers. Covers plan Sprint 1 steps 7 and 8.

## 1. Full-text search on LocalDB (step 7, risk K2, Q5)

**Result: not available.** Full-text search cannot be used on the development database.

- `SERVERPROPERTY('Edition')` reports Express Edition (64-bit), version 15.0.4382.1.
- `FULLTEXTSERVICEPROPERTY('IsFullTextInstalled')` returns 0.
- `CREATE FULLTEXT CATALOG` and `CREATE FULLTEXT INDEX` both fail with error 9982: "Cannot use full-text search in user instance."

**Decision**: use the plan's fallback. Keyword search in visit history (Q5) is limited to one patient's visits, filtered with an indexed query and a `LIKE` scan. That set is small enough to meet the 2-second target. Revisit when production is set up: Standard Edition or Azure SQL may support full-text indexes, and the search code should check for that at the time.

## 2. Password hashing work factor (step 8, CLAUDE.md 10.2)

Measured on the development machine (Release build, one hash and one verify per work factor, BCrypt.Net-Next):

| Work factor | Hash | Verify | Hash length |
|-------------|------|--------|-------------|
| 10 | 219 ms | 179 ms | 60 |
| 11 | 253 ms | 169 ms | 60 |
| 12 | 315 ms | 302 ms | 60 |
| 13 | 560 ms | 576 ms | 60 |

**Decision (proposed)**: work factor **12**. It is about 300 ms per login, which is well within the login experience budget, and it gives a margin above the common minimum of 10. Store it in configuration (CLAUDE.md 10.2), not in code. The tech lead should confirm before the auth sprint.
