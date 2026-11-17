import { test, expect, type Page } from "@playwright/test";

async function scrollToY(page: Page, top: number) {
  await page.evaluate((y) => {
    const root = document.documentElement;
    const previous = root.style.scrollBehavior;
    root.style.scrollBehavior = "auto";
    window.scrollTo(0, y);
    root.style.scrollBehavior = previous;
  }, top);
}

function computedOpacity(page: Page, selector: string) {
  return page
    .locator(selector)
    .first()
    .evaluate((el) => Number(getComputedStyle(el).opacity));
}

test.describe("V.A.Z Official Website Landing Page", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/website/");
  });

  test("Website tải thành công và hiển thị đầy đủ các section", async ({ page }) => {
    // Tiêu đề trang
    await expect(page).toHaveTitle(/V\.A\.Z Launcher/i);

    // Navbar
    await expect(page.locator("header.navbar")).toBeVisible();
    await expect(page.locator("a.brand")).toContainText("V.A.Z");

    // Hero Section
    await expect(page.locator("#hero")).toBeVisible();
    await expect(page.locator("h1.hero-title")).toContainText("Minecraft Launcher");
    await expect(page.locator('#hero a.btn-primary[href="#download"]')).toBeVisible();

    // 3D Canvas hoặc Fallback
    const canvas = page.locator("#heroSkinCanvas");
    await expect(canvas).toBeAttached();

    // Features Section
    await expect(page.locator("#features")).toBeVisible();
    await expect(page.locator(".pillar-card")).toHaveCount(3);

    // Showcase Section
    await expect(page.locator("#showcase")).toBeVisible();

    // Comparison Section
    await expect(page.locator("#compare")).toBeVisible();
    await expect(page.locator(".comparison-table")).toBeVisible();

    // Download Section
    await expect(page.locator("#download")).toBeVisible();
    await expect(page.locator(".download-card")).toHaveCount(3);

    // FAQ Section
    await expect(page.locator("#faq")).toBeVisible();
    await expect(page.locator(".faq-item")).toHaveCount(5);
  });

  test("Showcase tabs chuyển đổi mượt mà và tương tác Play button", async ({ page }) => {
    // Mặc định tab dock active
    await expect(page.locator("#tabContent-dock")).toBeVisible();
    await expect(page.locator("#tabContent-options")).toBeHidden();

    // Tương tác nút Play mô phỏng trên Dock
    const playBtn = page.locator("#interactivePlayBtn");
    await expect(playBtn).toBeVisible();
    await expect(page.locator("#playBtnText")).toContainText("CHƠI NGAY");
    await playBtn.click();
    await expect(page.locator("#playBtnText")).toContainText("ĐANG KHỞI CHẠY");

    // Chuyển sang tab options.txt
    await page.locator('button.showcase-tab-btn[data-tab="options"]').click();
    await expect(page.locator("#tabContent-options")).toBeVisible();
    await expect(page.locator("#tabContent-dock")).toBeHidden();

    // Kiểm tra slider FOV
    const fovSlider = page.locator("#fovSlider");
    await fovSlider.fill("105");
    await expect(page.locator("#fovVal")).toHaveText("105°");

    // Chuyển sang tab importer
    await page.locator('button.showcase-tab-btn[data-tab="importer"]').click();
    await expect(page.locator("#tabContent-importer")).toBeVisible();
    await expect(page.locator("#tabContent-options")).toBeHidden();
  });

  test("FAQ accordion mở và đóng câu hỏi chính xác", async ({ page }) => {
    const firstFaq = page.locator(".faq-item").first();
    const firstQuestion = firstFaq.locator(".faq-question");

    // Ban đầu chưa mở
    await expect(firstFaq).not.toHaveClass(/is-open/);

    // Click lần 1 -> Mở ra
    await firstQuestion.click();
    await expect(firstFaq).toHaveClass(/is-open/);

    // Click lần 2 -> Đóng lại
    await firstQuestion.click();
    await expect(firstFaq).not.toHaveClass(/is-open/);
  });

  test("Skin picker buttons đổi model thành công", async ({ page }) => {
    const alexBtn = page.locator('button.skin-btn[data-skin="alex"]');
    await expect(alexBtn).toBeVisible();
    await alexBtn.click();
    await expect(alexBtn).toHaveClass(/is-active/);

    const steveBtn = page.locator('button.skin-btn[data-skin="steve"]');
    await steveBtn.click();
    await expect(steveBtn).toHaveClass(/is-active/);
  });
});

