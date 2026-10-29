import { test, expect, Page } from "@playwright/test";

async function boot(page: Page) {
  await page.goto("/");
  await page.evaluate(() => {
    localStorage.clear();
  });
  await page.reload();
  await page.waitForSelector(".nav-item", { timeout: 10000 });
}

async function navigateToShop(page: Page) {
  const toggle = page.locator("#mobileNavToggle");
  if (await toggle.isVisible()) {
    await toggle.click();
    await page.waitForTimeout(200);
  }
  const shopNav = page.locator('.nav-item[data-screen="shop"]');
  await shopNav.click();
  await expect(page.locator("#page-shop")).toBeVisible({ timeout: 5000 });
}

test.describe("Cosmetics & Capes Store (cosmetics-shop)", () => {
  test.beforeEach(async ({ page }) => {
    await boot(page);
    await navigateToShop(page);
  });

  test("CS-AC-01: Cards display thumb/icon, title, category, rarity badge, price, and actions", async ({ page }) => {
    const cards = page.locator("#shopGrid .shop-card");
    const count = await cards.count();
    expect(count).toBeGreaterThanOrEqual(10);

    const firstCard = cards.first();
    await expect(firstCard.locator(".shop-card-icon")).toBeVisible();
    await expect(firstCard.locator(".shop-card-title")).toBeVisible();
    await expect(firstCard.locator(".shop-card-category")).toBeVisible();
    await expect(firstCard.locator(".shop-rarity-badge")).toBeVisible();
    await expect(firstCard.locator(".shop-card-actions button")).toHaveCount(2); // Try + Buy/Equip
  });

  test("CS-AC-02: Category chips and sort filter list and update counter", async ({ page }) => {
    const countPill = page.locator("#shopCount");
    const initialText = await countPill.textContent();

    // Click Capes chip
    const capesChip = page.locator('#shopCategoryChips button[data-shop-cat="capes"]');
    await capesChip.click();
    await expect(capesChip).toHaveClass(/is-active/);

    const capesCount = await page.locator("#shopGrid .shop-card").count();
    expect(capesCount).toBe(4);
    await expect(countPill).toContainText("4 vật phẩm");

    // Click Wings chip
    const wingsChip = page.locator('#shopCategoryChips button[data-shop-cat="wings"]');
    await wingsChip.click();
    await expect(wingsChip).toHaveClass(/is-active/);
    const wingsCount = await page.locator("#shopGrid .shop-card").count();
    expect(wingsCount).toBe(3);
    await expect(countPill).toContainText("3 vật phẩm");

    // Sort by price-asc
    const sortSelect = page.locator("#shopSortSelect");
    await sortSelect.selectOption("price-asc");

    // Return to All
    const allChip = page.locator('#shopCategoryChips button[data-shop-cat="all"]');
    await allChip.click();
    await expect(allChip).toHaveClass(/is-active/);
    const allCount = await page.locator("#shopGrid .shop-card").count();
    expect(allCount).toBeGreaterThanOrEqual(10);
  });

  test("CS-AC-03: Search query filters items, shows empty state if no matches", async ({ page }) => {
    const searchInput = page.locator("#shopSearch");
    await searchInput.fill("Ender Dragon");

    const cards = page.locator("#shopGrid .shop-card");
    await expect(cards).toHaveCount(1);
    await expect(cards.first().locator(".shop-card-title")).toContainText("Ender Dragon Wings");

    // Search nonexistent keyword
    await searchInput.fill("xyznonexistentitem12345");
    await expect(page.locator("#shopEmptyState")).toBeVisible();
    await expect(page.locator("#shopGrid")).toBeHidden();

    // Clear search
    await searchInput.fill("");
    await expect(page.locator("#shopEmptyState")).toBeHidden();
    await expect(page.locator("#shopGrid")).toBeVisible();
  });

  test("CS-AC-04: Live 3D Try-On modal opens with 3D canvas and closes on button or Escape", async ({ page }) => {
    const modalHost = page.locator("#shopTryOnModalHost");
    await expect(modalHost).toBeHidden();

    // Click Try on the second card
    const tryBtn = page.locator("#shopGrid .shop-card .shop-tryon-btn").nth(1);
    await tryBtn.click();

    await expect(modalHost).toBeVisible();
    await expect(page.locator("#shopTryOnTitle")).not.toBeEmpty();
    await expect(page.locator("#shopViewer")).toBeVisible();

    // Close with Close button in header
    const closeBtn = page.locator("#shopTryOnCloseBtn");
    await closeBtn.click();
    await expect(modalHost).toBeHidden();

    // Open again and close with Escape
    await tryBtn.click();
    await expect(modalHost).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(modalHost).toBeHidden();
  });

  test("CS-AC-05: Buy item deducts coins, marks owned, updates toast and card UI", async ({ page }) => {
    const coinBalance = page.locator("#shopCoinBalance");
    const initialCoins = parseInt((await coinBalance.textContent())?.replace(/\D/g, "") || "0", 10);
    expect(initialCoins).toBe(650);

    // Find Sakura Breeze Cape (price: 150)
    const sakuraCard = page.locator('#shopGrid .shop-card[data-shop-id="cape-sakura"]');
    await expect(sakuraCard).toBeVisible();

    const buyBtn = sakuraCard.locator('.shop-buy-btn[data-item-id="cape-sakura"]');
    await buyBtn.click();

    // Assert coin balance deducted: 650 - 150 = 500
    await expect(coinBalance).toHaveText("500");

    // Card should now be marked owned
    await expect(sakuraCard.locator(".shop-owned-tag")).toHaveText("Đã sở hữu");
    await expect(sakuraCard.locator(".shop-equip-btn")).toBeVisible();
  });

  test("CS-AC-06: Equip / Unequip toggles state and active badge", async ({ page }) => {
    // Emerald Knight Cape is pre-owned in default state
    const emeraldCard = page.locator('#shopGrid .shop-card[data-shop-id="cape-emerald"]');
    await expect(emeraldCard).toBeVisible();

    // Initially equipped
    await expect(emeraldCard.locator(".shop-equipped-badge")).toBeVisible();
    const equipBtn = emeraldCard.locator('.shop-equip-btn[data-item-id="cape-emerald"]');
    await expect(equipBtn).toHaveText("Gỡ");

    // Click to Unequip
    await equipBtn.click();
    await expect(emeraldCard.locator(".shop-equipped-badge")).toBeHidden();
    await expect(equipBtn).toHaveText("Dùng");

    // Click to re-equip
    await equipBtn.click();
    await expect(emeraldCard.locator(".shop-equipped-badge")).toBeVisible();
    await expect(equipBtn).toHaveText("Gỡ");
  });

  test("CS-AC-07: Daily Coin Claim grants 100 coins and disables button", async ({ page }) => {
    const coinBalance = page.locator("#shopCoinBalance");
    const initialCoins = parseInt((await coinBalance.textContent())?.replace(/\D/g, "") || "0", 10);
    expect(initialCoins).toBe(650);

    const claimBtn = page.locator("#claimDailyCoinBtn");
    await expect(claimBtn).toBeEnabled();

    await claimBtn.click();

    await expect(coinBalance).toHaveText("750");
    await expect(claimBtn).toBeDisabled();
    await expect(claimBtn).toContainText("Đã nhận hôm nay");
  });

  test("CS-AC-08: Responsive (390px, 620px) without overflow and accessible focus rings", async ({ page }) => {
    // Test 620px viewport
    await page.setViewportSize({ width: 620, height: 800 });
    const overflow620 = await page.evaluate(() => {
      const pageEl = document.querySelector("#page-shop");
      return pageEl ? pageEl.scrollWidth > pageEl.clientWidth : false;
    });
    expect(overflow620).toBe(false);

    // Test 390px viewport
    await page.setViewportSize({ width: 390, height: 844 });
    const overflow390 = await page.evaluate(() => {
      const pageEl = document.querySelector("#page-shop");
      return pageEl ? pageEl.scrollWidth > pageEl.clientWidth : false;
    });
    expect(overflow390).toBe(false);

    // Check focus ring on searchbox
    const search = page.locator("#shopSearch");
    await search.focus();
    const hasFocus = await page.evaluate(() => {
      const el = document.querySelector("#shopSearch");
      if (!el) return false;
      const s = window.getComputedStyle(el);
      return s.outlineStyle !== "none" || s.boxShadow !== "none";
    });
    expect(hasFocus).toBe(true);
  });

  test("CS-AC-10: No transition: all in stylesheets", async ({ page }) => {
    const badTransitions = await page.evaluate(() => {
      const issues: string[] = [];
      for (const sheet of Array.from(document.styleSheets)) {
        try {
          for (const rule of Array.from(sheet.cssRules)) {
            if (rule instanceof CSSStyleRule) {
              const text = rule.style.cssText;
              if (
                /transition\s*:\s*all\b/i.test(text) ||
                /transition-property\s*:\s*all\b/i.test(text)
              ) {
                issues.push(`${rule.selectorText}: ${text}`);
              }
            }
          }
        } catch {
          // cross-origin stylesheets ignore
        }
      }
      return issues;
    });
    expect(badTransitions).toEqual([]);
  });

  test("CS-AC-09: Capture Visual Evidence Screenshots", async ({ page }) => {
    // 1. Main Shop View
    await page.setViewportSize({ width: 1280, height: 800 });
    await page.screenshot({ path: "docs/design-preview/screenshots/evidence-shop-01-main.png" });

    // 2. Try-On Modal
    await page.click('.shop-card[data-shop-id="wing-ender"] [data-shop-action="tryon"]');
    await expect(page.locator("#shopTryOnModalHost")).toBeVisible();
    await page.waitForTimeout(500);
    await page.screenshot({ path: "docs/design-preview/screenshots/evidence-shop-02-tryon-modal.png" });
    await page.click("#shopTryOnCloseBtn");

    // 3. Purchase & Equip State
    await page.click('.shop-card[data-shop-id="cape-sakura"] [data-shop-action="buy"]');
    await expect(page.locator('.shop-card[data-shop-id="cape-sakura"]')).toHaveClass(/is-owned/);
    await page.screenshot({ path: "docs/design-preview/screenshots/evidence-shop-03-owned-equipped.png" });

    // 4. Mobile Viewport 390px
    await page.setViewportSize({ width: 390, height: 844 });
    await page.screenshot({ path: "docs/design-preview/screenshots/evidence-shop-04-mobile.png" });
  });
});
