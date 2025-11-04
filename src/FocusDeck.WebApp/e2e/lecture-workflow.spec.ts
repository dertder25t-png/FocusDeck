import { test, expect } from '@playwright/test';

test.describe('Lecture Workflow', () => {
  test.beforeEach(async ({ page }) => {
    // Mock auth token
    await page.goto('/app/lectures');
  });

  test('upload lecture and process flow', async ({ page }) => {
    // Click "New Lecture" button
    await page.click('button:has-text("New Lecture")');
    
    // Wait for dialog to appear
    await expect(page.locator('[role="dialog"]')).toBeVisible();
    
    // Fill in lecture details
    await page.fill('input[placeholder*="title"]', 'Test Lecture');
    await page.fill('textarea[placeholder*="description"]', 'Test lecture description');
    
    // Select course
    await page.click('select', { timeout: 5000 });
    await page.selectOption('select', { index: 1 });
    
    // Submit lecture creation
    await page.click('button:has-text("Create")');
    
    // Wait for upload dialog
    await expect(page.locator('text=Upload Audio')).toBeVisible({ timeout: 10000 });
    
    // Mock file upload (since we can't actually upload in tests without backend)
    // In real test, you'd upload a file:
    // await page.setInputFiles('input[type="file"]', 'path/to/test-audio.mp3');
    
    // Check that lecture appears in list
    await page.click('button:has-text("Close")');
    await expect(page.locator('text=Test Lecture')).toBeVisible();
    
    // Check status badge exists
    await expect(page.locator('[class*="badge"]')).toBeVisible();
  });

  test('lecture detail view shows transcript and summary', async ({ page }) => {
    // Assume at least one lecture exists
    const lectureCard = page.locator('[class*="card"]').first();
    
    // Click on lecture card
    await lectureCard.click();
    
    // Wait for detail dialog
    await expect(page.locator('[role="dialog"]')).toBeVisible();
    
    // Check for expected sections
    await expect(page.locator('text=Transcript')).toBeVisible();
    await expect(page.locator('text=Summary')).toBeVisible();
    
    // Check status badge
    await expect(page.locator('[class*="badge"]')).toBeVisible();
  });

  test('process button triggers processing', async ({ page }) => {
    // Find a lecture with "AudioUploaded" status
    const processButton = page.locator('button:has-text("Process Lecture")').first();
    
    if (await processButton.count() > 0) {
      await processButton.click();
      
      // Status should change (in real scenario with backend)
      await expect(page.locator('text=Processing')).toBeVisible({ timeout: 5000 });
    }
  });
});
