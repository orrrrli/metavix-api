# Why the ownership-check pattern exists

Patient-side handlers used to do `GetPatientIdByUserIdAsync` + manual `callerId != request.PatientId`
+ `GetByIdAsync` + `if (patient is null) return NotFound`.

That pattern leaked a patient-id enumeration oracle: probing ids and reading 200 vs 404 revealed
which ids existed, even for patients the caller had no relationship with. It also cost an extra
query per request and scattered the auth check across the handler body instead of one operation.

Fixed by collapsing it into a single `GetOwnedPatientAsync`-style repository call (PRs #253, #254,
#255) applied across 18 handlers. `null` → `Forbidden` uniformly, never `NotFound`.

**When reviewing a new or changed handler**: flag any handler that does existence-check and
ownership-check as two separate steps, or that returns `NotFound` when an aggregate doesn't belong
to the caller. That regresses this fix.
