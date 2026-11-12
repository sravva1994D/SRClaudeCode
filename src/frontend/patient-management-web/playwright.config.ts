import { defineConfig, devices } from '@playwright/test';

// The app and API must already be running (npm start and the API on its development profile). The base URLs can be
// overridden for another host; the defaults are the local development addresses (CLAUDE.md section 12).
const baseURL = process.env['E2E_BASE_URL'] ?? 'http://localhost:4200';
// Optional: run Chromium through an installed browser (for example 'chrome') when the bundled one cannot start.
const chromiumChannel = process.env['E2E_CHROMIUM_CHANNEL'];

export default defineConfig({
  testDir: './e2e',
  // Each journey creates its own synthetic patient, so journeys never depend on each other's data.
  fullyParallel: false,
  workers: 1,
  reporter: 'list',
  use: {
    baseURL,
    trace: 'retain-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'], channel: chromiumChannel } },
    { name: 'webkit', use: { ...devices['Desktop Safari'] } },
  ],
});