test.describe("Website UI polish — motion & interaction runtime", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/website/");
  });

  test("UX-AC-01: navbar nhận is-scrolled sau khi cuộn và mất khi về đầu trang", async ({ page }) => {
    const navbar = page.locator("header.navbar");
    await expect(navbar).not.toHaveClass(/is-scrolled/);

    await scrollToY(page, 600);
    await expect(navbar).toHaveClass(/is-scrolled/);

    const scrolledStyles = await navbar.evaluate((el) => {
      const styles = getComputedStyle(el);
      return { background: styles.backgroundColor, shadow: styles.boxShadow };
    });
    expect(scrolledStyles.shadow).not.toBe("none");

    await scrollToY(page, 0);
    await expect(navbar).not.toHaveClass(/is-scrolled/);
  });

  test("UX-AC-02: section reveal một lần, nội dung hiện đầy đủ sau khi cuộn tới", async ({ page }) => {
    const cards = page.locator("#features .pillar-card");
    await expect(cards).toHaveCount(3);

    const sectionHeader = page.locator("#features .section-header");
    await sectionHeader.scrollIntoViewIfNeeded();
    await expect(sectionHeader).toHaveClass(/is-revealed/);

    for (let i = 0; i < 3; i += 1) {
      await cards.nth(i).scrollIntoViewIfNeeded();
      await expect(cards.nth(i)).toHaveClass(/is-revealed/);
      await expect
        .poll(() => cards.nth(i).evaluate((el) => Number(getComputedStyle(el).opacity)))
        .toBe(1);
    }

    // Reveal chỉ chạy một lần: cuộn đi rồi quay lại vẫn giữ trạng thái đã hiện
    await scrollToY(page, 0);
    await page.waitForTimeout(150);
    await sectionHeader.scrollIntoViewIfNeeded();
    for (let i = 0; i < 3; i += 1) {
      await expect(cards.nth(i)).toHaveClass(/is-revealed/);
    }
  });

  test("UX-AC-03: FAQ mở hết đáp án dài, không bị cắt, aria-expanded đúng", async ({ page }) => {
    const item = page.locator(".faq-item").nth(1);
    const question = item.locator(".faq-question");
    const answer = item.locator(".faq-answer");

    // Kéo dài đáp án vượt xa giới hạn 300px cũ để chứng minh không bị cắt
    await answer.evaluate((el) => {
      const paragraph = el.querySelector("p");
      if (paragraph) {
        const text = paragraph.textContent || "";
        paragraph.textContent = `${text} ${text} ${text} ${text} ${text}`;
      }
    });

    await question.click();
    await expect(item).toHaveClass(/is-open/);
    await expect(question).toHaveAttribute("aria-expanded", "true");

    await expect
      .poll(() => answer.evaluate((el) => el.clientHeight >= el.scrollHeight))
      .toBe(true);

    const answerBox = await answer.evaluate((el) => {
      const styles = getComputedStyle(el);
      return {
        height: el.getBoundingClientRect().height,
        overflow: styles.overflowY
      };
    });
    expect(answerBox.height).toBeGreaterThan(300);
    expect(answerBox.overflow).toBe("hidden");

    // Đóng lại
    await question.click();
    await expect(item).not.toHaveClass(/is-open/);
    await expect(question).toHaveAttribute("aria-expanded", "false");
    await expect
      .poll(() => answer.evaluate((el) => el.getBoundingClientRect().height))
      .toBeLessThan(2);

    // Chỉ một câu mở tại một thời điểm
    const first = page.locator(".faq-item").first();
    await first.locator(".faq-question").click();
    await expect(first).toHaveClass(/is-open/);
    await expect(first.locator(".faq-question")).toHaveAttribute("aria-expanded", "true");
    await expect(question).toHaveAttribute("aria-expanded", "false");
  });

  test("UX-AC-05: Tab vào control đầu tiên có focus-visible outline rõ ràng", async ({ page }) => {
    await page.keyboard.press("Tab");

    const focus = await page.evaluate(() => {
      const el = document.activeElement;
      if (!el) return null;
      const styles = getComputedStyle(el);
      return {
        tag: el.tagName.toLowerCase(),
        outlineStyle: styles.outlineStyle,
        outlineWidth: Number.parseFloat(styles.outlineWidth),
        outlineOffset: Number.parseFloat(styles.outlineOffset)
      };
    });

    expect(focus).not.toBeNull();
    expect(focus?.outlineStyle).not.toBe("none");
    expect(focus?.outlineWidth).toBeGreaterThanOrEqual(2);
    expect(focus?.outlineOffset).toBeGreaterThanOrEqual(2);
  });

  test("UX-AC-06: prefers-reduced-motion tắt animation và reveal hiện ngay", async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.goto("/website/");

    // Không cuộn: reveal phải hiện ngay
    const cards = page.locator(".pillar-card");
    await expect(cards).toHaveCount(3);
    await expect.poll(() => computedOpacity(page, ".pillar-card")).toBe(1);

    const pulse = await page.locator(".pulse-dot").evaluate((el) => {
      const styles = getComputedStyle(el);
      return { animationName: styles.animationName };
    });
    expect(pulse.animationName).toBe("none");

    const scrollBehavior = await page.evaluate(
      () => getComputedStyle(document.documentElement).scrollBehavior
    );
    expect(scrollBehavior).toBe("auto");

    const transitionDuration = await cards.first().evaluate((el) => {
      const styles = getComputedStyle(el);
      return styles.transitionDuration.split(",")[0].trim();
    });
    expect(Number.parseFloat(transitionDuration)).toBeLessThanOrEqual(0.05);
  });
});

