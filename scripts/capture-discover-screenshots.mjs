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

  // 1. Evidence: News Carousel on Home Page
  console.log("Capturing 01: News Carousel on Home page...");
  await page.waitForSelector("#newsCarousel");
  await page.screenshot({
    path: path.join(outDir, "evidence-news-01-carousel.png"),
    fullPage: false,
  });

  // 2. Evidence: News Article Reader Dialog
  console.log("Capturing 02: News Article Reader dialog...");
  await page.locator(".news-slide").first().click({ force: true });
  await page.waitForSelector("#dialogHost:not([hidden]) .news-article-dialog");
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(outDir, "evidence-news-02-reader.png"),
    fullPage: false,
  });

  // Close news dialog
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);

  // 3. Evidence: Servers Page overview with Category chips, MOTD, Ping, Players
  console.log("Capturing 03: Servers list overview...");
  await page.locator('button.nav-item[data-screen="servers"]').click();
  await page.waitForSelector("#page-servers:not([hidden])");
  await page.waitForSelector("#serverList .server-row");
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(outDir, "evidence-servers-01-list.png"),
    fullPage: false,
  });

  // 4. Evidence: Add Server Dialog with Ping test
  console.log("Capturing 04: Add Server dialog with ping test...");
  await page.locator("#addServerBtn").click();
  await page.waitForSelector("#dialogHost:not([hidden]) #serverNameInput");
  await page.locator("#serverNameInput").fill("Pixelmon Vietnam");
  await page.locator("#serverAddressInput").fill("play.pixelmon.vn");
  await page.locator("#serverPingCheckBtn").click();
  await page.waitForSelector("#serverPingResult:has-text('ms')");
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(outDir, "evidence-servers-02-add-dialog.png"),
    fullPage: false,
  });

  // Close dialog
  await page.keyboard.press("Escape");
  await page.waitForTimeout(400);

  // 5. Evidence: Mobile 390px responsive view
  console.log("Capturing 05: Mobile 390px view...");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(outDir, "evidence-servers-03-mobile.png"),
    fullPage: false,
  });

  await browser.close();
  console.log("All screenshots captured successfully in " + outDir);
}

run().catch((err) => {
  console.error("Error capturing screenshots:", err);
  process.exit(1);
});
