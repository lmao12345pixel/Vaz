import { test, expect } from "@playwright/test";

test.describe("Crash Analyzer & Auto Diagnostic Assistant Runtime", () => {
  test.beforeEach(async ({ page, context }) => {
    await context.grantPermissions(["clipboard-read", "clipboard-write"]).catch(() => {});
    await page.goto("/");
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  });

  async function triggerScenario(page, scenario: "oom" | "java" | "dependency" | "mixin") {
    // Open Dev Tools popover from bottom dock
    await page.locator("#mcDevToolsBtn").click({ force: true });
    await expect(page.locator("#mcDevPopover")).toBeVisible();

    // Select scenario and trigger crash
    await page.locator("#crashScenarioSelect").selectOption(scenario);
    await page.locator("#simulateCrashBtn").click({ force: true });

    // Dialog pops up, click "Xem Logs & Quick Fix"
    const primaryBtn = page.locator("#dialogHost [data-dialog-primary]");
    await expect(primaryBtn).toBeVisible();
    await primaryBtn.click({ force: true });
    await expect(page.locator("#dialogHost")).toBeHidden();

    // Ensure we landed on detail screen and logs tab
    await expect(page.locator("#page-detail")).toBeVisible();
    await expect(page.locator("#tab-logs")).toBeVisible();
    await expect(page.locator("#crashAnalyzerBanner")).toBeVisible();
  }

  test("CRASH-AC-01 & CRASH-AC-02 (OOM): Phát hiện Out of Memory và Quick Fix tăng RAM", async ({ page }) => {
    await triggerScenario(page, "oom");

    const banner = page.locator("#crashAnalyzerBanner");
    await expect(banner).toBeVisible();
    await expect(banner.locator(".crash-analyzer-title")).toContainText("Tràn bộ nhớ RAM");
    await expect(banner.locator(".crash-fix-btn")).toContainText("Tự động nâng RAM");

    // Click 1-Click Quick Fix
    await banner.locator(".crash-fix-btn").click({ force: true });

    // Verify banner switches to is-fixed state
    await expect(banner).toHaveClass(/is-fixed/);
    await expect(banner.locator(".crash-analyzer-title")).toContainText("Đã áp dụng Quick Fix thành công!");

    // Verify toast notification
    await expect(page.locator(".toast", { hasText: "Đã nâng RAM lên" })).toBeVisible();
  });

  test("CRASH-AC-02 (Java): Phát hiện sai phiên bản Java và Quick Fix chuyển sang Java 21 LTS", async ({ page }) => {
    await triggerScenario(page, "java");

    const banner = page.locator("#crashAnalyzerBanner");
    await expect(banner).toBeVisible();
    await expect(banner.locator(".crash-analyzer-title")).toContainText("Không tương thích Java Runtime");
    await expect(banner.locator(".crash-fix-btn")).toContainText("Đổi sang Java 21 LTS");

    // Click Quick Fix
    await banner.locator(".crash-fix-btn").click({ force: true });

    await expect(banner).toHaveClass(/is-fixed/);
    await expect(page.locator(".toast", { hasText: "Đã chuyển sang Java 21 LTS" })).toBeVisible();
  });

  test("CRASH-AC-02 (Dependency): Phát hiện thiếu Fabric API và Quick Fix tự động thêm mod", async ({ page }) => {
    await triggerScenario(page, "dependency");

    const banner = page.locator("#crashAnalyzerBanner");
    await expect(banner).toBeVisible();
    await expect(banner.locator(".crash-analyzer-title")).toContainText("Thiếu Mod Phụ Thuộc");
    await expect(banner.locator(".crash-fix-btn")).toContainText("Tự động cài đặt Fabric API");

    // Click Quick Fix
    await banner.locator(".crash-fix-btn").click({ force: true });

    await expect(banner).toHaveClass(/is-fixed/);
    await expect(page.locator(".toast", { hasText: "Đã cài đặt Fabric API" })).toBeVisible();

    // Switch to Mods tab to verify fabric-api is present
    await page.locator('button.tab[data-tab="mods"]').click({ force: true });
    await expect(page.locator("#tab-mods")).toBeVisible();
    await expect(page.locator(".mod-row", { hasText: "Fabric API" })).toBeVisible();
  });

  test("CRASH-AC-02 (Mixin): Phát hiện xung đột Mixin và Quick Fix vô hiệu hóa mod", async ({ page }) => {
    await triggerScenario(page, "mixin");

    const banner = page.locator("#crashAnalyzerBanner");
    await expect(banner).toBeVisible();
    await expect(banner.locator(".crash-analyzer-title")).toContainText("Xung đột Mixin");
    await expect(banner.locator(".crash-fix-btn")).toContainText("Tắt mod");

    // Click Quick Fix
    await banner.locator(".crash-fix-btn").click({ force: true });

    await expect(banner).toHaveClass(/is-fixed/);
    await expect(page.locator(".toast", { hasText: "Đã tắt mod" })).toBeVisible();
  });

  test("CRASH-AC-04: Nút Sao chép báo cáo lưu chẩn đoán vào clipboard", async ({ page }) => {
    await triggerScenario(page, "oom");

    const copyBtn = page.locator("#crashCopyReportBtn");
    await expect(copyBtn).toBeVisible();
    await copyBtn.click({ force: true });

    await expect(page.locator(".toast", { hasText: "Đã sao chép báo cáo chẩn đoán" })).toBeVisible();
  });

  test("CRASH-AC-05: Nút Bỏ qua ẩn Crash Analyzer banner", async ({ page }) => {
    await triggerScenario(page, "oom");

    const banner = page.locator("#crashAnalyzerBanner");
    await expect(banner).toBeVisible();

    const ignoreBtn = page.locator("#crashIgnoreBtn");
    await expect(ignoreBtn).toBeVisible();
    await ignoreBtn.click({ force: true });

    await expect(banner).toBeHidden();
  });
});
