import { test, expect, type Page } from "@playwright/test";

/**
 * Runtime spec cho contract `prototype-ui-polish` (POL-AC-03 … POL-AC-10).
 * Chỉ assert hành vi người dùng thấy: computed style, visibility, vị trí,
 * trạng thái aria và việc phần tử có còn trên DOM hay không.
 */

const TAB_NAMES = ["mods", "worlds", "screenshots", "logs", "settings"];

async function boot(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
}

async function openInstanceDetail(page: Page) {
  await page.locator(".instance-card").first().click({ force: true });
  await expect(page.locator("#page-detail")).toBeVisible();
  await expect(page.locator("#tab-mods")).toBeVisible();
}

type TransitionInfo = {
  property: string;
  duration: string;
  maxDuration: number;
  animationName: string;
  background: string;
  color: string;
};

async function readStyle(page: Page, selector: string): Promise<TransitionInfo> {
  return page.evaluate((sel) => {
    const el = document.querySelector(sel);
    if (!el) throw new Error(`Không tìm thấy ${sel}`);
    const cs = getComputedStyle(el);
    const durations = cs.transitionDuration.split(",").map((d) => parseFloat(d) || 0);
    return {
      property: cs.transitionProperty,
      duration: cs.transitionDuration,
      maxDuration: Math.max(...durations),
      animationName: cs.animationName,
      background: cs.backgroundColor,
      color: cs.color,
    };
  }, selector);
}

/**
 * Đưa focus vào control bằng bàn phím: click để lấy focus, Tab đi tiếp rồi
 * Shift+Tab quay lại — lúc này :focus-visible mới bật, đúng như người dùng Tab.
 */
async function keyboardFocusInfo(page: Page, selector: string) {
  await page.locator(selector).first().click({ force: true });
  await page.keyboard.press("Tab");
  await page.keyboard.press("Shift+Tab");

  const info = await page.evaluate((sel) => {
    const active = document.activeElement as HTMLElement | null;
    const target = document.querySelector(sel);
    if (!active || !target) return null;
    const cs = getComputedStyle(active);
    return {
      isTarget: active === target,
      tag: active.tagName.toLowerCase(),
      focusVisible: active.matches(":focus-visible"),
      outlineStyle: cs.outlineStyle,
      outlineWidth: parseFloat(cs.outlineWidth) || 0,
      outlineOffset: parseFloat(cs.outlineOffset) || 0,
      outlineColor: cs.outlineColor,
    };
  }, selector);

  expect(info, `Không lấy được focus ring cho ${selector}`).not.toBeNull();
  return info!;
}

function expectStrongFocusRing(info: Awaited<ReturnType<typeof keyboardFocusInfo>>, label: string) {
  expect(info.isTarget, `${label}: focus không nằm trên control`).toBe(true);
  expect(info.focusVisible, `${label}: control không ở trạng thái :focus-visible`).toBe(true);
  expect(info.outlineStyle, `${label}: không có outline`).not.toBe("none");
  expect(info.outlineWidth, `${label}: outline < 2px`).toBeGreaterThanOrEqual(2);
  expect(info.outlineOffset, `${label}: outline-offset < 1px`).toBeGreaterThanOrEqual(1);
}

