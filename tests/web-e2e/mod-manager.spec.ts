import { test, expect, Page } from "@playwright/test";

async function bootAndOpenMods(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
  await page.locator(".instance-card").first().click({ force: true });
  await expect(page.locator("#page-detail")).toBeVisible();
  await expect(page.locator("#tab-mods")).toBeVisible();
}

test.describe("Mod Manager Deep Flow Runtime (mod-manager.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await bootAndOpenMods(page);
  });

  test("MM-AC-01: Click mod row mở Mod Inspector với đầy đủ metadata", async ({ page }) => {
    const sodiumRow = page.locator(".mod-row", { hasText: "Sodium" }).first();
    await expect(sodiumRow).toBeVisible();
    await sodiumRow.click();

    const host = page.locator("#modInspectorHost");
    const drawer = page.locator("#modInspector");
    await expect(host).toBeVisible();
    await expect(host).toHaveClass(/is-open/);
    await expect(drawer).toBeVisible();

    await expect(page.locator("#modInspectorTitle")).toContainText("Sodium");
    await expect(page.locator("#modInspectorVersion")).toBeVisible();
    await expect(page.locator("#modInspectorLoader")).toContainText("Fabric");
    await expect(page.locator("#modInspectorFile")).toContainText(".jar");
    await expect(page.locator("#modInspectorAuthor")).toContainText("jellysquid3");
    await expect(page.locator("#modInspectorDesc")).toBeVisible();
    await expect(page.locator("#modInspectorCategory")).toContainText("Hiệu năng");
    await expect(page.locator("#modInspectorLinks a", { hasText: "Modrinth" })).toBeVisible();
  });

  test("MM-AC-02: Mod thiếu dependency hiển thị badge cảnh báo và nút 1-click resolve", async ({ page }) => {
    const createRow = page.locator(".mod-row", { hasText: "Create" }).first();
    await expect(createRow).toBeVisible();
    await expect(createRow.locator(".mod-badge-warning")).toContainText("Thiếu dependency");

    await createRow.click();
    await expect(page.locator("#modInspectorHost")).toBeVisible();

    await expect(page.locator("#modInspectorDepStatus")).toContainText("Thiếu");
    await expect(page.locator("#modInspectorDeps")).toContainText("Fabric API");
    const resolveBtn = page.locator("#modInspectorResolveDepBtn");
    await expect(resolveBtn).toBeVisible();

    await resolveBtn.click();

    await expect(page.locator(".toast", { hasText: "Đã giải quyết" })).toBeVisible();
    await expect(page.locator("#modInspectorDepStatus")).toContainText("Đầy đủ");
    await expect(createRow.locator(".mod-badge-warning")).toBeHidden();
    await expect(page.locator(".mod-row", { hasText: "Fabric API" })).toBeVisible();
  });

  test("MM-AC-03: Bộ lọc Category lọc danh sách chính xác và cập nhật bộ đếm", async ({ page }) => {
    const totalCount = await page.locator(".mod-row").count();
    expect(totalCount).toBeGreaterThan(3);

    const perfChip = page.locator('#modCategoryChips button[data-mod-category="performance"]');
    await perfChip.click();
    await expect(perfChip).toHaveClass(/is-selected/);
    await expect(page.locator(".mod-row", { hasText: "Sodium" })).toBeVisible();
    await expect(page.locator(".mod-row", { hasText: "Create" })).toBeHidden();
    await expect(page.locator("#modSummary")).toContainText("Hiển thị 2 /");

    const techChip = page.locator('#modCategoryChips button[data-mod-category="technology"]');
    await techChip.click();
    await expect(techChip).toHaveClass(/is-selected/);
    await expect(page.locator(".mod-row", { hasText: "Create" })).toBeVisible();
    await expect(page.locator(".mod-row", { hasText: "Sodium" })).toBeHidden();

    const allChip = page.locator('#modCategoryChips button[data-mod-category="all"]');
    await allChip.click();
    await expect(allChip).toHaveClass(/is-selected/);
    await expect(page.locator(".mod-row")).toHaveCount(totalCount);
  });

  test("MM-AC-04: Tải mod từ Catalog tự động phát hiện dependency và hiển thị dialog gợi ý cài kèm", async ({ page }) => {
    await page.evaluate(() => {
      const s = (window as any).__vazState;
      if (s && s.mods && s.mods["test fabric"]) {
        s.mods["test fabric"] = s.mods["test fabric"].filter(
          (m: any) => !m.file.toLowerCase().includes("sodium")
        );
      }
      (window as any).__vazRenderMods?.();
    });

    await page.locator("#openModBrowserBtn").click();
    await expect(page.locator("#dialogHost")).toBeVisible();

    await page.locator("#mbSearch").fill("sodium-extra");
    const modResult = page.locator('.mod-result[data-slug="sodium-extra"]');
    await expect(modResult).toBeVisible();
    await modResult.locator("button[data-install]").click();

    const confirmDialog = page.locator("#dialogHost", { hasText: "Cài đặt kèm dependencies?" });
    await expect(confirmDialog).toBeVisible();
    await expect(confirmDialog).toContainText("Sodium");

    await confirmDialog.locator("[data-dialog-primary]").click();

    // Chờ quá trình cài hoàn tất (hiện status và pipeline xong)
    await expect(page.locator("#dialogHost #mbStatus")).toContainText("Đã cài Sodium Extra", { timeout: 15_000 });

    // Đóng catalog dialog
    await page.locator("#dialogHost .dialog-footer [data-dialog-close]").click();

    // Cả 2 mod xuất hiện trong danh sách mod
    await expect(page.locator(".mod-row", { hasText: "Sodium Extra" })).toBeVisible();
    const sodiumOnlyRow = page.locator(".mod-row").filter({ hasText: "Sodium" }).filter({ hasNotText: "Extra" });
    await expect(sodiumOnlyRow).toBeVisible();
  });

  test("MM-AC-05: Tắt mod đang là dependency của mod khác hiển thị toast cảnh báo ảnh hưởng", async ({ page }) => {
    const sodiumRow = page.locator(".mod-row").filter({ hasText: "Sodium" }).filter({ hasNotText: "Extra" });
    await expect(sodiumRow).toBeVisible();

    // Click switch để tắt Sodium
    await sodiumRow.locator(".switch").click({ force: true });

    await expect(page.locator(".toast", { hasText: "Cảnh báo: Tắt Sodium" })).toBeVisible();
    await expect(page.locator(".toast", { hasText: "Sodium Extra" })).toBeVisible();
  });

  test("MM-AC-06: Hỗ trợ thêm file mod .jar từ bên ngoài qua input file và kéo thả", async ({ page }) => {
    // 1. Thêm qua file input
    await page.locator("#modFileInput").setInputFiles({
      name: "custom-utility-1.4.2.jar",
      mimeType: "application/java-archive",
      buffer: Buffer.from("dummy jar binary"),
    });

    await expect(page.locator(".toast", { hasText: "custom utility" })).toBeVisible();
    await expect(page.locator(".mod-row", { hasText: "custom-utility-1.4.2.jar" })).toBeVisible();

    // 2. Thêm qua drag & drop event với DataTransfer trong browser
    await page.evaluate(() => {
      const dt = new DataTransfer();
      const file = new File(["dummy jar binary"], "another-addon-2.0.0.jar", {
        type: "application/java-archive",
      });
      dt.items.add(file);
      const dropEvent = new DragEvent("drop", { dataTransfer: dt, bubbles: true, cancelable: true });
      document.getElementById("tab-mods")?.dispatchEvent(dropEvent);
    });

    await expect(page.locator(".toast", { hasText: "another addon" })).toBeVisible();
    await expect(page.locator(".mod-row", { hasText: "another-addon-2.0.0.jar" })).toBeVisible();
  });

  test("MM-AC-07: Responsive không tràn ngang ở 390px/620px, Escape đóng Inspector", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    const sodiumRow = page.locator(".mod-row").filter({ hasText: "Sodium" }).filter({ hasNotText: "Extra" });
    await sodiumRow.click();

    const host = page.locator("#modInspectorHost");
    await expect(host).toHaveClass(/is-open/);

    const overflow390 = await page.evaluate(() => {
      const doc = document.documentElement;
      return doc.scrollWidth <= window.innerWidth + 1;
    });
    expect(overflow390).toBe(true);

    await page.keyboard.press("Escape");
    await expect(host).not.toHaveClass(/is-open/);

    await page.setViewportSize({ width: 620, height: 900 });
    await sodiumRow.click();
    await expect(host).toHaveClass(/is-open/);

    const overflow620 = await page.evaluate(() => {
      const doc = document.documentElement;
      return doc.scrollWidth <= window.innerWidth + 1;
    });
    expect(overflow620).toBe(true);

    await page.locator("#modInspectorClose").click();
    await expect(host).not.toHaveClass(/is-open/);
  });
});
