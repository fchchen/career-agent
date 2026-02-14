import fs from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const baseUrl = process.env.UI_BASE_URL ?? 'https://mango-water-0a9e94f0f.4.azurestaticapps.net';
const outputDir = path.resolve('docs/videos');
const outputFile = path.join(outputDir, 'ui-navigation.webm');

const waitMs = 1500;

async function safeClick(page, selector) {
  const locator = page.locator(selector).first();
  if (await locator.isVisible().catch(() => false)) {
    await locator.click({ timeout: 5000 });
    await page.waitForTimeout(waitMs);
    return true;
  }
  return false;
}

await fs.mkdir(outputDir, { recursive: true });

const browser = await chromium.launch({
  headless: true,
});

const context = await browser.newContext({
  viewport: { width: 1366, height: 768 },
  recordVideo: {
    dir: outputDir,
    size: { width: 1366, height: 768 },
  },
});

const page = await context.newPage();
const video = page.video();
let recordedVideoPath = '';

try {
  await page.goto(baseUrl, { waitUntil: 'networkidle', timeout: 60000 });
  await page.waitForTimeout(2200);

  await safeClick(page, 'a[routerlink="/jobs"]');
  await safeClick(page, 'a:has-text("Jobs")');
  await page.waitForTimeout(1200);

  const firstCard = page.locator('app-job-card').first();
  if (await firstCard.isVisible().catch(() => false)) {
    await firstCard.click({ timeout: 5000 });
    await page.waitForTimeout(waitMs);
    await page.goBack({ waitUntil: 'networkidle' });
    await page.waitForTimeout(waitMs);
  }

  await safeClick(page, 'a[routerlink="/resume"]');
  await safeClick(page, 'a:has-text("Resume")');
  await page.waitForTimeout(waitMs);

  await safeClick(page, 'a[routerlink="/settings"]');
  await safeClick(page, 'a:has-text("Settings")');
  await page.waitForTimeout(2500);
} finally {
  await context.close();
  if (video) {
    recordedVideoPath = await video.path();
  }
  await browser.close();
}

if (!video || !recordedVideoPath) {
  throw new Error('No video was recorded.');
}

await fs.copyFile(recordedVideoPath, outputFile);
console.log(`Saved video: ${outputFile}`);
