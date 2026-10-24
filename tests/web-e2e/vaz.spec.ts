import { test, expect } from "@playwright/test";

test.describe("V.A.Z web prototype runtime", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/");
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  });

  test("sidebar mở Settings và ẩn trang Instances", async ({ page }) => {
    await expect(page.locator("#page-instances")).toBeVisible();

    await page.locator('button.nav-item[data-screen="settings"]').click();

    await expect(page.locator("#page-settings")).toBeVisible();
    await expect(page.locator("#page-instances")).toBeHidden();
    await expect(page.getByRole("heading", { name: "Cài đặt" })).toBeVisible();
    await expect(page.locator("#globalRam")).toBeVisible();
  });

  test("Settings lưu RAM và giữ giá trị sau reload", async ({ page }) => {
    await page.locator('button.nav-item[data-screen="settings"]').click();

    await page.locator("#globalRamValue").fill("8192");
    await expect(page.locator("#globalRam")).toHaveValue("8192");

    await page.locator("#saveGlobalSettingsBtn").click();
    await expect(page.locator("#globalSettingsStatus")).toHaveText("Đã lưu cài đặt chung.");

    await page.reload();
    await page.locator('button.nav-item[data-screen="settings"]').click();
    await expect(page.locator("#globalRamValue")).toHaveValue("8192");
    await expect(page.locator("#globalRam")).toHaveValue("8192");
  });

  test("Library hiển thị empty state khi không có instance", async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem("vazer-demo-v1", JSON.stringify({ instances: [] }));
    });
    await page.reload();

    await expect(page.locator("#page-instances")).toBeVisible();
    await expect(page.locator("#instancesEmpty")).toBeVisible();
    await expect(page.locator("#instancesEmptyTitle")).toHaveText("Chưa có instance nào.");
    await expect(page.locator("#instancesEmptyHint")).toHaveText("Bấm + để tạo instance mới.");
  });
});
