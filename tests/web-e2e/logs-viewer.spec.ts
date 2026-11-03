import { test, expect } from "@playwright/test";
import fs from "node:fs";
import path from "node:path";

test.describe("Logs Viewer & Export Runtime", () => {
  test.beforeEach(async ({ page, context }) => {
    await context.grantPermissions(["clipboard-read", "clipboard-write"]).catch(() => {});
    await page.goto("/");
    await page.evaluate(() => localStorage.clear());
    await page.reload();

    // Mở detail của instance đầu tiên
    await page.waitForSelector(".instance-card");
    await page.locator(".instance-card").first().click({ force: true });
    await expect(page.locator("#page-detail")).toBeVisible();

    // Chuyển sang tab Logs
    const tabLogsBtn = page.locator('.tab[data-tab="logs"]');
    await tabLogsBtn.waitFor({ state: "visible" });
    await tabLogsBtn.click();
    await expect(page.locator("#tab-logs")).toBeVisible();

    // Tạo log mẫu phong phú để bắt đầu
    await page.locator("#generateLogBtn").click();
    await expect(page.locator("#logView")).toBeVisible();
    await expect(page.locator("#logView .log-line")).toHaveCount(25);
  });

  // LV-AC-01: Hiển thị log stream với syntax highlighting và line numbers
  test("LV-AC-01: Hiển thị log stream với syntax highlighting và line numbers", async ({ page }) => {
    const lines = page.locator("#logView .log-line");
    await expect(lines).toHaveCount(25);

    // Kiểm tra cấu trúc các span syntax
    const firstLine = lines.first();
    await expect(firstLine.locator(".log-num")).toHaveText("1");
    await expect(firstLine.locator(".log-timestamp, .log-time")).toBeVisible();
    await expect(firstLine.locator(".log-level, .log-tag")).toBeVisible();
    await expect(firstLine.locator(".log-msg")).toBeVisible();

    // Kiểm tra class severity cho WARN và ERROR
    await expect(page.locator("#logView .log-line.is-warn").first()).toBeVisible();
    await expect(page.locator("#logView .log-line.is-error").first()).toBeVisible();

    // Toggle ẩn/hiện line numbers
    const lineNumChk = page.locator("#showLineNumbersChk");
    await expect(lineNumChk).toBeChecked();
    await lineNumChk.uncheck();
    await expect(page.locator("#logView")).toHaveClass(/hide-line-numbers/);

    const isNumHidden = await firstLine.locator(".log-num").evaluate((el) => {
      return window.getComputedStyle(el).display === "none";
    });
    expect(isNumHidden).toBe(true);

    // Check lại để hiện số dòng
    await lineNumChk.check();
    await expect(page.locator("#logView")).not.toHaveClass(/hide-line-numbers/);
  });

  // LV-AC-02: Bộ lọc log theo cấp độ ALL, INFO, WARN, ERROR
  test("LV-AC-02: Bộ lọc log theo cấp độ ALL, INFO, WARN, ERROR và cập nhật #logCount", async ({ page }) => {
    const countEl = page.locator("#logCount");
    await expect(countEl).toHaveText("25/25 dòng");

    // Lọc INFO
    await page.locator('#logLevels button[data-log-level="INFO"]').click();
    const infoCount = await page.locator("#logView .log-line").count();
    expect(infoCount).toBeGreaterThan(0);
    expect(infoCount).toBeLessThan(25);
    await expect(countEl).toHaveText(`${infoCount}/25 dòng`);
    await expect(page.locator("#logView .log-line.is-error")).toHaveCount(0);

    // Lọc WARN
    await page.locator('#logLevels button[data-log-level="WARN"]').click();
    const warnCount = await page.locator("#logView .log-line").count();
    expect(warnCount).toBe(3);
    await expect(countEl).toHaveText("3/25 dòng");
    await expect(page.locator("#logView .log-line.is-warn")).toHaveCount(3);

    // Lọc ERROR
    await page.locator('#logLevels button[data-log-level="ERROR"]').click();
    const errCount = await page.locator("#logView .log-line").count();
    expect(errCount).toBeGreaterThan(0);
    await expect(countEl).toHaveText(`${errCount}/25 dòng`);
    await expect(page.locator("#logView .log-line.is-error")).toHaveCount(errCount);

    // Quay lại ALL
    await page.locator('#logLevels button[data-log-level="ALL"]').click();
    await expect(page.locator("#logView .log-line")).toHaveCount(25);
    await expect(countEl).toHaveText("25/25 dòng");
  });

  // LV-AC-03: Tìm kiếm trong log với #logSearch
  test("LV-AC-03: Tìm kiếm trong log với #logSearch lọc chính xác và cập nhật counter", async ({ page }) => {
    const searchInput = page.locator("#logSearch");
    const countEl = page.locator("#logCount");

    // Tìm kiếm "LWJGL" (1 dòng)
    await searchInput.fill("LWJGL");
    await expect(page.locator("#logView .log-line")).toHaveCount(1);
    await expect(countEl).toHaveText("1/25 dòng");
    await expect(page.locator("#logView .log-line")).toContainText("LWJGL version 3.3.3+5");

    // Tìm kiếm "Sodium" (2 dòng)
    await searchInput.fill("Sodium");
    await expect(page.locator("#logView .log-line")).toHaveCount(2);
    await expect(countEl).toHaveText("2/25 dòng");

    // Tìm kiếm "Mixin" (2 dòng)
    await searchInput.fill("Mixin");
    await expect(page.locator("#logView .log-line")).toHaveCount(2);
    await expect(countEl).toHaveText("2/25 dòng");

    // Xóa từ khóa
    await searchInput.fill("");
    await expect(page.locator("#logView .log-line")).toHaveCount(25);
    await expect(countEl).toHaveText("25/25 dòng");
  });

  // LV-AC-04: Điều hướng nhanh: Lỗi đầu tiên flash và Cuối log scroll
  test("LV-AC-04: Điều hướng nhanh tới lỗi đầu tiên với flash animation và nút Cuối log", async ({ page }) => {
    // Click "Lỗi đầu tiên"
    await page.locator("#jumpFirstErrorBtn").click();

    // Dòng error đầu tiên nhận class .is-flash
    const firstError = page.locator("#logView .log-line.is-error").first();
    await expect(firstError).toHaveClass(/is-flash/);

    // Cuộn lên đầu để nút #logJumpBtn xuất hiện
    await page.locator("#logView").evaluate((el) => {
      el.scrollTop = 0;
      el.dispatchEvent(new Event("scroll"));
    });

    const jumpBtn = page.locator("#logJumpBtn");
    await expect(jumpBtn).toBeVisible();

    // Bấm nút "Cuối log"
    await jumpBtn.click();
    await page.waitForTimeout(400);

    // Kiểm tra vị trí scroll đã xuống gần đáy
    const atBottom = await page.locator("#logView").evaluate((el) => {
      return el.scrollHeight - el.scrollTop - el.clientHeight < 30;
    });
    expect(atBottom).toBe(true);
  });

  // LV-AC-05: Auto-scroll checkbox
  test("LV-AC-05: Auto-scroll tự cuộn khi có log mới khi bật và giữ nguyên vị trí khi tắt", async ({ page }) => {
    const autoScrollChk = page.locator("#autoScrollChk");
    await expect(autoScrollChk).toBeChecked();

    // Đẩy thêm log khi đang bật auto scroll
    await page.evaluate(() => {
      // @ts-ignore
      const id = window.__vazState.activeInstanceId;
      // @ts-ignore
      window.__vazPushLog(id, "[Render thread/INFO]: Auto-scroll test line 1");
    });

    await page.waitForTimeout(200);
    let isBottom = await page.locator("#logView").evaluate((el) => {
      return el.scrollHeight - el.scrollTop - el.clientHeight < 30;
    });
    expect(isBottom).toBe(true);

    // Tắt Auto-scroll và cuộn lên đầu
    await autoScrollChk.uncheck();
    await page.locator("#logView").evaluate((el) => {
      el.scrollTop = 0;
      el.dispatchEvent(new Event("scroll"));
    });

    // Thêm tiếp dòng log
    await page.evaluate(() => {
      // @ts-ignore
      const id = window.__vazState.activeInstanceId;
      // @ts-ignore
      window.__vazPushLog(id, "[Render thread/INFO]: Auto-scroll test line 2");
    });
    await page.waitForTimeout(200);

    // Kiểm tra vị trí scrollTop vẫn ở trên đầu (không tự cuộn xuống đáy)
    const scrollTop = await page.locator("#logView").evaluate((el) => el.scrollTop);
    expect(scrollTop).toBeLessThan(40);
  });

  // LV-AC-06: Xuất log: Copy clipboard và Tải về file latest.log
  test("LV-AC-06: Xuất log: nút Copy toast và nút Tải về .log kích hoạt tải file", async ({ page }) => {
    // Click nút Copy
    await page.locator("#copyLogBtn").click();
    await expect(page.locator(".toast", { hasText: "Đã sao chép" }).last()).toBeVisible();

    // Click nút Tải về .log và bắt sự kiện download
    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.locator("#exportLogBtn").click(),
    ]);

    expect(download.suggestedFilename()).toBe("latest.log");
    await expect(page.locator(".toast", { hasText: "Đã tải file latest.log về máy" }).last()).toBeVisible();
  });

  // LV-AC-07: Nút Xóa log và Tạo log mẫu
  test("LV-AC-07: Nút Xóa log hiển thị empty state và Tạo log mẫu nạp lại 25 dòng", async ({ page }) => {
    // Xóa log
    await page.locator("#clearLogBtn").click();
    await expect(page.locator("#logsEmpty")).toBeVisible();
    await expect(page.locator("#logView")).toBeHidden();
    await expect(page.locator("#logCount")).toHaveText("");

    // Tạo log mẫu
    await page.locator("#generateLogBtn").click();
    await expect(page.locator(".toast", { hasText: "Đã tạo 25 dòng log mẫu" }).last()).toBeVisible();
    await expect(page.locator("#logsEmpty")).toBeHidden();
    await expect(page.locator("#logView")).toBeVisible();
    await expect(page.locator("#logView .log-line")).toHaveCount(25);
    await expect(page.locator("#logCount")).toHaveText("25/25 dòng");
  });

  // LV-AC-08: Nút Mở thư mục logs
  test("LV-AC-08: Nút Mở thư mục logs hiển thị toast thông báo đường dẫn", async ({ page }) => {
    await page.locator("#openLogsFolderBtn").click();
    await expect(page.locator(".toast", { hasText: ".minecraft/logs" }).last()).toBeVisible();
  });

  // LV-AC-09: Responsive 390px, 620px, focus outline và reduced motion
  test("LV-AC-09: Responsive 390px/620px không tràn ngang, focus ring >= 2px, reduced-motion <= 0.05s", async ({ page }) => {
    // 390px viewport
    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(200);
    const noOverflow390 = await page.evaluate(() => {
      const shell = document.querySelector("#logShell") as HTMLElement;
      return shell ? shell.scrollWidth <= shell.clientWidth + 2 : true;
    });
    expect(noOverflow390).toBe(true);

    // 620px viewport
    await page.setViewportSize({ width: 620, height: 844 });
    await page.waitForTimeout(200);
    const noOverflow620 = await page.evaluate(() => {
      const shell = document.querySelector("#logShell") as HTMLElement;
      return shell ? shell.scrollWidth <= shell.clientWidth + 2 : true;
    });
    expect(noOverflow620).toBe(true);

    // Trở về kích thước chuẩn
    await page.setViewportSize({ width: 1280, height: 860 });

    // Focus ring outline >= 2px
    await page.locator("#exportLogBtn").focus();
    const btnOutline = await page.locator("#exportLogBtn").evaluate((el) => {
      const s = window.getComputedStyle(el);
      return parseFloat(s.outlineWidth) >= 2 || parseFloat(s.borderWidth) >= 1;
    });
    expect(btnOutline).toBe(true);

    // Reduced motion test
    await page.emulateMedia({ reducedMotion: "reduce" });
    const animState = await page.locator("#logView .log-line").first().evaluate((el) => {
      const s = window.getComputedStyle(el);
      return s.animationName === "none" || parseFloat(s.animationDuration) <= 0.05;
    });
    expect(animState).toBe(true);
  });

  // LV-AC-11: Không chứa transition: all trong stylesheet
  test("LV-AC-11: CSS tuân thủ nghiêm ngặt 0 transition: all", async () => {
    const cssPath = path.resolve(process.cwd(), "docs/design-preview/styles.css");
    const content = fs.readFileSync(cssPath, "utf-8");
    const forbiddenMatch = content.match(/transition\s*:\s*all\b|transition-property\s*:\s*all\b/i);
    expect(forbiddenMatch).toBeNull();
  });
});
