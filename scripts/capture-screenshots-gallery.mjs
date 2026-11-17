import { chromium } from "playwright";
import fs from "node:fs";
import path from "node:path";
import http from "node:http";

const outDir = "docs/design-preview/screenshots";
if (!fs.existsSync(outDir)) {
  fs.mkdirSync(outDir, { recursive: true });
}

// Simple server for design preview
function startServer(port = 5173) {
  return new Promise((resolve, reject) => {
    const previewDir = path.resolve("docs/design-preview");
    const mime = {
      ".html": "text/html; charset=utf-8",
      ".css": "text/css; charset=utf-8",
      ".js": "text/javascript; charset=utf-8",
      ".json": "application/json; charset=utf-8",
      ".svg": "image/svg+xml",
      ".png": "image/png",
      ".jpg": "image/jpeg",
      ".webp": "image/webp",
    };
    const srv = http.createServer((req, res) => {
      let reqPath = decodeURIComponent(new URL(req.url, `http://localhost:${port}`).pathname);
      if (reqPath === "/") reqPath = "/index.html";
      const filePath = path.join(previewDir, reqPath);
      if (!fs.existsSync(filePath)) {
        res.writeHead(404);
        res.end("Not Found");
        return;
      }
      const ext = path.extname(filePath);
      res.writeHead(200, { "Content-Type": mime[ext] || "application/octet-stream" });
      fs.createReadStream(filePath).pipe(res);
    });

    srv.on("error", (err) => {
      if (err.code === "EADDRINUSE") {
        resolve({ close: () => {} }); // already running
      } else {
        reject(err);
      }
    });

    srv.listen(port, "127.0.0.1", () => {
      resolve(srv);
    });
  });
}

async function run() {
  const srv = await startServer(5173);
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

  // Go to Screenshots tab
  const tabBtn = page.locator('.tab[data-tab="screenshots"]');
  await tabBtn.waitFor({ state: "visible" });
  await tabBtn.click();
  await page.waitForTimeout(400);
  await page.waitForSelector("#screenshotGallery .screenshot-card");

  // 1. Evidence 01: Gallery Overview
  console.log("Capturing 01: Screenshots gallery overview...");
  await page.screenshot({
    path: path.join(outDir, "evidence-ss-01-gallery.png"),
    fullPage: false,
  });

  // 2. Evidence 02: Lightbox view
  console.log("Capturing 02: Lightbox modal...");
  await page.locator("#screenshotGallery .screenshot-card").first().click();
  await page.waitForSelector("#lightboxHost:not([hidden])");
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(outDir, "evidence-ss-02-lightbox.png"),
    fullPage: false,
  });

  // Close lightbox
  await page.keyboard.press("Escape");
  await page.waitForTimeout(300);

  // 3. Evidence 03: Search & Sort
  console.log("Capturing 03: Search & Sort...");
  await page.locator("#screenshotSearch").fill("Nether");
  await page.waitForTimeout(300);
  await page.screenshot({
    path: path.join(outDir, "evidence-ss-03-search-sort.png"),
    fullPage: false,
  });
  await page.locator("#screenshotSearch").fill("");
  await page.waitForTimeout(300);

  // 4. Evidence 04: Mobile 390px
  console.log("Capturing 04: Mobile 390px...");
  await page.setViewportSize({ width: 390, height: 844 });
  await page.waitForTimeout(400);
  await page.screenshot({
    path: path.join(outDir, "evidence-ss-04-mobile.png"),
    fullPage: false,
  });

  await browser.close();
  srv.close();
  console.log("Screenshots captured successfully!");
}

run().catch((err) => {
  console.error("Screenshot capture failed:", err);
  process.exit(1);
});