test.describe("Website mobile drawer", () => {
  test.use({ viewport: { width: 390, height: 844 } });

  test.beforeEach(async ({ page }) => {
    await page.goto("/website/");
  });

  test("UX-AC-04: drawer mở bằng transform, khóa scroll body, Escape đóng", async ({ page }) => {
    const toggle = page.locator("#navToggle");
    const menu = page.locator("#mobileMenu");

    await expect(toggle).toBeVisible();
    await expect(menu).toBeHidden();

    await toggle.click();
    await expect(menu).toBeVisible();
    await expect(toggle).toHaveAttribute("aria-expanded", "true");
    await expect
      .poll(() => page.evaluate(() => getComputedStyle(document.body).overflowY))
      .toBe("hidden");

    const openState = await menu.evaluate((el) => {
      const styles = getComputedStyle(el);
      return {
        display: styles.display,
        visibility: styles.visibility,
        opacity: Number(styles.opacity)
      };
    });
    expect(openState.display).not.toBe("none");
    expect(openState.visibility).toBe("visible");
    await expect.poll(() => menu.evaluate((el) => Number(getComputedStyle(el).opacity))).toBe(1);

    await page.keyboard.press("Escape");
    await expect(menu).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
    await expect
      .poll(() => page.evaluate(() => getComputedStyle(document.body).overflowY))
      .toBe("auto");
  });

  test("UX-AC-04: click ra ngoài và chọn link đều đóng drawer", async ({ page }) => {
    const toggle = page.locator("#navToggle");
    const menu = page.locator("#mobileMenu");

    await toggle.click();
    await expect(menu).toBeVisible();

    // Click vùng navbar (ngoài drawer) để đóng
    await page.locator("header.navbar").click({ position: { x: 8, y: 8 } });
    await expect(menu).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");

    // Mở lại rồi chọn link trong drawer
    await toggle.click();
    await expect(menu).toBeVisible();
    await menu.locator("a.mobile-link").first().click();
    await expect(menu).toBeHidden();
    await expect(toggle).toHaveAttribute("aria-expanded", "false");
    await expect
      .poll(() => page.evaluate(() => getComputedStyle(document.body).overflowY))
      .toBe("auto");
  });

  test("UX-AC-11: không tràn ngang 390px, navToggle trong viewport, CTA vẫn có trong drawer", async ({ page }) => {
    const viewportWidth = 390;

    // Không tràn ngang (tolerance 1px cho sub-pixel)
    const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
    expect(scrollWidth).toBeLessThanOrEqual(viewportWidth + 1);

    // Nút mở menu nằm trọn trong viewport, không bị navbar đẩy ra ngoài
    const toggle = page.locator("#navToggle");
    await expect(toggle).toBeVisible();
    const toggleBox = await toggle.boundingBox();
    expect(toggleBox).not.toBeNull();
    expect(toggleBox!.x).toBeGreaterThanOrEqual(0);
    expect(toggleBox!.x + toggleBox!.width).toBeLessThanOrEqual(viewportWidth);

    // CTA header trùng lặp bị ẩn ở mobile...
    await expect(page.locator(".nav-actions > a.btn-primary")).toBeHidden();

    // ...nhưng CTA trong drawer vẫn hiển thị và dùng được
    await toggle.click();
    const menu = page.locator("#mobileMenu");
    await expect(menu).toBeVisible();
    const drawerCta = menu.locator("a.btn-primary.mobile-link");
    await expect(drawerCta).toBeVisible();
    await expect(drawerCta).toContainText("Tải V.A.Z Launcher");
  });
});

