import { chromium } from 'playwright';

const browser = await chromium.launch();
const context = await browser.newContext({
  viewport: { width: 1280, height: 800 },
  colorScheme: 'dark',
});
const page = await context.newPage();

const dir = '/home/fchch/dev/career-agent/docs/screenshots';

// Dashboard
console.log('Capturing dashboard...');
await page.goto('http://localhost:4200/dashboard', { waitUntil: 'networkidle' });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${dir}/dashboard.png` });

// Job Search (has location filter + remote badges + default 3-day filter)
console.log('Capturing job search...');
await page.goto('http://localhost:4200/jobs', { waitUntil: 'networkidle' });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${dir}/job-search.png` });

// Job Detail (has apply links + remote badge)
console.log('Capturing job detail...');
const firstJob = page.locator('app-job-card').first();
if (await firstJob.isVisible()) {
  await firstJob.click();
  await page.waitForTimeout(2000);
  await page.screenshot({ path: `${dir}/job-detail.png` });
} else {
  await page.goto('http://localhost:4200/jobs/1', { waitUntil: 'networkidle' });
  await page.waitForTimeout(2000);
  await page.screenshot({ path: `${dir}/job-detail.png` });
}

// Resume Tailor
console.log('Capturing resume tailor...');
await page.goto('http://localhost:4200/tailor/1', { waitUntil: 'networkidle' });
await page.waitForTimeout(2000);
await page.screenshot({ path: `${dir}/resume-tailor.png` });

await browser.close();
console.log('Done!');
