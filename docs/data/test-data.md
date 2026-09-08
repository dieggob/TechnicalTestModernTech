# Test data: Vehicle Maintenance Tracker

Hand-typed data for manual testing through the client or the API. Every value respects the validation rules of the API (password of at least 8 characters with a letter and a digit; VIN of up to 17 letters and digits, unique per user; cost of zero or more with at most two decimals; date performed not in the future; mileage of zero or more). Dates are before 2026-09-08.

Where links come from: after registering or requesting a reset, read the emailed link from http://localhost:5000/api/v1/dev/emails (Development only) or from the API log line `Email to <address>: <subject> <link>`.

## 1. Accounts

| Purpose | Email | Password | Notes |
|---|---|---|---|
| Primary user, owns the vehicles below | `ana.torres@example.com` | `Garage2026` | Verify through the emailed link to make the banner disappear |
| Second user, for ownership isolation | `luis.mendez@example.com` | `Wrench2026` | Must never see Ana's vehicles; may register Ana's VIN under his own account |
| Unverified user | `sam.reyes@example.com` | `Spark2026` | Leave unverified to test the banner, resend, and `Auth__RequireEmailVerification=true` |
| Password reset | `nora.kim@example.com` | `Brake2026` then `Brake2027` | Request a reset, follow the link, log in with the new password |

Invalid registrations, each expected to fail with a field message:

| Email | Password | Expected |
|---|---|---|
| `not-an-email` | `Garage2026` | Email is not a valid address |
| `ana.torres@example.com` | `Garage2026` | Conflict: the address is already registered |
| `new.user@example.com` | `short1` | Password must be at least 8 characters |
| `new.user@example.com` | `onlyletters` | Password must contain a digit |
| `new.user@example.com` | `12345678` | Password must contain a letter |

## 2. Vehicles (Ana Torres)

| # | Make | Model | Year | VIN | License plate | Current mileage |
|---|---|---|---|---|---|---|
| V1 | Toyota | Corolla | 2019 | `JTDBR32E720123456` | `ABC-1234` | 58200 |
| V2 | Ford | F-150 | 2016 | `1FTEW1EP5GFA98765` | `TRK-7781` | 121450 |
| V3 | Honda | CR-V | 2022 | `7FARW2H58NE045678` | `HND-2022` | 14980 |
| V4 | Volkswagen | Golf | 2014 | `WVWZZZAUZEW112233` | `GLF-0414` | 176300 |

Vehicle for Luis Mendez, sharing V1's VIN to prove uniqueness is per user:

| Make | Model | Year | VIN | License plate | Current mileage |
|---|---|---|---|---|---|
| Toyota | Corolla | 2019 | `JTDBR32E720123456` | `LMZ-9001` | 40100 |

Invalid vehicles, each expected to fail:

| Make | Model | Year | VIN | Plate | Mileage | Expected |
|---|---|---|---|---|---|---|
| Tesla | Model 3 | 2023 | `JTDBR32E720123456` | `TSL-0003` | 5000 | Conflict for Ana: a vehicle with this VIN is already registered |
| Mazda | 3 | 2020 | `VIN WITH SPACES` | `MZD-0003` | 5000 | VIN must be letters and digits |
| Mazda | 3 | 2020 | `TOOLONGVINVALUE123456` | `MZD-0003` | 5000 | VIN has at most 17 characters |
| Mazda | 3 | 1800 | `JM1BL1SF6A1234567` | `MZD-0003` | 5000 | Year out of range |
| Mazda | 3 | 2020 | `JM1BL1SF6A1234567` | `MZD-0003` | -1 | Mileage cannot be negative |
|  | 3 | 2020 | `JM1BL1SF6A1234567` | `MZD-0003` | 5000 | Make is required |

Edit case: change V1's plate to `ABC-9999` and its mileage to `60000`; the list shows both immediately.

## 3. Maintenance records

Newest first is the display order; the tables below are in the order to enter them so the mileage advance can be observed.

### V1 Toyota Corolla (starts at 58200 mi)

| # | Description | Cost (USD) | Date performed | Mileage at service | Service provider | Notes | Effect on vehicle mileage |
|---|---|---|---|---|---|---|---|
| R1 | Oil and filter change | 74.99 | 2026-02-14 | 55000 | Quick Lube Downtown | 0W-20 synthetic | none (below 58200) |
| R2 | Tire rotation and balance | 49.00 | 2026-05-03 | 57100 | Quick Lube Downtown | | none |
| R3 | Front brake pads and rotors | 412.50 | 2026-08-22 | 59350 | Mendoza Auto Repair | Ceramic pads | advances to 59350 |
| R4 | Cabin air filter | 28.75 | 2026-09-01 | 59800 | | Done at home | advances to 59800 |

### V2 Ford F-150 (starts at 121450 mi)

| # | Description | Cost (USD) | Date performed | Mileage at service | Service provider | Notes | Effect |
|---|---|---|---|---|---|---|---|
| R5 | Transmission fluid service | 289.00 | 2025-11-18 | 118900 | Ford Dealer North | | none |
| R6 | Spark plugs (8) | 236.40 | 2026-03-27 | 120500 | Ford Dealer North | Iridium | none |
| R7 | Battery replacement | 189.99 | 2026-07-09 | 122010 | AutoParts Plus | Group 65, 3-year warranty | advances to 122010 |

### V3 Honda CR-V (starts at 14980 mi)

