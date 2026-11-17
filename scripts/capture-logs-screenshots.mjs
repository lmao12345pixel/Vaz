import { chromium } from "playwright";
import fs from "node:fs";
import path from "node:path";

const outDir = "docs/design-preview/screenshots";
if (!fs.existsSync(outDir)) {
  fs.mkdirSync(outDir, { recursive: true });
}

async function run() {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1280, height: 860 },
    deviceScaleFactor: 1.5,
  });
  const page = await context.newPage();

  console.log("Navigating to http://127.0.0.1:5173/ ...");
  await page.goto("http://127.0.0.1:5173/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();

  await page.waitForSelector(".instance-card");
  await page.locator(".instance-card").first().click({ force: true });
  await page.waitForSelector("#page-detail");

  // Go to Logs tab
  const tabBtn = page.locator('.tab[data-tab="logs"]');
  await tabBtn.waitFor({ state: "visible" });
  await tabBtn.click();
  await page.waitForTimeout(400);

  // Generate sample logs
  await page.locator("#generateLogBtn").click();
  await page.waitForSelector("#logView .log-line");
  await page.waitForTimeout(300);

  // 1. Evidence 01: Viewer with syntax highlighting & line numbers
  console.log("Capturing 01: Viewer with syntax highlighting & line numbers...");
  await page.screenshot({
    path: path.join(outDir, "evidence-logs-01-viewer.png"),
    fullPage: false,
  });

  // 2. Evidence 02: Filter ERROR & Flash animation
  console.log("Capturing 02: Filter ERROR & Flash...");
  await page.locator('#logLevels button[data-log-level="ERROR"]').click();
  await page.locator("#jumpFirstErrorBtn").click();
  await page.waitForTimeout(150);
  await page.screenshot({
    path: path.join(outDir, "evidence-logs-02-filter-error.png"),
    fullPage: false,
  });

  // Reset filter to ALL
  await page.locator('#logLevels button[data-log-level="ALL"]').click();
  await page.waitForTimeout(200);

  // 3. Evidence 03: Search "Sodium"
  console.log("Capturing 03: Search Sodium...");
  await page.locator("#logSearch").fill("Sodium");
  await page.waitForTimeout(200);
  await page.screenshot({
    path: path.join(outDir, "evidence-logs-03-search.png"),
    fullPage: false,
  });

  // Reset search
  await page.locator("#logSearch").fill("");
  await page.waitForTimeout(200);

  // 4. Evidence 04: Mobile viewport 390px
  console.log("Capturing 04: Mobile 390px viewport...");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(outDir, "evidence-logs-04-mobile.png"),
    fullPage: false,
  });

  await browser.close();
  console.log("All 4 logs screenshots captured successfully in:", outDir);
}

run().catch((err) => {
  console.error(err);
  process.exit(1);
});
