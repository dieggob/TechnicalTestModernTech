# 0001. Advance the vehicle's mileage inside the maintenance record's transaction

Date: 2026-09-08 (slice S27). Status: accepted.

## Context

A maintenance record carries the mileage at service. The product decision is that a record whose mileage is above the vehicle's current mileage advances the vehicle, and that editing or deleting a record never lowers it. Two aggregates change together, and a crash between the two writes would leave a record whose mileage the vehicle does not reflect. The plain `DbContext` injected into repositories (slice S06) was enough while every use case wrote one aggregate.

## Decision

- The rule lives in the domain: `Vehicle.AdvanceMileage(mileage, now)` returns whether the vehicle moved, so the service tells the vehicle instead of comparing fields itself.
- `MaintenanceService` writes the record and the vehicle inside one transaction through `IUnitOfWork.BeginTransactionAsync`, an Application-layer abstraction implemented over the `DbContext` in Infrastructure. Single-aggregate writes keep using the repositories and `SaveChangesAsync` directly.
- The write endpoints return `MaintenanceWriteResult` with the record and `vehicleCurrentMileage`, so the client refreshes the vehicle without a second request.

## Consequences

- The invariant is testable in a unit test of `Vehicle` and in an integration test that observes both rows after a create, an edit with a lower mileage, and a delete.
- `IUnitOfWork` is the second persistence abstraction beside the repositories; it exists only for multi-aggregate writes, which keeps S06's simpler shape everywhere else.
- Lowering a vehicle's mileage is only possible through the vehicle's own edit form, by design.
