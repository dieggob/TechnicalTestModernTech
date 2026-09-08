# e2e

Playwright browser journeys against running apps, driven through the installed Google Chrome (`channel: chrome`).

| Path | Holds |
|---|---|
| `tests/auth/`, `tests/vehicles/`, `tests/maintenance/` | One spec per journey group, each seeding its own account and data through the API |
| `tests/seed/` | Runs `scripts/lib/seed.mjs` against the API under test and checks the sample data in the UI, including that a second run changes nothing |
| `tests/support/api.ts` | `ApiHelper`: register, login, create vehicles and records, read the emailed links from the Development-only `/api/v1/dev/emails` |
| `tests/support/pages/` | Page objects (`LoginPage`) |
| `tests/support/global-setup.ts` | One warm-up login before the suite |

`WEB_BASE_URL` and `API_BASE_URL` come from `.env` (copied from `.env.example` by `scripts/setup.sh`) or the environment. `scripts/test.sh` starts a fresh API with a raised auth rate limit and a production build of the client, then runs `npx playwright test` here; the README at the repository root shows the same against the container stack.
