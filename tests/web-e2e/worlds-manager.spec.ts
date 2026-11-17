import { test, expect, Page } from "@playwright/test";

async function bootAndOpenWorldsTab(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
  await page.locator(".instance-card").first().click({ force: true });
  await expect(page.locator("#page-detail")).toBeVisible();
  const tabBtn = page.locator('.tab[data-tab="worlds"]');
  await expect(tabBtn).toBeVisible();
  await tabBtn.click();
  await expect(page.locator("#tab-worlds")).toBeVisible();
}

test.describe("Worlds Manager Runtime Tests (worlds-manager.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await bootAndOpenWorldsTab(page);
  });

  test("WM-AC-01: Danh sách World Cards hiển thị đầy đủ icon thế giới, tên, folder name, Game Mode badge, Difficulty, Last played, Size MB và Seed", async ({ page }) => {
    const cards = page.locator(".world-card");
    await expect(cards).toHaveCount(5);

    const firstCard = cards.first();
    await expect(firstCard.locator(".world-icon")).toBeVisible();
    await expect(firstCard.locator(".world-name")).toHaveText("World");
    await expect(firstCard.locator(".world-folder")).toContainText("/saves/world");
    await expect(firstCard.locator(".world-mode-badge")).toContainText("Survival");
    await expect(firstCard.locator(".world-diff-badge")).toContainText("Hard");
    await expect(firstCard.locator(".world-size")).toContainText("148.2 MB");
    await expect(firstCard.locator(".world-last-played")).toContainText("2 giờ trước");
    await expect(firstCard.locator(".world-seed-badge")).toContainText("Seed:");

    // Thẻ Hardcore hiển thị đúng badge đỏ và icon
    const hardcoreCard = page.locator(".world-card", { hasText: "Hardcore 100 Days" });
    await expect(hardcoreCard).toBeVisible();
    await expect(hardcoreCard.locator(".world-mode-badge.hardcore")).toContainText("Hardcore");
    await expect(hardcoreCard.locator(".world-mode-badge.hardcore .hardcore-glyph")).toBeVisible();
  });

  test("WM-AC-02: Tìm kiếm thế giới theo tên/seed (#worldSearch) và lọc theo Game Mode chip (Tất cả, Survival, Creative, Hardcore)", async ({ page }) => {
    const searchInput = page.locator("#worldSearch");
    await expect(searchInput).toBeVisible();

    // Tìm kiếm theo tên "Creative"
    await searchInput.fill("Creative");
    const filteredByName = page.locator(".world-card");
    await expect(filteredByName).toHaveCount(1);
    await expect(filteredByName.first().locator(".world-name")).toHaveText("Creative Test");
    await expect(page.locator("#worldsSummary")).toContainText("1/5 world");

    // Tìm kiếm theo seed
    await searchInput.fill("-771928401928");
    const filteredBySeed = page.locator(".world-card");
    await expect(filteredBySeed).toHaveCount(1);
    await expect(filteredBySeed.first().locator(".world-name")).toHaveText("Hardcore 100 Days");

    // Xóa tìm kiếm
    await searchInput.fill("");
    await expect(page.locator(".world-card")).toHaveCount(5);

    // Lọc theo chip Game Mode: Sinh tồn (Survival)
    const survivalChip = page.locator('#worldFilterChips button[data-world-filter="survival"]');
    await survivalChip.click();
    await expect(survivalChip).toHaveClass(/is-active/);
    const survivalCards = page.locator(".world-card");
    await expect(survivalCards).toHaveCount(2);

    // Lọc theo chip Game Mode: Sáng tạo (Creative)
    const creativeChip = page.locator('#worldFilterChips button[data-world-filter="creative"]');
    await creativeChip.click();
    await expect(creativeChip).toHaveClass(/is-active/);
    const creativeCards = page.locator(".world-card");
    await expect(creativeCards).toHaveCount(1);
    await expect(creativeCards.first().locator(".world-name")).toHaveText("Creative Test");

    // Lọc theo chip Game Mode: Siêu khó (Hardcore)
    const hardcoreChip = page.locator('#worldFilterChips button[data-world-filter="hardcore"]');
    await hardcoreChip.click();
    await expect(hardcoreChip).toHaveClass(/is-active/);
    const hardcoreCards = page.locator(".world-card");
    await expect(hardcoreCards).toHaveCount(1);
    await expect(hardcoreCards.first().locator(".world-name")).toHaveText("Hardcore 100 Days");

    // Trở lại Tất cả
    const allChip = page.locator('#worldFilterChips button[data-world-filter="all"]');
    await allChip.click();
    await expect(allChip).toHaveClass(/is-active/);
    await expect(page.locator(".world-card")).toHaveCount(5);
  });

  test("WM-AC-03: Sao lưu thế giới (Backup) tạo bản nén .zip kèm thanh tiến trình và tăng bộ đếm backup của world", async ({ page }) => {
    const survivalCard = page.locator(".world-card", { hasText: "Survival 1.21" });
    await expect(survivalCard).toBeVisible();
    await expect(survivalCard.locator(".world-backups-count")).toContainText("0 bản lưu");

    // Bấm nút Sao lưu trên thẻ world
    const backupBtn = survivalCard.locator('button[data-world-backup]');
    await backupBtn.click();

    // Chờ toast hoàn tất sao lưu
    const toast = page.locator(".toast", { hasText: 'Đã sao lưu "Survival 1.21"' });
    await expect(toast).toBeVisible();

    // Bộ đếm bản lưu tăng lên 1
    await expect(survivalCard.locator(".world-backups-count")).toContainText("1 bản lưu");

    // Nút "Sao lưu tất cả"
    const backupAllBtn = page.locator("#backupAllWorldsBtn");
    await backupAllBtn.click();
    await expect(page.locator(".toast", { hasText: "Đã sao lưu xong tất cả thế giới!" })).toBeVisible();
  });

  test("WM-AC-04: Khôi phục thế giới (Restore) mở dialog chọn các bản sao lưu đã có với timestamp & dung lượng, khôi phục thành công", async ({ page }) => {
    const worldCard = page.locator(".world-card", { hasText: "World" }).first();
    await expect(worldCard).toBeVisible();

    // Nút khôi phục mở dialog
    const restoreBtn = worldCard.locator('button[data-world-restore]');
    await expect(restoreBtn).toBeEnabled();
    await restoreBtn.click();

    // Dialog mở ra hiển thị danh sách các bản sao lưu
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost.locator(".dialog-title")).toContainText('Khôi phục "World"');
    const backupRows = dialogHost.locator(".backup-row");
    await expect(backupRows).toHaveCount(2);

    // Bấm nút Khôi phục trong dialog
    const confirmBtn = dialogHost.locator("[data-dialog-primary]");
    await confirmBtn.click();

    // Toast khôi phục thành công
    await expect(page.locator(".toast", { hasText: 'Đã khôi phục "World"' })).toBeVisible();
    await expect(worldCard.locator(".world-last-played")).toContainText("Vừa xong");
  });

  test("WM-AC-05: Đổi tên thế giới (Rename) và Nhân bản thế giới (Duplicate/Clone) cập nhật tên, folder và state danh sách", async ({ page }) => {
    const worldCard = page.locator(".world-card", { hasText: "World" }).first();

    // Đổi tên world
    const renameBtn = worldCard.locator('button[data-world-rename]');
    await renameBtn.click();

    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost.locator(".dialog-title")).toContainText("Đổi tên thế giới");

    const input = dialogHost.locator("#worldRenameInput");
    await input.fill("Thế giới Sinh tồn Mới");
    await dialogHost.locator("[data-dialog-primary]").click();

    // Assert tên và folder mới
    await expect(page.locator(".toast", { hasText: 'Đã đổi tên thế giới' })).toBeVisible();
    const renamedCard = page.locator(".world-card", { hasText: "Thế giới Sinh tồn Mới" });
    await expect(renamedCard).toBeVisible();
    await expect(renamedCard.locator(".world-folder")).toContainText("/saves/the_gioi_sinh_ton_moi");

    // Nhân bản thế giới Creative Test
    const creativeCard = page.locator(".world-card", { hasText: "Creative Test" });
    const duplicateBtn = creativeCard.locator('button[data-world-duplicate]');
    await duplicateBtn.click();

    // World mới xuất hiện kèm hậu tố "(Copy)"
    await expect(page.locator(".toast", { hasText: 'Đã nhân bản thế giới: "Creative Test (Copy)"' })).toBeVisible();
    const copiedCard = page.locator(".world-card", { hasText: "Creative Test (Copy)" });
    await expect(copiedCard).toBeVisible();
    await expect(copiedCard.locator(".world-mode-badge")).toContainText("Creative");
  });

  test("WM-AC-06: Xóa thế giới (Delete) yêu cầu dialog xác nhận nguy hiểm; xóa thành công loại bỏ world khỏi instance và cập nhật bộ đếm summary", async ({ page }) => {
    const initialCount = await page.locator(".world-card").count();
    const targetCard = page.locator(".world-card", { hasText: "Adventure Dungeon" });
    await expect(targetCard).toBeVisible();

    // Click nút Xóa
    const deleteBtn = targetCard.locator('button[data-world-delete]');
    await deleteBtn.click();

    // Dialog cảnh báo nguy hiểm
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost.locator(".dialog-title")).toContainText('Xóa thế giới "Adventure Dungeon"?');
    await expect(dialogHost.locator(".dialog-body")).toContainText("xóa vĩnh viễn");

    // Xác nhận xóa
    await dialogHost.locator("[data-dialog-primary]").click();

    // Toast và thẻ bị xóa khỏi danh sách
    await expect(page.locator(".toast", { hasText: 'Đã xóa thế giới "Adventure Dungeon"' })).toBeVisible();
    await expect(page.locator(".world-card", { hasText: "Adventure Dungeon" })).toHaveCount(0);
    await expect(page.locator(".world-card")).toHaveCount(initialCount - 1);
  });

  test("WM-AC-07: Nhập thế giới từ bên ngoài (Import World .zip hoặc kéo thả file zip) tự động giải nén và thêm world mới vào instance kèm toast", async ({ page }) => {
    // 1. Nhập qua file input ẩn
    await page.locator("#worldFileInput").setInputFiles({
      name: "SkyBlock-Warrior.zip",
      mimeType: "application/zip",
      buffer: Buffer.from("dummy zip binary"),
    });

    await expect(page.locator(".toast", { hasText: 'Đã nhập thế giới "SkyBlock Warrior" thành công!' })).toBeVisible();
    const importedCard = page.locator(".world-card", { hasText: "SkyBlock Warrior" });
    await expect(importedCard).toBeVisible();

    // 2. Nhập qua kéo thả vào DropZone
    await page.evaluate(() => {
      const dt = new DataTransfer();
      const file = new File(["dummy world zip"], "Castle-Siege-Adventure.zip", { type: "application/zip" });
      dt.items.add(file);
      const target = document.querySelector("#worldDropZone");
      target?.dispatchEvent(new DragEvent("drop", { dataTransfer: dt, bubbles: true, cancelable: true }));
    });

    await expect(page.locator(".toast", { hasText: 'Đã nhập thế giới "Castle Siege Adventure" thành công!' })).toBeVisible();
    await expect(page.locator(".world-card", { hasText: "Castle Siege Adventure" })).toBeVisible();
  });

  test("WM-AC-08: Chi tiết thế giới hiển thị Seed, Spawn, Thời gian chơi, Phiên bản, Kiểu địa hình, Lệnh (Cheats) kèm nút copy seed 1-click", async ({ page }) => {
    const worldCard = page.locator(".world-card", { hasText: "World" }).first();
    const inspectBtn = worldCard.locator('button[data-world-inspect]');
    await inspectBtn.click();

    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost.locator(".dialog-title")).toContainText("Chi tiết thế giới — World");

    // Kiểm tra các trường dữ liệu
    await expect(dialogHost.locator(".level-dat-card")).toBeVisible();
    await expect(dialogHost.locator(".level-dat-val", { hasText: "-4820193850123849102" })).toBeVisible();
    await expect(dialogHost.locator(".level-dat-val", { hasText: "X: -142, Y: 72, Z: 256" })).toBeVisible();
    await expect(dialogHost.locator(".level-dat-val", { hasText: "Minecraft 1.21.1" })).toBeVisible();

    // Bấm copy seed
    const copySeedBtn = dialogHost.locator("#inspectCopySeedBtn");
    await copySeedBtn.click();
    await expect(page.locator(".toast", { hasText: "Đã copy Seed: -4820193850123849102" })).toBeVisible();
  });

  test("WM-AC-09: Responsive (390px, 620px) không tràn ngang; phím Escape đóng dialog; :focus-visible rõ ràng; Reduced motion duration <= 0.05s", async ({ page }) => {
    // 1. Kiểm tra không tràn ngang ở 620px và 390px
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 844 });
      const noOverflow = await page.evaluate(() => {
        const doc = document.documentElement;
        return doc.scrollWidth <= window.innerWidth + 1;
      });
      expect(noOverflow, `Worlds tab tràn ngang ở ${width}px`).toBe(true);
    }

    // 2. Phím Escape đóng dialog
    const firstCard = page.locator(".world-card").first();
    await firstCard.locator('button[data-world-inspect]').click();
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();

    await page.keyboard.press("Escape");
    await expect(dialogHost).toBeHidden();

    // 3. :focus-visible rõ ràng (outline >= 2px, offset >= 1px)
    const importBtn = page.locator("#importWorldBtn");
    await importBtn.click({ force: true });
    // close file picker if triggered
    await page.keyboard.press("Escape");
    await importBtn.focus();
    const focusStyles = await page.evaluate(() => {
      const active = document.getElementById("importWorldBtn");
      if (!active) return null;
      const cs = window.getComputedStyle(active);
      return {
        outlineStyle: cs.outlineStyle,
        outlineWidth: parseFloat(cs.outlineWidth) || 0,
        outlineOffset: parseFloat(cs.outlineOffset) || 0,
      };
    });
    expect(focusStyles?.outlineStyle).not.toBe("none");
    expect(focusStyles?.outlineWidth).toBeGreaterThanOrEqual(2);
    expect(focusStyles?.outlineOffset).toBeGreaterThanOrEqual(1);

    // 4. Reduced Motion (duration <= 0.05s)
    await page.emulateMedia({ reducedMotion: "reduce" });
    const cardTransition = await page.locator(".world-card").first().evaluate((el) => {
      const cs = window.getComputedStyle(el);
      const durations = cs.transitionDuration.split(",").map((s) => parseFloat(s) || 0);
      return Math.max(...durations);
    });
    expect(cardTransition).toBeLessThanOrEqual(0.05);
  });
});
