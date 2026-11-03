/**
 * V.A.Z Motion Lab — Executable Specification & Interaction Engine
 * Follows docs/contracts/motion-system.contract.md
 */

const REDUCE_KEY = "vaz.motionLab.reduceMotion";
const THEME_KEY = "vaz.motionLab.theme";

// ---------- Helpers ----------
const $ = (sel, parent = document) => parent.querySelector(sel);
const $$ = (sel, parent = document) => Array.from(parent.querySelectorAll(sel));

function isReducedMotion() {
  return (
    document.documentElement.getAttribute("data-reduced-motion") === "true" ||
    document.documentElement.classList.contains("reduce-motion") ||
    window.matchMedia("(prefers-reduced-motion: reduce)").matches
  );
}

// ---------- 1. Token Table Population ----------
function updateTokensTable() {
  const rootStyle = getComputedStyle(document.documentElement);
  const rows = $$(".ml-token-row");
  rows.forEach((row) => {
    const token = row.dataset.token;
    if (!token) return;
    const rawVal = rootStyle.getPropertyValue(token).trim();
    const cell = $(".ml-token-val", row);
    if (cell) {
      cell.textContent = rawVal || "not found";
    }
  });
}

// ---------- 2. Reduce Motion & Theme Controls ----------
function initControls() {
  const root = document.documentElement;
  const reduceBtn = $("#mlReduceMotionToggle");
  const themeBtn = $("#mlThemeToggle");

  // Read initial reduced-motion
  let storedReduce = null;
  try {
    storedReduce = localStorage.getItem(REDUCE_KEY);
  } catch (_) {}

  const shouldReduce = storedReduce === "1";
  if (shouldReduce) {
    root.setAttribute("data-reduced-motion", "true");
    root.classList.add("reduce-motion");
  }

  function syncReduceUI() {
    const active = root.getAttribute("data-reduced-motion") === "true";
    if (reduceBtn) {
      reduceBtn.setAttribute("aria-checked", active ? "true" : "false");
      const stateEl = $(".ml-toggle-state", reduceBtn);
      if (stateEl) stateEl.textContent = active ? "BẬT" : "TẮT";
    }
  }

  if (reduceBtn) {
    reduceBtn.addEventListener("click", () => {
      const current = root.getAttribute("data-reduced-motion") === "true";
      const next = !current;
      if (next) {
        root.setAttribute("data-reduced-motion", "true");
        root.classList.add("reduce-motion");
      } else {
        root.removeAttribute("data-reduced-motion");
        root.classList.remove("reduce-motion");
      }
      try {
        localStorage.setItem(REDUCE_KEY, next ? "1" : "0");
      } catch (_) {}
      syncReduceUI();
      updateTokensTable();
    });
  }

  if (themeBtn) {
    themeBtn.addEventListener("click", () => {
      const cur = root.getAttribute("data-theme") || "dark";
      const next = cur === "dark" ? "light" : "dark";
      root.setAttribute("data-theme", next);
      try {
        localStorage.setItem(THEME_KEY, next);
      } catch (_) {}
      updateTokensTable();
    });
  }

  syncReduceUI();
}

