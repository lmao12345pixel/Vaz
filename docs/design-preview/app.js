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
    },
    {
      id: "vanilla-1.20.1",
      name: "Vanilla 1.20.1",
      mc: "1.20.1",
      loader: "vanilla",
      loaderVersion: null,
      path: "C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\vanilla-1.20.1",
      lastPlayed: "3 ngày trước",
    },
    {
      id: "neoforge-1.21.1",
      name: "NeoForge Modded",
      mc: "1.21.1",
      loader: "neoforge",
      loaderVersion: "21.1.72",
      path: "C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\neoforge-1.21.1",
      lastPlayed: "1 tuần trước",
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
  tab: "mods",
  modBrowser: {
    query: "",
    loader: "Auto",
    version: "Auto",
    sort: "Relevance",
    limit: MB_PAGE_SIZE,
    busy: false,
    installingSlug: null,
  },
};

/* ------------------------------------------------------------
   PERSISTENCE (mock state — để cài mod xong reload vẫn còn)
   ------------------------------------------------------------ */
function saveState() {
  try {
    const instances = state.instances.map((inst) => {
      const { launching, status, pid, ...rest } = inst;
      return rest;
    });
    localStorage.setItem(STORAGE_KEY, JSON.stringify({
      instances,
      mods: state.mods,
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

function modFileName(project, inst) {
  if (inst.loader === "fabric") return `${project.slug}-fabric-${project.version}+mc${inst.mc}.jar`;
  if (inst.loader === "neoforge") return `${project.slug}-neoforge-${project.version}.jar`;
  return `${project.slug}-${project.version}.jar`;
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
function showScreen(name) {
  state.screen = name;
  $$(".nav-item").forEach((b) => b.classList.toggle("is-selected", b.dataset.screen === name));
  $("#page-instances").hidden = name !== "instances";
  $("#page-accounts").hidden = name !== "accounts";
  $("#page-detail").hidden = name !== "detail";
  $("#content").scrollTop = 0;

  if (name === "instances") renderInstances();
  if (name === "accounts") renderAccounts();
}

function openDetail(id) {
  const inst = getInstance(id);
  if (!inst) return;
  state.activeInstanceId = id;
  state.tab = "mods";

  $("#detailName").textContent = inst.name;
  $("#detailVersion").textContent = inst.mc;
  $("#detailLoader").textContent = inst.loader + (inst.loaderVersion ? ` ${inst.loaderVersion}` : "");
  $("#detailLaunchStatus").textContent = inst.status || "";
  $("#detailStatus").textContent = "";
  $("#detailError").hidden = true;
  $("#settingsFolder").value = inst.path;

  setTab("mods");
  renderMods();
  renderLogs();
  if ((inst.status || "").startsWith("Đang chạy")) streamLogs(id);

  showScreen("detail");
}

/* ------------------------------------------------------------
   INSTANCES
   ------------------------------------------------------------ */
function renderInstances() {
  const grid = $("#instanceGrid");
  const empty = $("#instancesEmpty");

  grid.innerHTML = state.instances.map((inst) => {
    const spinner = inst.launching
      ? `<span class="spinner"></span>`
      : `<span class="glyph">&#xE768;</span>`;
    const status = inst.status
      ? `<div class="status">${esc(inst.status)}</div>`
      : "";

    return `
      <div class="instance-card" data-id="${esc(inst.id)}" tabindex="0">
        <div class="name">${esc(inst.name)}</div>
        <div class="meta">
          <span>${esc(inst.mc)}</span>
          <span>${esc(inst.loader)}</span>
        </div>
        ${status}
        <div class="card-actions">
          <button class="btn icon-only" data-action="play" ${inst.launching ? "disabled" : ""}>${spinner}</button>
          <button class="btn icon-only" data-action="edit"><span class="glyph">&#xE713;</span></button>
        </div>
      </div>`;
  }).join("");

  empty.hidden = state.instances.length > 0;
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

async function launchInstance(id, { fromDetail = false } = {}) {
  const inst = getInstance(id);
  if (!inst || inst.launching) return;

  if (!state.accounts.some((a) => a.active)) {
    await showAlert("Chưa có tài khoản", "Vui lòng chọn hoặc thêm tài khoản ở trang Accounts trước khi khởi chạy.");
    return;
  }

  inst.launching = true;
  setCardStatus(id, "Đang khởi chạy...");
  if (fromDetail) {
    $("#detailLaunchBtn").disabled = true;
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
  inst.launching = false;
  inst.pid = pid;
  setCardStatus(id, `Đang chạy (PID ${pid})`);
  // latest.log được truncate mỗi lần chạy game.
  state.logs[id] = [];
  saveState();

  if (fromDetail) {
    $("#detailLaunchBtn").disabled = false;
    $("#detailProgress").hidden = true;
    setDetailProgress(0);
    $("#detailStatus").textContent = "";
    state.tab = "logs";
    setTab("logs");
    streamLogs(id);
  }
}

function setDetailProgress(pct) {
  $("#detailProgress").firstElementChild.style.width = pct + "%";
}

/* ------------------------------------------------------------
   DETAIL — tabs
   ------------------------------------------------------------ */
function setTab(tab) {
  state.tab = tab;
  $$(".tab").forEach((t) => t.classList.toggle("is-selected", t.dataset.tab === tab));
  $("#tab-mods").hidden = tab !== "mods";
  $("#tab-logs").hidden = tab !== "logs";
  $("#tab-settings").hidden = tab !== "settings";
  if (tab === "logs") renderLogs();
}

/* ------------------------------------------------------------
   DETAIL — mods
   ------------------------------------------------------------ */
function renderMods() {
  const mods = currentMods();
  const list = $("#modList");
  const empty = $("#modsEmpty");

  list.innerHTML = mods.map((mod, i) => {
    const project = mod.projectId ? MOCK.catalog.find((p) => p.id === mod.projectId) : null;
    const label = project ? project.name : mod.file;
    return `
    <div class="list-row mod-row${mod.enabled ? "" : " is-disabled"}">
      <span class="glyph">&#xE74C;</span>
      <div class="mod-main">
        <div class="mod-file" title="${esc(mod.file)}">${esc(mod.file)}</div>
        <div class="mod-version">${esc(label)} · v${esc(mod.version)}</div>
      </div>
      ${mod.update ? `<span class="badge">Cập nhật ${esc(mod.update)}</span>` : ""}
      <label class="switch">
        <input type="checkbox" data-mod-toggle="${i}" ${mod.enabled ? "checked" : ""} />
        <span class="track"></span>
      </label>
    </div>`;
  }).join("");

  empty.hidden = mods.length > 0;
  const updates = mods.filter((m) => m.update).length;
  $("#updatesBar").hidden = updates === 0;
  $("#updatesBarText").textContent = `${updates} mod có bản cập nhật.`;
}

async function checkUpdates() {
  const mods = currentMods();
  if (mods.length === 0) {
    setDetailStatus("Chưa có mod nào để kiểm tra.");
    return;
  }

  setDetailStatus("Đang kiểm tra cập nhật...");
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

  renderMods();
  saveState();
  setDetailStatus(count > 0
    ? `Có ${count} mod có bản cập nhật mới.`
    : "Tất cả mod đã là bản mới nhất.");
}

async function updateAll() {
  const mods = currentMods();
  const pending = mods.filter((m) => m.update);
  if (pending.length === 0) {
    setDetailStatus("Không có mod nào cần cập nhật.");
    return;
  }

  const inst = currentInstance();
  $("#detailProgress").hidden = false;
  let done = 0;

  for (const mod of pending) {
    const project = mod.projectId ? MOCK.catalog.find((p) => p.id === mod.projectId) : null;
    const label = project ? project.name : mod.file;
    const from = mod.version;
    const to = mod.update;

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
    renderMods();
  }

  $("#detailProgress").hidden = true;
  setDetailProgress(0);
  renderMods();
  saveState();
  setDetailStatus(`Đã cập nhật xong ${done} mod.`);
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

function renderLogs() {
  const lines = state.logs[state.activeInstanceId] || [];
  const view = $("#logView");
  const empty = $("#logsEmpty");

  view.hidden = lines.length === 0;
  empty.hidden = lines.length > 0;
  view.textContent = lines.join("\n");

  if (lines.length && $("#autoScrollChk").checked) {
    view.scrollTop = view.scrollHeight;
  }
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
  menu.innerHTML = `
    <button data-ctx="play"><span class="glyph">&#xE768;</span>Play</button>
    <button data-ctx="edit"><span class="glyph">&#xE713;</span>Edit</button>
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
  host.hidden = false;
  host.innerHTML = `
    <div class="dialog ${wide ? "wide" : ""}" role="dialog" aria-modal="true">
      <h2 class="dialog-title">${esc(title)}</h2>
      <div class="dialog-body">${body}</div>
      <div class="dialog-footer">
        ${primary ? `<button class="btn accent" data-dialog-primary>${esc(primary)}</button>` : ""}
        <button class="btn" data-dialog-close>${esc(closeText)}</button>
      </div>
    </div>`;

  const close = () => { host.hidden = true; host.innerHTML = ""; };
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
      });
      state.mods[id] = [];
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
      <div class="mod-result${installing ? " is-installing" : ""}">
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

async function installMod(dialog, slug) {
  const inst = currentInstance();
  const project = catalogProject(slug);
  if (!inst || !project || state.modBrowser.busy) return;
  if (isProjectInstalled(inst.id, project)) return;

  const deps = resolveDependencies(project, inst);
  const queue = [project, ...deps];

  state.modBrowser.busy = true;
  state.modBrowser.installingSlug = project.slug;

  const progress = $("#mbProgress", dialog.host);
  const status = $("#mbStatus", dialog.host);
  progress.hidden = false;
  progress.firstElementChild.style.width = "0%";
  status.textContent = `Đang tải ${project.name}... 0%`;
  renderModResults(dialog, { preserveStatus: true });

  let added = 0;

  for (const item of queue) {
    const isDependency = item !== project;

    for (let p = 0; p <= 100; p += rand(8, 14)) {
      const pct = Math.min(p, 100);
      progress.firstElementChild.style.width = pct + "%";
      status.textContent = isDependency
        ? `Đang cài dependency ${item.name}... ${pct}%`
        : `Đang tải ${item.name}... ${pct}%`;
      await sleep(90);
    }

    if (addInstalledMod(item, inst)) added++;
    if (state.screen === "detail") renderMods();
  }

  state.modBrowser.busy = false;
  state.modBrowser.installingSlug = null;
  progress.hidden = true;
  progress.firstElementChild.style.width = "0%";
  renderModResults(dialog, { preserveStatus: true });

  const depCount = Math.max(0, added - 1);
  status.textContent = depCount > 0
    ? `Đã cài ${project.name} (+${depCount} dependency)`
    : `Đã cài ${project.name}`;

  saveState();
  if (state.screen === "detail") renderMods();
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
function wireEvents() {
  // Nav
  $$(".nav-item").forEach((btn) => {
    btn.addEventListener("click", () => showScreen(btn.dataset.screen));
  });

  // Instances
  $("#newInstanceBtn").addEventListener("click", openNewInstanceDialog);

  $("#instanceGrid").addEventListener("click", async (e) => {
    const card = e.target.closest(".instance-card");
    if (!card) return;
    const id = card.dataset.id;
    const action = e.target.closest("[data-action]")?.dataset.action;

    if (action === "play") {
      e.stopPropagation();
      await launchInstance(id);
    } else if (action === "edit") {
      e.stopPropagation();
      openDetail(id);
    } else {
      openDetail(id);
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
    if (card) { e.preventDefault(); openDetail(card.dataset.id); }
  });

  $("#contextMenu").addEventListener("click", async (e) => {
    const btn = e.target.closest("[data-ctx]");
    const menu = $("#contextMenu");
    if (!btn) return;
    const id = menu.dataset.instanceId;
    const inst = getInstance(id);
    menu.hidden = true;

    switch (btn.dataset.ctx) {
      case "play": await launchInstance(id); break;
      case "edit": openDetail(id); break;
      case "folder": showToastStatus("Đã mở thư mục instance (demo)."); break;
      case "delete":
        if (await confirmDialog(`Xóa "${inst.name}"?`, "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.")) {
          state.instances = state.instances.filter((i) => i.id !== id);
          delete state.mods[id];
          ensureActiveInstance();
          saveState();
          renderInstances();
        }
        break;
    }
  });

  // Detail header
  $("#detailLaunchBtn").addEventListener("click", () => {
    launchInstance(state.activeInstanceId, { fromDetail: true });
  });

  // Tabs
  $$(".tab").forEach((tab) => tab.addEventListener("click", () => setTab(tab.dataset.tab)));

  // Mods toolbar
  $("#checkUpdatesBtn").addEventListener("click", checkUpdates);
  $("#updateAllBtn").addEventListener("click", updateAll);
  $("#openModBrowserBtn").addEventListener("click", openModBrowser);
  $("#openModsFolderBtn").addEventListener("click", () => showToastStatus("Đã mở thư mục mods (demo)."));
  $("#mrpackBtn").addEventListener("click", installMrpack);
  $("#optifineBtn").addEventListener("click", installOptiFine);

  // Mod list toggle (delegated)
  $("#modList").addEventListener("change", (e) => {
    const input = e.target.closest("[data-mod-toggle]");
    if (!input) return;
    const mod = currentMods()[Number(input.dataset.modToggle)];
    if (mod) {
      mod.enabled = input.checked;
      saveState();
      setDetailStatus(`${input.checked ? "Đã bật" : "Đã tắt"} ${mod.file}.`);
    }
  });

  // Logs toolbar
  $("#clearLogBtn").addEventListener("click", () => {
    state.logs[state.activeInstanceId] = [];
    renderLogs();
  });
  $("#copyLogBtn").addEventListener("click", async () => {
    const text = (state.logs[state.activeInstanceId] || []).join("\n");
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

  // Settings
  $("#deleteInstanceBtn").addEventListener("click", async () => {
    const inst = getInstance(state.activeInstanceId);
    if (await confirmDialog(`Xóa "${inst.name}"?`, "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.")) {
      state.instances = state.instances.filter((i) => i.id !== inst.id);
      delete state.mods[inst.id];
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

  document.addEventListener("keydown", (e) => {
    if (e.key === "Escape") $("#contextMenu").hidden = true;
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
  setTimeout(() => toast.remove(), 1800);
}

/* ------------------------------------------------------------
   INIT
   ------------------------------------------------------------ */
loadState();
if (!state.instances.some((i) => i.id === state.activeInstanceId) && state.instances[0]) {
  state.activeInstanceId = state.instances[0].id;
}
$("#themeToggle").textContent = document.documentElement.dataset.theme === "light" ? "Light" : "Dark";

wireEvents();
renderInstances();
renderAccounts();
