/**
 * V.A.Z Launcher Official Website Interactive Logic
 * Handles 3D WebGL Skin Viewer, Interactive Tabs, Sliders, FAQ Accordion, and Mobile Menu
 */

(function () {
  "use strict";

  // =========================================================================
  // 1. 3D WebGL Skin Viewer Engine (skinview3d)
  // =========================================================================
  const skinPresets = {
    steve: { name: "Steve", model: "classic", file: "assets/skins/steve.png" },
    alex: { name: "Alex", model: "slim", file: "assets/skins/alex.png" },
    ari: { name: "Ari", model: "slim", file: "assets/skins/ari.png" },
    kai: { name: "Kai", model: "classic", file: "assets/skins/kai.png" }
  };

  const animModes = [
    { id: "walk", label: "Đi bộ" },
    { id: "run", label: "Chạy nhanh" },
    { id: "fly", label: "Lượn Elytra" },
    { id: "idle", label: "Đứng yên" }
  ];

  let currentSkin = "steve";
  let currentAnimIndex = 0;
  let isAutoRotating = true;
  let skinViewer = null;

  // -------------------------------------------------------------------------
  // Motion preference (OS "Reduce motion")
  // -------------------------------------------------------------------------
  const reducedMotionQuery = window.matchMedia("(prefers-reduced-motion: reduce)");
  let prefersReducedMotion = reducedMotionQuery.matches;
  let revealObserver = null;
  let revealItems = [];
  let invalidateNavMetrics = null;

  function shouldAutoRotate() {
    return isAutoRotating && !prefersReducedMotion;
  }

  class CustomPoseAnimation {
    constructor(mode = "walk") {
      this.mode = mode;
      this.progress = 0;
      this.speed = 1.0;
    }

    update(player, delta) {
      if (!player?.skin) return;
      this.progress += delta * this.speed;
      const t = this.progress;

      if (this.mode === "walk") {
        const step = 7 * t;
        player.skin.leftLeg.rotation.x = 0.5 * Math.sin(step);
        player.skin.rightLeg.rotation.x = 0.5 * Math.sin(step + Math.PI);
        player.skin.leftArm.rotation.x = 0.5 * Math.sin(step + Math.PI);
        player.skin.rightArm.rotation.x = 0.5 * Math.sin(step);
        player.skin.head.rotation.y = 0.12 * Math.sin(step / 4);
        player.skin.head.rotation.x = 0.06 * Math.sin(step / 3);
      } else if (this.mode === "run") {
        const step = 12 * t;
        player.skin.leftLeg.rotation.x = 0.9 * Math.sin(step);
        player.skin.rightLeg.rotation.x = 0.9 * Math.sin(step + Math.PI);
        player.skin.leftArm.rotation.x = 0.9 * Math.sin(step + Math.PI);
        player.skin.rightArm.rotation.x = 0.9 * Math.sin(step);
        player.skin.body.rotation.x = 0.2;
        player.skin.head.rotation.y = 0.08 * Math.sin(step / 4);
      } else if (this.mode === "fly") {
        player.rotation.x = Math.PI / 3.4;
        player.position.y = Math.sin(t * 3) * 1.5;
        player.skin.head.rotation.x = -Math.PI / 4;
        player.skin.leftArm.rotation.x = 0.15;
        player.skin.leftArm.rotation.z = 0.3;
        player.skin.rightArm.rotation.x = 0.15;
        player.skin.rightArm.rotation.z = -0.3;
        player.skin.leftLeg.rotation.x = 0.2;
        player.skin.rightLeg.rotation.x = 0.2;
      } else {
        // Idle breathing
        player.rotation.x = 0;
        player.position.y = 0;
        player.skin.leftLeg.rotation.x = 0;
        player.skin.rightLeg.rotation.x = 0;
        player.skin.leftArm.rotation.x = 0;
        player.skin.rightArm.rotation.x = 0;
        player.skin.leftArm.rotation.z = 0.06 + Math.sin(t * 2) * 0.02;
        player.skin.rightArm.rotation.z = -0.06 - Math.sin(t * 2) * 0.02;
        player.skin.head.rotation.y = Math.sin(t * 1.2) * 0.08;
        player.skin.head.rotation.x = Math.sin(t * 1.5) * 0.04;
      }
    }
  }

  function initHeroSkinViewer() {
    const canvas = document.getElementById("heroSkinCanvas");
    const container = canvas ? canvas.parentElement : null;
    const fallback = document.getElementById("viewerFallback");

    if (!canvas || !container) return;

    if (!window.skinview3d || !window.skinview3d.SkinViewer) {
      if (fallback) fallback.classList.add("is-active");
      if (canvas) canvas.style.display = "none";
      return;
    }

    try {
      const width = container.clientWidth || 360;
      const height = container.clientHeight || 380;

      skinViewer = new window.skinview3d.SkinViewer({
        canvas: canvas,
        width: width,
        height: height,
        background: null,
        preserveDrawingBuffer: false,
        pixelRatio: "match-device"
      });

      skinViewer.autoRotate = shouldAutoRotate();
      skinViewer.autoRotateSpeed = 0.45;
      skinViewer.controls.enablePan = false;
      skinViewer.controls.enableDamping = true;
      skinViewer.controls.dampingFactor = 0.08;
      skinViewer.controls.rotateSpeed = 0.7;
      skinViewer.controls.zoomSpeed = 0.7;
      skinViewer.globalLight.intensity = 2.4;
      skinViewer.cameraLight.intensity = 0.8;

      if (skinViewer.fxaaPass) {
        skinViewer.fxaaPass.enabled = false;
      }

      loadSkin(currentSkin);
      applyAnimation();

      // Resize listener
      if ("ResizeObserver" in window) {
        const ro = new ResizeObserver(() => {
          if (skinViewer && container.clientWidth > 0 && container.clientHeight > 0) {
            skinViewer.setSize(container.clientWidth, container.clientHeight);
          }
        });
        ro.observe(container);
      } else {
        window.addEventListener("resize", () => {
          if (skinViewer && container.clientWidth > 0 && container.clientHeight > 0) {
            skinViewer.setSize(container.clientWidth, container.clientHeight);
          }
        });
      }
    } catch {
      if (fallback) fallback.classList.add("is-active");
      if (canvas) canvas.style.display = "none";
    }
  }

  function loadSkin(skinKey) {
    if (!skinViewer) return;
    const preset = skinPresets[skinKey] || skinPresets.steve;
    currentSkin = skinKey;

    try {
      skinViewer.loadSkin(preset.file, { model: preset.model }).catch(() => {});
    } catch {
      // Ignored
    }
  }

  function applyAnimation() {
    if (!skinViewer) return;
    const animMode = animModes[currentAnimIndex].id;
    try {
      if (skinViewer.playerObject) {
        if (typeof skinViewer.playerObject.resetJoints === "function") {
          skinViewer.playerObject.resetJoints();
        }
        skinViewer.playerObject.rotation.set(0, 0, 0);
        skinViewer.playerObject.position.set(0, 0, 0);

        if (animMode === "fly") {
          skinViewer.playerObject.backEquipment = "elytra";
        } else {
          skinViewer.playerObject.backEquipment = null;
        }
      }
      skinViewer.animation = new CustomPoseAnimation(animMode);
    } catch {
      // Ignored
    }

    const btnAnimToggle = document.getElementById("btnAnimToggle");
    if (btnAnimToggle) {
      btnAnimToggle.querySelector("span").textContent = animModes[currentAnimIndex].label;
    }
  }

  // Bind Skin Controls
  function setupSkinControls() {
    const skinButtons = document.querySelectorAll(".skin-btn");
    skinButtons.forEach((btn) => {
      btn.addEventListener("click", () => {
        const skin = btn.dataset.skin;
        if (!skin || skin === currentSkin) return;

        skinButtons.forEach((b) => b.classList.remove("is-active"));
        btn.classList.add("is-active");
        loadSkin(skin);
      });
    });

    const btnAnimToggle = document.getElementById("btnAnimToggle");
    if (btnAnimToggle) {
      btnAnimToggle.addEventListener("click", () => {
        currentAnimIndex = (currentAnimIndex + 1) % animModes.length;
        applyAnimation();
      });
    }

    const btnRotateToggle = document.getElementById("btnRotateToggle");
    if (btnRotateToggle) {
      btnRotateToggle.addEventListener("click", () => {
        isAutoRotating = !isAutoRotating;
        if (skinViewer) {
          skinViewer.autoRotate = shouldAutoRotate();
        }
        btnRotateToggle.classList.toggle("is-active", isAutoRotating);
      });
    }
  }

  // =========================================================================
  // 2. Interactive Showcase Tabs
  // =========================================================================
  function setupShowcaseTabs() {
    const tabButtons = document.querySelectorAll(".showcase-tab-btn");
    const tabPanes = document.querySelectorAll(".tab-pane");

    function activateTab(btn) {
      const tabId = btn.dataset.tab;
      if (!tabId) return;

      tabButtons.forEach((b) => {
        b.classList.remove("is-active");
        b.setAttribute("aria-selected", "false");
      });
      btn.classList.add("is-active");
      btn.setAttribute("aria-selected", "true");

      tabPanes.forEach((pane) => {
        const isActive = pane.id === `tabContent-${tabId}`;
        pane.classList.remove("is-entering");
        pane.classList.toggle("is-active", isActive);

        if (!isActive) return;

        // Only animate on user activation, never on initial paint
        if (!prefersReducedMotion) {
          void pane.offsetWidth;
          pane.classList.add("is-entering");
          pane.addEventListener(
            "animationend",
            () => pane.classList.remove("is-entering"),
            { once: true }
          );
        }

        // Tabs hidden at page load never intersect: reveal them right away
        pane.querySelectorAll("[data-reveal]").forEach((el) => {
          el.classList.add("is-revealed");
        });
      });
    }

    tabButtons.forEach((btn) => {
      btn.addEventListener("click", () => activateTab(btn));
    });
  }

  // =========================================================================
  // 3. Interactive Emerald Play Dock Simulation
  // =========================================================================
  function setupPlayDockDemo() {
    const playBtn = document.getElementById("interactivePlayBtn");
    const playText = document.getElementById("playBtnText");
    let isLaunching = false;

    if (playBtn && playText) {
      playBtn.addEventListener("click", () => {
        if (isLaunching) return;
        isLaunching = true;

        playText.textContent = "ĐANG KHỞI CHẠY (1.2s)...";
        playBtn.style.opacity = "0.85";

        setTimeout(() => {
          playText.textContent = "GAME ĐANG CHẠY (PID: 14280)";
          playBtn.style.background = "#38bdf8";
          playBtn.style.boxShadow = "0 0 24px rgba(56, 189, 248, 0.5)";

          setTimeout(() => {
            playText.textContent = "CHƠI NGAY (PLAY)";
            playBtn.style.background = "";
            playBtn.style.boxShadow = "";
            playBtn.style.opacity = "1";
            isLaunching = false;
          }, 3500);
        }, 1200);
      });
    }
  }

  // =========================================================================
  // 4. Interactive options.txt Visual Sliders
  // =========================================================================
  function setupOptionsSliders() {
    const fovSlider = document.getElementById("fovSlider");
    const fovVal = document.getElementById("fovVal");
    if (fovSlider && fovVal) {
      fovSlider.addEventListener("input", (e) => {
        fovVal.textContent = `${e.target.value}°`;
      });
    }

    const renderSlider = document.getElementById("renderSlider");
    const renderVal = document.getElementById("renderVal");
    if (renderSlider && renderVal) {
      renderSlider.addEventListener("input", (e) => {
        renderVal.textContent = `${e.target.value} Chunks`;
      });
    }

    const gammaSlider = document.getElementById("gammaSlider");
    const gammaVal = document.getElementById("gammaVal");
    if (gammaSlider && gammaVal) {
      gammaSlider.addEventListener("input", (e) => {
        const val = e.target.value;
        const label = val > 75 ? "(Bright)" : val < 25 ? "(Moody)" : "(Standard)";
        gammaVal.textContent = `${val}% ${label}`;
      });
    }
  }

  // =========================================================================
  // 5. FAQ Accordion Logic
  // =========================================================================
  function setupFaqAccordion() {
    const faqItems = Array.from(document.querySelectorAll(".faq-item"));

    function setAnswerHeight(item, height) {
      const answer = item.querySelector(".faq-answer");
      if (!answer) return;
      answer.style.setProperty("--faq-h", typeof height === "number" ? `${height}px` : height);
    }

    function closeItem(item) {
      if (!item.classList.contains("is-open")) return;
      setAnswerHeight(item, 0);
      item.classList.remove("is-open");
      item.querySelector(".faq-question")?.setAttribute("aria-expanded", "false");
    }

    function openItem(item) {
      item.classList.add("is-open");
      item.querySelector(".faq-question")?.setAttribute("aria-expanded", "true");
      const answer = item.querySelector(".faq-answer");
      if (answer) {
        // scrollHeight phản ánh toàn bộ nội dung kể cả khi đang bị max-height cắt
        answer.style.setProperty("--faq-h", `${answer.scrollHeight}px`);
      }
    }

    faqItems.forEach((item) => {
      const button = item.querySelector(".faq-question");
      if (!button) return;

      button.addEventListener("click", () => {
        const isOpen = item.classList.contains("is-open");

        // Single-open behavior
        faqItems.forEach((other) => {
          if (other !== item) closeItem(other);
        });

        if (isOpen) {
          closeItem(item);
        } else {
          openItem(item);
        }

        if (typeof invalidateNavMetrics === "function") {
          invalidateNavMetrics();
        }
      });
    });

    // Recompute measured height for open answers when layout width changes
    let resizeFrame = 0;
    window.addEventListener(
      "resize",
      () => {
        if (resizeFrame) return;
        resizeFrame = requestAnimationFrame(() => {
          resizeFrame = 0;
          faqItems.forEach((item) => {
            if (!item.classList.contains("is-open")) return;
            const answer = item.querySelector(".faq-answer");
            if (answer) answer.style.setProperty("--faq-h", `${answer.scrollHeight}px`);
          });
        });
      },
      { passive: true }
    );
  }

  // =========================================================================
  // 6. Mobile Drawer, Navbar Scroll State & Active Link
  // =========================================================================
  function setupNavigation() {
    const navToggle = document.getElementById("navToggle");
    const mobileMenu = document.getElementById("mobileMenu");
    const mobileLinks = document.querySelectorAll(".mobile-link");
    const navbar = document.querySelector("header.navbar");

    let drawerOpen = false;

    function setDrawer(open) {
      if (!navToggle || !mobileMenu) return;
      drawerOpen = open;
      mobileMenu.classList.toggle("is-active", open);
      document.body.classList.toggle("drawer-open", open);
      navToggle.setAttribute("aria-expanded", String(open));
      navToggle.setAttribute("aria-label", open ? "Đóng menu" : "Mở menu");
      navToggle.textContent = open ? "✕" : "☰";
    }

    if (navToggle && mobileMenu) {
      navToggle.addEventListener("click", () => {
        setDrawer(!drawerOpen);
      });

      mobileLinks.forEach((link) => {
        link.addEventListener("click", () => setDrawer(false));
      });

      document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && drawerOpen) {
          setDrawer(false);
          navToggle.focus();
        }
      });

      document.addEventListener("click", (event) => {
        if (!drawerOpen) return;
        const target = event.target;
        if (!(target instanceof Node)) return;
        if (mobileMenu.contains(target) || navToggle.contains(target)) return;
        setDrawer(false);
      });

      // Never leave the page scroll-locked if the drawer is hidden by a breakpoint change
      if (typeof window.matchMedia === "function") {
        const desktopQuery = window.matchMedia("(min-width: 769px)");
        const onBreakpointChange = (event) => {
          if (event.matches) setDrawer(false);
        };
        if (typeof desktopQuery.addEventListener === "function") {
          desktopQuery.addEventListener("change", onBreakpointChange);
        } else if (typeof desktopQuery.addListener === "function") {
          desktopQuery.addListener(onBreakpointChange);
        }
      }
    }

    // ---------------------------------------------------------------------
    // Scroll state: rAF-throttled, geometry cached, invalidated on resize
    // ---------------------------------------------------------------------
    const sections = Array.from(document.querySelectorAll("section[id]"));
    const navLinks = Array.from(document.querySelectorAll(".nav-link:not(.mobile-link)"));

    let sectionMetrics = null;
    let scrollFrame = 0;
    let lastActiveSection = "";
    let lastScrolledState = null;

    function measureSections() {
      const scrollY = window.pageYOffset || document.documentElement.scrollTop || 0;
      sectionMetrics = sections.map((section) => {
        const rect = section.getBoundingClientRect();
        return {
          id: section.getAttribute("id") || "",
          top: rect.top + scrollY,
          height: rect.height
        };
      });
    }

    function updateScrollState() {
      scrollFrame = 0;

      const scrollY = window.pageYOffset || document.documentElement.scrollTop || 0;

      if (navbar) {
        const isScrolled = scrollY > 24;
        if (isScrolled !== lastScrolledState) {
          lastScrolledState = isScrolled;
          navbar.classList.toggle("is-scrolled", isScrolled);
        }
      }

      if (!sectionMetrics) measureSections();

      let currentSectionId = "";
      for (let i = 0; i < sectionMetrics.length; i += 1) {
        const metric = sectionMetrics[i];
        const start = metric.top - 120;
        if (scrollY >= start && scrollY < start + metric.height) {
          currentSectionId = metric.id;
          break;
        }
      }

      if (currentSectionId !== lastActiveSection) {
        lastActiveSection = currentSectionId;
        navLinks.forEach((link) => {
          link.classList.toggle("is-active", link.getAttribute("href") === `#${currentSectionId}`);
        });
      }
    }

    function requestScrollStateUpdate() {
      if (scrollFrame) return;
      scrollFrame = requestAnimationFrame(updateScrollState);
    }

    invalidateNavMetrics = () => {
      sectionMetrics = null;
      requestScrollStateUpdate();
    };

    window.addEventListener("scroll", requestScrollStateUpdate, { passive: true });
    window.addEventListener(
      "resize",
      () => {
        sectionMetrics = null;
        requestScrollStateUpdate();
      },
      { passive: true }
    );

    requestScrollStateUpdate();
  }

  // =========================================================================
  // 7. Scroll Reveal (IntersectionObserver, một lần, có fallback)
  // =========================================================================
  function getRevealIndex(el) {
    const parent = el.parentElement;
    if (!parent) return 0;
    const siblings = Array.from(parent.children).filter((child) =>
      child.hasAttribute("data-reveal")
    );
    return Math.max(0, siblings.indexOf(el));
  }

  function readTimeToken(name, fallback) {
    const raw = getComputedStyle(document.documentElement).getPropertyValue(name);
    const parsed = Number.parseFloat(raw);
    return Number.isFinite(parsed) ? parsed : fallback;
  }

  function revealElement(el) {
    const index = getRevealIndex(el);
    const staggerStep = readTimeToken("--stagger-step", 70);
    const revealDuration = readTimeToken("--dur-slow", 420);
    const delay = index * staggerStep;

    if (!prefersReducedMotion && delay > 0) {
      el.style.transitionDelay = `${delay}ms`;
      window.setTimeout(() => {
        el.style.transitionDelay = "";
      }, revealDuration + delay);
    }

    el.classList.add("is-revealed");
  }

  function revealAll() {
    revealItems.forEach((el) => {
      el.style.transitionDelay = "";
      el.classList.add("is-revealed");
    });
  }

  function setupScrollReveal() {
    revealItems = Array.from(document.querySelectorAll("[data-reveal]"));
    if (!revealItems.length) return;

    // Fallback: không có IntersectionObserver hoặc đang reduced motion → hiện ngay
    if (prefersReducedMotion || typeof window.IntersectionObserver !== "function") {
      revealAll();
      return;
    }

    document.documentElement.classList.add("js-reveal-ready");

    if (revealObserver) revealObserver.disconnect();
    revealObserver = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (!entry.isIntersecting) return;
          revealElement(entry.target);
          if (revealObserver) revealObserver.unobserve(entry.target);
        });
      },
      { threshold: 0.15, rootMargin: "0px 0px -8% 0px" }
    );

    revealItems.forEach((el) => {
      if (el.classList.contains("is-revealed")) return;
      revealObserver.observe(el);
    });
  }

  function onMotionPreferenceChange(event) {
    prefersReducedMotion = event.matches;

    if (skinViewer) {
      skinViewer.autoRotate = shouldAutoRotate();
    }

    if (prefersReducedMotion) {
      if (revealObserver) {
        revealObserver.disconnect();
        revealObserver = null;
      }
      revealAll();
    } else if (!revealObserver) {
      setupScrollReveal();
    }
  }

  if (typeof reducedMotionQuery.addEventListener === "function") {
    reducedMotionQuery.addEventListener("change", onMotionPreferenceChange);
  } else if (typeof reducedMotionQuery.addListener === "function") {
    reducedMotionQuery.addListener(onMotionPreferenceChange);
  }

  // =========================================================================
  // Bootstrap when DOM is Ready
  // =========================================================================
  document.addEventListener("DOMContentLoaded", () => {
    initHeroSkinViewer();
    setupSkinControls();
    setupShowcaseTabs();
    setupPlayDockDemo();
    setupOptionsSliders();
    setupFaqAccordion();
    setupNavigation();
    setupScrollReveal();
  });
})();
