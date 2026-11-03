import { test, expect, Page } from "@playwright/test";

async function bootAndOpenScreenshotsTab(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
  await page.locator(".instance-card").first().click({ force: true });
  await expect(page.locator("#page-detail")).toBeVisible();
  const tabBtn = page.locator('.tab[data-tab="screenshots"]');
  await expect(tabBtn).toBeVisible();
  await tabBtn.click();
  await expect(page.locator("#tab-screenshots")).toBeVisible();
}

test.describe("Screenshots Gallery & Lightbox Viewer (screenshots-gallery.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await bootAndOpenScreenshotsTab(page);
  });

  test("SG-AC-01: Grid cards hiển thị đủ thumbnail, title, tên file, ngày chụp, tag thế giới và độ phân giải", async ({ page }) => {
    const cards = page.locator("#screenshotGallery .screenshot-card");
    await expect(cards).toHaveCount(4);

    const firstCard = cards.first();
    await expect(firstCard.locator(".screenshot-thumb-img")).toBeVisible();
    await expect(firstCard.locator(".screenshot-title")).toBeVisible();
    await expect(firstCard.locator(".screenshot-filename")).toContainText(".png");
    await expect(firstCard.locator(".screenshot-world-tag")).toBeVisible();
    await expect(firstCard.locator(".screenshot-res-tag")).toContainText("×");
    await expect(firstCard.locator(".screenshot-date")).toContainText("/");
    await expect(page.locator("#screenshotsSummary")).toContainText("4 ảnh chụp màn hình");
  });

  test("SG-AC-02: Search và sort lọc đúng và cập nhật counter", async ({ page }) => {
    const searchInput = page.locator("#screenshotSearch");
    await expect(searchInput).toBeVisible();

    // 1. Tìm theo từ khóa "Nether"
    await searchInput.fill("Nether");
    let cards = page.locator("#screenshotGallery .screenshot-card");
    await expect(cards).toHaveCount(1);
    await expect(cards.first().locator(".screenshot-title")).toContainText("Nether");
    await expect(page.locator("#screenshotsSummary")).toContainText("1/4 ảnh chụp màn hình");

    // 2. Tìm theo thế giới "Survival"
    await searchInput.fill("Survival");
    cards = page.locator("#screenshotGallery .screenshot-card");
    await expect(cards).toHaveCount(1);
    await expect(cards.first().locator(".screenshot-world-tag")).toContainText("Survival");

    // 3. Xóa tìm kiếm
    await searchInput.fill("");
    await expect(page.locator("#screenshotGallery .screenshot-card")).toHaveCount(4);
    await expect(page.locator("#screenshotsSummary")).toHaveText("4 ảnh chụp màn hình");

    // 4. Sắp xếp: cũ nhất -> mới nhất
    const sortSelect = page.locator("#screenshotSort");
    await sortSelect.selectOption("oldest");
    const oldestFirst = page.locator("#screenshotGallery .screenshot-card").first();
    await expect(oldestFirst.locator(".screenshot-title")).toContainText("Base lúc hoàng hôn");

    await sortSelect.selectOption("newest");
    const newestFirst = page.locator("#screenshotGallery .screenshot-card").first();
    await expect(newestFirst.locator(".screenshot-title")).toContainText("Kho khoáng sản");
  });

  test("SG-AC-03: Nhấp vào ảnh hoặc nút Zoom mở Lightbox toàn màn hình với metadata", async ({ page }) => {
    const firstCard = page.locator("#screenshotGallery .screenshot-card").first();
    const firstTitle = await firstCard.locator(".screenshot-title").textContent();

    await firstCard.locator(".screenshot-zoom-btn").click({ force: true });
    const lightbox = page.locator("#lightboxHost");
    await expect(lightbox).toBeVisible();

    await expect(page.locator("#lightboxImg")).toBeVisible();
    await expect(page.locator("#lightboxTitle")).toHaveText(firstTitle || "");
    await expect(page.locator("#lightboxWorldName")).toBeVisible();
    await expect(page.locator("#lightboxDimensions")).toContainText("×");
    await expect(page.locator("#lightboxCounter")).toContainText("1 / 4");

    // Đóng bằng nút close
    await page.locator("#lightboxCloseBtn").click();
    await expect(lightbox).toBeHidden();
  });

  test("SG-AC-04: Điều hướng trong Lightbox: Next/Prev buttons và ArrowLeft/ArrowRight, Escape đóng", async ({ page }) => {
    // Mở ảnh đầu tiên
    await page.locator("#screenshotGallery .screenshot-card").first().click();
    const lightbox = page.locator("#lightboxHost");
    await expect(lightbox).toBeVisible();
    await expect(page.locator("#lightboxCounter")).toHaveText("1 / 4");

    // Click Next
    await page.locator("#lightboxNextBtn").click();
    await expect(page.locator("#lightboxCounter")).toHaveText("2 / 4");

    // Phím ArrowRight chuyển tiếp sang ảnh 3
    await page.keyboard.press("ArrowRight");
    await expect(page.locator("#lightboxCounter")).toHaveText("3 / 4");

    // Click Prev trở lại ảnh 2
    await page.locator("#lightboxPrevBtn").click();
    await expect(page.locator("#lightboxCounter")).toHaveText("2 / 4");

    // Phím ArrowLeft trở lại ảnh 1
    await page.keyboard.press("ArrowLeft");
    await expect(page.locator("#lightboxCounter")).toHaveText("1 / 4");

    // Phím Escape đóng Lightbox
    await page.keyboard.press("Escape");
    await expect(lightbox).toBeHidden();
  });

  test("SG-AC-05: Actions trong Lightbox: Copy toast, Set Cover instance, Download toast", async ({ page }) => {
    await page.locator("#screenshotGallery .screenshot-card").first().click();
    await expect(page.locator("#lightboxHost")).toBeVisible();

    // 1. Copy filename
    await page.locator("#lightboxCopyBtn").click();
    await expect(page.locator(".toast").last()).toContainText("Đã sao chép tên file");

    // 2. Set as Cover
    await page.locator("#lightboxSetCoverBtn").click();
    await expect(page.locator(".toast").last()).toContainText("Đã đặt làm ảnh bìa instance");

    // 3. Download
    await page.locator("#lightboxDownloadBtn").click();
    await expect(page.locator(".toast").last()).toContainText("Đã lưu ảnh về máy");

    await page.locator("#lightboxCloseBtn").click();
  });

  test("SG-AC-06: Xóa ảnh chụp màn hình yêu cầu dialog xác nhận, loại bỏ ảnh khỏi gallery", async ({ page }) => {
    const cards = page.locator("#screenshotGallery .screenshot-card");
    await expect(cards).toHaveCount(4);

    // Mở ảnh qua lightbox và click Xóa
    await cards.first().click();
    await expect(page.locator("#lightboxHost")).toBeVisible();
    await page.locator("#lightboxDeleteBtn").click();

    // Dialog xác nhận xuất hiện
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost).toContainText("Xóa ảnh chụp màn hình?");

    // Hủy xóa -> ảnh vẫn còn 4
    await dialogHost.locator("button", { hasText: "Hủy" }).click();
    await expect(dialogHost).toBeHidden();
    await expect(page.locator("#lightboxHost")).toBeVisible();
    await page.locator("#lightboxCloseBtn").click();
    await expect(cards).toHaveCount(4);

    // Thao tác xóa từ thẻ ảnh qua nút quick delete
    const deleteBtn = cards.first().locator(".screenshot-delete-btn");
    await deleteBtn.click({ force: true });
    await expect(dialogHost).toBeVisible();
    await dialogHost.locator("button[data-dialog-primary]").click();

    // Assert số lượng giảm còn 3
    await expect(page.locator("#screenshotGallery .screenshot-card")).toHaveCount(3);
    await expect(page.locator("#screenshotsSummary")).toContainText("3 ảnh chụp màn hình");
    await expect(page.locator(".toast").last()).toContainText("Đã xóa ảnh chụp màn hình");
  });

  test("SG-AC-07: Thêm ảnh chụp màn hình mới: Chụp demo (F2) và Import file ảnh", async ({ page }) => {
    const cards = page.locator("#screenshotGallery .screenshot-card");
    await expect(cards).toHaveCount(4);

    // 1. Chụp demo F2
    const takeDemoBtn = page.locator("#takeDemoScreenshotBtn");
    await expect(takeDemoBtn).toBeVisible();
    await takeDemoBtn.click();

    await expect(cards).toHaveCount(5);
    await expect(page.locator("#screenshotsSummary")).toContainText("5 ảnh chụp màn hình");
    await expect(page.locator(".toast").last()).toContainText("Đã chụp ảnh màn hình mới (F2)");

    // 2. Import file ảnh
    const fileInput = page.locator("#screenshotFileInput");
    await fileInput.setInputFiles({
      name: "imported-screenshot-test.png",
      mimeType: "image/png",
      buffer: Buffer.from(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
        "base64"
      ),
    });

    await expect(cards).toHaveCount(6);
    await expect(page.locator("#screenshotsSummary")).toContainText("6 ảnh chụp màn hình");
    await expect(page.locator(".toast").last()).toContainText("Đã thêm ảnh");
  });

  test("SG-AC-08: Nút 'Mở thư mục screenshots' hiển thị toast thông báo", async ({ page }) => {
    const openFolderBtn = page.locator("#openScreenshotsFolderBtn");
    await expect(openFolderBtn).toBeVisible();
    await openFolderBtn.click();
    await expect(page.locator(".toast").last()).toContainText("Đã mở thư mục screenshots");
  });

  test("SG-AC-09: Responsive 390px/620px không tràn ngang, focus ring >= 2px, Escape, reduced-motion <= 0.05s", async ({ page }) => {
    // 1. Kiểm tra responsive 620px và 390px
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 720 });
      const noOverflow = await page.evaluate(() => {
        const tab = document.getElementById("tab-screenshots");
        if (!tab) return false;
        return tab.scrollWidth <= tab.clientWidth + 1;
      });
      expect(noOverflow, `Screenshots tab tràn ngang ở ${width}px`).toBe(true);
    }

    // 2. Phím Escape đóng Lightbox
    await page.setViewportSize({ width: 1200, height: 720 });
    await page.locator("#screenshotGallery .screenshot-card").first().click();
    const lightbox = page.locator("#lightboxHost");
    await expect(lightbox).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(lightbox).toBeHidden();

    // 3. :focus-visible rõ ràng (outline >= 2px)
    const takeDemoBtn = page.locator("#takeDemoScreenshotBtn");
    await takeDemoBtn.focus();
    const focusStyles = await page.evaluate(() => {
      const active = document.getElementById("takeDemoScreenshotBtn");
      if (!active) return null;
      const cs = window.getComputedStyle(active);
      return {
        outlineStyle: cs.outlineStyle,
        outlineWidth: parseFloat(cs.outlineWidth) || 0,
      };
    });
    expect(focusStyles?.outlineStyle).not.toBe("none");
    expect(focusStyles?.outlineWidth).toBeGreaterThanOrEqual(2);

    // 4. Reduced Motion <= 0.05s
    await page.emulateMedia({ reducedMotion: "reduce" });
    const cardTransition = await page.locator("#screenshotGallery .screenshot-card").first().evaluate((el) => {
      const cs = window.getComputedStyle(el);
      const durations = cs.transitionDuration.split(",").map((s) => parseFloat(s) || 0);
      return Math.max(...durations);
    });
    expect(cardTransition).toBeLessThanOrEqual(0.05);
  });

  test("SG-AC-11: Không hồi quy CSS: Không tồn tại transition: all hoặc transition-property: all", async ({ page }) => {
    const hasTransitionAll = await page.evaluate(() => {
      let found = false;
      for (const sheet of Array.from(document.styleSheets)) {
        try {
          for (const rule of Array.from(sheet.cssRules || [])) {
            const cssText = rule.cssText || "";
            if (/transition\s*:\s*all\b/i.test(cssText) || /transition-property\s*:\s*all\b/i.test(cssText)) {
              found = true;
              break;
            }
          }
        } catch (_) {
          // ignore CORS
        }
      }
      return found;
    });
    expect(hasTransitionAll).toBe(false);
  });
});
