# Architecture decision records

Decisions made during implementation that reach beyond one slice, beyond what the implementation plan's Pattern Proposals Register records. Files are numbered `0001-<title>.md` and follow the context, decision, consequences structure.

| Record | Decision |
|---|---|
| [0001](0001-advance-vehicle-mileage-in-the-record-transaction.md) | Advance the vehicle's mileage inside the maintenance record's transaction, through `Vehicle.AdvanceMileage` and `IUnitOfWork` |
