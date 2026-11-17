import { chromium } from "playwright";
import fs from "node:fs";
import path from "node:path";

const outDir = path.resolve("docs/design-preview/screenshots");
if (!fs.existsSync(outDir)) {
  fs.mkdirSync(outDir, { recursive: true });
}

async function capture() {
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1280, height: 900 } });

  await page.goto("http://127.0.0.1:5173/");
  await page.waitForSelector(".instance-card");

  // 1. Open instance detail -> tab settings
  await page.locator(".instance-card").first().click();
  await page.waitForSelector("#page-detail:not([hidden])");
  await page.locator('.tab[data-tab="settings"]').click({ force: true });
  await page.waitForSelector("#tab-settings:not([hidden])");
  await page.waitForTimeout(300);

  // Screenshot 1: Overview of Java Resolver + RAM Allocation + GC presets
  await page.screenshot({
    path: path.join(outDir, "evidence-java-01-settings.png"),
    fullPage: false
  });
  console.log("Captured evidence-java-01-settings.png");

  // Screenshot 2: Incompatible Java warning
  await page.selectOption("#javaRuntimeSelect", "java-17");
  await page.waitForSelector("#javaIncompatibleBanner:not([hidden])");
  await page.waitForTimeout(200);
  await page.screenshot({
    path: path.join(outDir, "evidence-java-02-incompatible-warning.png"),
    fullPage: false
  });
  console.log("Captured evidence-java-02-incompatible-warning.png");

  // Fix back to recommended
  await page.click("#btnUseRecommendedJava");
  await page.waitForTimeout(200);

  // Screenshot 3: High RAM warning (> 85% RAM) + ZGC selection
  await page.fill("#setMaxRamValue", "14336");
  await page.dispatchEvent("#setMaxRamValue", "input");
  await page.click('.gc-preset-chip[data-gc="zgc"]');
  await page.waitForSelector("#ramWarningBanner:not([hidden])");
  await page.waitForTimeout(200);
  await page.screenshot({
    path: path.join(outDir, "evidence-java-03-ram-warning.png"),
    fullPage: false
  });
  console.log("Captured evidence-java-03-ram-warning.png");

  // Screenshot 4: Custom Java Path probe test result
  await page.fill("#setJavaCustomPath", "C:\\Program Files\\Java\\jdk-21\\bin\\javaw.exe");
  await page.click("#btnTestJava");
  await page.waitForSelector("#javaProbeResult:not([hidden])");
  await page.waitForTimeout(200);
  await page.screenshot({
    path: path.join(outDir, "evidence-java-04-test-probe.png"),
    fullPage: false
  });
  console.log("Captured evidence-java-04-test-probe.png");

  await browser.close();
}

capture().catch((err) => {
  console.error(err);
  process.exit(1);
});