// ---------- 3. Shared Element Transition (FLIP) ----------
function initSharedElementDemo() {
  const grid = $("#mlSharedGrid");
  const detail = $("#mlSharedDetail");
  const backBtn = $("#mlDetailBackBtn");
  const detailIcon = $("#mlDetailIconBox");
  const detailTitle = $("#mlDetailTitle");
  const detailGlyph = $("#mlDetailGlyph");
  let lastActiveCard = null;

  function openCard(card) {
    lastActiveCard = card;
    const cardIcon = $(".ml-card-icon-box", card);
    const cardTitle = $(".ml-card-name", card);
    const glyph = $(".ml-icon-glyph", card)?.textContent || "📦";

    // FIRST: Measure source rects
    const firstIcon = cardIcon.getBoundingClientRect();
    const firstTitle = cardTitle.getBoundingClientRect();

    // Change text/glyph in detail view
    if (detailGlyph) detailGlyph.textContent = glyph;
    if (detailTitle) detailTitle.textContent = cardTitle.textContent.trim();

    // Switch view
    grid.hidden = true;
    detail.hidden = false;

    // LAST: Measure target rects
    const lastIcon = detailIcon.getBoundingClientRect();
    const lastTitle = detailTitle.getBoundingClientRect();

    // INVERT: calculate deltas
    const dxIcon = firstIcon.left - lastIcon.left;
    const dyIcon = firstIcon.top - lastIcon.top;
    const scaleIcon = lastIcon.width > 0 ? firstIcon.width / lastIcon.width : 1;

    const dxTitle = firstTitle.left - lastTitle.left;
    const dyTitle = firstTitle.top - lastTitle.top;

    detailIcon.style.transition = "none";
    detailTitle.style.transition = "none";
    detailIcon.style.transform = `translate(${dxIcon}px, ${dyIcon}px) scale(${scaleIcon})`;
    detailTitle.style.transform = `translate(${dxTitle}px, ${dyTitle}px)`;

    // Force reflow
    void detailIcon.offsetWidth;

    // PLAY: Animate to natural position
    requestAnimationFrame(() => {
      const reduced = isReducedMotion();
      detailIcon.style.transition = reduced
        ? "none"
        : "transform var(--dur-slow, 380ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";
      detailTitle.style.transition = reduced
        ? "none"
        : "transform var(--dur-slow, 380ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";

      detailIcon.style.transform = "none";
      detailTitle.style.transform = "none";
    });
  }

  function closeDetail() {
    if (!lastActiveCard) {
      detail.hidden = true;
      grid.hidden = false;
      return;
    }

    const cardIcon = $(".ml-card-icon-box", lastActiveCard);
    const cardTitle = $(".ml-card-name", lastActiveCard);

    // FIRST: Measure detail rects
    const firstIcon = detailIcon.getBoundingClientRect();
    const firstTitle = detailTitle.getBoundingClientRect();

    // Switch views
    detail.hidden = true;
    grid.hidden = false;

    // LAST: Measure card rects
    const lastIcon = cardIcon.getBoundingClientRect();
    const lastTitle = cardTitle.getBoundingClientRect();

    const dxIcon = firstIcon.left - lastIcon.left;
    const dyIcon = firstIcon.top - lastIcon.top;
    const scaleIcon = lastIcon.width > 0 ? firstIcon.width / lastIcon.width : 1;

    const dxTitle = firstTitle.left - lastTitle.left;
    const dyTitle = firstTitle.top - lastTitle.top;

    cardIcon.style.transition = "none";
    cardTitle.style.transition = "none";
    cardIcon.style.transform = `translate(${dxIcon}px, ${dyIcon}px) scale(${scaleIcon})`;
    cardTitle.style.transform = `translate(${dxTitle}px, ${dyTitle}px)`;

    void cardIcon.offsetWidth;

    requestAnimationFrame(() => {
      const reduced = isReducedMotion();
      cardIcon.style.transition = reduced
        ? "none"
        : "transform var(--dur-base, 220ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";
      cardTitle.style.transition = reduced
        ? "none"
        : "transform var(--dur-base, 220ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";

      cardIcon.style.transform = "none";
      cardTitle.style.transform = "none";
    });
  }

  $$(".ml-instance-card", grid).forEach((card) => {
    card.addEventListener("click", () => openCard(card));
  });

  if (backBtn) {
    backBtn.addEventListener("click", closeDetail);
  }

  return {
    replay: () => {
      const firstCard = $(".ml-instance-card", grid);
      if (grid.hidden) {
        closeDetail();
        setTimeout(() => {
          if (firstCard) openCard(firstCard);
        }, 120);
      } else {
        if (firstCard) openCard(firstCard);
      }
    },
  };
}

