import { test, expect, Page } from "@playwright/test";

async function bootAndOpenSettings(page: Page) {
  await page.goto("/");
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.locator("#page-instances")).toBeVisible();
  // Open first instance: "Test Fabric" (MC 1.21.1)
  await page.locator(".instance-card").first().click({ force: true });
  await expect(page.locator("#page-detail")).toBeVisible();
  // Switch to settings tab
  await page.locator('.tab[data-tab="settings"]').click({ force: true });
  await expect(page.locator("#tab-settings")).toBeVisible();
}

test.describe("Java Resolver & JVM Memory Manager Runtime Tests", () => {
  test.beforeEach(async ({ page }) => {
    await bootAndOpenSettings(page);
  });

  test("JMM-AC-01: Danh sách Java Runtime tự động phát hiện hiển thị Vendor, Version, Path, Arch và badge Khuyến nghị", async ({ page }) => {
    // 1. Selector dropdown contains detected runtimes
    const select = page.locator("#javaRuntimeSelect");
    await expect(select).toBeVisible();
    await expect(select.locator('option[value="java-21"]')).toContainText("Java 21");
    await expect(select.locator('option[value="java-17"]')).toContainText("Java 17");
    await expect(select.locator('option[value="java-8"]')).toContainText("Java 8");

    // 2. Runtime cards show vendor, arch, JDK/JRE, path
    const cards = page.locator(".java-runtime-card");
    await expect(cards).toHaveCount(3);

    const java21Card = page.locator('.java-runtime-card[data-runtime-id="java-21"]');
    await expect(java21Card).toBeVisible();
    await expect(java21Card).toContainText("Eclipse Temurin 21.0.4");
    await expect(java21Card).toContainText("x64");
    await expect(java21Card).toContainText("JDK");
    await expect(java21Card).toContainText("Adoptium");

    // 3. Recommended badge matches MC 1.21.1 -> Java 21 has badge "Khuyến nghị"
    const recBadge = java21Card.locator(".java-rec-badge, .badge-recommended");
    await expect(recBadge).toBeVisible();
    await expect(recBadge).toContainText("Khuyến nghị");

    // Java 17 and Java 8 do NOT have recommended badge for MC 1.21.1
    const java17Card = page.locator('.java-runtime-card[data-runtime-id="java-17"]');
    await expect(java17Card.locator(".java-rec-badge, .badge-recommended")).toHaveCount(0);
  });

  test("JMM-AC-02: Cảnh báo Java không tương thích xuất hiện khi chọn sai runtime và 1-click sửa", async ({ page }) => {
    // Select Java 17 for MC 1.21.1 instance
    await page.locator("#javaRuntimeSelect").selectOption("java-17");

    // Incompatible warning banner must appear
    const banner = page.locator("#javaIncompatibleBanner");
    await expect(banner).toBeVisible();
    await expect(banner).toContainText("Phiên bản Java không tương thích");
    await expect(banner).toContainText("Java 17");

    // Click 1-click "Dùng Java khuyến nghị"
    const fixBtn = page.locator("#btnUseRecommendedJava");
    await expect(fixBtn).toBeVisible();
    await fixBtn.click({ force: true });

    // Warning banner should be hidden and Java 21 selected
    await expect(banner).toBeHidden();
    await expect(page.locator("#javaRuntimeSelect")).toHaveValue("java-21");
  });

  test("JMM-AC-03: Nhập đường dẫn Java tùy biến với nút Kiểm tra (Test Java) probe version", async ({ page }) => {
    const input = page.locator("#setJavaCustomPath");
    const testBtn = page.locator("#btnTestJava");
    const result = page.locator("#javaProbeResult");

    // 1. Valid custom path
    await input.fill("C:\\Program Files\\Adoptium\\jdk-21.0.4\\bin\\javaw.exe");
    await testBtn.click({ force: true });
    await expect(result).toBeVisible();
    await expect(result).toHaveClass(/is-success/);
    await expect(result).toContainText("Hợp lệ");
    await expect(result).toContainText("Java 21");

    // 2. Invalid path
    await input.fill("C:\\invalid\\path\\fake_file.txt");
    await testBtn.click({ force: true });
    await expect(result).toBeVisible();
    await expect(result).toHaveClass(/is-error/);
    await expect(result).toContainText("Đường dẫn không hợp lệ");
  });

  test("JMM-AC-04: 1-Click Cài đặt Java: nút Tải Java 21 (Temurin) hiển thị tiến trình và kích hoạt runtime", async ({ page }) => {
    // Switch to Java 8 first
    await page.locator("#javaRuntimeSelect").selectOption("java-8");

    const dlBtn = page.locator("#btnDownloadJava");
    await expect(dlBtn).toBeVisible();
    await dlBtn.click({ force: true });

    // Progress bar wrap appears
    const progressWrap = page.locator("#javaDownloadProgressWrap");
    await expect(progressWrap).toBeVisible();

    // After simulation finishes, Java 21 becomes selected
    await expect(page.locator("#javaRuntimeSelect")).toHaveValue("java-21", { timeout: 10_000 });
    // Toast notification visible
    await expect(page.locator(".toast", { hasText: "Đã tải và cài đặt Java 21" })).toBeVisible();
  });

  test("JMM-AC-05: Điều chỉnh RAM (Min/Max RAM) đồng bộ giữa slider và input, các preset nhanh", async ({ page }) => {
    const maxSlider = page.locator("#setMaxRam");
    const maxInput = page.locator("#setMaxRamValue");
    const minSlider = page.locator("#setMinRam");
    const minInput = page.locator("#setMinRamValue");

    // 1. Slider Max RAM -> Input Max RAM sync
    await maxSlider.fill("6144");
    await maxSlider.dispatchEvent("input");
    await expect(maxInput).toHaveValue("6144");

    // 2. Input Max RAM -> Slider Max RAM sync
    await maxInput.fill("8192");
    await maxInput.dispatchEvent("input");
    await expect(maxSlider).toHaveValue("8192");

    // 3. Quick preset chips (2 GB, 4 GB, 8 GB, 12 GB)
    const chip2gb = page.locator('.ram-preset-chip[data-ram="2048"]');
    await chip2gb.click({ force: true });
    await expect(maxInput).toHaveValue("2048");
    await expect(chip2gb).toHaveClass(/is-selected/);

    const chip12gb = page.locator('.ram-preset-chip[data-ram="12288"]');
    await chip12gb.click({ force: true });
    await expect(maxInput).toHaveValue("12288");
    await expect(chip12gb).toHaveClass(/is-selected/);

    // 4. Min RAM sync
    await minSlider.fill("2048");
    await minSlider.dispatchEvent("input");
    await expect(minInput).toHaveValue("2048");
  });

  test("JMM-AC-06: Biểu đồ thanh RAM hệ thống phân bổ và cảnh báo an toàn khi > 85% hoặc < 1024 MB", async ({ page }) => {
    const maxInput = page.locator("#setMaxRamValue");
    const warnBanner = page.locator("#ramWarningBanner");

    // 1. Safe RAM allocation (4096 MB) -> No warning
    await maxInput.fill("4096");
    await maxInput.dispatchEvent("input");
    await expect(warnBanner).toBeHidden();

    // Check system memory gauge segments exist
    const gauge = page.locator("#sysRamGauge");
    await expect(gauge).toBeVisible();
    await expect(page.locator("#ramSegOs")).toBeVisible();
    await expect(page.locator("#ramSegMc")).toBeVisible();
    await expect(page.locator("#ramSegFree")).toBeVisible();

    // 2. High RAM allocation (> 85% of 16384 MB = > 13926 MB, e.g. 14336 MB) -> Warning visible
    await maxInput.fill("14336");
    await maxInput.dispatchEvent("input");
    await expect(warnBanner).toBeVisible();
    await expect(warnBanner).toContainText("quá 85% RAM hệ thống");

    // 3. Low RAM allocation (< 1024 MB, e.g. 512 MB) -> Under allocation warning
    await maxInput.fill("512");
    await maxInput.dispatchEvent("input");
    await expect(warnBanner).toBeVisible();
    await expect(warnBanner).toContainText("dưới 1024 MB");
  });

  test("JMM-AC-07: Chọn GC Preset tự động điền cờ JVM tương ứng", async ({ page }) => {
    const jvmTextarea = page.locator("#setJvm");

    // 1. ZGC Generational
    const zgcChip = page.locator('.gc-preset-chip[data-gc="zgc"]');
    await zgcChip.scrollIntoViewIfNeeded();
    await zgcChip.click();
    await expect(zgcChip).toHaveClass(/is-selected/);
    await expect(jvmTextarea).toHaveValue("-XX:+UseZGC -XX:+ZGenerational");

    // 2. Shenandoah
    const shenChip = page.locator('.gc-preset-chip[data-gc="shenandoah"]');
    await shenChip.scrollIntoViewIfNeeded();
    await shenChip.click();
    await expect(shenChip).toHaveClass(/is-selected/);
    await expect(jvmTextarea).toHaveValue("-XX:+UseShenandoahGC");

    // 3. G1GC (Default)
    const g1gcChip = page.locator('.gc-preset-chip[data-gc="g1gc"]');
    await g1gcChip.scrollIntoViewIfNeeded();
    await g1gcChip.click();
    await expect(g1gcChip).toHaveClass(/is-selected/);
    await expect(jvmTextarea).toHaveValue("-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions");
  });

  test("JMM-AC-08: Lưu cấu hình vào localStorage (reload giữ nguyên) và Mặc định khôi phục", async ({ page }) => {
    // Modify settings: RAM 8192, ZGC, Save
    const chip8gb = page.locator('.ram-preset-chip[data-ram="8192"]');
    await chip8gb.click({ force: true });

    const zgcChip = page.locator('.gc-preset-chip[data-gc="zgc"]');
    await zgcChip.click({ force: true });

    const saveBtn = page.locator("#saveSettingsBtn");
    await saveBtn.click({ force: true });
    await expect(page.locator(".toast", { hasText: "Đã lưu cài đặt" })).toBeVisible();

    // Reload page and navigate back to settings
    await page.reload();
    await page.locator(".instance-card").first().click({ force: true });
    await page.locator('.tab[data-tab="settings"]').click({ force: true });

    // Assert values persisted
    await expect(page.locator("#setMaxRamValue")).toHaveValue("8192");
    await expect(page.locator("#setJvm")).toHaveValue("-XX:+UseZGC -XX:+ZGenerational");

    // Click Mặc định (Reset)
    const resetBtn = page.locator("#resetSettingsBtn");
    await resetBtn.click({ force: true });
    await expect(page.locator(".toast", { hasText: "Đã khôi phục cài đặt mặc định" })).toBeVisible();

    // Verify reset to defaults
    await expect(page.locator("#setMaxRamValue")).toHaveValue("4096");
    await expect(page.locator("#setJvm")).toHaveValue("-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions");
  });

  test("JMM-AC-09: Responsive 390px và 620px không tràn ngang, focus visible rõ ràng", async ({ page }) => {
    // 1. Tablet breakpoint (620px)
    await page.setViewportSize({ width: 620, height: 900 });
    const isOverflowing620 = await page.evaluate(() => {
      const el = document.querySelector("#tab-settings");
      return el ? el.scrollWidth > el.clientWidth : false;
    });
    expect(isOverflowing620).toBe(false);

    // 2. Mobile breakpoint (390px)
    await page.setViewportSize({ width: 390, height: 844 });
    const isOverflowing390 = await page.evaluate(() => {
      const el = document.querySelector("#tab-settings");
      return el ? el.scrollWidth > el.clientWidth : false;
    });
    expect(isOverflowing390).toBe(false);

    // 3. Focus visible ring on controls
    const chip = page.locator(".ram-preset-chip").first();
    await chip.focus();
    const hasFocus = await chip.evaluate((el) => {
      return document.activeElement === el;
    });
    expect(hasFocus).toBe(true);
  });
});