| # | Description | Cost (USD) | Date performed | Mileage at service | Service provider | Notes | Effect |
|---|---|---|---|---|---|---|---|
| R8 | First service: oil change and inspection | 0.00 | 2026-01-20 | 9800 | Honda Dealer | Covered by warranty | none |
| R9 | Windshield chip repair | 65.00 | 2026-06-11 | 15200 | Glass Masters | Passenger side | advances to 15200 |

### V4 Volkswagen Golf (starts at 176300 mi)

| # | Description | Cost (USD) | Date performed | Mileage at service | Service provider | Notes | Effect |
|---|---|---|---|---|---|---|---|
| R10 | Timing belt and water pump | 985.00 | 2025-09-30 | 171000 | Euro Werks | Includes tensioner | none |
| R11 | Alternator replacement | 540.20 | 2026-04-15 | 175100 | Euro Werks | Remanufactured unit | none |
| R12 | Coolant flush | 119.00 | 2026-08-30 | 176900 | | | advances to 176900 |

Edit and delete cases:

| Action | Data | Expected |
|---|---|---|
| Edit R4 mileage down to 59000 | V1 | Record shows 59000; vehicle mileage stays 59800 |
| Edit R2 mileage up to 61000 | V1 | Vehicle mileage advances to 61000 |
| Delete R3 | V1 | Record disappears; vehicle mileage unchanged |
| Delete V4 | Ana's list | Vehicle and its three records disappear |

Invalid records, each expected to fail with a field message:

| Description | Cost | Date | Mileage | Expected |
|---|---|---|---|---|
|  | 50.00 | 2026-08-01 | 60000 | Description is required |
| Wiper blades | -5.00 | 2026-08-01 | 60000 | Cost cannot be negative |
| Wiper blades | 19.999 | 2026-08-01 | 60000 | Cost must have at most two decimals |
| Wiper blades | 19.99 | 2999-01-01 | 60000 | Date performed cannot be in the future |
| Wiper blades | 19.99 | 2026-08-01 | -10 | Mileage cannot be negative |
| (201 characters) | 19.99 | 2026-08-01 | 60000 | Description has at most 200 characters |

## 4. Ownership and session checks

| Check | Steps | Expected |
|---|---|---|
| Isolation | Log in as Luis, open `/vehicles/<id of Ana's V1>` | "This vehicle could not be found" (404 from the API) |
| Cross-user write | With Luis's token, `POST /api/v1/vehicles/<Ana's V1 id>/maintenance` | 404 |
| No token | `GET /api/v1/vehicles` without `Authorization` | 401 |
| Expired link | Wait 30 minutes after registering, then open the verification link | "This link is invalid or has expired" |
| Verification flag | Set `Auth__RequireEmailVerification=true`, log in as Sam, open the vehicles list | 403 with code `EmailNotVerified`; verifying Sam restores access |
| Rate limit | 11 failed logins within a minute from one address | The 11th answers 429 |

## 5. JSON bodies for the API

Register and log in (`POST /api/v1/auth/register`, `POST /api/v1/auth/login`):

```json
{ "email": "ana.torres@example.com", "password": "Garage2026" }
```

Create V1 (`POST /api/v1/vehicles`, bearer token required):

```json
{ "make": "Toyota", "model": "Corolla", "year": 2019, "vin": "JTDBR32E720123456", "licensePlate": "ABC-1234", "currentMileage": 58200 }
```

Create R3 (`POST /api/v1/vehicles/{vehicleId}/maintenance`):

```json
{ "description": "Front brake pads and rotors", "costUsd": 412.50, "datePerformed": "2026-08-22", "mileageAtService": 59350, "serviceProvider": "Mendoza Auto Repair", "notes": "Ceramic pads" }
```

Record with the optional fields empty (`serviceProvider` and `notes` may be `null` or omitted):

```json
{ "description": "Cabin air filter", "costUsd": 28.75, "datePerformed": "2026-09-01", "mileageAtService": 59800, "serviceProvider": null, "notes": "Done at home" }
```

## 6. Seeding Ana's account from a terminal

With the API running on port 5000:

```bash
API=http://localhost:5000/api/v1
curl -s -X POST $API/auth/register -H 'content-type: application/json' -d '{"email":"ana.torres@example.com","password":"Garage2026"}'
TOKEN=$(curl -s -X POST $API/auth/login -H 'content-type: application/json' -d '{"email":"ana.torres@example.com","password":"Garage2026"}' | sed 's/.*"token":"\([^"]*\)".*/\1/')
V1=$(curl -s -X POST $API/vehicles -H "authorization: Bearer $TOKEN" -H 'content-type: application/json' -d '{"make":"Toyota","model":"Corolla","year":2019,"vin":"JTDBR32E720123456","licensePlate":"ABC-1234","currentMileage":58200}' | sed 's/.*"id":"\([^"]*\)".*/\1/')
curl -s -X POST $API/vehicles/$V1/maintenance -H "authorization: Bearer $TOKEN" -H 'content-type: application/json' -d '{"description":"Oil and filter change","costUsd":74.99,"datePerformed":"2026-02-14","mileageAtService":55000,"serviceProvider":"Quick Lube Downtown","notes":"0W-20 synthetic"}'
curl -s -X POST $API/vehicles/$V1/maintenance -H "authorization: Bearer $TOKEN" -H 'content-type: application/json' -d '{"description":"Front brake pads and rotors","costUsd":412.50,"datePerformed":"2026-08-22","mileageAtService":59350,"serviceProvider":"Mendoza Auto Repair","notes":"Ceramic pads"}'
```

The second record advances V1 to 59350 mi; the vehicle list and the vehicle page both show it.
