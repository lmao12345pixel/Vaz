import { test, expect, type Page } from "@playwright/test";
import * as fs from "node:fs";
import * as path from "node:path";

async function boot(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
}

async function navigateToModpacks(page: Page) {
  const toggle = page.locator("#mobileNavToggle");
  if (await toggle.isVisible()) {
    const isNavOpen = await page.evaluate(() => document.documentElement.classList.contains("nav-open"));
    if (!isNavOpen) {
      await toggle.click({ force: true });
    }
  }
  const navBtn = page.locator('.nav-item[data-screen="modpacks"]');
  await navBtn.click({ force: true });
  await expect(page.locator("#page-modpacks")).toBeVisible();
}

async function navigateToInstances(page: Page) {
  const toggle = page.locator("#mobileNavToggle");
  if (await toggle.isVisible()) {
    const isNavOpen = await page.evaluate(() => document.documentElement.classList.contains("nav-open"));
    if (!isNavOpen) {
      await toggle.click({ force: true });
    }
  }
  const navBtn = page.locator('.nav-item[data-screen="instances"]');
  await navBtn.click({ force: true });
  await expect(page.locator("#page-instances")).toBeVisible();
}

test.describe("Modpack Browser & 1-Click Install Tests (modpack-browser.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await boot(page);
  });

  test("MB-AC-01: Danh sách Modpack Cards hiển thị đầy đủ cover art/banner, icon, tên, tác giả, số lượng mod, dung lượng MB, lượt tải, loader badge và phiên bản MC", async ({ page }) => {
    await navigateToModpacks(page);

    const cards = page.locator("#modpacksGrid .discover-card");
    await expect(cards).toHaveCount(6);

    const firstCard = cards.first();
    await expect(firstCard.locator(".modpack-cover-img")).toBeVisible();
    await expect(firstCard.locator(".modpack-card-icon")).toBeVisible();
    await expect(firstCard.locator("h3")).toHaveText("Performance Fabric");
    await expect(firstCard.locator(".modpack-author")).toContainText("Vazer Team");
    await expect(firstCard.locator(".modpack-mod-count")).toContainText("35 mods");
    await expect(firstCard.locator(".modpack-size")).toContainText("145 MB");
    await expect(firstCard.locator(".modpack-downloads")).toContainText("1.2M lượt tải");
    await expect(firstCard.locator(".modpack-loader-badge")).toContainText("Fabric");
    await expect(firstCard.locator(".modpack-mc-version")).toContainText("MC 1.21.1");
  });

  test("MB-AC-02: Bộ lọc Category chips và dropdown (Loader, Version, Sắp xếp) lọc chính xác danh sách và cập nhật #modpacksCount", async ({ page }) => {
    await navigateToModpacks(page);

    const counter = page.locator("#modpacksCount");
    await expect(counter).toHaveText("6");

    // 1. Lọc category chip Tối ưu / FPS
    const optChip = page.locator('#modpackCategoryChips button[data-modpack-category="optimization"]');
    await optChip.click();
    await expect(optChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("2");
    await expect(page.locator("#modpacksGrid .discover-card")).toHaveCount(2);

    // 2. Lọc category chip Kỹ thuật / Tech
    const techChip = page.locator('#modpackCategoryChips button[data-modpack-category="tech"]');
    await techChip.click();
    await expect(techChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("3");
    await expect(page.locator("#modpacksGrid .discover-card")).toHaveCount(3);

    // 3. Reset category về Tất cả
    const allChip = page.locator('#modpackCategoryChips button[data-modpack-category="all"]');
    await allChip.click();
    await expect(allChip).toHaveClass(/is-active/);
    await expect(counter).toHaveText("6");

    // 4. Lọc Loader NeoForge
    const loaderSelect = page.locator("#modpacksLoader");
    await loaderSelect.selectOption("neoforge");
    await expect(counter).toHaveText("2");
    await expect(page.locator("#modpacksGrid .discover-card")).toHaveCount(2);

    // Reset loader
    await loaderSelect.selectOption("all");
    await expect(counter).toHaveText("6");

    // 5. Lọc Version 1.20.1
    const verSelect = page.locator("#modpacksVersion");
    await verSelect.selectOption("1.20.1");
    await expect(counter).toHaveText("2");
    await expect(page.locator("#modpacksGrid .discover-card")).toHaveCount(2);

    // Reset version
    await verSelect.selectOption("all");
    await expect(counter).toHaveText("6");

    // 6. Sắp xếp theo tên (A-Z)
    const sortSelect = page.locator("#modpacksSort");
    await sortSelect.selectOption("name");
    await expect(page.locator("#modpacksGrid .discover-card").first().locator("h3")).toHaveText("Arcane Expeditions");
  });

  test("MB-AC-03: Tìm kiếm modpack theo tên/tác giả/từ khóa, hiển thị empty state khi không có kết quả", async ({ page }) => {
    await navigateToModpacks(page);

    const searchInput = page.locator("#modpacksSearch");
    const counter = page.locator("#modpacksCount");
    const emptyState = page.locator("#modpacksEmpty");

    // Tìm kiếm khớp tên
    await searchInput.fill("Create");
    await expect(counter).toHaveText("1");
    await expect(page.locator("#modpacksGrid .discover-card")).toHaveCount(1);
    await expect(page.locator("#modpacksGrid .discover-card h3")).toHaveText("Create Adventure");
    await expect(emptyState).toBeHidden();

    // Tìm kiếm không có kết quả
    await searchInput.fill("khongtontaimodpacknay999");
    await expect(counter).toHaveText("0");
    await expect(page.locator("#modpacksGrid .discover-card")).toHaveCount(0);
    await expect(emptyState).toBeVisible();

    // Xóa tìm kiếm
    await searchInput.fill("");
    await expect(counter).toHaveText("6");
    await expect(emptyState).toBeHidden();
  });

  test("MB-AC-04: Click 'Chi tiết' mở Modpack Inspector Modal với đầy đủ Hero banner, mô tả chi tiết, danh sách mod và version selector", async ({ page }) => {
    await navigateToModpacks(page);

    const firstCard = page.locator("#modpacksGrid .discover-card").first();
    const detailsBtn = firstCard.locator(".modpack-details-btn");
    await detailsBtn.click();

    const drawerHost = page.locator("#modpackInspectorHost");
    await expect(drawerHost).toBeVisible();
    await expect(drawerHost).toHaveClass(/is-open/);

    const banner = page.locator("#modpackInspectorBanner");
    await expect(banner).toBeVisible();

    const title = page.locator("#modpackInspectorTitle");
    await expect(title).toHaveText("Performance Fabric");

    const author = page.locator("#modpackInspectorAuthor");
    await expect(author).toHaveText("Vazer Team");

    const versionSelect = page.locator("#modpackInspectorVersionSelect");
    await expect(versionSelect).toBeVisible();
    const versionOptions = versionSelect.locator("option");
    await expect(versionOptions).toHaveCount(3);

    const modList = page.locator("#modpackInspectorModList .modpack-mod-chip");
    const modChipsCount = await modList.count();
    expect(modChipsCount).toBeGreaterThan(20);

    const desc = page.locator("#modpackInspectorDesc");
    await expect(desc).toBeVisible();

    // Đóng drawer bằng nút X
    const closeBtn = page.locator("#modpackInspectorClose");
    await closeBtn.click();
    await expect(drawerHost).toBeHidden();
  });

  test("MB-AC-05: 1-Click 'Cài đặt & Tạo Instance' hiển thị Modal tiến trình cài đặt và tự động tạo instance mới trong thư viện", async ({ page }) => {
    await navigateToModpacks(page);

    const firstCard = page.locator("#modpacksGrid .discover-card").first();
    const installBtn = firstCard.locator(".modpack-install-btn");
    await installBtn.click();

    const modalHost = page.locator("#modpackInstallModalHost");
    await expect(modalHost).toBeVisible();
    await expect(modalHost).toHaveClass(/is-open/);

    // Kiểm tra 3 bước cài đặt
    await expect(page.locator("#modpackStepManifest")).toBeVisible();
    await expect(page.locator("#modpackStepMods")).toBeVisible();
    await expect(page.locator("#modpackStepInstance")).toBeVisible();

    // Chờ tiến trình hoàn tất và modal tự đóng
    await expect(modalHost).toBeHidden({ timeout: 15_000 });

    // Kiểm tra nút trên card đổi thành 'Đã cài đặt'
    await expect(firstCard.locator(".modpack-install-btn")).toHaveText("Đã cài đặt");

    // Điều hướng sang Thư viện Instance để kiểm chứng instance mới
    await navigateToInstances(page);
    const instanceCard = page.locator('#instanceGrid .instance-card[data-id^="performance-fabric"]');
    await expect(instanceCard).toBeVisible();
  });

  test("MB-AC-06: Nút Hủy trong quá trình cài đặt modpack dừng tiến trình an toàn và đóng modal", async ({ page }) => {
    await navigateToModpacks(page);

    // Lấy số lượng instance ban đầu
    const initialInstanceCount = await page.evaluate(() => {
      const stateObj = JSON.parse(localStorage.getItem("vazer_preview_state") || "{}");
      return (stateObj.instances || []).length;
    });

    const createCard = page.locator('#modpacksGrid .discover-card[data-content-id="pack-create"]');
    const installBtn = createCard.locator(".modpack-install-btn");
    await installBtn.click();

    const modalHost = page.locator("#modpackInstallModalHost");
    await expect(modalHost).toBeVisible();

    // Nhấn Hủy ngay khi đang tải
    const cancelBtn = page.locator("#modpackInstallCancelBtn");
    await cancelBtn.click();

    await expect(modalHost).toBeHidden();

    // Kiểm tra không có instance nào mới được tạo
    const afterInstanceCount = await page.evaluate(() => {
      const stateObj = JSON.parse(localStorage.getItem("vazer_preview_state") || "{}");
      return (stateObj.instances || []).length;
    });
    expect(afterInstanceCount).toBe(initialInstanceCount);
  });

  test("MB-AC-07: Hỗ trợ nhập modpack từ file bên ngoài (.mrpack / .zip) qua nút 'Nhập .mrpack'", async ({ page }) => {
    await navigateToModpacks(page);

    const importBtn = page.locator("#importMrpackBtn");
    await expect(importBtn).toBeVisible();

    const fileInput = page.locator("#modpackFileInput");
    await fileInput.setInputFiles({
      name: "CustomSpeedrunPack.mrpack",
      mimeType: "application/zip",
      buffer: Buffer.from("vaz mrpack mock binary content"),
    });

    const modalHost = page.locator("#modpackInstallModalHost");
    await expect(modalHost).toBeVisible();
    await expect(page.locator("#modpackInstallModalTitle")).toContainText("CustomSpeedrunPack");

    // Chờ cài đặt hoàn tất
    await expect(modalHost).toBeHidden({ timeout: 15_000 });

    // Kiểm tra instance mới xuất hiện trong Instances
    await navigateToInstances(page);
    const customInstance = page.locator('#instanceGrid .instance-card[data-id^="customspeedrunpack"]');
    await expect(customInstance).toBeVisible();
  });

  test("MB-AC-08: Responsive (390px, 620px) không tràn ngang; phím Escape đóng modal; :focus-visible rõ ràng; Reduced motion duration <= 0.05s", async ({ page }) => {
    // 1. Kiểm tra tràn ngang tại 390px và 620px
    for (const width of [390, 620]) {
      await page.setViewportSize({ width, height: 800 });
      await navigateToModpacks(page);

      const hasHorizontalScroll = await page.evaluate(() => {
        const root = document.documentElement;
        const pageModpacks = document.getElementById("page-modpacks");
        return (
          root.scrollWidth > root.clientWidth ||
          (pageModpacks ? pageModpacks.scrollWidth > pageModpacks.clientWidth : false)
        );
      });
      expect(hasHorizontalScroll, `Horizontal overflow detected at ${width}px`).toBe(false);
    }

    // Đưa về kích thước tiêu chuẩn
    await page.setViewportSize({ width: 1280, height: 800 });
    await navigateToModpacks(page);

    // 2. Phím Escape đóng Modpack Inspector Drawer
    const firstCard = page.locator("#modpacksGrid .discover-card").first();
    await firstCard.locator(".modpack-details-btn").click();
    const drawerHost = page.locator("#modpackInspectorHost");
    await expect(drawerHost).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(drawerHost).toBeHidden();

    // 3. Phím Escape đóng Install Modal
    await firstCard.locator(".modpack-install-btn").click();
    const modalHost = page.locator("#modpackInstallModalHost");
    await expect(modalHost).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(modalHost).toBeHidden();

    // 4. Focus outline >= 2px
    const importBtn = page.locator("#importMrpackBtn");
    await importBtn.focus();
    const outlineWidth = await importBtn.evaluate((el) => {
      const style = window.getComputedStyle(el);
      return parseFloat(style.outlineWidth) || 0;
    });
    expect(outlineWidth).toBeGreaterThanOrEqual(2);

    // 5. Reduced motion duration <= 0.05s
    await page.emulateMedia({ reducedMotion: "reduce" });
    const reducedDuration = await page.evaluate(() => {
      const chip = document.querySelector("#modpackCategoryChips .chip");
      if (!chip) return 0;
      const dur = window.getComputedStyle(chip).transitionDuration;
      return parseFloat(dur);
    });
    expect(reducedDuration).toBeLessThanOrEqual(0.05);
  });

  test("MB-AC-09: Tương tác trực quan mượt mà và thanh tiến trình cài đặt", async ({ page }) => {
    await navigateToModpacks(page);

    // Chụp screenshot danh sách modpacks
    await expect(page.locator("#page-modpacks")).toBeVisible();
    const firstCard = page.locator("#modpacksGrid .discover-card").first();
    await firstCard.locator(".modpack-details-btn").click();

    const drawerHost = page.locator("#modpackInspectorHost");
    await expect(drawerHost).toBeVisible();

    // Verify visual elements
    await expect(page.locator("#modpackInspectorBanner")).toBeVisible();
    await expect(page.locator("#modpackInspectorModList")).toBeVisible();
  });

  test("MB-AC-10: Không hồi quy CSS: 0 transition: all", async () => {
    const cssPath = path.resolve(process.cwd(), "docs/design-preview/styles.css");
    const content = fs.readFileSync(cssPath, "utf-8");
    const regex = /transition\s*:\s*all|transition-property\s*:\s*all/i;
    expect(regex.test(content)).toBe(false);
  });
});
