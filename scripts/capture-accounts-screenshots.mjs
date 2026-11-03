import { chromium } from "playwright";
import fs from "fs";
import path from "path";

const OUT_DIR = path.resolve("docs/design-preview/screenshots");
if (!fs.existsSync(OUT_DIR)) {
  fs.mkdirSync(OUT_DIR, { recursive: true });
}

async function run() {
  const browser = await chromium.launch();
  const context = await browser.newContext({
    viewport: { width: 1280, height: 850 },
    deviceScaleFactor: 1.5,
  });
  const page = await context.newPage();

  console.log("Navigating to http://127.0.0.1:5173/ ...");
  await page.goto("http://127.0.0.1:5173/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();

  // Navigate to Accounts page
  console.log("Navigating to Accounts page...");
  const accNav = page.locator('.nav-item[data-screen="accounts"]');
  if (await accNav.isVisible()) {
    await accNav.click();
  } else {
    await page.locator('#userBtn').click();
  }
  await page.waitForTimeout(300);

  // 1. Evidence: Accounts list with cards
  const file01 = path.join(OUT_DIR, "evidence-accounts-01-list.png");
  await page.screenshot({ path: file01 });
  console.log("Saved:", file01);

  // 2. Evidence: Microsoft Device Code Flow Modal
  console.log("Opening Microsoft Device Code Modal...");
  await page.locator("#addMicrosoftBtn").click();
  await page.waitForTimeout(400);

  const file02 = path.join(OUT_DIR, "evidence-accounts-02-device-code.png");
  await page.screenshot({ path: file02 });
  console.log("Saved:", file02);

  // Close modal via Escape
  await page.keyboard.press("Escape");
  await page.waitForTimeout(300);

  // 3. Evidence: Offline Account Form Modal
  console.log("Opening Offline Account Modal...");
  await page.locator("#addOfflineBtn").click();
  await page.waitForTimeout(300);

  // Type invalid username to show validation error
  await page.locator("#offlineUsernameInput").fill("a");
  await page.locator("#offlineSubmitBtn").click();
  await page.waitForTimeout(200);

  const file03 = path.join(OUT_DIR, "evidence-accounts-03-offline-validation.png");
  await page.screenshot({ path: file03 });
  console.log("Saved:", file03);

  await browser.close();
  console.log("All Accounts screenshots captured successfully.");
}

run().catch((err) => {
  console.error("Error capturing screenshots:", err);
  process.exit(1);
});
