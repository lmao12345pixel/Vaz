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
      { file: "sodium-fabric-0.6.13+mc1.21.1.jar", version: "0.6.13", enabled: true, update: null },
      { file: "lithium-fabric-0.14.3+mc1.21.1.jar", version: "0.14.3", enabled: true, update: "0.15.0" },
    ],
    "vanilla-1.20.1": [],
    "neoforge-1.21.1": [
      { file: "jei-1.21.1-neoforge-19.21.0.247.jar", version: "19.21.0", enabled: true, update: null },
      { file: "ferritecore-7.0.1-neoforge.jar", version: "7.0.1", enabled: true, update: null },
      { file: "cloth-config-15.0.140-neoforge.jar", version: "15.0.140", enabled: false, update: null },
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
  ],
};

const TOTAL_RESULTS = 19_628;

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
    loader: "any",
    version: "any",
    sort: "relevance",
    limit: 10,
    installed: {},
    busy: false,
  },
};

const getInstance = (id) => state.instances.find((i) => i.id === id);

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
  setCardStatus(id, `Đang chạy (PID ${pid})`);

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
function currentMods() {
  return state.mods[state.activeInstanceId] || [];
}

function renderMods() {
  const mods = currentMods();
  const list = $("#modList");
  const empty = $("#modsEmpty");

  list.innerHTML = mods.map((mod, i) => `
    <div class="list-row mod-row">
      <span class="glyph">&#xE74C;</span>
      <div>
        <div class="mod-file">${esc(mod.file)}</div>
        <div class="mod-version">${esc(mod.version)}</div>
      </div>
      <span class="badge">${mod.update ? "Cập nhật " + esc(mod.update) : ""}</span>
      <label class="switch">
        <input type="checkbox" data-mod-toggle="${i}" ${mod.enabled ? "checked" : ""} />
        <span class="track"></span>
      </label>
    </div>`).join("");

  empty.hidden = mods.length > 0;
  const updates = mods.filter((m) => m.update).length;
  $("#updatesBar").hidden = updates === 0;
  $("#updatesBarText").textContent = `${updates} mod có bản cập nhật.`;
}

async function checkUpdates() {
  const mods = currentMods();
  $("#detailStatus").textContent = "Đang kiểm tra cập nhật...";
  await sleep(900);
  if (mods[1]) mods[1].update = "0.15.0";
  renderMods();
  $("#detailStatus").textContent = mods.some((m) => m.update)
    ? "Có bản cập nhật mới."
    : "Tất cả mod đã là bản mới nhất.";
}

async function updateAll() {
  const mods = currentMods();
  if (!mods.some((m) => m.update)) {
    $("#detailStatus").textContent = "Không có mod nào cần cập nhật.";
    return;
  }
  $("#detailProgress").hidden = false;
  for (let p = 0; p <= 100; p += 10) {
    setDetailProgress(p);
    $("#detailStatus").textContent = `Đang cập nhật mod... ${p}%`;
    await sleep(80);
  }
  mods.forEach((m) => { m.update = null; });
  renderMods();
  $("#detailProgress").hidden = true;
  setDetailProgress(0);
  $("#detailStatus").textContent = "Đã cập nhật xong.";
}

/* ------------------------------------------------------------
   DETAIL — logs
   ------------------------------------------------------------ */
