import { test, expect } from '@playwright/test'

/** Smoke testy responsywności. Uruchamiane na profilu mobilnym (Pixel 5)
 *  i desktopowym (zob. playwright.config.ts) — bez backendu, na publicznym
 *  ekranie logowania, do którego przekierowuje ProtectedRoute. */
test.describe('Responsywność', () => {
  test('ekran logowania renderuje się bez poziomego przewijania', async ({ page }) => {
    await page.goto('/')

    // Formularz logowania jest widoczny niezależnie od szerokości.
    await expect(page.getByRole('button', { name: 'Zaloguj się' })).toBeVisible()

    // Brak poziomego scrolla (tolerancja 1px na zaokrąglenia subpikselowe).
    const overflow = await page.evaluate(
      () => document.documentElement.scrollWidth - document.documentElement.clientWidth
    )
    expect(overflow).toBeLessThanOrEqual(1)
  })

  test('manifest PWA jest podpięty', async ({ page }) => {
    await page.goto('/')
    await expect(page.locator('link[rel="manifest"]')).toHaveAttribute(
      'href',
      '/manifest.webmanifest'
    )
  })
})
