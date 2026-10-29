import { test, expect, Page } from "@playwright/test";

async function bootAndOpenDetail(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
  await page.locator(".instance-card").first().click({ force: true });
  await expect(page.locator("#page-detail")).toBeVisible();
}

test.describe("Resource Packs & Shaders Runtime Tests (resource-packs-shaders.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await bootAndOpenDetail(page);
  });

  test("RPS-AC-01: Tab Resource Packs trong Instance Detail hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt", async ({ page }) => {
    const tabBtn = page.locator('.tab[data-tab="resourcepacks"]');
    await expect(tabBtn).toBeVisible();
    await tabBtn.click();

    const panel = page.locator("#tab-resourcepacks");
    await expect(panel).toBeVisible();

    const cards = page.locator(".rp-card");
    await expect(cards).toHaveCount(4);

    const firstCard = cards.first();
    await expect(firstCard.locator(".rp-name")).toContainText("Faithful 32x");
    await expect(firstCard.locator(".rp-icon")).toBeVisible();
    await expect(firstCard.locator(".rp-res")).toContainText("32x");
    await expect(firstCard.locator(".rp-size")).toBeVisible();
    await expect(firstCard.locator(".rp-toggle input")).toBeChecked();

    // Toggle switch tắt
    await firstCard.locator(".rp-toggle .track").click();
    await expect(firstCard).toHaveClass(/is-disabled/);
    await expect(firstCard.locator(".rp-toggle input")).not.toBeChecked();

    // Toggle switch bật lại
    await firstCard.locator(".rp-toggle .track").click();
    await expect(firstCard).toHaveClass(/is-enabled/);
    await expect(firstCard.locator(".rp-toggle input")).toBeChecked();
  });

  test("RPS-AC-02: Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Lên/Xuống (Move Up/Down); pack phía trên có độ ưu tiên cao hơn và được lưu vào state", async ({ page }) => {
    await page.locator('.tab[data-tab="resourcepacks"]').click();
    await expect(page.locator("#tab-resourcepacks")).toBeVisible();

    const firstTitle = await page.locator(".rp-card .rp-name").nth(0).innerText();
    const secondTitle = await page.locator(".rp-card .rp-name").nth(1).innerText();

    expect(firstTitle).toBe("Faithful 32x");
    expect(secondTitle).toBe("Bare Bones");

    // Click Move Up trên pack thứ hai
    const moveUpBtn = page.locator(".rp-card").nth(1).locator(".rp-move-up");
    await moveUpBtn.click();

    // Vị trí đổi lên đầu danh sách
    await expect(page.locator(".rp-card .rp-name").nth(0)).toHaveText(secondTitle);
    await expect(page.locator(".rp-card .rp-name").nth(1)).toHaveText(firstTitle);

    // Reload trang kiểm tra persistence
    await page.reload();
    await expect(page.locator("#page-instances")).toBeVisible();
    await page.locator(".instance-card").first().click({ force: true });
    await expect(page.locator("#page-detail")).toBeVisible();
    await page.locator('.tab[data-tab="resourcepacks"]').click();
    await expect(page.locator(".rp-card .rp-name").nth(0)).toHaveText(secondTitle);
    await expect(page.locator(".rp-card .rp-name").nth(1)).toHaveText(firstTitle);
  });

  test("RPS-AC-03: Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu pack format không khớp phiên bản MC của instance", async ({ page }) => {
    await page.locator('.tab[data-tab="resourcepacks"]').click();
    await expect(page.locator("#tab-resourcepacks")).toBeVisible();

    // Thêm pack cũ MC 1.20 vào instance 1.21.1
    await page.locator("#rpFileInput").setInputFiles({
      name: "retro-textures-mc1.20.zip",
      mimeType: "application/zip",
      buffer: Buffer.from("dummy zip"),
    });

    const newCard = page.locator(".rp-card").first();
    await expect(newCard.locator(".rp-name")).toContainText("retro textures mc1.20");

    const warningBadge = newCard.locator(".badge.is-warning");
    await expect(warningBadge).toBeVisible();
    await expect(warningBadge).toContainText("Không tương thích");
  });

  test("RPS-AC-04: Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc 'Tắt shader', cập nhật ảnh preview shader", async ({ page }) => {
    const tabBtn = page.locator('.tab[data-tab="shaders"]');
    await expect(tabBtn).toBeVisible();
    await tabBtn.click();

    const panel = page.locator("#tab-shaders");
    await expect(panel).toBeVisible();

    // Chọn BSL Shaders
    const bslCard = page.locator('#shaderCardGrid .shader-card[data-shader-id="bsl-shaders"]');
    await expect(bslCard).toBeVisible();
    await bslCard.click();

    await expect(bslCard).toHaveClass(/is-active/);
    await expect(bslCard.locator(".badge.is-active-badge")).toContainText("Đang dùng");
    await expect(page.locator("#shaderActiveSummary")).toContainText("BSL Shaders");

    const previewSrc = await page.locator("#shaderPreviewImg").getAttribute("src");
    expect(previewSrc).toContain("bsl-shaders");
    await expect(page.locator("#shaderPreviewBadge")).toHaveText("BSL Shaders");

    // Chọn "Tắt shader"
    const offCard = page.locator('#shaderCardGrid .shader-card[data-shader-id="off"]');
    await expect(offCard).toBeVisible();
    await offCard.click();

    await expect(offCard).toHaveClass(/is-active/);
    await expect(offCard.locator(".badge.is-active-badge")).toContainText("Đang dùng");
    await expect(page.locator("#shaderActiveSummary")).toContainText("Tắt");
    await expect(page.locator("#shaderPreviewBadge")).toHaveText("Đã tắt shader");
  });

  test("RPS-AC-05: Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì và phản hồi thị giác", async ({ page }) => {
    await page.locator('.tab[data-tab="shaders"]').click();
    await expect(page.locator("#tab-shaders")).toBeVisible();

    const ultraChip = page.locator('#shaderProfileChips .profile-chip[data-profile="ultra"]');
    await expect(ultraChip).toBeVisible();
    await ultraChip.click();

    await expect(ultraChip).toHaveClass(/is-selected/);
    await expect(ultraChip).toHaveAttribute("aria-pressed", "true");
    await expect(page.locator("#shaderProfileLabel")).toHaveText("Ultra");
    await expect(page.locator(".toast")).toContainText("ULTRA");

    // Chuyển sang Low
    const lowChip = page.locator('#shaderProfileChips .profile-chip[data-profile="low"]');
    await lowChip.click();
    await expect(lowChip).toHaveClass(/is-selected/);
    await expect(page.locator("#shaderProfileLabel")).toHaveText("Low");
  });

  test("RPS-AC-06: Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris; sau khi cài trạng thái chuyển sang Sẵn sàng", async ({ page }) => {
    // Quay lại danh sách instances và mở Vanilla 1.20.1 (chưa có Iris)
    await page.locator("#btnBackToInstances").click();
    await expect(page.locator("#page-instances")).toBeVisible();

    const vanillaCard = page.locator(".instance-card", { hasText: "Vanilla 1.20.1" });
    await expect(vanillaCard).toBeVisible();
    await vanillaCard.click();

    await expect(page.locator("#page-detail")).toBeVisible();
    await page.locator('.tab[data-tab="shaders"]').click();
    await expect(page.locator("#tab-shaders")).toBeVisible();

    // Banner cảnh báo thiếu loader
    const warningBanner = page.locator("#shaderWarningBanner");
    await expect(warningBanner).toBeVisible();
    await expect(warningBanner).toContainText("Thiếu Shader Loader");

    const installIrisBtn = page.locator("#btnInstallIris");
    await expect(installIrisBtn).toBeVisible();
    await installIrisBtn.click();

    // Sau khi cài chuyển sang Sẵn sàng hoạt động
    const readyBanner = page.locator("#shaderReadyBanner");
    await expect(readyBanner).toBeVisible();
    await expect(readyBanner).toContainText("Sẵn sàng hoạt động (Iris)");

    // Kiểm tra tab Mods đã có Iris Shaders
    await page.locator('.tab[data-tab="mods"]').click();
    await expect(page.locator(".mod-row", { hasText: "Iris Shaders" })).toBeVisible();
  });

  test("RPS-AC-07: Hỗ trợ thêm file pack .zip từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders)", async ({ page }) => {
    // 1. Resource Packs: Thêm qua file input
    await page.locator('.tab[data-tab="resourcepacks"]').click();
    await expect(page.locator("#tab-resourcepacks")).toBeVisible();

    await page.locator("#rpFileInput").setInputFiles({
      name: "cyber-punk-pack.zip",
      mimeType: "application/zip",
      buffer: Buffer.from("dummy cyber punk"),
    });

    await expect(page.locator(".toast", { hasText: "Đã thêm" })).toBeVisible();
    await expect(page.locator(".rp-card", { hasText: "cyber punk pack" })).toBeVisible();

    // 2. Shaders: Thêm qua kéo thả (Drag & Drop)
    await page.locator('.tab[data-tab="shaders"]').click();
    await expect(page.locator("#tab-shaders")).toBeVisible();

    await page.evaluate(() => {
      const dt = new DataTransfer();
      const file = new File(["dummy shader zip"], "astral-shaderpack.zip", { type: "application/zip" });
      dt.items.add(file);
      const target = document.querySelector("#shaderDropZone");
      target?.dispatchEvent(new DragEvent("drop", { dataTransfer: dt, bubbles: true, cancelable: true }));
    });

    await expect(page.locator(".toast", { hasText: "Đã thêm shaderpack" })).toBeVisible();
    await expect(page.locator('#shaderCardGrid .shader-card', { hasText: "astral shaderpack" })).toBeVisible();
  });

  test("RPS-AC-08: Responsive (390px, 620px) không tràn ngang; :focus-visible rõ ràng; Reduced motion duration <= 0.05s", async ({ page }) => {
    await page.locator('.tab[data-tab="resourcepacks"]').click();
    await expect(page.locator("#tab-resourcepacks")).toBeVisible();

    // Kiểm tra không tràn ngang ở 620px và 390px
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 844 });
      const overflowRp = await page.evaluate(() => {
        const doc = document.documentElement;
        return doc.scrollWidth <= window.innerWidth + 1;
      });
      expect(overflowRp, `Resource Packs tràn ngang ở ${width}px`).toBe(true);

      // Chuyển sang Shaders tab
      await page.locator('.tab[data-tab="shaders"]').click();
      await expect(page.locator("#tab-shaders")).toBeVisible();

      const overflowShader = await page.evaluate(() => {
        const doc = document.documentElement;
        return doc.scrollWidth <= window.innerWidth + 1;
      });
      expect(overflowShader, `Shaders tràn ngang ở ${width}px`).toBe(true);

      // Quay lại tab resourcepacks cho vòng lặp tiếp
      await page.locator('.tab[data-tab="resourcepacks"]').click();
    }

    // Kiểm tra :focus-visible rõ ràng
    const folderBtn = page.locator("#openResourcePacksFolderBtn");
    await folderBtn.click({ force: true });
    await page.keyboard.press("Tab");
    await page.keyboard.press("Shift+Tab");
    const focusStyles = await page.evaluate(() => {
      const active = document.activeElement as HTMLElement | null;
      if (!active) return null;
      const cs = window.getComputedStyle(active);
      return {
        id: active.id,
        focusVisible: active.matches(":focus-visible"),
        outlineStyle: cs.outlineStyle,
        outlineWidth: parseFloat(cs.outlineWidth) || 0,
        outlineOffset: parseFloat(cs.outlineOffset) || 0,
      };
    });
    expect(focusStyles?.id).toBe("openResourcePacksFolderBtn");
    expect(focusStyles?.focusVisible).toBe(true);
    expect(focusStyles?.outlineStyle).not.toBe("none");
    expect(focusStyles?.outlineWidth).toBeGreaterThanOrEqual(2);
    expect(focusStyles?.outlineOffset).toBeGreaterThanOrEqual(1);

    // Kiểm tra Reduced Motion
    await page.emulateMedia({ reducedMotion: "reduce" });
    const cardTransition = await page.locator(".rp-card").first().evaluate((el) => {
      const cs = window.getComputedStyle(el);
      const durations = cs.transitionDuration.split(",").map((s) => parseFloat(s) || 0);
      return Math.max(...durations);
    });
    expect(cardTransition).toBeLessThanOrEqual(0.05);
  });
});
