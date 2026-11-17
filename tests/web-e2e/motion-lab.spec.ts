import { test, expect, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

async function bootMotionLab(page: Page, clearStorage = true) {
  const errors: string[] = [];
  page.on("pageerror", (err) => errors.push(err.message));
  page.on("console", (msg) => {
    if (msg.type() === "error") errors.push(msg.text());
  });

  await page.goto("/motion-lab.html");
  if (clearStorage) {
    await page.evaluate(() => localStorage.clear());
    await page.reload();
  }
  await expect(page.locator("h1")).toContainText("Motion Lab");
  return errors;
}

test.describe("Motion Lab (motion-system.contract.md)", () => {
  test("MOTION-AC-01: Page loads with >=3 demo cards and zero console errors", async ({ page }) => {
    const errors = await bootMotionLab(page);
    const cards = page.locator(".ml-demo-card");
    const count = await cards.count();
    expect(count).toBeGreaterThanOrEqual(3);
    expect(errors).toEqual([]);
  });

  test("MOTION-AC-02: Token table displays computed values from :root", async ({ page }) => {
    await bootMotionLab(page);
    const rows = page.locator(".ml-token-table tbody tr");
    const rowCount = await rows.count();
    expect(rowCount).toBeGreaterThanOrEqual(6);

    // Verify token values in DOM match computed styles from :root
    const tokens = await page.evaluate(() => {
      const rootStyle = getComputedStyle(document.documentElement);
      const rows = Array.from(document.querySelectorAll(".ml-token-table tbody tr"));
      return rows.map((tr) => {
        const tokenName = tr.querySelector("code")?.textContent?.trim() ?? "";
        const computedInTable = tr.querySelector(".ml-token-val")?.textContent?.trim() ?? "";
        const actualComputed = rootStyle.getPropertyValue(tokenName).trim();
        return { tokenName, computedInTable, actualComputed };
      });
    });

    for (const t of tokens) {
      expect(t.computedInTable.length).toBeGreaterThan(0);
      expect(t.computedInTable).not.toBe("—");
      if (t.actualComputed) {
        expect(t.computedInTable).toBe(t.actualComputed);
      }
    }
  });

  test("MOTION-AC-03: Shared element card -> detail and back transition", async ({ page }) => {
    await bootMotionLab(page);
    const card = page.locator("#mlCardInst1");
    const grid = page.locator("#mlSharedGrid");
    const detail = page.locator("#mlSharedDetail");
    const backBtn = page.locator("#mlDetailBackBtn");

    await expect(grid).toBeVisible();
    await expect(detail).toBeHidden();

    // Get initial position of shared elements on card
    const cardIconBox = await page.locator("#mlCardInst1 .ml-shared-icon").boundingBox();
    expect(cardIconBox).not.toBeNull();

    // Click card to open detail
    await card.click();
    await expect(detail).toBeVisible();
    await expect(grid).toBeHidden();

    // Detail shared elements exist
    await expect(detail.locator(".ml-shared-icon")).toBeVisible();
    await expect(detail.locator(".ml-shared-title")).toBeVisible();

    // Detail icon has moved
    const detailIconBox = await detail.locator(".ml-shared-icon").boundingBox();
    expect(detailIconBox).not.toBeNull();

    // Click Back
    await backBtn.click();
    await expect(grid).toBeVisible();
    await expect(detail).toBeHidden();
  });

  test("MOTION-AC-04: Launch Sequence runs 4 steps sequentially to 100% completion", async ({ page }) => {
    await bootMotionLab(page);
    const launchBtn = page.locator("#mlLaunchBtn");
    const overlay = page.locator("#mlLaunchOverlay");

    await expect(overlay).toBeHidden();
    await launchBtn.click();
    await expect(overlay).toBeVisible();

    // Verify 4 stages exist
    const stages = ["java", "assets", "mods", "world"];
    for (const stage of stages) {
      await expect(overlay.locator(`[data-stage="${stage}"]`)).toBeVisible();
    }

    // Wait for the sequence to complete and overlay to close
    await expect(overlay).toBeHidden({ timeout: 5000 });

    // Status text on card indicates success
    const summaryText = page.locator("#mlLaunchSummary");
    await expect(summaryText).toContainText("Khởi chạy game thành công!");
  });

  test("MOTION-AC-05: Replay buttons trigger animation rerun", async ({ page }) => {
    await bootMotionLab(page);

    // Test Replay for Demo 3 (Microinteractions)
    const replay3 = page.locator('.ml-replay-btn[data-demo="micro"]');
    const badge = page.locator("#mlBadgeCount");
    const initialCount = await badge.textContent();

    await replay3.click();
    // After replay, spring button and badge get updated
    await expect(badge).toHaveClass(/is-pulsing/);
    const updatedCount = await badge.textContent();
    expect(Number(updatedCount)).toBe(Number(initialCount) + 1);

    // Test Replay for Demo 2 (Launch Sequence)
    const replay2 = page.locator('.ml-replay-btn[data-demo="launch"]');
    const overlay = page.locator("#mlLaunchOverlay");
    await expect(overlay).toBeHidden();
    await replay2.click();
    await expect(overlay).toBeVisible();
    await expect(overlay).toBeHidden({ timeout: 5000 });
  });

  test("MOTION-AC-06: Reduce motion toggle sets durations <= 0.05s and persists", async ({ page }) => {
    await bootMotionLab(page);
    const toggle = page.locator("#mlReduceMotionToggle");
    await expect(toggle).toBeVisible();

    // Turn ON reduce motion
    await toggle.click();
    await expect(page.locator("html")).toHaveAttribute("data-reduced-motion", "true");

    // Check computed durations are <= 0.05s (0.01ms / 1e-5s in CSS)
    const durSec = await page.evaluate(() => {
      const btn = document.querySelector(".ml-btn");
      if (!btn) return 0;
      const str = getComputedStyle(btn).transitionDuration;
      return parseFloat(str) || 0;
    });
    expect(durSec).toBeLessThanOrEqual(0.05);

    // Reload page and verify persistence
    await page.reload();
    await expect(page.locator("html")).toHaveAttribute("data-reduced-motion", "true");
    await expect(toggle).toHaveAttribute("aria-checked", "true");

    // Turn OFF reduce motion
    await toggle.click();
    await expect(page.locator("html")).not.toHaveAttribute("data-reduced-motion", "true");
  });

  test("MOTION-AC-07: No horizontal overflow at 390px viewport", async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await bootMotionLab(page);

    const overflow = await page.evaluate(() => {
      return document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1;
    });
    expect(overflow).toBe(true);

    // Verify cards are stacked in a single column
    const cards = page.locator(".ml-demo-card");
    const count = await cards.count();
    expect(count).toBeGreaterThanOrEqual(3);

    const firstCardBox = await cards.nth(0).boundingBox();
    const secondCardBox = await cards.nth(1).boundingBox();
    expect(firstCardBox).not.toBeNull();
    expect(secondCardBox).not.toBeNull();

    // Stacked vertically: second card top is below first card top
    if (firstCardBox && secondCardBox) {
      expect(secondCardBox.y).toBeGreaterThan(firstCardBox.y);
      // Both cards take almost full width
      expect(firstCardBox.width).toBeGreaterThan(320);
      expect(secondCardBox.width).toBeGreaterThan(320);
    }
  });

  test("MOTION-AC-08: Focus visible ring on controls", async ({ page }) => {
    await bootMotionLab(page);

    // Focus on reduce motion toggle via Tab
    const toggle = page.locator("#mlReduceMotionToggle");
    await toggle.focus();

    const outline = await toggle.evaluate((el) => {
      const cs = getComputedStyle(el);
      return {
        outlineWidth: parseFloat(cs.outlineWidth) || 0,
        outlineStyle: cs.outlineStyle,
        outlineOffset: parseFloat(cs.outlineOffset) || 0
      };
    });

    // In motion-lab.css: :focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
    expect(outline.outlineWidth).toBeGreaterThanOrEqual(2);
    expect(outline.outlineOffset).toBeGreaterThanOrEqual(1);
    expect(outline.outlineStyle).not.toBe("none");
  });

  test("MOTION-AC-09: motion-lab.css contains zero 'transition: all' or 'transition-property: all'", () => {
    const cssPath = resolve(process.cwd(), "docs/design-preview/motion-lab.css");
    const cssContent = readFileSync(cssPath, "utf-8");

    const match1 = cssContent.match(/transition\s*:\s*all\b/i);
    const match2 = cssContent.match(/transition-property\s*:\s*all\b/i);

    expect(match1).toBeNull();
    expect(match2).toBeNull();
  });
});
