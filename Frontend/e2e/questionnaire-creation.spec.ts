import { test, expect } from '@playwright/test';

test('create a questionnaire with text and boolean questions', async ({ page }) => {
  const title = `Test Questionnaire ${Date.now()}`;

  // 1. Navigate to the questionnaire list page
  await page.goto('/questionnaires');
  await expect(page.getByRole('heading', { name: 'Questionnaires' })).toBeVisible();

  // 2. Click the button to create a new questionnaire
  await page.getByRole('link', { name: 'New Questionnaire' }).click();
  await expect(page.getByRole('heading', { name: 'New Questionnaire' })).toBeVisible();

  // 3. Fill in a title
  await page.getByLabel('Title').fill(title);

  // 4a. Fill in the first question (Text, required) — one question exists by default
  await page.getByLabel('Question text').first().fill('What is your name?');
  // Type defaults to Text, so just mark it as required
  await page.getByLabel('Required').first().check();

  // 4b. Add a second question (Boolean, optional)
  await page.getByRole('button', { name: 'Add Question' }).click();
  const secondQuestionText = page.getByLabel('Question text').nth(1);
  await secondQuestionText.fill('Do you agree?');
  await page.getByLabel('Type').nth(1).selectOption('Boolean');
  // Leave Required unchecked for the second question (optional)

  // 5. Save the questionnaire
  await page.getByRole('button', { name: 'Create Questionnaire' }).click();

  // 6. Verify that we navigate back to the list and the new questionnaire appears
  await expect(page.getByRole('heading', { name: 'Questionnaires' })).toBeVisible();
  await expect(page.getByRole('cell', { name: title, exact: true })).toBeVisible();
});
