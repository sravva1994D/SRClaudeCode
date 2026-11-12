import { expect, Page, test } from '@playwright/test';

// Consultation journey (CLAUDE.md section 9.3): sign in, record vitals, save notes, complete the visit, then correct the
// vitals and notes on the completed visit. Data is synthetic and created through the API for this run only.
const API_URL = process.env['E2E_API_URL'] ?? 'http://localhost:5118/api/v1';
const USERNAME = 'dev.doctor';
const PASSWORD = process.env['E2E_DOCTOR_PASSWORD'];
const GENDER_FEMALE = 2;

test.beforeAll(() => {
  if (!PASSWORD) {
    throw new Error(
      'Set E2E_DOCTOR_PASSWORD to the development doctor password before running the E2E tests.',
    );
  }
});

test('a consultation runs from vitals to completion and can be corrected afterwards', async ({
  page,
  request,
}) => {
  const visitId = await startVisitThroughApi(request, `E2E Patient ${Date.now()}`);

  await signIn(page);

  // Vitals: record, then correct the saved values.
  await openInApp(page, `/consultation/visits/${visitId}/vitals`);
  await page.getByLabel('Pulse (bpm)').fill('72');
  await page.getByLabel('Temperature (°C)').fill('36.6');
  await page.getByRole('button', { name: 'Save vitals' }).click();
  await expect(page.getByRole('heading', { name: 'Recorded vitals' })).toBeVisible();

  // Notes: save a complaint as a draft, then complete the visit.
  await openInApp(page, `/consultation/visits/${visitId}/notes`);
  await page.getByRole('button', { name: 'Add complaint' }).click();
  await page.getByLabel('Complaint', { exact: true }).first().fill('Cough');
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByText('Draft saved.')).toBeVisible();

  await page.getByRole('button', { name: 'Complete visit' }).click();
  await expect(page.getByText('Visit completed.')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Complete visit' })).toHaveCount(0);

  // The completed visit still shows its notes, and they can be changed and saved.
  await expect(page.getByLabel('Complaint', { exact: true }).first()).toHaveValue('Cough');
  await page.getByRole('button', { name: 'Add complaint' }).click();
  await page.getByLabel('Complaint', { exact: true }).nth(1).fill('Fever');
  await page.getByRole('button', { name: 'Save draft' }).click();
  await expect(page.getByText('Draft saved.')).toBeVisible();
  await expect(page.getByText(/Last modified:/)).toBeVisible();

  // Vitals on the completed visit: correct the pulse.
  await openInApp(page, `/consultation/visits/${visitId}/vitals`);
  await page.getByRole('button', { name: 'Edit vitals' }).click();
  await page.getByLabel('Pulse (bpm)').fill('80');
  await page.getByRole('button', { name: 'Save changes' }).click();
  await expect(page.getByRole('heading', { name: 'Recorded vitals' })).toBeVisible();
  await expect(page.locator('.recorded')).toContainText('80');
});

/**
 * Navigates inside the app without a page load. A full load drops the in-memory access token (CLAUDE.md 8.4), and
 * WebKit does not send the Secure refresh cookie over plain http://localhost, so the session would be lost.
 */
async function openInApp(page: Page, path: string): Promise<void> {
  await page.evaluate((target) => {
    window.history.pushState({}, '', target);
    window.dispatchEvent(new PopStateEvent('popstate', { state: {} }));
  }, path);
}

async function signIn(page: Page): Promise<void> {
  await page.goto('/login');
  await page.getByLabel('Username').fill(USERNAME);
  await page.getByLabel('Password').fill(PASSWORD ?? '');
  await page.getByRole('button', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/dashboard/);
}

/** Creates a synthetic patient, books a future appointment and starts its visit, all through the API. */
async function startVisitThroughApi(
  request: import('@playwright/test').APIRequestContext,
  fullName: string,
): Promise<number> {
  const login = await request.post(`${API_URL}/auth/login`, {
    data: { username: USERNAME, password: PASSWORD },
  });
  expect(login.ok()).toBeTruthy();
  const { accessToken } = (await login.json()) as { accessToken: string };
  const headers = { Authorization: `Bearer ${accessToken}` };

  const patient = await request.post(`${API_URL}/patients`, {
    headers,
    data: {
      fullName,
      dateOfBirth: '1990-05-14',
      genderId: GENDER_FEMALE,
      phoneNumber: '9000000001',
      address: null,
    },
  });
  expect(patient.ok()).toBeTruthy();
  const { patientId } = (await patient.json()) as { patientId: number };

  // A slot unique to this run, so a repeat run never collides with an earlier booking (the API refuses double bookings).
  const slot = new Date(Date.now() + (10 + (Date.now() % 380)) * 24 * 60 * 60 * 1000);
  slot.setUTCHours(
    9 + (Math.floor(Date.now() / 1000) % 8),
    (Math.floor(Date.now() / 60000) % 4) * 15,
    0,
    0,
  );
  const appointment = await request.post(`${API_URL}/appointments`, {
    headers,
    data: { patientId, slotStart: slot.toISOString() },
  });
  expect(appointment.ok(), await appointment.text()).toBeTruthy();
  const { appointmentId } = (await appointment.json()) as { appointmentId: number };

  const started = await request.post(`${API_URL}/appointments/${appointmentId}/start-visit`, {
    headers,
  });
  expect(started.status()).toBe(201);
  const { visitId } = (await started.json()) as { visitId: number };
  return visitId;
}
