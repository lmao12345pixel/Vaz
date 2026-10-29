import { test, expect, type Page } from "@playwright/test";

/**
 * Runtime test suite cho feature Accounts & Microsoft Device Code Authentication
 * Contract: docs/contracts/accounts-auth.contract.md (AUTH-AC-01 ... AUTH-AC-08, AUTH-AC-10)
 */

async function bootAccountsPage(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await page.locator('.nav-item[data-screen="accounts"]').click({ force: true });
  await expect(page.locator("#page-accounts")).toBeVisible();
}

test.describe("Accounts & Microsoft Device Code Flow (Desktop & Mobile)", () => {
  test.beforeEach(async ({ page }) => {
    await bootAccountsPage(page);
  });

  test("AUTH-AC-01: Click Add Microsoft mở Modal Device Code Flow với mã, nút sao chép, timer và link", async ({ page }) => {
    const modalHost = page.locator("#msAuthModalHost");
    await expect(modalHost).toBeHidden();

    await page.locator("#addMicrosoftBtn").click();
    await expect(modalHost).toBeVisible();

    const userCode = page.locator("#msUserCode");
    await expect(userCode).toBeVisible();
    await expect(userCode).toHaveText(/^[A-Z0-9]{4}-[A-Z0-9]{4}$/);

    const copyBtn = page.locator("#msCopyCodeBtn");
    await expect(copyBtn).toBeVisible();

    const timer = page.locator("#msCountdownTimer");
    await expect(timer).toBeVisible();
    await expect(timer).toHaveText(/^\d{2}:\d{2}$/);

    const openLinkBtn = page.locator("#msOpenLinkBtn");
    await expect(openLinkBtn).toBeVisible();
    await expect(openLinkBtn).toHaveAttribute("href", "https://microsoft.com/link");
    await expect(openLinkBtn).toHaveAttribute("target", "_blank");
  });

  test("AUTH-AC-02: Sao chép mã hiển thị toast; mô phỏng hoàn tất xác thực tạo tài khoản Microsoft mới và đóng modal", async ({ page }) => {
    await page.locator("#addMicrosoftBtn").click();
    const modalHost = page.locator("#msAuthModalHost");
    await expect(modalHost).toBeVisible();

    await page.locator("#msCopyCodeBtn").click();
    const toast = page.locator(".toast", { hasText: "Đã sao chép mã kết nối" });
    await expect(toast).toBeVisible();

    await page.locator("#msSimulateSuccessBtn").click();
    await expect(modalHost).toBeHidden();

    const newMsCard = page.locator(".account-card").filter({ hasText: "MCPlayer" });
    await expect(newMsCard).toBeVisible();
    await expect(newMsCard.locator(".account-badge-kind")).toHaveText("Microsoft");
  });

  test("AUTH-AC-03: Modal Offline có validation tên Minecraft (3-16 ký tự, ký tự đặc biệt, trùng lặp) và tạo thành công", async ({ page }) => {
    const offlineModal = page.locator("#offlineModalHost");
    await expect(offlineModal).toBeHidden();

    await page.locator("#addOfflineBtn").click();
    await expect(offlineModal).toBeVisible();

    const usernameInput = page.locator("#offlineUsernameInput");
    const errorBox = page.locator("#offlineUsernameError");
    const submitBtn = page.locator("#offlineSubmitBtn");

    // Lỗi: rỗng
    await submitBtn.click();
    await expect(errorBox).toBeVisible();
    await expect(errorBox).toContainText("Tên người chơi không được để trống");

    // Lỗi: dưới 3 ký tự
    await usernameInput.fill("ab");
    await submitBtn.click();
    await expect(errorBox).toBeVisible();
    await expect(errorBox).toContainText("từ 3 đến 16 ký tự");

    // Lỗi: ký tự không hợp lệ
    await usernameInput.fill("Player@123");
    await submitBtn.click();
    await expect(errorBox).toBeVisible();
    await expect(errorBox).toContainText("chỉ được chứa chữ cái, số và dấu gạch dưới");

    // Lỗi: trùng tên đã có (Steve)
    await usernameInput.fill("Steve");
    await submitBtn.click();
    await expect(errorBox).toBeVisible();
    await expect(errorBox).toContainText("Tài khoản này đã tồn tại");

    // Hợp lệ: tạo tài khoản mới
    await usernameInput.fill("Miner_Pro26");
    await submitBtn.click();
    await expect(offlineModal).toBeHidden();

    const newCard = page.locator(".account-card").filter({ hasText: "Miner_Pro26" });
    await expect(newCard).toBeVisible();
    await expect(newCard.locator(".account-badge-kind")).toHaveText("Offline");
  });

  test("AUTH-AC-04: Thẻ tài khoản hiển thị đầy đủ avatar face, badge loại, UUID, token status và Cape chip", async ({ page }) => {
    const cards = page.locator(".account-card");
    await expect(cards).toHaveCount(2);

    for (let i = 0; i < 2; i++) {
      const card = cards.nth(i);
      await expect(card.locator(".avatar .avatar-face")).toBeVisible();
      await expect(card.locator(".account-badge-kind")).toBeVisible();
      await expect(card.locator(".account-uuid")).toBeVisible();
      await expect(card.locator(".account-token-status")).toBeVisible();
      await expect(card.locator(".account-cape-chip")).toBeVisible();
    }
  });

  test("AUTH-AC-05: Chuyển đổi tài khoản Active cập nhật badge trên thẻ và đồng bộ sang #userBtn", async ({ page }) => {
    const steveCard = page.locator('.account-card[data-account="acc-steve"]');
    const alexCard = page.locator('.account-card[data-account="acc-alex"]');

    await expect(steveCard.locator(".active-mark")).toBeVisible();
    await expect(alexCard.locator(".active-mark")).toBeHidden();

    const userBtn = page.locator("#userBtn");
    await expect(userBtn).toContainText("Steve");

    // Đổi active sang Alex
    await alexCard.locator('button[data-acc-action="activate"]').click();

    await expect(alexCard.locator(".active-mark")).toBeVisible();
    await expect(steveCard.locator(".active-mark")).toBeHidden();
    await expect(userBtn).toContainText("Alex");
  });

  test("AUTH-AC-06: Nút Refresh token trên tài khoản Microsoft mô phỏng làm mới token thành công và cập nhật trạng thái", async ({ page }) => {
    const alexCard = page.locator('.account-card[data-account="acc-alex"]');
    const refreshBtn = alexCard.locator('button[data-acc-action="refresh"]');
    await expect(refreshBtn).toBeVisible();

    await refreshBtn.click();

    const toast = page.locator(".toast", { hasText: "Đã làm mới token cho \"Alex\"" });
    await expect(toast).toBeVisible();
    await expect(alexCard.locator(".account-token-status")).toHaveText("Token: Hợp lệ (14 ngày)");
  });

  test("AUTH-AC-07: Xóa tài khoản yêu cầu xác nhận; xóa tài khoản active tự động chuyển active sang tài khoản khác", async ({ page }) => {
    const steveCard = page.locator('.account-card[data-account="acc-steve"]');
    await expect(steveCard.locator(".active-mark")).toBeVisible();

    await steveCard.locator('button[data-acc-action="remove"]').click();

    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost.locator(".dialog-title")).toContainText('Xóa tài khoản "Steve"?');

    await dialogHost.locator("button", { hasText: "Xóa" }).click();
    await expect(dialogHost).toBeHidden();

    await expect(page.locator('.account-card[data-account="acc-steve"]')).toBeHidden();

    const alexCard = page.locator('.account-card[data-account="acc-alex"]');
    await expect(alexCard).toBeVisible();
    await expect(alexCard.locator(".active-mark")).toBeVisible();
    await expect(page.locator("#userBtn")).toContainText("Alex");
  });

  test("AUTH-AC-08: Responsive (390px, 620px), phím Escape đóng modal, focus ring >= 2px, reduced motion <= 0.05s", async ({ page }) => {
    // 1. Responsive checks
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 800 });
      const noOverflow = await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1);
      expect(noOverflow, `Tràn ngang tại viewport width ${width}px`).toBe(true);
    }

    // 2. Escape đóng modals
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.locator("#addMicrosoftBtn").click();
    const msModal = page.locator("#msAuthModalHost");
    await expect(msModal).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(msModal).toBeHidden();

    await page.locator("#addOfflineBtn").click();
    const offModal = page.locator("#offlineModalHost");
    await expect(offModal).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(offModal).toBeHidden();

    // 3. Focus visible ring on controls
    await page.locator("#addMicrosoftBtn").click();
    await page.locator("#msCopyCodeBtn").focus();
    const focusRing = await page.evaluate(() => {
      const el = document.querySelector("#msCopyCodeBtn");
      if (!el) return null;
      const cs = getComputedStyle(el);
      return {
        outlineStyle: cs.outlineStyle,
        outlineWidth: parseFloat(cs.outlineWidth) || 0,
        outlineOffset: parseFloat(cs.outlineOffset) || 0,
      };
    });
    expect(focusRing?.outlineStyle).not.toBe("none");
    expect(focusRing?.outlineWidth).toBeGreaterThanOrEqual(2);
    expect(focusRing?.outlineOffset).toBeGreaterThanOrEqual(1);
    await page.keyboard.press("Escape");

    // 4. Reduced motion duration
    await page.emulateMedia({ reducedMotion: "reduce" });
    const durations = await page.evaluate(() => {
      const modal = document.querySelector(".auth-modal-card");
      const card = document.querySelector(".account-card");
      const getDur = (el: Element | null) => {
        if (!el) return 0;
        const cs = getComputedStyle(el);
        return Math.max(...cs.transitionDuration.split(",").map((d) => parseFloat(d) || 0));
      };
      return {
        modalDur: getDur(modal),
        cardDur: getDur(card),
      };
    });
    expect(durations.modalDur).toBeLessThanOrEqual(0.05);
    expect(durations.cardDur).toBeLessThanOrEqual(0.05);
  });
});