function logLines(inst) {
  return [
    `[${now()}] [main/INFO]: Loading Minecraft ${inst.mc} with ${inst.loader} ${inst.loaderVersion || ""}`.trim(),
    `[${now()}] [main/INFO]: Loading ${34 + rand(0, 6)} mods:`,
    `\t- fabric-api 0.115.0+1.21.1`,
    `\t- lithium 0.15.0`,
    `\t- sodium 0.6.13+mc1.21.1`,
    `[${now()}] [main/INFO]: SpongePowered MIXIN is active.`,
    `[${now()}] [Render thread/INFO]: Setting user: Steve`,
    `[${now()}] [Render thread/INFO]: Backend library: LWJGL version 3.3.3+5`,
    `[${now()}] [Render thread/INFO]: OpenGL debug context created`,
    `[${now()}] [Render thread/INFO]: Narrator library for x64 successfully loaded`,
    `[${now()}] [Render thread/INFO]: Reloading ResourceManager: vanilla, fabric`,
    `[${now()}] [Worker-Main-1/INFO]: Found 12 mods with data generators`,
    `[${now()}] [Render thread/INFO]: Sound engine started`,
    `[${now()}] [Render thread/INFO]: Created: 1024x512x4 minecraft:textures/atlas/blocks.png`,
    `[${now()}] [Render thread/INFO]: Loaded 1287 recipes`,
    `[${now()}] [Render thread/INFO]: Connecting to localhost:25565`,
    `[${now()}] [Netty Client IO #0/INFO]: Connected to a modded server.`,
    `[${now()}] [Render thread/INFO]: Sodium: Using GL_ARB_buffer_storage`,
  ];
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
      renderInstances();
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
  const inst = getInstance(state.activeInstanceId);
  state.modBrowser.limit = 10;
  state.modBrowser.installed = {};
  currentMods().forEach((m) => { state.modBrowser.installed[m.version] = true; });

  const dialog = showDialog({
    title: "Download Mods",
    closeText: "Đóng",
    wide: true,
    body: `
      <div class="searchbox">
        <span class="glyph">&#xE721;</span>
        <input id="mbSearch" placeholder="Tìm mod trên Modrinth..." autocomplete="off" />
        <button class="btn icon-only" id="mbClear" hidden style="min-height:26px;width:26px;border:0;background:transparent">
          <span class="glyph">&#xE711;</span>
        </button>
      </div>

      <div class="filters">
        <label class="field"><span>Loader</span>
          <select class="select" id="mbLoader">
            <option value="any">Any</option>
            <option value="fabric">Fabric</option>
            <option value="neoforge">NeoForge</option>
          </select>
        </label>
        <label class="field"><span>MC version</span>
          <select class="select" id="mbVersion">
            <option value="any">Any</option>
            <option>1.21.1</option>
            <option>1.20.1</option>
            <option>1.19.2</option>
          </select>
        </label>
        <label class="field"><span>Sort</span>
          <select class="select" id="mbSort">
            <option value="relevance">Relevance</option>
            <option value="downloads">Downloads</option>
            <option value="name">Name</option>
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
  let timer = null;

  const update = () => {
    state.modBrowser.query = search.value;
    $("#mbClear", dialog.host).hidden = search.value.length === 0;
    renderModResults(dialog);
  };

  search.addEventListener("input", () => {
    clearTimeout(timer);
    $("#mbStatus", dialog.host).textContent = "Đang tìm...";
    timer = setTimeout(update, 300);
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

  $("#mbLoader", dialog.host).onchange = (e) => { state.modBrowser.loader = e.target.value; update(); };
  $("#mbVersion", dialog.host).onchange = (e) => { state.modBrowser.version = e.target.value; update(); };
  $("#mbSort", dialog.host).onchange = (e) => { state.modBrowser.sort = e.target.value; update(); };

  $("#mbLoadMore", dialog.host).onclick = () => {
    state.modBrowser.limit += 8;
    renderModResults(dialog);
  };

  $("#mbResults", dialog.host).addEventListener("click", (e) => {
    const btn = e.target.closest("[data-install]");
    if (btn) installMod(dialog, btn.dataset.install);
  });

  renderModResults(dialog);
  setTimeout(() => search.focus(), 50);
}

function filteredCatalog() {
  const { query, loader, version, sort } = state.modBrowser;
  const tokens = query.toLowerCase().split(/\s+/).filter(Boolean);

  let items = MOCK.catalog.filter((p) => {
    if (loader !== "any" && !p.loaders.includes(loader)) return false;
    if (version !== "any" && !p.versions.includes(version)) return false;
    const hay = `${p.name} ${p.slug} ${p.desc} ${p.author}`.toLowerCase();
    return tokens.every((t) => hay.includes(t));
  });

  if (sort === "downloads") items = [...items].sort((a, b) => b.downloads - a.downloads);
  if (sort === "name") items = [...items].sort((a, b) => a.name.localeCompare(b.name));

  return items;
}

function fmtDownloads(n) {
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1).replace(".0", "") + "M";
  if (n >= 1_000) return (n / 1_000).toFixed(1).replace(".0", "") + "K";
  return String(n);
}

function renderModResults(dialog) {
  const items = filteredCatalog();
  const shown = items.slice(0, state.modBrowser.limit);
  const list = $("#mbResults", dialog.host);
  const status = $("#mbStatus", dialog.host);

  list.innerHTML = shown.map((p) => {
    const installed = state.modBrowser.installed[p.version] === true;
    return `
      <div class="mod-result">
        <img class="mod-icon" src="https://cdn.modrinth.com/data/${p.id}/icon.png" alt="" data-fallback="${esc(p.name[0])}" />
        <div>
          <div class="mod-title">${esc(p.name)}</div>
          <div class="mod-author">${esc(p.author)}</div>
          <div class="mod-desc">${esc(p.desc)}</div>
        </div>
        <div class="mod-side">
          <span class="mod-downloads">${fmtDownloads(p.downloads)}</span>
          <button class="btn" data-install="${esc(p.slug)}" ${installed || state.modBrowser.busy ? "disabled" : ""}>
            ${installed ? "Đã cài" : "Cài"}
          </button>
        </div>
      </div>`;
  }).join("");

  $("#mbCount", dialog.host).textContent = `${shown.length} / ${TOTAL_RESULTS.toLocaleString("en-US")} results`;
  status.textContent = "";
  if (items.length === 0) {
    list.innerHTML = `<div class="empty-state small"><span class="glyph empty-icon">&#xE721;</span><p class="text-2">Không tìm thấy mod nào phù hợp.</p></div>`;
  }
  $("#mbLoadMore", dialog.host).hidden = shown.length >= items.length;

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
  const project = MOCK.catalog.find((p) => p.slug === slug);
  if (!project || state.modBrowser.busy) return;

  state.modBrowser.busy = true;
  const progress = $("#mbProgress", dialog.host);
  const status = $("#mbStatus", dialog.host);
  progress.hidden = false;
  status.textContent = `Đang tải ${project.name}... 0%`;
  renderModResults(dialog);
  $("#mbStatus", dialog.host).textContent = `Đang tải ${project.name}... 0%`;
  $("#mbProgress", dialog.host).hidden = false;

  for (let p = 0; p <= 100; p += rand(7, 13)) {
    const pct = Math.min(p, 100);
    $("#mbProgress", dialog.host).firstElementChild.style.width = pct + "%";
    $("#mbStatus", dialog.host).textContent = `Đang tải ${project.name}... ${pct}%`;
    await sleep(120);
  }

  state.modBrowser.installed[project.version] = true;
  state.modBrowser.busy = false;
  $("#mbProgress", dialog.host).hidden = true;
  $("#mbStatus", dialog.host).textContent = `Đã cài ${project.name}`;
  renderModResults(dialog);
  $("#mbStatus", dialog.host).textContent = `Đã cài ${project.name}`;

  const mods = state.mods[state.activeInstanceId] ||= [];
  if (!mods.some((m) => m.file.startsWith(project.slug))) {
    mods.push({
      file: `${project.slug}-${project.version}.jar`,
      version: project.version,
      enabled: true,
      update: null,
    });
    if (state.screen === "detail") renderMods();
  }
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
  const name = "MCPlayer";
  state.accounts.push({ id: "acc-" + Date.now(), name, kind: "Microsoft", active: false });
  renderAccounts();
  accountStatus(`Đã thêm tài khoản Microsoft "${name}" (demo).`);
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
  $("#openModsFolderBtn").addEventListener("click", () => setDetailStatus("Đã mở thư mục mods (demo)."));
  $("#mrpackBtn").addEventListener("click", () => setDetailStatus("Chọn file .mrpack để cài (demo)."));
  $("#optifineBtn").addEventListener("click", () => setDetailStatus("Đang mở OptiFine installer (demo)."));

  // Mod list toggle (delegated)
  $("#modList").addEventListener("change", (e) => {
    const input = e.target.closest("[data-mod-toggle]");
    if (!input) return;
    const mod = currentMods()[Number(input.dataset.modToggle)];
    if (mod) mod.enabled = input.checked;
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
  });
  $("#resetDemo").addEventListener("click", () => location.reload());

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
wireEvents();
renderInstances();
renderAccounts();