test.describe("Prototype UI polish — desktop", () => {
  test.use({ viewport: { width: 1280, height: 800 } });

  test("POL-AC-03: hover button/nav item dùng transition property tường minh", async ({ page }) => {
    await boot(page);
    await page.mouse.move(2, 2);

    for (const selector of ["#newInstanceBtn", '.nav-item[data-screen="skins"]']) {
      const before = await readStyle(page, selector);
      expect(before.property, `${selector} còn animate all`).not.toMatch(/\ball\b/);
      expect(before.property.trim().length, `${selector} không khai báo property`).toBeGreaterThan(0);
      expect(before.maxDuration, `${selector} không có transition`).toBeGreaterThan(0);

      await page.locator(selector).hover();
      await expect
        .poll(async () => (await readStyle(page, selector)).background, {
          message: `${selector} không đổi nền khi hover`,
          timeout: 2000,
        })
        .not.toBe(before.background);
    }
  });

  test("POL-AC-04: đóng dialog thì #dialogHost ẩn hẳn, không treo is-closing", async ({ page }) => {
    await boot(page);

    await page.locator("#newInstanceBtn").click();
    const host = page.locator("#dialogHost");
    await expect(host).toBeVisible();
    await expect(host.locator('[role="dialog"]')).toBeVisible();

    await page.locator("#niCancelBtn").click();

    await expect(host).toBeHidden();
    const state = await page.evaluate(() => {
      const el = document.querySelector("#dialogHost") as HTMLElement;
      return {
        hidden: el.hidden,
        isClosing: el.classList.contains("is-closing"),
        dialogId: el.getAttribute("data-dialog-id"),
        dialogCount: el.querySelectorAll(".dialog").length,
        innerHtml: el.innerHTML.trim().length,
      };
    });

    expect(state.hidden, "#dialogHost chưa ẩn").toBe(true);
    expect(state.isClosing, "is-closing còn treo trên #dialogHost").toBe(false);
    expect(state.dialogId, "data-dialog-id còn treo").toBeNull();
    expect(state.dialogCount, "dialog vẫn còn trong DOM").toBe(0);
    expect(state.innerHtml, "nội dung dialog chưa được dọn").toBe(0);
  });

  test("POL-AC-05: chuyển 5 tab, mỗi lần đúng 1 panel và aria-selected đúng", async ({ page }) => {
    await boot(page);
    await openInstanceDetail(page);

    for (const name of ["worlds", "screenshots", "logs", "settings", "mods"]) {
      await page.locator(`button.tab[data-tab="${name}"]`).click();

      await expect(page.locator(`#tab-${name}`)).toBeVisible();
      // POL-AC-05: chỉ đúng 1 panel visible ngay lập tức, không có layout push
      await expect(page.locator(".tab-panel:visible")).toHaveCount(1);

      for (const other of TAB_NAMES) {
        await expect(page.locator(`button.tab[data-tab="${other}"]`)).toHaveAttribute(
          "aria-selected",
          other === name ? "true" : "false",
        );
      }
    }
  });

  test("POL-AC-06: hai toast xếp chồng, có is-out rồi tự biến mất", async ({ page }) => {
    await boot(page);

    await page.locator('button.nav-item[data-screen="settings"]').click();
    await expect(page.locator("#page-settings")).toBeVisible();

    await page.locator("#saveGlobalSettingsBtn").click();
    await page.locator("#openDataFolderBtn").click();

    const toasts = page.locator(".toast");
    await expect(toasts).toHaveCount(2);

    const boxes = await toasts.evaluateAll((els) =>
      els.map((el) => {
        const r = el.getBoundingClientRect();
        return {
          text: (el.textContent || "").trim(),
          top: r.top,
          bottom: r.bottom,
          visible: r.width > 0 && r.height > 0,
        };
      }),
    );

    for (const box of boxes) {
      expect(box.text.length, "toast rỗng").toBeGreaterThan(0);
      expect(box.visible, "toast không hiển thị").toBe(true);
    }
    expect(boxes[0].bottom, "hai toast đè lên nhau").toBeLessThanOrEqual(boxes[1].top + 1);

    await expect(toasts.first()).toHaveClass(/is-out/, { timeout: 4000 });
    await expect(toasts).toHaveCount(0, { timeout: 5000 });
  });

  test("POL-AC-08: focus ring ≥2px, offset ≥1px trên nav item, button, input, tab", async ({ page }) => {
    await boot(page);

    expectStrongFocusRing(
      await keyboardFocusInfo(page, '.nav-item[data-screen="instances"]'),
      "nav item",
    );

    expectStrongFocusRing(await keyboardFocusInfo(page, "#instanceSearch"), "input");

    await page.locator('.nav-item[data-screen="settings"]').click();
    await expect(page.locator("#page-settings")).toBeVisible();
    expectStrongFocusRing(
      await keyboardFocusInfo(page, "#saveGlobalSettingsBtn"),
      "button",
    );

    await page.locator('.nav-item[data-screen="instances"]').click();
    await openInstanceDetail(page);
    expectStrongFocusRing(
      await keyboardFocusInfo(page, 'button.tab[data-tab="worlds"]'),
      "tab",
    );
  });

  test("POL-AC-09: reduced motion OS tắt chuyển động, không tạo ripple, pulse không animate", async ({ page }) => {
    await boot(page);

    const normal = await readStyle(page, "#newInstanceBtn");
    expect(normal.maxDuration, "chế độ thường phải có transition").toBeGreaterThan(0.05);

    await page.locator("#newInstanceBtn").click();
    await expect(page.locator("#dialogHost")).toBeVisible();
    await expect(page.locator("#newInstanceBtn .ripple")).toHaveCount(1, { timeout: 1000 });
    await page.locator("#niCancelBtn").click();
    await expect(page.locator("#dialogHost")).toBeHidden();

    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.reload();
    await expect(page.locator("#page-instances")).toBeVisible();

    const reduced = await readStyle(page, "#newInstanceBtn");
    expect(reduced.maxDuration, "transition vẫn chạy khi prefers-reduced-motion").toBeLessThanOrEqual(0.05);
    expect(reduced.animationName, "animation vẫn bật khi prefers-reduced-motion").toBe("none");

    await page.locator("#newInstanceBtn").click();
    await expect(page.locator("#dialogHost")).toBeVisible();
    await expect(page.locator(".ripple")).toHaveCount(0);
    await page.locator("#niCancelBtn").click();
    await expect(page.locator("#dialogHost")).toBeHidden();

    await openInstanceDetail(page);
    await page.locator("#checkUpdatesBtn").click();
    await expect(page.locator("#detailStatus")).toHaveClass(/is-pulse/, { timeout: 5000 });
    const pulse = await readStyle(page, "#detailStatus");
    expect(pulse.animationName, "pulse vẫn animate khi prefers-reduced-motion").toBe("none");
  });

  test("POL-AC-09: công tắc Giảm chuyển động trong Settings tắt chuyển động", async ({ page }) => {
    await boot(page);

    await page.locator('.nav-item[data-screen="settings"]').click();
    await expect(page.locator("#page-settings")).toBeVisible();

    await page.locator("#globalReduceMotion").check();
    await expect(page.locator("html")).toHaveClass(/reduce-motion/);

    const reduced = await readStyle(page, "#globalReduceMotion");
    expect(reduced.maxDuration).toBeLessThanOrEqual(0.05);
    const button = await readStyle(page, "#saveGlobalSettingsBtn");
    expect(button.animationName).toBe("none");
    expect(button.maxDuration).toBeLessThanOrEqual(0.05);

    await page.locator("#openDataFolderBtn").click();
    await expect(page.locator(".toast")).toHaveCount(1);
    await expect(page.locator(".ripple")).toHaveCount(0);
  });
});