// ---------- 4. Launch Sequence (4 Sequential Stages) ----------
function initLaunchDemo() {
  const launchBtn = $("#mlLaunchBtn");
  const cancelBtn = $("#mlLaunchCancelBtn");
  const overlay = $("#mlLaunchOverlay");
  const statusEl = $("#mlLaunchStatus");
  const summaryEl = $("#mlLaunchSummary");
  const stageRows = $$(".ml-stage-row", overlay);
  let isRunning = false;
  let timerId = null;

  function resetStages() {
    stageRows.forEach((row) => {
      row.setAttribute("data-state", "idle");
      const status = $(".ml-stage-status", row);
      if (status) status.textContent = "Chờ";
      const fill = $(".ml-progress-fill", row);
      if (fill) fill.style.width = "0%";
    });
    if (statusEl) statusEl.textContent = "Đang chuẩn bị...";
  }

  function runStage(index, callback) {
    if (!isRunning || index >= stageRows.length) {
      if (callback) callback();
      return;
    }

    const row = stageRows[index];
    row.setAttribute("data-state", "active");
    const status = $(".ml-stage-status", row);
    if (status) status.textContent = "Đang tải...";
    const fill = $(".ml-progress-fill", row);

    const reduced = isReducedMotion();
    const stageDuration = reduced ? 20 : 160;

    // Fill progress
    if (fill) fill.style.width = "100%";

    timerId = setTimeout(() => {
      if (!isRunning) return;
      row.setAttribute("data-state", "done");
      if (status) status.textContent = "Xong";
      runStage(index + 1, callback);
    }, stageDuration);
  }

  function startLaunch() {
    if (isRunning) return;
    isRunning = true;
    overlay.hidden = false;
    resetStages();

    const reduced = isReducedMotion();
    const startDelay = reduced ? 10 : 80;

    timerId = setTimeout(() => {
      runStage(0, () => {
        if (!isRunning) return;
        if (statusEl) statusEl.textContent = "Khởi chạy game thành công!";
        if (summaryEl) summaryEl.textContent = "Khởi chạy game thành công!";
        const closeDelay = reduced ? 30 : 350;
        timerId = setTimeout(() => {
          overlay.hidden = true;
          isRunning = false;
        }, closeDelay);
      });
    }, startDelay);
  }

  function cancelLaunch() {
    isRunning = false;
    if (timerId) clearTimeout(timerId);
    overlay.hidden = true;
    resetStages();
  }

  if (launchBtn) launchBtn.addEventListener("click", startLaunch);
  if (cancelBtn) cancelBtn.addEventListener("click", cancelLaunch);

  return {
    replay: () => {
      cancelLaunch();
      setTimeout(startLaunch, 50);
    },
  };
}

// ---------- 5. Microinteractions Demo ----------
function initMicroDemo() {
  const springBtn = $("#mlSpringBtn");
  const badge = $("#mlBadgeCount");
  const stage = $("#mlMicroStage");
  let count = 0;

  function trigger() {
    count++;
    if (badge) {
      badge.textContent = String(count);
      badge.classList.remove("is-pulsing");
      void badge.offsetWidth;
      badge.classList.add("is-pulsing");
    }

    if (springBtn) {
      springBtn.classList.remove("is-bumped");
      void springBtn.offsetWidth;
      springBtn.classList.add("is-bumped");
    }
  }

  if (springBtn) {
    springBtn.addEventListener("click", trigger);
  }

  return {
    replay: () => {
      if (stage) {
        stage.classList.remove("is-replaying");
        void stage.offsetWidth;
        stage.classList.add("is-replaying");
      }
      trigger();
    },
  };
}

// ---------- 6. Replay Handlers ----------
function initReplayButtons(sharedDemo, launchDemo, microDemo) {
  $$(".ml-replay-btn").forEach((btn) => {
    btn.addEventListener("click", () => {
      const demo = btn.dataset.demo;
      if (demo === "shared-element") sharedDemo.replay();
      else if (demo === "launch") launchDemo.replay();
      else if (demo === "micro") microDemo.replay();
    });
  });
}

// ---------- INIT ----------
document.addEventListener("DOMContentLoaded", () => {
  initControls();
  updateTokensTable();
  const sharedDemo = initSharedElementDemo();
  const launchDemo = initLaunchDemo();
  const microDemo = initMicroDemo();
  initReplayButtons(sharedDemo, launchDemo, microDemo);
});
