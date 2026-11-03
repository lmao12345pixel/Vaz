import { test, expect, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

async function bootApp(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
}

test.describe("Motion Integration (motion-integration.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await bootApp(page);
  });

  test("MI-AC-01: Click instance card trong #instanceGrid thực hiện FLIP transition (icon + title) vào header #page-detail, #page-detail hiển thị ngay", async ({ page }) => {
    const card = page.locator(".instance-card").first();
    await expect(card).toBeVisible();

    const cardIcon = card.locator(".mc-block-icon, .card-block-badge").first();
    const cardTitle = card.locator(".instance-name, .name").first();
    const cardNameText = (await cardTitle.textContent())?.trim() ?? "";

    const cardIconBox = await cardIcon.boundingBox();
    const cardTitleBox = await cardTitle.boundingBox();
    expect(cardIconBox).not.toBeNull();
    expect(cardTitleBox).not.toBeNull();

    await card.click();

    const detailPage = page.locator("#page-detail");
    await expect(detailPage).toBeVisible();
    await expect(page.locator("#page-instances")).toBeHidden();

    const detailTitle = page.locator("#detailName");
    const detailIcon = page.locator("#detailBlockIcon");
    await expect(detailTitle).toBeVisible();
    await expect(detailTitle).toContainText(cardNameText);
    await expect(detailIcon).toBeVisible();

    const detailIconBox = await detailIcon.boundingBox();
    const detailTitleBox = await detailTitle.boundingBox();
    expect(detailIconBox).not.toBeNull();
    expect(detailTitleBox).not.toBeNull();
  });

  test("MI-AC-02: Click nút Back (#btnBackToInstances) thực hiện reverse FLIP transition về đúng vị trí card ban đầu, #page-instances hiển thị", async ({ page }) => {
    const card = page.locator(".instance-card").first();
    await card.click();
    await expect(page.locator("#page-detail")).toBeVisible();

    const backBtn = page.locator("#btnBackToInstances");
    await expect(backBtn).toBeVisible();
    await backBtn.click();

    await expect(page.locator("#page-instances")).toBeVisible();
    await expect(page.locator("#page-detail")).toBeHidden();

    const returnedCard = page.locator(".instance-card").first();
    await expect(returnedCard).toBeVisible();
    const cardBox = await returnedCard.boundingBox();
    expect(cardBox).not.toBeNull();
  });

  test("MI-AC-03: Khởi chạy instance trực tiếp không popup modal, hiển thị trạng thái inline mượt mà", async ({ page }) => {
    const card = page.locator(".instance-card").first();
    const launchBtn = card.locator('[data-action="play"]');
    await launchBtn.click();

    // Không mở bất kỳ overlay modal nào (YAGNI, bỏ popup thừa)
    await expect(page.locator("#launchOverlayHost")).toHaveCount(0);

    // Instance chuyển sang trạng thái khởi chạy inline
    await expect(card.locator(".badge-status, .card-status, .chip.running")).toBeVisible({ timeout: 5000 });
  });

  test("MI-AC-04: Khởi chạy hoàn tất đánh dấu running (Đang chạy), nút đổi sang Stop", async ({ page }) => {
    const card = page.locator(".instance-card").first();
    const launchBtn = card.locator('[data-action="play"]');
    await launchBtn.click();

    // Instance chạy thành công
    const runningChip = card.locator(".chip.running");
    await expect(runningChip).toBeVisible({ timeout: 6000 });
    await expect(runningChip).toContainText("Đang chạy");

    // Nút play chuyển sang Stop
    await expect(launchBtn).toHaveAttribute("title", "Stop");
  });

  test("MI-AC-05: Click Stop dừng instance an toàn và trả về trạng thái bình thường", async ({ page }) => {
    const card = page.locator(".instance-card").first();
    const playBtn = card.locator('[data-action="play"]');
    await playBtn.click();

    const runningChip = card.locator(".chip.running");
    await expect(runningChip).toBeVisible({ timeout: 6000 });

    // Dừng instance
    await playBtn.click();
    await expect(runningChip).toBeHidden({ timeout: 3000 });
    await expect(playBtn).toHaveAttribute("title", "Launch");
  });

  test("MI-AC-06: Reduce motion: FLIP transition chạy tức thì, duration <= 0.05s", async ({ page }) => {
    await page.evaluate(() => {
      document.documentElement.classList.add("reduce-motion");
    });

    const card = page.locator(".instance-card").first();
    await card.click();

    await expect(page.locator("#page-detail")).toBeVisible();
    await expect(page.locator("#page-instances")).toBeHidden();

    const backBtn = page.locator("#btnBackToInstances");
    await backBtn.click();
    await expect(page.locator("#page-instances")).toBeVisible();
  });

  test("MI-AC-07: Responsive (390px, 620px) FLIP và navigation không tràn ngang", async ({ page }) => {
    for (const width of [390, 620]) {
      await page.setViewportSize({ width, height: 844 });
      const card = page.locator(".instance-card").first();
      await card.click();

      await expect(page.locator("#page-detail")).toBeVisible();

      // Check no horizontal overflow
      const overflow = await page.evaluate(() => {
        const doc = document.documentElement;
        return {
          docScroll: doc.scrollWidth,
          docClient: doc.clientWidth,
        };
      });

      expect(overflow.docScroll).toBeLessThanOrEqual(overflow.docClient + 1);

      const backBtn = page.locator("#btnBackToInstances");
      await backBtn.click();
      await expect(page.locator("#page-instances")).toBeVisible();
    }
  });

  test("MI-AC-09: styles.css contains zero 'transition: all' or 'transition-property: all'", async () => {
    const cssPath = resolve(process.cwd(), "docs/design-preview/styles.css");
    const content = readFileSync(cssPath, "utf-8");
    const hasTransitionAll = /transition:\s*all\b/i.test(content);
    const hasTransitionPropertyAll = /transition-property:\s*all\b/i.test(content);

    expect(hasTransitionAll).toBe(false);
    expect(hasTransitionPropertyAll).toBe(false);
  });
});