test.describe("Prototype UI polish — mobile", () => {
  test.use({ viewport: { width: 620, height: 900 } });

  test("POL-AC-07: sidebar mobile mở/đóng bằng toggle, Escape, click ngoài; khoá scroll", async ({ page }) => {
    await boot(page);

    const toggle = page.locator(".nav-mobile-toggle");
    const nav = page.locator("aside.nav");
    const scrim = page.locator(".nav-scrim");
    const content = page.locator("#content");
    const navWidth = async () => (await nav.boundingBox())?.width ?? 0;

    await expect(toggle).toBeVisible();
    await expect(nav).toBeVisible();
    expect(await navWidth(), "sidebar phải thu gọn thành rail").toBeLessThan(100);
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
    await expect(scrim).toHaveCSS("opacity", "0");

    // Mở drawer
    await toggle.click({ force: true });
    await expect(page.locator("html")).toHaveClass(/nav-open/);
    await expect(toggle).toHaveAttribute("aria-expanded", "true");
    await expect(scrim).toHaveCSS("opacity", "1");
    await expect(content).toHaveCSS("overflow", "hidden");
    await expect(page.locator("body")).toHaveCSS("overflow", "hidden");
    await expect(nav).toHaveCSS("width", "272px");
    await expect.poll(navWidth, { message: "drawer chưa mở rộng" }).toBeGreaterThan(200);

    // Escape đóng
    await page.keyboard.press("Escape");
    await expect(page.locator("html")).not.toHaveClass(/nav-open/);
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
    await expect(scrim).toHaveCSS("opacity", "0");
    await expect(content).toHaveCSS("overflow", "auto");
    await expect.poll(navWidth, { message: "drawer chưa thu về rail" }).toBeLessThan(100);

    // Click ngoài drawer đóng
    await toggle.click({ force: true });
    await expect(page.locator("html")).toHaveClass(/nav-open/);
    await expect(nav).toHaveCSS("width", "272px");
    await page.mouse.click(600, 400);
    await expect(page.locator("html")).not.toHaveClass(/nav-open/);
    await expect(content).toHaveCSS("overflow", "auto");
    await expect.poll(navWidth).toBeLessThan(100);

    // Chọn mục trong drawer: điều hướng + đóng drawer
    await toggle.click({ force: true });
    await expect(page.locator("html")).toHaveClass(/nav-open/);
    await expect(nav).toHaveCSS("width", "272px");
    await page.locator('.nav-item[data-screen="skins"]').click({ force: true });
    await expect(page.locator("#page-skins")).toBeVisible();
    await expect(page.locator("html")).not.toHaveClass(/nav-open/);
    await expect(content).toHaveCSS("overflow", "auto");
    await expect.poll(navWidth).toBeLessThan(100);
  });

  test("POL-AC-10: không tràn ngang ở 620px và 390px, sidebar vẫn dùng được", async ({ page }) => {
    await boot(page);

    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 900 });
      await expect
        .poll(async () => page.evaluate(() => document.documentElement.scrollWidth), { timeout: 3000 })
        .toBeLessThanOrEqual(width + 1);

      const metrics = await page.evaluate(() => ({
        scrollWidth: document.documentElement.scrollWidth,
        clientWidth: document.documentElement.clientWidth,
      }));
      expect(metrics.scrollWidth, `tràn ngang ở ${width}px`).toBeLessThanOrEqual(metrics.clientWidth + 1);

      // Sidebar vẫn mở được ở mọi breakpoint
      await expect(page.locator(".nav-mobile-toggle")).toBeVisible();
      await page.locator(".nav-mobile-toggle").click({ force: true });
      await expect(page.locator("aside.nav")).toBeVisible();
      await expect
        .poll(async () => (await page.locator("aside.nav").boundingBox())?.width ?? 0, { timeout: 3000 })
        .toBeGreaterThan(200);
      await expect
        .poll(async () => page.evaluate(() => document.documentElement.scrollWidth), { timeout: 3000 })
        .toBeLessThanOrEqual(width + 1);
      await page.keyboard.press("Escape");
      await expect
        .poll(async () => (await page.locator("aside.nav").boundingBox())?.width ?? 0, { timeout: 3000 })
        .toBeLessThan(100);
    }
  });
});
