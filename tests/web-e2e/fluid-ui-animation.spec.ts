import { test, expect, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

async function boot(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
}

test.describe("Fluid UI & Animation Polish (fluid-ui-animation.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await boot(page);
  });

  test("FA-AC-01: Phản hồi xúc giác tức thì (:active scale) trên các phần tử tương tác", async ({ page }) => {
    // Check CSS rules in stylesheet for tactile :active transform scale < 1.0
    const activeRules = await page.evaluate(() => {
      const results: { selector: string; transform: string }[] = [];
      for (const sheet of Array.from(document.styleSheets)) {
        try {
          for (const rule of Array.from(sheet.cssRules || [])) {
            if (rule instanceof CSSStyleRule && rule.selectorText && rule.selectorText.includes(":active")) {
              if (rule.style.transform && rule.style.transform.includes("scale")) {
                results.push({ selector: rule.selectorText, transform: rule.style.transform });
              }
            }
          }
        } catch {
          // Ignore external stylesheet access restrictions
        }
      }
      return results;
    });

    expect(activeRules.some((r) => r.selector.includes(".btn:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".nav-item:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".tab:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".instance-card:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".shop-card:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".skin-card:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".screenshot-card:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".world-card:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".server-row:active"))).toBe(true);
    expect(activeRules.some((r) => r.selector.includes(".chip:active"))).toBe(true);
  });

  test("FA-AC-02: Chuyển động thanh trượt Nav Glider & Tab Glider mượt mà theo đường cong spring tự nhiên", async ({ page }) => {
    const navGlider = page.locator(".nav-glider").first();
    await expect(navGlider).toHaveClass(/is-ready/);
    await expect(navGlider).toHaveCSS("opacity", "1");

    // Switch to Skins and verify glider moves
    const skinsNav = page.locator('.nav-item[data-screen="skins"]');
    await skinsNav.click({ force: true });
    await expect(page.locator("#page-skins")).toBeVisible();
    await expect(skinsNav).toHaveClass(/is-selected/);

    // Navigate to instance detail to check tab glider
    const instancesNav = page.locator('.nav-item[data-screen="instances"]');
    await instancesNav.click({ force: true });
    await page.locator(".instance-card").first().click({ force: true });
    await expect(page.locator("#page-detail")).toBeVisible();

    const tabGlider = page.locator(".tab-glider").first();
    await expect(tabGlider).toHaveClass(/is-ready/);
    await expect(tabGlider).toHaveCSS("opacity", "1");

    // Click settings tab and verify transition
    const settingsTab = page.locator('.tab[data-tab="settings"]');
    await settingsTab.click({ force: true });
    await expect(settingsTab).toHaveClass(/is-selected/);
  });

  test("FA-AC-03: Chất liệu kính mờ (Frosted glass) và viền phản quang bắt sáng", async ({ page }) => {
    // Check dialog-host and modal-scrim backdrop-filter
    const scrimStyles = await page.evaluate(() => {
      const scrim = document.querySelector(".modal-scrim") || document.querySelector(".dialog-host");
      if (!scrim) return { backdropFilter: "none" };
      const cs = window.getComputedStyle(scrim);
      return {
        backdropFilter: cs.backdropFilter || (cs as unknown as Record<string, string>)["-webkit-backdrop-filter"] || "none",
      };
    });
    expect(scrimStyles.backdropFilter).toMatch(/blur/);

    const sidebarBlur = await page.evaluate(() => {
      const nav = document.querySelector(".nav");
      if (!nav) return "none";
      const cs = window.getComputedStyle(nav);
      return cs.backdropFilter || (cs as unknown as Record<string, string>)["-webkit-backdrop-filter"] || "none";
    });
    expect(sidebarBlur).toMatch(/blur/);
  });

  test("FA-AC-04: Chuẩn hóa hiệu ứng mở/đóng Modal & Dialog", async ({ page }) => {
    // Open Offline Account Modal
    const accountsNav = page.locator('.nav-item[data-screen="accounts"]');
    await accountsNav.click({ force: true });
    await expect(page.locator("#page-accounts")).toBeVisible();

    const addOfflineBtn = page.locator("#addOfflineBtn");
    await addOfflineBtn.click({ force: true });

    const modalHost = page.locator("#offlineModalHost");
    await expect(modalHost).toBeVisible();

    const modalAnimation = await page.locator("#offlineModalHost .modal-card").evaluate((el) => {
      const cs = window.getComputedStyle(el);
      return cs.animationName;
    });
    expect(modalAnimation).toMatch(/dialog-in/);

    // Close modal
    const closeBtn = page.locator("#offlineModalHost .modal-close-btn");
    await closeBtn.click({ force: true });
    await expect(modalHost).toBeHidden();
  });

  test("FA-AC-05: Hiệu ứng nâng thẻ (Card Hover Elevation) khi hover", async ({ page }) => {
    const card = page.locator(".instance-card").first();
    await card.hover();

    const elevation = await card.evaluate((el) => {
      const cs = window.getComputedStyle(el);
      return {
        transform: cs.transform,
        boxShadow: cs.boxShadow,
      };
    });

    expect(elevation.transform).not.toBe("none");
    expect(elevation.boxShadow).not.toBe("none");
  });

  test("FA-AC-06: Phân cấp thị giác quang học (Optical Typography)", async ({ page }) => {
    const pageTitleTracking = await page.locator(".page-title").first().evaluate((el) => {
      return window.getComputedStyle(el).letterSpacing;
    });
    // Expected negative tracking for large titles
    expect(pageTitleTracking).not.toBe("normal");

    const modalTitleTracking = await page.evaluate(() => {
      const title = document.querySelector(".modal-title") || document.querySelector(".dialog-title");
      if (!title) return "normal";
      return window.getComputedStyle(title).letterSpacing;
    });
    expect(modalTitleTracking).not.toBe("normal");
  });

  test("FA-AC-07: Tuyệt đối KHÔNG sử dụng transition: all hoặc transition-property: all trên toàn bộ CSS", async () => {
    const cssPath = resolve(process.cwd(), "docs/design-preview/styles.css");
    const content = readFileSync(cssPath, "utf-8");

    const matches = content.match(/transition(-property)?\s*:\s*([^;}]*all[^;}]*)[;}]/gi) || [];
    expect(matches).toEqual([]);
  });

  test("FA-AC-08: Hỗ trợ Reduced Motion khi prefers-reduced-motion: reduce", async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.reload();

    const durations = await page.evaluate(() => {
      const items = [
        document.querySelector(".btn"),
        document.querySelector(".nav-item"),
        document.querySelector(".tab"),
        document.querySelector(".instance-card"),
        document.querySelector(".shop-card"),
      ].filter(Boolean);

      return items.map((el) => {
        const cs = window.getComputedStyle(el!);
        const transDur = cs.transitionDuration.split(",").map((s) => parseFloat(s));
        const animDur = cs.animationDuration.split(",").map((s) => parseFloat(s));
        return {
          maxTrans: Math.max(...transDur, 0),
          maxAnim: Math.max(...animDur, 0),
        };
      });
    });

    for (const d of durations) {
      expect(d.maxTrans).toBeLessThanOrEqual(0.05);
      expect(d.maxAnim).toBeLessThanOrEqual(0.05);
    }
  });

  test("FA-AC-09: Responsive hoàn hảo tại 390px và 620px không bị tràn ngang", async ({ page }) => {
    for (const width of [390, 620]) {
      await page.setViewportSize({ width, height: 800 });
      await page.goto("/");
      await page.waitForTimeout(100);

      const overflow = await page.evaluate(() => {
        const doc = document.documentElement;
        return {
          scrollWidth: doc.scrollWidth,
          clientWidth: doc.clientWidth,
          hasOverflow: doc.scrollWidth > doc.clientWidth + 1,
        };
      });

      expect(overflow.hasOverflow).toBe(false);
    }
  });
});
