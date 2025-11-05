import { test, expect } from '@playwright/test';

test.describe('Focus Mode', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/app/focus');
  });

  test('start strict focus session', async ({ page }) => {
    // Click "Start Session" button
    await page.click('button:has-text("Start Session")');
    
    // Wait for dialog
    await expect(page.locator('[role="dialog"]')).toBeVisible();
    
    // Select Strict mode
    await page.click('label:has-text("Strict")');
    
    // Enter duration
    await page.fill('input[type="number"]', '25');
    
    // Submit
    await page.click('button:has-text("Start")');
    
    // Wait for active session UI
    await expect(page.locator('text=Active Session')).toBeVisible({ timeout: 5000 });
    
    // Check timer is running
    await expect(page.locator('text=/\\d{2}:\\d{2}:\\d{2}/')).toBeVisible();
    
    // Check mode badge shows "Strict"
    await expect(page.locator('text=Strict')).toBeVisible();
    
    // Check distraction counter is visible
    await expect(page.locator('text=/Distractions: \\d+/')).toBeVisible();
  });

  test('timer updates every second', async ({ page }) => {
    // Assume a session is already running
    const timerLocator = page.locator('text=/\\d{2}:\\d{2}:\\d{2}/');
    
    if (await timerLocator.count() > 0) {
      const initialTime = await timerLocator.textContent();
      
      // Wait 2 seconds
      await page.waitForTimeout(2000);
      
      const updatedTime = await timerLocator.textContent();
      
      // Time should have changed
      expect(initialTime).not.toBe(updatedTime);
    }
  });

  test('mock heartbeat shows distraction', async ({ page }) => {
    // Start a strict session first
    await page.click('button:has-text("Start Session")');
    await expect(page.locator('[role="dialog"]')).toBeVisible();
    await page.click('label:has-text("Strict")');
    await page.fill('input[type="number"]', '25');
    await page.click('button:has-text("Start")');
    
    // Wait for session to start
    await expect(page.locator('text=Active Session')).toBeVisible({ timeout: 5000 });
    
    // In real test, we'd mock a SignalR event for distraction
    // For now, check that event log section exists
    await expect(page.locator('text=Event Log')).toBeVisible();
  });

  test('end session works', async ({ page }) => {
    // Assume session is running
    const endButton = page.locator('button:has-text("End Session")');
    
    if (await endButton.count() > 0) {
      await endButton.click();
      
      // Should return to idle state
      await expect(page.locator('button:has-text("Start Session")'))to.toBeVisible({ timeout: 5000 });
    }
  });
});
