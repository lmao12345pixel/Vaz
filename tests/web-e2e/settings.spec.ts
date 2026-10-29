import { test, expect, type Page } from "@playwright/test";

async function bootToSettings(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();

  // If mobile viewport, open nav drawer
  const toggle = page.locator("#mobileNavToggle");
  if (await toggle.isVisible()) {
    const isNavOpen = await page.evaluate(() => document.documentElement.classList.contains("nav-open"));
    if (!isNavOpen) {
      await toggle.click({ force: true });
    }
  }

  // Click Settings nav
  const settingsNav = page.locator('.nav-item[data-screen="settings"]');
  await settingsNav.click({ force: true });
  await expect(page.locator("#page-settings")).toBeVisible();
}

test.describe("Settings: Comprehensive Modern Settings Suite (settings.contract.md)", () => {
  test.beforeEach(async ({ page }) => {
    await bootToSettings(page);
  });

  test("SET-AC-01: Điều hướng danh mục lọc chính xác thẻ cài đặt, tab Tất cả hiển thị toàn bộ", async ({ page }) => {
    const chips = page.locator("#settingsCategoryChips .chip");
    await expect(chips).toHaveCount(8);

    // Click 'launcher' tab
    await page.locator('#settingsCategoryChips .chip[data-settings-group="launcher"]').click();
    await expect(page.locator('.settings-card[data-settings-group="launcher"]')).toBeVisible();
    await expect(page.locator('.settings-card[data-settings-group="java"]')).toBeHidden();

    // Click 'java' tab
    await page.locator('#settingsCategoryChips .chip[data-settings-group="java"]').click();
    await expect(page.locator('.settings-card[data-settings-group="java"]')).toBeVisible();
    await expect(page.locator('.settings-card[data-settings-group="launcher"]')).toBeHidden();

    // Click 'all' tab
    await page.locator('#settingsCategoryChips .chip[data-settings-group="all"]').click();
    await expect(page.locator('.settings-card[data-settings-group="launcher"]')).toBeVisible();
    await expect(page.locator('.settings-card[data-settings-group="java"]')).toBeVisible();
    await expect(page.locator('.settings-card[data-settings-group="appearance"]')).toBeVisible();
  });

  test("SET-AC-02: Cấu hình Java & RAM: Slider và input đồng bộ 2 chiều, clamp [1024, 16384] MB, preset JVM và dò Java", async ({ page }) => {
    // Check max RAM input sync
    await page.locator("#globalRamValue").fill("8192");
    await page.locator("#globalRamValue").dispatchEvent("input");
    await expect(page.locator("#globalRam")).toHaveValue("8192");

    // Slider input sync
    await page.locator("#globalRam").fill("6144");
    await page.locator("#globalRam").dispatchEvent("input");
    await expect(page.locator("#globalRamValue")).toHaveValue("6144");

    // Save and check clamping
    await page.locator("#globalRamValue").fill("99999");
    await page.locator("#globalRamValue").dispatchEvent("input");
    await page.locator("#saveGlobalSettingsBtn").click();

    // Verify clamped to 16384 in state
    const savedRam = await page.evaluate(() => {
      const state = JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}");
      return state.globalSettings?.ramMb;
    });
    expect(savedRam).toBe(16384);

    // Change JVM preset to aikar
    await page.locator("#settingJvmPreset").selectOption("aikar");
    const jvmArgs = await page.locator("#settingJvmArgs").inputValue();
    expect(jvmArgs).toContain("MaxGCPauseMillis=200");
    expect(jvmArgs).toContain("AlwaysPreTouch");

    // Auto-detect Java
    await page.locator("#detectJavaBtn").click();
    const javaPath = await page.locator("#globalJavaPath").inputValue();
    expect(javaPath).toContain("jdk-21");
    await expect(page.locator(".toast").filter({ hasText: "Java 21 LTS" })).toBeVisible();
  });

  test("SET-AC-03: Thiết lập kích thước cửa sổ game (Resolution Width x Height) và tùy chọn Khởi chạy toàn màn hình (Fullscreen) cập nhật và lưu trữ đúng", async ({ page }) => {
    await page.locator("#settingWindowWidth").fill("1920");
    await page.locator("#settingWindowHeight").fill("1080");
    await page.locator("#settingStartFullscreen").check();
    await page.locator("#saveGlobalSettingsBtn").click();

    const saved = await page.evaluate(() => {
      const state = JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}");
      return {
        w: state.globalSettings?.windowWidth,
        h: state.globalSettings?.windowHeight,
        fs: state.globalSettings?.startFullscreen,
      };
    });
    expect(saved.w).toBe(1920);
    expect(saved.h).toBe(1080);
    expect(saved.fs).toBe(true);
  });

  test("SET-AC-04: Giao diện: Chuyển đổi Theme Dark / Light / Obsidian áp dụng ngay lên data-theme; Bảng màu nhấn (Accent color) đổi biến --accent", async ({ page }) => {
    // Pick violet accent
    await page.locator('.accent-swatch[data-accent="violet"]').click();
    await expect(page.locator('.accent-swatch[data-accent="violet"]')).toHaveClass(/is-selected/);

    const accentVar = await page.evaluate(() => {
      return getComputedStyle(document.documentElement).getPropertyValue("--accent").trim();
    });
    expect(accentVar).toBe("#8b5cf6");

    // Change theme to obsidian
    await page.locator("#globalTheme").selectOption("obsidian");
    const htmlTheme = await page.evaluate(() => document.documentElement.dataset.theme);
    expect(htmlTheme).toBe("obsidian");

    // Save and check persistence
    await page.locator("#saveGlobalSettingsBtn").click();
    const savedAccent = await page.evaluate(() => {
      const state = JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}");
      return state.globalSettings?.accentColor;
    });
    expect(savedAccent).toBe("violet");
  });

  test("SET-AC-05: Công tắc Giảm chuyển động (Reduce motion) áp dụng class reduce-motion; Âm thanh UI lưu vào state", async ({ page }) => {
    await page.locator("#globalReduceMotion").check();
    await page.locator("#globalReduceMotion").dispatchEvent("change");
    const isReduced = await page.evaluate(() => document.documentElement.classList.contains("reduce-motion"));
    expect(isReduced).toBe(true);

    await page.locator("#settingUiSound").uncheck();
    await page.locator("#saveGlobalSettingsBtn").click();

    const saved = await page.evaluate(() => {
      const state = JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}");
      return state.globalSettings?.uiSoundEnabled;
    });
    expect(saved).toBe(false);
  });

  test("SET-AC-06: Cấu hình Mạng & Tải xuống: Tùy chỉnh số luồng tải đồng thời, CDN mirror và giới hạn băng thông lưu chính xác", async ({ page }) => {
    // Default mirror phải khớp một option có thật (không rỗng)
    await expect(page.locator("#settingDownloadMirror")).toHaveValue("cloudflare");

    await page.locator("#settingDownloadThreads").selectOption("16");
    await page.locator("#settingDownloadMirror").selectOption("cloudflare");
    await page.locator("#settingBandwidthLimit").selectOption("10mb");
    await page.locator("#saveGlobalSettingsBtn").click();

    const saved = await page.evaluate(() => {
      const state = JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}");
      return {
        threads: state.globalSettings?.downloadThreads,
        mirror: state.globalSettings?.downloadMirror,
        limit: state.globalSettings?.bandwidthLimit,
      };
    });
    expect(saved.threads).toBe(16);
    expect(saved.mirror).toBe("cloudflare");
    expect(saved.limit).toBe("10mb");
  });

  test("SET-AC-07: Discord Rich Presence & Quyền riêng tư: Bật / tắt Discord RPC và tùy biến trạng thái in-game phản ánh tức thì vào Live Discord Preview", async ({ page }) => {
    await page.locator("#discordStatusInput").fill("Đang xây lâu đài Nether");
    await page.locator("#discordStatusInput").dispatchEvent("input");

    await expect(page.locator("#discordCustomText")).toHaveText("Đang xây lâu đài Nether");

    await page.locator("#saveGlobalSettingsBtn").click();
    const saved = await page.evaluate(() => {
      const state = JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}");
      return state.globalSettings?.discordStatus;
    });
    expect(saved).toBe("Đang xây lâu đài Nether");
  });

  test("SET-AC-08: Lưu trữ & Dọn dẹp cache: Bấm 'Xóa cache tải xuống' mở dialog xác nhận, giải phóng bộ nhớ và cập nhật bộ đếm 0 MB", async ({ page }) => {
    await page.locator("#clearCacheBtn").click();

    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();

    // Confirm dialog
    await dialogHost.locator("[data-dialog-primary]").click();

    // Wait for task completion
    await expect(page.locator("#settingCacheSizeLabel")).toHaveText("0 MB");
    await expect(page.locator(".toast").filter({ hasText: "Đã xóa cache tải xuống" })).toBeVisible();
  });

  test("SET-AC-09: Thông tin hệ thống & Báo cáo chẩn đoán: Hiển thị đầy đủ thông số OS, CPU, RAM, GPU, Java; kiểm tra cập nhật và xuất log chẩn đoán", async ({ page }) => {
    // Assert system spec elements
    await expect(page.locator("#specOs")).toBeVisible();
    await expect(page.locator("#specCpu")).toBeVisible();
    await expect(page.locator("#specGpu")).toBeVisible();
    await expect(page.locator("#specRam")).toBeVisible();
    await expect(page.locator("#specJava")).toBeVisible();

    // Check update
    await page.locator("#checkForUpdatesBtn").click();
    await expect(page.locator(".toast").filter({ hasText: "V.A.Z v2.4.0 đang là phiên bản mới nhất" })).toBeVisible();

    // Export diagnostic
    await page.locator("#exportDiagnosticBtn").click();
    await expect(page.locator(".toast").filter({ hasText: "Đã xuất báo cáo chẩn đoán" })).toBeVisible();
  });

  test("SET-AC-10: Nút Khôi phục mặc định mở Dialog xác nhận; xác nhận xong đưa toàn bộ thiết lập về giá trị khởi tạo; nút Lưu cài đặt lưu vào localStorage bền vững", async ({ page }) => {
    // Modify settings first
    await page.locator("#globalRamValue").fill("8192");
    await page.locator("#globalRamValue").dispatchEvent("input");
    await page.locator("#settingWindowWidth").fill("1600");
    await page.locator("#globalTheme").selectOption("light");
    await page.locator("#saveGlobalSettingsBtn").click();

    // Verify modified in state
    let state = await page.evaluate(() => JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}"));
    expect(state.globalSettings?.ramMb).toBe(8192);
    expect(state.globalSettings?.windowWidth).toBe(1600);
    expect(state.globalSettings?.theme).toBe("light");

    // Click Reset to Default button
    await page.locator("#resetGlobalSettingsBtn").click();

    // Expect confirmation dialog
    const dialogHost = page.locator("#dialogHost");
    await expect(dialogHost).toBeVisible();
    await expect(dialogHost).toContainText("Khôi phục cài đặt mặc định?");

    // Confirm dialog phải có nhãn primary "Xác nhận" (không dùng default "Xóa")
    await expect(dialogHost.locator("[data-dialog-primary]")).toHaveText("Xác nhận");
    await dialogHost.locator("[data-dialog-primary]").click();

    // Expect toast confirmation
    await expect(page.locator(".toast").filter({ hasText: "Đã khôi phục cài đặt mặc định" })).toBeVisible();

    // Check inputs restored to defaults
    await expect(page.locator("#globalRamValue")).toHaveValue("4096");
    await expect(page.locator("#settingWindowWidth")).toHaveValue("1280");
    await expect(page.locator("#globalTheme")).toHaveValue("dark");

    // Check localStorage restored to defaults
    state = await page.evaluate(() => JSON.parse(localStorage.getItem("vazer-demo-v1") || "{}"));
    expect(state.globalSettings?.ramMb).toBe(4096);
    expect(state.globalSettings?.windowWidth).toBe(1280);
    expect(state.globalSettings?.theme).toBe("dark");
  });

  test("SET-AC-11: Không hồi quy: Responsive (390px, 620px) không tràn ngang; CSS tuyệt đối không dùng transition: all", async ({ page }) => {
    for (const width of [620, 390]) {
      await page.setViewportSize({ width, height: 800 });
      const scrollWidth = await page.evaluate(() => document.documentElement.scrollWidth);
      const clientWidth = await page.evaluate(() => document.documentElement.clientWidth);
      expect(scrollWidth).toBeLessThanOrEqual(clientWidth + 1);
    }

    // Verify zero transition: all in all stylesheets
    const allViolations = await page.evaluate(() => {
      const bad = [];
      for (const sheet of document.styleSheets) {
        try {
          for (const rule of sheet.cssRules) {
            if (rule.style && (rule.style.transition?.includes("all") || rule.style.transitionProperty?.includes("all"))) {
              bad.push(rule.selectorText);
            }
          }
        } catch {
          // ignore external
        }
      }
      return bad;
    });
    expect(allViolations).toHaveLength(0);
  });
});
