/* ============================================================
   Vazer Launcher — UI prototype (vanilla JS)
   PROTOTYPE: mock data + simulated behaviour. Không gọi API thật.
   Chỉnh mock data ở MOCK, style ở styles.css.
   ============================================================ */

const $ = (sel, root = document) => root.querySelector(sel);
const $$ = (sel, root = document) => [...root.querySelectorAll(sel)];

const esc = (s) => String(s ?? "").replace(/[&<>"']/g, (c) => ({
  "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
}[c]));

const now = () => new Date().toLocaleTimeString("vi-VN", { hour12: false });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const rand = (min, max) => Math.floor(Math.random() * (max - min + 1)) + min;

/* ------------------------------------------------------------
   MOCK DATA
   ------------------------------------------------------------ */
const MOCK = {
  instances: [
    {
      id: "test fabric",
      name: "Test Fabric",
      mc: "1.21.1",
      loader: "fabric",
      loaderVersion: "0.19.5",
      path: "C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\test fabric",
      lastPlayed: "2 giờ trước",
      lastPlayedTs: Date.now() - 2 * 3600_000,
      createdTs: Date.now() - 30 * 86_400_000,
    },
    {
      id: "vanilla-1.20.1",
      name: "Vanilla 1.20.1",
      mc: "1.20.1",
      loader: "vanilla",
      loaderVersion: null,
      path: "C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\vanilla-1.20.1",
      lastPlayed: "3 ngày trước",
      lastPlayedTs: Date.now() - 3 * 86_400_000,
      createdTs: Date.now() - 10 * 86_400_000,
    },
    {
      id: "neoforge-1.21.1",
      name: "NeoForge Modded",
      mc: "1.21.1",
      loader: "neoforge",
      loaderVersion: "21.1.72",
      path: "C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\neoforge-1.21.1",
      lastPlayed: "1 tuần trước",
      lastPlayedTs: Date.now() - 7 * 86_400_000,
      createdTs: Date.now() - 3 * 86_400_000,
    },
  ],

  mods: {
    "test fabric": [
      { projectId: "AANobbMI", file: "sodium-fabric-0.6.13+mc1.21.1.jar", version: "0.6.13", enabled: true, update: null },
      { projectId: "gvQqBUqZ", file: "lithium-fabric-0.14.3+mc1.21.1.jar", version: "0.14.3", enabled: true, update: "0.15.0" },
    ],
    "vanilla-1.20.1": [],
    "neoforge-1.21.1": [
      { projectId: "u6dRKJwZ", file: "jei-1.21.1-neoforge-19.21.0.247.jar", version: "19.21.0", enabled: true, update: null },
      { projectId: "uXXizFIs", file: "ferritecore-7.0.1-neoforge.jar", version: "7.0.1", enabled: true, update: null },
      { projectId: "9s6osm5g", file: "cloth-config-15.0.140-neoforge.jar", version: "15.0.140", enabled: false, update: null },
    ],
  },

  accounts: [
    { id: "acc-steve", name: "Steve", kind: "Offline", active: true },
    { id: "acc-alex", name: "Alex", kind: "Microsoft", active: false },
  ],

  worlds: {
    "test fabric": [
      { name: "World", sizeMb: 148.2, lastPlayed: "2 giờ trước", backups: 2 },
      { name: "Survival 1.21", sizeMb: 96.4, lastPlayed: "3 ngày trước", backups: 0 },
      { name: "Creative Test", sizeMb: 12.7, lastPlayed: "1 tuần trước", backups: 1 },
    ],
    "vanilla-1.20.1": [
      { name: "World", sizeMb: 42.8, lastPlayed: "3 ngày trước", backups: 0 },
    ],
    "neoforge-1.21.1": [
      { name: "ATM10", sizeMb: 512.6, lastPlayed: "1 tuần trước", backups: 4 },
      { name: "Skyblock", sizeMb: 88.1, lastPlayed: "2 tuần trước", backups: 1 },
    ],
  },

  // Rút gọn từ kết quả Modrinth thật; icon lấy từ CDN.
  catalog: [
    { id: "AANobbMI", slug: "sodium", name: "Sodium", author: "jellysquid3", desc: "A high-performance rendering engine replacement for Minecraft, which greatly improves frame rates and reduces micro-stutter.", downloads: 248_000_000, version: "0.6.13", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "gvQqBUqZ", slug: "lithium", name: "Lithium", author: "jellysquid3", desc: "No-compromises game logic/server optimization mod.", downloads: 189_000_000, version: "0.15.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "PpjUUSmh", slug: "sodium-extra", name: "Sodium Extra", author: "FlashyReese", desc: "A Sodium addon that adds features that shouldn't be in Sodium.", downloads: 104_300_000, version: "0.6.0", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
    { id: "Bh37bMuy", slug: "reeses-sodium-options", name: "Reese's Sodium Options", author: "FlashyReese", desc: "Alternative Options Menu for Sodium.", downloads: 86_900_000, version: "1.8.3", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
    { id: "YL57xq9U", slug: "iris", name: "Iris Shaders", author: "coderbot", desc: "A modern shader pack loader for Minecraft, designed to be compatible with Sodium.", downloads: 74_200_000, version: "1.8.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "uXXizFIs", slug: "ferrite-core", name: "FerriteCore", author: "malte0811", desc: "Memory usage optimizations for Minecraft.", downloads: 71_500_000, version: "7.0.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "u6dRKJwZ", slug: "jei", name: "Just Enough Items (JEI)", author: "mezz", desc: "View items and recipes.", downloads: 65_800_000, version: "19.21.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "P7dR8mSH", slug: "fabric-api", name: "Fabric API", author: "modmuss50", desc: "Lightweight and modular API providing common hooks and intercompatibility measures utilized by mods using the Fabric toolchain.", downloads: 61_400_000, version: "0.115.0", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
    { id: "mOgUt4GM", slug: "modmenu", name: "Mod Menu", author: "Prospector", desc: "Adds a mod menu to view the list of mods you have installed.", downloads: 54_900_000, version: "11.0.3", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
    { id: "5ZwdcRci", slug: "immediatelyfast", name: "ImmediatelyFast", author: "RaphiMC", desc: "Speed up immediate mode rendering in Minecraft.", downloads: 43_700_000, version: "1.3.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "NNAgCjsB", slug: "entityculling", name: "Entity Culling", author: "tr7zw", desc: "Using async path-tracing to skip rendering blocks and entities which are not visible.", downloads: 39_800_000, version: "1.7.3", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "Orvt0mRa", slug: "indium", name: "Indium", author: "comp500", desc: "Sodium addon providing support for the Fabric Rendering API.", downloads: 33_100_000, version: "1.0.35", loaders: ["fabric"], versions: ["1.21.1"] },
    { id: "9s6osm5g", slug: "cloth-config", name: "Cloth Config API", author: "shedaniel", desc: "Configuration library for Minecraft mods.", downloads: 31_600_000, version: "15.0.140", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "1IjD5062", slug: "continuity", name: "Continuity", author: "PepperCode1", desc: "A Fabric mod that allows for efficient connected textures.", downloads: 27_400_000, version: "3.0.0", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
    { id: "lhGA9TYQ", slug: "architectury-api", name: "Architectury API", author: "shedaniel", desc: "An intermediary api aimed to ease developing multiplatform mods.", downloads: 24_900_000, version: "13.0.8", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "H8CaAYZC", slug: "starlight", name: "Starlight", author: "PaperMC", desc: "Rewrites the light engine to fix lighting performance and lighting bugs.", downloads: 21_200_000, version: "1.1.5", loaders: ["fabric"], versions: ["1.21.1"] },
    { id: "LNytGWDc", slug: "create", name: "Create", author: "simibubi", desc: "Building tools and aesthetic technology for Minecraft.", downloads: 19_600_000, version: "6.0.4", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "1bokaNcj", slug: "xaeros-minimap", name: "Xaero's Minimap", author: "xaero96", desc: "Displays the nearby terrain, and entities such as mobs, players and dropped items in a minimap.", downloads: 18_300_000, version: "24.2.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "EsAfCjCV", slug: "appleskin", name: "AppleSkin", author: "squeek502", desc: "Food-related HUD improvements: hunger/saturation values and tooltips.", downloads: 17_100_000, version: "3.0.6", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "nvQzSEkH", slug: "jade", name: "Jade", author: "Snownee", desc: "Shows information about what you are looking at.", downloads: 16_400_000, version: "15.3.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "fRiHVvU7", slug: "emi", name: "EMI", author: "Emi", desc: "A featureful and accessible item and recipe viewer.", downloads: 15_200_000, version: "1.1.22", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "nmDcB62a", slug: "modernfix", name: "ModernFix", author: "embeddedt", desc: "Dynamic performance improvements and bug fixes for modern Minecraft.", downloads: 14_600_000, version: "5.23.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"] },
    { id: "g96Z4WVZ", slug: "badoptimizations", name: "BadOptimizations", author: "ThetaDev", desc: "A collection of micro-optimizations for the client.", downloads: 12_900_000, version: "2.2.1", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
    { id: "1eAoo2KR", slug: "yacl", name: "YetAnotherConfigLib", author: "isXander", desc: "A builder-based configuration library for Fabric mods.", downloads: 11_800_000, version: "3.7.1", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"] },
  ],
};

const TOTAL_RESULTS = 19_628;
const MB_PAGE_SIZE = 20;
const STORAGE_KEY = "vazer-demo-v1";

const DEFAULT_SETTINGS = {
  ramMb: 4096,
  jvmArgs: "",
  javaPath: "",
  width: 854,
  height: 480,
};

const clamp = (n, min, max) => Math.min(max, Math.max(min, n));
const dateStamp = () => {
  const d = new Date();
  const p = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}_${p(d.getHours())}-${p(d.getMinutes())}-${p(d.getSeconds())}`;
};

const VERSION_OPTIONS = ["Auto", "All", "1.21.1", "1.21", "1.20.1", "1.20", "1.19.4", "1.19.2", "1.18.2", "1.16.5"];
const SORT_OPTIONS = ["Relevance", "Downloads", "Updated", "Newest"];

// Giống Core với resolveDependencies: true — cài kèm mod phụ thuộc.
// Fabric API chỉ áp dụng cho instance Fabric.
const DEPENDENCIES = {
  "sodium-extra": ["fabric-api"],
  "reeses-sodium-options": ["fabric-api"],
  iris: ["fabric-api"],
  indium: ["fabric-api"],
  modmenu: ["fabric-api"],
  continuity: ["fabric-api"],
  create: ["fabric-api"],
  "xaeros-minimap": ["fabric-api"],
  jei: ["fabric-api"],
  entityculling: ["fabric-api"],
  immediatelyfast: ["fabric-api"],
  "ferrite-core": ["fabric-api"],
  starlight: ["fabric-api"],
};

/* ------------------------------------------------------------
   STATE
   ------------------------------------------------------------ */
const state = {
  screen: "instances",
  activeInstanceId: MOCK.instances[0].id,
  instances: structuredClone(MOCK.instances),
  mods: structuredClone(MOCK.mods),
  accounts: structuredClone(MOCK.accounts),
  logs: {},          // instanceId -> string[]
  logTimers: {},     // instanceId -> interval id
  worlds: structuredClone(MOCK.worlds), // instanceId -> world[]
  tab: "mods",
  instanceQuery: "",
  instanceSort: "lastPlayed",
  modBrowser: {
    query: "",
    loader: "Auto",
    version: "Auto",
    sort: "Relevance",
    limit: MB_PAGE_SIZE,
    busy: false,
    installingSlug: null,
  },
  logFilter: { query: "", level: "ALL" },
  logRenderKey: null,
  modSelection: { active: false, files: new Set(), lastIndex: null },
  palette: { open: false, index: 0, items: [] },
};

/* ------------------------------------------------------------
   PERSISTENCE (mock state — để cài mod xong reload vẫn còn)
   ------------------------------------------------------------ */
function saveState() {
  try {
    const instances = state.instances.map((inst) => {
      const { launching, running, status, pid, ...rest } = inst;
      return rest;
    });
    localStorage.setItem(STORAGE_KEY, JSON.stringify({
      instances,
      mods: state.mods,
      worlds: state.worlds,
      accounts: state.accounts,
      activeInstanceId: state.activeInstanceId,
      theme: document.documentElement.dataset.theme,
    }));
  } catch {
    // private mode / storage đầy — bỏ qua, demo vẫn chạy in-memory
  }
}

function loadState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return;
    const data = JSON.parse(raw);
    if (Array.isArray(data.instances)) state.instances = data.instances;
    if (data.mods && typeof data.mods === "object") state.mods = data.mods;
    if (data.worlds && typeof data.worlds === "object") state.worlds = data.worlds;
    if (Array.isArray(data.accounts)) state.accounts = data.accounts;
    if (data.activeInstanceId && state.instances.some((i) => i.id === data.activeInstanceId)) {
      state.activeInstanceId = data.activeInstanceId;
    }
    if (data.theme === "light" || data.theme === "dark") {
      document.documentElement.dataset.theme = data.theme;
    }
  } catch {
    // state hỏng — dùng MOCK
  }
}

/** Bù các field mới cho state cũ trong localStorage. */
function normalizeState() {
  state.instances.forEach((inst) => {
    inst.settings = { ...DEFAULT_SETTINGS, ...(inst.settings || {}) };
    if (typeof inst.lastPlayedTs !== "number") inst.lastPlayedTs = 0;
    if (typeof inst.createdTs !== "number") inst.createdTs = 0;
    if (!state.worlds[inst.id]) state.worlds[inst.id] = structuredClone(MOCK.worlds[inst.id] || []);
  });
  Object.keys(state.worlds).forEach((id) => {
    if (!getInstance(id)) delete state.worlds[id];
  });
}

const getInstance = (id) => state.instances.find((i) => i.id === id);
const currentInstance = () => getInstance(state.activeInstanceId);
const currentMods = () => state.mods[state.activeInstanceId] || [];
const activeAccountName = () => (state.accounts.find((a) => a.active) || {}).name || "Player";

function ensureActiveInstance() {
  if (!getInstance(state.activeInstanceId)) {
    state.activeInstanceId = state.instances[0]?.id || null;
  }
}

function catalogProject(slugOrId) {
  return MOCK.catalog.find((p) => p.slug === slugOrId || p.id === slugOrId);
}

function isProjectInstalled(instanceId, project) {
  return (state.mods[instanceId] || []).some((m) => m.projectId === project.id);
}

function modFileName(project, inst, version = project.version) {
  if (inst.loader === "fabric") return `${project.slug}-fabric-${version}+mc${inst.mc}.jar`;
  if (inst.loader === "neoforge") return `${project.slug}-neoforge-${version}.jar`;
  return `${project.slug}-${version}.jar`;
}

/** Cài 1 project vào instance; trả về true nếu thêm mới. */
function addInstalledMod(project, inst) {
  const mods = (state.mods[inst.id] ||= []);
  if (mods.some((m) => m.projectId === project.id)) return false;

  mods.push({
    projectId: project.id,
    file: modFileName(project, inst),
    version: project.version,
    enabled: true,
    update: null,
  });
  return true;
}

function compareVersions(a, b) {
  const pa = String(a).split(/[.+-]/).map((n) => parseInt(n, 10) || 0);
  const pb = String(b).split(/[.+-]/).map((n) => parseInt(n, 10) || 0);
  for (let i = 0; i < Math.max(pa.length, pb.length); i++) {
    const d = (pa[i] || 0) - (pb[i] || 0);
    if (d) return d;
  }
  return 0;
}

/** Project phụ thuộc cần cài kèm (giống resolveDependencies trong Core). */
function resolveDependencies(project, inst) {
  if (inst.loader !== "fabric") return [];
  return (DEPENDENCIES[project.slug] || [])
    .map(catalogProject)
    .filter((dep) => dep && !isProjectInstalled(inst.id, dep));
}

/* ------------------------------------------------------------
   ROUTING
   ------------------------------------------------------------ */
function moveNavGlider() {
  const glider = $("#navGlider");
  const active = $(".nav-item.is-selected");
  if (!glider || !active) return;
  glider.style.height = active.offsetHeight + "px";
  glider.style.transform = `translateY(${active.offsetTop}px)`;
  glider.classList.add("is-ready");
}

function moveTabGlider() {
  const glider = $("#tabGlider");
  const active = $(".tab.is-selected");
  if (!glider || !active || active.offsetWidth === 0) return;
  glider.style.width = Math.max(0, active.offsetWidth - 16) + "px";
  glider.style.transform = `translateX(${active.offsetLeft + 8}px)`;
  glider.classList.add("is-ready");
}

function showScreen(name) {
  const leavingDetail = state.screen === "detail" && name !== "detail";
  const headerEl = leavingDetail ? $(".page-header", $("#page-detail")) : null;
  const headerRect = headerEl ? headerEl.getBoundingClientRect() : null;

  state.screen = name;
  $$(".nav-item").forEach((b) => b.classList.toggle("is-selected", b.dataset.screen === name));
  $("#page-instances").hidden = name !== "instances";
  $("#page-accounts").hidden = name !== "accounts";
  $("#page-detail").hidden = name !== "detail";
  $("#content").scrollTop = 0;

  if (name === "instances") renderInstances();
  if (name === "accounts") renderAccounts();

  if (leavingDetail && name === "instances" && headerRect && headerEl) {
    const card = $$(".instance-card").find((c) => c.dataset.id === state.activeInstanceId);
    if (card) {
      morphGhost(headerEl, headerRect, card.getBoundingClientRect());
      card.classList.add("is-morph-target");
      setTimeout(() => card.classList.remove("is-morph-target"), 460);
    }
  }

  requestAnimationFrame(() => {
    moveNavGlider();
    if (name === "detail") moveTabGlider();
  });
}

/** Card ↔ Detail morph: một "bóng" bay theo cùng đường vào/ra. */
function morphGhost(sourceEl, from, to) {
  if (prefersReducedMotion() || !sourceEl || !to || from.width === 0) return;

  const ghost = sourceEl.cloneNode(true);
  ghost.querySelectorAll("[id]").forEach((n) => n.removeAttribute("id"));
  ghost.classList.add("morph-ghost");
  ghost.removeAttribute("tabindex");
  Object.assign(ghost.style, {
    left: `${from.left}px`,
    top: `${from.top}px`,
    width: `${from.width}px`,
    height: `${from.height}px`,
  });
  document.body.appendChild(ghost);

  const anim = ghost.animate([
    { left: `${from.left}px`, top: `${from.top}px`, width: `${from.width}px`, height: `${from.height}px`, opacity: 0.96, borderRadius: "12px" },
    { left: `${to.left}px`, top: `${to.top}px`, width: `${to.width}px`, height: `${to.height}px`, opacity: 0, borderRadius: "10px" },
  ], {
    duration: 380,
    easing: "cubic-bezier(.22,1,.36,1)",
    fill: "forwards",
  });
  anim.onfinish = () => ghost.remove();
}

function closeDetail() {
  showScreen("instances");
}

function openDetail(id, sourceCard = null) {
  const inst = getInstance(id);
  if (!inst) return;
  const from = sourceCard ? sourceCard.getBoundingClientRect() : null;
  state.activeInstanceId = id;
  state.tab = "mods";
  state.modSelection.active = false;
  state.modSelection.files.clear();
  state.modSelection.lastIndex = null;

  $("#detailName").textContent = inst.name;
  $("#detailVersion").textContent = inst.mc;
  $("#detailLoader").textContent = inst.loader + (inst.loaderVersion ? ` ${inst.loaderVersion}` : "");
  $("#detailLaunchStatus").textContent = inst.status || "";
  $("#detailStatus").textContent = "";
  $("#detailError").hidden = true;
  $("#settingsFolder").value = inst.path;

  setTab("mods");
  renderMods();
  renderWorlds();
  renderSettings();
  renderLogs();
  updateDetailLaunchButton();
  if (inst.running) streamLogs(id);

  showScreen("detail");

  if (from && sourceCard) {
    const header = $(".page-header", $("#page-detail"));
    morphGhost(sourceCard, from, header.getBoundingClientRect());
    header.classList.add("is-morphing");
    setTimeout(() => header.classList.remove("is-morphing"), 400);
  }
}

/* ------------------------------------------------------------
   INSTANCES
   ------------------------------------------------------------ */
function visibleInstances() {
  const q = state.instanceQuery.trim().toLowerCase();
  const list = state.instances.filter((inst) => {
    if (!q) return true;
    return [inst.name, inst.mc, inst.loader].some((v) => String(v).toLowerCase().includes(q));
  });

  if (state.instanceSort === "name") {
    return list.sort((a, b) => a.name.localeCompare(b.name, "vi"));
  }
  if (state.instanceSort === "created") {
    return list.sort((a, b) => (b.createdTs || 0) - (a.createdTs || 0));
  }
  return list.sort((a, b) => (b.lastPlayedTs || 0) - (a.lastPlayedTs || 0));
}

function renderInstances() {
  const grid = $("#instanceGrid");
  const empty = $("#instancesEmpty");
  const list = visibleInstances();

  grid.innerHTML = list.map((inst, i) => {
    const spinner = inst.launching
      ? `<span class="spinner"></span>`
      : `<span class="glyph">${inst.running ? "&#xE71A;" : "&#xE768;"}</span>`;
    const status = inst.status
      ? `<div class="status">${esc(inst.status)}</div>`
      : "";
    const runningChip = inst.running ? `<span class="chip running">Đang chạy</span>` : "";
    const lastPlayed = inst.lastPlayed ? `Chơi lần cuối: ${esc(inst.lastPlayed)}` : "Chưa chơi lần nào";

    return `
      <div class="instance-card${inst.running ? " is-running" : ""}" data-id="${esc(inst.id)}" tabindex="0" style="--i:${i}">
        <div class="name">${esc(inst.name)}</div>
        <div class="meta">
          <span>${esc(inst.mc)} · ${esc(inst.loader)}</span>
          <span class="text-3">${lastPlayed}</span>
        </div>
        ${runningChip}
        ${status}
        <div class="card-actions">
          <button class="btn icon-only play-btn" data-action="play" title="${inst.running ? "Stop" : "Launch"}" ${inst.launching ? "disabled" : ""}>${spinner}</button>
          <button class="btn icon-only" data-action="edit" title="Edit"><span class="glyph">&#xE713;</span></button>
        </div>
      </div>`;
  }).join("");

  empty.hidden = list.length > 0;
  $("#instancesEmptyTitle").textContent = state.instances.length === 0
    ? "Chưa có instance nào."
    : "Không tìm thấy instance nào.";
  $("#instancesEmptyHint").textContent = state.instances.length === 0
    ? "Bấm + để tạo instance mới."
    : "Thử từ khoá khác hoặc xoá bộ lọc.";
}

function setCardStatus(id, text) {
  const inst = getInstance(id);
  if (!inst) return;
  inst.status = text;
  if (state.screen === "instances") renderInstances();
  if (state.screen === "detail" && state.activeInstanceId === id) {
    $("#detailLaunchStatus").textContent = text;
  }
}

function updateDetailLaunchButton() {
  const inst = currentInstance();
  const running = !!inst?.running;
  $("#detailLaunchLabel").textContent = running ? "Stop" : "Launch";
  $("#detailLaunchBtn .glyph").innerHTML = running ? "&#xE71A;" : "&#xE768;";
  $("#detailLaunchBtn").disabled = !!inst?.launching;
}

async function launchInstance(id, { fromDetail = false } = {}) {
  const inst = getInstance(id);
  if (!inst || inst.launching) return;

  if (inst.running) {
    showToastStatus(`${inst.name} đang chạy. Bấm Stop trước khi launch lại.`);
    return;
  }

  if (!state.accounts.some((a) => a.active)) {
    await showAlert("Chưa có tài khoản", "Vui lòng chọn hoặc thêm tài khoản ở trang Accounts trước khi khởi chạy.");
    return;
  }

  inst.launching = true;
  setCardStatus(id, "Đang khởi chạy...");
  updateDetailLaunchButton();
  if (fromDetail) {
    $("#detailProgress").hidden = false;
    setDetailProgress(10);
    $("#detailStatus").textContent = "Đang chuẩn bị Java runtime...";
  }

  await sleep(700);
  if (fromDetail) { setDetailProgress(45); $("#detailStatus").textContent = "Đang tải metadata phiên bản..."; }
  await sleep(700);
  if (fromDetail) { setDetailProgress(80); $("#detailStatus").textContent = "Đang tạo tiến trình game..."; }
  await sleep(500);

  const pid = rand(10000, 65000);
  const settings = { ...DEFAULT_SETTINGS, ...(inst.settings || {}) };
  inst.launching = false;
  inst.running = true;
  inst.pid = pid;
  inst.lastPlayed = "Vừa xong";
  inst.lastPlayedTs = Date.now();
  setCardStatus(id, `Đang chạy (PID ${pid})`);
  updateDetailLaunchButton();

  // latest.log được truncate mỗi lần chạy game + in cấu hình đang dùng.
  state.logs[id] = [];
  pushLog(id, `[Launcher/INFO]: Java runtime: ${settings.javaPath || "auto-detected (Java 21)"}`);
  pushLog(id, `[Launcher/INFO]: JVM args: -Xmx${settings.ramMb}M -Xms${Math.min(1024, settings.ramMb)}M ${settings.jvmArgs}`.trim());
  pushLog(id, `[Launcher/INFO]: Window: ${settings.width}x${settings.height}`);
  saveState();

  if (fromDetail) {
    $("#detailProgress").hidden = true;
    setDetailProgress(0);
    $("#detailStatus").textContent = "";
    state.tab = "logs";
    setTab("logs");
    streamLogs(id);
  }
}

function stopInstance(id) {
  const inst = getInstance(id);
  if (!inst || !inst.running) return;

  if (state.logTimers[id]) {
    clearInterval(state.logTimers[id]);
    delete state.logTimers[id];
  }
  inst.running = false;
  inst.pid = null;
  pushLog(id, "[Launcher/INFO]: Game stopped by user.");
  setCardStatus(id, "Đã dừng.");
  if (state.screen === "detail" && state.activeInstanceId === id) {
    updateDetailLaunchButton();
    setDetailStatus("Đã dừng instance.");
    renderLogs();
  }
  saveState();
}

function setDetailProgress(pct) {
  $("#detailProgress").firstElementChild.style.width = pct + "%";
}

/* ------------------------------------------------------------
   DETAIL — tabs
   ------------------------------------------------------------ */
const TAB_ORDER = ["mods", "worlds", "logs", "settings"];

function setTab(tab) {
  const prev = state.tab;
  const animate = prev !== tab && state.screen === "detail" && !prefersReducedMotion();
  state.tab = tab;
  $$(".tab").forEach((t) => t.classList.toggle("is-selected", t.dataset.tab === tab));

  const panels = TAB_ORDER.map((t) => ({ name: t, el: $(`#tab-${t}`) }));
  panels.forEach(({ name, el }) => {
    if (name !== tab && name !== prev) el.hidden = true;
  });

  const nextPanel = $(`#tab-${tab}`);
  const prevPanel = prev !== tab ? $(`#tab-${prev}`) : null;

  if (animate && prevPanel && !prevPanel.hidden) {
    const dir = TAB_ORDER.indexOf(tab) > TAB_ORDER.indexOf(prev) ? 1 : -1;
    const inClass = dir > 0 ? "tab-in-right" : "tab-in-left";
    const outClass = dir > 0 ? "tab-out-left" : "tab-out-right";

    nextPanel.hidden = false;
    prevPanel.classList.remove("tab-in-left", "tab-in-right", "tab-out-left", "tab-out-right");
    nextPanel.classList.remove("tab-in-left", "tab-in-right", "tab-out-left", "tab-out-right");
    prevPanel.classList.add(outClass);
    nextPanel.classList.add(inClass);

    setTimeout(() => {
      if (state.tab !== tab) return;
      prevPanel.hidden = true;
      prevPanel.classList.remove(outClass);
      nextPanel.classList.remove(inClass);
    }, 260);
  } else {
    panels.forEach(({ name, el }) => { el.hidden = name !== tab; });
  }

  if (tab === "logs") renderLogs();
  if (tab === "worlds") renderWorlds();
  if (tab === "settings") renderSettings();
  requestAnimationFrame(moveTabGlider);
}

/* ------------------------------------------------------------
   DETAIL — mods
   ------------------------------------------------------------ */
function renderMods(animateBadges = false) {
  const mods = currentMods();
  const list = $("#modList");
  const empty = $("#modsEmpty");
  const selecting = state.modSelection.active;

  list.innerHTML = mods.map((mod, i) => {
    const project = mod.projectId ? MOCK.catalog.find((p) => p.id === mod.projectId) : null;
    const label = project ? project.name : mod.file;
    const selected = state.modSelection.files.has(mod.file);
    const lead = selecting
      ? `<label class="mod-check"><input type="checkbox" data-mod-select="${i}" ${selected ? "checked" : ""} /><span class="box"></span></label>`
      : `<span class="glyph">&#xE74C;</span>`;
    return `
    <div class="list-row mod-row${mod.enabled ? "" : " is-disabled"}${selected ? " is-selected" : ""}${selecting ? " is-selectable" : ""}" data-mod-row="${i}">
      ${lead}
      <div class="mod-main">
        <div class="mod-file" title="${esc(mod.file)}">${esc(mod.file)}</div>
        <div class="mod-version">${esc(label)} · v${esc(mod.version)}</div>
      </div>
      ${mod.update ? `<span class="badge${animateBadges ? " just-badged" : ""}">Cập nhật ${esc(mod.update)}</span>` : ""}
      <button class="btn icon-only mod-menu-btn" data-mod-menu="${i}" title="Tùy chọn mod">
        <span class="glyph">&#xE712;</span>
      </button>
      <label class="switch">
        <input type="checkbox" data-mod-toggle="${i}" ${mod.enabled ? "checked" : ""} />
        <span class="track"></span>
      </label>
    </div>`;
  }).join("");

  empty.hidden = mods.length > 0;
  syncUpdatesBar(animateBadges);
  syncBulkBar();
}

/** Đồng bộ thanh chọn nhiều mod + trạng thái nút Select. */
function syncBulkBar() {
  const active = state.modSelection.active;
  const count = state.modSelection.files.size;
  $("#selectModsBtn").classList.toggle("is-active", active);
  $("#selectModsBtn").setAttribute("aria-pressed", String(active));
  $("#bulkBar").hidden = !active;
  $("#bulkCount").textContent = `${count} đã chọn`;
}

function toggleModSelection(index, { shift = false, checked = null } = {}) {
  const mods = currentMods();
  const mod = mods[index];
  if (!mod) return;

  const files = state.modSelection.files;
  if (shift && state.modSelection.lastIndex != null) {
    const a = Math.min(state.modSelection.lastIndex, index);
    const b = Math.max(state.modSelection.lastIndex, index);
    const select = checked ?? !files.has(mod.file);
    for (let i = a; i <= b; i++) {
      if (select) files.add(mods[i].file);
      else files.delete(mods[i].file);
    }
  } else {
    const select = checked ?? !files.has(mod.file);
    if (select) files.add(mod.file);
    else files.delete(mod.file);
  }

  state.modSelection.lastIndex = index;
  renderMods();
}

async function bulkModAction(action) {
  const mods = currentMods();
  const files = state.modSelection.files;

  if (action === "clear") {
    state.modSelection.active = false;
    files.clear();
    state.modSelection.lastIndex = null;
    renderMods();
    return;
  }

  const targets = mods.filter((m) => files.has(m.file));
  if (targets.length === 0) {
    showToastStatus("Chưa chọn mod nào.");
    return;
  }

  if (action === "enable" || action === "disable") {
    const enabled = action === "enable";
    targets.forEach((m) => { m.enabled = enabled; });
    saveState();
    setDetailStatus(`${enabled ? "Đã bật" : "Đã tắt"} ${targets.length} mod.`);
    renderMods();
    return;
  }

  if (action === "uninstall") {
    const ok = await confirmDialog(
      `Gỡ ${targets.length} mod?`,
      "Các file mod đã chọn sẽ bị xóa khỏi instance. Không thể hoàn tác.",
      "Gỡ"
    );
    if (!ok) return;
    state.mods[state.activeInstanceId] = mods.filter((m) => !files.has(m.file));
    files.clear();
    state.modSelection.lastIndex = null;
    saveState();
    setDetailStatus(`Đã gỡ ${targets.length} mod.`);
    renderMods();
  }
}

/** Đồng bộ thanh "N mod có bản cập nhật" (không re-render cả danh sách). */
function syncUpdatesBar(animateEnter = false) {
  const count = currentMods().filter((m) => m.update).length;
  const bar = $("#updatesBar");
  const wasHidden = bar.hidden;
  bar.hidden = count === 0;
  $("#updatesBarText").textContent = `${count} mod có bản cập nhật.`;

  if (animateEnter && wasHidden && count > 0) {
    bar.classList.remove("is-enter");
    void bar.offsetWidth;
    bar.classList.add("is-enter");
    bar.addEventListener("animationend", () => bar.classList.remove("is-enter"), { once: true });
  }
}

/** Cập nhật DOM của một dòng mod sau khi version đổi — không re-render cả list. */
function commitRowUpdate(index, mod, project, version) {
  const row = rowAt(index);
  if (!row) return;

  const fileEl = row.querySelector(".mod-file");
  const versionEl = row.querySelector(".mod-version");
  if (fileEl) fileEl.textContent = mod.file;
  if (versionEl) versionEl.textContent = `${project ? project.name : mod.file} · v${version}`;

  const badge = row.querySelector(".badge");
  if (badge) {
    badge.classList.add("is-out");
    setTimeout(() => badge.remove(), 240);
  }

  row.classList.remove("is-updating");
  row.classList.add("is-updated");
  setTimeout(() => row.classList.remove("is-updated"), 950);
}

/* ---------- Motion helpers ---------- */
function prefersReducedMotion() {
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches;
}

function attachRipple(e) {
  if (prefersReducedMotion() || e.button !== 0) return;
  const target = e.target.closest(".btn, .demo-btn");
  if (!target || target.disabled) return;

  const rect = target.getBoundingClientRect();
  const size = Math.max(rect.width, rect.height) * 2;
  const ripple = document.createElement("span");
  ripple.className = "ripple";
  ripple.style.width = ripple.style.height = `${size}px`;
  ripple.style.left = `${e.clientX - rect.left - size / 2}px`;
  ripple.style.top = `${e.clientY - rect.top - size / 2}px`;
  target.appendChild(ripple);
  ripple.addEventListener("animationend", () => ripple.remove(), { once: true });
}

function setButtonBusy(btn, busy, label = "Đang xử lý...") {
  if (!btn) return;
  if (busy) {
    if (!btn.dataset.idleHtml) {
      // Không lưu ripple đang chạy vào markup gốc, tránh nó "sống lại" sau khi restore.
      const idle = btn.cloneNode(true);
      idle.querySelectorAll(".ripple").forEach((r) => r.remove());
      btn.dataset.idleHtml = idle.innerHTML;
    }
    btn.disabled = true;
    btn.classList.add("is-busy");
    btn.innerHTML = `<span class="spinner"></span><span>${esc(label)}</span>`;
  } else {
    if (btn.dataset.idleHtml) {
      btn.innerHTML = btn.dataset.idleHtml;
      delete btn.dataset.idleHtml;
    }
    btn.disabled = false;
    btn.classList.remove("is-busy");
  }
}

function pulseStatus() {
  const el = $("#detailStatus");
  el.classList.remove("is-pulse");
  void el.offsetWidth;
  el.classList.add("is-pulse");
}

function bumpUpdatesBar() {
  const bar = $("#updatesBar");
  if (bar.hidden) return;
  bar.classList.remove("is-bump");
  void bar.offsetWidth;
  bar.classList.add("is-bump");
}

function rowAt(index) {
  return $("#modList").querySelectorAll(".mod-row")[index] || null;
}

function markRowChecking(index, delay = 0) {
  const row = rowAt(index);
  if (!row) return;
  setTimeout(() => {
    row.classList.add("is-checking");
    setTimeout(() => row.classList.remove("is-checking"), 600);
  }, delay);
}

function setRowUpdating(index, on) {
  rowAt(index)?.classList.toggle("is-updating", on);
}

function flashProgressDone() {
  const bar = $("#detailProgress");
  bar.classList.remove("is-done");
  void bar.offsetWidth;
  bar.classList.add("is-done");
  setTimeout(() => bar.classList.remove("is-done"), 750);
}

async function checkUpdates() {
  const mods = currentMods();
  if (mods.length === 0) {
    setDetailStatus("Chưa có mod nào để kiểm tra.");
    return;
  }

  const btn = $("#checkUpdatesBtn");
  setButtonBusy(btn, true, "Đang kiểm tra...");
  setDetailStatus("Đang kiểm tra cập nhật...");
  mods.forEach((_, i) => markRowChecking(i, i * 90));
  await sleep(900);

  let count = 0;
  mods.forEach((mod) => {
    const project = mod.projectId ? MOCK.catalog.find((p) => p.id === mod.projectId) : null;
    if (!project) return;
    // Modrinth chỉ trả version tương thích loader/MC — mock dùng version mới nhất trong catalog.
    const newer = compareVersions(project.version, mod.version) > 0;
    mod.update = newer ? project.version : null;
    if (newer) count++;
  });

  renderMods(true);
  saveState();
  setButtonBusy(btn, false);
  setDetailStatus(count > 0
    ? `Có ${count} mod có bản cập nhật mới.`
    : "Tất cả mod đã là bản mới nhất.");
  pulseStatus();
  bumpUpdatesBar();
}

async function updateAll() {
  const mods = currentMods();
  const pending = mods.filter((m) => m.update);
  if (pending.length === 0) {
    setDetailStatus("Không có mod nào cần cập nhật.");
    return;
  }

  const inst = currentInstance();
  const btn = $("#updateAllBtn");
  setButtonBusy(btn, true, "Đang cập nhật...");
  $("#detailProgress").hidden = false;
  let done = 0;

  for (const mod of pending) {
    const index = mods.indexOf(mod);
    const project = mod.projectId ? MOCK.catalog.find((p) => p.id === mod.projectId) : null;
    const label = project ? project.name : mod.file;
    const from = mod.version;
    const to = mod.update;

    setRowUpdating(index, true);

    for (let p = 0; p <= 100; p += 10) {
      setDetailProgress(p);
      setDetailStatus(`Đang cập nhật ${label} ${from} → ${to}... ${p}%`);
      await sleep(70);
    }

    mod.version = to;
    mod.file = project
      ? modFileName(project, inst)
      : mod.file.replace(from, to);
    mod.update = null;
    done++;
    commitRowUpdate(index, mod, project, to);
    syncUpdatesBar();
    bumpUpdatesBar();
    await sleep(120);
  }

  flashProgressDone();
  await sleep(320);
  $("#detailProgress").hidden = true;
  setDetailProgress(0);
  saveState();
  setButtonBusy(btn, false);
  setDetailStatus(`Đã cập nhật xong ${done} mod.`);
  pulseStatus();
}

/** Giảm segment số cuối: 0.6.13 -> 0.6.12 */
function prevVersion(v) {
  const parts = String(v).split(".");
  const last = parseInt(parts[parts.length - 1], 10);
  if (Number.isFinite(last) && last > 0) parts[parts.length - 1] = String(last - 1);
  else parts.push("0");
  return parts.join(".");
}

async function updateSingleMod(index) {
  const mod = currentMods()[index];
  if (!mod || !mod.update) return;

  const inst = currentInstance();
  const project = mod.projectId ? catalogProject(mod.projectId) : null;
  const label = project ? project.name : mod.file;
  const from = mod.version;
  const to = mod.update;

  $("#detailProgress").hidden = false;
  setRowUpdating(index, true);
  for (let p = 0; p <= 100; p += 10) {
    setDetailProgress(p);
    setDetailStatus(`Đang cập nhật ${label} ${from} → ${to}... ${p}%`);
    await sleep(70);
  }

  mod.version = to;
  mod.file = project ? modFileName(project, inst, to) : mod.file.replace(from, to);
  mod.update = null;
  commitRowUpdate(index, mod, project, to);
  syncUpdatesBar();
  bumpUpdatesBar();
  flashProgressDone();
  await sleep(320);
  $("#detailProgress").hidden = true;
  setDetailProgress(0);
  saveState();
  setDetailStatus(`Đã cập nhật ${label} lên ${to}.`);
  pulseStatus();
}

function openVersionPicker(index) {
  const mod = currentMods()[index];
  if (!mod) return;
  const inst = currentInstance();
  const project = mod.projectId ? catalogProject(mod.projectId) : null;
  const name = project ? project.name : mod.file;

  const versions = [...new Set([project?.version, mod.version, prevVersion(mod.version)].filter(Boolean))];

  const dialog = showDialog({
    title: `Chọn phiên bản — ${name}`,
    primary: "Cài bản đã chọn",
    closeText: "Hủy",
    body: `
      <div class="version-list">
        ${versions.map((v) => `
          <label class="version-row">
            <input type="radio" name="verPick" value="${esc(v)}" ${v === mod.version ? "checked" : ""} />
            <span class="ver-name">${esc(v)}</span>
            <span class="caption text-2">${
              v === mod.version
                ? "Đang dùng"
                : compareVersions(v, mod.version) > 0 ? "Mới hơn" : "Cũ hơn"
            }</span>
          </label>`).join("")}
      </div>
      <p class="caption text-2">Demo chỉ đổi metadata; app thật tải đúng file version từ Modrinth.</p>`,
    onPrimary: () => {
      const picked = $("input[name=verPick]:checked", dialog.host)?.value;
      if (!picked) return false;
      mod.version = picked;
      mod.file = project ? modFileName(project, inst, picked) : mod.file;
      mod.update = project && compareVersions(project.version, picked) > 0 ? project.version : null;
      renderMods();
      saveState();
      setDetailStatus(`Đã đổi ${name} sang phiên bản ${picked}.`);
      return true;
    },
  });
  return dialog;
}

async function uninstallMod(index) {
  const mod = currentMods()[index];
  if (!mod) return;
  const project = mod.projectId ? catalogProject(mod.projectId) : null;
  const name = project ? project.name : mod.file;

  if (await confirmDialog(`Gỡ mod "${name}"?`, "File mod sẽ bị xóa khỏi thư mục mods của instance.", "Gỡ")) {
    currentMods().splice(index, 1);
    renderMods();
    saveState();
    setDetailStatus(`Đã gỡ ${name}.`);
  }
}

function openModMenu(x, y, index) {
  const mod = currentMods()[index];
  if (!mod) return;
  const menu = $("#contextMenu");
  delete menu.dataset.instanceId;
  menu.innerHTML = `
    ${mod.update ? `<button data-modmenu="update"><span class="glyph">&#xE896;</span>Cập nhật ${esc(mod.update)}</button>` : ""}
    <button data-modmenu="version"><span class="glyph">&#xE81C;</span>Chọn phiên bản</button>
    <hr />
    <button data-modmenu="uninstall" class="danger"><span class="glyph">&#xE74D;</span>Gỡ mod</button>`;

  menu.hidden = false;
  const rect = menu.getBoundingClientRect();
  menu.style.left = Math.min(x, window.innerWidth - rect.width - 8) + "px";
  menu.style.top = Math.min(y, window.innerHeight - rect.height - 8) + "px";
  menu.dataset.modIndex = index;

  const close = () => {
    menu.hidden = true;
    document.removeEventListener("click", close);
    document.removeEventListener("keydown", onKey);
  };
  const onKey = (e) => { if (e.key === "Escape") close(); };
  setTimeout(() => {
    document.addEventListener("click", close);
    document.addEventListener("keydown", onKey);
  }, 0);
}

async function handleModMenu(action, index) {
  if (action === "update") await updateSingleMod(index);
  if (action === "version") openVersionPicker(index);
  if (action === "uninstall") await uninstallMod(index);
}

/* ------------------------------------------------------------
   DETAIL — logs
   ------------------------------------------------------------ */
function logLines(inst) {
  const enabledMods = (state.mods[inst.id] || []).filter((m) => m.enabled);
  const loaderLabel = inst.loaderVersion ? `${inst.loader} ${inst.loaderVersion}` : inst.loader;
  const lines = [
    `[${now()}] [main/INFO]: Loading Minecraft ${inst.mc} with ${loaderLabel}`,
  ];

  if (enabledMods.length > 0) {
    lines.push(`[${now()}] [main/INFO]: Loading ${enabledMods.length} mods:`);
    enabledMods.slice(0, 8).forEach((m) => lines.push(`\t- ${m.file.replace(/\.jar$/, "")}`));
    if (enabledMods.length > 8) lines.push(`\t- ... và ${enabledMods.length - 8} mod khác`);
  }

  lines.push(
    `[${now()}] [main/INFO]: SpongePowered MIXIN is active.`,
    `[${now()}] [Render thread/INFO]: Setting user: ${activeAccountName()}`,
    `[${now()}] [Render thread/INFO]: Backend library: LWJGL version 3.3.3+5`,
    `[${now()}] [Render thread/INFO]: OpenGL debug context created`,
    `[${now()}] [Render thread/INFO]: Narrator library for x64 successfully loaded`,
    `[${now()}] [Render thread/INFO]: Reloading ResourceManager: vanilla${inst.loader !== "vanilla" ? ", " + inst.loader : ""}`,
    `[${now()}] [Worker-Main-1/INFO]: Found ${Math.max(4, enabledMods.length - 2)} mods with data generators`,
    `[${now()}] [Render thread/INFO]: Sound engine started`,
    `[${now()}] [Render thread/INFO]: Created: 1024x512x4 minecraft:textures/atlas/blocks.png`,
    `[${now()}] [Render thread/INFO]: Loaded 1287 recipes`,
    `[${now()}] [Render thread/INFO]: Connecting to localhost:25565`,
    `[${now()}] [Netty Client IO #0/INFO]: Connected to a modded server.`,
    `[${now()}] [Render thread/INFO]: Sodium: Using GL_ARB_buffer_storage`,
  );

  return lines;
}

function streamLogs(id) {
  const inst = getInstance(id);
  if (!inst || state.logTimers[id]) return;

  const lines = logLines(inst);
  let index = 0;

  state.logTimers[id] = setInterval(() => {
    if (index >= lines.length) {
      clearInterval(state.logTimers[id]);
      delete state.logTimers[id];
      return;
    }
    pushLog(id, lines[index++]);
  }, 420);
}

function pushLog(id, line) {
  (state.logs[id] ||= []).push(line);
  if (state.screen === "detail" && state.activeInstanceId === id && state.tab === "logs") {
    renderLogs();
  }
}

function logSeverity(line) {
  if (/\bERROR\b|Exception|\bCaused by:/.test(line)) return "ERROR";
  if (/\bWARN\b/.test(line)) return "WARN";
  return "INFO";
}

function filteredLogLines() {
  const lines = state.logs[state.activeInstanceId] || [];
  const level = state.logFilter.level;
  const q = state.logFilter.query.trim().toLowerCase();
  return lines.filter((line) => {
    if (level !== "ALL" && logSeverity(line) !== level) return false;
    return !q || line.toLowerCase().includes(q);
  });
}

function logLineHtml(line) {
  const sev = logSeverity(line);
  return `<div class="log-line${sev === "ERROR" ? " is-error" : sev === "WARN" ? " is-warn" : ""}">${esc(line)}</div>`;
}

function updateLogJump() {
  const view = $("#logView");
  const btn = $("#logJumpBtn");
  if (!view || !btn) return;
  const far = !view.hidden && view.scrollHeight - view.scrollTop - view.clientHeight > 60;
  btn.hidden = !far;
}

function jumpToFirstError() {
  const lines = filteredLogLines();
  const index = lines.findIndex((line) => logSeverity(line) === "ERROR");
  if (index < 0) {
    showToastStatus("Không có dòng ERROR trong log hiện tại.");
    return;
  }
  const el = $("#logView").children[index];
  if (!el) return;
  el.scrollIntoView({ block: "center", behavior: prefersReducedMotion() ? "auto" : "smooth" });
  el.classList.add("is-flash");
  setTimeout(() => el.classList.remove("is-flash"), 1000);
}

function renderLogs() {
  const id = state.activeInstanceId;
  const all = state.logs[id] || [];
  const filtered = filteredLogLines();
  const view = $("#logView");
  const empty = $("#logsEmpty");

  $("#logCount").textContent = all.length ? `${filtered.length}/${all.length} dòng` : "";
  view.hidden = filtered.length === 0;
  empty.hidden = filtered.length > 0;
  empty.querySelector("p").textContent = all.length
    ? "Không có dòng log nào khớp bộ lọc."
    : "Chưa có log nào. Bấm Launch để bắt đầu.";

  if (filtered.length === 0) {
    view.innerHTML = "";
    view.dataset.count = "0";
    state.logRenderKey = `${id}|${state.logFilter.level}|${state.logFilter.query}|0`;
    $("#logJumpBtn").hidden = true;
    return;
  }

  const wasNearBottom = view.scrollHeight - view.scrollTop - view.clientHeight < 60;
  const key = `${id}|${state.logFilter.level}|${state.logFilter.query}|${filtered.length}`;
  const prev = state.logRenderKey ? state.logRenderKey.split("|") : null;
  const sameBase = prev && prev[0] === id && prev[1] === state.logFilter.level && prev[2] === state.logFilter.query;
  const rendered = Number(view.dataset.count || 0);

  if (sameBase && filtered.length === Number(prev[3]) + 1 && filtered.length === rendered + 1) {
    view.insertAdjacentHTML("beforeend", logLineHtml(filtered[filtered.length - 1]));
    const last = view.lastElementChild;
    last.classList.add("is-new");
    last.addEventListener("animationend", () => last.classList.remove("is-new"), { once: true });
  } else if (!sameBase || filtered.length !== rendered) {
    view.innerHTML = filtered.map(logLineHtml).join("");
  }

  view.dataset.count = String(filtered.length);
  state.logRenderKey = key;

  if ($("#autoScrollChk").checked && (wasNearBottom || !sameBase)) {
    view.scrollTop = view.scrollHeight;
  }
  updateLogJump();
}

/* ------------------------------------------------------------
   DETAIL — worlds
   ------------------------------------------------------------ */
function worldsFor(inst) {
  return (state.worlds[inst.id] ||= []);
}

function renderWorlds() {
  const inst = currentInstance();
  if (!inst) return;

  const worlds = worldsFor(inst);
  const totalMb = worlds.reduce((sum, w) => sum + (w.sizeMb || 0), 0);
  const totalBackups = worlds.reduce((sum, w) => sum + (w.backups || 0), 0);
  $("#worldsSummary").textContent =
    `${worlds.length} world · ${totalMb.toFixed(1)} MB · ${totalBackups} backup`;

  $("#worldList").innerHTML = worlds.map((w, i) => `
    <div class="list-row world-row">
      <span class="glyph">&#xE81E;</span>
      <div class="world-main">
        <div class="world-name">${esc(w.name)}</div>
        <div class="caption text-2">
          ${w.sizeMb.toFixed(1)} MB · Chơi lần cuối: ${esc(w.lastPlayed)} · ${w.backups || 0} backup
        </div>
      </div>
      <button class="btn" data-world-backup="${i}">
        <span class="glyph">&#xE896;</span><span>Backup</span>
      </button>
    </div>`).join("");

  $("#worldsEmpty").hidden = worlds.length > 0;
}

async function backupWorld(index) {
  const inst = currentInstance();
  if (!inst) return;
  const world = worldsFor(inst)[index];
  if (!world) return;

  const progress = $("#worldsProgress");
  const status = $("#worldsStatus");
  progress.hidden = false;

  for (let p = 0; p <= 100; p += 10) {
    progress.firstElementChild.style.width = p + "%";
    status.textContent = `Đang nén "${world.name}"... ${p}%`;
    await sleep(45);
  }

  world.backups = (world.backups || 0) + 1;
  saveState();
  renderWorlds();

  progress.hidden = true;
  progress.firstElementChild.style.width = "0%";
  status.textContent =
    `Đã backup "${world.name}" → backups/${world.name.replace(/\s+/g, "_")}-${dateStamp()}.zip (${world.sizeMb.toFixed(1)} MB).`;
  showToastStatus(`Đã backup "${world.name}".`);
}

async function backupAllWorlds() {
  const inst = currentInstance();
  if (!inst) return;
  const worlds = [...worldsFor(inst)];
  if (worlds.length === 0) {
    showToastStatus("Không có world nào để backup.");
    return;
  }

  for (let i = 0; i < worlds.length; i++) {
    await backupWorld(i);
  }
  $("#worldsStatus").textContent = `Đã backup xong ${worlds.length} world.`;
}

/* ------------------------------------------------------------
   DETAIL — settings
   ------------------------------------------------------------ */
function renderSettings() {
  const inst = currentInstance();
  if (!inst) return;

  inst.settings = { ...DEFAULT_SETTINGS, ...(inst.settings || {}) };
  const s = inst.settings;

  $("#setRam").value = s.ramMb;
  $("#setRamValue").value = s.ramMb;
  $("#setJvm").value = s.jvmArgs;
  $("#setJava").value = s.javaPath;
  $("#setWidth").value = s.width;
  $("#setHeight").value = s.height;
  $("#settingsFolder").value = inst.path;
  $("#settingsStatus").textContent = "";
}

function saveSettings() {
  const inst = currentInstance();
  if (!inst) return;

  const ramMb = clamp(parseInt($("#setRamValue").value, 10) || DEFAULT_SETTINGS.ramMb, 1024, 16384);
  const width = clamp(parseInt($("#setWidth").value, 10) || DEFAULT_SETTINGS.width, 320, 7680);
  const height = clamp(parseInt($("#setHeight").value, 10) || DEFAULT_SETTINGS.height, 240, 4320);

  inst.settings = {
    ramMb,
    jvmArgs: $("#setJvm").value.trim(),
    javaPath: $("#setJava").value.trim(),
    width,
    height,
  };

  saveState();
  renderSettings();
  $("#settingsStatus").textContent = "Đã lưu cài đặt.";
  showToastStatus("Đã lưu cài đặt instance.");
}

function resetSettings() {
  const inst = currentInstance();
  if (!inst) return;
  inst.settings = { ...DEFAULT_SETTINGS };
  saveState();
  renderSettings();
  $("#settingsStatus").textContent = "Đã khôi phục mặc định.";
}

/* ------------------------------------------------------------
   DETAIL — rename + crash
   ------------------------------------------------------------ */
function openRenameDialog(id) {
  const inst = getInstance(id);
  if (!inst) return;

  const dialog = showDialog({
    title: "Đổi tên instance",
    primary: "Lưu",
    closeText: "Hủy",
    body: `
      <label class="field"><span>Tên mới</span>
        <input class="textbox" id="renameInput" value="${esc(inst.name)}" />
      </label>
      <div class="infobar info">
        <span class="glyph">&#xE946;</span>
        <div>Demo chỉ đổi tên hiển thị. App thật sẽ đổi cả tên thư mục instance.</div>
      </div>`,
    onPrimary: () => {
      const input = $("#renameInput", dialog.host);
      const name = input.value.trim();
      if (!name) {
        input.focus();
        return false;
      }
      inst.name = name;
      saveState();
      renderInstances();
      if (state.screen === "detail" && state.activeInstanceId === inst.id) {
        $("#detailName").textContent = name;
      }
      showToastStatus(`Đã đổi tên thành "${name}".`);
      return true;
    },
  });

  setTimeout(() => $("#renameInput", dialog.host)?.select(), 50);
}

function simulateCrash() {
  const inst = state.instances.find((i) => i.running);
  if (!inst) {
    showToastStatus("Không có instance nào đang chạy. Bấm Launch trước đã.");
    return;
  }

  if (state.logTimers[inst.id]) {
    clearInterval(state.logTimers[inst.id]);
    delete state.logTimers[inst.id];
  }
  inst.running = false;
  inst.launching = false;
  inst.pid = null;
  inst.status = "Đã crash (exit code 1)";

  const report = `crash-${dateStamp()}-client.txt`;
  pushLog(inst.id, "[Render thread/ERROR]: java.lang.OutOfMemoryError: Java heap space");
  pushLog(inst.id, "[Render thread/ERROR]: \tat net.minecraft.client.Minecraft.run(Minecraft.java:1015)");
  pushLog(inst.id, `[Launcher/ERROR]: Game crashed with exit code 1. Report: crash-reports/${report}`);
  saveState();

  if (state.screen === "instances") renderInstances();
  if (state.screen === "detail" && state.activeInstanceId === inst.id) {
    $("#detailLaunchStatus").textContent = inst.status;
    updateDetailLaunchButton();
    renderLogs();
  }

  showDialog({
    title: "Instance đã crash",
    primary: "Mở crash report",
    closeText: "Đóng",
    body: `
      <div class="infobar error">
        <span class="glyph">&#xE7BA;</span>
        <div><strong>${esc(inst.name)} đã thoát bất thường</strong><p>Exit code 1 · ${now()}</p></div>
      </div>
      <label class="field"><span>Crash report</span>
        <input class="textbox" readonly value="${esc(inst.path)}\\crash-reports\\${report}" />
      </label>
      <p class="caption text-2">Demo: mở report chỉ hiện thông báo. App thật mở file bằng Notepad/Explorer.</p>`,
    onPrimary: () => {
      showToastStatus(`Đã mở crash-reports/${report} (demo).`);
      return true;
    },
  });
}

/* ------------------------------------------------------------
   ACCOUNTS
   ------------------------------------------------------------ */
function avatarUrl(name) {
  return `https://minotar.net/helm/${encodeURIComponent(name)}/64.png`;
}

function renderAccounts() {
  const list = $("#accountList");

  list.innerHTML = state.accounts.map((acc) => `
    <div class="account-card" data-account="${esc(acc.id)}">
      <div class="avatar"><img src="${avatarUrl(acc.name)}" alt="${esc(acc.name)}" /></div>
      <div>
        <div class="account-name">${esc(acc.name)}</div>
        <div class="account-sub">
          <span class="chip">${esc(acc.kind)}</span>
          ${acc.active ? `<span class="active-mark">✓ Active</span>` : ""}
        </div>
      </div>
      <button class="btn" data-acc-action="activate" ${acc.active ? "disabled" : ""}>Set active</button>
      <button class="btn" data-acc-action="remove">Xóa</button>
    </div>`).join("");

  $("#addOfflineBtn").disabled = false;
}

function accountStatus(text) {
  $("#accountStatus").textContent = text;
}

function setActiveAccount(id) {
  state.accounts.forEach((a) => { a.active = a.id === id; });
  saveState();
  renderAccounts();
  accountStatus(`Đã đặt "${getAccount(id).name}" làm tài khoản active.`);
}

function getAccount(id) {
  return state.accounts.find((a) => a.id === id);
}

/* ------------------------------------------------------------
   CONTEXT MENU
   ------------------------------------------------------------ */
function openContextMenu(x, y, id) {
  const menu = $("#contextMenu");
  const inst = getInstance(id);
  delete menu.dataset.modIndex;
  menu.innerHTML = `
    <button data-ctx="${inst?.running ? "stop" : "play"}"><span class="glyph">${inst?.running ? "&#xE71A;" : "&#xE768;"}</span>${inst?.running ? "Stop" : "Play"}</button>
    <button data-ctx="edit"><span class="glyph">&#xE713;</span>Edit</button>
    <button data-ctx="rename"><span class="glyph">&#xE8AC;</span>Rename</button>
    <button data-ctx="folder"><span class="glyph">&#xE838;</span>Open folder</button>
    <hr />
    <button data-ctx="delete" class="danger"><span class="glyph">&#xE74D;</span>Delete</button>`;

  menu.hidden = false;
  const rect = menu.getBoundingClientRect();
  menu.style.left = Math.min(x, window.innerWidth - rect.width - 8) + "px";
  menu.style.top = Math.min(y, window.innerHeight - rect.height - 8) + "px";
  menu.dataset.instanceId = id;

  const close = () => {
    menu.hidden = true;
    document.removeEventListener("click", close);
    document.removeEventListener("keydown", onKey);
  };
  const onKey = (e) => { if (e.key === "Escape") close(); };

  setTimeout(() => {
    document.addEventListener("click", close);
    document.addEventListener("keydown", onKey);
  }, 0);
}

/* ------------------------------------------------------------
   DIALOGS
   ------------------------------------------------------------ */
function showDialog({ title, body = "", primary = null, closeText = "Đóng", wide = false, onPrimary = null }) {
  const host = $("#dialogHost");
  const dialogId = `${Date.now()}-${Math.random()}`;
  host.dataset.dialogId = dialogId;
  host.hidden = false;
  host.classList.remove("is-closing");
  host.innerHTML = `
    <div class="dialog ${wide ? "wide" : ""}" role="dialog" aria-modal="true">
      <h2 class="dialog-title">${esc(title)}</h2>
      <div class="dialog-body">${body}</div>
      <div class="dialog-footer">
        ${primary ? `<button class="btn accent" data-dialog-primary>${esc(primary)}</button>` : ""}
        <button class="btn" data-dialog-close>${esc(closeText)}</button>
      </div>
    </div>`;

  let closing = false;
  const close = () => {
    if (closing) return;
    closing = true;
    host.classList.add("is-closing");
    setTimeout(() => {
      if (host.dataset.dialogId !== dialogId) return;
      host.hidden = true;
      host.innerHTML = "";
      host.classList.remove("is-closing");
      closing = false;
    }, 180);
  };
  $("[data-dialog-close]", host).onclick = close;
  const primaryBtn = $("[data-dialog-primary]", host);
  if (primaryBtn) {
    primaryBtn.onclick = async () => {
      if (onPrimary && (await onPrimary()) === false) return;
      close();
    };
  }

  return { close, host };
}

function showAlert(title, message) {
  return new Promise((resolve) => {
    const d = showDialog({
      title,
      body: `<p class="text-2">${esc(message)}</p>`,
      closeText: "Đóng",
    });
    $("[data-dialog-close]", d.host).addEventListener("click", resolve, { once: true });
    d.host.addEventListener("click", (e) => { if (e.target === d.host) resolve(); }, { once: true });
  });
}

function confirmDialog(title, message, confirmText = "Xóa") {
  return new Promise((resolve) => {
    const d = showDialog({
      title,
      body: `<p class="text-2">${esc(message)}</p>`,
      primary: confirmText,
      closeText: "Hủy",
    });

    let settled = false;
    const done = (value) => {
      if (settled) return;
      settled = true;
      d.close();
      resolve(value);
    };

    $("[data-dialog-primary]", d.host).onclick = () => done(true);
    $("[data-dialog-close]", d.host).onclick = () => done(false);
    d.host.addEventListener("click", (e) => { if (e.target === d.host) done(false); });
  });
}

/* ------------------------------------------------------------
   NEW INSTANCE DIALOG
   ------------------------------------------------------------ */
function openNewInstanceDialog() {
  const dialog = showDialog({
    title: "Tạo instance mới",
    primary: "Tạo",
    closeText: "Hủy",
    body: `
      <label class="field"><span>Tên instance</span>
        <input class="textbox" id="niName" placeholder="New Instance" />
      </label>
      <label class="field"><span>Loader</span>
        <select class="select" id="niLoader">
          <option value="vanilla">Vanilla</option>
          <option value="fabric" selected>Fabric</option>
          <option value="neoforge">NeoForge</option>
        </select>
      </label>
      <label class="field"><span>Minecraft version</span>
        <select class="select" id="niVersion">
          <option>1.21.1</option>
          <option>1.21</option>
          <option>1.20.1</option>
          <option>1.20</option>
          <option>1.19.2</option>
        </select>
      </label>
      <label class="field"><span>Loader version (để trống = latest)</span>
        <input class="textbox" id="niLoaderVersion" placeholder="latest" />
      </label>
      <div class="result-status" id="niBusy" hidden>
        <span class="spinner" style="color:var(--accent)"></span>
        <span id="niBusyText">Đang tạo instance...</span>
      </div>
      <div class="infobar error" id="niError" hidden><span class="glyph">&#xE7BA;</span><div id="niErrorText"></div></div>`,
    onPrimary: async () => {
      const name = $("#niName").value.trim();
      if (!name) {
        $("#niError").hidden = false;
        $("#niErrorText").textContent = "Tên instance không được để trống.";
        return false;
      }

      const loader = $("#niLoader").value;
      const version = $("#niVersion").value;
      const loaderVersion = $("#niLoaderVersion").value.trim() || null;

      $("#niBusy").hidden = false;
      $("[data-dialog-primary]", dialog.host).disabled = true;
      $("#niBusyText").textContent = "Đang resolve version manifest...";
      await sleep(700);
      $("#niBusyText").textContent = "Đang cài đặt loader...";
      await sleep(700);
      $("#niBusyText").textContent = "Đang tạo thư mục instance...";
      await sleep(500);

      const id = name.toLowerCase().replace(/[^a-z0-9]+/g, "-");
      state.instances.unshift({
        id,
        name,
        mc: version,
        loader,
        loaderVersion,
        path: `C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\${id}`,
        lastPlayed: null,
        lastPlayedTs: 0,
        createdTs: Date.now(),
        settings: { ...DEFAULT_SETTINGS },
      });
      state.mods[id] = [];
      state.worlds[id] = [];
      saveState();
      renderInstances();
      showToastStatus(`Đã tạo instance "${name}".`);
      return true;
    },
  });

  const closeBtn = $("[data-dialog-close]", dialog.host);
  closeBtn.onclick = () => {
    if (!$("#niBusy").hidden) return; // đang tạo thì không cho hủy
    dialog.close();
  };
}

/* ------------------------------------------------------------
   MOD BROWSER DIALOG
   ------------------------------------------------------------ */
function openModBrowser() {
  const inst = currentInstance();
  if (!inst) {
    showToastStatus("Chưa chọn instance để cài mod.");
    return;
  }

  // Mỗi lần mở dialog: reset bộ lọc như ModBrowserViewModel mới.
  state.modBrowser.query = "";
  state.modBrowser.loader = "Auto";
  state.modBrowser.version = "Auto";
  state.modBrowser.sort = "Relevance";
  state.modBrowser.limit = MB_PAGE_SIZE;
  state.modBrowser.busy = false;
  state.modBrowser.installingSlug = null;

  const dialog = showDialog({
    title: "Download Mods",
    closeText: "Đóng",
    wide: true,
    body: `
      <div class="searchbox">
        <span class="glyph">&#xE721;</span>
        <input id="mbSearch" placeholder="Tìm mod trên Modrinth (ví dụ: sodium, iris, lithium)..." autocomplete="off" />
        <button class="btn icon-only" id="mbClear" hidden style="min-height:26px;width:26px;border:0;background:transparent">
          <span class="glyph">&#xE711;</span>
        </button>
      </div>

      <div class="filters">
        <label class="field"><span>Loader</span>
          <select class="select" id="mbLoader">
            ${["Auto", "All", "fabric", "neoforge"].map((v) => `<option value="${v}">${v}</option>`).join("")}
          </select>
        </label>
        <label class="field"><span>MC version</span>
          <select class="select" id="mbVersion">
            ${VERSION_OPTIONS.map((v) => `<option value="${v}">${v}</option>`).join("")}
          </select>
        </label>
        <label class="field"><span>Sort</span>
          <select class="select" id="mbSort">
            ${SORT_OPTIONS.map((v) => `<option value="${v}">${v}</option>`).join("")}
          </select>
        </label>
      </div>

      <div class="result-status">
        <span id="mbCount">—</span>
        <span id="mbStatus" class="text-2"></span>
      </div>
      <div class="progress thin" id="mbProgress" hidden><div></div></div>

      <div id="mbResults"></div>
      <button class="btn load-more" id="mbLoadMore">Load more</button>`,
  });

  const search = $("#mbSearch", dialog.host);
  const status = () => $("#mbStatus", dialog.host);
  let timer = null;

  const update = ({ searching = false } = {}) => {
    state.modBrowser.query = search.value;
    $("#mbClear", dialog.host).hidden = search.value.length === 0;
    if (searching) status().textContent = "Đang tìm...";
    renderModResults(dialog);
  };

  search.addEventListener("input", () => {
    clearTimeout(timer);
    status().textContent = "Đang tìm...";
    timer = setTimeout(() => update(), 300);
  });

  search.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
      clearTimeout(timer);
      update();
    }
  });

  $("#mbClear", dialog.host).onclick = () => {
    search.value = "";
    update();
    search.focus();
  };

  $("#mbLoader", dialog.host).onchange = (e) => {
    state.modBrowser.loader = e.target.value;
    update();
  };
  $("#mbVersion", dialog.host).onchange = (e) => {
    state.modBrowser.version = e.target.value;
    update();
  };
  $("#mbSort", dialog.host).onchange = (e) => {
    state.modBrowser.sort = e.target.value;
    update();
  };

  $("#mbLoadMore", dialog.host).onclick = () => {
    state.modBrowser.limit += MB_PAGE_SIZE;
    renderModResults(dialog);
  };

  $("#mbResults", dialog.host).addEventListener("click", (e) => {
    const btn = e.target.closest("[data-install]");
    if (btn) installMod(dialog, btn.dataset.install);
  });

  renderModResults(dialog);
  setTimeout(() => search.focus(), 50);
}

const stableHash = (s) =>
  Math.abs([...String(s)].reduce((h, c) => ((h << 5) - h + c.charCodeAt(0)) | 0, 0));

/** Auto = theo instance đang mở (giống ResolveLoaders/ResolveGameVersions trong ViewModel). */
function resolvedFilters() {
  const inst = currentInstance();
  const loader = state.modBrowser.loader === "Auto"
    ? (inst && inst.loader !== "vanilla" ? inst.loader : null)
    : state.modBrowser.loader === "All" ? null : state.modBrowser.loader;
  const version = state.modBrowser.version === "Auto"
    ? (inst ? inst.mc : null)
    : state.modBrowser.version === "All" ? null : state.modBrowser.version;
  return { loader, version };
}

function filteredCatalog() {
  const { query, sort } = state.modBrowser;
  const { loader, version } = resolvedFilters();
  const tokens = query.toLowerCase().split(/\s+/).filter(Boolean);

  let items = MOCK.catalog.filter((p) => {
    if (loader && !p.loaders.includes(loader)) return false;
    if (version && !p.versions.includes(version)) return false;
    if (tokens.length === 0) return true;
    const hay = `${p.name} ${p.slug} ${p.desc} ${p.author}`.toLowerCase();
    return tokens.every((t) => hay.includes(t));
  });

  const score = (p) => {
    const q = query.trim().toLowerCase();
    if (!q) return 0;
    const name = p.name.toLowerCase();
    if (name === q) return 1000;
    if (name.startsWith(q)) return 800;
    if (name.includes(q)) return 600;
    if (p.slug.includes(q)) return 500;
    return 100;
  };

  if (sort === "Downloads") items.sort((a, b) => b.downloads - a.downloads);
  else if (sort === "Updated") items.sort((a, b) => stableHash(b.id) - stableHash(a.id));
  else if (sort === "Newest") items.sort((a, b) => stableHash(a.id) - stableHash(b.id));
  // Relevance: ưu tiên khớp tên, sau đó tới lượt tải (Modrinth fuzzy search).
  else items.sort((a, b) => score(b) - score(a) || b.downloads - a.downloads);

  return items;
}

function fmtDownloads(n) {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(".0", "") + "M";
  if (n >= 1_000) return (n / 1_000).toFixed(1).replace(".0", "") + "K";
  return String(n);
}

function renderModResults(dialog, { preserveStatus = false } = {}) {
  const inst = currentInstance();
  const items = filteredCatalog();
  const shown = items.slice(0, state.modBrowser.limit);
  const list = $("#mbResults", dialog.host);
  const status = $("#mbStatus", dialog.host);

  list.innerHTML = shown.map((p) => {
    const installed = inst ? isProjectInstalled(inst.id, p) : false;
    const installing = state.modBrowser.installingSlug === p.slug;
    const disabled = installed || state.modBrowser.busy;
    return `
      <div class="mod-result${installing ? " is-installing" : ""}" data-slug="${esc(p.slug)}">
        <img class="mod-icon" src="https://cdn.modrinth.com/data/${p.id}/icon.png" alt="" data-fallback="${esc(p.name[0])}" />
        <div>
          <div class="mod-title">${esc(p.name)}</div>
          <div class="mod-author">${esc(p.author)}</div>
          <div class="mod-desc">${esc(p.desc)}</div>
        </div>
        <div class="mod-side">
          <span class="mod-downloads">${fmtDownloads(p.downloads)}</span>
          <button class="btn" data-install="${esc(p.slug)}" ${disabled ? "disabled" : ""}>
            ${installing ? `<span class="spinner"></span> Đang cài` : installed ? "Đã cài" : "Cài"}
          </button>
        </div>
        <div class="pipeline" hidden>
          <div class="pipeline-stages">
            <div class="stage" data-stage="download"><span class="stage-dot"></span><span>Tải</span></div>
            <div class="stage" data-stage="verify"><span class="stage-dot"></span><span>Kiểm tra</span></div>
            <div class="stage" data-stage="install"><span class="stage-dot"></span><span>Cài</span></div>
          </div>
          <div class="pipeline-deps"></div>
        </div>
      </div>`;
  }).join("");

  $("#mbCount", dialog.host).textContent = `${shown.length} / ${TOTAL_RESULTS.toLocaleString("en-US")} results`;

  if (items.length === 0) {
    list.innerHTML = `<div class="empty-state small"><span class="glyph empty-icon">&#xE721;</span><p class="text-2">Không tìm thấy mod nào phù hợp.</p></div>`;
    if (!preserveStatus) status.textContent = "No results";
  } else if (!preserveStatus) {
    status.textContent = "";
  }

  $("#mbLoadMore", dialog.host).hidden = shown.length >= items.length || items.length === 0;

  $$("img.mod-icon", list).forEach((img) => {
    img.addEventListener("error", () => {
      const div = document.createElement("div");
      div.className = "mod-icon";
      div.style.display = "grid";
      div.style.placeItems = "center";
      div.style.fontWeight = "600";
      div.textContent = img.dataset.fallback || "?";
      img.replaceWith(div);
    }, { once: true });
  });
}

function pipelineStages(row) {
  return {
    download: $('[data-stage="download"]', row),
    verify: $('[data-stage="verify"]', row),
    install: $('[data-stage="install"]', row),
  };
}

function setStageState(stageEl, stateName) {
  if (!stageEl) return;
  stageEl.classList.toggle("is-active", stateName === "active");
  stageEl.classList.toggle("is-done", stateName === "done");
}

async function installMod(dialog, slug) {
  const inst = currentInstance();
  const project = catalogProject(slug);
  if (!inst || !project || state.modBrowser.busy) return;
  if (isProjectInstalled(inst.id, project)) return;

  const deps = resolveDependencies(project, inst);
  const queue = [project, ...deps];
  const row = $$(".mod-result", dialog.host).find((r) => r.dataset.slug === project.slug);
  const pipeline = row ? $(".pipeline", row) : null;
  const depBox = row ? $(".pipeline-deps", row) : null;
  const stages = row ? pipelineStages(row) : null;

  state.modBrowser.busy = true;
  state.modBrowser.installingSlug = project.slug;

  const progress = $("#mbProgress", dialog.host);
  const status = $("#mbStatus", dialog.host);
  progress.hidden = false;
  progress.firstElementChild.style.width = "0%";

  if (row) {
    row.classList.add("is-installing");
    $$("[data-install]", dialog.host).forEach((b) => { b.disabled = true; });
    const btn = $("[data-install]", row);
    if (btn) btn.innerHTML = `<span class="spinner"></span> Đang cài`;
    if (pipeline) pipeline.hidden = false;
    if (depBox) {
      depBox.innerHTML = deps
        .map((d, i) => `<span class="dep-chip" style="--d:${i}">+ ${esc(d.name)}</span>`)
        .join("");
    }
  }

  const updateStages = (pct) => {
    if (!stages) return;
    setStageState(stages.download, pct >= 45 ? "done" : "active");
    setStageState(stages.verify, pct >= 70 ? "done" : pct >= 45 ? "active" : "");
    setStageState(stages.install, pct >= 100 ? "done" : pct >= 70 ? "active" : "");
  };

  let added = 0;
  for (let qi = 0; qi < queue.length; qi++) {
    const item = queue[qi];
    const isDependency = qi > 0;

    for (let p = 0; p < 100; p += rand(10, 16)) {
      const pct = Math.min(p, 100);
      const overall = Math.round((qi * 100 + pct) / queue.length);
      progress.firstElementChild.style.width = overall + "%";
      status.textContent = isDependency
        ? `Đang cài dependency ${item.name}... ${overall}%`
        : pct < 55
          ? `Đang tải ${item.name}... ${overall}%`
          : `Đang cài ${item.name}... ${overall}%`;
      updateStages(pct);
      await sleep(80);
    }

    updateStages(100);
    if (addInstalledMod(item, inst)) added++;
    if (state.screen === "detail") renderMods();
  }

  progress.firstElementChild.style.width = "100%";
  progress.hidden = true;
  progress.firstElementChild.style.width = "0%";

  state.modBrowser.busy = false;
  state.modBrowser.installingSlug = null;

  const depCount = Math.max(0, added - 1);
  status.textContent = depCount > 0
    ? `Đã cài ${project.name} (+${depCount} dependency)`
    : `Đã cài ${project.name}`;

  saveState();
  if (state.screen === "detail") renderMods();

  // Pipeline gập lại sau khi xong, rồi trả hàng về trạng thái "Đã cài".
  if (pipeline) {
    pipeline.classList.add("is-complete");
    await sleep(360);
  }
  renderModResults(dialog, { preserveStatus: true });
}

/* ------------------------------------------------------------
   ACCOUNT DIALOGS
   ------------------------------------------------------------ */
function openAddOfflineDialog() {
  const dialog = showDialog({
    title: "Thêm tài khoản Offline",
    primary: "Thêm",
    closeText: "Hủy",
    body: `
      <label class="field"><span>Tên người chơi</span>
        <input class="textbox" id="offName" placeholder="Steve" value="" />
      </label>
      <div class="infobar error" id="offError" hidden><span class="glyph">&#xE7BA;</span><div id="offErrorText"></div></div>`,
    onPrimary: () => {
      const name = $("#offName").value.trim();
      if (!name) {
        $("#offError").hidden = false;
        $("#offErrorText").textContent = "Tên không được để trống.";
        return false;
      }
      if (state.accounts.some((a) => a.name.toLowerCase() === name.toLowerCase())) {
        $("#offError").hidden = false;
        $("#offErrorText").textContent = "Tài khoản này đã tồn tại.";
        return false;
      }
      state.accounts.push({ id: "acc-" + Date.now(), name, kind: "Offline", active: false });
      saveState();
      renderAccounts();
      accountStatus(`Đã thêm tài khoản offline "${name}".`);
      return true;
    },
  });
  setTimeout(() => $("#offName", dialog.host).focus(), 50);
}

async function addMicrosoftAccount() {
  accountStatus("Đang mở trình duyệt để đăng nhập Microsoft...");
  await sleep(1600);

  // Demo: mỗi lần đăng nhập trả về một tên khác nhau để không trùng.
  const base = "MCPlayer";
  let name = base;
  let n = 2;
  while (state.accounts.some((a) => a.name.toLowerCase() === name.toLowerCase())) {
    name = base + n++;
  }

  state.accounts.push({ id: "acc-" + Date.now(), name, kind: "Microsoft", active: false });
  saveState();
  renderAccounts();
  accountStatus(`Đã thêm tài khoản Microsoft "${name}" (demo).`);
}

/* ------------------------------------------------------------
   MODPACK / OPTIFINE (mô phỏng installer)
   ------------------------------------------------------------ */
const MRPACK_DEMO = {
  name: "Fabulously Optimized 6.5.0.mrpack",
  projects: ["AANobbMI", "gvQqBUqZ", "YL57xq9U", "mOgUt4GM", "uXXizFIs"],
};

function installMrpack() {
  const inst = currentInstance();
  if (!inst) return;

  const dialog = showDialog({
    title: "Install .mrpack",
    primary: "Cài",
    closeText: "Hủy",
    body: `
      <label class="field"><span>File modpack</span>
        <input class="textbox" id="mrpackPath" value="C:\\Users\\TOSHIBA\\Downloads\\${MRPACK_DEMO.name}" readonly />
      </label>
      <div class="infobar info"><span class="glyph">&#xE946;</span><div>Demo: file được chọn sẵn. App thật mở hộp thoại chọn file .mrpack.</div></div>
      <div class="result-status" id="mrpackStatus" hidden>
        <span class="spinner" style="color:var(--accent)"></span><span id="mrpackStatusText"></span>
      </div>
      <div class="progress thin" id="mrpackProgress" hidden><div></div></div>`,
    onPrimary: async () => {
      const btn = $("[data-dialog-primary]", dialog.host);
      btn.disabled = true;
      $("#mrpackStatus").hidden = false;
      $("#mrpackProgress").hidden = false;

      const phases = [
        "Đang tải modpack... 20%",
        "Đang giải nén overrides... 45%",
        "Đang resolve 5 mod... 70%",
        "Đang cài mod... 90%",
      ];

      for (let i = 0; i < phases.length; i++) {
        $("#mrpackStatusText").textContent = phases[i];
        $("#mrpackProgress").firstElementChild.style.width = `${(i + 1) * 22}%`;
        await sleep(600);
      }

      let added = 0;
      MRPACK_DEMO.projects.forEach((id) => {
        const project = catalogProject(id);
        if (project && addInstalledMod(project, inst)) added++;
      });

      $("#mrpackProgress").firstElementChild.style.width = "100%";
      $("#mrpackStatusText").textContent = `Đã cài ${MRPACK_DEMO.name} (${added} mod)`;
      renderMods();
      saveState();
      setDetailStatus(`Đã cài modpack "${MRPACK_DEMO.name}" (${added} mod).`);
      await sleep(500);
      return true;
    },
  });

  return dialog;
}

function installOptiFine() {
  const inst = currentInstance();
  if (!inst) return;

  const dialog = showDialog({
    title: "Install OptiFine",
    primary: "Cài",
    closeText: "Hủy",
    body: `
      <label class="field"><span>Phiên bản OptiFine</span>
        <select class="select" id="ofVersion">
          <option>1.21.1</option><option>1.21</option><option>1.20.1</option><option>1.19.2</option>
        </select>
      </label>
      <div class="infobar info"><span class="glyph">&#xE946;</span><div>Demo: tải installer và patch vào instance (không chạy file thật).</div></div>
      <div class="progress thin" id="ofProgress" hidden><div></div></div>
      <div class="caption text-2" id="ofStatus"></div>`,
    onPrimary: async () => {
      const btn = $("[data-dialog-primary]", dialog.host);
      btn.disabled = true;
      const version = $("#ofVersion").value;
      const progress = $("#ofProgress");
      const status = $("#ofStatus");
      progress.hidden = false;

      for (let p = 0; p <= 100; p += 10) {
        progress.firstElementChild.style.width = p + "%";
        status.textContent = `Đang cài OptiFine ${version}... ${p}%`;
        await sleep(100);
      }

      const mods = (state.mods[inst.id] ||= []);
      const file = `OptiFine_${version}_HD_U_I6.jar`;
      if (!mods.some((m) => m.file === file)) {
        mods.push({ file, version, enabled: true, update: null });
      }

      renderMods();
      saveState();
      setDetailStatus(`Đã cài OptiFine ${version}.`);
      await sleep(400);
      return true;
    },
  });

  return dialog;
}

/* ------------------------------------------------------------
   EVENT WIRING
   ------------------------------------------------------------ */
/* ------------------------------------------------------------
   COMMAND PALETTE (Ctrl+K)
   ------------------------------------------------------------ */
function paletteActions() {
  const items = [];

  state.instances.forEach((inst) => {
    items.push({
      icon: "E7F8",
      title: inst.name,
      sub: `${inst.mc} · ${inst.loader} — mở instance`,
      keywords: "mở open instance chi tiết",
      run: () => openDetail(inst.id),
    });
    items.push({
      icon: inst.running ? "E71A" : "E768",
      title: `${inst.running ? "Dừng" : "Chơi"} ${inst.name}`,
      sub: inst.running ? `Đang chạy · PID ${inst.pid}` : "Khởi chạy instance",
      keywords: "launch play stop chơi dừng",
      run: () => (inst.running ? stopInstance(inst.id) : launchInstance(inst.id)),
    });
  });

  items.push(
    { icon: "E77B", title: "Accounts", sub: "Mở trang tài khoản", keywords: "account tài khoản", run: () => showScreen("accounts") },
    { icon: "E710", title: "Tạo instance mới", sub: "Mở dialog New Instance", keywords: "new tạo instance", run: openNewInstanceDialog },
    {
      icon: "E793",
      title: "Đổi giao diện sáng / tối",
      sub: "Chuyển theme Dark ↔ Light",
      keywords: "theme dark light sáng tối giao diện",
      run: () => $("#themeToggle").click(),
    }
  );

  if (state.screen === "detail") {
    items.push(
      { icon: "E710", title: "Download Mods", sub: "Mở Mod Browser cho instance này", keywords: "mod browser tải cài mod", run: openModBrowser },
      { icon: "E838", title: "Mở thư mục mods", sub: currentInstance()?.path || "", keywords: "folder thư mục mods", run: () => showToastStatus("Đã mở thư mục mods (demo).") }
    );
  }

  return items;
}

function refreshPalette(query) {
  const tokens = query.trim().toLowerCase().split(/\s+/).filter(Boolean);
  const all = paletteActions();
  const items = all.filter((it) => {
    const hay = `${it.title} ${it.sub} ${it.keywords}`.toLowerCase();
    return tokens.every((t) => hay.includes(t));
  });
  state.palette.items = items;
  state.palette.index = 0;
  renderPalette();
}

function renderPalette() {
  const list = $("#paletteList");
  const items = state.palette.items;
  const index = state.palette.index;

  if (items.length === 0) {
    list.innerHTML = `<div class="palette-empty">Không có lệnh nào khớp.</div>`;
    return;
  }

  list.innerHTML = items.map((it, i) => `
    <button class="palette-item${i === index ? " is-selected" : ""}" data-palette-index="${i}" role="option" aria-selected="${i === index}">
      <span class="glyph palette-icon">&#x${it.icon};</span>
      <span class="palette-main">
        <span class="palette-title">${esc(it.title)}</span>
        <span class="palette-sub">${esc(it.sub)}</span>
      </span>
      <span class="glyph palette-enter">&#xE751;</span>
    </button>`).join("");

  const selected = $(".palette-item.is-selected", list);
  if (selected) selected.scrollIntoView({ block: "nearest" });
}

function openPalette() {
  state.palette.open = true;
  const host = $("#paletteHost");
  host.hidden = false;
  $("#paletteInput").value = "";
  refreshPalette("");
  requestAnimationFrame(() => {
    host.classList.add("is-open");
    $("#paletteInput").focus();
  });
}

function closePalette() {
  state.palette.open = false;
  const host = $("#paletteHost");
  host.classList.remove("is-open");
  host.hidden = true;
}

function movePalette(delta) {
  const count = state.palette.items.length;
  if (count === 0) return;
  state.palette.index = (state.palette.index + delta + count) % count;
  renderPalette();
}

function runPaletteItem(index) {
  const item = state.palette.items[index];
  if (!item) return;
  closePalette();
  item.run();
}

/* ------------------------------------------------------------
   EVENTS
   ------------------------------------------------------------ */
function wireEvents() {
  // Nav
  $$(".nav-item").forEach((btn) => {
    btn.addEventListener("click", () => {
      const target = btn.dataset.screen;
      if (target === "instances" && state.screen === "detail") closeDetail();
      else showScreen(target);
    });
  });

  // Instances
  $("#newInstanceBtn").addEventListener("click", openNewInstanceDialog);

  $("#instanceGrid").addEventListener("pointermove", (e) => {
    const card = e.target.closest(".instance-card");
    if (!card) return;
    const r = card.getBoundingClientRect();
    card.style.setProperty("--px", `${e.clientX - r.left}px`);
    card.style.setProperty("--py", `${e.clientY - r.top}px`);
  }, { passive: true });

  window.addEventListener("resize", () => {
    moveNavGlider();
    moveTabGlider();
  });

  $("#instanceSearch").addEventListener("input", (e) => {
    state.instanceQuery = e.target.value;
    renderInstances();
  });

  $("#instanceSort").addEventListener("change", (e) => {
    state.instanceSort = e.target.value;
    renderInstances();
  });

  $("#instanceGrid").addEventListener("click", async (e) => {
    const card = e.target.closest(".instance-card");
    if (!card) return;
    const id = card.dataset.id;
    const action = e.target.closest("[data-action]")?.dataset.action;

    if (action === "play") {
      e.stopPropagation();
      if (getInstance(id)?.running) stopInstance(id);
      else await launchInstance(id);
    } else if (action === "edit") {
      e.stopPropagation();
      openDetail(id, card);
    } else {
      openDetail(id, card);
    }
  });

  $("#instanceGrid").addEventListener("contextmenu", (e) => {
    const card = e.target.closest(".instance-card");
    if (!card) return;
    e.preventDefault();
    openContextMenu(e.clientX, e.clientY, card.dataset.id);
  });

  $("#instanceGrid").addEventListener("keydown", (e) => {
    if (e.key !== "Enter" && e.key !== " ") return;
    const card = e.target.closest(".instance-card");
    if (card) { e.preventDefault(); openDetail(card.dataset.id, card); }
  });

  $("#contextMenu").addEventListener("click", async (e) => {
    const menu = $("#contextMenu");
    const modBtn = e.target.closest("[data-modmenu]");
    if (modBtn) {
      const index = Number(menu.dataset.modIndex);
      menu.hidden = true;
      await handleModMenu(modBtn.dataset.modmenu, index);
      return;
    }

    const btn = e.target.closest("[data-ctx]");
    if (!btn) return;
    const id = menu.dataset.instanceId;
    const inst = getInstance(id);
    menu.hidden = true;

    switch (btn.dataset.ctx) {
      case "play": await launchInstance(id); break;
      case "stop": stopInstance(id); break;
      case "edit": openDetail(id); break;
      case "rename": openRenameDialog(id); break;
      case "folder": showToastStatus("Đã mở thư mục instance (demo)."); break;
      case "delete":
        if (await confirmDialog(`Xóa "${inst.name}"?`, "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.")) {
          state.instances = state.instances.filter((i) => i.id !== id);
          delete state.mods[id];
          delete state.worlds[id];
          delete state.logs[id];
          if (state.logTimers[id]) {
            clearInterval(state.logTimers[id]);
            delete state.logTimers[id];
          }
          ensureActiveInstance();
          saveState();
          renderInstances();
        }
        break;
    }
  });

  // Detail header
  $("#detailBackBtn").addEventListener("click", closeDetail);
  $("#detailLaunchBtn").addEventListener("click", () => {
    const inst = currentInstance();
    if (!inst) return;
    if (inst.running) stopInstance(inst.id);
    else launchInstance(inst.id, { fromDetail: true });
  });

  // Tabs
  $$(".tab").forEach((tab) => tab.addEventListener("click", () => setTab(tab.dataset.tab)));

  // Mods toolbar
  $("#checkUpdatesBtn").addEventListener("click", checkUpdates);
  $("#updateAllBtn").addEventListener("click", updateAll);
  $("#openModBrowserBtn").addEventListener("click", openModBrowser);
  $("#openModsFolderBtn").addEventListener("click", () => showToastStatus("Đã mở thư mục mods (demo)."));
  $("#selectModsBtn").addEventListener("click", () => {
    state.modSelection.active = !state.modSelection.active;
    state.modSelection.files.clear();
    state.modSelection.lastIndex = null;
    renderMods();
    if (state.modSelection.active) setDetailStatus("Chọn mod để bật/tắt hoặc gỡ hàng loạt.");
  });
  $("#bulkBar").addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-bulk]");
    if (btn) await bulkModAction(btn.dataset.bulk);
  });
  $("#mrpackBtn").addEventListener("click", installMrpack);
  $("#optifineBtn").addEventListener("click", installOptiFine);

  // Mod list toggle (delegated)
  $("#modList").addEventListener("change", (e) => {
    const select = e.target.closest("[data-mod-select]");
    if (select) {
      toggleModSelection(Number(select.dataset.modSelect), { shift: e.shiftKey, checked: select.checked });
      return;
    }
    const input = e.target.closest("[data-mod-toggle]");
    if (!input) return;
    const mod = currentMods()[Number(input.dataset.modToggle)];
    if (mod) {
      mod.enabled = input.checked;
      saveState();
      setDetailStatus(`${input.checked ? "Đã bật" : "Đã tắt"} ${mod.file}.`);
      renderMods();
    }
  });

  // Mod row menu + multi-select (delegated)
  $("#modList").addEventListener("click", (e) => {
    const btn = e.target.closest("[data-mod-menu]");
    if (btn) {
      openModMenu(e.clientX, e.clientY, Number(btn.dataset.modMenu));
      return;
    }
    if (!state.modSelection.active) return;
    if (e.target.closest(".switch, .mod-check, .mod-menu-btn")) return;
    const row = e.target.closest("[data-mod-row]");
    if (row) toggleModSelection(Number(row.dataset.modRow), { shift: e.shiftKey });
  });

  // Worlds
  $("#worldList").addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-world-backup]");
    if (!btn) return;
    await backupWorld(Number(btn.dataset.worldBackup));
  });
  $("#backupAllWorldsBtn").addEventListener("click", backupAllWorlds);
  $("#openWorldsFolderBtn").addEventListener("click", () => showToastStatus("Đã mở thư mục saves (demo)."));

  // Settings
  $("#setRam").addEventListener("input", (e) => {
    $("#setRamValue").value = e.target.value;
  });
  $("#setRamValue").addEventListener("input", (e) => {
    const value = clamp(parseInt(e.target.value, 10) || DEFAULT_SETTINGS.ramMb, 1024, 16384);
    $("#setRam").value = value;
  });
  $("#saveSettingsBtn").addEventListener("click", saveSettings);
  $("#resetSettingsBtn").addEventListener("click", resetSettings);

  // Logs toolbar
  $("#clearLogBtn").addEventListener("click", () => {
    state.logs[state.activeInstanceId] = [];
    state.logRenderKey = null;
    renderLogs();
    updateLogJump();
  });
  $("#copyLogBtn").addEventListener("click", async () => {
    const text = filteredLogLines().join("\n");
    try {
      await navigator.clipboard.writeText(text);
      $("#copyLogBtn").lastElementChild.textContent = "Đã copy";
      setTimeout(() => { $("#copyLogBtn").lastElementChild.textContent = "Copy to Clipboard"; }, 1500);
    } catch {
      // file:// / quyền clipboard bị chặn
      const ta = document.createElement("textarea");
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
    }
  });

  $("#logSearch").addEventListener("input", (e) => {
    state.logFilter.query = e.target.value;
    renderLogs();
  });

  $("#logLevels").addEventListener("click", (e) => {
    const chip = e.target.closest("[data-log-level]");
    if (!chip) return;
    state.logFilter.level = chip.dataset.logLevel;
    $$(".sev-chip").forEach((c) => c.classList.toggle("is-selected", c === chip));
    renderLogs();
  });

  $("#jumpFirstErrorBtn").addEventListener("click", jumpToFirstError);
  $("#logJumpBtn").addEventListener("click", () => {
    const view = $("#logView");
    view.scrollTo({ top: view.scrollHeight, behavior: prefersReducedMotion() ? "auto" : "smooth" });
  });
  $("#logView").addEventListener("scroll", updateLogJump, { passive: true });

  // Settings
  $("#deleteInstanceBtn").addEventListener("click", async () => {
    const inst = getInstance(state.activeInstanceId);
    if (await confirmDialog(`Xóa "${inst.name}"?`, "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.")) {
      state.instances = state.instances.filter((i) => i.id !== inst.id);
      delete state.mods[inst.id];
      delete state.worlds[inst.id];
      delete state.logs[inst.id];
      if (state.logTimers[inst.id]) {
        clearInterval(state.logTimers[inst.id]);
        delete state.logTimers[inst.id];
      }
      ensureActiveInstance();
      saveState();
      showScreen("instances");
    }
  });

  // Accounts
  $("#addOfflineBtn").addEventListener("click", openAddOfflineDialog);
  $("#addMicrosoftBtn").addEventListener("click", addMicrosoftAccount);
  $("#refreshActiveBtn").addEventListener("click", () => {
    const active = state.accounts.find((a) => a.active);
    accountStatus(active ? `Đã làm mới token cho "${active.name}" (demo).` : "Chưa có tài khoản active.");
  });
  $("#reloadAccountsBtn").addEventListener("click", () => {
    renderAccounts();
    accountStatus("Đã tải lại danh sách tài khoản (demo).");
  });

  $("#accountList").addEventListener("click", async (e) => {
    const card = e.target.closest("[data-account]");
    if (!card) return;
    const id = card.dataset.account;
    const action = e.target.closest("[data-acc-action]")?.dataset.accAction;
    const acc = getAccount(id);

    if (action === "activate") setActiveAccount(id);
    if (action === "remove") {
      if (acc.active) {
        await showAlert("Không thể xóa", "Đây là tài khoản đang active. Hãy đặt tài khoản khác làm active trước.");
        return;
      }
      if (await confirmDialog(`Xóa tài khoản "${acc.name}"?`, "Tài khoản sẽ bị xóa khỏi danh sách lưu trên máy.")) {
        state.accounts = state.accounts.filter((a) => a.id !== id);
        saveState();
        renderAccounts();
        accountStatus(`Đã xóa tài khoản "${acc.name}".`);
      }
    }
  });

  // Demo bar
  $("#simulateCrashBtn").addEventListener("click", simulateCrash);
  $("#themeToggle").addEventListener("click", () => {
    const html = document.documentElement;
    const next = html.dataset.theme === "dark" ? "light" : "dark";
    html.dataset.theme = next;
    $("#themeToggle").textContent = next === "dark" ? "Dark" : "Light";
    saveState();
  });
  $("#resetDemo").addEventListener("click", () => {
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* ignore */ }
    location.reload();
  });

  document.addEventListener("pointerdown", attachRipple);

  // Command palette
  $("#paletteInput").addEventListener("input", (e) => refreshPalette(e.target.value));
  $("#paletteInput").addEventListener("keydown", (e) => {
    if (e.key === "ArrowDown") { e.preventDefault(); movePalette(1); }
    else if (e.key === "ArrowUp") { e.preventDefault(); movePalette(-1); }
    else if (e.key === "Enter") { e.preventDefault(); runPaletteItem(state.palette.index); }
    else if (e.key === "Escape") { e.preventDefault(); closePalette(); }
  });
  $("#paletteList").addEventListener("click", (e) => {
    const item = e.target.closest("[data-palette-index]");
    if (item) runPaletteItem(Number(item.dataset.paletteIndex));
  });
  $("#paletteHost").addEventListener("click", (e) => {
    if (e.target === e.currentTarget) closePalette();
  });

  document.addEventListener("keydown", (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
      e.preventDefault();
      if (state.palette.open) closePalette();
      else openPalette();
      return;
    }
    if (e.key === "Escape") {
      $("#contextMenu").hidden = true;
      if (state.palette.open) closePalette();
    }
  });
}

function setDetailStatus(text) {
  $("#detailStatus").textContent = text;
}

function showToastStatus(text) {
  const toast = document.createElement("div");
  toast.className = "toast";
  toast.textContent = text;
  document.body.appendChild(toast);
  setTimeout(() => {
    toast.classList.add("is-out");
    setTimeout(() => toast.remove(), 240);
  }, 1900);
}

/* ------------------------------------------------------------
   INIT
   ------------------------------------------------------------ */
loadState();
normalizeState();
if (!state.instances.some((i) => i.id === state.activeInstanceId) && state.instances[0]) {
  state.activeInstanceId = state.instances[0].id;
}
$("#themeToggle").textContent = document.documentElement.dataset.theme === "light" ? "Light" : "Dark";
$("#instanceSort").value = state.instanceSort;

wireEvents();
renderInstances();
renderAccounts();
requestAnimationFrame(moveNavGlider);
