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

  // Switch to Modpacks page via nav
  console.log("Navigating to #page-modpacks...");
  const navBtn = page.locator('.nav-item[data-screen="modpacks"]');
  await navBtn.click();
  await page.waitForSelector("#page-modpacks:not([hidden])");
  await page.waitForTimeout(400);

  // 1. Evidence 01: Modpacks List with Category chips, Cards, Filter controls
  console.log("Capturing 01: Modpacks List...");
  await page.screenshot({
    path: path.join(outDir, "evidence-modpacks-01-list.png"),
    fullPage: false,
  });

  // 2. Evidence 02: Modpack Inspector Drawer
  console.log("Capturing 02: Modpack Inspector...");
  const firstCard = page.locator("#modpacksGrid .discover-card").first();
  const detailBtn = firstCard.locator(".modpack-details-btn");
  await detailBtn.click();
  await page.waitForSelector("#modpackInspectorHost.is-open");
  await page.waitForTimeout(350);
  await page.screenshot({
    path: path.join(outDir, "evidence-modpacks-02-inspector.png"),
    fullPage: false,
  });

  // Close Inspector
  await page.locator("#modpackInspectorClose").click();
  await page.waitForTimeout(300);

  // 3. Evidence 03: 1-Click Install Modal with 3-phase progress
  console.log("Capturing 03: Install Modal...");
  const secondCard = page.locator("#modpacksGrid .discover-card").nth(1);
  const installBtn = secondCard.locator(".modpack-install-btn");
  await installBtn.click();
  await page.waitForSelector("#modpackInstallModalHost.is-open");
  await page.waitForTimeout(500); // let phase animate
  await page.screenshot({
    path: path.join(outDir, "evidence-modpacks-03-install-modal.png"),
    fullPage: false,
  });

  // Wait for install to finish
  await page.waitForTimeout(2500);

  // 4. Evidence 04: Mobile 390px Viewport
  console.log("Capturing 04: Mobile Responsive...");
  const mobileContext = await browser.newContext({
    viewport: { width: 390, height: 844 },
    deviceScaleFactor: 2,
    isMobile: true,
  });
  const mobilePage = await mobileContext.newPage();
  await mobilePage.goto("http://127.0.0.1:5173/");
  await mobilePage.locator('.nav-item[data-screen="modpacks"]').click();
  await mobilePage.waitForSelector("#page-modpacks:not([hidden])");
  await mobilePage.waitForTimeout(400);

  await mobilePage.screenshot({
    path: path.join(outDir, "evidence-modpacks-04-mobile.png"),
    fullPage: false,
  });

  await browser.close();
  console.log("Captured 4 screenshots successfully into:", outDir);
}

run().catch((err) => {
  console.error("Error capturing screenshots:", err);
  process.exit(1);
});
