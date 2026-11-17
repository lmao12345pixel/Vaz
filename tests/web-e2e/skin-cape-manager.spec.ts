import { test, expect, type Page } from "@playwright/test";
import * as fs from "node:fs";
import * as path from "node:path";

async function boot(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
}

async function navigateToSkins(page: Page) {
  const toggle = page.locator("#mobileNavToggle");
  if (await toggle.isVisible()) {
    const isNavOpen = await page.evaluate(() => document.documentElement.classList.contains("nav-open"));
    if (!isNavOpen) {
      await toggle.click({ force: true });
    }
  }
  const navBtn = page.locator('.nav-item[data-screen="skins"]');
  await navBtn.click({ force: true });
  await expect(page.locator("#page-skins")).toBeVisible();
}

test.describe("Skin & Cape Manager Tests (skin-cape-manager.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await boot(page);
  });

  test("SCM-AC-01: Danh sách Skin Presets hiển thị dạng lưới thẻ (.skin-card) với thumbnail texture, tên, kiểu model và badge Đang dùng", async ({ page }) => {
    await navigateToSkins(page);

    const cards = page.locator("#skinGrid .skin-card");
    await expect(cards).toHaveCount(6);

    const firstCard = cards.first();
    await expect(firstCard.locator(".skin-thumb")).toBeVisible();
    await expect(firstCard.locator(".skin-card-name")).toHaveText("Steve");
    await expect(firstCard.locator(".skin-card-model")).toHaveText("Classic");
    await expect(firstCard.locator(".skin-card-badge")).toHaveText("Đang dùng");

    const secondCard = cards.nth(1);
    await expect(secondCard.locator(".skin-card-name")).toHaveText("Alex");
    await expect(secondCard.locator(".skin-card-model")).toHaveText("Slim");
  });

  test("SCM-AC-02: Canvas mô hình 3D (#skinViewer) khởi tạo thành công, hỗ trợ bật/tắt tự động xoay (#skinAutoRotateBtn)", async ({ page }) => {
    await navigateToSkins(page);

    const canvas = page.locator("#skinViewer");
    await expect(canvas).toBeVisible();

    const autoRotateBtn = page.locator("#skinAutoRotateBtn");
    await expect(autoRotateBtn).toBeVisible();

    const wasActive = await autoRotateBtn.evaluate((el) => el.classList.contains("is-active"));
    await autoRotateBtn.click();
    const isNowActive = await autoRotateBtn.evaluate((el) => el.classList.contains("is-active"));
    expect(isNowActive).toBe(!wasActive);
  });

  test("SCM-AC-03: Bộ điều khiển hoạt ảnh 3D (#skinAnimGroup): hỗ trợ các tư thế (walk, fly, sneak, wave, bow), cập nhật class is-active và hiển thị Elytra khi bay", async ({ page }) => {
    await navigateToSkins(page);

    const animGroup = page.locator("#skinAnimGroup");
    await expect(animGroup).toBeVisible();

    const anims = ["walk", "fly", "sneak", "wave", "bow"];
    for (const anim of anims) {
      const btn = animGroup.locator(`button[data-skin-anim="${anim}"]`);
      await expect(btn).toBeVisible();
      await btn.click();
      await expect(btn).toHaveClass(/is-active/);

      if (anim === "fly") {
        await page.waitForTimeout(500);
        const elytraVisible = await page.evaluate(() => {
          const sv = (window as any).skinViewer;
          return sv?.playerObject?.backEquipment === "elytra" && sv?.playerObject?.elytra?.visible === true;
        });
        expect(elytraVisible).toBe(true);
      }
    }
  });

  test("SCM-AC-04: Nút chuyển đổi Model tay (#skinModelToggleBtn): chuyển đổi qua lại giữa Classic và Slim", async ({ page }) => {
    await navigateToSkins(page);

    const toggleBtn = page.locator("#skinModelToggleBtn");
    const modelText = page.locator("#skinPreviewModel");
    await expect(toggleBtn).toBeVisible();

    const initial = await modelText.textContent();
    await toggleBtn.click();
    const changed = await modelText.textContent();
    expect(changed).not.toBe(initial);

    await toggleBtn.click();
    const restored = await modelText.textContent();
    expect(restored).toBe(initial);
  });

  test("SCM-AC-05: Nút Áp dụng skin (#applySkinBtn) lưu skin vào tài khoản đang chọn kèm toast thông báo và cập nhật badge; nút Mặc định (#resetSkinBtn) khôi phục skin gốc", async ({ page }) => {
    await navigateToSkins(page);

    const alexCard = page.locator('#skinGrid .skin-card[data-skin-id="alex"]');
    await alexCard.click();

    const applyBtn = page.locator("#applySkinBtn");
    await expect(applyBtn).toBeVisible();
    await applyBtn.click();

    const toast = page.locator(".toast").last();
    await expect(toast).toBeVisible();
    await expect(toast).toContainText("Đã đổi skin");
    await expect(alexCard.locator(".skin-card-badge")).toHaveText("Đang dùng");

    const resetBtn = page.locator("#resetSkinBtn");
    await expect(resetBtn).toBeVisible();
    await resetBtn.click();

    const steveCard = page.locator('#skinGrid .skin-card[data-skin-id="steve"]');
    await expect(steveCard.locator(".skin-card-badge")).toHaveText("Đang dùng");
  });

  test("SCM-AC-06: Danh sách Áo choàng (#skinCapeBox): ẩn với tài khoản Offline và tài khoản không có cape; hiển thị đầy đủ cape thuộc sở hữu khi chọn tài khoản Microsoft có cape", async ({ page }) => {
    await navigateToSkins(page);

    // 1. Tài khoản mặc định là Steve (Offline) -> Không có cape -> #skinCapeBox bị ẩn hoàn toàn
    const capeBox = page.locator("#skinCapeBox");
    await expect(capeBox).toBeHidden();

    // 2. Chuyển sang tài khoản Alex (Microsoft / Premium có cape) -> #skinCapeBox hiển thị
    const accountSelect = page.locator("#skinAccountSelect");
    await accountSelect.selectOption("acc-alex");
    await expect(capeBox).toBeVisible();

    const capeItems = page.locator("#skinCapeList .skin-cape-item");
    const count = await capeItems.count();
    expect(count).toBeGreaterThanOrEqual(4);

    const vanillaCape = page.locator('#skinCapeList .skin-cape-item[data-cape-id="vanilla"]');
    await expect(vanillaCape).toBeVisible();
    await vanillaCape.click();

    const capeBadge = page.locator("#skinCapeBadge");
    await expect(capeBadge).toHaveText("Vanilla");

    const noneCape = page.locator('#skinCapeList .skin-cape-item[data-cape-id="none"]');
    await noneCape.click();
    await expect(capeBadge).toHaveText("Không dùng");

    // 3. Đổi lại về tài khoản Steve (Offline) -> #skinCapeBox lại bị ẩn
    await accountSelect.selectOption("acc-steve");
    await expect(capeBox).toBeHidden();
  });

  test("SCM-AC-07: Thêm skin tùy chỉnh từ file PNG qua input file (#skinFileInput), đặt tên, thêm vào thư viện; hỗ trợ xóa custom skin", async ({ page }) => {
    await navigateToSkins(page);

    const skinFilePath = path.resolve(__dirname, "../../docs/design-preview/assets/skins/steve.png");
    const skinBuffer = fs.readFileSync(skinFilePath);

    const fileInput = page.locator("#skinFileInput");
    await fileInput.setInputFiles({
      name: "custom-warrior.png",
      mimeType: "image/png",
      buffer: skinBuffer,
    });

    const dialog = page.locator("#dialogHost");
    await expect(dialog).toBeVisible();
    await expect(dialog.locator(".dialog-title")).toContainText("Thêm skin tùy chỉnh");

    const nameInput = page.locator("#customSkinNameInput");
    await nameInput.fill("Warrior_Custom");

    const primaryBtn = dialog.locator("[data-dialog-primary]");
    await primaryBtn.click();

    const customCard = page.locator('#skinGrid .skin-card:has-text("Warrior_Custom")');
    await expect(customCard).toBeVisible();
    await expect(customCard.locator(".skin-card-model")).toContainText("Custom");

    const deleteBtn = customCard.locator(".skin-card-delete");
    await expect(deleteBtn).toBeVisible();
    await deleteBtn.click();

    await expect(customCard).not.toBeVisible();
    await expect(page.locator(".toast").last()).toContainText("Đã xóa skin tùy chỉnh");
  });

  test("SCM-AC-08: Tải skin theo username Minecraft (#skinGrabInput, #skinGrabBtn): tải skin thành công từ tên người chơi, thêm vào danh sách", async ({ page }) => {
    await navigateToSkins(page);

    const grabInput = page.locator("#skinGrabInput");
    const grabBtn = page.locator("#skinGrabBtn");

    await grabInput.fill("Technoblade");
    await grabBtn.click();

    const toast = page.locator(".toast").last();
    await expect(toast).toBeVisible();
    await expect(toast).toContainText("Đã lấy skin của Technoblade");

    const technoCard = page.locator('#skinGrid .skin-card:has-text("Technoblade")');
    await expect(technoCard).toBeVisible();
  });

  test("SCM-AC-09: Tải lên Cape tùy chỉnh (#capeFileInput, #capeUploadBtn): chọn file cape PNG, thêm vào danh sách cape", async ({ page }) => {
    await navigateToSkins(page);

    // Chuyển sang tài khoản Alex (Microsoft) để mở phần quản lý Cape
    const accountSelect = page.locator("#skinAccountSelect");
    await accountSelect.selectOption("acc-alex");
    await expect(page.locator("#skinCapeBox")).toBeVisible();

    const capeFilePath = path.resolve(__dirname, "../../docs/design-preview/assets/capes/migrator.png");
    const capeBuffer = fs.readFileSync(capeFilePath);

    const fileInput = page.locator("#capeFileInput");
    await fileInput.setInputFiles({
      name: "dragon-cape.png",
      mimeType: "image/png",
      buffer: capeBuffer,
    });

    const toast = page.locator(".toast").last();
    await expect(toast).toBeVisible();
    await expect(toast).toContainText("Đã thêm cape");

    const customCapeItem = page.locator('#skinCapeList .skin-cape-item:has-text("dragon-cape")');
    await expect(customCapeItem).toBeVisible();
  });

  test("SCM-AC-10: Responsive (390px, 620px) không tràn ngang; :focus-visible rõ ràng; Reduced motion duration <= 0.05s", async ({ page }) => {
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 800 });
      await navigateToSkins(page);

      const overflow = await page.evaluate(() => {
        const docEl = document.documentElement;
        return docEl.scrollWidth > docEl.clientWidth + 1;
      });
      expect(overflow, `Overflow detected at ${width}px`).toBe(false);
    }

    await page.setViewportSize({ width: 1280, height: 800 });
    await navigateToSkins(page);

    await page.keyboard.press("Tab");
    const randomBtn = page.locator("#randomSkinBtn");
    await randomBtn.focus();
    const outline = await randomBtn.evaluate((el) => {
      const cs = window.getComputedStyle(el);
      return {
        width: parseFloat(cs.outlineWidth) || 0,
        style: cs.outlineStyle,
      };
    });
    expect(outline.width).toBeGreaterThanOrEqual(2);
    expect(outline.style).not.toBe("none");

    await page.emulateMedia({ reducedMotion: "reduce" });
    const duration = await randomBtn.evaluate((el) => {
      const d = window.getComputedStyle(el).transitionDuration;
      return parseFloat(d) || 0;
    });
    expect(duration).toBeLessThanOrEqual(0.05);
  });

  test("SCM-AC-12: CSS tuân thủ nghiêm ngặt 0 transition: all", async () => {
    const cssPath = path.resolve(__dirname, "../../docs/design-preview/styles.css");
    const cssContent = fs.readFileSync(cssPath, "utf-8");
    const matches = cssContent.match(/transition(-property)?\s*:\s*all/gi);
    expect(matches).toBeNull();
  });
});
