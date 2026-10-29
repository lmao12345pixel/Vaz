import { test, expect, type Page } from "@playwright/test";

async function boot(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
}

async function navigateToServers(page: Page) {
  const toggle = page.locator("#mobileNavToggle");
  if (await toggle.isVisible()) {
    const isNavOpen = await page.evaluate(() => document.documentElement.classList.contains("nav-open"));
    if (!isNavOpen) {
      await toggle.click({ force: true });
    }
  }
  const navBtn = page.locator('.nav-item[data-screen="servers"]');
  await navBtn.click({ force: true });
  await expect(page.locator("#page-servers")).toBeVisible();
}

test.describe("Discover: Server Browser & News Feed Runtime Tests (discover-servers-news.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await boot(page);
  });

  test("DSN-AC-01: Danh sách Server Cards hiển thị đầy đủ icon, tên, địa chỉ IP (kèm nút copy), version & loader badge, MOTD Minecraft đa màu, số người chơi online/max và cột sóng ping trực tiếp (ms)", async ({ page }) => {
    test.skip(true, "Mục Server đã được gỡ bỏ theo yêu cầu người dùng");
    await navigateToServers(page);

    const cards = page.locator("#serverList .server-row");
    await expect(cards).toHaveCount(4);

    const firstCard = cards.first();
    await expect(firstCard.locator(".server-icon")).toBeVisible();
    await expect(firstCard.locator(".server-name")).toHaveText("Aurora SMP");
    await expect(firstCard.locator(".server-ip")).toHaveText("play.aurora-smp.net");
    await expect(firstCard.locator(".server-inline-copy-btn")).toBeVisible();
    await expect(firstCard.locator(".server-version-badge")).toContainText("1.21.1 · Vanilla");
    await expect(firstCard.locator(".server-motd-preview")).toBeVisible();
    await expect(firstCard.locator(".server-motd-preview")).toContainText("Aurora SMP");
    await expect(firstCard.locator(".server-players-badge")).toContainText("128/500 người chơi");

    const pingIndicator = firstCard.locator(".server-ping-wrap");
    await expect(pingIndicator).toBeVisible();
    await expect(pingIndicator.locator(".mc-ping-bars")).toBeVisible();
    await expect(pingIndicator).toContainText("42 ms");
  });

  test("DSN-AC-02: Bộ lọc Category chips và dropdown lọc lọc chính xác danh sách server và cập nhật bộ đếm", async ({ page }) => {
    test.skip(true, "Mục Server đã được gỡ bỏ theo yêu cầu người dùng");
    await navigateToServers(page);

    const counter = page.locator("#serversCount");
    await expect(counter).toHaveText("4");

    // Lọc Survival chip
    const survivalChip = page.locator('#serverCategoryChips button[data-server-category="survival"]');
    await survivalChip.click();
    await expect(survivalChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("1");
    await expect(page.locator("#serverList .server-row")).toHaveCount(1);
    await expect(page.locator("#serverList .server-row .server-name")).toHaveText("Aurora SMP");

    // Lọc Creative chip
    const creativeChip = page.locator('#serverCategoryChips button[data-server-category="creative"]');
    await creativeChip.click();
    await expect(creativeChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("1");
    await expect(page.locator("#serverList .server-row .server-name")).toHaveText("Creative Hub");

    // Lọc Minigames chip
    const minigamesChip = page.locator('#serverCategoryChips button[data-server-category="minigames"]');
    await minigamesChip.click();
    await expect(minigamesChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("1");
    await expect(page.locator("#serverList .server-row .server-name")).toHaveText("Hypixel Asia Proxy");

    // Lọc Modded chip
    const moddedChip = page.locator('#serverCategoryChips button[data-server-category="modded"]');
    await moddedChip.click();
    await expect(moddedChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("1");
    await expect(page.locator("#serverList .server-row .server-name")).toHaveText("Modded Survival");

    // Trở về Tất cả chip
    const allChip = page.locator('#serverCategoryChips button[data-server-category="all"]');
    await allChip.click();
    await expect(allChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("4");

    // Dropdown lọc: Yêu thích (favorite)
    const filterSelect = page.locator("#serverFilter");
    await filterSelect.selectOption("favorite");
    await expect(counter).toHaveText("1");
    await expect(page.locator("#serverList .server-row .server-fav-mark")).toBeVisible();

    // Dropdown lọc: Online
    await filterSelect.selectOption("online");
    await expect(counter).toHaveText("3");

    // Reset dropdown: Tất cả
    await filterSelect.selectOption("all");
    await expect(counter).toHaveText("4");
  });

  test("DSN-AC-03: Tìm kiếm server theo tên, địa chỉ hoặc phiên bản (#serverSearch), hiển thị empty state phù hợp khi không có kết quả", async ({ page }) => {
    test.skip(true, "Mục Server đã được gỡ bỏ theo yêu cầu người dùng");
    await navigateToServers(page);

    const searchInput = page.locator("#serverSearch");
    const emptyState = page.locator("#serversEmpty");

    // Tìm kiếm theo tên
    await searchInput.fill("Aurora");
    await expect(page.locator("#serverList .server-row")).toHaveCount(1);
    await expect(emptyState).toBeHidden();

    // Tìm kiếm theo IP/địa chỉ
    await searchInput.fill("hypixel.net");
    await expect(page.locator("#serverList .server-row")).toHaveCount(1);
    await expect(page.locator("#serverList .server-row .server-name")).toHaveText("Hypixel Asia Proxy");
    await expect(emptyState).toBeHidden();

    // Tìm kiếm theo phiên bản
    await searchInput.fill("1.20.1");
    await expect(page.locator("#serverList .server-row")).toHaveCount(2);
    await expect(emptyState).toBeHidden();

    // Tìm kiếm không tồn tại
    await searchInput.fill("NonExistentKeyword9999");
    await expect(page.locator("#serverList .server-row")).toHaveCount(0);
    await expect(emptyState).toBeVisible();

    // Xóa tìm kiếm
    await searchInput.fill("");
    await expect(page.locator("#serverList .server-row")).toHaveCount(4);
    await expect(emptyState).toBeHidden();
  });

  test("DSN-AC-04: Thêm server mới (#addServerBtn) qua Dialog với tên, địa chỉ, loader; tự động kiểm tra ping và thêm vào danh sách", async ({ page }) => {
    test.skip(true, "Mục Server đã được gỡ bỏ theo yêu cầu người dùng");
    await navigateToServers(page);

    await page.locator("#addServerBtn").click();
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();

    await page.locator("#serverNameInput").fill("Aethelgard SMP");
    await page.locator("#serverAddressInput").fill("mc.aethelgard.net");
    await page.locator("#serverCategoryInput").selectOption("survival");

    // Kiểm tra ping ngay trong dialog
    const pingBtn = page.locator("#serverPingCheckBtn");
    await pingBtn.click();
    const pingResult = page.locator("#serverPingResult");
    await expect(pingResult).toContainText("ms", { timeout: 4000 });

    // Xác nhận thêm
    await dialogHost.locator("[data-dialog-primary]").click();
    await expect(dialogHost).toBeHidden();

    // Kiểm tra toast và danh sách
    await expect(page.locator(".toast")).toContainText("Aethelgard SMP");
    await expect(page.locator("#serversCount")).toHaveText("5");
    await expect(page.locator("#serverList .server-row", { hasText: "Aethelgard SMP" })).toBeVisible();
  });

  test("DSN-AC-05: Chỉnh sửa server (Edit) và xóa server (Delete) yêu cầu dialog xác nhận; toggle yêu thích (Favorite) đánh dấu sao và lưu state", async ({ page }) => {
    test.skip(true, "Mục Server đã được gỡ bỏ theo yêu cầu người dùng");
    await navigateToServers(page);

    // Toggle yêu thích server Creative Hub (ban đầu chưa yêu thích)
    const creativeRow = page.locator('#serverList .server-row[data-server-id="server-creative"]');
    await expect(creativeRow.locator(".server-fav-mark")).toHaveCount(0);
    await creativeRow.locator("[data-server-favorite]").click();
    await expect(creativeRow.locator(".server-fav-mark")).toBeVisible();

    // Reload kiểm tra persistence
    await page.reload();
    await navigateToServers(page);
    const persistedRow = page.locator('#serverList .server-row[data-server-id="server-creative"]');
    await expect(persistedRow.locator(".server-fav-mark")).toBeVisible();

    // Sửa server
    await persistedRow.locator("[data-server-edit]").click();
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await page.locator("#serverNameInput").fill("Creative Hub V2");
    await dialogHost.locator("[data-dialog-primary]").click();
    await expect(dialogHost).toBeHidden();
    await expect(persistedRow.locator(".server-name")).toHaveText("Creative Hub V2");

    // Xóa server có dialog xác nhận
    await persistedRow.locator("[data-server-delete]").click();
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost.locator(".dialog-title")).toContainText("Xác nhận xóa");
    await dialogHost.locator("[data-dialog-primary]").click();
    await expect(dialogHost).toBeHidden();

    // Assert đã xóa
    await expect(page.locator("#serversCount")).toHaveText("3");
    await expect(page.locator('#serverList .server-row[data-server-id="server-creative"]')).toHaveCount(0);
  });

  test("DSN-AC-06: Nút Chơi ngay (Join) trên thẻ server kết nối bằng instance phù hợp, nút Sao chép IP copy địa chỉ kèm toast", async ({ page }) => {
    test.skip(true, "Mục Server đã được gỡ bỏ theo yêu cầu người dùng");
    await navigateToServers(page);

    const firstCard = page.locator("#serverList .server-row").first();

    // Nút copy IP inline
    await firstCard.locator(".server-inline-copy-btn").click();
    await expect(page.locator(".toast")).toContainText("Đã sao chép địa chỉ");

    // Nút Chơi ngay
    const joinBtn = firstCard.locator("[data-server-join]");
    await joinBtn.click();
    await expect(page.locator(".toast").last()).toContainText(/kết nối/i);
  });

  test("DSN-AC-07: News Carousel trên trang chủ hiển thị các slide tin tức với badge danh mục, ngày đăng, tiêu đề; nút Next/Prev và dots điều hướng hoạt động mượt", async ({ page }) => {
    // Đang ở trang chủ #page-instances
    const carousel = page.locator("#newsCarousel");
    await expect(carousel).toBeVisible();

    const slides = carousel.locator(".news-slide");
    await expect(slides).toHaveCount(3);

    const dots = page.locator("#newsDots .news-dot");
    await expect(dots).toHaveCount(3);
    await expect(dots.nth(0)).toHaveClass(/is-active/);

    // Chuyển sang slide tiếp theo bằng nút Next
    await page.locator("#newsNavNext").click();
    await expect(dots.nth(1)).toHaveClass(/is-active/);

    // Chuyển sang slide tiếp theo nữa
    await page.locator("#newsNavNext").click();
    await expect(dots.nth(2)).toHaveClass(/is-active/);

    // Quay lại slide trước bằng nút Prev
    await page.locator("#newsNavPrev").click();
    await expect(dots.nth(1)).toHaveClass(/is-active/);

    // Chuyển trực tiếp qua click dot
    await dots.nth(0).click();
    await expect(dots.nth(0)).toHaveClass(/is-active/);
  });

  test("DSN-AC-08: Click vào slide tin tức mở Dialog đọc bài viết chi tiết (News Article Reader) với hero banner, nội dung đầy đủ và nút hành động nhanh", async ({ page }) => {
    const firstSlide = page.locator("#newsCarousel .news-slide").first();
    await firstSlide.click();

    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();

    const articleDialog = dialogHost.locator(".news-article-dialog");
    await expect(articleDialog).toBeVisible();
    await expect(articleDialog.locator(".news-article-hero")).toBeVisible();
    await expect(articleDialog.locator(".news-article-title")).toBeVisible();
    await expect(articleDialog.locator(".news-article-body")).toBeVisible();

    const actionBtn = articleDialog.locator("#newsInnerActionBtn");
    await expect(actionBtn).toBeVisible();

    // Bấm nút đóng dialog
    await dialogHost.locator("[data-dialog-close]").click();
    await expect(dialogHost).toBeHidden();
  });

  test("DSN-AC-09: Responsive (390px, 620px) không tràn ngang; phím Escape đóng các dialog; :focus-visible rõ ràng; Reduced motion duration <= 0.05s", async ({ page }) => {
    // Responsive 390px và 620px không tràn ngang
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 800 });
      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
      expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 1);
    }

    // Reset viewport về desktop
    await page.setViewportSize({ width: 1280, height: 800 });

    // Escape đóng dialog
    await page.locator("#newsCarousel .news-slide").first().click();
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(dialogHost).toBeHidden();

    // :focus-visible outline >= 2px
    const newsNextBtn = page.locator("#newsNavNext");
    await newsNextBtn.focus();
    const outlineWidth = await newsNextBtn.evaluate((el) => {
      const style = window.getComputedStyle(el);
      return parseFloat(style.outlineWidth) || 0;
    });
    // Trình duyệt hỗ trợ focus-visible
    expect(outlineWidth).toBeGreaterThanOrEqual(2);

    // Reduced motion duration <= 0.05s
    await page.emulateMedia({ reducedMotion: "reduce" });
    const carouselTrack = page.locator("#newsCarouselTrack");
    if (await carouselTrack.count() > 0) {
      const duration = await carouselTrack.evaluate((el) => {
        const style = window.getComputedStyle(el);
        return parseFloat(style.transitionDuration) || 0;
      });
      expect(duration).toBeLessThanOrEqual(0.05);
    }
  });

  test("DSN-AC-11: Không hồi quy CSS: Không tồn tại transition: all hoặc transition-property: all", async ({ page }) => {
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
          // ignore CORS cross-origin sheets
        }
      }
      return found;
    });
    expect(hasTransitionAll).toBe(false);
  });
});
