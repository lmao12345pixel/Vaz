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

  // 1. Evidence 01: Resource Packs Tab
  console.log("Capturing Evidence 01: Resource Packs Tab...");
  await page.locator(".instance-card").first().click();
  await page.locator('.tab[data-tab="resourcepacks"]').click();
  await page.waitForTimeout(300);

  // Add incompatible pack to show warning badge
  await page.locator("#rpFileInput").setInputFiles({
    name: "retro-textures-mc1.20.zip",
    mimeType: "application/zip",
    buffer: Buffer.from("dummy zip"),
  });
  await page.waitForTimeout(300);

  const file01 = path.join(OUT_DIR, "evidence-01-resourcepacks.png");
  await page.screenshot({ path: file01, fullPage: false });
  console.log("Saved:", file01);

  // 2. Evidence 02: Shaders Tab (Active + Profile)
  console.log("Capturing Evidence 02: Shaders Tab...");
  await page.locator('.tab[data-tab="shaders"]').click();
  await page.waitForTimeout(300);

  // Click BSL Shaders
  const bslCard = page.locator('#shaderCardGrid .shader-card[data-shader-id="bsl-shaders"]');
  if (await bslCard.isVisible()) {
    await bslCard.click();
  }
  // Click Ultra profile
  const ultraChip = page.locator('#shaderProfileChips .profile-chip[data-profile="ultra"]');
  if (await ultraChip.isVisible()) {
    await ultraChip.click();
  }
  await page.waitForTimeout(300);

  const file02 = path.join(OUT_DIR, "evidence-02-shaders-active.png");
  await page.screenshot({ path: file02, fullPage: false });
  console.log("Saved:", file02);

  // 3. Evidence 03: Shaders Loader Warning Banner
  console.log("Capturing Evidence 03: Shaders Warning Banner on Vanilla instance...");
  await page.locator("#btnBackToInstances").click();
  await page.waitForTimeout(300);

  const vanillaCard = page.locator(".instance-card", { hasText: "Vanilla 1.20.1" });
  if (await vanillaCard.isVisible()) {
    await vanillaCard.click();
    await page.locator('.tab[data-tab="shaders"]').click();
    await page.waitForTimeout(300);
  }

  const file03 = path.join(OUT_DIR, "evidence-03-shaders-warning.png");
  await page.screenshot({ path: file03, fullPage: false });
  console.log("Saved:", file03);

  // 4. Evidence 04: Mod Inspector Drawer
  console.log("Capturing Evidence 04: Mod Inspector Drawer...");
  await page.locator("#btnBackToInstances").click();
  await page.locator(".instance-card").first().click();
  await page.locator('.tab[data-tab="mods"]').click();
  await page.waitForTimeout(300);

  // Click first mod row to open Inspector
  const modRow = page.locator(".mod-row").first();
  if (await modRow.isVisible()) {
    await modRow.click();
    await page.waitForTimeout(300);
  }

  const file04 = path.join(OUT_DIR, "evidence-04-mod-inspector.png");
  await page.screenshot({ path: file04, fullPage: false });
  console.log("Saved:", file04);

  // 5. Evidence 05: Mobile Responsive 390px
  console.log("Capturing Evidence 05: Mobile 390px Resource Packs & Shaders...");
  await page.setViewportSize({ width: 390, height: 844 });
  // Close inspector if open
  await page.keyboard.press("Escape");
  await page.locator('.tab[data-tab="resourcepacks"]').click();
  await page.waitForTimeout(300);

  const file05 = path.join(OUT_DIR, "evidence-05-mobile-resourcepacks.png");
  await page.screenshot({ path: file05, fullPage: false });
  console.log("Saved:", file05);

  await browser.close();
  console.log("All screenshots captured successfully.");
}

run().catch((err) => {
  console.error("Error capturing screenshots:", err);
  process.exit(1);
});
