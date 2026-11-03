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

  // Go to Worlds tab
  const tabBtn = page.locator('.tab[data-tab="worlds"]');
  await tabBtn.waitFor({ state: "visible" });
  await tabBtn.click();
  await page.waitForTimeout(500);
  await page.waitForSelector("#worldList .world-card");

  // 1. Evidence 01: Worlds List overview
  console.log("Capturing 01: Worlds list overview...");
  await page.screenshot({
    path: path.join(outDir, "evidence-worlds-01-list.png"),
    fullPage: false,
  });

  // 2. Evidence 02: Filter chips & search
  console.log("Capturing 02: Filter & search...");
  await page.locator('#worldFilterChips button[data-world-filter="hardcore"]').click();
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(outDir, "evidence-worlds-02-filter.png"),
    fullPage: false,
  });

  // Reset filter to all
  await page.locator('#worldFilterChips button[data-world-filter="all"]').click();
  await page.waitForTimeout(300);

  // 3. Evidence 03: Restore backup dialog
  console.log("Capturing 03: Restore dialog...");
  const restoreBtn = page.locator('.world-card [data-world-restore]:not([disabled])').first();
  await restoreBtn.click();
  await page.waitForSelector("#dialogHost:not([hidden]) .backup-list");
  await page.screenshot({
    path: path.join(outDir, "evidence-worlds-03-restore.png"),
    fullPage: false,
  });
  // Close dialog via Escape
  await page.keyboard.press("Escape");
  await page.waitForTimeout(300);

  // 4. Evidence 04: Level.dat Inspector
  console.log("Capturing 04: Level.dat Inspector...");
  const inspectBtn = page.locator('.world-card [data-world-inspect]').first();
  await inspectBtn.click();
  await page.waitForSelector("#dialogHost:not([hidden]) .level-dat-card");
  await page.screenshot({
    path: path.join(outDir, "evidence-worlds-04-leveldat-inspector.png"),
    fullPage: false,
  });
  await page.keyboard.press("Escape");
  await page.waitForTimeout(300);

  // 5. Evidence 05: Mobile viewport 390px
  console.log("Capturing 05: Mobile 390px...");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(outDir, "evidence-worlds-05-mobile.png"),
    fullPage: false,
  });

  await browser.close();
  console.log("All screenshots captured successfully!");
}

run().catch((err) => {
  console.error("Screenshot capture failed:", err);
  process.exit(1);
});
