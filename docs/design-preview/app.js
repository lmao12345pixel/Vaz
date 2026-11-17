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

function makeSvgScreenshot(bg1, bg2, accentColor, sceneSvg, label) {
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1280 720" width="1280" height="720">
    <defs>
      <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
        <stop offset="0%" stop-color="${bg1}"/>
        <stop offset="100%" stop-color="${bg2}"/>
      </linearGradient>
    </defs>
    <rect width="1280" height="720" fill="url(#bg)"/>
    ${sceneSvg}
    <rect y="648" width="1280" height="72" fill="rgba(11,15,22,0.85)"/>
    <line x1="0" y1="648" x2="1280" y2="648" stroke="${accentColor}" stroke-width="2" opacity="0.6"/>
    <text x="32" y="692" fill="#eaf0f7" font-family="'Segoe UI', system-ui, sans-serif" font-size="20" font-weight="600">${label}</text>
    <text x="1248" y="692" fill="${accentColor}" font-family="'Cascadia Mono', monospace" font-size="16" font-weight="bold" text-anchor="end">VAZER LAUNCHER</text>
  </svg>`;
  return `data:image/svg+xml;utf8,${encodeURIComponent(svg)}`;
}

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
      { projectId: "PpjUUSmh", file: "sodium-extra-0.6.0+mc1.21.1.jar", version: "0.6.0", enabled: true, update: null },
      { projectId: "YL57xq9U", file: "iris-1.8.1+mc1.21.1.jar", version: "1.8.1", enabled: true, update: null },
      { projectId: "LNytGWDc", file: "create-fabric-6.0.4+mc1.21.1.jar", version: "6.0.4", enabled: true, update: null },
      { projectId: "EsAfCjCV", file: "appleskin-fabric-3.0.6+mc1.21.1.jar", version: "3.0.6", enabled: true, update: null },
      { projectId: "botania", file: "botania-fabric-4.4.1+mc1.21.1.jar", version: "4.4.1", enabled: false, update: null },
    ],
    "vanilla-1.20.1": [],
    "neoforge-1.21.1": [
      { projectId: "u6dRKJwZ", file: "jei-1.21.1-neoforge-19.21.0.247.jar", version: "19.21.0", enabled: true, update: null },
      { projectId: "uXXizFIs", file: "ferritecore-7.0.1-neoforge.jar", version: "7.0.1", enabled: true, update: null },
      { projectId: "9s6osm5g", file: "cloth-config-15.0.140-neoforge.jar", version: "15.0.140", enabled: false, update: null },
    ],
  },

  resourcePacks: {
    "test fabric": [
      {
        id: "faithful-32x",
        name: "Faithful 32x",
        version: "v1.21-r1",
        resolution: "32x",
        size: "18.5 MB",
        icon: "assets/resourcepacks/faithful-icon.png",
        enabled: true,
        mc: "1.21.1",
        mcVersions: ["1.21.1", "1.20.1"],
      },
      {
        id: "bare-bones",
        name: "Bare Bones",
        version: "v1.21.1",
        resolution: "16x",
        size: "4.2 MB",
        icon: "assets/resourcepacks/bare-bones-icon.png",
        enabled: true,
        mc: "1.21.1",
        mcVersions: ["1.21.1", "1.20.1", "1.19.4"],
      },
      {
        id: "stay-true",
        name: "Stay True",
        version: "1.21.1-v1",
        resolution: "16x",
        size: "12.8 MB",
        icon: "assets/resourcepacks/stay-true-icon.png",
        enabled: false,
        mc: "1.21.1",
        mcVersions: ["1.21.1", "1.20.1"],
      },
      {
        id: "fresh-animations",
        name: "Fresh Animations",
        version: "v1.9.2",
        resolution: "16x",
        size: "2.1 MB",
        icon: "assets/resourcepacks/fresh-animations-icon.png",
        enabled: true,
        mc: "1.21.1",
        mcVersions: ["1.21.1"],
      },
    ],
    "vanilla-1.20.1": [],
    "neoforge-1.21.1": [
      {
        id: "faithful-32x",
        name: "Faithful 32x",
        version: "v1.21-r1",
        resolution: "32x",
        size: "18.5 MB",
        icon: "assets/resourcepacks/faithful-icon.png",
        enabled: true,
        mc: "1.21.1",
        mcVersions: ["1.21.1", "1.20.1"],
      },
    ],
  },

  instanceShaders: {
    "test fabric": {
      activeId: "bsl-shaders",
      profile: "medium",
      installed: [
        {
          id: "bsl-shaders",
          name: "BSL Shaders",
          version: "10.1.8",
          author: "CaptTatsu",
          size: "1.1 MB",
          image: "assets/shaders/bsl-shaders.jpg",
          desc: "Ánh sáng rực rỡ, màu sắc nổi bật và bầu trời ấn tượng — lựa chọn cân bằng quen thuộc.",
        },
        {
          id: "complementary-reimagined",
          name: "Complementary Shaders - Reimagined",
          version: "r5.9.3",
          author: "EminGT",
          size: "0.5 MB",
          image: "assets/shaders/complementary-reimagined.jpg",
          desc: "Giữ trọn phong cách Minecraft, nâng chất lượng ánh sáng, nước và thời tiết với hiệu năng ổn định.",
        },
        {
          id: "photon-shader",
          name: "Photon Shaders",
          version: "v1.4",
          author: "sixthsurge",
          size: "3.7 MB",
          image: "assets/shaders/photon-shader.jpg",
          desc: "Hướng gameplay, bán thực tế: nắng ấm, sương mù và phản chiếu nhẹ nhàng, giữ FPS ổn định.",
        },
        {
          id: "solas-shader",
          name: "Solas Shader",
          version: "3.8",
          author: "Septonious",
          size: "1.2 MB",
          image: "assets/shaders/solas-shader.jpg",
          desc: "Fantasy hiện đại với ánh sáng màu, bóng đổ mềm và không khí điện ảnh.",
        },
      ],
    },
    "vanilla-1.20.1": {
      activeId: null,
      profile: "medium",
      installed: [],
    },
    "neoforge-1.21.1": {
      activeId: null,
      profile: "medium",
      installed: [],
    },
  },

  accounts: [
    { id: "acc-steve", name: "Steve", kind: "Offline", active: true, skinId: "steve", uuid: "b62a-89f1-4890-a29e", tokenStatus: "Offline session", capeId: "none", capes: [] },
    { id: "acc-alex", name: "Alex", kind: "Microsoft", active: false, skinId: "alex", uuid: "c069-42fb-8501-1b4a", tokenStatus: "Token: Hợp lệ (14 ngày)", capeId: "vanilla", capes: ["vanilla", "migrator", "15th-anniversary", "cherry"] },
  ],

  worlds: {
    "test fabric": [
      {
        name: "World",
        folder: "world",
        sizeMb: 148.2,
        lastPlayed: "2 giờ trước",
        backups: 2,
        seed: "-4820193850123849102",
        gameMode: "Survival",
        difficulty: "Hard",
        spawn: { x: -142, y: 72, z: 256 },
        gameTime: "1,248,300 ticks (~17.3h)",
        dayCount: 52,
        version: "1.21.1",
        generator: "Default",
        allowCommands: false,
      },
      {
        name: "Survival 1.21",
        folder: "survival_1_21",
        sizeMb: 96.4,
        lastPlayed: "3 ngày trước",
        backups: 0,
        seed: "86753098231904",
        gameMode: "Survival",
        difficulty: "Normal",
        spawn: { x: 34, y: 64, z: -118 },
        gameTime: "680,450 ticks (~9.4h)",
        dayCount: 28,
        version: "1.21.1",
        generator: "Default",
        allowCommands: false,
      },
      {
        name: "Creative Test",
        folder: "creative_test",
        sizeMb: 12.7,
        lastPlayed: "1 tuần trước",
        backups: 1,
        seed: "123456789",
        gameMode: "Creative",
        difficulty: "Peaceful",
        spawn: { x: 0, y: 4, z: 0 },
        gameTime: "45,000 ticks (~0.6h)",
        dayCount: 2,
        version: "1.21.1",
        generator: "Flat",
        allowCommands: true,
      },
      {
        name: "Hardcore 100 Days",
        folder: "hardcore_100_days",
        sizeMb: 230.5,
        lastPlayed: "5 giờ trước",
        backups: 3,
        seed: "-771928401928",
        gameMode: "Hardcore",
        difficulty: "Hard",
        spawn: { x: 120, y: 68, z: -400 },
        gameTime: "2,410,000 ticks (~33.5h)",
        dayCount: 101,
        version: "1.21.1",
        generator: "Large Biomes",
        allowCommands: false,
      },
      {
        name: "Adventure Dungeon",
        folder: "adventure_dungeon",
        sizeMb: 45.0,
        lastPlayed: "2 tuần trước",
        backups: 0,
        seed: "998877665544",
        gameMode: "Adventure",
        difficulty: "Normal",
        spawn: { x: -10, y: 55, z: 80 },
        gameTime: "120,000 ticks (~1.7h)",
        dayCount: 5,
        version: "1.21.1",
        generator: "Custom",
        allowCommands: true,
      },
    ],
    "vanilla-1.20.1": [
      {
        name: "World",
        sizeMb: 42.8,
        lastPlayed: "3 ngày trước",
        backups: 0,
        seed: "5581920391283",
        gameMode: "Survival",
        difficulty: "Normal",
        spawn: { x: 105, y: 70, z: -44 },
        gameTime: "310,200 ticks (~4.3h)",
        dayCount: 13,
        version: "1.20.1",
        generator: "Default",
        allowCommands: false,
      },
    ],
    "neoforge-1.21.1": [
      {
        name: "ATM10",
        sizeMb: 512.6,
        lastPlayed: "1 tuần trước",
        backups: 4,
        seed: "-99281748291048",
        gameMode: "Survival",
        difficulty: "Hard",
        spawn: { x: -512, y: 68, z: 120 },
        gameTime: "4,510,000 ticks (~62.6h)",
        dayCount: 188,
        version: "1.21.1",
        generator: "Default",
        allowCommands: true,
      },
      {
        name: "Skyblock",
        sizeMb: 88.1,
        lastPlayed: "2 tuần trước",
        backups: 1,
        seed: "777",
        gameMode: "Survival",
        difficulty: "Normal",
        spawn: { x: 0, y: 100, z: 0 },
        gameTime: "1,120,000 ticks (~15.5h)",
        dayCount: 46,
        version: "1.21.1",
        generator: "Void",
        allowCommands: true,
      },
    ],
  },

  screenshots: {
    "test fabric": [
      {
        id: "ss-tf-1",
        name: "2026-10-20_17.42.10.png",
        title: "Base lúc hoàng hôn",
        date: "20/10/2026, 17:42",
        world: "World",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#2e1065",
          "#f97316",
          "#22c55e",
          `<circle cx="640" cy="380" r="80" fill="#fef08a" opacity="0.95"/>
           <polygon points="0,520 320,280 640,480 960,240 1280,500 1280,648 0,648" fill="#1e1b4b" opacity="0.85"/>
           <polygon points="0,648 180,440 460,648 720,410 1060,648 1280,460 1280,648" fill="#0f172a"/>
           <rect x="580" y="520" width="90" height="60" fill="#451a03"/>
           <polygon points="560,520 625,470 690,520" fill="#78350f"/>
           <rect x="615" y="540" width="20" height="20" fill="#fef08a"/>
           <polygon points="120,540 140,490 160,540" fill="#064e3b"/>
           <polygon points="110,570 140,520 170,570" fill="#064e3b"/>`,
          "Base lúc hoàng hôn · Test Fabric"
        ),
      },
      {
        id: "ss-tf-2",
        name: "2026-10-21_18.45.12.png",
        title: "Cổng Nether & Pháo đài",
        date: "21/10/2026, 18:45",
        world: "World",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#450a0a",
          "#1c0505",
          "#a855f7",
          `<path d="M0,560 Q320,530 640,570 T1280,550 L1280,648 L0,648 Z" fill="#ea580c"/>
           <rect x="520" y="240" width="240" height="340" fill="#0f0c1b" stroke="#3b0764" stroke-width="12"/>
           <rect x="550" y="270" width="180" height="280" fill="#7c3aed" opacity="0.85"/>
           <circle cx="480" cy="360" r="6" fill="#f97316"/>
           <circle cx="820" cy="400" r="5" fill="#f97316"/>
           <circle cx="640" cy="220" r="4" fill="#c084fc"/>`,
          "Cổng Nether & Pháo đài"
        ),
      },
      {
        id: "ss-tf-3",
        name: "2026-10-22_09.10.33.png",
        title: "Rừng anh đào & Núi tuyết",
        date: "22/10/2026, 09:10",
        world: "Survival 1.21",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#0284c7",
          "#fbcfe8",
          "#f472b6",
          `<polygon points="260,560 640,160 1020,560" fill="#f8fafc"/>
           <polygon points="560,260 640,160 720,260" fill="#e2e8f0"/>
           <ellipse cx="380" cy="460" rx="90" ry="60" fill="#f472b6"/>
           <ellipse cx="880" cy="480" rx="110" ry="70" fill="#fb7185"/>
           <rect x="370" y="510" width="20" height="80" fill="#713f12"/>
           <rect x="870" y="530" width="24" height="80" fill="#713f12"/>`,
          "Rừng anh đào & Núi tuyết · Survival 1.21"
        ),
      },
      {
        id: "ss-tf-4",
        name: "2026-10-22_11.04.50.png",
        title: "Kho khoáng sản lòng đất",
        date: "22/10/2026, 11:04",
        world: "Creative Test",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#051515",
          "#042f2e",
          "#2dd4bf",
          `<line x1="200" y1="0" x2="200" y2="280" stroke="#15803d" stroke-width="6"/>
           <circle cx="200" cy="200" r="8" fill="#facc15"/>
           <line x1="840" y1="0" x2="840" y2="240" stroke="#15803d" stroke-width="6"/>
           <circle cx="840" cy="180" r="8" fill="#facc15"/>
           <rect x="580" y="440" width="120" height="120" fill="#334155" stroke="#475569" stroke-width="4"/>
           <circle cx="620" cy="470" r="12" fill="#38bdf8"/>
           <circle cx="660" cy="510" r="10" fill="#38bdf8"/>
           <circle cx="610" cy="520" r="8" fill="#38bdf8"/>`,
          "Kho khoáng sản lòng đất · Deepslate & Diamond"
        ),
      },
    ],
    "vanilla-1.20.1": [
      {
        id: "ss-v1",
        name: "2026-10-19_20.15.00.png",
        title: "Làng ven biển Vanilla",
        date: "19/10/2026, 20:15",
        world: "World",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#0284c7",
          "#fed7aa",
          "#22c55e",
          `<rect x="0" y="520" width="1280" height="128" fill="#38bdf8" opacity="0.6"/>
           <polygon points="100,520 300,520 280,440 120,440" fill="#d97706"/>
           <rect x="180" y="400" width="40" height="40" fill="#b45309"/>`,
          "Làng ven biển Vanilla · 1.20.1"
        ),
      },
    ],
    "neoforge-1.21.1": [
      {
        id: "ss-n1",
        name: "2026-10-15_16.30.22.png",
        title: "Nhà máy tự động hóa Modded",
        date: "15/10/2026, 16:30",
        world: "ATM10",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#111827",
          "#0f172a",
          "#fb923c",
          `<circle cx="640" cy="380" r="70" fill="none" stroke="#d97706" stroke-width="24" stroke-dasharray="24 16"/>
           <circle cx="640" cy="380" r="28" fill="#78350f"/>
           <rect x="200" y="460" width="880" height="30" fill="#b45309" stroke="#78350f" stroke-width="4"/>`,
          "Nhà máy tự động hóa · AllTheMods 10"
        ),
      },
      {
        id: "ss-n2",
        name: "2026-10-18_21.05.44.png",
        title: "Đảo bay Skyblock",
        date: "18/10/2026, 21:05",
        world: "Skyblock",
        width: 1920,
        height: 1080,
        url: makeSvgScreenshot(
          "#0284c7",
          "#bae6fd",
          "#38bdf8",
          `<polygon points="460,420 820,420 740,560 540,560" fill="#713f12"/>
           <polygon points="440,410 840,410 820,440 460,440" fill="#15803d"/>
           <rect x="630" y="320" width="20" height="90" fill="#451a03"/>
           <ellipse cx="640" cy="300" rx="60" ry="45" fill="#16a34a"/>
           <rect x="570" y="440" width="16" height="180" fill="#38bdf8" opacity="0.85"/>`,
          "Đảo bay Skyblock · Vực thẳm"
        ),
      },
    ],
  },

  // Rút gọn từ kết quả Modrinth thật; icon lấy từ CDN.
  catalog: [
    { id: "AANobbMI", slug: "sodium", name: "Sodium", author: "jellysquid3", desc: "A high-performance rendering engine replacement for Minecraft, which greatly improves frame rates and reduces micro-stutter.", downloads: 248_000_000, version: "0.6.13", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.8 MB", icon: "assets/mods/sodium.webp", accent: "#38bdf8", gallery: [{ url: "assets/gallery/sodium-1.jpg", title: "Sodium - Cài đặt đồ họa nâng cao", name: "sodium-1.jpg", date: "Hôm nay", world: "Benchmark", width: 1920, height: 1080 }, { url: "assets/gallery/sodium-2.jpg", title: "Sodium - Hiển thị FPS & Chunk rendering", name: "sodium-2.jpg", date: "Hôm nay", world: "Benchmark", width: 1920, height: 1080 }, { url: "assets/gallery/sodium-3.jpg", title: "Sodium - Tối ưu hóa mượt mà", name: "sodium-3.jpg", date: "Hôm nay", world: "Benchmark", width: 1920, height: 1080 }] },
    { id: "gvQqBUqZ", slug: "lithium", name: "Lithium", author: "jellysquid3", desc: "No-compromises game logic/server optimization mod.", downloads: 189_000_000, version: "0.15.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.6 MB", icon: "assets/mods/lithium.webp", accent: "#f59e0b" },
    { id: "PpjUUSmh", slug: "sodium-extra", name: "Sodium Extra", author: "FlashyReese", desc: "A Sodium addon that adds features that shouldn't be in Sodium.", downloads: 104_300_000, version: "0.6.0", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "graphics", categoryLabel: "Đồ họa", size: "0.5 MB", icon: "assets/mods/sodium-extra.webp", accent: "#38bdf8" },
    { id: "Bh37bMuy", slug: "reeses-sodium-options", name: "Reese's Sodium Options", author: "FlashyReese", desc: "Alternative Options Menu for Sodium.", downloads: 86_900_000, version: "1.8.3", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "graphics", categoryLabel: "Đồ họa", size: "0.3 MB", icon: "assets/mods/reeses-sodium-options.png", accent: "#38bdf8" },
    { id: "YL57xq9U", slug: "iris", name: "Iris Shaders", author: "coderbot", desc: "A modern shader pack loader for Minecraft, designed to be compatible with Sodium.", downloads: 74_200_000, version: "1.8.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "graphics", categoryLabel: "Đồ họa", size: "1.5 MB", icon: "assets/mods/iris.webp", accent: "#a78bfa", gallery: [{ url: "assets/gallery/iris-1.jpg", title: "Iris Shaders - Đổ bóng & Nước thời gian thực", name: "iris-1.jpg", date: "Hôm nay", world: "Survival", width: 1920, height: 1080 }, { url: "assets/gallery/iris-2.jpg", title: "Iris Shaders - Cảnh hoàng hôn chân thực", name: "iris-2.jpg", date: "Hôm nay", world: "Survival", width: 1920, height: 1080 }, { url: "assets/gallery/iris-3.jpg", title: "Iris Shaders - Cài đặt shader pack trực quan", name: "iris-3.jpg", date: "Hôm nay", world: "Survival", width: 1920, height: 1080 }] },
    { id: "uXXizFIs", slug: "ferrite-core", name: "FerriteCore", author: "malte0811", desc: "Memory usage optimizations for Minecraft.", downloads: 71_500_000, version: "7.0.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.4 MB", icon: "assets/mods/ferrite-core.webp", accent: "#ef4444" },
    { id: "u6dRKJwZ", slug: "jei", name: "Just Enough Items (JEI)", author: "mezz", desc: "View items and recipes.", downloads: 65_800_000, version: "19.21.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "survival", categoryLabel: "Sinh tồn/QoL", size: "1.2 MB", icon: "assets/mods/jei.webp", accent: "#3b82f6" },
    { id: "P7dR8mSH", slug: "fabric-api", name: "Fabric API", author: "modmuss50", desc: "Lightweight and modular API providing common hooks and intercompatibility measures utilized by mods using the Fabric toolchain.", downloads: 61_400_000, version: "0.115.0", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "technology", categoryLabel: "Công nghệ", size: "2.1 MB", icon: "assets/mods/fabric-api.png", accent: "#e0e7ff" },
    { id: "mOgUt4GM", slug: "modmenu", name: "Mod Menu", author: "Prospector", desc: "Adds a mod menu to view the list of mods you have installed.", downloads: 54_900_000, version: "11.0.3", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "survival", categoryLabel: "Sinh tồn/QoL", size: "0.5 MB", icon: "assets/mods/modmenu.png", accent: "#22c55e" },
    { id: "5ZwdcRci", slug: "immediatelyfast", name: "ImmediatelyFast", author: "RaphiMC", desc: "Speed up immediate mode rendering in Minecraft.", downloads: 43_700_000, version: "1.3.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.3 MB", icon: "assets/mods/immediatelyfast.webp", accent: "#eab308" },
    { id: "NNAgCjsB", slug: "entityculling", name: "Entity Culling", author: "tr7zw", desc: "Using async path-tracing to skip rendering blocks and entities which are not visible.", downloads: 39_800_000, version: "1.7.3", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.2 MB", icon: "assets/mods/entityculling.webp", accent: "#10b981" },
    { id: "Orvt0mRa", slug: "indium", name: "Indium", author: "comp500", desc: "Sodium addon providing support for the Fabric Rendering API.", downloads: 33_100_000, version: "1.0.35", loaders: ["fabric"], versions: ["1.21.1"], type: "mods", category: "technology", categoryLabel: "Công nghệ", size: "0.4 MB", icon: "assets/mods/indium.webp", accent: "#6366f1" },
    { id: "9s6osm5g", slug: "cloth-config", name: "Cloth Config API", author: "shedaniel", desc: "Configuration library for Minecraft mods.", downloads: 31_600_000, version: "15.0.140", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "technology", categoryLabel: "Công nghệ", size: "1.1 MB", icon: "assets/mods/cloth-config.webp", accent: "#ec4899" },
    { id: "1IjD5062", slug: "continuity", name: "Continuity", author: "PepperCode1", desc: "A Fabric mod that allows for efficient connected textures.", downloads: 27_400_000, version: "3.0.0", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "graphics", categoryLabel: "Đồ họa", size: "0.7 MB", icon: "assets/mods/continuity.png", accent: "#14b8a6" },
    { id: "lhGA9TYQ", slug: "architectury-api", name: "Architectury API", author: "shedaniel", desc: "An intermediary api aimed to ease developing multiplatform mods.", downloads: 24_900_000, version: "13.0.8", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "technology", categoryLabel: "Công nghệ", size: "0.6 MB", icon: "assets/mods/architectury-api.webp", accent: "#8b5cf6" },
    { id: "H8CaAYZC", slug: "starlight", name: "Starlight", author: "PaperMC", desc: "Rewrites the light engine to fix lighting performance and lighting bugs.", downloads: 21_200_000, version: "1.1.5", loaders: ["fabric"], versions: ["1.21.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.5 MB", icon: "assets/mods/starlight.webp", accent: "#fbbf24" },
    { id: "LNytGWDc", slug: "create", name: "Create", author: "simibubi", desc: "Building tools and aesthetic technology for Minecraft.", downloads: 19_600_000, version: "6.0.4", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "technology", categoryLabel: "Công nghệ", size: "16.4 MB", icon: "assets/mods/create.webp", accent: "#d97706" },
    { id: "1bokaNcj", slug: "xaeros-minimap", name: "Xaero's Minimap", author: "xaero96", desc: "Displays the nearby terrain, and entities such as mobs, players and dropped items in a minimap.", downloads: 18_300_000, version: "24.2.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "survival", categoryLabel: "Sinh tồn/QoL", size: "1.8 MB", icon: "assets/mods/xaeros-minimap.webp", accent: "#84cc16" },
    { id: "EsAfCjCV", slug: "appleskin", name: "AppleSkin", author: "squeek502", desc: "Food-related HUD improvements: hunger/saturation values and tooltips.", downloads: 17_100_000, version: "3.0.6", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "survival", categoryLabel: "Sinh tồn/QoL", size: "0.2 MB", icon: "assets/mods/appleskin.png", accent: "#ef4444" },
    { id: "nvQzSEkH", slug: "jade", name: "Jade", author: "Snownee", desc: "Shows information about what you are looking at.", downloads: 16_400_000, version: "15.3.0", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "survival", categoryLabel: "Sinh tồn/QoL", size: "0.9 MB", icon: "assets/mods/jade.webp", accent: "#06b6d4" },
    { id: "fRiHVvU7", slug: "emi", name: "EMI", author: "Emi", desc: "A featureful and accessible item and recipe viewer.", downloads: 15_200_000, version: "1.1.22", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "survival", categoryLabel: "Sinh tồn/QoL", size: "1.4 MB", icon: "assets/mods/emi.webp", accent: "#f43f5e" },
    { id: "nmDcB62a", slug: "modernfix", name: "ModernFix", author: "embeddedt", desc: "Dynamic performance improvements and bug fixes for modern Minecraft.", downloads: 14_600_000, version: "5.23.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "1.0 MB", icon: "assets/mods/modernfix.webp", accent: "#0284c7" },
    { id: "g96Z4WVZ", slug: "badoptimizations", name: "BadOptimizations", author: "ThetaDev", desc: "A collection of micro-optimizations for the client.", downloads: 12_900_000, version: "2.2.1", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "performance", categoryLabel: "Hiệu năng", size: "0.1 MB", icon: "assets/mods/badoptimizations.webp", accent: "#64748b" },
    { id: "1eAoo2KR", slug: "yacl", name: "YetAnotherConfigLib", author: "isXander", desc: "A builder-based configuration library for Fabric mods.", downloads: 11_800_000, version: "3.7.1", loaders: ["fabric"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "technology", categoryLabel: "Công nghệ", size: "1.3 MB", icon: "assets/mods/yacl.webp", accent: "#8b5cf6" },
    { id: "ironspells", slug: "irons-spells", name: "Iron's Spells 'n Spellbooks", author: "IronSpells", desc: "A complete magic progression mod with RPG-style spells, wizard armor and bosses.", downloads: 9_200_000, version: "3.4.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "magic", categoryLabel: "Phép thuật", size: "12.8 MB", icon: "assets/mods/irons-spells.png", accent: "#9333ea" },
    { id: "botania", slug: "botania", name: "Botania", author: "Vazkii", desc: "An innovative natural magic mod themed around magical flora and botanical mana.", downloads: 14_500_000, version: "4.4.1", loaders: ["fabric", "neoforge"], versions: ["1.21.1", "1.20.1"], type: "mods", category: "magic", categoryLabel: "Phép thuật", size: "9.5 MB", icon: "assets/mods/botania.webp", accent: "#10b981" },
  ],
};

const TOTAL_RESULTS = 19_628;
const MB_PAGE_SIZE = 20;
const STORAGE_KEY = "vazer-demo-v1";

const TOTAL_SYS_RAM = 16384; // 16 GB
const OS_RESERVED_RAM = 4096; // 4 GB

const SYSTEM_JAVA_RUNTIMES = [
  {
    id: "java-21",
    name: "Java 21",
    vendor: "Eclipse Temurin",
    fullName: "Eclipse Temurin 21.0.4",
    version: "21.0.4",
    majorVersion: 21,
    arch: "x64",
    isJdk: true,
    path: "C:\\Program Files\\Eclipse Adoptium\\jdk-21.0.4.7-hotspot\\bin\\javaw.exe",
  },
  {
    id: "java-17",
    name: "Java 17",
    vendor: "Microsoft Build of OpenJDK",
    fullName: "Microsoft Build of OpenJDK 17.0.12",
    version: "17.0.12",
    majorVersion: 17,
    arch: "x64",
    isJdk: true,
    path: "C:\\Program Files\\Microsoft\\jdk-17.0.12.7-hotspot\\bin\\javaw.exe",
  },
  {
    id: "java-8",
    name: "Java 8",
    vendor: "Oracle",
    fullName: "Oracle JRE 8u421",
    version: "1.8.0_421",
    majorVersion: 8,
    arch: "x64",
    isJdk: false,
    path: "C:\\Program Files\\Java\\jre1.8.0_421\\bin\\javaw.exe",
  },
];

const GC_PRESETS = {
  g1gc: "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions",
  zgc: "-XX:+UseZGC -XX:+ZGenerational",
  shenandoah: "-XX:+UseShenandoahGC",
  custom: "",
};

function getRecommendedJavaVersion(mcStr) {
  if (!mcStr) return 21;
  const parts = String(mcStr).split(".").map((n) => parseInt(n, 10) || 0);
  const major = parts[0] || 1;
  const minor = parts[1] || 0;
  const patch = parts[2] || 0;

  if (major === 1) {
    if (minor > 20) return 21;
    if (minor === 20 && patch >= 5) return 21;
    if (minor >= 17) return 17;
    return 8;
  }
  return 21;
}

const DEFAULT_SETTINGS = {
  minRamMb: 1024,
  ramMb: 4096,
  jvmArgs: "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions",
  javaPath: "",
  javaVersion: 21,
  gcPreset: "g1gc",
  width: 854,
  height: 480,
};

const DEFAULT_MC_OPTIONS = {
  version: "3955",
  autoJump: "false",
  entityShadows: "true",
  enableVsync: "false",
  fov: "70.0",
  gamma: "0.5",
  graphicsMode: "fancy",
  guiScale: "0",
  maxFps: "120",
  mouseSensitivity: "0.5",
  invertYMouse: "false",
  renderClouds: "fancy",
  renderDistance: "12",
  simulationDistance: "12",
  sneakToggle: "false",
  sprintToggle: "false",
  soundCategory_master: "1.0",
  soundCategory_music: "0.5",
  soundCategory_weather: "1.0",
  soundCategory_block: "1.0",
  soundCategory_hostile: "1.0",
  soundCategory_player: "1.0",
  soundCategory_ambient: "1.0",
  "key_key.forward": "key.keyboard.w",
  "key_key.left": "key.keyboard.a",
  "key_key.back": "key.keyboard.s",
  "key_key.right": "key.keyboard.d",
  "key_key.jump": "key.keyboard.space",
  "key_key.sneak": "key.keyboard.left.shift",
  "key_key.sprint": "key.keyboard.left.control",
  "key_key.attack": "key.mouse.left",
  "key_key.use": "key.mouse.right",
  "key_key.inventory": "key.keyboard.e",
  "key_key.drop": "key.keyboard.q",
  "key_key.chat": "key.keyboard.t",
};

const MC_KEYBINDS = [
  { id: "key_key.forward", label: "Tiến (Forward)", defaultKey: "key.keyboard.w" },
  { id: "key_key.left", label: "Sang trái (Left)", defaultKey: "key.keyboard.a" },
  { id: "key_key.back", label: "Lùi (Back)", defaultKey: "key.keyboard.s" },
  { id: "key_key.right", label: "Sang phải (Right)", defaultKey: "key.keyboard.d" },
  { id: "key_key.jump", label: "Nhảy (Jump)", defaultKey: "key.keyboard.space" },
  { id: "key_key.sneak", label: "Cúi (Sneak)", defaultKey: "key.keyboard.left.shift" },
  { id: "key_key.sprint", label: "Chạy nhanh (Sprint)", defaultKey: "key.keyboard.left.control" },
  { id: "key_key.attack", label: "Tấn công / Đập (Attack)", defaultKey: "key.mouse.left" },
  { id: "key_key.use", label: "Sử dụng / Đặt (Use)", defaultKey: "key.mouse.right" },
  { id: "key_key.inventory", label: "Túi đồ (Inventory)", defaultKey: "key.keyboard.e" },
  { id: "key_key.drop", label: "Vứt vật phẩm (Drop)", defaultKey: "key.keyboard.q" },
  { id: "key_key.chat", label: "Trò chuyện (Chat)", defaultKey: "key.keyboard.t" },
];

const MC_PRESETS = {
  fps: {
    name: "Tối ưu FPS",
    values: {
      renderDistance: "6",
      simulationDistance: "5",
      maxFps: "260",
      graphicsMode: "fast",
      enableVsync: "false",
      renderClouds: "false",
      entityShadows: "false",
      guiScale: "0",
      gamma: "1.0",
    },
  },
  balanced: {
    name: "Cân bằng",
    values: {
      renderDistance: "12",
      simulationDistance: "10",
      maxFps: "120",
      graphicsMode: "fancy",
      enableVsync: "false",
      renderClouds: "fast",
      entityShadows: "true",
      guiScale: "0",
      gamma: "0.5",
    },
  },
  high: {
    name: "Đồ họa cao",
    values: {
      renderDistance: "20",
      simulationDistance: "16",
      maxFps: "144",
      graphicsMode: "fabulous",
      enableVsync: "true",
      renderClouds: "fancy",
      entityShadows: "true",
      guiScale: "0",
      gamma: "0.5",
    },
  },
};

const DEFAULT_GLOBAL_SETTINGS = {
  // Chung / Launcher
  language: "vi",
  launchAction: "hide",
  closeToTray: true,
  autoCheckUpdates: true,

  // Java & Minecraft
  javaPath: "",
  minRamMb: 2048,
  ramMb: 4096,
  jvmPreset: "standard",
  jvmArgs: "-XX:+UseG1GC -XX:+ParallelRefProcEnabled",
  windowWidth: 1280,
  windowHeight: 720,
  startFullscreen: false,

  // Giao diện / Appearance
  theme: document.documentElement.dataset.theme === "light" ? "light" : "dark",
  accentColor: "emerald",
  reduceMotion: false,
  uiSoundEnabled: true,

  // Mạng & Tải xuống / Network
  downloadThreads: 8,
  downloadMirror: "cloudflare",
  bandwidthLimit: "unlimited",

  // Discord & Riêng tư
  discordRpc: true,
  discordStatus: "Đang sinh tồn ở thế giới mới",
  discordShowInstance: true,
  anonymousTelemetry: true,

  // Lưu trữ & Dữ liệu
  dataPath: "C:/Users/TOSHIBA/AppData/Roaming/Vazer",
  backupPath: "C:/Users/TOSHIBA/AppData/Roaming/Vazer/backups",
  cacheSizeMb: 248,
};

const DEFAULT_SERVERS = [
  {
    id: "server-aurora",
    name: "Aurora SMP",
    address: "play.aurora-smp.net",
    version: "1.21.1",
    loader: "vanilla",
    category: "survival",
    icon: "assets/blocks/grass_block.png",
    favorite: true,
    online: true,
    players: 128,
    max: 500,
    ping: 42,
    lastPlayed: "Hôm qua",
    motd: "§a§lAurora SMP §7- §eSurvival 1.21.1 §6[Season 4]\n§b§oĐã mở cửa đón chào cư dân mới!",
    playerSample: [
      { name: "Vazer_Admin", skin: "assets/skins/steve.png" },
      { name: "Alex_Builder", skin: "assets/skins/alex.png" },
      { name: "Kai_Craft", skin: "assets/skins/kai.png" },
      { name: "Ari_Redstone", skin: "assets/skins/ari.png" }
    ],
  },
  {
    id: "server-creative",
    name: "Creative Hub",
    address: "hub.creativehub.gg",
    version: "1.20.1",
    loader: "vanilla",
    category: "creative",
    icon: "assets/blocks/diamond_block.png",
    favorite: false,
    online: true,
    players: 64,
    max: 200,
    ping: 118,
    lastPlayed: "3 ngày trước",
    motd: "§d§lCreative Hub §8» §fXây dựng không giới hạn\n§eplot.creativehub.gg §7| §aFree WorldEdit §b§l★",
    playerSample: [
      { name: "Jeb_Artist", skin: "assets/skins/makena.png" },
      { name: "Efe_Designer", skin: "assets/skins/efe.png" },
      { name: "Nova_Blocks", skin: "assets/skins/ari.png" }
    ],
  },
  {
    id: "server-hypixel",
    name: "Hypixel Asia Proxy",
    address: "asia.hypixel.net",
    version: "1.20.1",
    loader: "vanilla",
    category: "minigames",
    icon: "assets/blocks/tnt.png",
    favorite: false,
    online: true,
    players: 34120,
    max: 100000,
    ping: 185,
    lastPlayed: "1 tuần trước",
    motd: "§e§lHYPIXEL NETWORK §7[1.8-1.21]\n§aBed Wars §7| §6SkyBlock §7| §dDuels §c§l[HOT]",
    playerSample: [
      { name: "Hypixel_Pro", skin: "assets/skins/steve.png" },
      { name: "BedWarsGod", skin: "assets/skins/alex.png" }
    ],
  },
  {
    id: "server-modded",
    name: "Modded Survival",
    address: "survival.modded.gg",
    version: "1.21.1",
    loader: "fabric",
    category: "modded",
    icon: "assets/blocks/furnace.png",
    favorite: false,
    online: false,
    players: 0,
    max: 120,
    ping: null,
    lastPlayed: "Chưa vào",
    motd: "§c[BẢO TRÌ] §7Modded Survival Fabric 1.21.1\n§4§oHẹn gặp lại lúc 20:00 tối nay!",
    playerSample: [],
  },
];

const DISCOVER_PACKS = [
  {
    id: "pack-performance",
    name: "Performance Fabric",
    author: "Vazer Team",
    desc: "Bộ mod tối ưu FPS, giảm lag và cải thiện thời gian tải chunk cho Minecraft hiện đại.",
    longDesc: "Performance Fabric là bộ sưu tập mod tối ưu hóa toàn diện cho Fabric 1.21.1, kết hợp các công nghệ render hiện đại nhất như Sodium, Lithium, FerriteCore và ImmediatelyFast giúp FPS tăng tới 300% và khung hình mượt mà tuyệt đối.",
    loader: "fabric",
    loaderVersion: "0.19.5",
    mc: "1.21.1",
    downloads: "1.2M",
    downloadsNum: 1250000,
    size: "145 MB",
    modCount: 35,
    categories: ["optimization", "performance"],
    tag: "Tối ưu / FPS",
    mark: "P",
    accent: "#5eead4",
    icon: "assets/modpacks/performance-icon.png",
    image: "assets/modpacks/performance-banner.jpg",
    gallery: [
      { url: "assets/gallery/performance-1.jpg", title: "Performance Fabric - Tối ưu FPS", name: "perf-1.png", date: "Hôm nay", world: "Benchmark", width: 1920, height: 1080 },
      { url: "assets/gallery/performance-2.jpg", title: "Performance Fabric - Độ trễ chunk cực thấp", name: "perf-2.png", date: "Hôm nay", world: "Benchmark", width: 1920, height: 1080 },
      { url: "assets/gallery/performance-3.jpg", title: "Performance Fabric - Hoạt động mượt mà", name: "perf-3.png", date: "Hôm nay", world: "Benchmark", width: 1920, height: 1080 },
    ],
    versions: [
      { version: "1.4.2", mc: "1.21.1", loader: "Fabric 0.19.5", date: "2026-10-15", size: "145 MB" },
      { version: "1.3.8", mc: "1.20.1", loader: "Fabric 0.15.11", date: "2026-08-20", size: "138 MB" },
      { version: "1.5.0-beta.1", mc: "1.21.1", loader: "Fabric 0.19.5", date: "2026-10-25", size: "150 MB" },
    ],
    changelog: [
      "Cập nhật Sodium lên build mới nhất tương thích MC 1.21.1",
      "Bổ sung ImmediatelyFast giảm độ trễ render entity và HUD",
      "Tối ưu mức tiêu thụ bộ nhớ RAM giảm hơn 40%",
    ],
    mods: [
      "Sodium", "Lithium", "ImmediatelyFast", "EntityCulling", "ModernFix",
      "FerriteCore", "Indium", "MemoryLeakFix", "Krypton", "Cull Less Leaves",
      "Cloth Config", "ModMenu", "YetAnotherConfigLib", "Fabric API", "Dynamic FPS",
      "Concurrent Chunk Management Engine", "Smooth Boot", "Enhanced Block Entities",
      "FastAnim", "Bobby", "Entity Texture Features", "Entity Model Features",
      "Iris Shaders", "LambDynamicLights", "AppleSkin", "Status Effect Bars",
      "Language Reload", "No Chat Reports", "BadOptimizations", "ThreadTweak",
      "Faster Random", "Scalable Cat's Force", "Vazer Core Tweaks", "Packet Fixer", "Zoomify",
    ],
  },
  {
    id: "pack-create",
    name: "Create Adventure",
    author: "Vazer Team",
    desc: "Create, JEI và các tiện ích sinh tồn — sẵn sàng cho một thế giới kỹ thuật mới.",
    longDesc: "Hành trình khám phá thế giới kỹ xảo bánh răng và tự động hóa với Create Mod kết hợp cùng hệ thống nhiệm vụ phiêu lưu hấp dẫn, các công cụ sinh tồn QoL và địa hình mới kỳ vĩ.",
    loader: "fabric",
    loaderVersion: "0.19.5",
    mc: "1.21.1",
    downloads: "864K",
    downloadsNum: 864000,
    size: "380 MB",
    modCount: 42,
    categories: ["tech", "adventure"],
    tag: "Kỹ thuật / Tech",
    mark: "C",
    accent: "#f59e0b",
    icon: "assets/modpacks/create-icon.png",
    image: "assets/modpacks/create-banner.jpg",
    versions: [
      { version: "2.1.0", mc: "1.21.1", loader: "Fabric 0.19.5", date: "2026-10-10", size: "380 MB" },
      { version: "2.0.4", mc: "1.20.1", loader: "Fabric 0.15.11", date: "2026-07-12", size: "360 MB" },
    ],
    changelog: [
      "Thêm Create: Steam 'n Rails và Create Crafts & Additions",
      "Cân bằng công thức chế tạo bánh răng và động cơ hơi nước",
      "Tích hợp JEI và Jade tương thích hiển thị khối máy móc",
    ],
    mods: [
      "Create", "Create Steam & Rails", "Create Crafts & Additions", "Create Deco",
      "JEI", "Jade", "AppleSkin", "Architectury API", "Farmer's Delight",
      "Waystones", "Comforts", "Nature's Compass", "Explorer's Compass",
      "Xaero's World Map", "Xaero's Minimap", "Clumps", "Mouse Tweaks",
      "Inventory Profiles Next", "Sophisticated Backpacks", "Curios API",
      "Patchouli", "Sodium", "Lithium", "FerriteCore", "Indium", "Fabric API",
      "Cloth Config", "ModMenu", "Spark", "AttributeFix", "BadOptimizations",
      "Right Click Harvest", "Just Enough Resources", "Polymorph", "Simple Voice Chat",
      "Controlling", "Searchables", "Better F3", "Collective", "Visual Workbench",
      "FallingTree", "Fast Leaf Decay",
    ],
  },
  {
    id: "pack-neoforge",
    name: "NeoForge Essentials",
    author: "Vazer Team",
    desc: "Nền tảng NeoForge gọn nhẹ với JEI, Jade và các mod tối ưu thiết yếu.",
    longDesc: "NeoForge Essentials được thiết kế như một khung nền tảng modded vững chắc, tích hợp Mekanism, Applied Energistics 2 cùng hệ thống tối ưu hóa phần cứng hiện đại.",
    loader: "neoforge",
    loaderVersion: "21.1.72",
    mc: "1.21.1",
    downloads: "512K",
    downloadsNum: 512000,
    size: "420 MB",
    modCount: 38,
    categories: ["tech"],
    tag: "Kỹ thuật / Tech",
    mark: "N",
    accent: "#818cf8",
    icon: "assets/modpacks/neoforge-icon.png",
    image: "assets/modpacks/neoforge-banner.jpg",
    versions: [
      { version: "1.2.0", mc: "1.21.1", loader: "NeoForge 21.1.72", date: "2026-10-18", size: "420 MB" },
      { version: "1.1.5", mc: "1.20.1", loader: "NeoForge 20.4.237", date: "2026-06-30", size: "395 MB" },
    ],
    changelog: [
      "Nâng cấp NeoForge lên phiên bản 21.1.72 mới nhất",
      "Tích hợp Mekanism v10 với hệ thống năng lượng và lọc khoáng sản",
      "Cải thiện hiệu năng xử lý đường truyền dữ liệu AE2",
    ],
    mods: [
      "Mekanism", "Mekanism Generators", "Applied Energistics 2", "JEI", "Jade",
      "EMI", "FerriteCore", "ModernFix", "Cloth Config", "Architectury API",
      "Sophisticated Storage", "Refined Storage", "Pipez", "Iron Chests",
      "Curios API", "Waystones", "Farmer's Delight", "NetherPortalFix",
      "Mouse Tweaks", "Clumps", "Spark", "Connectivity", "Embeddium",
      "Oculus", "Better Ping Display", "AppleSkin", "Just Enough Resources",
      "Controlling", "Searchables", "AttributeFix", "FastSuite", "Placebo",
      "Cupboard", "Bookshelf", "PackMenu", "Crash Utilities", "KubeJS", "Rhino",
    ],
  },
  {
    id: "pack-cozy",
    name: "Cozy Fabric",
    author: "Vazer Team",
    desc: "QoL nhẹ nhàng: bản đồ nhỏ, recipe viewer, config rõ ràng và thông tin sinh tồn.",
    longDesc: "Một trải nghiệm Minecraft yên bình, ấm cúng với những cải tiến chất lượng cuộc sống (QoL) tinh tế, âm thanh môi trường sống động và giao diện tối giản thân thiện.",
    loader: "fabric",
    loaderVersion: "0.19.5",
    mc: "1.20.1",
    downloads: "438K",
    downloadsNum: 438000,
    size: "190 MB",
    modCount: 30,
    categories: ["adventure", "optimization"],
    tag: "Phiêu lưu / RPG",
    mark: "Q",
    accent: "#f472b6",
    icon: "assets/modpacks/cozy-icon.png",
    image: "assets/modpacks/cozy-banner.jpg",
    versions: [
      { version: "1.3.1", mc: "1.20.1", loader: "Fabric 0.15.11", date: "2026-09-14", size: "190 MB" },
      { version: "1.2.0", mc: "1.20.1", loader: "Fabric 0.15.11", date: "2026-05-10", size: "185 MB" },
    ],
    changelog: [
      "Thêm AmbientSounds và Sound Physics Remastered",
      "Cập nhật Complementary Shaders preset phong cách mùa thu",
      "Tối ưu hóa bảng công thức nấu ăn của Farmer's Delight",
    ],
    mods: [
      "ModMenu", "EMI", "Xaero's Minimap", "YetAnotherConfigLib", "AppleSkin",
      "Farmer's Delight", "Comforts", "Waystones", "AmbientSounds",
      "Sound Physics Remastered", "Iris Shaders", "Sodium", "Lithium",
      "EntityCulling", "Fabric API", "Cloth Config", "Presence Footsteps",
      "Continuity", "Effective", "FallLeaves", "Visuality", "Chat Heads",
      "Eating Animation", "Skin Layers 3D", "Detail Armor Bar",
      "Not Enough Animations", "Wavey Capes", "Auditory", "ItemPhysic Lite", "Blur",
    ],
  },
  {
    id: "pack-arcane",
    name: "Arcane Expeditions",
    author: "Vazer Team",
    desc: "Thế giới phép thuật kỳ bí, hầm ngục cổ xưa và hệ thống bùa chú Ars Nouveau.",
    longDesc: "Bước vào thế giới phép thuật huyền ảo với Ars Nouveau, Botania và Iron's Spells. Khám phá các pháo đài bị lãng quên, tiêu diệt trùm ma thuật và rèn đúc pháp bảo quyền năng.",
    loader: "fabric",
    loaderVersion: "0.19.5",
    mc: "1.21.1",
    downloads: "620K",
    downloadsNum: 620000,
    size: "310 MB",
    modCount: 36,
    categories: ["magic", "adventure"],
    tag: "Phép thuật / Magic",
    mark: "A",
    accent: "#c084fc",
    icon: "assets/modpacks/arcane-icon.png",
    image: "assets/modpacks/arcane-banner.jpg",
    versions: [
      { version: "1.0.4", mc: "1.21.1", loader: "Fabric 0.19.5", date: "2026-10-22", size: "310 MB" },
      { version: "1.0.0", mc: "1.21.1", loader: "Fabric 0.19.5", date: "2026-09-01", size: "295 MB" },
    ],
    changelog: [
      "Tích hợp Ars Nouveau với hơn 40 phép thuật mới",
      "Bổ sung phụ kiện ma thuật tương thích với Curios API",
      "Thêm hệ thống hiệu ứng hạt phép thuật mượt mà",
    ],
    mods: [
      "Ars Nouveau", "Botania", "Iron's Spells 'n Spellbooks", "GeckoLib",
      "Curios API", "Patchouli", "JEI", "Jade", "Waystones", "Clumps",
      "Fabric API", "Cloth Config", "ModMenu", "Sodium", "Lithium",
      "FerriteCore", "Indium", "AppleSkin", "Better Combat", "Simply Swords",
      "Dungeons and Taverns", "When Dungeons Arise", "Cataclysm", "Bosses of Mass Destruction",
      "Relics", "Artifacts", "Illager Invasion", "Majrusz's Progressive Difficulty",
      "Spellblades and Such", "Archon", "Runelic", "Wizards", "Paladins & Priests",
      "RPG Series Core", "Player Animator", "Combat Roll",
    ],
  },
  {
    id: "pack-skyblock",
    name: "SkyBlock Automata",
    author: "Vazer Team",
    desc: "Sinh tồn trên ốc đảo lơ lửng, tự động hóa tài nguyên từ hư vô đến hiện đại.",
    longDesc: "Thử thách sinh tồn SkyBlock đỉnh cao kết hợp hệ thống nhiệm vụ FTB Quests, sàng lọc tài nguyên Ex Nihilo và công nghệ tự động hóa năng lượng.",
    loader: "neoforge",
    loaderVersion: "21.1.72",
    mc: "1.20.1",
    downloads: "340K",
    downloadsNum: 340000,
    size: "220 MB",
    modCount: 32,
    categories: ["tech", "magic"],
    tag: "Kỹ thuật / Tech",
    mark: "S",
    accent: "#38bdf8",
    icon: "assets/modpacks/skyblock-icon.png",
    image: "assets/modpacks/skyblock-banner.jpg",
    versions: [
      { version: "1.1.2", mc: "1.20.1", loader: "NeoForge 20.4.237", date: "2026-08-11", size: "220 MB" },
    ],
    changelog: [
      "Cập nhật cây nhiệm vụ FTB Quests chương V: Năng lượng hạt nhân",
      "Tối ưu tỷ lệ rơi vật phẩm khi sàng lọc sỏi và cát",
      "Thêm sách hướng dẫn chế tạo tự động cho người chơi mới",
    ],
    mods: [
      "Ex Nihilo: Sequentia", "FTB Quests", "FTB Teams", "FTB Library",
      "Botany Pots", "Mini Coal", "JEI", "Jade", "Pipez", "Iron Chests",
      "Refined Storage", "Flux Networks", "Thermal Expansion", "Thermal Foundation",
      "Mekanism", "Solar Flux Reborn", "Clumps", "Mouse Tweaks", "FastSuite",
      "AppleSkin", "Placebo", "Click Machine", "Item Collectors", "Trash Cans",
      "Entangled", "Building Gadgets", "Cobble For Days", "Cooking for Blockheads",
      "Speedy Ladders", "Simple Storage Network", "Compact Machines", "Storage Drawers",
    ],
  },
];

const SHADER_PACKS = [
  {
    id: "complementary-reimagined",
    name: "Complementary Shaders - Reimagined",
    author: "EminGT",
    desc: "Giữ trọn phong cách Minecraft, nâng chất lượng ánh sáng, nước và thời tiết với hiệu năng ổn định.",
    longDesc: "Complementary Reimagined giữ nguyên cảm giác Minecraft nhưng nâng cấp gần như mọi thứ: ánh sáng thể tích, nước trong và phản chiếu, bóng đổ mềm, mây và thời tiết. Bộ preset được cân sẵn để vừa đẹp vừa giữ FPS ổn định.",
    style: "balanced",
    styleLabel: "Cân bằng",
    downloads: 72347971,
    version: "r5.9.3",
    size: "0.5 MB",
    mc: "1.7.10 – 26.3",
    image: "assets/shaders/complementary-reimagined.jpg",
    accent: "#f3b15c",
    features: ["Ánh sáng thể tích", "Nước & phản chiếu", "Preset tối ưu FPS"],
  },
  {
    id: "bsl-shaders",
    name: "BSL Shaders",
    author: "CaptTatsu",
    desc: "Ánh sáng rực rỡ, màu sắc nổi bật và bầu trời ấn tượng — lựa chọn cân bằng quen thuộc.",
    longDesc: "BSL là lựa chọn quen thuộc cho khung hình rực rỡ, giàu màu sắc. Bầu trời nhiều lớp mây, nắng ấm và nước trong giúp thế giới trông sống động mà vẫn dễ chịu khi chơi lâu.",
    style: "balanced",
    styleLabel: "Cân bằng",
    downloads: 31692480,
    version: "10.1.8",
    size: "1.1 MB",
    mc: "1.7.10 – 26.3",
    image: "assets/shaders/bsl-shaders.jpg",
    accent: "#68c7ff",
    features: ["Màu sắc rực rỡ", "Bầu trời chi tiết", "Tương thích rộng"],
  },
  {
    id: "photon-shader",
    name: "Photon Shaders",
    author: "sixthsurge",
    desc: "Hướng gameplay, bán thực tế: nắng ấm, sương mù và phản chiếu nhẹ nhàng, giữ FPS ổn định.",
    longDesc: "Photon tập trung vào trải nghiệm chơi: ánh sáng tự nhiên, sương mù nhẹ và phản chiếu vừa phải. Shader cân bằng giữa vẻ đẹp điện ảnh và khả năng đọc cảnh tốt khi sinh tồn hoặc PvP.",
    style: "realistic",
    styleLabel: "Chân thực",
    downloads: 29275132,
    version: "v1.4",
    size: "3.7 MB",
    mc: "1.16.5 – 26.3",
    image: "assets/shaders/photon-shader.jpg",
    accent: "#ffb26b",
    features: ["Bán thực tế", "Tối ưu gameplay", "Hiệu ứng thời tiết"],
  },
  {
    id: "solas-shader",
    name: "Solas Shader",
    author: "Septonious",
    desc: "Fantasy hiện đại với ánh sáng màu, bóng đổ mềm và không khí điện ảnh.",
    longDesc: "Solas mang phong cách fantasy hiện đại với ánh sáng màu, bóng đổ mềm và không khí huyền ảo. Đây là lựa chọn cho ai muốn thế giới trông như một bức tranh.",
    style: "fantasy",
    styleLabel: "Fantasy",
    downloads: 18628121,
    version: "3.8",
    size: "1.2 MB",
    mc: "1.12.2 – 26.3",
    image: "assets/shaders/solas-shader.jpg",
    accent: "#8b7bff",
    features: ["Ánh sáng màu", "Phong cách fantasy", "Bloom điện ảnh"],
  },
  {
    id: "bliss-shader",
    name: "Bliss Shaders",
    author: "Xonk",
    desc: "Fantasy giàu chi tiết, nhiều biến thể bầu trời/thời tiết và tùy chỉnh sâu.",
    longDesc: "Bliss tạo khung hình fantasy giàu chi tiết, thay đổi theo thời tiết và thời gian. Nhiều preset cho phép tinh chỉnh từ nhẹ nhàng đến rực rỡ mà vẫn giữ hiệu năng tốt.",
    style: "fantasy",
    styleLabel: "Fantasy",
    downloads: 15783345,
    version: "2.1.2",
    size: "1.7 MB",
    mc: "1.17 – 26.3",
    image: "assets/shaders/bliss-shader.jpg",
    accent: "#2fb7a8",
    features: ["Nhiều preset", "Thời tiết động", "Tùy chỉnh sâu"],
  },
  {
    id: "makeup-ultra-fast-shaders",
    name: "MakeUp - Ultra Fast",
    author: "KDXavier",
    desc: "Tỉ lệ chất lượng/hiệu năng cực tốt, phù hợp máy yếu vẫn muốn có shader.",
    longDesc: "MakeUp được thiết kế cho máy yếu: thêm nắng, bóng đổ và phản chiếu với chi phí thấp. Preset linh hoạt giúp chọn mức đẹp phù hợp cấu hình.",
    style: "performance",
    styleLabel: "Hiệu năng",
    downloads: 14110132,
    version: "9.6",
    size: "0.4 MB",
    mc: "1.12.2 – 26.3",
    image: "assets/shaders/makeup-ultra-fast-shaders.jpg",
    accent: "#ed6a5a",
    features: ["Siêu nhẹ", "Preset linh hoạt", "Tốt cho máy yếu"],
  },
  {
    id: "astralex",
    name: "AstraLex Shaders",
    author: "LexBoosT",
    desc: "Hiệu ứng phong phú, tùy biến cao cho ai thích khung hình nổi bật.",
    longDesc: "AstraLex dành cho người thích tùy biến: rất nhiều hiệu ứng, màu sắc đậm và các preset khác nhau. Có thể nặng hơn các lựa chọn tối ưu nhưng đổi lại là khung hình rất nổi bật.",
    style: "fantasy",
    styleLabel: "Fantasy",
    downloads: 5748859,
    version: "93.0",
    size: "3.0 MB",
    mc: "1.16.5 – 26.3",
    image: "assets/shaders/astralex.jpg",
    accent: "#d86ee8",
    features: ["Tùy biến cao", "Hiệu ứng mạnh", "Phong cách đậm"],
  },
  {
    id: "miniature-shader",
    name: "Miniature Shader",
    author: "ukrech",
    desc: "Siêu nhẹ, chỉ thêm bóng đổ và phản chiếu — gần như không ảnh hưởng FPS.",
    longDesc: "Miniature cực nhẹ, gần như chỉ bổ sung bóng đổ và phản chiếu nước. Phù hợp để làm mới hình ảnh mà không đánh đổi FPS, kể cả trên máy cấu hình thấp.",
    style: "performance",
    styleLabel: "Hiệu năng",
    downloads: 5127559,
    version: "2.19",
    size: "<0.1 MB",
    mc: "1.7.2 – 26.3",
    image: "assets/shaders/miniature-shader.jpg",
    accent: "#8da2bf",
    features: ["Nhẹ nhất", "Bóng đổ cơ bản", "Phản chiếu nước"],
  },
];

const RESOURCE_PACKS = [
  {
    id: "faithful-32x",
    slug: "faithful-32x",
    name: "Faithful 32x",
    author: "Faithful Team",
    desc: "Giữ trọn vẹn phong cách nguyên bản của Minecraft nhưng nhân đôi độ phân giải lên 32x sắc nét.",
    longDesc: "Faithful 32x là resource pack kinh điển giữ nguyên cảm giác vanilla quen thuộc nhưng tăng độ chi tiết gấp đôi. Mọi khối, vật phẩm và sinh vật đều được vẽ lại sắc nét, thích hợp cả khi chơi sinh tồn thông thường lẫn xây dựng quy mô lớn.",
    downloads: 45200000,
    version: "v1.21-r1",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "resourcepacks",
    category: "graphics",
    categoryLabel: "Đồ họa",
    size: "18.5 MB",
    icon: "assets/resourcepacks/faithful-icon.png",
    image: "assets/resourcepacks/faithful-banner.jpg",
    gallery: [
      { url: "assets/gallery/faithful-1.jpg", title: "Faithful 32x - Khối và địa hình 32x", name: "faithful-1.jpg", date: "Hôm nay", world: "Faithful World", width: 1920, height: 1080 },
      { url: "assets/gallery/faithful-2.jpg", title: "Faithful 32x - Cây cối và bầu trời", name: "faithful-2.jpg", date: "Hôm nay", world: "Faithful World", width: 1920, height: 1080 },
      { url: "assets/gallery/faithful-3.jpg", title: "Faithful 32x - Vật phẩm & Giao diện", name: "faithful-3.jpg", date: "Hôm nay", world: "Faithful World", width: 1920, height: 1080 },
    ],
    accent: "#38bdf8",
    features: ["Độ phân giải 32x", "Giữ phong cách Vanilla", "Tương thích mọi loader"],
  },
  {
    id: "bare-bones",
    slug: "bare-bones",
    name: "Bare Bones",
    author: "RobotPantaloons",
    desc: "Đơn giản hóa vân bề mặt Minecraft theo phong cách trailer chính thức của Mojang.",
    longDesc: "Bare Bones biến thế giới Minecraft của bạn trở nên phẳng, rực rỡ và tối giản như các trailer chính thức của Mojang. Tông màu ấm và đường nét dứt khoát mang lại cảm giác cực kỳ thư giãn và gọn gàng.",
    downloads: 28400000,
    version: "v1.21.1",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1", "1.19.4"],
    type: "resourcepacks",
    category: "graphics",
    categoryLabel: "Đồ họa",
    size: "4.2 MB",
    icon: "assets/resourcepacks/bare-bones-icon.png",
    image: "assets/resourcepacks/bare-bones-banner.jpg",
    gallery: [
      { url: "assets/gallery/bare-bones-1.jpg", title: "Bare Bones - Phong cách Trailer Mojang", name: "bare-bones-1.jpg", date: "Hôm nay", world: "Trailer World", width: 1920, height: 1080 },
      { url: "assets/gallery/bare-bones-2.jpg", title: "Bare Bones - Màu sắc tươi sáng", name: "bare-bones-2.jpg", date: "Hôm nay", world: "Trailer World", width: 1920, height: 1080 },
      { url: "assets/gallery/bare-bones-3.jpg", title: "Bare Bones - Kiến trúc phẳng tối giản", name: "bare-bones-3.jpg", date: "Hôm nay", world: "Trailer World", width: 1920, height: 1080 },
    ],
    accent: "#fb923c",
    features: ["Phong cách Trailer Mojang", "Tối giản & Tươi sáng", "Tăng FPS nhẹ"],
  },
  {
    id: "stay-true",
    slug: "stay-true",
    name: "Stay True",
    author: "HaiScribe",
    desc: "Tối ưu và làm mới texture vanilla: kết nối lá cây mượt mà, biến thể khối và hoa cỏ sinh động.",
    longDesc: "Stay True giữ nguyên tông màu mặc định nhưng thêm vào các biến thể khối tự nhiên, kết nối viền khối mượt mà và làm rậm rạp các tán lá cây. Mang lại một thế giới thiên nhiên sống động mà không làm mất đi chất Minecraft.",
    downloads: 22100000,
    version: "1.21.1-v1",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "resourcepacks",
    category: "survival",
    categoryLabel: "Sinh tồn/QoL",
    size: "12.8 MB",
    icon: "assets/resourcepacks/stay-true-icon.png",
    image: "assets/resourcepacks/stay-true-banner.jpg",
    accent: "#4ade80",
    features: ["Biến thể khối tự nhiên", "Lá cây rậm & kết nối", "Hài hòa với Shader"],
  },
  {
    id: "fresh-animations",
    slug: "fresh-animations",
    name: "Fresh Animations",
    author: "FreshLX",
    desc: "Cải tổ toàn bộ hoạt ảnh chuyển động của sinh vật (mobs) trở nên sống động, mượt mà và biểu cảm.",
    longDesc: "Fresh Animations mang lại chuyển động sống động và biểu cảm khuôn mặt cho tất cả sinh vật trong game. Creeper, Zombie, Dân làng hay Gấu Bắc cực đều có cử động tự nhiên như phim hoạt hình chất lượng cao.",
    downloads: 36900000,
    version: "1.9.3",
    loaders: ["fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "resourcepacks",
    category: "graphics",
    categoryLabel: "Đồ họa",
    size: "3.1 MB",
    icon: "assets/resourcepacks/fresh-animations-icon.png",
    image: "assets/resourcepacks/fresh-animations-banner.jpg",
    gallery: [
      { url: "assets/gallery/fresh-animations-1.jpg", title: "Fresh Animations - Cử động sinh động", name: "fa-1.jpg", date: "Hôm nay", world: "Animation Demo", width: 1920, height: 1080 },
      { url: "assets/gallery/fresh-animations-2.jpg", title: "Fresh Animations - Biểu cảm cử chỉ khuôn mặt", name: "fa-2.jpg", date: "Hôm nay", world: "Animation Demo", width: 1920, height: 1080 },
      { url: "assets/gallery/fresh-animations-3.jpg", title: "Fresh Animations - Dáng đi mượt mà", name: "fa-3.jpg", date: "Hôm nay", world: "Animation Demo", width: 1920, height: 1080 },
    ],
    accent: "#a78bfa",
    features: ["Animation cử động mượt", "Biểu cảm khuôn mặt", "Yêu cầu EMF / ETF"],
  },
];

const DATA_PACKS = [
  {
    id: "terralith",
    slug: "terralith",
    name: "Terralith",
    author: "Starmute",
    desc: "Đại tu địa hình thế giới Minecraft với 85+ biome hoàn toàn mới mà không cần cài mod client.",
    longDesc: "Terralith nâng cấp hoàn toàn hệ thống sinh quyển thế giới thường (Overworld) với gần 100 biome tráng lệ, từ những rặng núi tuyết hùng vĩ đến hẻm núi sâu thăm thẳm, hoàn toàn dựa trên các khối vanilla.",
    downloads: 19800000,
    version: "2.5.4",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "datapacks",
    category: "adventure",
    categoryLabel: "Phiêu lưu",
    size: "8.6 MB",
    icon: "assets/datapacks/terralith-icon.png",
    image: "assets/datapacks/terralith-banner.jpg",
    gallery: [
      { url: "assets/gallery/terralith-1.jpg", title: "Terralith - Hẻm núi và rặng tuyết", name: "terra-1.jpg", date: "Hôm nay", world: "Terralith Seed", width: 1920, height: 1080 },
      { url: "assets/gallery/terralith-2.jpg", title: "Terralith - Rừng thông ôn đới", name: "terra-2.jpg", date: "Hôm nay", world: "Terralith Seed", width: 1920, height: 1080 },
      { url: "assets/gallery/terralith-3.jpg", title: "Terralith - Thung lũng hoa oải hương", name: "terra-3.jpg", date: "Hôm nay", world: "Terralith Seed", width: 1920, height: 1080 },
    ],
    accent: "#10b981",
    features: ["85+ Biome Overworld", "Hang động kỳ vĩ", "Chạy hoàn toàn vanilla"],
  },
  {
    id: "incendium",
    slug: "incendium",
    name: "Incendium",
    author: "Starmute",
    desc: "Biến đổi Nether thành một chiều không gian hiểm trở với các hầm ngục, lâu đài và biome mới.",
    longDesc: "Incendium mở rộng Nether với 8 biome độc đáo, các pháo đài rực lửa và boss mới thử thách người chơi. Tương thích hoàn toàn với các server và modpack hiện đại.",
    downloads: 11400000,
    version: "5.4.1",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "datapacks",
    category: "adventure",
    categoryLabel: "Phiêu lưu",
    size: "7.2 MB",
    icon: "assets/datapacks/incendium-icon.png",
    image: "assets/datapacks/incendium-banner.jpg",
    accent: "#ef4444",
    features: ["Nether đại tu", "Lâu đài ngục tối mới", "Vũ khí thần thoại"],
  },
  {
    id: "nullscape",
    slug: "nullscape",
    name: "Nullscape",
    author: "Starmute",
    desc: "Đại tu toàn diện The End: tăng trần thế giới lên 384 block, quần xã đảo lơ lửng và tinh thể bí ẩn.",
    longDesc: "Nullscape làm mới hoàn toàn chiều không gian The End bằng cách tận dụng độ cao 384 block, tạo ra những dải đảo nổi khổng lồ, cấu trúc tinh thể phát sáng và vực sâu vô tận đầy kịch tính.",
    downloads: 8700000,
    version: "1.2.9",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "datapacks",
    category: "adventure",
    categoryLabel: "Phiêu lưu",
    size: "1.8 MB",
    image: "assets/shaders/astralex.jpg",
    accent: "#8b5cf6",
    features: ["The End 384 block", "Đảo nổi đa tầng", "Kiến trúc pha lê"],
  },
  {
    id: "blazeandcaves-advancements",
    slug: "blazeandcave-advancements",
    name: "BlazeandCave's Advancements",
    author: "Cavestatus",
    desc: "Bổ sung hơn 1000 tiến trình (advancements) phong phú thử thách người chơi từ cơ bản đến siêu khó.",
    longDesc: "Gói mở rộng tiến trình lớn nhất cho Minecraft với hơn 1000 nhiệm vụ thú vị chia thành nhiều danh mục: nông nghiệp, quái vật, thám hiểm, phép thuật và thử thách đỉnh cao.",
    downloads: 14200000,
    version: "1.17.2",
    loaders: ["vanilla", "fabric", "neoforge"],
    versions: ["1.21.1", "1.20.1"],
    type: "datapacks",
    category: "survival",
    categoryLabel: "Sinh tồn/QoL",
    size: "4.5 MB",
    image: "assets/shaders/miniature-shader.jpg",
    accent: "#eab308",
    features: ["1000+ Tiến trình", "Phần thưởng danh hiệu", "Hỗ trợ multiplayer"],
  },
];

const CONTENT_DEPENDENCIES = {
  iris: {
    required: ["sodium", "fabric-api"],
    optional: ["sodium-extra", "reeses-sodium-options"],
  },
  continuity: {
    required: ["indium", "fabric-api"],
    optional: [],
  },
  create: {
    required: ["fabric-api"],
    optional: ["jei", "appleskin"],
  },
  "sodium-extra": {
    required: ["sodium", "fabric-api"],
    optional: ["reeses-sodium-options"],
  },
  "reeses-sodium-options": {
    required: ["sodium", "fabric-api"],
    optional: [],
  },
  indium: {
    required: ["sodium", "fabric-api"],
    optional: [],
  },
  "irons-spells": {
    required: ["fabric-api", "cloth-config"],
    optional: ["jei"],
  },
  botania: {
    required: ["fabric-api", "cloth-config"],
    optional: ["jei", "emi"],
  },
  modmenu: {
    required: ["fabric-api"],
    optional: [],
  },
  "xaeros-minimap": {
    required: ["fabric-api"],
    optional: [],
  },
  jei: {
    required: ["fabric-api"],
    optional: [],
  },
  entityculling: {
    required: ["fabric-api"],
    optional: [],
  },
  immediatelyfast: {
    required: ["fabric-api"],
    optional: [],
  },
  "ferrite-core": {
    required: ["fabric-api"],
    optional: [],
  },
  starlight: {
    required: ["fabric-api"],
    optional: [],
  },
};

const MOD_CHANGELOGS = {
  lithium: {
    from: "0.14.3",
    to: "0.15.0",
    items: [
      { type: "add", text: "Thêm thuật toán nén AI entity pathfinding mới giảm 35% CPU tick" },
      { type: "perf", text: "Tối ưu hóa collision check cho hopper và block chứa đồ lớn" },
      { type: "fix", text: "Sửa lỗi crash khi load chunk ranh giới Nether portal ở tốc độ cao" },
    ],
  },
  sodium: {
    from: "0.6.12",
    to: "0.6.13",
    items: [
      { type: "add", text: "Hỗ trợ pipeline render mới cho driver NVIDIA 560+ (Vulkan/GL)" },
      { type: "perf", text: "Cải thiện 15% render latency khi dùng chunk render distance > 32" },
      { type: "fix", text: "Khắc phục hiện tượng xé hình trên một số GPU AMD Radeon RX series" },
    ],
  },
  jei: {
    from: "19.20.0",
    to: "19.21.0",
    items: [
      { type: "add", text: "Bookmark tags và bộ lọc vật phẩm nâng cao theo namespace mod" },
      { type: "perf", text: "Giảm thời gian index recipe khi tải instance có hơn 200 mod" },
      { type: "fix", text: "Sửa hiển thị tooltip bị tràn viền màn hình tỉ lệ 21:9 siêu rộng" },
    ],
  },
  "ferrite-core": {
    from: "7.0.0",
    to: "7.0.1",
    items: [
      { type: "perf", text: "Dọn dẹp triệt để cache Neighbor Table trong RAM giúp tiết kiệm thêm 60MB" },
      { type: "fix", text: "Tương thích tốt hơn với các mod can thiệp pipeline ánh sáng ngầm" },
    ],
  },
};

const SKIN_PRESETS = [
  {
    id: "steve",
    name: "Steve",
    model: "Classic",
    texture: "assets/skins/steve.png",
    accent: "#2aa7a1",
  },
  {
    id: "alex",
    name: "Alex",
    model: "Slim",
    texture: "assets/skins/alex.png",
    accent: "#78a868",
  },
  {
    id: "ari",
    name: "Ari",
    model: "Classic",
    texture: "assets/skins/ari.png",
    accent: "#d68ab0",
  },
  {
    id: "efe",
    name: "Efe",
    model: "Slim",
    texture: "assets/skins/efe.png",
    accent: "#d49b3f",
  },
  {
    id: "kai",
    name: "Kai",
    model: "Slim",
    texture: "assets/skins/kai.png",
    accent: "#4f8ad9",
  },
  {
    id: "makena",
    name: "Makena",
    model: "Classic",
    texture: "assets/skins/makena.png",
    accent: "#d96a86",
  },
];

const CAPE_PRESETS = [
  { id: "none", name: "Không dùng", file: null, thumb: null, desc: "Tắt áo choàng" },
  {
    id: "migrator",
    name: "Migrator",
    file: "assets/capes/migrator.png",
    thumb: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQAAAAgCAYAAAASYli2AAAA3UlEQVR4AeyQOwoCQRBE21ETA2NDjT2HZ/Fy4qkMDIw2MBE/VPBg6GVolm0mUiiK/kz1c8v+cPxmqljy7x84/4OG3/B8elit6GQYGAX4eTMQqttlYbXo+yDqZiALU30UCAFUPpA+e34+CvQLU+vCJRyCKGi4rkziHZ5PCJGuSduyNCkiZK43Ejn5hFya6/pXUj6hUiUId+uNSepJ9L1rpxbzfEKSudaq6YtaosZ5n09IMpe8i6ZWtJ9P6Inm1v0I76+nSVOJ+xFCNnzeVkvUEnPv/Qih8gStPnvphD8AAAD///GEt+kAAAAGSURBVAMACBOCifBnn+UAAAAASUVORK5CYII=",
    desc: "Kỷ niệm chuyển đổi Mojang sang Microsoft",
  },
  {
    id: "vanilla",
    name: "Vanilla",
    file: "assets/capes/vanilla.png",
    thumb: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQAAAAgCAYAAAASYli2AAAB7ElEQVR4AbTUPWtTUQDG8SdBK8j12hDEaOzU4Cwl4CJ6d3Xo6gfwGwTBzcFvkUWFrgoGXHyJi4Og3LFDXyhtSvrGpQnNUNre3n/JAyXQ7STw495z7mnPvydpyq+XnuR4//N5jndfFnNwD+4vYw5vPr7I4WeLbx/nKCvwq/zoTyxs9A+Fa/GxcDKYEbiH9x1slYT4QS70RntC6fu+EL7QO9/4dChQCarg5/V6XbgZXxdYg+rnM0Hj1/QK7z99JVAJdgfniJ3husB5gTUY3l0QxoGaXuHg/1chim4J7A7ODdSCObiotPpb8Dh84b/lTcE7+Eoljj+sCVTBz6+6hi/8tXYk7GZDIV54KVxVMDnfOxgJG6ezQvjCh42GkGYzwsqPJWGyxON8/plAFdL9XPDz8IX+zXeqVaFXvidwnvBzX7f/fhNGs/NCq9US+FlMr/B2pSK4hErwGQXnBargde12W/A4fCFVSJJESYF71OfmBN75C+N3kzmwBo3iUwLuEb7Qf3u321W3EEVR8W0TKU3TC1mWCV6Xjue9zvMeT6/QO7iAKlSKdx8uqdVqQr/fF5Li3OHn4Qsny7wTVaASnqcO1KHT6QjNZlMIXzh5Zi6hCh776vUe+8p/C8IXegdfqYLHnCM89pjzvMzzwQvPAQAA//8H/fneAAAABklEQVQDAAH5YkibB4TTAAAAAElFTkSuQmCC",
    desc: "Sở hữu cả bản Java & Bedrock Edition",
  },
  {
    id: "15th-anniversary",
    name: "15th Year",
    file: "assets/capes/15th-anniversary.png",
    thumb: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQAAAAgCAYAAAASYli2AAABCklEQVR4AdSVMYoCURBEG4NlE5cNNtjdQA/gBQRv4gG9ieAFTAUN1EAQFAxM1AeWFANiYBk48Oie6pk/9YsP0/rufZ6StCp8vcGC7e5HQT15sQa8bsusDk2jaM69ufS8w/38WI67ofcZPZqD5uQdau+pmnfoedB7HvRoDprjM/q8w1R2Wifv0POgJwcHzfEZvc/o8w6191TNOyQHhxygM/gt0Kw37BSgOZqr5h02s8MFNHXdT0eLcqSr5h2Sl7MYrwv0Rc10j3tHc9W8Q335UVVuuId7z+cd6vzc6t9XtS/IwX//p0C56QxuZ4eC23vX/3veoZyoLiebAmW2X+0KyA2k8wzoPdW4wzMAAAD//zt4xG4AAAAGSURBVAMA7hjnwW+rOKAAAAAASUVORK5CYII=",
    desc: "Kỷ niệm 15 năm Minecraft (Creeper)",
  },
  {
    id: "cherry",
    name: "Cherry",
    file: "assets/capes/cherry.png",
    thumb: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABQAAAAgCAYAAAASYli2AAABfklEQVR4AbSWPUoEQRCF34yLmYmCkSBosh7ASDD1FEamHscbeBPRK2ikIBgJRmYajP0VVb1La4OwNQsf1V1dXfX6Z3ZmnG4eJ/i+vp82gRwwKvk3TmfHgsVyV9Dm37o6FSwuloJ2nDlADshXGBXJDlRbZ3j7FEwHO4L1MdoxP+x8CoejbUFUqvb1XSoMD8+C6m8arALmU6jbJxlR+XBfguj3LDFwvicVZlQYCqgGZd/YO1NJP8bD4itw8qC7D0G+Qk4GorCpQl2pbupoQw3wBr4CcwGVkK+QrOB1V6ZUN7Urz9+tJi5fYS0be1Yd/2w08/IVckLALTeail2dHsf+A/8DkK+Q7MAtN5pT6yr0OFYH08uXIF8h6wa7c1TtSuoMMAd8OF8h6wZ7bjk5r9Q1xEAE0Abv5yv0xPbeYB9588EvxZcnUoEbAXWc/QNPlK+QEwZUAXcK7Kkp7wh8ht8zYgGVhn954IN8hZww+BbY14FVdkXhD0ssRD8sPkhX+AMAAP//lanE+AAAAAZJREFUAwCQl/WlPAlmIAAAAABJRU5ErkJggg==",
    desc: "Hoa anh đào nở rộ (Cherry Blossom)",
  },
];

const clamp = (n, min, max) => Math.min(max, Math.max(min, n));
function parseNum(val, def) {
  if (val == null || (typeof val === "string" && !val.trim())) return def;
  const n = Number(val);
  return !isNaN(n) ? n : def;
}
const dateStamp = () => {
  const d = new Date();
  const p = (n) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}_${p(d.getHours())}-${p(d.getMinutes())}-${p(d.getSeconds())}`;
};

/** "12h 30m" / "45m" — hiển thị thời gian chơi. */
function formatPlaytime(minutes) {
  const m = Math.max(0, Math.round(minutes || 0));
  if (m < 60) return `${m}m`;
  return `${Math.floor(m / 60)}h ${m % 60}m`;
}

/** Đếm số lên mượt (instance count, kết quả search). */
function countUp(el, to, { duration = 360, suffix = "" } = {}) {
  if (!el) return;
  const from = Number(el.dataset.value || 0);
  el.dataset.value = String(to);
  if (from === to || prefersReducedMotion()) {
    el.textContent = to.toLocaleString("en-US") + suffix;
    return;
  }
  const start = performance.now();
  cancelAnimationFrame(el._countRaf || 0);
  const tick = (now) => {
    const t = Math.min(1, (now - start) / duration);
    const eased = 1 - Math.pow(1 - t, 3);
    el.textContent = Math.round(from + (to - from) * eased).toLocaleString("en-US") + suffix;
    if (t < 1) el._countRaf = requestAnimationFrame(tick);
  };
  el._countRaf = requestAnimationFrame(tick);
}

const VERSION_OPTIONS = ["Auto", "All", "1.21.1", "1.21", "1.20.1", "1.20", "1.19.4", "1.19.2", "1.18.2", "1.16.5"];
const SORT_OPTIONS = ["Relevance", "Downloads", "Updated", "Newest"];

// Giống Core với resolveDependencies: true — cài kèm mod phụ thuộc.
// Fabric API chỉ áp dụng cho instance Fabric.
const DEPENDENCIES = {
  "sodium-extra": ["sodium"],
  "reeses-sodium-options": ["sodium"],
  iris: ["sodium", "fabric-api"],
  indium: ["sodium", "fabric-api"],
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
  screenshots: structuredClone(MOCK.screenshots), // instanceId -> screenshot[]
  resourcePacks: structuredClone(MOCK.resourcePacks), // instanceId -> pack[]
  instanceShaders: structuredClone(MOCK.instanceShaders), // instanceId -> shaderState
  tab: "mods",
  worldSearchQuery: "",
  worldFilterMode: "all",
  screenshotSearch: "",
  screenshotSort: "newest",
  instanceQuery: "",
  instanceSort: "lastPlayed",
  modFilter: { query: "", status: "all", sort: "name-asc", category: "all" },
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
  showLogLineNumbers: localStorage.getItem("vaz_show_log_line_numbers") !== "false",
  modSelection: { active: false, files: new Set(), lastIndex: null },
  palette: { open: false, index: 0, items: [] },
  accountHighlight: null,
  discover: {
    type: "mods",
    category: "all",
    query: "",
    loader: "all",
    version: "all",
    sort: "downloads",
  },
  contentFilters: {
    mods: { query: "", category: "all", loader: "all", version: "all", sort: "downloads" },
    resourcepacks: { query: "", version: "all", sort: "downloads" },
    datapacks: { query: "", version: "all", sort: "downloads" },
    modpacks: { query: "", category: "all", loader: "all", version: "all", sort: "downloads" },
  },
  shaders: { query: "", style: "all" },
  servers: structuredClone(DEFAULT_SERVERS),
  serverQuery: "",
  serverFilter: "all",
  serverCategory: "all",
  skinAccountId: null,
  skinSelectedId: null,
  skinAnim: "walk",
  skinAutoRotate: true,
  customSkins: [],
  customCapes: [],
  discordRpc: { enabled: true, customStatus: "Đang sinh tồn ở thế giới mới" },
  soundEnabled: true,
  globalSettings: structuredClone(DEFAULT_GLOBAL_SETTINGS),
  tasks: [],
  taskFilter: "all",
  taskSeq: 0,
  shopCoins: 650,
  shopDailyClaimed: false,
  shopOwned: ["cape-emerald"],
  shopEquipped: { capes: "cape-emerald", wings: null, hats: null, effects: null },
  shopFilter: { category: "all", query: "", sort: "featured" },
  shopTryOnItem: null,
};

/* ------------------------------------------------------------
   PERSISTENCE (mock state — để cài mod xong reload vẫn còn)
   ------------------------------------------------------------ */
let saveStateDebounceTimer = null;
function debouncedSaveState(delay = 250) {
  clearTimeout(saveStateDebounceTimer);
  saveStateDebounceTimer = setTimeout(() => {
    saveStateDebounceTimer = null;
    saveState();
  }, delay);
}

function saveState() {
  if (saveStateDebounceTimer) {
    clearTimeout(saveStateDebounceTimer);
    saveStateDebounceTimer = null;
  }
  try {
    const instances = state.instances.map((inst) => {
      const { launching, running, status, pid, sessionStart, ...rest } = inst;
      return rest;
    });
    localStorage.setItem(STORAGE_KEY, JSON.stringify({
      instances,
      mods: state.mods,
      worlds: state.worlds,
      screenshots: state.screenshots,
      resourcePacks: state.resourcePacks,
      instanceShaders: state.instanceShaders,
      accounts: state.accounts,
      servers: state.servers,
      customSkins: state.customSkins,
      customCapes: state.customCapes,
      discordRpc: state.discordRpc,
      soundEnabled: state.soundEnabled,
      globalSettings: state.globalSettings,
      activeInstanceId: state.activeInstanceId,
      theme: document.documentElement.dataset.theme,
      tasks: state.tasks.filter((t) => t.status !== "running").slice(0, 20),
      shopCoins: state.shopCoins,
      shopDailyClaimed: state.shopDailyClaimed,
      shopOwned: state.shopOwned,
      shopEquipped: state.shopEquipped,
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
    if (data.screenshots && typeof data.screenshots === "object") state.screenshots = data.screenshots;
    if (data.resourcePacks && typeof data.resourcePacks === "object") state.resourcePacks = data.resourcePacks;
    if (data.instanceShaders && typeof data.instanceShaders === "object") state.instanceShaders = data.instanceShaders;
    if (Array.isArray(data.accounts)) state.accounts = data.accounts;
    if (Array.isArray(data.servers)) state.servers = data.servers;
    if (Array.isArray(data.customSkins)) state.customSkins = data.customSkins;
    if (Array.isArray(data.customCapes)) state.customCapes = data.customCapes;
    if (data.discordRpc && typeof data.discordRpc === "object") {
      state.discordRpc = { ...state.discordRpc, ...data.discordRpc };
    }
    if (typeof data.soundEnabled === "boolean") state.soundEnabled = data.soundEnabled;
    if (data.globalSettings && typeof data.globalSettings === "object") {
      state.globalSettings = { ...state.globalSettings, ...data.globalSettings };
    }
    if (Array.isArray(data.tasks)) state.tasks = data.tasks;
    if (data.activeInstanceId && state.instances.some((i) => i.id === data.activeInstanceId)) {
      state.activeInstanceId = data.activeInstanceId;
    }
    if (data.theme === "light" || data.theme === "dark") {
      document.documentElement.dataset.theme = data.theme;
      state.globalSettings.theme = data.theme;
    }
    if (typeof data.shopCoins === "number") state.shopCoins = data.shopCoins;
    if (typeof data.shopDailyClaimed === "boolean") state.shopDailyClaimed = data.shopDailyClaimed;
    if (Array.isArray(data.shopOwned)) state.shopOwned = data.shopOwned;
    if (data.shopEquipped && typeof data.shopEquipped === "object") state.shopEquipped = data.shopEquipped;
  } catch {
    // state hỏng — dùng MOCK
  }
}

/** Bù các field mới cho state cũ trong localStorage. */
function normalizeState() {
  state.servers = (Array.isArray(state.servers) ? state.servers : structuredClone(DEFAULT_SERVERS)).map(normalizeServer);
  state.globalSettings = { ...DEFAULT_GLOBAL_SETTINGS, ...(state.globalSettings || {}) };
  state.globalSettings.ramMb = clamp(Math.round(Number(state.globalSettings.ramMb) || 4096), 1024, 16384);
  state.globalSettings.minRamMb = clamp(Math.round(Number(state.globalSettings.minRamMb) || 2048), 1024, state.globalSettings.ramMb);
  if (state.globalSettings.theme !== "light" && state.globalSettings.theme !== "dark" && state.globalSettings.theme !== "obsidian") {
    state.globalSettings.theme = "dark";
  }
  state.globalSettings.reduceMotion = !!state.globalSettings.reduceMotion;
  document.documentElement.dataset.theme = state.globalSettings.theme;
  document.documentElement.classList.toggle("reduce-motion", state.globalSettings.reduceMotion);
  if (typeof applyAccentColor === "function") {
    applyAccentColor(state.globalSettings.accentColor || "emerald");
  }
  DEFAULT_SETTINGS.ramMb = state.globalSettings.ramMb;
  DEFAULT_SETTINGS.javaPath = state.globalSettings.javaPath;

  state.customSkins = Array.isArray(state.customSkins) ? state.customSkins : [];
  state.customCapes = Array.isArray(state.customCapes) ? state.customCapes : [];
  state.discordRpc = state.discordRpc && typeof state.discordRpc === "object"
    ? { enabled: state.discordRpc.enabled !== false, customStatus: String(state.discordRpc.customStatus || "Đang sinh tồn ở thế giới mới") }
    : { enabled: true, customStatus: "Đang sinh tồn ở thế giới mới" };

  if (!["walk", "fly", "sneak", "wave", "bow", "idle", "run"].includes(state.skinAnim)) {
    state.skinAnim = "walk";
  }

  state.accounts = (Array.isArray(state.accounts) ? state.accounts : structuredClone(MOCK.accounts)).map((acc, index) => {
    const isMs = acc?.kind === "Microsoft";
    const capes = isMs ? (Array.isArray(acc?.capes) ? acc.capes : ["vanilla", "migrator", "15th-anniversary", "cherry"]) : [];
    return {
      ...acc,
      id: String(acc?.id || `acc-${index + 1}`),
      name: String(acc?.name || `Player ${index + 1}`),
      kind: isMs ? "Microsoft" : "Offline",
      active: !!acc?.active,
      skinId: getSkinPreset(acc?.skinId) ? acc.skinId : defaultSkinForAccount(acc),
      capes,
      capeId: isMs && allCapePresets().some((c) => c.id === acc?.capeId) && (acc?.capeId === "none" || capes.includes(acc?.capeId)) ? acc.capeId : (isMs && capes.length ? capes[0] : "none"),
      uuid: String(acc?.uuid || (isMs ? "c069-42fb-8501-1b4a" : "b62a-89f1-4890-a29e")),
      tokenStatus: String(acc?.tokenStatus || (isMs ? "Token: Hợp lệ (14 ngày)" : "Offline session")),
    };
  });
  if (state.accounts.length && !state.accounts.some((acc) => acc.active)) {
    state.accounts[0].active = true;
  }

  state.instances.forEach((inst) => {
    inst.settings = { ...DEFAULT_SETTINGS, ...(inst.settings || {}) };
    inst.mcOptions = { ...DEFAULT_MC_OPTIONS, ...(inst.mcOptions || {}) };
    state.instances[inst.id] = inst;
    if (typeof inst.lastPlayedTs !== "number") inst.lastPlayedTs = 0;
    if (typeof inst.createdTs !== "number") inst.createdTs = 0;
    if (typeof inst.playtimeMin !== "number") inst.playtimeMin = 0;
    if (!Array.isArray(inst.shaders)) inst.shaders = [];
    if (!state.resourcePacks[inst.id]) state.resourcePacks[inst.id] = structuredClone(MOCK.resourcePacks?.[inst.id] || []);
    if (!state.instanceShaders[inst.id]) state.instanceShaders[inst.id] = structuredClone(MOCK.instanceShaders?.[inst.id] || { activeId: null, profile: "medium", installed: [] });
    if (!state.worlds[inst.id]) state.worlds[inst.id] = structuredClone(MOCK.worlds[inst.id] || []);
    if (!state.screenshots[inst.id]) state.screenshots[inst.id] = structuredClone(MOCK.screenshots[inst.id] || []);

    // Bù thuộc tính Level.dat cho worlds cũ
    (state.worlds[inst.id] || []).forEach((w, idx) => {
      const mockW = (MOCK.worlds[inst.id] || [])[idx] || (MOCK.worlds["test fabric"] || [])[0];
      if (!w.folder) w.folder = (mockW && mockW.folder) || (w.name || "world").toLowerCase().replace(/[^a-z0-9]+/g, '_');
      if (!w.id) w.id = `w-${(w.folder || "world")}-${idx}`;
      if (!w.seed && mockW) {
        w.seed = mockW.seed;
        w.gameMode = mockW.gameMode;
        w.difficulty = mockW.difficulty;
        w.spawn = mockW.spawn;
        w.gameTime = mockW.gameTime;
        w.dayCount = mockW.dayCount;
        w.version = mockW.version;
        w.generator = mockW.generator;
        w.allowCommands = mockW.allowCommands;
      }
    });
  });
  Object.keys(state.worlds).forEach((id) => {
    if (!getInstance(id)) delete state.worlds[id];
  });
  Object.keys(state.screenshots || {}).forEach((id) => {
    if (!getInstance(id)) delete state.screenshots[id];
  });
  Object.keys(state.resourcePacks || {}).forEach((id) => {
    if (!getInstance(id)) delete state.resourcePacks[id];
  });
  Object.keys(state.instanceShaders || {}).forEach((id) => {
    if (!getInstance(id)) delete state.instanceShaders[id];
  });
  state.tasks = state.tasks
    .filter((t) => t && t.status !== "running")
    .slice(0, 20)
    .map((t) => ({
      detail: "",
      progress: t.status === "done" ? 100 : 0,
      ...t,
      endedTs: t.endedTs || t.startedTs || Date.now(),
    }));

  if (typeof state.shopCoins !== "number") state.shopCoins = 650;
  if (typeof state.shopDailyClaimed !== "boolean") state.shopDailyClaimed = false;
  if (!Array.isArray(state.shopOwned)) state.shopOwned = ["cape-emerald"];
  if (!state.shopEquipped || typeof state.shopEquipped !== "object") {
    state.shopEquipped = { capes: "cape-emerald", wings: null, hats: null, effects: null };
  }
  if (!state.shopFilter || typeof state.shopFilter !== "object") {
    state.shopFilter = { category: "all", query: "", sort: "featured" };
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

/* ------------------------------------------------------------
   ACTIVITY / TASK CENTER
   ------------------------------------------------------------ */
const TASK_KINDS = {
  launch: { icon: "E768", label: "Khởi chạy" },
  install: { icon: "E896", label: "Cài mod" },
  update: { icon: "E895", label: "Cập nhật" },
  backup: { icon: "E8B7", label: "Sao lưu" },
  restore: { icon: "E777", label: "Khôi phục" },
  create: { icon: "E710", label: "Tạo instance" },
  import: { icon: "E8B5", label: "Import" },
  crash: { icon: "E7BA", label: "Lỗi" },
};

function tasksPanelOpen() {
  return !$("#tasksHost").hidden;
}

function createTask(kind, title, detail = "") {
  const task = {
    id: `task-${Date.now()}-${++state.taskSeq}`,
    kind,
    title,
    detail,
    status: "running",
    progress: 0,
    startedTs: Date.now(),
    endedTs: null,
  };
  state.tasks.unshift(task);
  if (tasksPanelOpen()) renderTasks();
  updateTasksBadge();
  return task;
}

function updateTask(task, patch = {}) {
  if (!task) return;
  Object.assign(task, patch);
  if (typeof task.progress === "number") task.progress = clamp(task.progress, 0, 100);
  if (tasksPanelOpen()) updateTaskDom(task);
}

function finishTask(task, status = "done", detail = null) {
  if (!task) return;
  task.status = status;
  if (status === "done") task.progress = 100;
  if (detail) task.detail = detail;
  task.endedTs = Date.now();

  if (tasksPanelOpen()) {
    renderTasks();
    updateTasksBadge();
  } else if (status === "error") {
    flashTasksBadge("!", "is-error");
  } else {
    flashTasksBadge("✓", "is-done");
  }
  saveState();
}

function flashTasksBadge(text, cls = "") {
  const badge = $("#tasksBadge");
  if (!badge) return;
  clearTimeout(state.taskFlashTimer);
  state.taskFlash = { text, cls };
  state.taskFlashTimer = setTimeout(() => {
    state.taskFlash = null;
    updateTasksBadge();
  }, 3600);
  updateTasksBadge();
}

function updateTasksBadge() {
  const badge = $("#tasksBadge");
  if (!badge) return;
  const running = state.tasks.filter((t) => t.status === "running").length;

  if (state.taskFlash) {
    badge.hidden = false;
    badge.textContent = state.taskFlash.text;
    badge.className = `tasks-badge is-flash ${state.taskFlash.cls}`.trim();
    return;
  }
  badge.hidden = running === 0;
  badge.textContent = running > 0 ? String(running) : "";
  badge.className = "tasks-badge" + (running > 0 ? " is-running" : "");
}

function taskTimeAgo(ts) {
  const sec = Math.max(0, Math.round((Date.now() - ts) / 1000));
  if (sec < 45) return "vừa xong";
  const min = Math.round(sec / 60);
  if (min < 60) return `${min} phút trước`;
  const hr = Math.round(min / 60);
  if (hr < 24) return `${hr} giờ trước`;
  return `${Math.round(hr / 24)} ngày trước`;
}

function taskStatusText(status) {
  if (status === "running") return "Đang chạy";
  if (status === "error") return "Lỗi";
  if (status === "cancelled") return "Đã hủy";
  return "Hoàn tất";
}

function renderTasks() {
  const list = $("#taskList");
  if (!list) return;

  const running = state.tasks.filter((t) => t.status === "running").length;
  const finished = state.tasks.filter((t) => t.status !== "running").length;
  $("#tasksSummary").textContent = `${running} đang chạy · ${finished} đã kết thúc`;

  const filtered = state.tasks.filter((t) => {
    if (state.taskFilter === "running") return t.status === "running";
    if (state.taskFilter === "done") return t.status !== "running";
    return true;
  });

  list.innerHTML = filtered.map((task) => {
    const meta = TASK_KINDS[task.kind] || { icon: "E823", label: "Tác vụ" };
    const pct = clamp(task.progress || 0, 0, 100);
    const isRunning = task.status === "running";
    const isError = task.status === "error";
    return `
      <article class="task-item${isRunning ? " is-running" : ""}${isError ? " is-error" : ""}" data-task-id="${esc(task.id)}">
        <div class="task-icon glyph">&#x${meta.icon};</div>
        <div class="task-main">
          <div class="task-title-row">
            <span class="task-title">${esc(task.title)}</span>
            <span class="task-state">${taskStatusText(task.status)}</span>
          </div>
          <div class="task-detail">${esc(task.detail || "")}</div>
          <div class="task-track"><div class="task-fill" style="width:${pct}%"></div></div>
          <div class="task-meta">
            <span>${esc(meta.label)} · ${taskTimeAgo(task.startedTs)}</span>
            <span class="task-pct">${isRunning ? pct + "%" : taskStatusText(task.status)}</span>
          </div>
        </div>
      </article>`;
  }).join("");

  const empty = $("#tasksEmpty");
  empty.hidden = filtered.length > 0;
  if (!filtered.length) {
    empty.querySelector("p").textContent =
      state.taskFilter === "running" ? "Không có tác vụ đang chạy." :
      state.taskFilter === "done" ? "Chưa có lịch sử hoạt động." :
      "Chưa có hoạt động nào.";
  }

  $$("#tasksFilters [data-task-filter]").forEach((btn) => {
    btn.classList.toggle("is-selected", btn.dataset.taskFilter === state.taskFilter);
  });
}

function updateTaskDom(task) {
  const item = $(`.task-item[data-task-id="${task.id}"]`);
  if (!item) {
    renderTasks();
    return;
  }
  item.querySelector(".task-detail").textContent = task.detail || "";
  const pct = clamp(task.progress || 0, 0, 100);
  item.querySelector(".task-fill").style.width = pct + "%";
  item.querySelector(".task-pct").textContent = pct + "%";
}

function openTasks() {
  const host = $("#tasksHost");
  if (!host || !host.hidden && host.classList.contains("is-open")) return;
  clearTimeout(state.taskFlashTimer);
  state.taskFlash = null;
  host.hidden = false;
  void host.offsetWidth;
  host.classList.add("is-open");
  $("#tasksBtn").setAttribute("aria-expanded", "true");
  renderTasks();
  updateTasksBadge();
}

function closeTasks() {
  const host = $("#tasksHost");
  if (!host) return;
  host.classList.remove("is-open");
  $("#tasksBtn").setAttribute("aria-expanded", "false");
  setTimeout(() => {
    if (!host.classList.contains("is-open")) host.hidden = true;
  }, 180);
}

function clearTaskHistory() {
  const before = state.tasks.length;
  state.tasks = state.tasks.filter((t) => t.status === "running");
  if (state.tasks.length === before) {
    showToastStatus("Không có lịch sử để xóa.");
    return;
  }
  saveState();
  renderTasks();
  updateTasksBadge();
  showToastStatus("Đã xóa lịch sử hoạt động.");
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
  if (name === "discover") name = "mods";
  const leavingDetail = state.screen === "detail" && name !== "detail";
  const headerEl = leavingDetail ? $(".page-header", $("#page-detail")) : null;
  const headerRect = headerEl ? headerEl.getBoundingClientRect() : null;

  state.screen = name;
  $$(".nav-item").forEach((b) => b.classList.toggle("is-selected", b.dataset.screen === name));
  ["instances", "accounts", "detail", "mods", "resourcepacks", "shaders", "datapacks", "modpacks", "skins", "shop", "settings", "discover"].forEach((page) => {
    const el = $(`#page-${page}`);
    if (el) el.hidden = (page !== name && !(page === "mods" && name === "discover"));
  });
  $("#content").scrollTop = 0;
  if (skinViewer) skinViewer.renderPaused = name !== "skins";
  if (window.shopViewer) window.shopViewer.renderPaused = name !== "shop";

  if (name === "instances") renderInstances();
  if (name === "accounts") renderAccounts();
  if (name === "mods") renderContentPage("mods");
  if (name === "resourcepacks") renderContentPage("resourcepacks");
  if (name === "shaders") renderShaders();
  if (name === "datapacks") renderContentPage("datapacks");
  if (name === "modpacks") renderContentPage("modpacks");
  if (name === "skins") renderSkins();
  if (name === "shop") renderShop();
  if (name === "settings") syncGlobalSettingsUI();

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
    if (name === "skins") {
      if (skinViewer) {
        skinViewer.renderPaused = false;
        resizeSkinViewer();
      }
      const preset = getSkinPreset(state.skinSelectedId) || SKIN_PRESETS[0];
      if (preset) syncSkinViewer(preset);
    }
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

let lastActiveCard = null;

function closeDetail() {
  const detailIcon = $("#detailBlockIcon");
  const detailTitle = $("#detailName");
  let firstIcon = null;
  let firstTitle = null;

  if (detailIcon) firstIcon = detailIcon.getBoundingClientRect();
  if (detailTitle) firstTitle = detailTitle.getBoundingClientRect();

  showScreen("instances");

  const card = (lastActiveCard && lastActiveCard.isConnected)
    ? lastActiveCard
    : document.querySelector(`.instance-card[data-id="${state.activeInstanceId}"]`);

  if (card && firstIcon && firstTitle && firstIcon.width > 0) {
    const cardIcon = card.querySelector(".mc-block-icon, .card-block-badge") || card;
    const cardTitle = card.querySelector(".instance-name, .name") || card;
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
      const reduced = prefersReducedMotion();
      cardIcon.style.transition = reduced
        ? "none"
        : "transform var(--dur-base, 220ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";
      cardTitle.style.transition = reduced
        ? "none"
        : "transform var(--dur-base, 220ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";

      cardIcon.style.transform = "none";
      cardTitle.style.transform = "none";

      setTimeout(() => {
        cardIcon.style.transition = "";
        cardTitle.style.transition = "";
        cardIcon.style.transform = "";
        cardTitle.style.transform = "";
      }, reduced ? 20 : 250);
    });
  }
}

function openDetail(id, sourceCard = null) {
  const inst = getInstance(id);
  if (!inst) return;

  if (!sourceCard) {
    sourceCard = document.querySelector(`.instance-card[data-id="${id}"]`);
  }
  lastActiveCard = sourceCard;

  let firstIcon = null;
  let firstTitle = null;
  if (sourceCard) {
    const cardIcon = sourceCard.querySelector(".mc-block-icon, .card-block-badge") || sourceCard;
    const cardTitle = sourceCard.querySelector(".instance-name, .name") || sourceCard;
    if (cardIcon) firstIcon = cardIcon.getBoundingClientRect();
    if (cardTitle) firstTitle = cardTitle.getBoundingClientRect();
  }

  const previousId = state.activeInstanceId;
  state.activeInstanceId = id;
  state.tab = "mods";
  state.modSelection.active = false;
  state.modSelection.files.clear();
  state.modSelection.lastIndex = null;
  if (previousId !== id) resetModFilter();

  $("#detailName").textContent = inst.name;
  $("#detailVersion").textContent = inst.mc;
  $("#detailLoader").textContent = inst.loader + (inst.loaderVersion ? ` ${inst.loaderVersion}` : "");
  $("#detailLaunchStatus").textContent = inst.status || "";
  $("#detailStatus").textContent = "";
  $("#detailError").hidden = true;
  $("#settingsFolder").value = inst.path;

  const detailIcon = $("#detailBlockIcon");
  if (detailIcon) {
    detailIcon.innerHTML = getInstanceBlockIcon(inst.loader, "mc-block-icon", 28, inst.icon);
  }

  setTab("mods");
  renderMods();
  renderResourcePacks();
  renderInstanceShaders();
  renderWorlds();
  renderScreenshots();
  renderSettings();
  renderLogs();
  updateDetailLaunchButton();
  if (inst.running) streamLogs(id);

  showScreen("detail");

  // Shared Element FLIP: Card -> Detail
  if (firstIcon && firstTitle && detailIcon && firstIcon.width > 0) {
    const detailTitle = $("#detailName");
    const lastIcon = detailIcon.getBoundingClientRect();
    const lastTitle = detailTitle.getBoundingClientRect();

    const dxIcon = firstIcon.left - lastIcon.left;
    const dyIcon = firstIcon.top - lastIcon.top;
    const scaleIcon = lastIcon.width > 0 ? firstIcon.width / lastIcon.width : 1;

    const dxTitle = firstTitle.left - lastTitle.left;
    const dyTitle = firstTitle.top - lastTitle.top;

    detailIcon.style.transition = "none";
    detailTitle.style.transition = "none";
    detailIcon.style.transform = `translate(${dxIcon}px, ${dyIcon}px) scale(${scaleIcon})`;
    detailTitle.style.transform = `translate(${dxTitle}px, ${dyTitle}px)`;

    void detailIcon.offsetWidth;

    requestAnimationFrame(() => {
      const reduced = prefersReducedMotion();
      detailIcon.style.transition = reduced
        ? "none"
        : "transform var(--dur-slow, 380ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";
      detailTitle.style.transition = reduced
        ? "none"
        : "transform var(--dur-slow, 380ms) var(--ease-out, cubic-bezier(0.16, 1, 0.3, 1))";

      detailIcon.style.transform = "none";
      detailTitle.style.transform = "none";

      setTimeout(() => {
        detailIcon.style.transition = "";
        detailTitle.style.transition = "";
        detailIcon.style.transform = "";
        detailTitle.style.transform = "";
      }, reduced ? 20 : 400);
    });
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

function getLoaderIconSvg(loader) {
  if (loader === "neoforge") {
    return `<svg class="loader-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" xmlns="http://www.w3.org/2000/svg">
      <path d="M2.5 4L8 1.5L13.5 4V9.5L8 14.5L2.5 9.5V4Z" fill="currentColor" fill-opacity="0.25" stroke="currentColor" stroke-width="1.2"/>
      <path d="M8 5V11M5.5 8H10.5" stroke="#fbbf24" stroke-width="1.4" stroke-linecap="round"/>
    </svg>`;
  }
  if (loader === "forge") {
    return `<svg class="loader-icon" viewBox="0 0 16 16" width="12" height="12" fill="currentColor" xmlns="http://www.w3.org/2000/svg">
      <path d="M2 3h12v3H11v3H9v4H7V9H5V6H2V3z"/>
    </svg>`;
  }
  if (loader === "vanilla") {
    return `<svg class="loader-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" xmlns="http://www.w3.org/2000/svg">
      <rect x="2" y="2" width="12" height="12" rx="1.5" fill="#5cb736" stroke="currentColor" stroke-width="1"/>
      <path d="M2 6.5h12V14H2z" fill="#866043"/>
    </svg>`;
  }
  // Fabric / default: woven spool
  return `<svg class="loader-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" xmlns="http://www.w3.org/2000/svg">
    <path d="M8 1.5L1.5 5.5V10.5L8 14.5L14.5 10.5V5.5L8 1.5Z" stroke="currentColor" stroke-width="1.3"/>
    <path d="M4.5 5L11.5 11M11.5 5L4.5 11" stroke="currentColor" stroke-width="1.1"/>
  </svg>`;
}

const BLOCK_ICONS = [
  { id: "grass_block", name: "Grass Block" },
  { id: "crafting_table", name: "Crafting Table" },
  { id: "furnace", name: "Furnace" },
  { id: "anvil", name: "Anvil" },
  { id: "tnt", name: "TNT" },
  { id: "diamond_block", name: "Diamond Block" },
  { id: "bookshelf", name: "Bookshelf" },
];

function getInstanceBlockIcon(loader, className = "card-block-badge", size = 36, customIcon = null) {
  if (customIcon && (customIcon.startsWith("data:") || customIcon.startsWith("http") || customIcon.includes("/") || customIcon.endsWith(".png") || customIcon.endsWith(".jpg") || customIcon.endsWith(".webp"))) {
    return `<img class="${className}" src="${customIcon}" width="${size}" height="${size}" alt="Custom Icon" style="object-fit:cover;border-radius:4px;" loading="lazy" />`;
  }
  let file = "grass_block.png";
  let alt = "Grass Block";
  const match = customIcon ? BLOCK_ICONS.find((b) => b.id === customIcon) : null;
  if (match) {
    file = `${match.id}.png`;
    alt = match.name;
  } else if (loader === "neoforge") {
    file = "furnace.png";
    alt = "Furnace";
  } else if (loader === "vanilla") {
    file = "crafting_table.png";
    alt = "Crafting Table";
  } else if (loader === "forge") {
    file = "anvil.png";
    alt = "Anvil";
  }
  return `<img class="${className}" src="assets/blocks/${file}" width="${size}" height="${size}" alt="${alt}" loading="lazy" />`;
}

function getInstanceBlockSvg(loader, customIcon = null) {
  return getInstanceBlockIcon(loader, "card-block-badge mc-block-icon", 36, customIcon);
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
    const playtime = inst.playtimeMin ? ` · ${formatPlaytime(inst.playtimeMin)}` : "";
    const loaderClass = inst.loader === "fabric" ? "fabric" : inst.loader === "neoforge" ? "neoforge" : inst.loader === "forge" ? "forge" : "vanilla";
    const loaderLabel = inst.loader === "fabric" ? "Fabric" : inst.loader === "neoforge" ? "NeoForge" : inst.loader === "forge" ? "Forge" : "Vanilla";
    const isActiveTarget = inst.id === state.activeInstanceId;

    return `
      <div class="instance-card${inst.running ? " is-running" : ""}${inst.launching ? " is-launching" : ""}${isActiveTarget ? " is-active-target" : ""}" data-id="${esc(inst.id)}" tabindex="0" style="--i:${i}">
        ${getInstanceBlockSvg(inst.loader, inst.icon)}
        <div class="name instance-name">${esc(inst.name)}</div>
        <div class="meta">
          <span><span class="loader-tag ${loaderClass}">${getLoaderIconSvg(inst.loader)}${loaderLabel}</span>${esc(inst.mc)}</span>
          <span class="text-3">${lastPlayed}${playtime}</span>
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
  countUp($("#instancesCount"), list.length, {
    suffix: state.instanceQuery.trim() ? ` / ${state.instances.length}` : "",
  });
  $("#instancesEmptyTitle").textContent = state.instances.length === 0
    ? "Chưa có instance nào."
    : "Không tìm thấy instance nào.";
  $("#instancesEmptyHint").textContent = state.instances.length === 0
    ? "Bấm + để tạo instance mới."
    : "Thử từ khoá khác hoặc xoá bộ lọc.";
  updateGlobalQuickPlay();
}

function setCardStatus(id, text, headerText = null) {
  const inst = getInstance(id);
  if (!inst) return;
  inst.status = text;
  if (state.screen === "instances") renderInstances();
  if (state.screen === "detail" && state.activeInstanceId === id) {
    $("#detailLaunchStatus").textContent = headerText ?? text;
  }
}

function updateGlobalQuickPlay() {
  const select = $("#globalActiveInstance");
  const playBtn = $("#globalPlayBtn");
  const playIcon = $("#globalPlayIcon");
  const playText = $("#globalPlayText");
  const playSubText = $("#mcPlaySubText");
  const instanceSub = $("#mcInstanceSub");
  const shaderTarget = $("#shaderTargetInstance");

  if (select) {
    const currentOptions = Array.from(select.options).map((o) => o.value).join(",");
    const expectedOptions = state.instances.map((i) => i.id).join(",");
    if (currentOptions !== expectedOptions) {
      select.innerHTML = state.instances
        .map((i) => `<option value="${esc(i.id)}">${esc(i.name)} (${esc(i.mc)})</option>`)
        .join("");
    }
    if (select.value !== state.activeInstanceId) {
      select.value = state.activeInstanceId || (state.instances[0]?.id || "");
    }
  }

  if (shaderTarget) {
    const currentOptions = Array.from(shaderTarget.options).map((o) => o.value).join(",");
    const expectedOptions = state.instances.map((i) => i.id).join(",");
    if (currentOptions !== expectedOptions) {
      shaderTarget.innerHTML = state.instances
        .map((i) => `<option value="${esc(i.id)}">${esc(i.name)} (${esc(i.mc)} · ${esc(i.loader)})</option>`)
        .join("");
    }
    if (shaderTarget.value !== state.activeInstanceId) {
      shaderTarget.value = state.activeInstanceId || (state.instances[0]?.id || "");
    }
  }

  const inst = currentInstance();
  const instNameEl = $("#mcInstanceName");
  if (instNameEl && inst) {
    instNameEl.textContent = inst.name;
  }
  if (instanceSub && inst) {
    const loaderName = inst.loader === "fabric" ? "Fabric" : inst.loader === "neoforge" ? "NeoForge" : inst.loader === "forge" ? "Forge" : "Vanilla";
    instanceSub.textContent = `${inst.mc} · ${loaderName}`;
  }

  const iconWrap = $("#mcInstanceIconWrap");
  if (iconWrap && inst) {
    iconWrap.innerHTML = getInstanceBlockIcon(inst.loader, "mc-block-icon", 32, inst.icon);
  }

  if (!inst) {
    if (playBtn) {
      playBtn.disabled = true;
      if (playText) playText.textContent = "CHƠI";
      if (playSubText) playSubText.textContent = "Chưa chọn phiên bản";
    }
    return;
  }

  if (playBtn) {
    const running = !!inst.running;
    const launching = !!inst.launching;
    playBtn.disabled = launching;
    playBtn.classList.toggle("is-running", running);
    playBtn.classList.toggle("is-launching", launching);

    if (launching) {
      if (playIcon) playIcon.innerHTML = "&#xE768;";
      if (playText) playText.textContent = "ĐANG CHẠY...";
      if (playSubText) playSubText.textContent = `${inst.name} · Vui lòng đợi...`;
      playBtn.title = `${inst.name} đang khởi chạy...`;
    } else if (running) {
      if (playIcon) playIcon.innerHTML = "&#xE71A;";
      if (playText) playText.textContent = "DỪNG";
      if (playSubText) playSubText.textContent = `${inst.name} (PID: ${inst.pid || 1482})`;
      playBtn.title = `Dừng ${inst.name} (Ctrl+P)`;
    } else {
      if (playIcon) playIcon.innerHTML = "&#xE768;";
      if (playText) playText.textContent = "CHƠI";
      if (playSubText) playSubText.textContent = `Sẵn sàng chơi (${inst.mc})`;
      playBtn.title = `Chơi ${inst.name} (Ctrl+P)`;
    }
  }

  // Sync dock account
  syncDockAccount();

  // Update .is-active-target in instance cards
  $$("#instanceGrid .instance-card").forEach((card) => {
    card.classList.toggle("is-active-target", card.dataset.id === state.activeInstanceId);
  });
}

function syncDockAccount() {
  const activeAcc = state.accounts.find((a) => a.active) || state.accounts[0];
  const accNameEl = $("#mcAccountName");
  if (accNameEl) accNameEl.textContent = activeAcc ? activeAcc.name : "Player";
  const userBtn = $("#userBtn") || $("#mcAccountBadge");
  if (userBtn && activeAcc) {
    userBtn.setAttribute("title", `Tài khoản: ${activeAcc.name} (${activeAcc.kind}) · Đổi tài khoản / skin`);
    userBtn.setAttribute("aria-label", `Tài khoản active: ${activeAcc.name}`);
    const accStatusEl = $(".mc-account-status", userBtn);
    if (accStatusEl) accStatusEl.textContent = `${activeAcc.kind} · Sẵn sàng`;
  }
  const accFaceEl = $("#mcAccountFace");
  if (accFaceEl && activeAcc) {
    const skin = getSkinPreset(ensureAccountSkin(activeAcc));
    if (skin?.texture) {
      accFaceEl.style.setProperty("--skin-texture", `url('${skin.texture}')`);
    }
  }
}

function updateDock() {
  syncDockAccount();
  updateGlobalQuickPlay();
}

function updateDetailLaunchButton() {
  const inst = currentInstance();
  const running = !!inst?.running;
  $("#detailLaunchLabel").textContent = running ? "Stop" : "Launch";
  $("#detailLaunchBtn .glyph").innerHTML = running ? "&#xE71A;" : "&#xE768;";
  $("#detailLaunchBtn").disabled = !!inst?.launching;
  updateGlobalQuickPlay();
}

function setLaunchStep(name, stateName) {
  const el = $(`.launch-step[data-step="${name}"]`);
  if (!el) return;
  el.classList.toggle("is-active", stateName === "active");
  el.classList.toggle("is-done", stateName === "done");
}

function resetLaunchSteps() {
  $$(".launch-step").forEach((el) => el.classList.remove("is-active", "is-done"));
  $("#launchSteps").hidden = false;
}

/** Chạy thanh tiến trình từ % hiện tại tới % đích trong ms. */
async function animateProgressTo(to, ms) {
  const bar = $("#detailProgress").firstElementChild;
  const from = parseFloat(bar.style.width) || 0;
  if (prefersReducedMotion()) {
    setDetailProgress(to);
    await sleep(60);
    return;
  }
  const steps = 12;
  for (let i = 1; i <= steps; i++) {
    setDetailProgress(from + ((to - from) * i) / steps);
    await sleep(ms / steps);
  }
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
  playMinecraftSound("launch");
  const launchTask = createTask("launch", `Khởi chạy ${inst.name}`, "Đang chuẩn bị Java runtime...");
  setCardStatus(id, "Đang khởi chạy...");
  updateDetailLaunchButton();

  const phases = [
    { step: "java", pct: 25, ms: 140, card: "Đang chuẩn bị Java runtime...", detail: "Đang chuẩn bị Java runtime..." },
    { step: "meta", pct: 50, ms: 140, card: "Đang tải metadata...", detail: "Đang tải metadata..." },
    { step: "assets", pct: 75, ms: 140, card: "Đang kiểm tra assets & mods...", detail: "Đang kiểm tra assets & mods..." },
    { step: "process", pct: 100, ms: 140, card: "Đang tạo tiến trình game...", detail: "Đang tạo tiến trình game..." },
  ];

  if (fromDetail) {
    $("#detailProgress").hidden = false;
    barReset();
    resetLaunchSteps();
  }

  for (let i = 0; i < phases.length; i++) {
    const phase = phases[i];
    const phaseMs = prefersReducedMotion() ? 20 : phase.ms;
    setLaunchStep(phase.step, "active");
    updateTask(launchTask, { progress: phase.pct, detail: phase.detail });
    setCardStatus(id, phase.card, fromDetail ? "Đang khởi chạy..." : null);
    if (fromDetail) {
      $("#detailStatus").textContent = phase.detail;
      await animateProgressTo(phase.pct, phaseMs);
      setLaunchStep(phase.step, "done");
    } else {
      await sleep(phaseMs);
    }
    if (i > 0) setLaunchStep(phases[i - 1].step, "done");
  }

  const pid = rand(10000, 65000);
  const settings = { ...DEFAULT_SETTINGS, ...(inst.settings || {}) };
  inst.launching = false;
  inst.running = true;
  inst.pid = pid;
  inst.sessionStart = Date.now();
  inst.lastPlayed = "Vừa xong";
  inst.lastPlayedTs = Date.now();
  setCardStatus(id, `Đang chạy (PID ${pid})`);
  updateDetailLaunchButton();
  finishTask(launchTask, "done", `Đang chạy · PID ${pid}`);

  // latest.log được truncate mỗi lần chạy game + in cấu hình đang dùng.
  state.logs[id] = [];
  pushLog(id, `[Launcher/INFO]: Java runtime: ${settings.javaPath || "auto-detected (Java 21)"}`);
  pushLog(id, `[Launcher/INFO]: JVM args: -Xmx${settings.ramMb}M -Xms${Math.min(1024, settings.ramMb)}M ${settings.jvmArgs}`.trim());
  pushLog(id, `[Launcher/INFO]: Window: ${settings.width}x${settings.height}`);
  const mcOpts = inst.mcOptions || DEFAULT_MC_OPTIONS;
  const fpsStr = (mcOpts.maxFps === "260" || mcOpts.maxFps === 260) ? "Unlimited" : `${mcOpts.maxFps || 120} FPS`;
  pushLog(id, `[Launcher/INFO]: Loaded options.txt (renderDistance: ${mcOpts.renderDistance || 12}, maxFps: ${fpsStr}, graphics: ${mcOpts.graphicsMode || "fancy"}, fov: ${Math.round(parseFloat(mcOpts.fov) || 70)})`);
  saveState();

  if (fromDetail) {
    $("#detailStatus").textContent = "Game đã khởi chạy.";
    state.tab = "logs";
    setTab("logs");
    streamLogs(id);
  }
  return true;
}

function barReset() {
  setDetailProgress(0);
}

function stopInstance(id) {
  const inst = getInstance(id);
  if (!inst || !inst.running) return;

  if (state.logTimers[id]) {
    clearInterval(state.logTimers[id]);
    delete state.logTimers[id];
  }
  const minutes = inst.sessionStart
    ? Math.max(1, Math.round((Date.now() - inst.sessionStart) / 60000))
    : 1;
  inst.playtimeMin = (inst.playtimeMin || 0) + minutes;
  delete inst.sessionStart;
  inst.running = false;
  inst.pid = null;
  pushLog(id, "[Launcher/INFO]: Game stopped by user.");
  setCardStatus(id, `Đã dừng. Phiên chơi: ${minutes}m`);
  if (state.screen === "detail" && state.activeInstanceId === id) {
    updateDetailLaunchButton();
    setDetailStatus(`Đã dừng instance · phiên chơi ${minutes}m.`);
    renderLogs();
  }
  saveState();
  updateGlobalQuickPlay();
  if (state.screen === "instances") renderInstances();
}

function setDetailProgress(pct) {
  $("#detailProgress").firstElementChild.style.width = pct + "%";
}

/* ------------------------------------------------------------
   DETAIL — tabs
   ------------------------------------------------------------ */
const TAB_ORDER = ["mods", "resourcepacks", "shaders", "worlds", "screenshots", "logs", "settings"];

function setTab(tab) {
  const prev = state.tab;
  const animate = prev !== tab && state.screen === "detail" && !prefersReducedMotion();
  state.tab = tab;
  $$(".tab").forEach((t) => {
    const selected = t.dataset.tab === tab;
    t.classList.toggle("is-selected", selected);
    t.setAttribute("aria-selected", selected ? "true" : "false");
  });

  const panels = TAB_ORDER.map((t) => ({ name: t, el: $(`#tab-${t}`) }));
  panels.forEach(({ name, el }) => {
    if (name !== tab && name !== prev) el.hidden = true;
  });

  const nextPanel = $(`#tab-${tab}`);
  const prevPanel = prev !== tab ? $(`#tab-${prev}`) : null;

  // POL-AC-05: Ẩn panel cũ ngay lập tức để không đẩy panel mới xuống (tránh layout shift)
  panels.forEach(({ name, el }) => { el.hidden = name !== tab; });

  if (animate && prevPanel) {
    const dir = TAB_ORDER.indexOf(tab) > TAB_ORDER.indexOf(prev) ? 1 : -1;
    const inClass = dir > 0 ? "tab-in-right" : "tab-in-left";

    nextPanel.classList.remove("tab-in-left", "tab-in-right", "tab-out-left", "tab-out-right");
    nextPanel.classList.add(inClass);

    setTimeout(() => {
      if (state.tab !== tab) return;
      nextPanel.classList.remove(inClass);
    }, 260);
  }

  if (tab === "logs") renderLogs();
  if (tab === "worlds") renderWorlds();
  if (tab === "screenshots") renderScreenshots();
  if (tab === "settings") renderSettings();
  if (tab === "resourcepacks") renderResourcePacks();
  if (tab === "shaders") renderInstanceShaders();
  requestAnimationFrame(moveTabGlider);
}

/* ------------------------------------------------------------
   DETAIL — mods
   ------------------------------------------------------------ */
function resetModFilter() {
  state.modFilter.query = "";
  state.modFilter.status = "all";
  state.modFilter.sort = "name-asc";
  state.modFilter.category = "all";
}

function isModFilterActive() {
  const f = state.modFilter;
  return !!f.query.trim() || f.status !== "all" || f.sort !== "name-asc" || (f.category && f.category !== "all");
}

function clearModSelection() {
  state.modSelection.files.clear();
  state.modSelection.lastIndex = null;
}

/** Tên hiển thị của mod: tên project nếu có, ngược lại là tên file. */
function modDisplayName(mod) {
  if (mod.name) return mod.name;
  const project = mod.projectId ? catalogProject(mod.projectId) : catalogProject(modSlug(mod));
  return project ? project.name : mod.file;
}

function modSlug(mod) {
  if (mod.slug) return mod.slug;
  if (mod.projectId) {
    const p = catalogProject(mod.projectId);
    if (p) return p.slug;
  }
  const file = String(mod.file || "").toLowerCase();
  for (const p of MOCK.catalog) {
    if (file.includes(p.slug) || (p.name && file.includes(p.name.toLowerCase()))) {
      return p.slug;
    }
  }
  return "";
}

function getModCategory(mod) {
  if (mod.category) return mod.category;
  const project = mod.projectId ? catalogProject(mod.projectId) : catalogProject(modSlug(mod));
  return project?.category || "technology";
}

/** Thông tin phụ thuộc (dependencies) của mod trong instance */
function getModDependencyInfo(mod, inst) {
  if (!inst) inst = currentInstance();
  const slug = modSlug(mod);
  const requiredSlugs = DEPENDENCIES[slug] || [];
  if (requiredSlugs.length === 0) {
    return { required: [], missing: [], disabled: [], satisfied: true };
  }

  const instMods = state.mods[inst.id] || [];
  const required = [];
  const missing = [];
  const disabled = [];

  for (const depSlug of requiredSlugs) {
    const depProject = catalogProject(depSlug);
    const depName = depProject ? depProject.name : depSlug;
    const installedMod = instMods.find((m) => {
      if (m.projectId && depProject && m.projectId === depProject.id) return true;
      if (modSlug(m) === depSlug) return true;
      return String(m.file || "").toLowerCase().includes(depSlug.toLowerCase());
    });

    if (!installedMod) {
      missing.push({ slug: depSlug, name: depName, project: depProject, status: "missing" });
      required.push({ slug: depSlug, name: depName, project: depProject, status: "missing" });
    } else if (!installedMod.enabled) {
      disabled.push({ slug: depSlug, name: depName, mod: installedMod, status: "disabled" });
      required.push({ slug: depSlug, name: depName, mod: installedMod, status: "disabled" });
    } else {
      required.push({ slug: depSlug, name: depName, mod: installedMod, status: "active" });
    }
  }

  const satisfied = missing.length === 0 && disabled.length === 0;
  return { required, missing, disabled, satisfied };
}

/** Tìm các mod đang bật bị ảnh hưởng khi mod mục tiêu bị tắt hoặc gỡ */
function getDependentMods(targetMod, inst) {
  if (!inst) inst = currentInstance();
  const targetSlug = modSlug(targetMod);
  const instMods = state.mods[inst.id] || [];
  const affected = [];
  for (const m of instMods) {
    if (m === targetMod || !m.enabled) continue;
    const deps = DEPENDENCIES[modSlug(m)] || [];
    if (deps.includes(targetSlug) || (targetSlug === "sodium" && deps.includes("sodium"))) {
      affected.push(m);
    }
  }
  return affected;
}

function modMatchesFilter(mod) {
  const f = state.modFilter;
  const q = f.query.trim().toLowerCase();
  if (q) {
    const nameHit = modDisplayName(mod).toLowerCase().includes(q);
    const fileHit = String(mod.file).toLowerCase().includes(q);
    if (!nameHit && !fileHit) return false;
  }
  if (f.status === "on" && !mod.enabled) return false;
  if (f.status === "off" && mod.enabled) return false;
  if (f.status === "update" && !mod.update) return false;
  if (f.category && f.category !== "all") {
    if (getModCategory(mod) !== f.category) return false;
  }
  return true;
}

/** Danh sách mod đang hiển thị, kèm index gốc trong state.mods (không phụ thuộc DOM). */
function visibleMods() {
  const entries = [];
  currentMods().forEach((mod, index) => {
    if (modMatchesFilter(mod)) entries.push({ mod, index });
  });

  const dir = state.modFilter.sort === "name-desc" ? -1 : 1;
  entries.sort((a, b) => {
    const byName = modDisplayName(a.mod).localeCompare(modDisplayName(b.mod), "vi", { sensitivity: "base" });
    if (byName) return byName * dir;
    const byFile = a.mod.file.localeCompare(b.mod.file, "vi", { sensitivity: "base" });
    if (byFile) return byFile * dir;
    return a.index - b.index;
  });
  return entries;
}

function modStatusCounts(mods) {
  const counts = { all: mods.length, on: 0, off: 0, update: 0 };
  mods.forEach((mod) => {
    if (mod.enabled) counts.on++;
    else counts.off++;
    if (mod.update) counts.update++;
  });
  return counts;
}

/** Đồng bộ search/sort/chips/summary — không đụng vào list để giữ focus ô tìm. */
function syncModFilterControls(entries, mods) {
  const counts = modStatusCounts(mods);
  $$("#modStatusChips [data-mod-status]").forEach((btn) => {
    const key = btn.dataset.modStatus;
    const active = key === state.modFilter.status;
    btn.classList.toggle("is-selected", active);
    btn.setAttribute("aria-pressed", String(active));
    const countEl = btn.querySelector("[data-mod-count]");
    if (countEl) countEl.textContent = String(counts[key] ?? 0);
  });

  const catCounts = { all: mods.length, performance: 0, graphics: 0, technology: 0, survival: 0, magic: 0 };
  mods.forEach((m) => {
    const c = getModCategory(m);
    if (catCounts[c] !== undefined) catCounts[c]++;
  });
  $$("#modCategoryChips [data-mod-category]").forEach((btn) => {
    const key = btn.dataset.modCategory;
    const active = key === (state.modFilter.category || "all");
    btn.classList.toggle("is-selected", active);
    btn.setAttribute("aria-pressed", String(active));
    const countEl = btn.querySelector("[data-mod-cat-count]");
    if (countEl) countEl.textContent = String(catCounts[key] ?? 0);
  });

  const search = $("#modSearch");
  if (search && search.value !== state.modFilter.query) search.value = state.modFilter.query;
  const sort = $("#modSort");
  if (sort && sort.value !== state.modFilter.sort) sort.value = state.modFilter.sort;

  $("#modClearFiltersBtn").disabled = !isModFilterActive();
  $("#modSummary").textContent = `Hiển thị ${entries.length} / ${mods.length} mod`;
}

function renderMods(animateBadges = false) {
  const mods = currentMods();
  const entries = visibleMods();
  const list = $("#modList");
  const selecting = state.modSelection.active;
  const inst = currentInstance();

  list.innerHTML = entries.map(({ mod, index }) => {
    const label = modDisplayName(mod);
    const selected = state.modSelection.files.has(mod.file);
    const depInfo = mod.enabled ? getModDependencyInfo(mod, inst) : null;
    const hasMissingDeps = depInfo && !depInfo.satisfied;

    const lead = selecting
      ? `<label class="mod-check"><input type="checkbox" data-mod-select="${index}" ${selected ? "checked" : ""} /><span class="box"></span></label>`
      : `<span class="glyph">&#xE74C;</span>`;
    return `
    <div class="list-row mod-row${mod.enabled ? "" : " is-disabled"}${selected ? " is-selected" : ""}${selecting ? " is-selectable" : ""}" data-mod-row="${index}">
      ${lead}
      <div class="mod-main" title="Bấm để xem chi tiết mod">
        <div class="mod-file" title="${esc(mod.file)}">${esc(mod.file)}</div>
        <div class="mod-version">${esc(label)} · v${esc(mod.version)}</div>
      </div>
      ${hasMissingDeps ? `<span class="badge is-warning mod-badge-warning" title="Mod này thiếu thư viện phụ thuộc">Thiếu dependency</span>` : ""}
      ${mod.update ? `<span class="badge${animateBadges ? " just-badged" : ""}">Cập nhật ${esc(mod.update)}</span>` : ""}
      <button class="btn icon-only mod-menu-btn" data-mod-menu="${index}" title="Tùy chọn mod">
        <span class="glyph">&#xE712;</span>
      </button>
      <label class="switch">
        <input type="checkbox" data-mod-toggle="${index}" ${mod.enabled ? "checked" : ""} />
        <span class="track"></span>
      </label>
    </div>`;
  }).join("");

  $("#modsEmpty").hidden = mods.length > 0;
  const noResults = $("#modsNoResults");
  const showNoResults = mods.length > 0 && entries.length === 0;
  noResults.hidden = !showNoResults;
  if (showNoResults) {
    const q = state.modFilter.query.trim();
    $("#modsNoResultsHint").textContent = q
      ? `Không tìm thấy mod nào khớp "${q}".`
      : "Thử đổi trạng thái hoặc xoá bộ lọc.";
  }

  syncModFilterControls(entries, mods);
  syncUpdatesBar(animateBadges);
  syncBulkBar();
}

/** File của các mod đang được chọn và còn hiển thị dưới bộ lọc hiện tại. */
function selectedVisibleFiles() {
  const visible = new Set(visibleMods().map(({ mod }) => mod.file));
  return new Set([...state.modSelection.files].filter((file) => visible.has(file)));
}

/** Đổi tên file (khi update / đổi version) nhưng giữ selection đi theo file mới. */
function renameModFile(mod, nextFile) {
  if (!mod || !nextFile || nextFile === mod.file) return;
  if (mod.file.endsWith(".disabled") && !nextFile.endsWith(".disabled")) {
    nextFile += ".disabled";
  }
  const wasSelected = state.modSelection.files.delete(mod.file);
  mod.file = nextFile;
  if (wasSelected) state.modSelection.files.add(nextFile);
}

/** Đồng bộ thanh chọn nhiều mod + trạng thái nút Select. */
function syncBulkBar() {
  const active = state.modSelection.active;
  const count = selectedVisibleFiles().size;
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
  const entries = visibleMods();
  const last = entries.findIndex((entry) => entry.index === state.modSelection.lastIndex);
  const cur = entries.findIndex((entry) => entry.index === index);

  if (shift && last >= 0 && cur >= 0) {
    const a = Math.min(last, cur);
    const b = Math.max(last, cur);
    const select = checked ?? !files.has(mod.file);
    for (let i = a; i <= b; i++) {
      const file = entries[i].mod.file;
      if (select) files.add(file);
      else files.delete(file);
    }
  } else {
    const select = checked ?? !files.has(mod.file);
    if (select) files.add(mod.file);
    else files.delete(mod.file);
  }

  state.modSelection.lastIndex = index;
  renderMods();
}

/** Bỏ index gốc khỏi con trỏ shift-select sau khi list thay đổi. */
function shiftModSelectionIndexAfterRemoval(removedIndex) {
  const last = state.modSelection.lastIndex;
  if (last == null) return;
  if (last === removedIndex) state.modSelection.lastIndex = null;
  else if (last > removedIndex) state.modSelection.lastIndex = last - 1;
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

  const targets = visibleMods()
    .map((entry) => entry.mod)
    .filter((m) => files.has(m.file));
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
    const targetFiles = new Set(targets.map((m) => m.file));
    state.mods[state.activeInstanceId] = mods.filter((m) => !targetFiles.has(m.file));
    targetFiles.forEach((file) => files.delete(file));
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

  const tabBadge = $("#tabModsUpdateBadge");
  if (tabBadge) {
    tabBadge.hidden = count === 0;
    tabBadge.textContent = count;
  }

  const viewChangelogBtn = $("#viewChangelogBtn");
  if (viewChangelogBtn) {
    viewChangelogBtn.hidden = count === 0;
  }

  if (animateEnter && wasHidden && count > 0) {
    bar.classList.remove("is-enter");
    void bar.offsetWidth;
    bar.classList.add("is-enter");
    bar.addEventListener("animationend", () => bar.classList.remove("is-enter"), { once: true });
  }
}

function openChangelogDiffModal() {
  const inst = currentInstance();
  const pending = currentMods().filter((m) => m.update);
  if (pending.length === 0) {
    showToastStatus("Tất cả mod đã là phiên bản mới nhất.");
    return;
  }

  const cardsHtml = pending.map((mod) => {
    const project = mod.projectId ? catalogProject(mod.projectId) : null;
    const name = project ? project.name : mod.file;
    const author = project ? project.author : "Modrinth";
    const slug = (project ? project.slug : mod.file).replace(/\.jar$/, "").toLowerCase();
    const changelog = MOD_CHANGELOGS[slug] || MOD_CHANGELOGS[mod.projectId];

    let linesHtml = "";
    if (changelog && changelog.items?.length) {
      linesHtml = changelog.items.map((item) => {
        const cls = item.type === "add" ? "diff-add" : item.type === "perf" ? "diff-perf" : "diff-fix";
        const icon = item.type === "add" ? "&#xE710;" : item.type === "perf" ? "&#xE945;" : "&#xE73E;";
        const prefix = item.type === "add" ? "[Mới]" : item.type === "perf" ? "[Tối ưu]" : "[Sửa lỗi]";
        return `
          <div class="diff-line ${cls}">
            <span class="glyph" aria-hidden="true">${icon}</span>
            <span><strong>${prefix}</strong> ${esc(item.text)}</span>
          </div>
        `;
      }).join("");
    } else {
      linesHtml = `
        <div class="diff-line diff-add">
          <span class="glyph" aria-hidden="true">&#xE710;</span>
          <span><strong>[Mới]</strong> Tương thích hoàn chỉnh Minecraft ${esc(inst.mc)} và Loader ${esc(inst.loader)}</span>
        </div>
        <div class="diff-line diff-perf">
          <span class="glyph" aria-hidden="true">&#xE945;</span>
          <span><strong>[Tối ưu]</strong> Nâng cấp hiệu năng runtime và giảm độ trễ tick hệ thống</span>
        </div>
        <div class="diff-line diff-fix">
          <span class="glyph" aria-hidden="true">&#xE73E;</span>
          <span><strong>[Sửa lỗi]</strong> Khắc phục lỗi tương thích và ổn định khung hình</span>
        </div>
      `;
    }

    return `
      <div class="diff-card">
        <div class="diff-card-head">
          <div>
            <strong style="color:var(--text);font-size:14px;">${esc(name)}</strong>
            <span class="caption text-3" style="margin-left:6px;">bởi ${esc(author)}</span>
          </div>
          <div class="diff-version-jump">v${esc(mod.version)} → <strong>v${esc(mod.update)}</strong></div>
        </div>
        <div style="display:flex;flex-direction:column;gap:5px;">
          ${linesHtml}
        </div>
      </div>
    `;
  }).join("");

  showDialog({
    title: "Chi tiết thay đổi bản cập nhật (Changelog Diff)",
    wide: true,
    primary: `Cập nhật tất cả (${pending.length} mod)`,
    closeText: "Đóng",
    body: `
      <p class="caption text-2" style="margin-bottom:12px;">
        So sánh nhật ký thay đổi của <strong>${pending.length} mod</strong> sẵn sàng nâng cấp trên instance <strong>${esc(inst.name)}</strong>.
      </p>
      <div style="max-height:420px;overflow-y:auto;padding-right:4px;">
        ${cardsHtml}
      </div>
    `,
    onPrimary: () => {
      setTimeout(() => updateAll(), 150);
      return true;
    },
  });
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
/** True khi OS yêu cầu giảm chuyển động HOẶC công tắc "Giảm chuyển động" trong Settings bật.
 *  `html.force-motion` là override chỉ dành cho demo, luôn thắng cả hai. */
function prefersReducedMotion() {
  if (document.documentElement.classList.contains("force-motion")) return false;
  return window.matchMedia("(prefers-reduced-motion: reduce)").matches
    || document.documentElement.classList.contains("reduce-motion");
}

/* ---------- Demo motion override ---------- */
const FORCE_MOTION_KEY = "vaz.demo.forceMotion";

/** Banner nhỏ cho phép bật animation demo khi OS / Settings đang tắt hiệu ứng.
 *  Không có tác dụng phụ nào tới business logic; chỉ đổi class trên <html>. */
function initMotionOverride() {
  const mq = window.matchMedia("(prefers-reduced-motion: reduce)");
  const root = document.documentElement;
  let bar = null;
  let textEl = null;
  let toggleBtn = null;
  let dismissed = false;

  try {
    if (localStorage.getItem(FORCE_MOTION_KEY) === "1") root.classList.add("force-motion");
  } catch (_) {}

  const systemMotionOff = () => mq.matches || root.classList.contains("reduce-motion");

  function ensureBar() {
    if (bar && bar.isConnected) return bar;
    bar = document.createElement("div");
    bar.className = "motion-override-bar";
    bar.id = "motionOverrideBar";
    bar.hidden = true;

    textEl = document.createElement("span");
    textEl.className = "motion-override-text";

    toggleBtn = document.createElement("button");
    toggleBtn.className = "btn small";
    toggleBtn.type = "button";
    toggleBtn.id = "motionOverrideToggle";
    toggleBtn.addEventListener("click", () => {
      const next = !root.classList.contains("force-motion");
      root.classList.toggle("force-motion", next);
      try { localStorage.setItem(FORCE_MOTION_KEY, next ? "1" : "0"); } catch (_) {}
      dismissed = false;
      render();
    });

    const dismiss = document.createElement("button");
    dismiss.className = "btn icon-only small";
    dismiss.type = "button";
    dismiss.id = "motionOverrideDismiss";
    dismiss.title = "Ẩn gợi ý";
    dismiss.innerHTML = "&#xE711;";
    dismiss.addEventListener("click", () => {
      dismissed = true;
      bar.hidden = true;
    });

    bar.append(textEl, toggleBtn, dismiss);
    document.body.appendChild(bar);
    return bar;
  }

  function render() {
    const forced = root.classList.contains("force-motion");
    const off = systemMotionOff();
    if (!off && !forced) {
      if (bar) bar.hidden = true;
      return;
    }
    ensureBar();
    bar.hidden = dismissed;
    if (forced) {
      textEl.textContent = off
        ? "Đã bật animation demo (hệ thống đang tắt hiệu ứng)."
        : "Đang bật animation demo.";
      toggleBtn.textContent = "Tắt animation";
    } else {
      textEl.textContent = "Hệ thống đang tắt hiệu ứng chuyển động.";
      toggleBtn.textContent = "Bật animation";
    }
  }

  mq.addEventListener("change", () => {
    dismissed = false;
    render();
  });
  render();
}

/** Aurora trôi nhẹ theo con trỏ (tắt khi reduced motion). */
function initAmbient() {
  if (prefersReducedMotion()) return;
  let raf = 0;
  window.addEventListener("pointermove", (e) => {
    if (raf) return;
    raf = requestAnimationFrame(() => {
      raf = 0;
      const ax = (e.clientX / window.innerWidth) * 2 - 1;
      const ay = (e.clientY / window.innerHeight) * 2 - 1;
      const root = document.documentElement;
      root.style.setProperty("--ax", ax.toFixed(3));
      root.style.setProperty("--ay", ay.toFixed(3));
    });
  }, { passive: true });
}

const lastPointer = { x: 0, y: 0 };

function attachRipple(e) {
  lastPointer.x = e.clientX;
  lastPointer.y = e.clientY;
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

/**
 * Tìm dòng mod theo index gốc trong state.mods.
 * Filter/sort làm đổi thứ tự DOM nên tuyệt đối không dùng vị trí hiển thị.
 */
function rowAt(index) {
  const n = Number(index);
  if (!Number.isInteger(n)) return null;
  return $(`#modList .mod-row[data-mod-row="${n}"]`);
}

function flashModRow(index) {
  const row = rowAt(index);
  if (!row) return;
  row.classList.add("is-updated");
  setTimeout(() => row.classList.remove("is-updated"), 950);
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

function showUpdateQueue(pending) {
  const queue = $("#updateQueue");
  clearTimeout(queue._hideTimer);
  queue.hidden = false;
  queue.classList.remove("is-done");
  $("#queueSummary").textContent = `0 / ${pending.length} xong`;
  $("#queueBar").style.width = "0%";
  $("#queueItems").innerHTML = pending.map((mod, i) => {
    const project = mod.projectId ? catalogProject(mod.projectId) : null;
    const name = project ? project.name : mod.file;
    return `
      <div class="queue-item" data-queue="${i}">
        <span class="queue-dot"></span>
        <div class="queue-main">
          <div class="queue-name">${esc(name)}</div>
          <div class="queue-sub">${esc(mod.version)} → ${esc(mod.update)}</div>
        </div>
        <div class="queue-track"><div class="queue-fill"></div></div>
        <span class="queue-pct">Chờ</span>
      </div>`;
  }).join("");
}

function setQueueItem(index, stateName, pct = 0) {
  const item = $(`.queue-item[data-queue="${index}"]`);
  if (!item) return;
  item.classList.toggle("is-active", stateName === "active");
  item.classList.toggle("is-done", stateName === "done");
  item.querySelector(".queue-fill").style.width = pct + "%";
  item.querySelector(".queue-pct").textContent = stateName === "done" ? "Xong" : pct + "%";
}

function setQueueAggregate(done, total, pct) {
  const overall = total ? Math.round(((done + pct / 100) / total) * 100) : 0;
  $("#queueBar").style.width = overall + "%";
  $("#queueSummary").textContent = `${done} / ${total} xong`;
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
  showUpdateQueue(pending);
  const task = createTask("update", `Cập nhật ${pending.length} mod`, "Đang chuẩn bị...");
  let done = 0;

  for (let qi = 0; qi < pending.length; qi++) {
    const mod = pending[qi];
    const index = mods.indexOf(mod);
    const project = mod.projectId ? MOCK.catalog.find((p) => p.id === mod.projectId) : null;
    const label = project ? project.name : mod.file;
    const from = mod.version;
    const to = mod.update;

    setRowUpdating(index, true);
    setQueueItem(qi, "active", 0);

    for (let p = 0; p <= 100; p += 10) {
      setDetailProgress(p);
      setQueueItem(qi, "active", p);
      setQueueAggregate(done, pending.length, p);
      setDetailStatus(`Đang cập nhật ${label} ${from} → ${to}... ${p}%`);
      updateTask(task, {
        progress: Math.round(((qi + p / 100) / pending.length) * 100),
        detail: `${label} ${p}%`,
      });
      await sleep(70);
    }

    mod.version = to;
    renameModFile(mod, project
      ? modFileName(project, inst)
      : mod.file.replace(from, to));
    mod.update = null;
    done++;
    commitRowUpdate(index, mod, project, to);
    syncUpdatesBar();
    bumpUpdatesBar();
    setQueueItem(qi, "done", 100);
    setQueueAggregate(done, pending.length, 0);
    await sleep(120);
  }

  flashProgressDone();
  await sleep(320);
  $("#detailProgress").hidden = true;
  setDetailProgress(0);
  saveState();
  renderMods();
  if (done > 0) flashModRow(mods.indexOf(pending[pending.length - 1]));
  setButtonBusy(btn, false);
  setDetailStatus(`Đã cập nhật xong ${done} mod.`);
  finishTask(task, "done", `${done} mod đã cập nhật`);
  pulseStatus();
  const queue = $("#updateQueue");
  queue.classList.add("is-done");
  clearTimeout(queue._hideTimer);
  queue._hideTimer = setTimeout(() => {
    queue.hidden = true;
    queue.classList.remove("is-done");
  }, 1400);
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
  const task = createTask("update", `Cập nhật ${label}`, `${from} → ${to}`);

  $("#detailProgress").hidden = false;
  setRowUpdating(index, true);
  for (let p = 0; p <= 100; p += 10) {
    setDetailProgress(p);
    setDetailStatus(`Đang cập nhật ${label} ${from} → ${to}... ${p}%`);
    updateTask(task, { progress: p, detail: `${label} ${p}%` });
    await sleep(70);
  }

  mod.version = to;
  renameModFile(mod, project ? modFileName(project, inst, to) : mod.file.replace(from, to));
  mod.update = null;
  commitRowUpdate(index, mod, project, to);
  syncUpdatesBar();
  bumpUpdatesBar();
  renderMods();
  flashModRow(index);
  flashProgressDone();
  await sleep(320);
  $("#detailProgress").hidden = true;
  setDetailProgress(0);
  saveState();
  setDetailStatus(`Đã cập nhật ${label} lên ${to}.`);
  finishTask(task, "done", `Đã lên ${to}`);
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
      renameModFile(mod, project ? modFileName(project, inst, picked) : mod.file);
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
  const affected = getDependentMods(mod, currentInstance());

  let message = "File mod sẽ bị xóa khỏi thư mục mods của instance.";
  if (affected.length > 0) {
    const names = affected.map(modDisplayName).join(", ");
    message = `Cảnh báo: Các mod sau phụ thuộc vào mod này và có thể bị lỗi khi khởi động: ${names}. ` + message;
  }

  if (await confirmDialog(`Gỡ mod "${name}"?`, message, "Gỡ")) {
    state.modSelection.files.delete(mod.file);
    shiftModSelectionIndexAfterRemoval(index);
    currentMods().splice(index, 1);
    saveState();
    renderMods();
    setDetailStatus(`Đã gỡ ${name}.`);
    if (affected.length > 0) {
      showToastStatus(`Cảnh báo: Đã gỡ ${name}. Các mod (${affected.map(modDisplayName).join(", ")}) có thể thiếu dependency.`);
    }
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

/* ---------- Mod Inspector & External Jar Flow ---------- */
let currentInspectorIndex = null;

function openModInspector(index) {
  const mods = currentMods();
  const mod = mods[index];
  const inst = currentInstance();
  if (!mod || !inst) return;

  currentInspectorIndex = index;
  const project = mod.projectId ? catalogProject(mod.projectId) : catalogProject(modSlug(mod));
  const displayName = modDisplayName(mod);
  const depInfo = getModDependencyInfo(mod, inst);

  $("#modInspectorTitle").textContent = displayName;
  $("#modInspectorVersion").textContent = `v${mod.version || "1.0.0"}`;
  const loaderName = inst.loader ? (inst.loader.charAt(0).toUpperCase() + inst.loader.slice(1)) : "Fabric";
  $("#modInspectorLoader").textContent = loaderName;
  $("#modInspectorMc").textContent = inst.mc || "1.21.1";

  const iconEl = $("#modInspectorIcon");
  if (iconEl) {
    iconEl.src = project?.icon || "assets/mods/sodium.webp";
    iconEl.alt = displayName;
  }

  $("#modInspectorFile").textContent = mod.file;
  $("#modInspectorDesc").textContent = project?.desc || mod.desc || "Mod cài đặt cho Minecraft client.";
  $("#modInspectorAuthor").textContent = project?.author || mod.author || "Không rõ";

  const catKey = getModCategory(mod);
  const catLabels = {
    performance: "Hiệu năng",
    graphics: "Đồ họa",
    technology: "Công nghệ",
    survival: "Sinh tồn/QoL",
    magic: "Phép thuật",
  };
  $("#modInspectorCategory").textContent = catLabels[catKey] || "Chung";

  const linksEl = $("#modInspectorLinks");
  const slug = modSlug(mod);
  if (slug) {
    linksEl.innerHTML = `
      <a class="inspector-link-btn" href="https://modrinth.com/mod/${esc(slug)}" target="_blank" rel="noopener">
        <span class="glyph">&#xE774;</span>Modrinth
      </a>
      <a class="inspector-link-btn" href="https://github.com/search?q=${esc(slug)}" target="_blank" rel="noopener">
        <span class="glyph">&#xE8E5;</span>Mã nguồn
      </a>
    `;
  } else {
    linksEl.innerHTML = `<span class="caption text-3">Không có liên kết ngoài</span>`;
  }

  const depStatusEl = $("#modInspectorDepStatus");
  const depsListEl = $("#modInspectorDeps");
  const depActionEl = $("#modInspectorDepAction");

  if (depInfo.required.length === 0) {
    depStatusEl.className = "inspector-dep-badge is-ok";
    depStatusEl.textContent = "Không phụ thuộc";
    depsListEl.innerHTML = `<div class="caption text-3" style="padding: 4px 0;">Mod hoạt động độc lập, không yêu cầu thư viện bổ trợ.</div>`;
    depActionEl.hidden = true;
  } else if (depInfo.satisfied) {
    depStatusEl.className = "inspector-dep-badge is-ok";
    depStatusEl.textContent = "Đầy đủ";
    depsListEl.innerHTML = depInfo.required.map((dep) => `
      <div class="inspector-dep-item">
        <span class="inspector-dep-name">${esc(dep.name)}</span>
        <span class="inspector-dep-status-tag active">Đã bật</span>
      </div>
    `).join("");
    depActionEl.hidden = true;
  } else {
    depStatusEl.className = "inspector-dep-badge is-warning";
    depStatusEl.textContent = `Thiếu ${depInfo.missing.length + depInfo.disabled.length}`;
    depsListEl.innerHTML = depInfo.required.map((dep) => {
      let tagCls = "active";
      let tagText = "Đã bật";
      if (dep.status === "disabled") {
        tagCls = "disabled";
        tagText = "Đang tắt";
      } else if (dep.status === "missing") {
        tagCls = "missing";
        tagText = "Chưa cài";
      }
      return `
        <div class="inspector-dep-item">
          <span class="inspector-dep-name">${esc(dep.name)}</span>
          <span class="inspector-dep-status-tag ${tagCls}">${tagText}</span>
        </div>
      `;
    }).join("");

    depActionEl.hidden = false;
    if (depInfo.missing.length > 0) {
      $("#modInspectorResolveDepText").textContent = `Cài ${depInfo.missing.length} dependency còn thiếu`;
    } else {
      $("#modInspectorResolveDepText").textContent = `Bật ${depInfo.disabled.length} dependency bị tắt`;
    }
  }

  const host = $("#modInspectorHost");
  host.hidden = false;
  void host.offsetWidth;
  host.classList.add("is-open");
  $("#modInspectorClose").focus();
}

function closeModInspector() {
  const host = $("#modInspectorHost");
  if (!host || host.hidden) return;
  host.classList.remove("is-open");
  currentInspectorIndex = null;
  setTimeout(() => {
    if (!host.classList.contains("is-open")) {
      host.hidden = true;
    }
  }, 220);
}

function resolveInspectorDependencies() {
  if (currentInspectorIndex == null) return;
  const mods = currentMods();
  const mod = mods[currentInspectorIndex];
  const inst = currentInstance();
  if (!mod || !inst) return;

  const depInfo = getModDependencyInfo(mod, inst);
  let resolvedCount = 0;

  depInfo.disabled.forEach((dep) => {
    if (dep.mod) {
      dep.mod.enabled = true;
      resolvedCount++;
    }
  });

  depInfo.missing.forEach((dep) => {
    const project = dep.project || catalogProject(dep.slug);
    if (project) {
      addInstalledMod(project, inst);
      resolvedCount++;
    } else {
      (state.mods[inst.id] ||= []).push({
        file: `${dep.slug}-fabric.jar`,
        name: dep.name,
        version: "1.0.0",
        enabled: true,
        update: null,
        category: "technology",
      });
      resolvedCount++;
    }
  });

  saveState();
  renderMods();
  openModInspector(currentInspectorIndex);
  showToastStatus(`Đã giải quyết ${resolvedCount} dependency thành công.`);
}

function importModJarFile(fileName) {
  const inst = currentInstance();
  if (!inst) return;
  if (!fileName || typeof fileName !== "string") return;
  if (!fileName.toLowerCase().endsWith(".jar")) {
    showToastStatus("Chỉ hỗ trợ tệp định dạng .jar.");
    return;
  }

  const existing = (state.mods[inst.id] || []).find((m) => m.file.toLowerCase() === fileName.toLowerCase());
  if (existing) {
    showToastStatus(`Mod "${fileName}" đã tồn tại trong instance.`);
    return;
  }

  let cleanName = fileName.replace(/\.jar$/i, "");
  let version = "1.0.0";
  const match = cleanName.match(/[-_]v?(\d+\.\d+(?:\.\d+)?.*)$/i);
  if (match) {
    version = match[1];
    cleanName = cleanName.substring(0, match.index);
  }

  const slugGuess = cleanName.toLowerCase().replace(/[^a-z0-9]/g, "-");
  const project = catalogProject(slugGuess) || MOCK.catalog.find((p) => p.slug.includes(slugGuess) || slugGuess.includes(p.slug));

  const newMod = {
    file: fileName,
    version,
    name: project ? project.name : cleanName.replace(/[-_]/g, " "),
    enabled: true,
    update: null,
    projectId: project ? project.id : null,
    category: project ? project.category : "survival",
    author: project ? project.author : "Local Import",
    desc: project ? project.desc : "Mod được thêm từ file .jar cục bộ.",
  };

  (state.mods[inst.id] ||= []).unshift(newMod);
  saveState();
  renderMods();
  showToastStatus(`Đã import mod thành công: ${newMod.name}`);
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

function generateSampleLogs(inst) {
  const t = (offsetSec = 0) => {
    const d = new Date(Date.now() + offsetSec * 1000);
    return `${String(d.getHours()).padStart(2, "0")}:${String(d.getMinutes()).padStart(2, "0")}:${String(d.getSeconds()).padStart(2, "0")}`;
  };
  const loader = inst?.loader || "Fabric";
  const mc = inst?.mc || "1.21.1";
  return [
    `[${t(0)}] [main/INFO]: Loading Minecraft ${mc} with ${loader}`,
    `[${t(1)}] [main/INFO]: SpongePowered MIXIN Subsystem Version=0.8.5`,
    `[${t(1)}] [main/INFO]: Initializing Mixin service KNOT`,
    `[${t(2)}] [main/INFO]: Successfully loaded 18 mods: [fabric-api, sodium, iris, lithium, cloth-config]`,
    `[${t(2)}] [main/INFO]: Setting user: Steve`,
    `[${t(3)}] [Render thread/INFO]: Backend library: LWJGL version 3.3.3+5`,
    `[${t(3)}] [Render thread/INFO]: OpenGL debug context created (NVIDIA GeForce RTX 4070/PCIe/SSE2)`,
    `[${t(4)}] [Render thread/INFO]: Narrator library for x64 successfully loaded`,
    `[${t(4)}] [Render thread/INFO]: Reloading ResourceManager: vanilla, fabric`,
    `[${t(5)}] [Worker-Main-1/INFO]: Found 8 mods with data generators`,
    `[${t(5)}] [Render thread/INFO]: Sound engine started`,
    `[${t(6)}] [Render thread/INFO]: Created: 1024x512x4 minecraft:textures/atlas/blocks.png`,
    `[${t(6)}] [Render thread/INFO]: Loaded 1287 recipes`,
    `[${t(7)}] [Render thread/WARN]: Mod 'sample-opt' requested legacy OpenGL flags; fallback triggered.`,
    `[${t(7)}] [Render thread/WARN]: Texture 'custom_pack:gui/icon.png' has non-standard dimensions.`,
    `[${t(8)}] [Render thread/INFO]: Sodium: Using GL_ARB_buffer_storage`,
    `[${t(8)}] [Render thread/INFO]: Iris: Shaders initialized successfully`,
    `[${t(9)}] [Render thread/ERROR]: Failed to load registry entry for block 'custom:broken_tile'`,
    `[${t(9)}] [Render thread/ERROR]: java.lang.NullPointerException: Cannot invoke RegistryKey.getValue() because key is null`,
    `\tat net.minecraft.core.Registry.get(Registry.java:142)`,
    `\tat com.custom.block.TileInit.init(TileInit.java:45)`,
    `Caused by: java.lang.IllegalStateException: Missing block registration before freeze`,
    `\tat net.minecraft.core.MappedRegistry.validate(MappedRegistry.java:88)`,
    `[${t(10)}] [Render thread/WARN]: Game continuing in degraded mode after error recovery.`,
    `[${t(11)}] [Render thread/INFO]: Connecting to localhost:25565`,
  ];
}

function parseLogParts(raw) {
  let time = "";
  let tag = "";
  let msg = raw;

  const stdMatch = raw.match(/^(\[\d{2}:\d{2}:\d{2}\])\s+(\[[^\]]+\]):?\s*(.*)$/);
  const launcherMatch = raw.match(/^(\[Launcher\/\w+\]):?\s*(.*)$/);

  if (stdMatch) {
    time = stdMatch[1];
    tag = stdMatch[2];
    msg = stdMatch[3];
  } else if (launcherMatch) {
    tag = launcherMatch[1];
    msg = launcherMatch[2];
  }

  return { time, tag, msg };
}

function logLineHtml(line, index) {
  const sev = logSeverity(line);
  const { time, tag, msg } = parseLogParts(line);
  const num = typeof index === "number" ? index + 1 : 1;

  const timeHtml = time ? `<span class="log-time log-timestamp">${esc(time)}</span>` : "";
  const tagHtml = tag ? `<span class="log-tag log-level is-${sev.toLowerCase()}">${esc(tag)}</span>` : "";

  return `<div class="log-line${sev === "ERROR" ? " is-error" : sev === "WARN" ? " is-warn" : ""}">` +
    `<span class="log-num">${num}</span>` +
    timeHtml +
    tagHtml +
    `<span class="log-msg">${esc(msg)}</span>` +
  `</div>`;
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

  if ($("#showLineNumbersChk")) {
    $("#showLineNumbersChk").checked = state.showLogLineNumbers;
  }
  view.classList.toggle("hide-line-numbers", !state.showLogLineNumbers);

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
    renderCrashAnalyzer(id);
    return;
  }

  const wasNearBottom = view.scrollHeight - view.scrollTop - view.clientHeight < 60;
  const key = `${id}|${state.logFilter.level}|${state.logFilter.query}|${filtered.length}`;
  const prev = state.logRenderKey ? state.logRenderKey.split("|") : null;
  const sameBase = prev && prev[0] === id && prev[1] === state.logFilter.level && prev[2] === state.logFilter.query;
  const rendered = Number(view.dataset.count || 0);

  if (sameBase && filtered.length === Number(prev[3]) + 1 && filtered.length === rendered + 1) {
    view.insertAdjacentHTML("beforeend", logLineHtml(filtered[filtered.length - 1], filtered.length - 1));
    const last = view.lastElementChild;
    last.classList.add("is-new");
    last.addEventListener("animationend", () => last.classList.remove("is-new"), { once: true });
  } else if (!sameBase || filtered.length !== rendered) {
    view.innerHTML = filtered.map((line, idx) => logLineHtml(line, idx)).join("");
  }

  view.dataset.count = String(filtered.length);
  state.logRenderKey = key;

  if ($("#autoScrollChk").checked && (wasNearBottom || !sameBase)) {
    view.scrollTop = view.scrollHeight;
  }
  updateLogJump();
  renderCrashAnalyzer(id);
}

/* ------------------------------------------------------------
   DETAIL — resource packs
   ------------------------------------------------------------ */
function resourcePacksFor(inst) {
  if (!state.resourcePacks[inst.id]) {
    state.resourcePacks[inst.id] = structuredClone(MOCK.resourcePacks?.[inst.id] || []);
  }
  return state.resourcePacks[inst.id];
}

function isPackCompatible(pack, inst) {
  if (pack.incompatible === true) return false;
  if (pack.mc && pack.mc !== inst.mc && !pack.mcVersions?.includes(inst.mc)) return false;
  if (pack.mcVersions && pack.mcVersions.length > 0 && !pack.mcVersions.includes(inst.mc)) return false;
  return true;
}

function renderResourcePacks() {
  const inst = currentInstance();
  if (!inst) return;

  const packs = resourcePacksFor(inst);
  const searchInput = $("#rpSearch");
  const query = (searchInput?.value || "").trim().toLowerCase();

  const filtered = packs.filter((p) => {
    if (!query) return true;
    return (
      p.name.toLowerCase().includes(query) ||
      (p.resolution && p.resolution.toLowerCase().includes(query)) ||
      (p.version && p.version.toLowerCase().includes(query))
    );
  });

  const emptyEl = $("#rpEmpty");
  const listEl = $("#rpList");
  if (!listEl) return;

  if (filtered.length === 0) {
    if (emptyEl) {
      emptyEl.hidden = false;
      const textEl = emptyEl.querySelector(".text-2");
      if (textEl) {
        textEl.textContent = query
          ? "Không tìm thấy resource pack nào khớp với từ khóa."
          : "Chưa có resource pack nào trong instance này.";
      }
    }
    listEl.innerHTML = "";
    return;
  }

  if (emptyEl) emptyEl.hidden = true;

  listEl.innerHTML = filtered.map((pack) => {
    const realIdx = packs.findIndex((p) => p.id === pack.id);
    const compatible = isPackCompatible(pack, inst);
    const badgeHtml = compatible
      ? `<span class="badge is-success">Tương thích</span>`
      : `<span class="badge is-warning">Không tương thích (${esc(pack.mc || pack.version || "Khác MC")})</span>`;

    const isFirst = realIdx === 0;
    const isLast = realIdx === packs.length - 1;

    return `
      <div class="rp-card ${pack.enabled ? "is-enabled" : "is-disabled"}" data-pack-id="${esc(pack.id)}" role="listitem">
        <span class="rp-priority-num" title="Độ ưu tiên: ${realIdx + 1}">#${realIdx + 1}</span>
        <img class="rp-icon" src="${esc(pack.icon || 'assets/blocks/grass_block.png')}" alt="${esc(pack.name)}" onerror="this.src='assets/blocks/grass_block.png'" />
        <div class="rp-info">
          <div class="rp-title-row">
            <strong class="rp-name">${esc(pack.name)}</strong>
            <span class="chip rp-res">${esc(pack.resolution || "16x")}</span>
            ${badgeHtml}
          </div>
          <div class="rp-meta-row">
            <span class="caption text-3 rp-size">${esc(pack.size || "Unknown")}</span>
            <span class="caption text-3">v${esc(pack.version || "1.0")}</span>
          </div>
        </div>
        <div class="rp-actions">
          <button type="button" class="btn icon-only rp-move-up" data-pack-id="${esc(pack.id)}" ${isFirst ? "disabled" : ""} title="Di chuyển lên" aria-label="Di chuyển lên">
            <span class="glyph">&#xE74A;</span>
          </button>
          <button type="button" class="btn icon-only rp-move-down" data-pack-id="${esc(pack.id)}" ${isLast ? "disabled" : ""} title="Di chuyển xuống" aria-label="Di chuyển xuống">
            <span class="glyph">&#xE74B;</span>
          </button>
          <label class="switch rp-toggle" title="${pack.enabled ? 'Đang bật' : 'Đang tắt'}">
            <input type="checkbox" data-pack-id="${esc(pack.id)}" ${pack.enabled ? "checked" : ""} aria-label="Bật/Tắt pack ${esc(pack.name)}" />
            <span class="track"></span>
          </label>
        </div>
      </div>
    `;
  }).join("");

  listEl.querySelectorAll(".rp-move-up").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.stopPropagation();
      const packId = btn.dataset.packId;
      const idx = packs.findIndex((p) => p.id === packId);
      if (idx > 0) {
        const temp = packs[idx];
        packs[idx] = packs[idx - 1];
        packs[idx - 1] = temp;
        saveState();
        renderResourcePacks();
      }
    });
  });

  listEl.querySelectorAll(".rp-move-down").forEach((btn) => {
    btn.addEventListener("click", (e) => {
      e.stopPropagation();
      const packId = btn.dataset.packId;
      const idx = packs.findIndex((p) => p.id === packId);
      if (idx >= 0 && idx < packs.length - 1) {
        const temp = packs[idx];
        packs[idx] = packs[idx + 1];
        packs[idx + 1] = temp;
        saveState();
        renderResourcePacks();
      }
    });
  });

  listEl.querySelectorAll(".rp-toggle input").forEach((chk) => {
    chk.addEventListener("change", () => {
      const packId = chk.dataset.packId;
      const pack = packs.find((p) => p.id === packId);
      if (pack) {
        pack.enabled = chk.checked;
        saveState();
        showToastStatus(`Đã ${pack.enabled ? "bật" : "tắt"} gói "${pack.name}".`);
        renderResourcePacks();
      }
    });
  });
}

function addResourcePackFile(file, inst) {
  if (!file || !inst) return;
  const fileName = file.name;
  const baseName = fileName.replace(/\.zip$/i, "").replace(/[-_]/g, " ");
  const isMc120 = /1\.20/i.test(fileName);
  const packs = resourcePacksFor(inst);

  const newPack = {
    id: "rp-" + Date.now() + "-" + Math.random().toString(36).slice(2, 6),
    name: baseName,
    version: isMc120 ? "1.20" : inst.mc,
    mc: isMc120 ? "1.20" : inst.mc,
    resolution: /32x/i.test(fileName) ? "32x" : /64x/i.test(fileName) ? "64x" : "16x",
    size: file.size ? (file.size / (1024 * 1024)).toFixed(1) + " MB" : "4.8 MB",
    icon: "assets/resourcepacks/bare-bones-icon.png",
    enabled: true,
    mcVersions: isMc120 ? ["1.20", "1.20.1"] : [inst.mc],
    incompatible: isMc120 && inst.mc !== "1.20" && inst.mc !== "1.20.1",
  };

  packs.unshift(newPack);
  saveState();
  showToastStatus(`Đã thêm gói tài nguyên: ${newPack.name}`);
  renderResourcePacks();
}

/* ------------------------------------------------------------
   DETAIL — shaders
   ------------------------------------------------------------ */
function instanceShadersFor(inst) {
  if (!state.instanceShaders[inst.id]) {
    state.instanceShaders[inst.id] = structuredClone(
      MOCK.instanceShaders?.[inst.id] || {
        activeId: "bsl-shaders",
        profile: "medium",
        installed: [
          {
            id: "bsl-shaders",
            name: "BSL Shaders",
            version: "10.1.8",
            author: "CaptTatsu",
            size: "1.1 MB",
            image: "assets/shaders/bsl-shaders.jpg",
            desc: "Ánh sáng rực rỡ, màu sắc nổi bật và bầu trời ấn tượng — lựa chọn cân bằng quen thuộc.",
          },
          {
            id: "complementary-reimagined",
            name: "Complementary Shaders - Reimagined",
            version: "r5.9.3",
            author: "EminGT",
            size: "0.5 MB",
            image: "assets/shaders/complementary-reimagined.jpg",
            desc: "Giữ trọn phong cách Minecraft, nâng chất lượng ánh sáng, nước và thời tiết với hiệu năng ổn định.",
          },
          {
            id: "photon-shader",
            name: "Photon Shaders",
            version: "v1.4",
            author: "sixthsurge",
            size: "3.7 MB",
            image: "assets/shaders/photon-shader.jpg",
            desc: "Hướng gameplay, bán thực tế: nắng ấm, sương mù và phản chiếu nhẹ nhàng, giữ FPS ổn định.",
          },
          {
            id: "solas-shader",
            name: "Solas Shader",
            version: "3.8",
            author: "Septonious",
            size: "1.2 MB",
            image: "assets/shaders/solas-shader.jpg",
            desc: "Fantasy hiện đại với ánh sáng màu, bóng đổ mềm và không khí điện ảnh.",
          },
        ],
      }
    );
  }
  return state.instanceShaders[inst.id];
}

function hasIrisLoader(inst) {
  return (state.mods[inst.id] || []).some(
    (m) =>
      m.projectId === "YL57xq9U" ||
      m.slug === "iris" ||
      m.name?.toLowerCase().includes("iris") ||
      m.file?.toLowerCase().includes("iris")
  );
}

function installIrisLoader(inst) {
  if (!inst) return;
  const mods = state.mods[inst.id] || (state.mods[inst.id] = []);
  if (!hasIrisLoader(inst)) {
    mods.push({
      projectId: "YL57xq9U",
      slug: "iris",
      name: "Iris Shaders",
      file: `iris-1.8.1+mc${inst.mc}.jar`,
      version: "1.8.1",
      enabled: true,
      update: null,
    });
    if (inst.loader === "fabric" && !mods.some((m) => m.slug === "fabric-api" || m.file?.toLowerCase().includes("fabric-api"))) {
      mods.push({
        projectId: "P7dR8mSH",
        slug: "fabric-api",
        name: "Fabric API",
        file: `fabric-api-0.115.0+mc${inst.mc}.jar`,
        version: "0.115.0",
        enabled: true,
        update: null,
      });
    }
    saveState();
    showToastStatus(`Đã cài đặt Iris Shaders vào "${inst.name}".`);
    renderInstanceShaders();
    renderMods();
  }
}

function renderInstanceShaders() {
  const inst = currentInstance();
  if (!inst) return;

  const shaderState = instanceShadersFor(inst);
  const activeId = shaderState.activeId;
  const profile = shaderState.profile || "medium";
  const installed = shaderState.installed || [];

  // 1. Dependency check banner
  const bannerEl = $("#shaderLoaderBanner");
  if (bannerEl) {
    const hasIris = hasIrisLoader(inst);
    if (hasIris) {
      bannerEl.innerHTML = `
        <div class="shader-banner-ready" id="shaderReadyBanner">
          <span class="glyph" aria-hidden="true">&#xE73E;</span>
          <div class="shader-banner-copy">
            <strong>Sẵn sàng hoạt động (Iris)</strong>
            <span class="caption text-2">Instance đã có Iris Shaders. Các shaderpack được kích hoạt đầy đủ hiệu ứng.</span>
          </div>
        </div>
      `;
    } else {
      bannerEl.innerHTML = `
        <div class="shader-banner-warning" id="shaderWarningBanner">
          <div class="shader-banner-copy">
            <strong>Thiếu Shader Loader (Iris / OptiFine)</strong>
            <span class="caption text-2">Instance này chưa cài đặt Iris Shaders. Cần cài đặt loader để áp dụng shader.</span>
          </div>
          <button type="button" class="btn accent small" id="btnInstallIris">
            <span class="glyph">&#xE710;</span><span>Cài đặt Iris</span>
          </button>
        </div>
      `;
      const btn = $("#btnInstallIris");
      if (btn) {
        btn.addEventListener("click", () => installIrisLoader(inst));
      }
    }
  }

  // 2. Active Shader Details & Preview Hero
  const activeShader = installed.find((s) => s.id === activeId);
  const previewImg = $("#shaderPreviewImg");
  const previewBadge = $("#shaderPreviewBadge");
  const summaryEl = $("#shaderActiveSummary");
  const descEl = $("#shaderPreviewDesc");

  if (activeShader) {
    if (previewImg) previewImg.src = activeShader.image || "assets/shaders/bsl-shaders.jpg";
    if (previewBadge) previewBadge.textContent = activeShader.name;
    if (summaryEl) summaryEl.innerHTML = `Shader hiện tại: <strong>${esc(activeShader.name)}</strong>`;
    if (descEl) descEl.textContent = activeShader.desc || "Shaderpack chất lượng cao cho trải nghiệm hình ảnh tuyệt đẹp.";
  } else {
    // Tắt shader
    if (previewImg) previewImg.src = "assets/shaders/bsl-shaders.jpg";
    if (previewBadge) previewBadge.textContent = "Đã tắt shader";
    if (summaryEl) summaryEl.innerHTML = `Shader hiện tại: <strong>Tắt</strong>`;
    if (descEl) descEl.textContent = "Đang sử dụng đồ họa mặc định của Minecraft (không có hiệu ứng đổ bóng & phản chiếu).";
  }

  // 3. Profile Chips
  const profileLabel = $("#shaderProfileLabel");
  if (profileLabel) {
    profileLabel.textContent = profile.charAt(0).toUpperCase() + profile.slice(1);
  }
  $$("#shaderProfileChips .profile-chip").forEach((chip) => {
    const isSel = chip.dataset.profile.toLowerCase() === profile.toLowerCase();
    chip.classList.toggle("is-selected", isSel);
    chip.setAttribute("aria-pressed", isSel ? "true" : "false");
  });

  // 4. Shader Card Grid
  const gridEl = $("#shaderCardGrid");
  const countLabel = $("#shaderCountLabel");
  if (countLabel) {
    countLabel.textContent = `${installed.length} shaderpack đã cài`;
  }

  if (gridEl) {
    const isOffActive = activeId === null;
    const offCardHtml = `
      <div class="shader-card ${isOffActive ? "is-active is-selected" : ""}" data-shader-id="off" role="radio" aria-checked="${isOffActive ? "true" : "false"}" tabindex="0">
        <div class="shader-card-icon"><span class="glyph">&#xE711;</span></div>
        <div class="shader-card-body">
          <div class="shader-card-title-row">
            <strong class="shader-card-title">Tắt shader</strong>
            ${isOffActive ? '<span class="badge is-active-badge">Đang dùng</span>' : ""}
          </div>
          <span class="caption text-3">Đồ họa nguyên bản, tối đa FPS</span>
        </div>
      </div>
    `;

    const cardsHtml = installed.map((s) => {
      const isCardActive = activeId === s.id;
      return `
        <div class="shader-card ${isCardActive ? "is-active is-selected" : ""}" data-shader-id="${esc(s.id)}" role="radio" aria-checked="${isCardActive ? "true" : "false"}" tabindex="0">
          <img class="shader-card-thumb" src="${esc(s.image || "assets/shaders/bsl-shaders.jpg")}" alt="${esc(s.name)}" onerror="this.src='assets/shaders/bsl-shaders.jpg'" />
          <div class="shader-card-body">
            <div class="shader-card-title-row">
              <strong class="shader-card-title">${esc(s.name)}</strong>
              ${isCardActive ? '<span class="badge is-active-badge">Đang dùng</span>' : ""}
            </div>
            <span class="caption text-3">${esc(s.author || "Community")} · v${esc(s.version || "1.0")} · ${esc(s.size || "")}</span>
          </div>
        </div>
      `;
    }).join("");

    gridEl.innerHTML = offCardHtml + cardsHtml;

    gridEl.querySelectorAll(".shader-card").forEach((card) => {
      const handleSelect = () => {
        const sid = card.dataset.shaderId;
        if (sid === "off") {
          shaderState.activeId = null;
          showToastStatus("Đã tắt shader.");
        } else {
          shaderState.activeId = sid;
          const chosen = installed.find((s) => s.id === sid);
          showToastStatus(`Đã kích hoạt shader "${chosen?.name || sid}".`);
        }
        saveState();
        renderInstanceShaders();
      };

      card.addEventListener("click", handleSelect);
      card.addEventListener("keydown", (e) => {
        if (e.key === "Enter" || e.key === " ") {
          e.preventDefault();
          handleSelect();
        }
      });
    });
  }
}

function addShaderPackFile(file, inst) {
  if (!file || !inst) return;
  const fileName = file.name;
  const baseName = fileName.replace(/\.zip$/i, "").replace(/[-_]/g, " ");
  const shaderState = instanceShadersFor(inst);

  const newShader = {
    id: "shader-" + Date.now() + "-" + Math.random().toString(36).slice(2, 6),
    name: baseName,
    version: "1.0",
    author: "Tải lên",
    size: file.size ? (file.size / (1024 * 1024)).toFixed(1) + " MB" : "2.5 MB",
    image: "assets/shaders/bsl-shaders.jpg",
    desc: `Shaderpack cài đặt thủ công từ file ${fileName}.`,
  };

  shaderState.installed.push(newShader);
  shaderState.activeId = newShader.id;
  saveState();
  showToastStatus(`Đã thêm shaderpack: ${newShader.name}`);
  renderInstanceShaders();
}

function initResourcePacksAndShaders() {
  const rpAddZipBtn = $("#rpAddZipBtn");
  const rpFileInput = $("#rpFileInput");
  const rpSearch = $("#rpSearch");
  const rpDiscoverBtn = $("#rpDiscoverBtn");
  const openRpFolderBtn = $("#openResourcePacksFolderBtn");
  const rpDropZone = $("#rpDropZone");

  if (rpAddZipBtn && rpFileInput) {
    rpAddZipBtn.addEventListener("click", () => rpFileInput.click());
    rpFileInput.addEventListener("change", (e) => {
      const file = e.target.files?.[0];
      const inst = currentInstance();
      if (file && inst) {
        addResourcePackFile(file, inst);
      }
      e.target.value = "";
    });
  }

  if (rpSearch) {
    rpSearch.addEventListener("input", () => renderResourcePacks());
  }

  if (rpDiscoverBtn) {
    rpDiscoverBtn.addEventListener("click", () => showScreen("resourcepacks"));
  }

  if (openRpFolderBtn) {
    openRpFolderBtn.addEventListener("click", () => showToastStatus("Đã mở thư mục resourcepacks của instance."));
  }

  if (rpDropZone) {
    rpDropZone.addEventListener("dragover", (e) => {
      e.preventDefault();
      rpDropZone.classList.add("is-dragover");
    });
    rpDropZone.addEventListener("dragleave", () => {
      rpDropZone.classList.remove("is-dragover");
    });
    rpDropZone.addEventListener("drop", (e) => {
      e.preventDefault();
      rpDropZone.classList.remove("is-dragover");
      const files = e.dataTransfer?.files;
      const inst = currentInstance();
      if (files && files.length > 0 && inst) {
        addResourcePackFile(files[0], inst);
      }
    });
  }

  const shaderAddZipBtn = $("#shaderAddZipBtn");
  const shaderFileInput = $("#shaderFileInput");
  const shaderDiscoverBtn = $("#shaderDiscoverBtn");
  const openShadersFolderBtn = $("#openShadersFolderBtn");
  const shaderDropZone = $("#shaderDropZone");

  if (shaderAddZipBtn && shaderFileInput) {
    shaderAddZipBtn.addEventListener("click", () => shaderFileInput.click());
    shaderFileInput.addEventListener("change", (e) => {
      const file = e.target.files?.[0];
      const inst = currentInstance();
      if (file && inst) {
        addShaderPackFile(file, inst);
      }
      e.target.value = "";
    });
  }

  if (shaderDiscoverBtn) {
    shaderDiscoverBtn.addEventListener("click", () => showScreen("shaders"));
  }

  if (openShadersFolderBtn) {
    openShadersFolderBtn.addEventListener("click", () => showToastStatus("Đã mở thư mục shaderpacks của instance."));
  }

  if (shaderDropZone) {
    shaderDropZone.addEventListener("dragover", (e) => {
      e.preventDefault();
      shaderDropZone.classList.add("is-dragover");
    });
    shaderDropZone.addEventListener("dragleave", () => {
      shaderDropZone.classList.remove("is-dragover");
    });
    shaderDropZone.addEventListener("drop", (e) => {
      e.preventDefault();
      shaderDropZone.classList.remove("is-dragover");
      const files = e.dataTransfer?.files;
      const inst = currentInstance();
      if (files && files.length > 0 && inst) {
        addShaderPackFile(files[0], inst);
      }
    });
  }

  $$("#shaderProfileChips .profile-chip").forEach((chip) => {
    chip.addEventListener("click", () => {
      const inst = currentInstance();
      if (!inst) return;
      const profile = chip.dataset.profile;
      const shaderState = instanceShadersFor(inst);
      shaderState.profile = profile;
      saveState();
      showToastStatus(`Đã chuyển profile shader sang ${profile.toUpperCase()}.`);
      renderInstanceShaders();
    });
  });
}

/* ------------------------------------------------------------
   DETAIL — worlds
   ------------------------------------------------------------ */
let worldBackupBusy = false;

function slugifyWorldName(name) {
  return (name || "world")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '_')
    .replace(/^_+|_+$/g, '') || "world";
}

function worldFolder(w) {
  return w.folder || slugifyWorldName(w.name);
}

function ensureWorldMetadata(w, inst, idx = 0) {
  if (!w.folder) w.folder = worldFolder(w);
  if (!w.id) w.id = `w-${w.folder || "world"}-${idx}`;
  if (!w.gameMode) w.gameMode = "Survival";
  if (!w.difficulty) w.difficulty = "Normal";
  if (!w.version) w.version = inst?.mcVersion || "1.21.1";
  return w;
}

function worldsFor(inst) {
  if (!inst) return [];
  const list = (state.worlds[inst.id] ||= []);
  list.forEach((w, idx) => ensureWorldMetadata(w, inst, idx));
  return list;
}

function filteredWorldsFor(inst) {
  let list = worldsFor(inst);
  const q = (state.worldSearchQuery || "").toLowerCase();
  if (q) {
    list = list.filter((w) =>
      w.name.toLowerCase().includes(q) ||
      (w.seed && String(w.seed).toLowerCase().includes(q)) ||
      (w.folder && w.folder.toLowerCase().includes(q))
    );
  }
  const mode = (state.worldFilterMode || "all").toLowerCase();
  if (mode !== "all") {
    list = list.filter((w) => (w.gameMode || "survival").toLowerCase() === mode);
  }
  return list;
}

function findWorld(inst, idOrIndex) {
  const worlds = worldsFor(inst);
  if (typeof idOrIndex === "number") return worlds[idOrIndex];
  return worlds.find((w, i) => w.id === idOrIndex || String(i) === String(idOrIndex) || w.name === idOrIndex);
}

function renderWorlds() {
  const inst = currentInstance();
  if (!inst) return;

  const allWorlds = worldsFor(inst);
  const totalMb = allWorlds.reduce((sum, w) => sum + (w.sizeMb || 0), 0);
  const totalBackups = allWorlds.reduce((sum, w) => sum + (w.backups || 0), 0);
  const filtered = filteredWorldsFor(inst);

  const summary = $("#worldsSummary");
  if (summary) {
    if (filtered.length !== allWorlds.length) {
      summary.textContent = `${filtered.length}/${allWorlds.length} world · ${totalMb.toFixed(1)} MB · ${totalBackups} bản lưu`;
    } else {
      summary.textContent = `${allWorlds.length} world · ${totalMb.toFixed(1)} MB · ${totalBackups} bản lưu`;
    }
  }

  const listEl = $("#worldList");
  if (listEl) {
    listEl.innerHTML = filtered.map((w, i) => {
      const folder = worldFolder(w);
      const isHardcore = (w.gameMode || "").toLowerCase() === "hardcore";
      const modeLower = (w.gameMode || "survival").toLowerCase();
      const diffLower = (w.difficulty || "normal").toLowerCase();

      return `
        <div class="list-row world-row world-card" data-world-id="${esc(w.id)}" data-world-row="${i}" tabindex="0">
          <div class="world-card-top">
            <div class="world-icon" aria-hidden="true">
              <span class="glyph">&#xE81E;</span>
            </div>
            <div class="world-header-info">
              <div class="world-title-row">
                <strong class="world-name">${esc(w.name)}</strong>
                <span class="world-folder caption text-3" hidden>/saves/${esc(folder)}</span>
              </div>
              <div class="world-badges">
                <span class="world-mode-badge ${modeLower}">
                  ${isHardcore ? '<span class="glyph hardcore-glyph">&#xE706;</span> ' : ''}${esc(w.gameMode || "Survival")}
                </span>
                <span class="world-diff-badge ${diffLower}">${esc(w.difficulty || "Normal")}</span>
                ${w.seed ? `<span class="world-seed-badge" title="Seed: ${esc(w.seed)}"><span class="glyph">&#xE790;</span>Seed: <code>${esc(w.seed)}</code></span>` : ""}
              </div>
            </div>
          </div>
          <div class="world-meta caption text-2">
            <span class="world-size">${(w.sizeMb || 0).toFixed(1)} MB</span>
            <span class="world-meta-dot">·</span>
            <span class="world-last-played">Lần chơi cuối: ${esc(w.lastPlayed || "Chưa rõ")}</span>
            <span class="world-meta-dot">·</span>
            <span class="world-backups-count">${w.backups || 0} bản lưu</span>
          </div>
          <div class="world-bar"><div></div></div>
          <div class="world-actions">
            ${w.seed ? `<button class="btn world-action-btn" data-world-copy-seed="${esc(w.id)}" title="Copy Seed"><span class="glyph">&#xE8C8;</span><span>Copy Seed</span></button>` : ""}
            <button class="btn world-action-btn" data-world-inspect="${esc(w.id)}" title="Chi tiết thế giới"><span class="glyph">&#xE946;</span><span>Chi tiết</span></button>
            <button class="btn world-action-btn" data-world-backup="${esc(w.id)}" title="Sao lưu thế giới"><span class="glyph">&#xE896;</span><span>Sao lưu</span></button>
            <button class="btn world-action-btn" data-world-restore="${esc(w.id)}" title="Khôi phục bản lưu" ${w.backups ? "" : "disabled"}><span class="glyph">&#xE777;</span><span>Khôi phục</span></button>
            <button class="btn world-action-btn" data-world-rename="${esc(w.id)}" title="Đổi tên thế giới"><span class="glyph">&#xE8AC;</span><span>Đổi tên</span></button>
            <button class="btn world-action-btn" data-world-duplicate="${esc(w.id)}" title="Nhân bản thế giới"><span class="glyph">&#xE8C8;</span><span>Nhân bản</span></button>
            <button class="btn world-action-btn" data-world-export="${esc(w.id)}" title="Xuất file .zip"><span class="glyph">&#xEDE1;</span><span>Xuất zip</span></button>
            <button class="btn world-action-btn text-danger" data-world-delete="${esc(w.id)}" title="Xóa thế giới"><span class="glyph">&#xE74D;</span><span>Xóa</span></button>
          </div>
        </div>
      `;
    }).join("");
  }

  const emptyEl = $("#worldsEmpty");
  if (emptyEl) {
    if (allWorlds.length === 0) {
      emptyEl.hidden = false;
      emptyEl.querySelector(".text-2").textContent = "Instance này chưa có world nào.";
      emptyEl.querySelector(".text-3").textContent = "Bấm 'Import World' hoặc kéo thả file .zip vào đây.";
    } else if (filtered.length === 0) {
      emptyEl.hidden = false;
      emptyEl.querySelector(".text-2").textContent = "Không tìm thấy thế giới phù hợp.";
      emptyEl.querySelector(".text-3").textContent = "Thử xóa bộ lọc hoặc đổi từ khóa tìm kiếm.";
    } else {
      emptyEl.hidden = true;
    }
  }
}

function openLevelDatInspector(worldOrId) {
  const inst = currentInstance();
  if (!inst) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world) return;

  const spawnStr = world.spawn ? `X: ${world.spawn.x}, Y: ${world.spawn.y}, Z: ${world.spawn.z}` : "X: 0, Y: 64, Z: 0";
  const modeBadge = `<span class="level-badge ${(world.gameMode || 'survival').toLowerCase()}">${esc(world.gameMode || 'Survival')}</span>`;
  const diffBadge = `<span class="level-badge ${(world.difficulty || 'normal').toLowerCase()}">${esc(world.difficulty || 'Normal')}</span>`;

  const genLabel = (world.generator || "minecraft:normal").replace("minecraft:", "").replace("_", " ");
  const genDisplay = genLabel.charAt(0).toUpperCase() + genLabel.slice(1);

  showDialog({
    title: `Chi tiết thế giới — ${world.name}`,
    wide: true,
    closeText: "Đóng",
    body: `
      <div class="level-dat-card">
        <div class="level-dat-header">
          <div class="level-dat-title-row">
            <strong>${esc(world.name)}</strong>
          </div>
          <div class="level-dat-badges">
            ${modeBadge}
            ${diffBadge}
          </div>
        </div>

        <div class="level-dat-grid">
          <div class="level-dat-prop">
            <span class="level-dat-label">Seed</span>
            <div class="level-dat-val">
              <code>${esc(world.seed || "0")}</code>
              <button class="btn icon-only small" id="inspectCopySeedBtn" title="Copy Seed"><span class="glyph">&#xE8C8;</span></button>
            </div>
          </div>
          <div class="level-dat-prop">
            <span class="level-dat-label">Điểm hồi sinh</span>
            <div class="level-dat-val"><span>${esc(spawnStr)}</span></div>
          </div>
          <div class="level-dat-prop">
            <span class="level-dat-label">Thời gian chơi</span>
            <div class="level-dat-val"><span>Ngày ${world.dayCount || 1}</span></div>
          </div>
          <div class="level-dat-prop">
            <span class="level-dat-label">Phiên bản</span>
            <div class="level-dat-val"><span>Minecraft ${esc(world.version || "1.21.1")}</span></div>
          </div>
          <div class="level-dat-prop">
            <span class="level-dat-label">Kiểu địa hình</span>
            <div class="level-dat-val"><span>${esc(genDisplay)}</span></div>
          </div>
          <div class="level-dat-prop">
            <span class="level-dat-label">Lệnh (Cheats)</span>
            <div class="level-dat-val"><span>${world.allowCommands ? "Bật" : "Tắt"}</span></div>
          </div>
        </div>
      </div>
    `,
  });

  setTimeout(() => {
    $("#inspectCopySeedBtn")?.addEventListener("click", () => {
      navigator.clipboard?.writeText(String(world.seed));
      showToastStatus(`Đã copy Seed: ${world.seed}`);
    });
  }, 30);
}

async function backupWorld(worldOrId) {
  const inst = currentInstance();
  if (!inst || worldBackupBusy) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world) return;
  worldBackupBusy = true;
  const task = createTask("backup", `Sao lưu ${world.name}`, `${world.sizeMb.toFixed(1)} MB`);

  const progress = $("#worldsProgress");
  const status = $("#worldsStatus");
  const row = $(`.world-row[data-world-id="${world.id}"]`) || $(`.world-row[data-world-row="${worldOrId}"]`);
  const rowFill = row?.querySelector(".world-bar div");
  row?.classList.add("is-busy");
  if (progress) progress.hidden = false;

  for (let p = 0; p <= 100; p += 10) {
    if (progress?.firstElementChild) progress.firstElementChild.style.width = p + "%";
    if (rowFill) rowFill.style.width = p + "%";
    if (status) status.textContent = `Đang nén "${world.name}"... ${p}%`;
    updateTask(task, { progress: p, detail: `Đang nén... ${p}%` });
    await sleep(25);
  }

  world.backups = (world.backups || 0) + 1;
  worldBackupBusy = false;
  saveState();
  row?.classList.add("is-done");
  renderWorlds();

  const file = `backups/${world.name.replace(/\s+/g, "_")}-${dateStamp()}.zip`;
  if (progress) {
    progress.hidden = true;
    if (progress.firstElementChild) progress.firstElementChild.style.width = "0%";
  }
  if (status) {
    status.textContent = `Đã sao lưu "${world.name}" → ${file} (${world.sizeMb.toFixed(1)} MB).`;
  }
  finishTask(task, "done", `${file} · ${world.sizeMb.toFixed(1)} MB`);
  showToastStatus(`Đã sao lưu "${world.name}".`);
}

function worldBackups(world) {
  return Array.from({ length: world.backups || 0 }, (_, i) => {
    const d = new Date(Date.now() - i * 36e5);
    const stamp = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}_${String(d.getHours()).padStart(2, "0")}-${String(d.getMinutes()).padStart(2, "0")}`;
    return `backups/${world.name.replace(/\s+/g, "_")}-${stamp}.zip`;
  });
}

function openRestoreDialog(worldOrId) {
  const inst = currentInstance();
  if (!inst) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world || !world.backups) return;

  const backups = worldBackups(world);
  const body = `
    <p class="text-2">Chọn bản sao lưu để khôi phục cho <strong>${esc(world.name)}</strong>.</p>
    <div class="backup-list">
      ${backups.map((file, i) => `
        <label class="backup-row">
          <input type="radio" name="backupPick" value="${esc(file)}" ${i === 0 ? "checked" : ""} />
          <span class="backup-file">${esc(file.split("/").pop())}</span>
          <span class="caption text-3">${(world.sizeMb * (0.92 + i * 0.01)).toFixed(1)} MB</span>
        </label>`).join("")}
    </div>`;

  showDialog({
    title: `Khôi phục "${world.name}"`,
    body,
    primary: "Khôi phục",
    wide: true,
    onPrimary: async () => {
      const picked = document.querySelector('input[name="backupPick"]:checked')?.value;
      if (!picked) return false;
      const filename = picked.split("/").pop();
      const task = createTask("restore", `Khôi phục ${world.name}`, filename);
      const progress = $("#worldsProgress");
      const status = $("#worldsStatus");
      if (progress) progress.hidden = false;
      for (let p = 0; p <= 100; p += 10) {
        if (progress?.firstElementChild) progress.firstElementChild.style.width = p + "%";
        if (status) status.textContent = `Đang khôi phục "${world.name}"... ${p}%`;
        updateTask(task, { progress: p, detail: `${filename} · ${p}%` });
        await sleep(25);
      }
      if (progress) {
        progress.hidden = true;
        if (progress.firstElementChild) progress.firstElementChild.style.width = "0%";
      }
      world.lastPlayed = "Vừa xong";
      saveState();
      renderWorlds();
      if (status) $("#worldsStatus").textContent = `Đã khôi phục "${world.name}" từ ${filename}.`;
      finishTask(task, "done", filename);
      showToastStatus(`Đã khôi phục "${world.name}".`);
      return true;
    },
  });
}

function openRenameWorldDialog(worldOrId) {
  const inst = currentInstance();
  if (!inst) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world) return;

  const body = `
    <div class="field-row">
      <label class="field-label" for="worldRenameInput">Tên thế giới mới:</label>
      <input class="textbox" id="worldRenameInput" value="${esc(world.name)}" autocomplete="off" />
    </div>
    <p class="caption text-3" style="margin-top: 8px;">Thư mục /saves sẽ được cập nhật tương ứng theo tên mới.</p>
  `;

  showDialog({
    title: `Đổi tên thế giới: ${world.name}`,
    body,
    primary: "Lưu tên",
    closeText: "Hủy",
    onPrimary: () => {
      const input = $("#worldRenameInput");
      const newName = (input?.value || "").trim();
      if (!newName) {
        showToastStatus("Tên thế giới không được để trống!");
        return false;
      }
      const oldName = world.name;
      world.name = newName;
      world.folder = slugifyWorldName(newName);
      saveState();
      renderWorlds();
      showToastStatus(`Đã đổi tên thế giới "${oldName}" thành "${newName}".`);
      return true;
    },
  });

  setTimeout(() => {
    const input = $("#worldRenameInput");
    if (input) {
      input.focus();
      input.select();
    }
  }, 50);
}

function duplicateWorld(worldOrId) {
  const inst = currentInstance();
  if (!inst) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world) return;

  const worlds = worldsFor(inst);
  const copy = structuredClone(world);
  copy.id = `w-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`;
  copy.name = `${world.name} (Copy)`;
  copy.folder = `${worldFolder(world)}_copy_${Date.now().toString().slice(-4)}`;
  copy.backups = 0;
  copy.lastPlayed = "Vừa xong";

  const idx = worlds.indexOf(world);
  if (idx !== -1) {
    worlds.splice(idx + 1, 0, copy);
  } else {
    worlds.push(copy);
  }

  saveState();
  renderWorlds();
  showToastStatus(`Đã nhân bản thế giới: "${copy.name}".`);
}

function exportWorld(worldOrId) {
  const inst = currentInstance();
  if (!inst) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world) return;

  const filename = `${world.name.replace(/\s+/g, "_")}.zip`;
  showToastStatus(`Đang xuất "${world.name}" thành file .zip...`);

  setTimeout(() => {
    showToastStatus(`Đã xuất "${filename}" thành công.`);
  }, 200);
}

async function deleteWorld(worldOrId) {
  const inst = currentInstance();
  if (!inst) return;
  const world = typeof worldOrId === "object" ? worldOrId : findWorld(inst, worldOrId);
  if (!world) return;

  const folder = worldFolder(world);
  const confirmed = await confirmDialog(
    `Xóa thế giới "${world.name}"?`,
    `Hành động này sẽ xóa vĩnh viễn thư mục /saves/${folder} và toàn bộ dữ liệu thế giới. Bạn có chắc chắn muốn xóa?`,
    "Xóa vĩnh viễn"
  );

  if (!confirmed) return;

  const worlds = worldsFor(inst);
  const idx = worlds.indexOf(world);
  if (idx !== -1) {
    worlds.splice(idx, 1);
  }
  saveState();
  renderWorlds();
  showToastStatus(`Đã xóa thế giới "${world.name}".`);
}

function importWorldFile(file) {
  const inst = currentInstance();
  if (!inst || !file) return;

  if (!file.name.toLowerCase().endsWith(".zip")) {
    showToastStatus("Chỉ chấp nhận file .zip lưu thế giới Minecraft!");
    return;
  }

  const baseName = file.name.replace(/\.zip$/i, "").replace(/[_-]/g, " ").trim();
  const folderName = baseName.toLowerCase().replace(/[^a-z0-9]+/g, '_');
  const sizeMb = file.size ? +(file.size / (1024 * 1024)).toFixed(1) : +(Math.random() * 60 + 25).toFixed(1);

  const newWorld = {
    id: `w-${Date.now()}-${Math.random().toString(36).slice(2, 7)}`,
    name: baseName || "Imported World",
    folder: folderName || "imported_world",
    sizeMb: sizeMb > 0 ? sizeMb : 42.5,
    lastPlayed: "Vừa xong",
    backups: 0,
    seed: String(Math.floor(Math.random() * 9000000000000000) + 1000000000000000),
    gameMode: "Survival",
    difficulty: "Normal",
    spawn: { x: 0, y: 64, z: 0 },
    gameTime: "1,200 ticks (~0.1h)",
    dayCount: 1,
    version: inst.mcVersion || "1.21.1",
    generator: "Default",
    allowCommands: false,
  };

  const worlds = worldsFor(inst);
  worlds.unshift(newWorld);
  saveState();
  renderWorlds();
  showToastStatus(`Đã nhập thế giới "${newWorld.name}" thành công!`);
}

async function backupAllWorlds() {
  const inst = currentInstance();
  if (!inst || worldBackupBusy) return;
  const worlds = [...worldsFor(inst)];
  if (worlds.length === 0) {
    showToastStatus("Không có world nào để backup.");
    return;
  }

  for (let i = 0; i < worlds.length; i++) {
    await backupWorld(worlds[i]);
  }
  if ($("#worldsStatus")) {
    $("#worldsStatus").textContent = `Đã sao lưu xong ${worlds.length} world.`;
  }
  showToastStatus("Đã sao lưu xong tất cả thế giới!");
}

function initWorldsManager() {
  const importBtn = $("#importWorldBtn");
  const fileInput = $("#worldFileInput");
  if (importBtn && fileInput) {
    importBtn.addEventListener("click", () => fileInput.click());
    fileInput.addEventListener("change", (e) => {
      const file = e.target.files?.[0];
      if (file) importWorldFile(file);
      e.target.value = "";
    });
  }

  $("#backupAllWorldsBtn")?.addEventListener("click", backupAllWorlds);
  $("#openWorldsFolderBtn")?.addEventListener("click", () => {
    showToastStatus("Đã mở thư mục saves (demo).");
  });

  const searchInput = $("#worldSearch");
  if (searchInput) {
    searchInput.addEventListener("input", (e) => {
      state.worldSearchQuery = (e.target.value || "").trim();
      renderWorlds();
    });
  }

  const filterChips = $("#worldFilterChips");
  if (filterChips) {
    filterChips.addEventListener("click", (e) => {
      const chip = e.target.closest("[data-world-filter]");
      if (!chip) return;
      const filter = chip.dataset.worldFilter;
      state.worldFilterMode = filter;
      filterChips.querySelectorAll(".chip").forEach((c) => {
        c.classList.toggle("is-active", c.dataset.worldFilter === filter);
      });
      renderWorlds();
    });
  }

  const dropZone = $("#worldDropZone");
  if (dropZone) {
    dropZone.addEventListener("dragover", (e) => {
      e.preventDefault();
      dropZone.classList.add("is-dragover");
    });
    dropZone.addEventListener("dragleave", (e) => {
      if (!dropZone.contains(e.relatedTarget)) {
        dropZone.classList.remove("is-dragover");
      }
    });
    dropZone.addEventListener("drop", (e) => {
      e.preventDefault();
      dropZone.classList.remove("is-dragover");
      const file = e.dataTransfer?.files?.[0];
      if (file) importWorldFile(file);
    });
  }

  const worldList = $("#worldList");
  if (worldList) {
    worldList.addEventListener("click", async (e) => {
      const copySeedBtn = e.target.closest("[data-world-copy-seed]");
      if (copySeedBtn) {
        const id = copySeedBtn.dataset.worldCopySeed;
        const w = findWorld(currentInstance(), id);
        if (w?.seed) {
          navigator.clipboard?.writeText(String(w.seed));
          showToastStatus(`Đã copy Seed: ${w.seed}`);
        }
        return;
      }

      const inspectBtn = e.target.closest("[data-world-inspect]");
      if (inspectBtn) {
        const id = inspectBtn.dataset.worldInspect;
        const w = findWorld(currentInstance(), id);
        if (w) openLevelDatInspector(w);
        return;
      }

      const backupBtn = e.target.closest("[data-world-backup]");
      if (backupBtn) {
        const id = backupBtn.dataset.worldBackup;
        await backupWorld(id);
        return;
      }

      const restoreBtn = e.target.closest("[data-world-restore]");
      if (restoreBtn && !restoreBtn.disabled) {
        const id = restoreBtn.dataset.worldRestore;
        openRestoreDialog(id);
        return;
      }

      const renameBtn = e.target.closest("[data-world-rename]");
      if (renameBtn) {
        const id = renameBtn.dataset.worldRename;
        openRenameWorldDialog(id);
        return;
      }

      const duplicateBtn = e.target.closest("[data-world-duplicate]");
      if (duplicateBtn) {
        const id = duplicateBtn.dataset.worldDuplicate;
        duplicateWorld(id);
        return;
      }

      const exportBtn = e.target.closest("[data-world-export]");
      if (exportBtn) {
        const id = exportBtn.dataset.worldExport;
        exportWorld(id);
        return;
      }

      const deleteBtn = e.target.closest("[data-world-delete]");
      if (deleteBtn) {
        const id = deleteBtn.dataset.worldDelete;
        await deleteWorld(id);
        return;
      }
    });
  }
}

/* ------------------------------------------------------------
   DETAIL — screenshots
   ------------------------------------------------------------ */
function screenshotsFor(inst) {
  return (state.screenshots[inst.id] ||= []);
}

function parseScreenshotTime(ss) {
  if (ss.timestamp) return ss.timestamp;
  if (ss.name) {
    const m = ss.name.match(/^(\d{4})-(\d{2})-(\d{2})_(\d{2})\.(\d{2})\.(\d{2})/);
    if (m) {
      return new Date(`${m[1]}-${m[2]}-${m[3]}T${m[4]}:${m[5]}:${m[6]}`).getTime();
    }
  }
  if (ss.date) {
    const parts = ss.date.match(/(\d{1,2})\/(\d{1,2})\/(\d{4}),?\s*(\d{1,2}):(\d{2})/);
    if (parts) {
      return new Date(`${parts[3]}-${parts[2].padStart(2, "0")}-${parts[1].padStart(2, "0")}T${parts[4].padStart(2, "0")}:${parts[5]}:00`).getTime();
    }
  }
  return 0;
}

function getFilteredScreenshots(inst) {
  const all = screenshotsFor(inst);
  let list = all.slice();
  const q = (state.screenshotSearch || "").trim().toLowerCase();
  if (q) {
    list = list.filter((ss) => {
      const name = (ss.name || "").toLowerCase();
      const title = (ss.title || "").toLowerCase();
      const world = (ss.world || "").toLowerCase();
      return name.includes(q) || title.includes(q) || world.includes(q);
    });
  }

  const sort = state.screenshotSort || "newest";
  list.sort((a, b) => {
    const tA = parseScreenshotTime(a);
    const tB = parseScreenshotTime(b);
    return sort === "oldest" ? tA - tB : tB - tA;
  });

  return list;
}

let currentLightboxList = [];
let currentLightboxIdx = 0;

function renderScreenshots() {
  const inst = currentInstance();
  if (!inst) return;
  const allList = screenshotsFor(inst);
  const filteredList = getFilteredScreenshots(inst);

  const summaryEl = $("#screenshotsSummary");
  if (summaryEl) {
    if (allList.length === 0) {
      summaryEl.textContent = "0 ảnh chụp màn hình";
    } else if (state.screenshotSearch && filteredList.length !== allList.length) {
      summaryEl.textContent = `${filteredList.length}/${allList.length} ảnh chụp màn hình`;
    } else {
      summaryEl.textContent = `${filteredList.length} ảnh chụp màn hình`;
    }
  }

  const gallery = $("#screenshotGallery");
  const empty = $("#screenshotsEmpty");

  if (empty) empty.hidden = filteredList.length > 0;
  if (gallery) {
    gallery.hidden = filteredList.length === 0;
    if (filteredList.length === 0) {
      gallery.innerHTML = "";
      return;
    }

    gallery.innerHTML = filteredList.map((ss, idx) => `
      <div class="screenshot-card" data-ss-id="${esc(ss.id)}" data-ss-idx="${idx}" tabindex="0" role="button" aria-label="${esc(ss.title || ss.name)}">
        <div class="screenshot-thumb-box">
          <img class="screenshot-thumb-img" src="${ss.url}" alt="${esc(ss.title || ss.name)}" loading="lazy" />
          <div class="screenshot-thumb-overlay">
            <div class="screenshot-overlay-actions">
              <button class="screenshot-action-btn screenshot-zoom-btn" data-action="zoom" title="Phóng to ảnh (Toàn màn hình)" aria-label="Phóng to">
                <span class="glyph">&#xE71E;</span>
              </button>
              <button class="screenshot-action-btn screenshot-copy-btn" data-action="copy" title="Sao chép tên file" aria-label="Sao chép tên file">
                <span class="glyph">&#xE8C8;</span>
              </button>
              <button class="screenshot-action-btn screenshot-delete-btn" data-action="delete" title="Xóa ảnh này" aria-label="Xóa ảnh">
                <span class="glyph">&#xE74D;</span>
              </button>
            </div>
          </div>
        </div>
        <div class="screenshot-info-box">
          <div class="screenshot-title" title="${esc(ss.title || ss.name)}">${esc(ss.title || ss.name)}</div>
          <div class="screenshot-filename" title="${esc(ss.name)}">${esc(ss.name)}</div>
          <div class="screenshot-meta">
            <span class="screenshot-meta-left">
              <span class="screenshot-world-tag"><span class="glyph">&#xE81E;</span>${esc(ss.world || "World")}</span>
              <span class="screenshot-res-tag">${ss.width || 1920} × ${ss.height || 1080}</span>
            </span>
            <span class="screenshot-date">${esc(ss.date)}</span>
          </div>
        </div>
      </div>
    `).join("");
  }
}

function updateLightboxUI() {
  const ss = currentLightboxList[currentLightboxIdx];
  if (!ss) return;
  const host = $("#lightboxHost");
  if (!host) return;
  $("#lightboxImg").src = ss.url;
  $("#lightboxTitle").textContent = ss.title || ss.name;
  $("#lightboxWorldName").textContent = ss.world || "World";
  $("#lightboxDate").textContent = ss.date;
  $("#lightboxDimensions").textContent = `${ss.width || 1920} × ${ss.height || 1080}`;
  const counterEl = $("#lightboxCounter");
  if (counterEl) {
    counterEl.textContent = `${currentLightboxIdx + 1} / ${currentLightboxList.length}`;
  }
  const prevBtn = $("#lightboxPrevBtn");
  const nextBtn = $("#lightboxNextBtn");
  if (prevBtn) prevBtn.disabled = currentLightboxList.length <= 1;
  if (nextBtn) nextBtn.disabled = currentLightboxList.length <= 1;
}

function openLightbox(target, maybeIdx) {
  const inst = currentInstance();
  if (!inst) return;
  const list = getFilteredScreenshots(inst);
  if (!list.length) return;
  currentLightboxList = list;
  if (typeof target === "number") {
    currentLightboxIdx = Math.max(0, Math.min(target, list.length - 1));
  } else if (typeof maybeIdx === "number") {
    currentLightboxIdx = Math.max(0, Math.min(maybeIdx, list.length - 1));
  } else if (target && target.id) {
    const f = list.findIndex((s) => s.id === target.id);
    currentLightboxIdx = f >= 0 ? f : 0;
  } else {
    currentLightboxIdx = 0;
  }
  updateLightboxUI();
  const host = $("#lightboxHost");
  if (host) {
    host.hidden = false;
  }
  document.addEventListener("keydown", handleLightboxKeyDown);
}

function closeLightbox() {
  const host = $("#lightboxHost");
  if (host) host.hidden = true;
  const img = $("#lightboxImg");
  if (img) img.src = "";
  document.removeEventListener("keydown", handleLightboxKeyDown);
}

function lightboxPrev() {
  if (currentLightboxList.length <= 1) return;
  currentLightboxIdx = (currentLightboxIdx - 1 + currentLightboxList.length) % currentLightboxList.length;
  updateLightboxUI();
}

function lightboxNext() {
  if (currentLightboxList.length <= 1) return;
  currentLightboxIdx = (currentLightboxIdx + 1) % currentLightboxList.length;
  updateLightboxUI();
}

function handleLightboxKeyDown(e) {
  const dialogHost = $("#dialogHost");
  if (dialogHost && !dialogHost.hidden) return;
  if (e.key === "Escape") {
    e.stopPropagation();
    closeLightbox();
  } else if (e.key === "ArrowLeft") {
    e.preventDefault();
    lightboxPrev();
  } else if (e.key === "ArrowRight") {
    e.preventDefault();
    lightboxNext();
  }
}

async function deleteScreenshot(ss) {
  if (!ss) return;
  const ok = await confirmDialog("Xóa ảnh chụp màn hình?", `Bạn có chắc chắn muốn xóa ảnh "${ss.title || ss.name}"? Hành động này không thể hoàn tác.`, "Xóa");
  if (!ok) return;
  const inst = currentInstance();
  if (!inst) return;
  const list = screenshotsFor(inst);
  const idx = list.findIndex((item) => item.id === ss.id);
  if (idx !== -1) {
    list.splice(idx, 1);
    saveState();
    showToastStatus("Đã xóa ảnh chụp màn hình");
    const host = $("#lightboxHost");
    if (host && !host.hidden) {
      const remaining = getFilteredScreenshots(inst);
      if (remaining.length === 0) {
        closeLightbox();
      } else {
        currentLightboxList = remaining;
        if (currentLightboxIdx >= remaining.length) {
          currentLightboxIdx = remaining.length - 1;
        }
        updateLightboxUI();
      }
    }
    renderScreenshots();
  }
}

function takeDemoScreenshot() {
  const inst = currentInstance();
  if (!inst) return;
  const now = new Date();
  const pad = (n) => String(n).padStart(2, "0");
  const dStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}_${pad(now.getHours())}.${pad(now.getMinutes())}.${pad(now.getSeconds())}`;
  const dateFormatted = `${pad(now.getDate())}/${pad(now.getMonth() + 1)}/${now.getFullYear()}, ${pad(now.getHours())}:${pad(now.getMinutes())}`;
  const palettes = [
    { bg1: "#064e3b", bg2: "#022c22", acc: "#34d399", title: "Rừng già cổ thụ", svg: `<circle cx="640" cy="300" r="90" fill="#fef08a" opacity="0.9"/><polygon points="200,600 640,200 1080,600" fill="#064e3b"/><polygon points="400,600 640,260 880,600" fill="#047857"/>` },
    { bg1: "#1e1b4b", bg2: "#0f172a", acc: "#38bdf8", title: "Cung điện băng tuyết", svg: `<polygon points="300,580 640,160 980,580" fill="#bae6fd"/><polygon points="450,580 640,220 830,580" fill="#e0f2fe"/><circle cx="640" cy="400" r="16" fill="#38bdf8"/>` },
    { bg1: "#450a0a", bg2: "#1c0505", acc: "#f97316", title: "Pháo đài dung nham", svg: `<rect x="440" y="260" width="400" height="340" fill="#292524" stroke="#ea580c" stroke-width="8"/><rect x="520" y="340" width="80" height="120" fill="#f97316"/><rect x="680" y="340" width="80" height="120" fill="#f97316"/>` },
    { bg1: "#3b0764", bg2: "#1e1b4b", acc: "#c084fc", title: "Thành phố The End", svg: `<rect x="560" y="220" width="160" height="380" fill="#581c87"/><circle cx="640" cy="180" r="30" fill="#d8b4fe"/><polygon points="500,320 640,220 780,320" fill="#7e22ce"/>` },
  ];
  const p = palettes[Math.floor(Math.random() * palettes.length)];
  const newSs = {
    id: "ss-" + Date.now(),
    name: `${dStr}.png`,
    title: p.title,
    date: dateFormatted,
    timestamp: Date.now(),
    world: inst.name || "World",
    width: 1920,
    height: 1080,
    url: makeSvgScreenshot(p.bg1, p.bg2, p.acc, p.svg, `${p.title} · ${inst.name}`),
  };
  const list = screenshotsFor(inst);
  list.unshift(newSs);
  saveState();
  renderScreenshots();
  showToastStatus("Đã chụp ảnh màn hình mới (F2)");
}

function handleScreenshotFiles(files) {
  const inst = currentInstance();
  if (!inst || !files || !files.length) return;
  const list = screenshotsFor(inst);
  Array.from(files).forEach((file) => {
    if (!file.type.startsWith("image/")) return;
    const reader = new FileReader();
    reader.onload = (e) => {
      const url = e.target.result;
      const now = new Date();
      const pad = (n) => String(n).padStart(2, "0");
      const dateFormatted = `${pad(now.getDate())}/${pad(now.getMonth() + 1)}/${now.getFullYear()}, ${pad(now.getHours())}:${pad(now.getMinutes())}`;
      const newSs = {
        id: "ss-" + Date.now() + "-" + Math.random().toString(36).slice(2, 6),
        name: file.name,
        title: file.name.replace(/\.[^/.]+$/, ""),
        date: dateFormatted,
        timestamp: Date.now(),
        world: "World",
        width: 1920,
        height: 1080,
        url,
      };
      list.unshift(newSs);
      saveState();
      renderScreenshots();
      showToastStatus(`Đã thêm ảnh "${file.name}" vào screenshots`);
    };
    reader.readAsDataURL(file);
  });
}

function initScreenshotsGallery() {
  const gallery = $("#screenshotGallery");
  const tabScreenshots = $("#tab-screenshots");
  const dropZone = $("#screenshotDropZone");

  // Gallery Click
  gallery?.addEventListener("click", (e) => {
    const card = e.target.closest(".screenshot-card");
    if (!card) return;
    const idx = Number(card.dataset.ssIdx);
    const list = getFilteredScreenshots(currentInstance());
    const ss = list[idx];
    if (!ss) return;

    const actionBtn = e.target.closest("button[data-action]");
    const action = actionBtn?.dataset.action;

    if (action === "copy") {
      e.stopPropagation();
      navigator.clipboard?.writeText(ss.name);
      showToastStatus(`Đã sao chép tên file: ${ss.name}`);
      return;
    }

    if (action === "delete") {
      e.stopPropagation();
      deleteScreenshot(ss);
      return;
    }

    // Default or zoom button: open lightbox
    openLightbox(idx);
  });

  // Gallery keyboard Enter/Space
  gallery?.addEventListener("keydown", (e) => {
    if (e.key === "Enter" || e.key === " ") {
      const card = e.target.closest(".screenshot-card");
      if (card && e.target === card) {
        e.preventDefault();
        const idx = Number(card.dataset.ssIdx);
        openLightbox(idx);
      }
    }
  });

  // Toolbar buttons
  $("#takeDemoScreenshotBtn")?.addEventListener("click", () => {
    takeDemoScreenshot();
  });

  const fileInput = $("#screenshotFileInput");
  $("#importScreenshotBtn")?.addEventListener("click", () => {
    fileInput?.click();
  });

  fileInput?.addEventListener("change", (e) => {
    if (e.target.files && e.target.files.length) {
      handleScreenshotFiles(e.target.files);
      e.target.value = "";
    }
  });

  $("#openScreenshotsFolderBtn")?.addEventListener("click", () => {
    showToastStatus("Đã mở thư mục screenshots (demo): .minecraft/screenshots");
  });

  // Search & Sort
  const searchInput = $("#screenshotSearch");
  searchInput?.addEventListener("input", (e) => {
    state.screenshotSearch = e.target.value;
    renderScreenshots();
  });

  const sortSelect = $("#screenshotSort");
  sortSelect?.addEventListener("change", (e) => {
    state.screenshotSort = e.target.value;
    renderScreenshots();
  });

  // Drag and Drop
  if (tabScreenshots) {
    tabScreenshots.addEventListener("dragenter", (e) => {
      e.preventDefault();
      if (dropZone) {
        dropZone.hidden = false;
        dropZone.classList.add("is-dragover");
      }
    });

    tabScreenshots.addEventListener("dragover", (e) => {
      e.preventDefault();
      if (dropZone) dropZone.classList.add("is-dragover");
    });

    tabScreenshots.addEventListener("dragleave", (e) => {
      if (!e.relatedTarget || !tabScreenshots.contains(e.relatedTarget)) {
        if (dropZone) {
          dropZone.classList.remove("is-dragover");
          dropZone.hidden = true;
        }
      }
    });

    tabScreenshots.addEventListener("drop", (e) => {
      e.preventDefault();
      if (dropZone) {
        dropZone.classList.remove("is-dragover");
        dropZone.hidden = true;
      }
      if (e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files.length) {
        handleScreenshotFiles(e.dataTransfer.files);
      }
    });
  }

  // Lightbox Buttons
  $("#lightboxCloseBtn")?.addEventListener("click", closeLightbox);
  $("#lightboxScrim")?.addEventListener("click", closeLightbox);
  $("#lightboxPrevBtn")?.addEventListener("click", lightboxPrev);
  $("#lightboxNextBtn")?.addEventListener("click", lightboxNext);

  $("#lightboxCopyBtn")?.addEventListener("click", () => {
    const ss = currentLightboxList[currentLightboxIdx];
    if (ss) {
      navigator.clipboard?.writeText(ss.name);
      showToastStatus(`Đã sao chép tên file: ${ss.name}`);
    }
  });

  $("#lightboxSetCoverBtn")?.addEventListener("click", () => {
    const inst = currentInstance();
    const ss = currentLightboxList[currentLightboxIdx];
    if (inst && ss) {
      inst.cover = ss.url;
      inst.icon = ss.url;
      saveState();
      renderInstances();
      const detailIcon = $("#detailBlockIcon");
      if (detailIcon) {
        detailIcon.innerHTML = getInstanceBlockIcon(inst.loader, "mc-block-icon", 28, inst.icon);
      }
      showToastStatus("Đã đặt làm ảnh bìa instance");
    }
  });

  $("#lightboxDownloadBtn")?.addEventListener("click", () => {
    const ss = currentLightboxList[currentLightboxIdx];
    if (ss) {
      const a = document.createElement("a");
      a.href = ss.url;
      a.download = ss.name || "screenshot.png";
      document.body.appendChild(a);
      a.click();
      a.remove();
      showToastStatus("Đã lưu ảnh về máy");
    }
  });

  $("#lightboxDeleteBtn")?.addEventListener("click", () => {
    const ss = currentLightboxList[currentLightboxIdx];
    if (ss) {
      deleteScreenshot(ss);
    }
  });
}

/* ------------------------------------------------------------
   CRASH ANALYZER & 1-CLICK QUICK FIX
   ------------------------------------------------------------ */
function analyzeInstanceCrash(instId) {
  const logs = state.logs[instId] || [];
  const text = logs.join("\n");

  if (text.includes("OutOfMemoryError") || text.includes("Java heap space")) {
    const inst = getInstance(instId);
    const curRam = inst?.settings?.ramMb || DEFAULT_SETTINGS.ramMb;
    const targetRam = Math.min(16384, Math.max(4096, Math.ceil((curRam + 2048) / 512) * 512));
    return {
      type: "oom",
      title: "Tràn bộ nhớ RAM (OutOfMemoryError)",
      badge: "Tràn RAM",
      desc: `Minecraft đã hết bộ nhớ heap Java trong quá trình xử lý. Instance hiện tại được cấp <strong>${curRam} MB RAM</strong>, không đủ để nạp thế giới và modpack.`,
      snippet: "[Render thread/ERROR]: java.lang.OutOfMemoryError: Java heap space",
      actionLabel: `Tự động nâng RAM lên ${targetRam >= 1024 ? (targetRam / 1024).toFixed(1).replace('.0', '') + 'GB' : targetRam + 'MB'}`,
      fix: () => {
        if (inst) {
          inst.settings = inst.settings || {};
          inst.settings.ramMb = targetRam;
          if ($("#setRam")) $("#setRam").value = String(targetRam);
          if ($("#setRamValue")) $("#setRamValue").value = String(targetRam);
          pushLog(instId, `[${now()}] [Launcher/INFO]: [Quick Fix] Đã tự động nâng RAM instance lên ${targetRam} MB.`);
          saveState();
          showToastStatus(`[Quick Fix] Đã nâng RAM lên ${targetRam} MB! Bạn có thể khởi chạy lại.`);
          renderCrashAnalyzer(instId, true);
        }
      },
    };
  }

  if (text.includes("UnsupportedClassVersionError") || text.includes("class file version 65.0") || text.includes("Java 8")) {
    const inst = getInstance(instId);
    return {
      type: "java",
      title: "Không tương thích Java Runtime",
      badge: "Sai Java",
      desc: `Minecraft ${inst?.mc || "1.21.1"} được biên dịch cho Java 21 LTS (class version 65.0), nhưng instance đang chạy với môi trường Java cũ hơn (Java 8).`,
      snippet: "[main/ERROR]: java.lang.UnsupportedClassVersionError: class file version 65.0, only recognizes up to 52.0 (Java 8)",
      actionLabel: "Đổi sang Java 21 LTS",
      fix: () => {
        if (inst) {
          inst.settings = inst.settings || {};
          inst.settings.javaPath = "C:\\Program Files\\Java\\jdk-21\\bin\\javaw.exe";
          if ($("#setJava")) $("#setJava").value = inst.settings.javaPath;
          pushLog(instId, `[${now()}] [Launcher/INFO]: [Quick Fix] Đã cấu hình Java Runtime thành Java 21 LTS.`);
          saveState();
          showToastStatus("[Quick Fix] Đã chuyển sang Java 21 LTS! Bạn có thể khởi chạy lại.");
          renderCrashAnalyzer(instId, true);
        }
      },
    };
  }

  if (text.includes("requires fabric-api") || text.includes("missing dependency") || text.includes("Requires Fabric API") || text.includes("Missing or incompatible")) {
    const inst = getInstance(instId);
    return {
      type: "dependency",
      title: "Thiếu Mod Phụ Thuộc (Missing Dependency)",
      badge: "Thiếu Dependency",
      desc: `Mod trong instance yêu cầu thư viện nền tảng <strong>Fabric API</strong> để hoạt động, nhưng chưa được cài đặt trong thư mục mods.`,
      snippet: "[main/ERROR]: net.fabricmc.loader.impl.FormattedException: Mod 'sodium' requires fabric-api (missing)",
      actionLabel: "Tự động cài đặt Fabric API",
      fix: () => {
        if (inst) {
          state.mods[instId] = state.mods[instId] || [];
          let apiMod = state.mods[instId].find((m) => m.file?.toLowerCase().includes("fabric-api"));
          if (!apiMod) {
            apiMod = {
              id: "mod-fabric-api-" + Date.now(),
              projectId: "P7dR8mSH",
              name: "Fabric API",
              file: "fabric-api-0.92.0+1.21.1.jar",
              version: "0.92.0+1.21.1",
              size: "2.1 MB",
              enabled: true,
              essential: true,
            };
            state.mods[instId].unshift(apiMod);
          } else {
            apiMod.enabled = true;
          }
          pushLog(instId, `[${now()}] [Launcher/INFO]: [Quick Fix] Đã tự động cài đặt và kích hoạt Fabric API.`);
          saveState();
          renderMods();
          showToastStatus("[Quick Fix] Đã cài đặt Fabric API! Bạn có thể khởi chạy lại.");
          renderCrashAnalyzer(instId, true);
        }
      },
    };
  }

  if (text.includes("MixinTransformerError") || text.includes("MixinApplyError") || text.includes("Mixin prepare failed")) {
    const inst = getInstance(instId);
    const mods = state.mods[instId] || [];
    const conflictMod = mods.find((m) => m.enabled && (m.file.includes("lithium") || m.file.includes("cloth-config") || m.file.includes("sodium"))) || mods.find((m) => m.enabled);
    return {
      type: "mixin",
      title: "Xung đột Mixin giữa các Mod",
      badge: "Xung đột Mixin",
      desc: `Lỗi MixinTransformerError xảy ra do can thiệp bytecode xung đột nhau. Mod gây lỗi: <strong>${esc(conflictMod?.file || "Mod xung đột")}</strong>.`,
      snippet: "[main/ERROR]: org.spongepowered.asm.mixin.transformer.throwables.MixinTransformerError: Mixin apply failed",
      actionLabel: `Tắt mod "${conflictMod?.file?.replace(/\.jar$/, "") || "xung đột"}"`,
      fix: () => {
        if (conflictMod) {
          conflictMod.enabled = false;
          pushLog(instId, `[${now()}] [Launcher/INFO]: [Quick Fix] Đã tắt mod xung đột: ${conflictMod.file}.`);
          saveState();
          renderMods();
          showToastStatus(`[Quick Fix] Đã tắt mod ${conflictMod.file}!`);
          renderCrashAnalyzer(instId, true);
        }
      },
    };
  }

  return null;
}

function renderCrashAnalyzer(instId, justFixed = false) {
  const banner = $("#crashAnalyzerBanner");
  if (!banner) return;
  if (justFixed) {
    banner.hidden = false;
    banner.classList.add("is-fixed");
    banner.innerHTML = `
      <div class="crash-analyzer-header">
        <div class="crash-analyzer-badge-group">
          <span class="crash-analyzer-tag"><span class="glyph">&#xE73E;</span> Đã sửa lỗi</span>
          <strong class="crash-analyzer-title">Đã áp dụng Quick Fix thành công!</strong>
        </div>
        <button class="btn icon-only small" id="crashBannerCloseBtn" title="Đóng">&#xE711;</button>
      </div>
      <div class="crash-analyzer-body">
        Cấu hình đã được tối ưu hoá tự động. Bạn có thể bấm <strong>Launch</strong> để khởi động lại game.
      </div>
    `;
    $("#crashBannerCloseBtn")?.addEventListener("click", () => { banner.hidden = true; });
    return;
  }

  const analysis = analyzeInstanceCrash(instId);
  const inst = getInstance(instId);
  if (inst && inst.crashDismissed && !justFixed) {
    banner.hidden = true;
    banner.style.display = "none";
    return;
  }
  if (!analysis) {
    banner.hidden = true;
    banner.style.display = "none";
    banner.innerHTML = "";
    banner.classList.remove("is-fixed");
    return;
  }

  banner.hidden = false;
  banner.style.display = "";
  banner.classList.remove("is-fixed");
  banner.innerHTML = `
    <div class="crash-analyzer-header">
      <div class="crash-analyzer-badge-group">
        <span class="crash-analyzer-tag"><span class="glyph">&#xE7BA;</span> ${analysis.badge}</span>
        <strong class="crash-analyzer-title">${analysis.title}</strong>
      </div>
      <button class="btn icon-only small" id="crashBannerCloseBtn" title="Đóng banner">&#xE711;</button>
    </div>
    <div class="crash-analyzer-body">
      <p>${analysis.desc}</p>
      <div class="crash-analyzer-snippet">${esc(analysis.snippet)}</div>
    </div>
    <div class="crash-analyzer-actions">
      <button class="btn accent crash-fix-btn" id="crashQuickFixBtn">
        <span class="glyph">&#xE73E;</span><span>${analysis.actionLabel}</span>
      </button>
      <button class="btn ghost small crash-copy-btn" id="crashCopyReportBtn">
        <span class="glyph">&#xE8C8;</span><span>Sao chép báo cáo</span>
      </button>
      <button class="btn ghost small" id="crashIgnoreBtn">Bỏ qua</button>
    </div>
  `;

  $("#crashQuickFixBtn")?.addEventListener("click", () => {
    analysis.fix();
  });
  $("#crashCopyReportBtn")?.addEventListener("click", () => {
    const reportText = [
      `=== V.A.Z Crash Diagnostic Report ===`,
      `Time: ${new Date().toISOString()}`,
      `Instance: ${inst?.name || "Unknown"} (${inst?.mc || "1.21.1"} · ${inst?.loader || "Vanilla"})`,
      `Crash Type: ${analysis.badge} (${analysis.type})`,
      `Error Summary: ${analysis.title}`,
      `Log Snippet: ${analysis.snippet}`,
      `Suggested Fix: ${analysis.actionLabel}`,
      `RAM Allocation: ${inst?.settings?.ramMb || DEFAULT_SETTINGS.ramMb} MB`,
      `Java Path: ${inst?.settings?.javaPath || "Default"}`,
      `====================================`,
    ].join("\n");
    try {
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(reportText).catch(() => {});
      }
    } catch (_) {}
    showToastStatus("Đã sao chép báo cáo chẩn đoán vào clipboard!");
  });
  $("#crashBannerCloseBtn")?.addEventListener("click", () => {
    if (inst) inst.crashDismissed = true;
    banner.hidden = true;
    banner.style.display = "none";
  });
  $("#crashIgnoreBtn")?.addEventListener("click", () => {
    if (inst) inst.crashDismissed = true;
    banner.hidden = true;
    banner.style.display = "none";
  });
}



/* ------------------------------------------------------------
   DETAIL — settings
   ------------------------------------------------------------ */
let activeRecordingKeyId = null;
let activeKeybindCleanup = null;
let currentMcOptionsView = "visual";

function ensureInstanceMcOptions(inst) {
  if (!inst) return { ...DEFAULT_MC_OPTIONS };
  if (!inst.mcOptions || typeof inst.mcOptions !== "object") {
    inst.mcOptions = { ...DEFAULT_MC_OPTIONS };
  } else {
    inst.mcOptions = { ...DEFAULT_MC_OPTIONS, ...inst.mcOptions };
  }
  if (inst.id) {
    state.instances[inst.id] = inst;
  }
  return inst.mcOptions;
}

function parseOptionsTxt(rawText) {
  const result = {};
  if (!rawText) return result;
  const lines = rawText.split(/\r?\n/);
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) continue;
    const colonIdx = trimmed.indexOf(":");
    if (colonIdx > 0) {
      const key = trimmed.slice(0, colonIdx).trim();
      const val = trimmed.slice(colonIdx + 1).trim();
      result[key] = val;
    }
  }
  return result;
}

function serializeOptionsTxt(opts) {
  const source = opts || DEFAULT_MC_OPTIONS;
  const standardKeys = [
    "version", "autoJump", "entityShadows", "enableVsync", "fov", "gamma",
    "graphicsMode", "guiScale", "maxFps", "mouseSensitivity", "invertYMouse",
    "renderClouds", "renderDistance", "simulationDistance", "sneakToggle",
    "sprintToggle", "soundCategory_master", "soundCategory_music",
    "soundCategory_weather", "soundCategory_block", "soundCategory_hostile",
    "soundCategory_player", "soundCategory_ambient",
    "key_key.forward", "key_key.left", "key_key.back", "key_key.right",
    "key_key.jump", "key_key.sneak", "key_key.sprint", "key_key.attack",
    "key_key.use", "key_key.inventory", "key_key.drop", "key_key.chat"
  ];
  const lines = [];
  const emitted = new Set();

  for (const k of standardKeys) {
    if (k in source) {
      lines.push(`${k}:${source[k]}`);
      emitted.add(k);
    } else if (k in DEFAULT_MC_OPTIONS) {
      lines.push(`${k}:${DEFAULT_MC_OPTIONS[k]}`);
      emitted.add(k);
    }
  }

  // Preserve all unknown/custom keys
  for (const [k, v] of Object.entries(source)) {
    if (!emitted.has(k)) {
      lines.push(`${k}:${v}`);
      emitted.add(k);
    }
  }

  return lines.join("\n");
}

function formatMcKeyLabel(mcKey) {
  if (!mcKey) return "Chưa gán";
  if (mcKey === "key.mouse.left") return "Chuột trái";
  if (mcKey === "key.mouse.right") return "Chuột phải";
  if (mcKey === "key.mouse.middle") return "Chuột giữa";
  if (mcKey.startsWith("key.keyboard.")) {
    const raw = mcKey.replace("key.keyboard.", "");
    const map = {
      "left.shift": "L-Shift",
      "right.shift": "R-Shift",
      "left.control": "L-Ctrl",
      "right.control": "R-Ctrl",
      "left.alt": "L-Alt",
      "right.alt": "R-Alt",
      "space": "Space",
      "enter": "Enter",
      "tab": "Tab",
      "backspace": "Backspace",
      "caps.lock": "Caps Lock",
      "escape": "Esc",
    };
    return map[raw] || raw.toUpperCase();
  }
  return mcKey.toUpperCase();
}

function eventToMcKey(e) {
  if (e.code === "Space") return { key: "key.keyboard.space", label: "Space" };
  if (e.code === "ShiftLeft") return { key: "key.keyboard.left.shift", label: "L-Shift" };
  if (e.code === "ShiftRight") return { key: "key.keyboard.right.shift", label: "R-Shift" };
  if (e.code === "ControlLeft") return { key: "key.keyboard.left.control", label: "L-Ctrl" };
  if (e.code === "ControlRight") return { key: "key.keyboard.right.control", label: "R-Ctrl" };
  if (e.code === "AltLeft") return { key: "key.keyboard.left.alt", label: "L-Alt" };
  if (e.code === "AltRight") return { key: "key.keyboard.right.alt", label: "R-Alt" };
  if (e.code.startsWith("Key")) {
    const letter = e.code.slice(3).toLowerCase();
    return { key: `key.keyboard.${letter}`, label: letter.toUpperCase() };
  }
  if (e.code.startsWith("Digit")) {
    const digit = e.code.slice(5);
    return { key: `key.keyboard.${digit}`, label: digit };
  }
  const codeMap = {
    Tab: { key: "key.keyboard.tab", label: "Tab" },
    Enter: { key: "key.keyboard.enter", label: "Enter" },
    Backspace: { key: "key.keyboard.backspace", label: "Backspace" },
    CapsLock: { key: "key.keyboard.caps.lock", label: "Caps Lock" },
  };
  if (codeMap[e.code]) return codeMap[e.code];
  const simple = (e.key || "unknown").toLowerCase();
  return { key: `key.keyboard.${simple}`, label: (e.key || "?").toUpperCase() };
}

function renderMcKeybinds() {
  const grid = $("#mcKeybindsGrid");
  if (!grid) return;
  const inst = currentInstance();
  const opts = ensureInstanceMcOptions(inst);

  grid.innerHTML = MC_KEYBINDS.map((item) => {
    const curKey = opts[item.id] || item.defaultKey;
    const label = formatMcKeyLabel(curKey);
    const isRecording = activeRecordingKeyId === item.id;
    return `
      <div class="mc-keybind-item" data-key-id="${item.id}">
        <span class="mc-keybind-label">${esc(item.label)}</span>
        <button type="button" class="btn small mc-keybind-btn ${isRecording ? "is-recording" : ""}" data-key-id="${item.id}" title="Click để ghi phím mới">
          <span>${isRecording ? "Nhấn phím..." : esc(label)}</span>
        </button>
      </div>`;
  }).join("");
}

function applyRawOptionsToVisual(inst, rawText) {
  if (!inst) return;
  const parsed = parseOptionsTxt(rawText);
  inst.mcOptions = { ...DEFAULT_MC_OPTIONS, ...parsed };
  syncMcVisualControlsFromOptions(inst.mcOptions);
}

function syncMcVisualControlsFromOptions(opts) {
  if (!opts) return;

  // Graphics
  const rDist = parseInt(opts.renderDistance, 10) || 12;
  if ($("#mcOptRenderDistance")) $("#mcOptRenderDistance").value = rDist;
  if ($("#mcValRenderDistance")) $("#mcValRenderDistance").textContent = `${rDist} chunks`;

  const sDist = parseInt(opts.simulationDistance, 10) || 12;
  if ($("#mcOptSimDistance")) $("#mcOptSimDistance").value = sDist;
  if ($("#mcValSimDistance")) $("#mcValSimDistance").textContent = `${sDist} chunks`;

  const fps = parseInt(opts.maxFps, 10) || 120;
  if ($("#mcOptMaxFps")) $("#mcOptMaxFps").value = fps;
  if ($("#mcValMaxFps")) $("#mcValMaxFps").textContent = fps >= 260 ? "Vô hạn (Unlimited)" : `${fps} FPS`;

  if ($("#mcOptGraphicsMode")) $("#mcOptGraphicsMode").value = opts.graphicsMode || "fancy";
  if ($("#mcOptClouds")) $("#mcOptClouds").value = opts.renderClouds === "true" ? "fancy" : (opts.renderClouds || "fancy");
  if ($("#mcOptGuiScale")) $("#mcOptGuiScale").value = opts.guiScale || "0";

  const brightPct = Math.round(parseNum(opts.gamma, 0.5) * 100);
  if ($("#mcOptBrightness")) $("#mcOptBrightness").value = brightPct;
  if ($("#mcValBrightness")) $("#mcValBrightness").textContent = `${brightPct}%`;

  if ($("#mcOptVsync")) $("#mcOptVsync").checked = opts.enableVsync === "true" || opts.enableVsync === true;
  if ($("#mcOptEntityShadows")) $("#mcOptEntityShadows").checked = opts.entityShadows !== "false" && opts.entityShadows !== false;

  // Audio
  const masterPct = Math.round(parseNum(opts.soundCategory_master, 1.0) * 100);
  if ($("#mcOptSoundMaster")) $("#mcOptSoundMaster").value = masterPct;
  if ($("#mcValSoundMaster")) $("#mcValSoundMaster").textContent = `${masterPct}%`;

  const musicPct = Math.round(parseNum(opts.soundCategory_music, 0.5) * 100);
  if ($("#mcOptSoundMusic")) $("#mcOptSoundMusic").value = musicPct;
  if ($("#mcValSoundMusic")) $("#mcValSoundMusic").textContent = `${musicPct}%`;

  const weatherPct = Math.round(parseNum(opts.soundCategory_weather, 1.0) * 100);
  if ($("#mcOptSoundWeather")) $("#mcOptSoundWeather").value = weatherPct;
  if ($("#mcValSoundWeather")) $("#mcValSoundWeather").textContent = `${weatherPct}%`;

  const blockPct = Math.round(parseNum(opts.soundCategory_block, 1.0) * 100);
  if ($("#mcOptSoundBlocks")) $("#mcOptSoundBlocks").value = blockPct;
  if ($("#mcValSoundBlocks")) $("#mcValSoundBlocks").textContent = `${blockPct}%`;

  const hostilesPct = Math.round(parseNum(opts.soundCategory_hostile, 1.0) * 100);
  if ($("#mcOptSoundHostiles")) $("#mcOptSoundHostiles").value = hostilesPct;
  if ($("#mcValSoundHostiles")) $("#mcValSoundHostiles").textContent = `${hostilesPct}%`;

  const playersPct = Math.round(parseNum(opts.soundCategory_player, 1.0) * 100);
  if ($("#mcOptSoundPlayers")) $("#mcOptSoundPlayers").value = playersPct;
  if ($("#mcValSoundPlayers")) $("#mcValSoundPlayers").textContent = `${playersPct}%`;

  const ambientPct = Math.round(parseNum(opts.soundCategory_ambient, 1.0) * 100);
  if ($("#mcOptSoundAmbient")) $("#mcOptSoundAmbient").value = ambientPct;
  if ($("#mcValSoundAmbient")) $("#mcValSoundAmbient").textContent = `${ambientPct}%`;

  // Controls
  const sensPct = Math.round(parseNum(opts.mouseSensitivity, 0.5) * 200);
  if ($("#mcOptSensitivity")) $("#mcOptSensitivity").value = sensPct;
  if ($("#mcValSensitivity")) $("#mcValSensitivity").textContent = `${sensPct}%`;

  const fovVal = Math.round(parseNum(opts.fov, 70));
  if ($("#mcOptFov")) $("#mcOptFov").value = fovVal;
  if ($("#mcValFov")) $("#mcValFov").textContent = fovVal >= 110 ? "110° (Quake Pro)" : `${fovVal}°`;

  if ($("#mcOptSneakMode")) $("#mcOptSneakMode").value = opts.sneakToggle === "true" || opts.sneakToggle === true ? "true" : "false";
  if ($("#mcOptSprintMode")) $("#mcOptSprintMode").value = opts.sprintToggle === "true" || opts.sprintToggle === true ? "true" : "false";
  if ($("#mcOptInvertMouse")) $("#mcOptInvertMouse").checked = opts.invertYMouse === "true" || opts.invertYMouse === true;
  if ($("#mcOptAutoJump")) $("#mcOptAutoJump").checked = opts.autoJump === "true" || opts.autoJump === true;

  // Keybinds
  renderMcKeybinds();
}

function renderMcOptions() {
  const inst = currentInstance();
  if (!inst) return;
  const opts = ensureInstanceMcOptions(inst);

  syncMcVisualControlsFromOptions(opts);

  if ($("#mcRawTextarea")) {
    $("#mcRawTextarea").value = serializeOptionsTxt(opts);
  }

  switchMcOptionsView(currentMcOptionsView, false);
}

function switchMcOptionsView(mode, notify = true) {
  currentMcOptionsView = mode;
  const isVisual = mode === "visual";

  if ($("#mcOptionsVisual")) $("#mcOptionsVisual").hidden = !isVisual;
  if ($("#mcOptionsRaw")) $("#mcOptionsRaw").hidden = isVisual;

  if ($("#mcViewVisualBtn")) {
    $("#mcViewVisualBtn").classList.toggle("is-selected", isVisual);
    $("#mcViewVisualBtn").setAttribute("aria-selected", isVisual ? "true" : "false");
  }
  if ($("#mcViewRawBtn")) {
    $("#mcViewRawBtn").classList.toggle("is-selected", !isVisual);
    $("#mcViewRawBtn").setAttribute("aria-selected", !isVisual ? "true" : "false");
  }

  const inst = currentInstance();
  if (!inst) return;
  const opts = ensureInstanceMcOptions(inst);

  if (!isVisual) {
    if ($("#mcRawTextarea")) $("#mcRawTextarea").value = serializeOptionsTxt(opts);
  } else {
    if ($("#mcRawTextarea")) {
      applyRawOptionsToVisual(inst, $("#mcRawTextarea").value);
    }
  }
}

function applyMcPreset(presetKey) {
  const inst = currentInstance();
  if (!inst) return;
  ensureInstanceMcOptions(inst);

  const preset = MC_PRESETS[presetKey];
  if (!preset) return;

  Object.assign(inst.mcOptions, preset.values);
  saveState();
  syncMcVisualControlsFromOptions(inst.mcOptions);

  if ($("#mcRawTextarea")) {
    $("#mcRawTextarea").value = serializeOptionsTxt(inst.mcOptions);
  }

  showToastStatus(`Đã áp dụng preset "${preset.name}".`);
}

function exportOptionsTxt() {
  const inst = currentInstance();
  if (!inst) return;
  const opts = ensureInstanceMcOptions(inst);
  const text = serializeOptionsTxt(opts);

  const blob = new Blob([text], { type: "text/plain;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "options.txt";
  document.body.appendChild(a);
  a.click();
  document.body.removeChild(a);
  URL.revokeObjectURL(url);

  showToastStatus("Đã xuất file options.txt thành công.");
}

function startKeybindRecording(keyId) {
  stopKeybindRecording();
  activeRecordingKeyId = keyId;
  renderMcKeybinds();

  const onKey = (e) => {
    e.preventDefault();
    e.stopPropagation();

    if (e.key === "Escape") {
      stopKeybindRecording();
      return;
    }

    const mapped = eventToMcKey(e);
    const inst = currentInstance();
    if (inst && activeRecordingKeyId) {
      ensureInstanceMcOptions(inst);
      inst.mcOptions[activeRecordingKeyId] = mapped.key;
      saveState();
      if ($("#mcRawTextarea")) $("#mcRawTextarea").value = serializeOptionsTxt(inst.mcOptions);
      showToastStatus(`Đã gán phím [${mapped.label}].`);
    }

    stopKeybindRecording();
  };

  const onPointerDown = (e) => {
    if (e.target.closest(".mc-keybind-btn")) return;
    stopKeybindRecording();
  };

  const onContext = (e) => {
    e.preventDefault();
    e.stopPropagation();
    const inst = currentInstance();
    if (inst && activeRecordingKeyId) {
      ensureInstanceMcOptions(inst);
      inst.mcOptions[activeRecordingKeyId] = "key.mouse.right";
      saveState();
      if ($("#mcRawTextarea")) $("#mcRawTextarea").value = serializeOptionsTxt(inst.mcOptions);
      showToastStatus("Đã gán [Chuột phải].");
    }
    stopKeybindRecording();
  };

  activeKeybindCleanup = () => {
    window.removeEventListener("keydown", onKey, true);
    window.removeEventListener("pointerdown", onPointerDown, true);
    window.removeEventListener("contextmenu", onContext, true);
    activeKeybindCleanup = null;
  };

  window.addEventListener("keydown", onKey, true);
  window.addEventListener("pointerdown", onPointerDown, true);
  window.addEventListener("contextmenu", onContext, true);
}

function stopKeybindRecording() {
  if (activeKeybindCleanup) {
    activeKeybindCleanup();
  }
  activeRecordingKeyId = null;
  renderMcKeybinds();
}

function wireMcOptionsEvents() {
  // Presets
  $("#mcPresetFps")?.addEventListener("click", () => applyMcPreset("fps"));
  $("#mcPresetBalanced")?.addEventListener("click", () => applyMcPreset("balanced"));
  $("#mcPresetHigh")?.addEventListener("click", () => applyMcPreset("high"));

  // View switch
  $("#mcViewVisualBtn")?.addEventListener("click", () => switchMcOptionsView("visual"));
  $("#mcViewRawBtn")?.addEventListener("click", () => switchMcOptionsView("raw"));

  // Export
  $("#mcExportBtn")?.addEventListener("click", exportOptionsTxt);

  const updateOpt = (key, val, debounceSave = false) => {
    const inst = currentInstance();
    if (!inst) return;
    ensureInstanceMcOptions(inst);
    inst.mcOptions[key] = String(val);
    if (debounceSave) {
      debouncedSaveState();
    } else {
      saveState();
    }
    if ($("#mcRawTextarea")) $("#mcRawTextarea").value = serializeOptionsTxt(inst.mcOptions);
  };

  // Graphics
  $("#mcOptRenderDistance")?.addEventListener("input", (e) => {
    const val = e.target.value;
    if ($("#mcValRenderDistance")) $("#mcValRenderDistance").textContent = `${val} chunks`;
    updateOpt("renderDistance", val, true);
  });
  $("#mcOptSimDistance")?.addEventListener("input", (e) => {
    const val = e.target.value;
    if ($("#mcValSimDistance")) $("#mcValSimDistance").textContent = `${val} chunks`;
    updateOpt("simulationDistance", val, true);
  });
  $("#mcOptMaxFps")?.addEventListener("input", (e) => {
    const val = parseInt(e.target.value, 10);
    if ($("#mcValMaxFps")) $("#mcValMaxFps").textContent = val >= 260 ? "Vô hạn (Unlimited)" : `${val} FPS`;
    updateOpt("maxFps", val, true);
  });
  $("#mcOptGraphicsMode")?.addEventListener("change", (e) => {
    updateOpt("graphicsMode", e.target.value);
  });
  $("#mcOptClouds")?.addEventListener("change", (e) => {
    updateOpt("renderClouds", e.target.value);
  });
  $("#mcOptGuiScale")?.addEventListener("change", (e) => {
    updateOpt("guiScale", e.target.value);
  });
  $("#mcOptBrightness")?.addEventListener("input", (e) => {
    const val = e.target.value;
    if ($("#mcValBrightness")) $("#mcValBrightness").textContent = `${val}%`;
    updateOpt("gamma", (val / 100).toFixed(2), true);
  });
  $("#mcOptVsync")?.addEventListener("change", (e) => {
    updateOpt("enableVsync", e.target.checked ? "true" : "false");
  });
  $("#mcOptEntityShadows")?.addEventListener("change", (e) => {
    updateOpt("entityShadows", e.target.checked ? "true" : "false");
  });

  // Audio
  const wireAudio = (id, labelId, optKey) => {
    $(id)?.addEventListener("input", (e) => {
      const val = e.target.value;
      if ($(labelId)) $(labelId).textContent = `${val}%`;
      updateOpt(optKey, (val / 100).toFixed(2), true);
    });
  };
  wireAudio("#mcOptSoundMaster", "#mcValSoundMaster", "soundCategory_master");
  wireAudio("#mcOptSoundMusic", "#mcValSoundMusic", "soundCategory_music");
  wireAudio("#mcOptSoundWeather", "#mcValSoundWeather", "soundCategory_weather");
  wireAudio("#mcOptSoundBlocks", "#mcValSoundBlocks", "soundCategory_block");
  wireAudio("#mcOptSoundHostiles", "#mcValSoundHostiles", "soundCategory_hostile");
  wireAudio("#mcOptSoundPlayers", "#mcValSoundPlayers", "soundCategory_player");
  wireAudio("#mcOptSoundAmbient", "#mcValSoundAmbient", "soundCategory_ambient");

  // Controls
  $("#mcOptSensitivity")?.addEventListener("input", (e) => {
    const val = e.target.value;
    if ($("#mcValSensitivity")) $("#mcValSensitivity").textContent = `${val}%`;
    updateOpt("mouseSensitivity", (val / 200).toFixed(2), true);
  });
  $("#mcOptFov")?.addEventListener("input", (e) => {
    const val = parseInt(e.target.value, 10);
    if ($("#mcValFov")) $("#mcValFov").textContent = val >= 110 ? "110° (Quake Pro)" : `${val}°`;
    updateOpt("fov", val.toFixed(1), true);
  });
  $("#mcOptSneakMode")?.addEventListener("change", (e) => {
    updateOpt("sneakToggle", e.target.value);
  });
  $("#mcOptSprintMode")?.addEventListener("change", (e) => {
    updateOpt("sprintToggle", e.target.value);
  });
  $("#mcOptInvertMouse")?.addEventListener("change", (e) => {
    updateOpt("invertYMouse", e.target.checked ? "true" : "false");
  });
  $("#mcOptAutoJump")?.addEventListener("change", (e) => {
    updateOpt("autoJump", e.target.checked ? "true" : "false");
  });

  // Keybinds grid
  $("#mcKeybindsGrid")?.addEventListener("click", (e) => {
    const btn = e.target.closest(".mc-keybind-btn");
    if (!btn) return;
    const keyId = btn.dataset.keyId;
    if (!keyId) return;
    if (activeRecordingKeyId === keyId) {
      stopKeybindRecording();
    } else {
      startKeybindRecording(keyId);
    }
  });

  // Raw textarea input
  $("#mcRawTextarea")?.addEventListener("input", (e) => {
    const inst = currentInstance();
    if (!inst) return;
    ensureInstanceMcOptions(inst);
    applyRawOptionsToVisual(inst, e.target.value);
    debouncedSaveState();
  });
}

function probeJavaPath(path) {
  if (!path || !path.trim()) {
    return { ok: false, error: "Vui lòng nhập đường dẫn javaw.exe." };
  }
  const clean = path.trim().toLowerCase();
  if (
    clean.includes("invalid") ||
    clean.includes("notfound") ||
    clean.includes("fake") ||
    clean.includes("error") ||
    (!clean.includes("java") && !clean.includes("jdk") && !clean.includes("jre"))
  ) {
    return { ok: false, error: "Đường dẫn không hợp lệ hoặc không tìm thấy javaw.exe" };
  }

  if (clean.includes("21")) {
    return { ok: true, version: 21, info: "Hợp lệ: Java 21.0.4 64-bit (Eclipse Temurin)" };
  }
  if (clean.includes("17")) {
    return { ok: true, version: 17, info: "Hợp lệ: Java 17.0.12 64-bit (Microsoft Build of OpenJDK)" };
  }
  if (clean.includes("8") || clean.includes("1.8")) {
    return { ok: true, version: 8, info: "Hợp lệ: Java 8u421 64-bit (Oracle JRE)" };
  }
  return { ok: true, version: 21, info: "Hợp lệ: Java 21.0.0 64-bit (Custom OpenJDK)" };
}

function matchGcPreset(jvmArgs) {
  if (!jvmArgs) return "g1gc";
  const str = jvmArgs.trim();
  if (str.includes("-XX:+UseZGC")) return "zgc";
  if (str.includes("-XX:+UseShenandoahGC")) return "shenandoah";
  if (str.includes("-XX:+UseG1GC")) return "g1gc";
  return "custom";
}

function updateSysRamGauge(maxRam) {
  const totalRam = TOTAL_SYS_RAM; // 16384 MB
  const osRam = OS_RESERVED_RAM; // 4096 MB
  const mcRam = clamp(maxRam, 512, totalRam);
  const osPercent = (osRam / totalRam) * 100; // 25%
  const mcPercent = Math.min(100 - osPercent, (mcRam / totalRam) * 100);
  const freePercent = Math.max(0, 100 - osPercent - mcPercent);
  const freeRam = Math.max(0, totalRam - osRam - mcRam);

  const ramSegOs = $("#ramSegOs");
  const ramSegMc = $("#ramSegMc");
  const ramSegFree = $("#ramSegFree");
  if (ramSegOs) ramSegOs.style.width = `${osPercent}%`;
  if (ramSegMc) ramSegMc.style.width = `${mcPercent}%`;
  if (ramSegFree) ramSegFree.style.width = `${freePercent}%`;

  const legendMcRam = $("#legendMcRam");
  const legendMcPercent = $("#legendMcPercent");
  const legendFreeRam = $("#legendFreeRam");
  const legendFreePercent = $("#legendFreePercent");
  const sysRamSummary = $("#sysRamSummary");

  if (legendMcRam) legendMcRam.textContent = `${mcRam.toLocaleString()} MB`;
  if (legendMcPercent) legendMcPercent.textContent = `${Math.round(mcPercent)}%`;
  if (legendFreeRam) legendFreeRam.textContent = `${freeRam.toLocaleString()} MB`;
  if (legendFreePercent) legendFreePercent.textContent = `${Math.round(freePercent)}%`;
  if (sysRamSummary) sysRamSummary.textContent = `Minecraft: ${mcRam.toLocaleString()} MB (${Math.round(mcPercent)}%)`;

  const sysRamGauge = $("#sysRamGauge");
  if (sysRamGauge) {
    sysRamGauge.setAttribute("aria-valuenow", String(mcRam));
  }
}

function updateRamSafetyWarning(maxRam) {
  const banner = $("#ramWarningBanner");
  const text = $("#ramWarningText");
  if (!banner || !text) return;

  if (maxRam > 13926) {
    banner.hidden = false;
    banner.className = "infobar warning ram-warning-banner";
    text.textContent = `Cảnh báo: Cấp phát quá 85% RAM hệ thống (${(maxRam / 1024).toFixed(1)} GB / 16 GB) có thể khiến Windows bị treo hoặc ứng dụng khác bị crash do thiếu bộ nhớ!`;
  } else if (maxRam < 1024) {
    banner.hidden = false;
    banner.className = "infobar error ram-warning-banner";
    text.textContent = `Cảnh báo: Cấp phát RAM dưới 1024 MB (${maxRam} MB) quá thấp, Minecraft có thể không thể khởi chạy hoặc gặp lỗi OutOfMemory!`;
  } else {
    banner.hidden = true;
  }
}

function syncRamChips(maxRam) {
  $$(".ram-preset-chip").forEach((chip) => {
    chip.classList.toggle("is-selected", Number(chip.dataset.ram) === Number(maxRam));
  });
}

function renderJavaResolver(inst) {
  const recVer = getRecommendedJavaVersion(inst.mc || inst.mcVersion);
  let curPath = inst.settings.javaPath || "";
  let curVer = inst.settings.javaVersion;

  // Tìm runtime khớp theo path hoặc version
  let activeRuntime = SYSTEM_JAVA_RUNTIMES.find(
    (r) => (curPath && r.path.toLowerCase() === curPath.toLowerCase()) || r.majorVersion === curVer
  );

  if (!activeRuntime && !curPath) {
    activeRuntime = SYSTEM_JAVA_RUNTIMES.find((r) => r.majorVersion === recVer) || SYSTEM_JAVA_RUNTIMES[0];
    curPath = activeRuntime.path;
    curVer = activeRuntime.majorVersion;
    inst.settings.javaPath = curPath;
    inst.settings.javaVersion = curVer;
  }

  const selectedRuntimeId = activeRuntime ? activeRuntime.id : "custom";
  const selectedMajorVer = activeRuntime ? activeRuntime.majorVersion : probeJavaPath(curPath).version;

  // Selector dropdown
  const select = $("#javaRuntimeSelect");
  if (select) {
    select.value = selectedRuntimeId;
  }

  // Cards
  const cardsContainer = $("#javaRuntimeCards");
  if (cardsContainer) {
    cardsContainer.innerHTML = SYSTEM_JAVA_RUNTIMES.map((rt) => {
      const isRec = rt.majorVersion === recVer;
      const isSel = rt.id === selectedRuntimeId;
      return `
        <div class="java-runtime-card${isSel ? " is-selected" : ""}" data-runtime-id="${rt.id}" data-version="${rt.majorVersion}" role="radio" aria-checked="${isSel}" tabindex="0">
          <div class="java-card-top">
            <span class="java-card-title">${esc(rt.name)} (${esc(rt.fullName)})</span>
            ${isRec ? `<span class="badge badge-recommended java-rec-badge">Khuyến nghị</span>` : ""}
          </div>
          <div class="java-card-meta">${esc(rt.arch)} · ${rt.isJdk ? "JDK" : "JRE"} · ${esc(rt.vendor)}</div>
          <div class="java-card-path">${esc(rt.path)}</div>
        </div>
      `;
    }).join("");
  }

  // Warning banner
  const warnBanner = $("#javaIncompatibleBanner");
  const warnText = $("#javaIncompatibleText");
  if (warnBanner && warnText) {
    if (selectedMajorVer && selectedMajorVer !== recVer) {
      warnBanner.hidden = false;
      warnText.textContent = `Minecraft ${inst.mc || "1.21.1"} khuyến nghị Java ${recVer}, nhưng runtime hiện tại là Java ${selectedMajorVer}.`;
    } else {
      warnBanner.hidden = true;
    }
  }

  // Custom path inputs
  const customInput = $("#setJavaCustomPath");
  if (customInput) customInput.value = curPath;
  const legacyJavaInput = $("#setJava");
  if (legacyJavaInput) legacyJavaInput.value = curPath;

  const probeResult = $("#javaProbeResult");
  if (probeResult) probeResult.hidden = true;
}

function renderRamAllocation(inst) {
  const maxRam = clamp(parseInt(inst.settings.ramMb, 10) || DEFAULT_SETTINGS.ramMb, 512, 16384);
  const minRam = clamp(parseInt(inst.settings.minRamMb, 10) || DEFAULT_SETTINGS.minRamMb, 512, maxRam);

  inst.settings.ramMb = maxRam;
  inst.settings.minRamMb = minRam;

  const setMaxRam = $("#setMaxRam");
  const setMaxRamValue = $("#setMaxRamValue");
  const setRam = $("#setRam");
  const setRamValue = $("#setRamValue");

  if (setMaxRam) setMaxRam.value = maxRam;
  if (setMaxRamValue) setMaxRamValue.value = maxRam;
  if (setRam) setRam.value = maxRam;
  if (setRamValue) setRamValue.value = maxRam;

  const setMinRam = $("#setMinRam");
  const setMinRamValue = $("#setMinRamValue");
  if (setMinRam) setMinRam.value = minRam;
  if (setMinRamValue) setMinRamValue.value = minRam;

  syncRamChips(maxRam);
  updateSysRamGauge(maxRam);
  updateRamSafetyWarning(maxRam);
}

function renderJvmAndGc(inst) {
  const jvm = inst.settings.jvmArgs !== undefined ? inst.settings.jvmArgs : GC_PRESETS.g1gc;
  inst.settings.jvmArgs = jvm;

  const setJvm = $("#setJvm");
  if (setJvm) setJvm.value = jvm;

  const activeGc = inst.settings.gcPreset || matchGcPreset(jvm);
  inst.settings.gcPreset = activeGc;

  $$(".gc-preset-chip").forEach((chip) => {
    chip.classList.toggle("is-selected", chip.dataset.gc === activeGc);
  });
}

function renderSettings() {
  const inst = currentInstance();
  if (!inst) return;

  const recVer = getRecommendedJavaVersion(inst.mc || inst.mcVersion);
  const recRuntime = SYSTEM_JAVA_RUNTIMES.find((r) => r.majorVersion === recVer) || SYSTEM_JAVA_RUNTIMES[0];

  inst.settings = {
    ...DEFAULT_SETTINGS,
    javaVersion: recVer,
    javaPath: recRuntime.path,
    ...(inst.settings || {}),
  };
  const s = inst.settings;

  renderJavaResolver(inst);
  renderRamAllocation(inst);
  renderJvmAndGc(inst);

  if ($("#setWidth")) $("#setWidth").value = s.width || 854;
  if ($("#setHeight")) $("#setHeight").value = s.height || 480;
  if ($("#settingsFolder")) $("#settingsFolder").value = inst.path || "";
  if ($("#settingsStatus")) $("#settingsStatus").textContent = "";

  renderMcOptions();
}

function saveSettings() {
  const inst = currentInstance();
  if (!inst) return;

  const maxVal = parseInt($("#setMaxRamValue")?.value || $("#setRamValue")?.value, 10);
  const ramMb = clamp(maxVal || DEFAULT_SETTINGS.ramMb, 512, 16384);
  const minVal = parseInt($("#setMinRamValue")?.value, 10);
  const minRamMb = clamp(minVal || DEFAULT_SETTINGS.minRamMb, 512, ramMb);
  const width = clamp(parseInt($("#setWidth")?.value, 10) || DEFAULT_SETTINGS.width, 320, 7680);
  const height = clamp(parseInt($("#setHeight")?.value, 10) || DEFAULT_SETTINGS.height, 240, 4320);

  const selRtId = $("#javaRuntimeSelect")?.value || "java-21";
  let javaVersion = 21;
  let javaPath = $("#setJavaCustomPath")?.value.trim() || $("#setJava")?.value.trim() || "";

  if (selRtId !== "custom") {
    const rt = SYSTEM_JAVA_RUNTIMES.find((r) => r.id === selRtId);
    if (rt) {
      javaVersion = rt.majorVersion;
      javaPath = rt.path;
    }
  } else {
    const probe = probeJavaPath(javaPath);
    if (probe.ok && probe.version) javaVersion = probe.version;
  }

  const selectedGcChip = $(".gc-preset-chip.is-selected");
  const gcPreset = selectedGcChip?.dataset.gc || "custom";

  inst.settings = {
    minRamMb,
    ramMb,
    jvmArgs: $("#setJvm")?.value.trim() || "",
    javaPath,
    javaVersion,
    gcPreset,
    width,
    height,
  };

  ensureInstanceMcOptions(inst);
  saveState();
  renderSettings();
  if ($("#settingsStatus")) $("#settingsStatus").textContent = "Đã lưu cài đặt.";
  showToastStatus("Đã lưu cài đặt instance.");
}

function resetSettings() {
  const inst = currentInstance();
  if (!inst) return;

  const recVer = getRecommendedJavaVersion(inst.mc || inst.mcVersion);
  const recRt = SYSTEM_JAVA_RUNTIMES.find((r) => r.majorVersion === recVer) || SYSTEM_JAVA_RUNTIMES[0];

  inst.settings = {
    ...DEFAULT_SETTINGS,
    minRamMb: 1024,
    ramMb: 4096,
    jvmArgs: GC_PRESETS.g1gc,
    javaPath: recRt.path,
    javaVersion: recVer,
    gcPreset: "g1gc",
    width: 854,
    height: 480,
  };
  inst.mcOptions = { ...DEFAULT_MC_OPTIONS };
  saveState();
  renderSettings();
  if ($("#settingsStatus")) $("#settingsStatus").textContent = "Đã khôi phục mặc định.";
  showToastStatus("Đã khôi phục cài đặt mặc định.");
}

function wireSettingsEvents() {
  // Max RAM Slider & Input
  const handleMaxRamChange = (val) => {
    const clamped = clamp(parseInt(val, 10) || 4096, 512, 16384);
    if ($("#setMaxRam")) $("#setMaxRam").value = clamped;
    if ($("#setMaxRamValue")) $("#setMaxRamValue").value = clamped;
    if ($("#setRam")) $("#setRam").value = clamped;
    if ($("#setRamValue")) $("#setRamValue").value = clamped;

    const minVal = parseInt($("#setMinRamValue")?.value, 10) || 1024;
    if (minVal > clamped) {
      if ($("#setMinRam")) $("#setMinRam").value = clamped;
      if ($("#setMinRamValue")) $("#setMinRamValue").value = clamped;
    }

    updateSysRamGauge(clamped);
    updateRamSafetyWarning(clamped);
    syncRamChips(clamped);
  };

  $("#setMaxRam")?.addEventListener("input", (e) => handleMaxRamChange(e.target.value));
  $("#setMaxRamValue")?.addEventListener("input", (e) => handleMaxRamChange(e.target.value));
  $("#setRam")?.addEventListener("input", (e) => handleMaxRamChange(e.target.value));
  $("#setRamValue")?.addEventListener("input", (e) => handleMaxRamChange(e.target.value));

  // Min RAM Slider & Input
  const handleMinRamChange = (val) => {
    const maxVal = parseInt($("#setMaxRamValue")?.value, 10) || 4096;
    const clamped = clamp(parseInt(val, 10) || 1024, 512, 16384);
    if ($("#setMinRam")) $("#setMinRam").value = clamped;
    if ($("#setMinRamValue")) $("#setMinRamValue").value = clamped;

    if (clamped > maxVal) {
      handleMaxRamChange(clamped);
    }
  };

  $("#setMinRam")?.addEventListener("input", (e) => handleMinRamChange(e.target.value));
  $("#setMinRamValue")?.addEventListener("input", (e) => handleMinRamChange(e.target.value));

  // RAM Presets chips
  document.addEventListener("click", (e) => {
    const chip = e.target.closest(".ram-preset-chip");
    if (!chip) return;
    const ram = parseInt(chip.dataset.ram, 10);
    if (ram) handleMaxRamChange(ram);
  });

  // GC Presets chips
  document.addEventListener("click", (e) => {
    const chip = e.target.closest(".gc-preset-chip");
    if (!chip) return;
    const gc = chip.dataset.gc;
    $$(".gc-preset-chip").forEach((c) => c.classList.toggle("is-selected", c === chip));
    if (gc !== "custom" && GC_PRESETS[gc] !== undefined) {
      if ($("#setJvm")) $("#setJvm").value = GC_PRESETS[gc];
    }
  });

  // JVM arguments input
  $("#setJvm")?.addEventListener("input", (e) => {
    const matched = matchGcPreset(e.target.value);
    $$(".gc-preset-chip").forEach((c) => c.classList.toggle("is-selected", c.dataset.gc === matched));
  });

  // Java Runtime Select dropdown
  $("#javaRuntimeSelect")?.addEventListener("change", (e) => {
    const val = e.target.value;
    const inst = currentInstance();
    const recVer = inst ? getRecommendedJavaVersion(inst.mc || inst.mcVersion) : 21;

    if (val !== "custom") {
      const rt = SYSTEM_JAVA_RUNTIMES.find((r) => r.id === val);
      if (rt) {
        if ($("#setJavaCustomPath")) $("#setJavaCustomPath").value = rt.path;
        if ($("#setJava")) $("#setJava").value = rt.path;

        $$(".java-runtime-card").forEach((card) => {
          const isSel = card.dataset.runtimeId === rt.id;
          card.classList.toggle("is-selected", isSel);
          card.setAttribute("aria-checked", String(isSel));
        });

        const warnBanner = $("#javaIncompatibleBanner");
        const warnText = $("#javaIncompatibleText");
        if (warnBanner && warnText) {
          if (rt.majorVersion !== recVer) {
            warnBanner.hidden = false;
            warnText.textContent = `Minecraft ${inst?.mc || "1.21.1"} khuyến nghị Java ${recVer}, nhưng runtime hiện tại là Java ${rt.majorVersion}.`;
          } else {
            warnBanner.hidden = true;
          }
        }
      }
    } else {
      $$(".java-runtime-card").forEach((card) => {
        card.classList.remove("is-selected");
        card.setAttribute("aria-checked", "false");
      });
      $("#setJavaCustomPath")?.focus();
    }
  });

  // Java Runtime Cards Click
  document.addEventListener("click", (e) => {
    const card = e.target.closest(".java-runtime-card");
    if (!card) return;
    const id = card.dataset.runtimeId;
    if ($("#javaRuntimeSelect")) {
      $("#javaRuntimeSelect").value = id;
      $("#javaRuntimeSelect").dispatchEvent(new Event("change"));
    }
  });

  // Use recommended Java button
  $("#btnUseRecommendedJava")?.addEventListener("click", () => {
    const inst = currentInstance();
    const recVer = inst ? getRecommendedJavaVersion(inst.mc || inst.mcVersion) : 21;
    const recRt = SYSTEM_JAVA_RUNTIMES.find((r) => r.majorVersion === recVer) || SYSTEM_JAVA_RUNTIMES[0];

    if ($("#javaRuntimeSelect")) {
      $("#javaRuntimeSelect").value = recRt.id;
      $("#javaRuntimeSelect").dispatchEvent(new Event("change"));
    }
    showToastStatus(`Đã áp dụng Java ${recVer} khuyến nghị.`);
  });

  // Test Java button
  $("#btnTestJava")?.addEventListener("click", () => {
    const path = $("#setJavaCustomPath")?.value.trim() || $("#setJava")?.value.trim() || "";
    const res = probeJavaPath(path);
    const probeBox = $("#javaProbeResult");
    if (!probeBox) return;

    probeBox.hidden = false;
    if (res.ok) {
      probeBox.className = "java-probe-result caption is-success";
      probeBox.textContent = res.info;
    } else {
      probeBox.className = "java-probe-result caption is-error";
      probeBox.textContent = res.error;
    }
  });

  // 1-Click Download Java button
  $("#btnDownloadJava")?.addEventListener("click", async () => {
    const wrap = $("#javaDownloadProgressWrap");
    const bar = $("#javaDownloadProgress");
    const status = $("#javaDownloadStatus");
    const percent = $("#javaDownloadPercent");
    const btn = $("#btnDownloadJava");

    if (btn) btn.disabled = true;
    if (wrap) wrap.hidden = false;

    const steps = [
      { p: 20, text: "Đang kết nối Eclipse Adoptium API..." },
      { p: 55, text: "Đang tải gói cài đặt Temurin 21.0.4..." },
      { p: 85, text: "Đang giải nén bộ cài JDK 21 x64..." },
      { p: 100, text: "Hoàn tất cài đặt! Sẵn sàng sử dụng." },
    ];

    for (const step of steps) {
      if (bar) bar.style.width = `${step.p}%`;
      if (status) status.textContent = step.text;
      if (percent) percent.textContent = `${step.p}%`;
      await new Promise((r) => setTimeout(r, 60));
    }

    const temurinRt = SYSTEM_JAVA_RUNTIMES[0];
    if ($("#javaRuntimeSelect")) {
      $("#javaRuntimeSelect").value = temurinRt.id;
      $("#javaRuntimeSelect").dispatchEvent(new Event("change"));
    }

    showToastStatus("Đã tải và cài đặt Java 21 (Adoptium Temurin) thành công!");
    if (btn) btn.disabled = false;
    setTimeout(() => {
      if (wrap) wrap.hidden = true;
    }, 1500);
  });

  // Save & Reset buttons
  $("#saveSettingsBtn")?.addEventListener("click", saveSettings);
  $("#resetSettingsBtn")?.addEventListener("click", resetSettings);
  wireMcOptionsEvents();
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

function simulateCrash(scenario = null) {
  let inst = state.instances.find((i) => i.running);
  if (!inst) {
    inst = currentInstance() || state.instances[0];
    if (!inst) {
      showToastStatus("Không tìm thấy instance nào để giả lập crash.");
      return;
    }
  }

  const selectedScenario = scenario || $("#crashScenarioSelect")?.value || "oom";

  if (state.logTimers[inst.id]) {
    clearInterval(state.logTimers[inst.id]);
    delete state.logTimers[inst.id];
  }
  if (inst.sessionStart) {
    const minutes = Math.max(1, Math.round((Date.now() - inst.sessionStart) / 60000));
    inst.playtimeMin = (inst.playtimeMin || 0) + minutes;
    delete inst.sessionStart;
  }
  inst.running = false;
  inst.launching = false;
  inst.pid = null;
  inst.status = "Đã crash (exit code 1)";
  delete inst.crashDismissed;

  const report = `crash-${dateStamp()}-client.txt`;
  const crashTask = createTask("crash", `Crash: ${inst.name}`, `Exit code 1 · ${now()}`);
  finishTask(crashTask, "error", `crash-reports/${report}`);

  if (selectedScenario === "oom") {
    pushLog(inst.id, `[${now()}] [Render thread/INFO]: Allocating memory for vertex array buffers...`);
    pushLog(inst.id, `[${now()}] [Render thread/ERROR]: java.lang.OutOfMemoryError: Java heap space`);
    pushLog(inst.id, `[${now()}] [Render thread/ERROR]: \tat java.base/java.util.Arrays.copyOf(Arrays.java:3537)`);
    pushLog(inst.id, `[${now()}] [Render thread/ERROR]: \tat net.minecraft.client.Minecraft.run(Minecraft.java:1015)`);
    pushLog(inst.id, `[${now()}] [Launcher/ERROR]: Game crashed with exit code 1. Java heap space exhausted.`);
  } else if (selectedScenario === "java") {
    pushLog(inst.id, `[${now()}] [main/INFO]: Validating JVM version: Oracle Java 8 (1.8.0_391)`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: java.lang.UnsupportedClassVersionError: net/minecraft/client/main/Main has been compiled by a more recent version of the Java Runtime (class file version 65.0), this version only recognizes up to 52.0 (Java 8)`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: \tat java.lang.ClassLoader.defineClass1(Native Method)`);
    pushLog(inst.id, `[${now()}] [Launcher/ERROR]: Game crashed with exit code 1. Java 21 LTS is required.`);
  } else if (selectedScenario === "dependency") {
    pushLog(inst.id, `[${now()}] [main/INFO]: Loading Minecraft ${inst.mc || "1.21.1"} with Fabric Loader...`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: net.fabricmc.loader.impl.FormattedException: Some of your mods are missing required dependencies!`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: \t- Mod 'sodium' (0.5.8) requires fabric-api (missing)`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: \tat net.fabricmc.loader.impl.FabricLoaderImpl.setup(FabricLoaderImpl.java:235)`);
    pushLog(inst.id, `[${now()}] [Launcher/ERROR]: Game crashed with exit code 1. Missing dependency: fabric-api.`);
  } else if (selectedScenario === "mixin") {
    pushLog(inst.id, `[${now()}] [main/INFO]: SpongePowered MIXIN is active.`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: org.spongepowered.asm.mixin.transformer.throwables.MixinTransformerError: An unexpected critical error was encountered`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: \tat org.spongepowered.asm.mixin.transformer.MixinProcessor.applyMixins(MixinProcessor.java:392)`);
    pushLog(inst.id, `[${now()}] [main/ERROR]: Caused by: org.spongepowered.asm.mixin.throwables.MixinApplyError: Mixin [sodium.mixins.json:features.render.world.WorldRendererMixin] FAILED during APPLY`);
    pushLog(inst.id, `[${now()}] [Launcher/ERROR]: Game crashed with exit code 1. Mixin transformer conflict detected.`);
  }

  saveState();

  if (state.screen === "instances") renderInstances();
  if (state.screen === "detail" && state.activeInstanceId === inst.id) {
    $("#detailLaunchStatus").textContent = inst.status;
    updateDetailLaunchButton();
    renderLogs();
    renderCrashAnalyzer(inst.id);
  }

  showDialog({
    title: "Crash Analyzer — Phát hiện sự cố",
    primary: "Xem Logs & Quick Fix",
    closeText: "Đóng",
    body: `
      <div class="infobar error">
        <span class="glyph">&#xE7BA;</span>
        <div><strong>${esc(inst.name)} đã thoát bất thường</strong><p>Exit code 1 · Kịch bản: <strong>${esc(selectedScenario.toUpperCase())}</strong></p></div>
      </div>
      <label class="field"><span>Crash report</span>
        <input class="textbox" readonly value="${esc(inst.path)}\\crash-reports\\${report}" />
      </label>
      <p class="caption text-2">Bộ phân tích Crash Analyzer đã xác định nguyên nhân và sẵn sàng nút <strong>1-Click Quick Fix</strong> trong tab Logs.</p>`,
    onPrimary: () => {
      if (state.screen !== "detail" || state.activeInstanceId !== inst.id) {
        openDetail(inst.id);
      }
      setTab("logs");
      return true;
    },
  });
}

/* ------------------------------------------------------------
   ACCOUNTS
   ------------------------------------------------------------ */
function renderAccounts() {
  const list = $("#accountList");
  if (!list) return;

  list.innerHTML = state.accounts.map((acc) => {
    const skin = getSkinPreset(ensureAccountSkin(acc));
    const cape = getCapePreset(acc.capeId || "none");
    const uuidDisplay = acc.uuid
      ? (acc.uuid.length > 14 ? acc.uuid.slice(0, 4) + "..." + acc.uuid.slice(-4) : acc.uuid)
      : "c069...1b4a";
    const tokenDisplay = acc.tokenStatus || (acc.kind === "Microsoft" ? "Token: Hợp lệ (14 ngày)" : "Offline session");

    return `
    <div class="account-card${state.accountHighlight === acc.id ? " just-active" : ""}" data-account="${esc(acc.id)}">
      <div class="account-main-col">
        <div class="avatar" title="Skin ${esc(skin.name)}"><span class="skin-face avatar-face" role="img" aria-label="Mặt skin ${esc(skin.name)}" style="--skin-texture:url('${esc(skin.texture)}')"></span></div>
        <div class="account-details">
          <div class="account-name-row">
            <div class="account-name">${esc(acc.name)}</div>
            ${acc.active ? `<span class="active-mark">✓ Active</span>` : ""}
          </div>
          <div class="account-sub">
            <span class="chip account-badge-kind ${acc.kind.toLowerCase()}">${esc(acc.kind)}</span>
            <span class="account-uuid" title="UUID: ${esc(acc.uuid || '')}">${esc(uuidDisplay)}</span>
            <span class="chip account-token-status" title="Trạng thái phiên">${esc(tokenDisplay)}</span>
            <span class="chip account-cape-chip" title="Áo choàng"><span class="glyph">&#xE790;</span>${esc(cape?.name || "Không có")}</span>
            <span class="chip skin-chip" title="Skin đang dùng"><span class="skin-face" style="--skin-texture:url('${esc(skin.texture)}')"></span>${esc(skin.name)}</span>
          </div>
        </div>
      </div>
      <div class="account-actions">
        <button class="btn" data-acc-action="activate" ${acc.active ? "disabled" : ""}>Set active</button>
        ${acc.kind === "Microsoft" ? `<button class="btn" data-acc-action="refresh" title="Làm mới token xác thực">Refresh</button>` : ""}
        <button class="btn danger" data-acc-action="remove" title="Xóa tài khoản">Xóa</button>
      </div>
    </div>`;
  }).join("");

  const offBtn = $("#addOfflineBtn");
  if (offBtn) offBtn.disabled = false;
  const msBtn = $("#addMicrosoftBtn");
  if (msBtn) msBtn.disabled = false;
}

function accountStatus(text) {
  const el = $("#accountStatus");
  if (el) el.textContent = text;
}

function setActiveAccount(id) {
  state.accounts.forEach((a) => { a.active = a.id === id; });
  state.accountHighlight = id;
  const acc = getAccount(id);
  if (acc) {
    state.skinAccountId = id;
    state.skinSelectedId = ensureAccountSkin(acc);
  }
  saveState();
  renderAccounts();
  updateDock();
  accountStatus(`Đã đặt "${acc ? acc.name : id}" làm tài khoản active.`);
  showToastStatus(`Đã chuyển sang tài khoản active: ${acc ? acc.name : id}`);
  setTimeout(() => {
    state.accountHighlight = null;
  }, 700);
}

function getAccount(id) {
  return state.accounts.find((a) => a.id === id);
}

/* ------------------------------------------------------------
   WAVE 6 & 8 — SKINS (Library, 3D Preview, Capes, Custom Upload, Grab)
   ------------------------------------------------------------ */
function allSkinPresets() {
  return [...SKIN_PRESETS, ...(state.customSkins || [])];
}

function getSkinPreset(id) {
  return allSkinPresets().find((preset) => preset.id === id) || null;
}

function allCapePresets() {
  return [...CAPE_PRESETS, ...(state.customCapes || [])];
}

function getCapePreset(id) {
  return allCapePresets().find((c) => c.id === id) || CAPE_PRESETS[0];
}

function defaultSkinForAccount(acc) {
  return String(acc?.name || "").toLowerCase().includes("alex") ? "alex" : "steve";
}

function ensureAccountSkin(acc) {
  if (!acc) return null;
  if (!getSkinPreset(acc.skinId)) acc.skinId = defaultSkinForAccount(acc);
  if (!acc.capeId) acc.capeId = "none";
  return acc.skinId;
}

function selectedSkinAccount() {
  return state.accounts.find((acc) => acc.id === state.skinAccountId) || null;
}

function skinStatus(text) {
  const el = $("#skinStatus");
  if (el) el.textContent = text;
}

let skinViewer = null;
window.skinViewer = null;
let skinViewerObserver = null;
let skinViewerLoadedId = null;
let skinViewerLoadedCapeId = null;
let skinViewerPendingId = null;
let skinViewerLoadToken = 0;

function skinTextureFor(preset) {
  return preset?.texture || SKIN_PRESETS[0].texture;
}

function skinModelFor(preset) {
  return preset?.model === "Slim" ? "slim" : "default";
}

function skinMotionReduced() {
  return prefersReducedMotion() || document.documentElement.classList.contains("reduce-motion");
}

function setSkinViewerLoading(loading) {
  const loader = $("#skinViewerLoader");
  if (loader) loader.hidden = !loading;
}

function setSkinViewerError(error) {
  const box = $("#skinViewerError");
  if (box) box.hidden = !error;
}

const VANILLA_ELYTRA_BASE64 =
  "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAEAAAAAgBAMAAABQs2O3AAAAIVBMVEUAAAB/j593iJptf5JneIpfb4BYZnVPXGpKVmNGUV1CTFgLTatUAAAAAXRSTlMAQObYZgAAAKFJREFUeNrtzbENwjAQBVArigTpuExAvEG4BQL2ABSYmiaMYG+AU1Jy3sBMCRLQIOunoeQ3V9zT/+orjRCRQhE1F8mSMLiLXBFoniDNAYEgZwkYpBj2ACzzbfIQTCFcTgAsYvRuKL7Wr+PPzm0RGN146Itg9QFH26MGZ5h3qMFYZjhRszWEJmrDG40aqpaIEFCkuzIY3rejVkNQdUTqn1/nAThNI5j05ZvKAAAAAElFTkSuQmCC";

let elytraMaskImage = null;
function getElytraMaskImage() {
  if (!elytraMaskImage) {
    elytraMaskImage = new Image();
    elytraMaskImage.src = VANILLA_ELYTRA_BASE64;
  }
  return elytraMaskImage;
}

function getBuiltinAnimation(animMode) {
  if (!window.skinview3d) return null;
  switch (animMode) {
    case "walk":
      return new window.skinview3d.WalkingAnimation();
    case "fly":
      return new window.skinview3d.FunctionAnimation((player, progress) => {
        if (!player) return;
        player.rotation.x = Math.PI / 2.3;
        player.rotation.y = 0;
        player.position.y = -6;
        player.position.z = 2;

        if (player.skin) {
          player.skin.head.rotation.x = -Math.PI / 3.2;
          player.skin.head.rotation.y = 0;

          player.skin.leftArm.rotation.x = 0.2;
          player.skin.leftArm.rotation.y = 0.05;
          player.skin.leftArm.rotation.z = 0.25;

          player.skin.rightArm.rotation.x = 0.2;
          player.skin.rightArm.rotation.y = -0.05;
          player.skin.rightArm.rotation.z = -0.25;

          player.skin.leftLeg.rotation.set(0, 0, 0);
          player.skin.rightLeg.rotation.set(0, 0, 0);
        }

        if (player.elytra && player.elytra.leftWing) {
          player.elytra.visible = true;
          const flap = Math.sin(progress * 3.5) * 0.04;
          player.elytra.leftWing.rotation.x = 0.32 + flap;
          player.elytra.leftWing.rotation.y = -0.35;
          player.elytra.leftWing.rotation.z = 0.42 + flap * 0.5;
          if (typeof player.elytra.updateRightWing === "function") {
            player.elytra.updateRightWing();
          }
        }
      });
    case "sneak":
      return new window.skinview3d.CrouchAnimation();
    case "wave":
      return new window.skinview3d.WaveAnimation("right");
    case "bow":
      return new window.skinview3d.FunctionAnimation((player, progress) => {
        if (!player?.skin) return;
        player.skin.leftArm.rotation.x = -Math.PI / 2;
        player.skin.leftArm.rotation.y = 0.28;
        player.skin.leftArm.rotation.z = 0.06;
        player.skin.rightArm.rotation.x = -Math.PI / 2;
        player.skin.rightArm.rotation.y = -0.58;
        player.skin.rightArm.rotation.z = -0.06;
        player.skin.body.rotation.y = -0.22;
        player.skin.head.rotation.y = 0.22;
        player.skin.head.rotation.x = -0.04 + Math.sin(progress * 2) * 0.02;
        player.skin.leftLeg.rotation.set(0, 0, 0);
        player.skin.rightLeg.rotation.set(0, 0, 0);
      });
    default:
      return new window.skinview3d.IdleAnimation ? new window.skinview3d.IdleAnimation() : null;
  }
}

function getVanillaElytraDataUrl() {
  return VANILLA_ELYTRA_BASE64;
}

function ensureCapeElytraTexture() {
  if (!skinViewer || !skinViewer.capeCanvas) return;
  const canvas = skinViewer.capeCanvas;
  const ctx = canvas.getContext("2d", { willReadFrequently: true });
  if (!ctx) return;
  try {
    const imgData = ctx.getImageData(22, 0, 24, 22);
    let opaqueCount = 0;
    for (let i = 3; i < imgData.data.length; i += 4) {
      if (imgData.data[i] > 20) opaqueCount++;
    }
    // If texture already has authentic Minecraft Elytra wings (official Mojang capes or vanilla elytra), preserve exactly as designed
    if (opaqueCount >= 40) {
      return;
    }

    // For custom uploaded capes that lack pre-baked Elytra UVs, map the cape onto the Elytra cutout
    const mask = getElytraMaskImage();
    const applyMask = () => {
      try {
        ctx.drawImage(canvas, 1, 1, 10, 16, 24, 2, 10, 20);
        ctx.drawImage(canvas, 12, 1, 10, 16, 36, 2, 10, 20);
      } catch {}

      const wingCanvas = document.createElement("canvas");
      wingCanvas.width = 24;
      wingCanvas.height = 22;
      const wctx = wingCanvas.getContext("2d");
      if (wctx) {
        wctx.drawImage(canvas, 22, 0, 24, 22, 0, 0, 24, 22);
        wctx.globalCompositeOperation = "destination-in";
        wctx.drawImage(mask, 22, 0, 24, 22, 0, 0, 24, 22);

        ctx.clearRect(22, 0, 24, 22);
        ctx.drawImage(wingCanvas, 22, 0);
      }

      if (typeof skinViewer.recreateCapeTexture === "function") {
        skinViewer.recreateCapeTexture();
      }
    };

    if (mask.complete && mask.naturalWidth > 0) {
      applyMask();
    } else {
      mask.onload = applyMask;
    }
  } catch {}
}

function applySkinViewerAnimation() {
  if (!skinViewer || !window.skinview3d) return;
  const animMode = state.skinAnim || "walk";
  const player = skinViewer.playerObject;
  try {
    if (player) {
      if (typeof player.resetJoints === "function") player.resetJoints();
      player.rotation.set(0, 0, 0);
      player.position.set(0, 0, 0);

      const account = selectedSkinAccount();
      const isPremium = account?.kind === "Microsoft";
      const ownedCapes = isPremium && Array.isArray(account?.capes) ? account.capes : [];
      const hasCapes = isPremium && ownedCapes.length > 0;
      const capeId = hasCapes ? (account?.capeId || "none") : "none";
      const cape = getCapePreset(capeId);

      if (animMode === "fly") {
        const elytraTexture = (hasCapes && cape && cape.file && capeId !== "none") ? cape.file : getVanillaElytraDataUrl();
        if (typeof skinViewer.loadCape === "function") {
          skinViewer.loadCape(elytraTexture, { backEquipment: "elytra" }).then(() => {
            ensureCapeElytraTexture();
            if (skinViewer?.playerObject) {
              skinViewer.playerObject.backEquipment = "elytra";
              if (skinViewer.playerObject.elytra) {
                skinViewer.playerObject.elytra.visible = true;
              }
            }
          }).catch(() => {});
        }
        player.backEquipment = "elytra";
        if (player.elytra) player.elytra.visible = true;
      } else {
        if (!hasCapes || capeId === "none") {
          if (typeof skinViewer.resetCape === "function") {
            skinViewer.resetCape();
          }
          player.backEquipment = null;
        } else {
          syncSkinViewerCape(account);
        }
      }
    }

    if (skinMotionReduced()) {
      skinViewer.animation = null;
    } else {
      skinViewer.animation = getBuiltinAnimation(animMode);
    }
  } catch {
    // fallback
  }

  $$("#skinAnimGroup .stage-btn").forEach((btn) => {
    btn.classList.toggle("is-active", btn.dataset.skinAnim === animMode);
  });
  const autoRotateBtn = $("#skinAutoRotateBtn");
  if (autoRotateBtn) {
    autoRotateBtn.classList.toggle("is-active", !!skinViewer.autoRotate);
  }
}

function syncSkinViewerCape(account) {
  if (!skinViewer) return;
  const isPremium = account?.kind === "Microsoft";
  const ownedCapes = isPremium && Array.isArray(account?.capes) ? account.capes : [];
  const hasCapes = isPremium && ownedCapes.length > 0;
  const capeId = hasCapes ? (account?.capeId || "none") : "none";
  skinViewerLoadedCapeId = capeId;
  const cape = getCapePreset(capeId);
  try {
    if (!hasCapes || !cape || !cape.file || capeId === "none") {
      if (typeof skinViewer.resetCape === "function") {
        skinViewer.resetCape();
      }
    } else {
      if (typeof skinViewer.loadCape === "function") {
        skinViewer.loadCape(cape.file).then(() => {
          ensureCapeElytraTexture();
        }).catch(() => {});
      }
    }
    const player = skinViewer.playerObject;
    if (player && state.skinAnim !== "fly") {
      player.backEquipment = (hasCapes && capeId !== "none") ? "cape" : null;
    }
  } catch {
    // fallback
  }
}

function syncSkinViewerMotion() {
  if (skinViewer) {
    applySkinViewerAnimation();
  }
}

function resizeSkinViewer() {
  const shell = $("#skinViewer")?.closest(".skin-viewer-shell");
  if (!skinViewer || !shell) return;
  const width = shell.clientWidth;
  const height = shell.clientHeight;
  if (width > 0 && height > 0) skinViewer.setSize(width, height);
}

function ensureSkinViewer() {
  if (skinViewer) return skinViewer;

  const canvas = $("#skinViewer");
  const shell = canvas?.closest(".skin-viewer-shell");
  if (!canvas || !shell) return null;

  if (!window.skinview3d?.SkinViewer) {
    setSkinViewerLoading(false);
    setSkinViewerError(true);
    return null;
  }

  const width = shell.clientWidth;
  const height = shell.clientHeight;
  if (width < 80 || height < 80) return null;

  try {
    skinViewer = new window.skinview3d.SkinViewer({
      canvas,
      width,
      height,
      background: null,
      preserveDrawingBuffer: false,
      pixelRatio: "match-device",
    });
    window.skinViewer = skinViewer;
    skinViewer.autoRotate = state.skinAutoRotate ?? true;
    skinViewer.autoRotateSpeed = 1.6;
    applySkinViewerAnimation();
    if (skinViewer.fxaaPass) skinViewer.fxaaPass.enabled = false;
    skinViewer.controls.enablePan = false;
    skinViewer.controls.enableDamping = true;
    skinViewer.controls.dampingFactor = 0.08;
    skinViewer.controls.rotateSpeed = 0.65;
    skinViewer.controls.zoomSpeed = 0.7;
    skinViewer.globalLight.intensity = 2.6;
    skinViewer.cameraLight.intensity = 0.8;
    skinViewer.renderPaused = $("#page-skins")?.hidden !== false;

    if ("ResizeObserver" in window) {
      skinViewerObserver = new ResizeObserver(resizeSkinViewer);
      skinViewerObserver.observe(shell);
    } else {
      window.addEventListener("resize", resizeSkinViewer);
    }
    return skinViewer;
  } catch {
    skinViewer = null;
    setSkinViewerLoading(false);
    setSkinViewerError(true);
    return null;
  }
}

function syncSkinViewer(preset, account) {
  const viewer = ensureSkinViewer();
  if (!viewer) return;

  const capeId = account?.capeId || "none";
  if (skinViewerLoadedCapeId !== capeId) {
    syncSkinViewerCape(account);
  }

  if (skinViewerLoadedId === preset.id || skinViewerPendingId === preset.id) return;

  skinViewerPendingId = preset.id;
  const token = ++skinViewerLoadToken;
  setSkinViewerError(false);
  setSkinViewerLoading(true);

  let loading;
  try {
    loading = viewer.loadSkin(skinTextureFor(preset), { model: skinModelFor(preset) });
  } catch {
    skinViewerPendingId = null;
    setSkinViewerLoading(false);
    setSkinViewerError(true);
    return;
  }

  Promise.resolve(loading)
    .then(() => {
      if (token !== skinViewerLoadToken) return;
      skinViewerLoadedId = preset.id;
      skinViewerPendingId = null;
      setSkinViewerLoading(false);
      setSkinViewerError(false);
      $("#skinViewer")?.classList.add("is-ready");
      syncSkinViewerCape(account);
      applySkinViewerAnimation();
    })
    .catch(() => {
      if (token !== skinViewerLoadToken) return;
      skinViewerPendingId = null;
      setSkinViewerLoading(false);
      setSkinViewerError(true);
    });
}

function toggleSkinModel() {
  const preset = getSkinPreset(state.skinSelectedId);
  if (!preset) return;
  preset.model = preset.model === "Slim" ? "Classic" : "Slim";
  const modelText = $("#skinPreviewModel");
  if (modelText) modelText.textContent = preset.model;

  if (skinViewer) {
    skinViewerLoadedId = null;
    try {
      skinViewer.loadSkin(skinTextureFor(preset), { model: skinModelFor(preset) }).catch(() => {});
    } catch {
      // fallback
    }
  }
  saveState();
  renderSkins();
  skinStatus(`Đã đổi kiểu model sang: ${preset.model}.`);
  showToastStatus(`Model: ${preset.model}`);
}

function renderCapeSelector(account) {
  const box = $("#skinCapeBox");
  const list = $("#skinCapeList");
  const badge = $("#skinCapeBadge");
  if (!list) return;

  const isPremium = account?.kind === "Microsoft";
  const ownedCapes = isPremium && Array.isArray(account?.capes) ? account.capes : [];

  // Offline accounts have no cape; Premium accounts without capes also hide the cape section
  if (!isPremium || ownedCapes.length === 0) {
    if (box) box.hidden = true;
    if (account) account.capeId = "none";
    if (badge) badge.textContent = "Không dùng";
    list.innerHTML = "";
    return;
  }

  if (box) box.hidden = false;

  // Filter available capes to "none", capes owned by this account, and custom capes
  const availableCapes = allCapePresets().filter((c) => {
    return c.id === "none" || ownedCapes.includes(c.id) || !!c.custom;
  });

  const currentCapeId = availableCapes.some((c) => c.id === account?.capeId) ? account.capeId : "none";
  if (account && account.capeId !== currentCapeId) {
    account.capeId = currentCapeId;
  }
  const currentCape = getCapePreset(currentCapeId);
  if (badge) badge.textContent = currentCape.name;

  list.innerHTML = availableCapes.map((cape) => {
    const isSelected = cape.id === currentCapeId;
    const isCustom = !!cape.custom;
    const thumbHtml = cape.thumb
      ? `<span class="skin-cape-thumb"><img src="${esc(cape.thumb)}" alt="${esc(cape.name)}" /></span>`
      : cape.file
      ? `<span class="skin-cape-thumb"><img src="${esc(cape.file)}" alt="${esc(cape.name)}" /></span>`
      : `<span class="skin-cape-thumb is-none glyph">&#xE711;</span>`;
    return `
      <button class="skin-cape-item${isSelected ? " is-selected" : ""}" type="button"
        data-cape-id="${esc(cape.id)}" title="${esc(cape.name)} — ${esc(cape.desc)}" aria-pressed="${isSelected}">
        ${thumbHtml}
        <span class="skin-cape-label">${esc(cape.name)}</span>
        ${isCustom ? `<span class="skin-cape-delete glyph" data-delete-custom-cape="${esc(cape.id)}" title="Xóa cape này" aria-label="Xóa cape">&#xE74D;</span>` : ""}
      </button>`;
  }).join("");
}

function handleCapeUpload(file) {
  if (!file) return;
  const account = selectedSkinAccount();
  if (account && account.kind !== "Microsoft") {
    showToastStatus("Tài khoản Offline không hỗ trợ áo choàng.");
    skinStatus("Lỗi: Tài khoản Offline không có quyền trang bị áo choàng.");
    return;
  }
  if (!file.type.includes("png") && !file.name.toLowerCase().endsWith(".png")) {
    showToastStatus("Lỗi: Chỉ hỗ trợ file cape định dạng PNG.");
    skinStatus("Lỗi: File cape phải là định dạng .PNG.");
    return;
  }
  const reader = new FileReader();
  reader.onload = (e) => {
    const dataUrl = e.target.result;
    const capeName = file.name.replace(/\.png$/i, "").slice(0, 24) || "Custom Cape";
    const img = new Image();
    img.onload = () => {
      let thumbDataUrl = dataUrl;
      try {
        const canvas = document.createElement("canvas");
        canvas.width = 20;
        canvas.height = 32;
        const ctx = canvas.getContext("2d");
        ctx.imageSmoothingEnabled = false;
        if (img.width >= 22 && img.height >= 17) {
          ctx.drawImage(img, 12, 1, 10, 16, 0, 0, 20, 32);
        } else {
          ctx.drawImage(img, 0, 0, 20, 32);
        }
        thumbDataUrl = canvas.toDataURL("image/png");
      } catch {
        thumbDataUrl = dataUrl;
      }

      const newCape = {
        id: "cape-" + Date.now(),
        name: capeName,
        desc: "Áo choàng tùy chỉnh",
        file: dataUrl,
        thumb: thumbDataUrl,
        custom: true,
      };
      state.customCapes = Array.isArray(state.customCapes) ? state.customCapes : [];
      state.customCapes.push(newCape);
      if (account && account.kind === "Microsoft") {
        account.capes = Array.isArray(account.capes) ? account.capes : [];
        if (!account.capes.includes(newCape.id)) account.capes.push(newCape.id);
      }
      saveState();
      selectCape(newCape.id);
      showToastStatus(`Đã thêm cape "${capeName}"`);
      skinStatus(`Đã nạp áo choàng "${capeName}" lên model 3D.`);
    };
    img.onerror = () => {
      showToastStatus("Lỗi: Không thể đọc hình ảnh cape.");
    };
    img.src = dataUrl;
  };
  reader.readAsDataURL(file);
}

function deleteCustomCape(id) {
  state.customCapes = (state.customCapes || []).filter((c) => c.id !== id);
  state.accounts.forEach((acc) => {
    if (acc.capeId === id) acc.capeId = "none";
    if (Array.isArray(acc.capes)) acc.capes = acc.capes.filter((capId) => capId !== id);
  });
  saveState();
  const account = selectedSkinAccount();
  syncSkinViewerCape(account);
  renderCapeSelector(account);
  showToastStatus("Đã xóa cape tùy chỉnh.");
  skinStatus("Đã xóa cape tùy chỉnh.");
}

function selectCape(capeId) {
  const account = selectedSkinAccount();
  if (!account) {
    skinStatus("Chọn tài khoản trước khi chọn áo choàng.");
    return;
  }
  if (account.kind !== "Microsoft") {
    account.capeId = "none";
    saveState();
    syncSkinViewerCape(account);
    renderCapeSelector(account);
    showToastStatus("Tài khoản Offline không hỗ trợ áo choàng.");
    skinStatus("Tài khoản Offline không hỗ trợ áo choàng.");
    return;
  }
  const ownedCapes = Array.isArray(account.capes) ? account.capes : [];
  if (capeId !== "none" && !ownedCapes.includes(capeId) && !state.customCapes?.some((c) => c.id === capeId)) {
    showToastStatus("Tài khoản không sở hữu áo choàng này.");
    return;
  }
  account.capeId = capeId;
  saveState();
  syncSkinViewerCape(account);
  renderCapeSelector(account);
  const cape = getCapePreset(capeId);
  skinStatus(`Đã chọn áo choàng: ${cape.name} (${account.name}).`);
  showToastStatus(`Áo choàng: ${cape.name}`);
}

function setSkinAnimation(anim) {
  state.skinAnim = anim;
  saveState();
  applySkinViewerAnimation();
  $$("#skinAnimGroup .stage-btn").forEach((btn) => {
    btn.classList.toggle("is-active", btn.dataset.skinAnim === anim);
  });
  const labelMap = {
    walk: "Đi bộ (Walk)",
    fly: "Bay với Elytra (Fly)",
    sneak: "Rình rập (Sneak)",
    wave: "Vẫy tay (Wave)",
    bow: "Giương cung (Bow aim)",
  };
  skinStatus(`Hoạt ảnh: ${labelMap[anim] || anim}.`);
}

function toggleSkinAutoRotate() {
  if (!skinViewer) return;
  skinViewer.autoRotate = !skinViewer.autoRotate;
  state.skinAutoRotate = skinViewer.autoRotate;
  saveState();
  const btn = $("#skinAutoRotateBtn");
  if (btn) btn.classList.toggle("is-active", skinViewer.autoRotate);
  skinStatus(skinViewer.autoRotate ? "Bật tự xoay 3D." : "Tắt tự xoay 3D.");
  showToastStatus(skinViewer.autoRotate ? "Đã bật tự động xoay 3D" : "Đã tắt tự động xoay 3D");
}

function resetSkinCamera() {
  if (!skinViewer) return;
  if (typeof skinViewer.resetCameraPose === "function") {
    skinViewer.resetCameraPose();
  }
  if (skinViewer.controls) {
    if (typeof skinViewer.controls.reset === "function") skinViewer.controls.reset();
    if (skinViewer.controls.target) skinViewer.controls.target.set(0, 0, 0);
  }
  if (skinViewer.playerWrapper) {
    skinViewer.playerWrapper.rotation.set(0, 0, 0);
  }
  if (skinViewer.playerObject) {
    skinViewer.playerObject.rotation.set(0, 0, 0);
  }
  skinStatus("Đã đặt lại góc nhìn camera.");
  showToastStatus("Đã đặt lại góc nhìn camera (hướng thẳng)");
}

function renderSkinPreview(preset, account, appliedId) {
  const stage = $("#skinStage");
  if (!stage) return;

  const applied = preset.id === appliedId;
  stage.classList.toggle("is-empty", !account);
  stage.style.setProperty("--skin-accent", preset.accent || "#0b7272");
  const canvas = $("#skinViewer");
  if (canvas) {
    canvas.setAttribute("aria-label", `Mô hình 3D skin ${preset.name} của ${account?.name || "tài khoản"}`);
  }

  $("#skinPreviewName").textContent = preset.name;
  $("#skinPreviewModel").textContent = preset.model;
  $("#skinPreviewEyebrow").textContent = account ? `Skin cho ${account.name}` : "Chưa có tài khoản";
  $("#skinAppliedBadge").hidden = !applied || !account;
  $("#skinCurrentName").textContent = applied ? preset.name : getSkinPreset(appliedId)?.name || "—";
  $("#skinCurrentAccount").textContent = account
    ? `${account.name} · ${account.kind}${account.active ? " · Active" : ""}`
    : "Thêm tài khoản ở trang Accounts.";
  $("#applySkinBtn").disabled = !account || applied;
  $("#applySkinBtn").textContent = applied ? "Đang dùng" : "Áp dụng skin";
  $("#resetSkinBtn").disabled = !account || appliedId === defaultSkinForAccount(account);
  
  renderCapeSelector(account);
  syncSkinViewer(preset, account);
  applySkinViewerAnimation();
}

function renderSkins() {
  const grid = $("#skinGrid");
  const select = $("#skinAccountSelect");
  if (!grid || !select) return;

  if (!state.accounts.some((acc) => acc.id === state.skinAccountId)) {
    state.skinAccountId = (state.accounts.find((acc) => acc.active) || state.accounts[0])?.id || null;
  }
  const account = selectedSkinAccount();
  const appliedId = account ? ensureAccountSkin(account) : null;
  if (!getSkinPreset(state.skinSelectedId)) {
    state.skinSelectedId = appliedId || SKIN_PRESETS[0].id;
  }
  const selectedId = state.skinSelectedId;
  const preset = getSkinPreset(selectedId) || SKIN_PRESETS[0];

  select.innerHTML = state.accounts.length
    ? state.accounts.map((acc) => `<option value="${esc(acc.id)}">${esc(acc.name)}${acc.active ? " · Active" : ""}</option>`).join("")
    : '<option>Chưa có tài khoản</option>';
  select.value = state.skinAccountId || "";
  select.disabled = !state.accounts.length;
  $("#randomSkinBtn").disabled = state.accounts.length === 0;

  const allPresets = allSkinPresets();
  $("#skinsCount").textContent = allPresets.length;

  renderSkinPreview(preset, account, appliedId);

  grid.innerHTML = allPresets.map((item) => {
    const selected = item.id === selectedId;
    const applied = item.id === appliedId;
    const isCustom = !!item.custom;
    return `
      <button class="skin-card${selected ? " is-selected" : ""}${applied ? " is-applied" : ""}${isCustom ? " is-custom" : ""}" type="button"
        data-skin-id="${esc(item.id)}" aria-pressed="${selected}"
        style="--skin-texture:url('${esc(item.texture)}')">
        <span class="skin-thumb" aria-hidden="true"></span>
        <span class="skin-card-copy">
          <span class="skin-card-name">${esc(item.name)}</span>
          <span class="skin-card-model">${esc(item.model)}${isCustom ? " · Custom" : ""}</span>
        </span>
        ${applied ? '<span class="skin-card-badge">Đang dùng</span>' : selected ? '<span class="skin-card-check" aria-hidden="true">✓</span>' : ""}
        ${isCustom ? `<span class="skin-card-delete glyph" data-delete-custom-skin="${esc(item.id)}" title="Xóa skin này" aria-label="Xóa skin">&#xE74D;</span>` : ""}
      </button>`;
  }).join("");

  if (!state.accounts.length) {
    skinStatus("Chưa có tài khoản. Thêm tài khoản ở trang Accounts trước khi đổi skin.");
  } else if (($("#skinStatus")?.textContent || "").startsWith("Chưa có tài khoản")) {
    skinStatus("");
  }
}

function selectSkinAccount(id) {
  const acc = getAccount(id);
  if (!acc) return;
  state.skinAccountId = id;
  state.skinSelectedId = ensureAccountSkin(acc);
  renderSkins();
  skinStatus(`Đang xem skin của ${acc.name}.`);
}

function selectSkinPreset(id) {
  const preset = getSkinPreset(id);
  if (!preset) return;
  state.skinSelectedId = id;
  renderSkins();
  skinStatus(`Đã chọn ${preset.name}. Bấm "Áp dụng skin" để lưu.`);
}

function pulseSkinStage() {
  const stage = $("#skinStage");
  if (!stage) return;
  stage.classList.remove("is-applied");
  void stage.offsetWidth;
  stage.classList.add("is-applied");
  setTimeout(() => stage.classList.remove("is-applied"), 720);
}

function applySkinSelection() {
  const account = selectedSkinAccount();
  const preset = getSkinPreset(state.skinSelectedId);
  if (!account) {
    skinStatus("Chưa có tài khoản để áp dụng skin.");
    return;
  }
  if (!preset) return;
  account.skinId = preset.id;
  saveState();
  renderSkins();
  renderAccounts();
  pulseSkinStage();
  skinStatus(`Đã áp dụng skin ${preset.name} cho ${account.name}.`);
  showToastStatus(`Đã đổi skin ${account.name} sang ${preset.name}.`);
}

function resetSelectedSkin() {
  const account = selectedSkinAccount();
  if (!account) return;
  const fallback = defaultSkinForAccount(account);
  account.skinId = fallback;
  state.skinSelectedId = fallback;
  saveState();
  renderSkins();
  renderAccounts();
  pulseSkinStage();
  skinStatus(`Đã trả ${account.name} về skin mặc định (${getSkinPreset(fallback).name}).`);
}

function randomSkinSelection() {
  if (!state.accounts.length) return;
  const all = allSkinPresets();
  const choices = all.filter((preset) => preset.id !== state.skinSelectedId);
  const picked = choices[rand(0, choices.length - 1)] || all[0];
  state.skinSelectedId = picked.id;
  renderSkins();
  skinStatus(`Gợi ý ngẫu nhiên: ${picked.name}. Bấm "Áp dụng skin" nếu thích.`);
}

function deleteCustomSkin(id) {
  state.customSkins = (state.customSkins || []).filter((s) => s.id !== id);
  state.accounts.forEach((acc) => {
    if (acc.skinId === id) acc.skinId = defaultSkinForAccount(acc);
  });
  if (state.skinSelectedId === id) {
    state.skinSelectedId = SKIN_PRESETS[0].id;
  }
  saveState();
  renderSkins();
  showToastStatus("Đã xóa skin tùy chỉnh.");
  skinStatus("Đã xóa skin tùy chỉnh khỏi thư viện.");
}

function promptAddCustomSkin(textureDataUrl, defaultName) {
  showDialog({
    title: "Thêm skin tùy chỉnh",
    wide: false,
    primary: "Lưu vào thư viện",
    closeText: "Hủy",
    body: `
      <div class="custom-skin-dialog-body" style="display:flex;flex-direction:column;gap:14px;">
        <div style="display:flex;align-items:center;gap:14px;">
          <div style="width:58px;height:58px;border-radius:8px;background:#0d141e url('${textureDataUrl}') no-repeat center;background-size:contain;image-rendering:pixelated;border:1px solid var(--stroke);"></div>
          <div style="display:flex;flex-direction:column;gap:3px;">
            <strong>File skin hợp lệ (PNG)</strong>
            <span class="caption text-2">Đã kiểm tra kích thước chuẩn Minecraft (64x64/64x32).</span>
          </div>
        </div>
        <label class="field-label" style="display:flex;flex-direction:column;gap:6px;">
          <span>Tên hiển thị skin</span>
          <input class="input" id="customSkinNameInput" type="text" value="${esc(defaultName || "Custom Skin")}" maxlength="32" autocomplete="off" />
        </label>
        <label class="field-label" style="display:flex;flex-direction:column;gap:6px;">
          <span>Kiểu model tay (Model)</span>
          <select class="select" id="customSkinModelSelect">
            <option value="Classic">Classic (Steve — tay 4px)</option>
            <option value="Slim">Slim (Alex — tay 3px)</option>
          </select>
        </label>
      </div>
    `,
    onPrimary: () => {
      const nameInput = $("#customSkinNameInput");
      const modelSelect = $("#customSkinModelSelect");
      const skinName = nameInput?.value?.trim() || defaultName || "Custom Skin";
      const model = modelSelect?.value === "Slim" ? "Slim" : "Classic";

      const newSkin = {
        id: "custom-" + Date.now(),
        name: skinName,
        model,
        texture: textureDataUrl,
        accent: "#0b7272",
        custom: true,
      };

      state.customSkins = Array.isArray(state.customSkins) ? state.customSkins : [];
      state.customSkins.push(newSkin);
      state.skinSelectedId = newSkin.id;
      saveState();
      renderSkins();
      showToastStatus(`Đã thêm "${newSkin.name}"`);
      skinStatus(`Đã thêm skin "${newSkin.name}". Bấm "Áp dụng skin" để lưu cho tài khoản.`);
      return true;
    },
  });
}

function handleSkinFileUpload(file) {
  if (!file) return;
  if (!file.type.includes("png") && !file.name.toLowerCase().endsWith(".png")) {
    showToastStatus("Lỗi: Chỉ hỗ trợ file skin định dạng PNG.");
    skinStatus("Lỗi: File skin phải là định dạng .PNG.");
    return;
  }
  const reader = new FileReader();
  reader.onload = (e) => {
    const dataUrl = e.target.result;
    const img = new Image();
    img.onload = () => {
      if (!((img.width === 64 && img.height === 64) || (img.width === 64 && img.height === 32))) {
        showToastStatus(`Kích thước không hợp lệ: ${img.width}x${img.height}. Cần 64x64 hoặc 64x32.`);
        skinStatus(`Lỗi kích thước skin: ${img.width}x${img.height}px. Skin chuẩn Minecraft phải là 64x64 hoặc 64x32.`);
        return;
      }
      promptAddCustomSkin(dataUrl, file.name.replace(/\.png$/i, ""));
    };
    img.onerror = () => {
      showToastStatus("Lỗi: Không thể đọc hình ảnh skin.");
      skinStatus("Lỗi: Không thể đọc file hình ảnh.");
    };
    img.src = dataUrl;
  };
  reader.readAsDataURL(file);
}

function generateDeterministicSkin(username) {
  let hash = 0;
  for (let i = 0; i < username.length; i++) {
    hash = (hash << 5) - hash + username.charCodeAt(i);
    hash |= 0;
  }
  const hue = Math.abs(hash) % 360;
  const canvas = document.createElement("canvas");
  canvas.width = 64;
  canvas.height = 64;
  const ctx = canvas.getContext("2d");

  const skinTones = ["#ffdbac", "#f1c27d", "#e0ac69", "#c68642", "#8d5524"];
  const skinColor = skinTones[Math.abs(hash >> 3) % skinTones.length];
  const hairColors = ["#2c1b18", "#4a3728", "#7a4b2b", "#b55239", "#e6be8a", "#1a1a1a"];
  const hairColor = hairColors[Math.abs(hash >> 6) % hairColors.length];
  const shirtColor = `hsl(${hue}, 70%, 45%)`;
  const pantsColor = `hsl(${(hue + 180) % 360}, 50%, 30%)`;
  const eyeColor = `hsl(${(hue + 90) % 360}, 80%, 45%)`;
  const shoeColor = "#1f1f1f";

  // Head front (8, 8, 8, 8)
  ctx.fillStyle = skinColor;
  ctx.fillRect(8, 8, 8, 8);
  // Head top (8, 0, 8, 8)
  ctx.fillStyle = hairColor;
  ctx.fillRect(8, 0, 8, 8);
  // Hair on head front
  ctx.fillRect(8, 8, 8, 3);
  // Eyes
  ctx.fillStyle = "#ffffff";
  ctx.fillRect(9, 12, 2, 1);
  ctx.fillRect(13, 12, 2, 1);
  ctx.fillStyle = eyeColor;
  ctx.fillRect(10, 12, 1, 1);
  ctx.fillRect(13, 12, 1, 1);
  // Mouth
  ctx.fillStyle = "#a85f52";
  ctx.fillRect(11, 14, 2, 1);

  // Head sides & back
  ctx.fillStyle = hairColor;
  ctx.fillRect(0, 8, 8, 8);
  ctx.fillRect(16, 8, 8, 8);
  ctx.fillRect(24, 8, 8, 8);

  // Torso / Shirt front (20, 20, 8, 12)
  ctx.fillStyle = shirtColor;
  ctx.fillRect(20, 20, 8, 12);
  ctx.fillRect(16, 20, 4, 12);
  ctx.fillRect(28, 20, 4, 12);
  ctx.fillRect(32, 20, 8, 12);

  // Arms front (44, 20, 4, 12) & (36, 52, 4, 12)
  ctx.fillStyle = shirtColor;
  ctx.fillRect(44, 20, 4, 4);
  ctx.fillRect(36, 52, 4, 4);
  ctx.fillStyle = skinColor;
  ctx.fillRect(44, 24, 4, 8);
  ctx.fillRect(36, 56, 4, 8);

  // Legs front (4, 20, 4, 12) & (20, 52, 4, 12)
  ctx.fillStyle = pantsColor;
  ctx.fillRect(4, 20, 4, 9);
  ctx.fillRect(20, 52, 4, 9);
  ctx.fillStyle = shoeColor;
  ctx.fillRect(4, 29, 4, 3);
  ctx.fillRect(20, 61, 4, 3);

  return canvas.toDataURL("image/png");
}

async function fetchOrGenerateSkin(name) {
  try {
    const url = `https://minotar.net/skin/${encodeURIComponent(name)}`;
    const res = await fetch(url, { mode: "cors" });
    if (res.ok) {
      const blob = await res.blob();
      return await new Promise((resolve) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.readAsDataURL(blob);
      });
    }
  } catch {
    // Network or CORS -> procedural fallback
  }
  return generateDeterministicSkin(name);
}

async function grabSkinByUsername(username) {
  const name = (username || "").trim();
  if (!name) {
    skinStatus("Vui lòng nhập tên người chơi Minecraft.");
    return;
  }
  const btn = $("#skinGrabBtn");
  if (btn) {
    btn.disabled = true;
    btn.textContent = "Đang tải...";
  }
  skinStatus(`Đang tải skin của "${name}"...`);

  const textureDataUrl = await fetchOrGenerateSkin(name);
  if (btn) {
    btn.disabled = false;
    btn.textContent = "Lấy skin";
  }

  if (!textureDataUrl) {
    skinStatus(`Không thể tải skin của "${name}".`);
    showToastStatus(`Lỗi khi lấy skin của ${name}.`);
    return;
  }

  const isSlim = /alex|girl|slim|lily|eve/i.test(name);
  const newSkin = {
    id: "grab-" + Date.now(),
    name: name,
    model: isSlim ? "Slim" : "Classic",
    texture: textureDataUrl,
    accent: "#0b7272",
    custom: true,
  };

  state.customSkins = Array.isArray(state.customSkins) ? state.customSkins : [];
  state.customSkins.push(newSkin);
  state.skinSelectedId = newSkin.id;
  saveState();
  renderSkins();
  showToastStatus(`Đã lấy skin của ${name}!`);
  skinStatus(`Đã lấy thành công skin của "${name}" và thêm vào thư viện.`);
}

/* ------------------------------------------------------------
   COSMETICS & CAPES STORE (SHOP)
   ------------------------------------------------------------ */
const SHOP_ITEMS = [
  {
    id: "cape-emerald",
    name: "Emerald Knight Cape",
    category: "capes",
    rarity: "rare",
    price: 250,
    desc: "Áo choàng hiệp sĩ ngọc lục bảo ánh kim sáng chói.",
    icon: "&#xE790;",
    accent: "#10b981",
  },
  {
    id: "cape-void",
    name: "Void Monarch Cape",
    category: "capes",
    rarity: "legendary",
    price: 600,
    desc: "Áo choàng vua chúa bóng tối hư không huyền bí.",
    icon: "&#xE790;",
    accent: "#8b5cf6",
  },
  {
    id: "cape-phoenix",
    name: "Phoenix Fire Cape",
    category: "capes",
    rarity: "epic",
    price: 420,
    desc: "Đôi cánh lông vũ rực lửa phượng hoàng tái sinh.",
    icon: "&#xE790;",
    accent: "#f97316",
  },
  {
    id: "cape-sakura",
    name: "Sakura Breeze Cape",
    category: "capes",
    rarity: "common",
    price: 150,
    desc: "Những cánh hoa anh đào dịu nhẹ bay trong gió.",
    icon: "&#xE790;",
    accent: "#ec4899",
  },
  {
    id: "wing-ender",
    name: "Ender Dragon Wings",
    category: "wings",
    rarity: "legendary",
    price: 750,
    desc: "Đôi cánh rồng Ender bóng đêm uy quyền và dũng mãnh.",
    icon: "&#xE706;",
    accent: "#a855f7",
  },
  {
    id: "wing-cyber",
    name: "Cyberpunk Mech Wings",
    category: "wings",
    rarity: "epic",
    price: 480,
    desc: "Cánh cơ khí công nghệ cao neon tương lai rực rỡ.",
    icon: "&#xE706;",
    accent: "#06b6d4",
  },
  {
    id: "wing-angel",
    name: "Celestial Angel Wings",
    category: "wings",
    rarity: "rare",
    price: 320,
    desc: "Đôi cánh thiên thần trắng muốt thanh khiết từ thiên đàng.",
    icon: "&#xE706;",
    accent: "#38bdf8",
  },
  {
    id: "hat-crown",
    name: "Royal Crown",
    category: "hats",
    rarity: "legendary",
    price: 550,
    desc: "Vương miện hoàng gia vàng ròng đính đá quý lấp lánh.",
    icon: "&#xE735;",
    accent: "#eab308",
  },
  {
    id: "hat-bandana",
    name: "Ninja Bandana",
    category: "hats",
    rarity: "common",
    price: 120,
    desc: "Băng đô ninja đen bí ẩn cho các sát thủ bóng đêm.",
    icon: "&#xE735;",
    accent: "#64748b",
  },
  {
    id: "hat-neko",
    name: "Neko Cat Ears",
    category: "hats",
    rarity: "rare",
    price: 280,
    desc: "Đôi tai mèo anime dễ thương vẫy theo nhịp bước.",
    icon: "&#xE735;",
    accent: "#f43f5e",
  },
  {
    id: "effect-flame",
    name: "Flame Trail Effect",
    category: "effects",
    rarity: "epic",
    price: 380,
    desc: "Dấu chân lửa bùng cháy mỗi khi nhân vật di chuyển.",
    icon: "&#xE7E8;",
    accent: "#ef4444",
  },
  {
    id: "effect-nebula",
    name: "Cosmic Nebula Aura",
    category: "effects",
    rarity: "legendary",
    price: 700,
    desc: "Vầng hào quang dải ngân hà tinh vân xoay quanh cơ thể.",
    icon: "&#xE7E8;",
    accent: "#6366f1",
  },
  {
    id: "bundle-starter",
    name: "Cosmetics Starter Pack",
    category: "bundles",
    rarity: "epic",
    price: 500,
    desc: "Gói tân thủ: Sakura Cape + Ninja Bandana + Flame Trail.",
    bundleItems: ["cape-sakura", "hat-bandana", "effect-flame"],
    icon: "&#xE8F1;",
    accent: "#14b8a6",
  },
];

const RARITY_LABELS = {
  common: "Phổ biến",
  rare: "Hiếm",
  epic: "Sử thi",
  legendary: "Huyền thoại",
};

const RARITY_WEIGHT = {
  legendary: 4,
  epic: 3,
  rare: 2,
  common: 1,
};

const CATEGORY_LABELS = {
  capes: "Áo choàng",
  wings: "Cánh Elytra",
  hats: "Mũ 3D",
  effects: "Hiệu ứng",
  bundles: "Gói Combo",
};

let shopViewer = null;

function renderShop() {
  const page = $("#page-shop");
  if (!page || page.hidden) return;

  // 1. Header Coin Balance & Daily Claim
  const balanceEl = $("#shopCoinBalance");
  if (balanceEl) balanceEl.textContent = state.shopCoins.toLocaleString("vi-VN");

  const dailyBtn = $("#claimDailyCoinBtn");
  if (dailyBtn) {
    dailyBtn.disabled = !!state.shopDailyClaimed;
    dailyBtn.innerHTML = state.shopDailyClaimed
      ? '<span class="glyph">&#xE73E;</span><span>Đã nhận hôm nay</span>'
      : '<span class="glyph">&#xE719;</span><span>Nhận 100 Coin</span>';
  }

  // 2. Filter & Sort Items
  const cat = state.shopFilter?.category || "all";
  const query = (state.shopFilter?.query || "").trim().toLowerCase();
  const sort = state.shopFilter?.sort || "featured";

  let items = SHOP_ITEMS.filter((item) => {
    if (cat !== "all" && item.category !== cat) return false;
    if (query) {
      const matchName = item.name.toLowerCase().includes(query);
      const matchDesc = item.desc.toLowerCase().includes(query);
      const matchRarity = (RARITY_LABELS[item.rarity] || item.rarity).toLowerCase().includes(query);
      const matchCat = (CATEGORY_LABELS[item.category] || item.category).toLowerCase().includes(query);
      if (!matchName && !matchDesc && !matchRarity && !matchCat) return false;
    }
    return true;
  });

  if (sort === "price-asc") {
    items.sort((a, b) => a.price - b.price);
  } else if (sort === "price-desc") {
    items.sort((a, b) => b.price - a.price);
  } else if (sort === "rarity") {
    items.sort((a, b) => (RARITY_WEIGHT[b.rarity] || 0) - (RARITY_WEIGHT[a.rarity] || 0));
  }

  // 3. Count Pill & Empty State
  const countPill = $("#shopCount");
  if (countPill) countPill.textContent = `${items.length} vật phẩm`;

  const grid = $("#shopGrid");
  const empty = $("#shopEmptyState");

  if (!items.length) {
    if (grid) grid.innerHTML = "";
    if (grid) grid.hidden = true;
    if (empty) empty.hidden = false;
    return;
  }

  if (empty) empty.hidden = true;
  if (grid) grid.hidden = false;

  // 4. Render Cards
  if (grid) {
    grid.innerHTML = items
      .map((item) => {
        const isOwned = state.shopOwned.includes(item.id);
        const itemCat = item.category === "bundles" ? "bundles" : item.category;
        const isEquipped = isOwned && state.shopEquipped[itemCat] === item.id;

        return `
          <div class="shop-card ${isOwned ? "is-owned" : ""}" data-shop-id="${esc(item.id)}">
            <div class="shop-card-thumb" style="--card-accent: ${esc(item.accent)}">
              <span class="shop-card-icon glyph">${item.icon}</span>
              <span class="shop-rarity-badge rarity-${esc(item.rarity)}">${esc(RARITY_LABELS[item.rarity] || item.rarity)}</span>
              ${isEquipped ? '<span class="shop-equipped-badge"><span class="glyph">&#xE73E;</span> Đang dùng</span>' : ""}
            </div>
            <div class="shop-card-info">
              <div class="shop-card-category">${esc(CATEGORY_LABELS[item.category] || item.category)}</div>
              <h3 class="shop-card-title">${esc(item.name)}</h3>
              <p class="shop-card-desc">${esc(item.desc)}</p>
            </div>
            <div class="shop-card-footer">
              <div class="shop-card-price-wrap">
                ${
                  isOwned
                    ? '<span class="shop-owned-tag">Đã sở hữu</span>'
                    : `<span class="shop-card-price"><span class="glyph">&#xE719;</span><strong>${item.price}</strong> <span class="shop-coin-unit">Coins</span></span>`
                }
              </div>
              <div class="shop-card-actions">
                <button type="button" class="btn compact shop-tryon-btn" data-shop-action="tryon" data-item-id="${esc(item.id)}" title="Thử đồ 3D">
                  <span class="glyph">&#xE790;</span><span>Thử</span>
                </button>
                ${
                  isOwned
                    ? `<button type="button" class="btn compact shop-equip-btn ${isEquipped ? "is-equipped" : ""}" data-shop-action="equip" data-item-id="${esc(item.id)}">
                        <span>${isEquipped ? "Gỡ" : "Dùng"}</span>
                      </button>`
                    : `<button type="button" class="btn compact accent shop-buy-btn" data-shop-action="buy" data-item-id="${esc(item.id)}">
                        <span>Mua</span>
                      </button>`
                }
              </div>
            </div>
          </div>
        `;
      })
      .join("");
  }
}

function claimDailyCoin() {
  if (state.shopDailyClaimed) {
    showToastStatus("Hôm nay bạn đã nhận thưởng rồi. Hãy quay lại vào ngày mai!");
    return;
  }
  state.shopCoins += 100;
  state.shopDailyClaimed = true;
  saveState();
  renderShop();
  showToastStatus("Đã nhận thành công 100 Vaz Coins điểm danh hàng ngày!");
}

function buyShopItem(id) {
  const item = SHOP_ITEMS.find((it) => it.id === id);
  if (!item) return;

  if (state.shopOwned.includes(item.id)) {
    showToastStatus(`Bạn đã sở hữu vật phẩm "${item.name}" rồi.`);
    return;
  }

  if (state.shopCoins < item.price) {
    showToastStatus(`Không đủ Vaz Coins! Cần ${item.price} Coins nhưng bạn chỉ có ${state.shopCoins} Coins.`);
    return;
  }

  state.shopCoins -= item.price;
  state.shopOwned.push(item.id);

  // Gói Bundle tự động mở khóa các item con
  if (Array.isArray(item.bundleItems)) {
    item.bundleItems.forEach((subId) => {
      if (!state.shopOwned.includes(subId)) state.shopOwned.push(subId);
    });
  }

  // Tự động trang bị ngay sau khi mua
  const cat = item.category === "bundles" ? "bundles" : item.category;
  state.shopEquipped[cat] = item.id;

  saveState();
  renderShop();
  showToastStatus(`Đã mua và trang bị thành công: ${item.name}!`);

  // Nếu đang mở try-on modal cho item này, cập nhật nút trong modal
  if (state.shopTryOnItem?.id === item.id) {
    updateShopTryOnActions(item);
  }
}

function toggleEquipShopItem(id) {
  const item = SHOP_ITEMS.find((it) => it.id === id);
  if (!item) return;

  if (!state.shopOwned.includes(item.id)) {
    showToastStatus("Bạn chưa sở hữu vật phẩm này.");
    return;
  }

  const cat = item.category === "bundles" ? "bundles" : item.category;
  if (state.shopEquipped[cat] === item.id) {
    state.shopEquipped[cat] = null;
    showToastStatus(`Đã gỡ vật phẩm: ${item.name}`);
  } else {
    state.shopEquipped[cat] = item.id;
    showToastStatus(`Đã trang bị: ${item.name}`);
  }

  saveState();
  renderShop();

  if (state.shopTryOnItem?.id === item.id) {
    updateShopTryOnActions(item);
  }
}

function openShopTryOnModal(id) {
  const item = SHOP_ITEMS.find((it) => it.id === id);
  if (!item) return;

  state.shopTryOnItem = item;

  const host = $("#shopTryOnModalHost");
  if (!host) return;

  $("#shopTryOnTitle").textContent = item.name;
  const rarityBadge = $("#shopTryOnRarity");
  if (rarityBadge) {
    rarityBadge.className = `shop-rarity-badge rarity-${item.rarity}`;
    rarityBadge.textContent = RARITY_LABELS[item.rarity] || item.rarity;
  }
  $("#shopTryOnDesc").textContent = item.desc;
  $("#shopTryOnPrice").textContent = state.shopOwned.includes(item.id)
    ? "Đã sở hữu"
    : `${item.price} Vaz Coins`;

  updateShopTryOnActions(item);

  host.hidden = false;
  initShopViewer(item);
}

function updateShopTryOnActions(item) {
  const actionsWrap = $("#shopTryOnActions");
  if (!actionsWrap) return;

  const isOwned = state.shopOwned.includes(item.id);
  const cat = item.category === "bundles" ? "bundles" : item.category;
  const isEquipped = isOwned && state.shopEquipped[cat] === item.id;

  actionsWrap.innerHTML = `
    <button type="button" class="btn" id="shopTryOnCloseActionBtn">Đóng</button>
    ${
      isOwned
        ? `<button type="button" class="btn ${isEquipped ? "" : "accent"}" id="shopTryOnEquipBtn">
            <span>${isEquipped ? "Gỡ bỏ" : "Trang bị"}</span>
          </button>`
        : `<button type="button" class="btn accent" id="shopTryOnBuyBtn">
            <span class="glyph">&#xE719;</span><span>Mua (${item.price} Coins)</span>
          </button>`
    }
  `;

  $("#shopTryOnCloseActionBtn")?.addEventListener("click", closeShopTryOnModal);
  $("#shopTryOnBuyBtn")?.addEventListener("click", () => buyShopItem(item.id));
  $("#shopTryOnEquipBtn")?.addEventListener("click", () => toggleEquipShopItem(item.id));
}

function closeShopTryOnModal() {
  const host = $("#shopTryOnModalHost");
  if (host) host.hidden = true;
  state.shopTryOnItem = null;
  if (shopViewer) {
    shopViewer.renderPaused = true;
  }
}

function initShopViewer(item) {
  const canvas = $("#shopViewer");
  if (!canvas || !window.skinview3d) return;

  if (!shopViewer) {
    try {
      shopViewer = new window.skinview3d.SkinViewer({
        canvas,
        width: 320,
        height: 380,
        skin: "assets/skins/steve.png",
      });
      shopViewer.autoRotate = true;
      shopViewer.autoRotateSpeed = 1.4;
      shopViewer.animation = new window.skinview3d.WalkingAnimation();
      window.shopViewer = shopViewer;
    } catch {
      // Skinview3d canvas fallback
    }
  }

  if (shopViewer) {
    shopViewer.renderPaused = false;
    // Tùy theo loại cosmetic, đổi preview tương ứng
    if (item.category === "capes") {
      shopViewer.loadCape("assets/capes/vanilla.png", { backEquipment: "cape" });
      shopViewer.playerObject.backEquipment = "cape";
      shopViewer.animation = new window.skinview3d.WalkingAnimation();
    } else if (item.category === "wings") {
      shopViewer.loadCape("assets/capes/migrator.png", { backEquipment: "elytra" });
      shopViewer.playerObject.backEquipment = "elytra";
      shopViewer.animation = new window.skinview3d.FlyingAnimation();
    } else {
      shopViewer.animation = new window.skinview3d.WalkingAnimation();
    }
  }
}

/* ------------------------------------------------------------
   CONTEXT MENU & INSTANCE ACTIONS
   ------------------------------------------------------------ */
/** Nhân bản instance (hỗ trợ Full clone hoặc Clean instance). */
function duplicateInstance(id) {
  const src = getInstance(id);
  if (!src) return;

  const n = state.instances.filter((i) => i.id.startsWith(`${src.id}-copy`)).length + 1;
  const defaultName = `${src.name} (Copy${n > 1 ? ` ${n}` : ""})`;
  const worldsCount = (state.worlds[src.id] || []).length;
  const ssCount = (state.screenshots[src.id] || []).length;

  showDialog({
    title: `Nhân bản Instance — ${src.name}`,
    primary: "Nhân bản",
    closeText: "Hủy",
    body: `
      <label class="field"><span>Tên instance mới</span>
        <input class="textbox" id="cloneInstanceName" value="${esc(defaultName)}" autocomplete="off" />
      </label>

      <div class="clone-options-list">
        <label class="option-card">
          <input type="radio" name="cloneType" value="full" checked />
          <div class="option-card-content">
            <span class="option-card-title">
              <strong>Nhân bản đầy đủ (Full Clone)</strong>
            </span>
            <span class="option-card-desc">
              Giữ nguyên toàn bộ: Mods, Configs, ${worldsCount} Worlds và ${ssCount} Screenshots. Tiếp tục chơi ngay với bản sao y hệt.
            </span>
          </div>
        </label>

        <label class="option-card">
          <input type="radio" name="cloneType" value="clean" />
          <div class="option-card-content">
            <span class="option-card-title">
              <strong>Tạo instance sạch (Clean Instance)</strong>
              <span class="badge">Bắt đầu mới</span>
            </span>
            <span class="option-card-desc">
              Chỉ sao chép danh sách Mods và Cấu hình; dọn sạch Worlds và Screenshots để tạo thế giới mới từ đầu.
            </span>
          </div>
        </label>
      </div>
    `,
    onPrimary: () => {
      const input = $("#cloneInstanceName");
      const newName = (input?.value || "").trim() || defaultName;
      const cloneType = $('input[name="cloneType"]:checked')?.value || "full";

      const copy = structuredClone(src);
      copy.id = `${src.id}-copy-${Date.now().toString(36)}`;
      copy.name = newName;
      copy.path = `${src.path}-copy-${Date.now().toString(36)}`;
      copy.createdTs = Date.now();
      copy.lastPlayedTs = 0;
      copy.lastPlayed = null;
      copy.playtimeMin = 0;
      delete copy.status;
      delete copy.sessionStart;
      copy.running = false;
      copy.launching = false;
      copy.pid = null;

      state.instances.push(copy);
      state.instances[copy.id] = copy;
      state.mods[copy.id] = structuredClone(state.mods[src.id] || []);

      if (cloneType === "full") {
        state.worlds[copy.id] = structuredClone(state.worlds[src.id] || []);
        state.screenshots[copy.id] = structuredClone(state.screenshots[src.id] || []);
      } else {
        state.worlds[copy.id] = [];
        state.screenshots[copy.id] = [];
      }

      const task = createTask("clone", `Nhân bản ${src.name}`, `Tạo "${newName}" (${cloneType})`);
      finishTask(task, "done", `Đã nhân bản thành "${newName}"`);

      saveState();
      renderInstances();
      showToastStatus(`Đã nhân bản thành công: "${newName}".`);
      return true;
    },
  });

  setTimeout(() => $("#cloneInstanceName")?.select(), 40);
}

function openExportDialog(instId) {
  const inst = getInstance(instId);
  if (!inst) return;

  const modsCount = (state.mods[instId] || []).length;
  const worldsCount = (state.worlds[instId] || []).length;
  const screenshotsCount = (state.screenshots[instId] || []).length;

  showDialog({
    title: `Xuất Instance — ${inst.name}`,
    wide: true,
    primary: "Bắt đầu xuất",
    closeText: "Hủy",
    body: `
      <p class="text-2">Chọn định dạng gói xuất phù hợp với mục đích chia sẻ hoặc sao lưu của bạn:</p>
      
      <div class="export-options-list">
        <label class="option-card">
          <input type="radio" name="exportFormat" value="mrpack" checked />
          <div class="option-card-content">
            <span class="option-card-title">
              <strong>Modrinth Modpack (.mrpack)</strong>
              <span class="badge accent">Khuyến nghị chia sẻ</span>
            </span>
            <span class="option-card-desc">
              Siêu nhẹ (~1-2 MB). Chỉ xuất danh sách mods, loader (${esc(inst.loader)}) và file config cần thiết. Người nhận sẽ tự tải mods từ Modrinth khi cài đặt. Không chứa saves hay screenshots riêng tư.
            </span>
          </div>
        </label>

        <label class="option-card">
          <input type="radio" name="exportFormat" value="zip" />
          <div class="option-card-content">
            <span class="option-card-title">
              <strong>Full Backup Archive (.zip)</strong>
              <span class="badge">Sao lưu đầy đủ</span>
            </span>
            <span class="option-card-desc">
              Đóng gói toàn bộ thư mục instance: ${modsCount} mods (.jar), configs, ${worldsCount} worlds và ${screenshotsCount} screenshots. Thích hợp lưu trữ offline hoặc chuyển sang PC khác.
            </span>
          </div>
        </label>
      </div>

      <div class="settings-stack" style="margin-top: 10px;">
        <label class="checkbox">
          <input type="checkbox" id="exportIncludeConfigs" checked />
          <span class="box"></span>Bao gồm thiết lập cấu hình mod (config/, options.txt)
        </label>
        <label class="checkbox" id="exportWorldsChkLabel">
          <input type="checkbox" id="exportIncludeWorlds" />
          <span class="box"></span>Bao gồm thế giới / saves (${worldsCount} worlds)
        </label>
      </div>
    `,
    onPrimary: () => {
      const format = $('input[name="exportFormat"]:checked')?.value || "mrpack";
      startExportSimulation(inst, format);
      return true;
    },
  });

  setTimeout(() => {
    $$('input[name="exportFormat"]').forEach((radio) => {
      radio.addEventListener("change", () => {
        const isZip = radio.value === "zip";
        const wChk = $("#exportIncludeWorlds");
        if (wChk) {
          wChk.checked = isZip;
          wChk.disabled = !isZip;
        }
      });
    });
    const wChk = $("#exportIncludeWorlds");
    if (wChk) wChk.disabled = true;
  }, 20);
}

async function startExportSimulation(inst, format) {
  const ext = format === "mrpack" ? "mrpack" : "zip";
  const sizeStr = format === "mrpack" ? "1.2 MB" : "318 MB";
  const filename = `${inst.name.replace(/\s+/g, "_")}.${ext}`;

  const task = createTask("export", `Xuất ${inst.name} (.${ext})`, "Chuẩn bị nén tệp...");
  openTasks();

  for (let p = 0; p <= 100; p += 25) {
    updateTask(task, { progress: p, detail: `Đang nén dữ liệu... ${p}%` });
    await sleep(60);
  }

  finishTask(task, "done", `${filename} · ${sizeStr}`);
  showToastStatus(`Đã xuất "${filename}" thành công!`);
}

function openContextMenu(x, y, id) {
  const menu = $("#contextMenu");
  const inst = getInstance(id);
  delete menu.dataset.modIndex;
  menu.innerHTML = `
    <button data-ctx="${inst?.running ? "stop" : "play"}"><span class="glyph">${inst?.running ? "&#xE71A;" : "&#xE768;"}</span>${inst?.running ? "Stop" : "Play"}</button>
    <button data-ctx="edit"><span class="glyph">&#xE713;</span>Edit</button>
    <button data-ctx="rename"><span class="glyph">&#xE8AC;</span>Rename</button>
    <button data-ctx="duplicate"><span class="glyph">&#xE8C8;</span>Clone</button>
    <button data-ctx="export"><span class="glyph">&#xEDE1;</span>Export</button>
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
function showDialog({ title, body = "", primary = null, closeText = "Đóng", wide = false, onPrimary = null, onClose = null, footer = null }) {
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
        ${footer ?? `
          ${primary ? `<button class="btn accent" data-dialog-primary>${esc(primary)}</button>` : ""}
          <button class="btn" data-dialog-close>${esc(closeText)}</button>
        `}
      </div>
    </div>`;

  const panel = $(".dialog", host);
  const panelRect = panel.getBoundingClientRect();
  if (lastPointer.x || lastPointer.y) {
    panel.style.transformOrigin =
      `${clamp(lastPointer.x - panelRect.left, 0, panelRect.width)}px ${clamp(lastPointer.y - panelRect.top, 0, panelRect.height)}px`;
  }

  let closing = false;
  const onKey = (e) => {
    if (e.key === "Escape") {
      e.stopPropagation();
      close();
    }
  };
  window.addEventListener("keydown", onKey);

  const close = () => {
    if (closing) return;
    closing = true;
    window.removeEventListener("keydown", onKey);
    if (onClose) {
      try { onClose(); } catch (_) {}
    }
    host.classList.add("is-closing");
    setTimeout(() => {
      if (host.dataset.dialogId !== dialogId) return;
      host.hidden = true;
      host.innerHTML = "";
      host.classList.remove("is-closing");
      host.removeAttribute("data-dialog-id");
      closing = false;
    }, 170);
  };
  $$("[data-dialog-close]", host).forEach((btn) => { btn.onclick = close; });
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
    const host = $("#dialogHost");
    const existingDialog = !host.hidden && host.querySelector(".dialog");
    if (existingDialog) {
      const confirmLayer = document.createElement("div");
      confirmLayer.className = "dialog-sub-overlay";
      confirmLayer.style.cssText =
        "position:absolute;inset:0;background:rgba(4,8,14,0.72);backdrop-filter:blur(10px);display:flex;align-items:center;justify-content:center;z-index:100;border-radius:18px;";
      confirmLayer.innerHTML = `
        <div class="dialog" role="dialog" style="max-width:440px;box-shadow:0 18px 50px rgba(0,0,0,0.7);background:var(--dialog-bg,#0f1622);border:1px solid var(--stroke-strong);">
          <h2 class="dialog-title">${esc(title)}</h2>
          <div class="dialog-body"><p class="text-2">${esc(message)}</p></div>
          <div class="dialog-footer">
            <button class="btn accent" data-dialog-primary>${esc(confirmText)}</button>
            <button class="btn" data-dialog-close>Hủy</button>
          </div>
        </div>
      `;
      existingDialog.appendChild(confirmLayer);

      let settled = false;
      const onConfirmKey = (e) => {
        if (e.key === "Escape") {
          e.stopPropagation();
          done(false);
        }
      };
      window.addEventListener("keydown", onConfirmKey);

      const done = (value) => {
        if (settled) return;
        settled = true;
        window.removeEventListener("keydown", onConfirmKey);
        confirmLayer.remove();
        resolve(value);
      };

      confirmLayer.querySelector("[data-dialog-primary]").onclick = (e) => {
        e.stopPropagation();
        done(true);
      };
      confirmLayer.querySelector("[data-dialog-close]").onclick = (e) => {
        e.stopPropagation();
        done(false);
      };
      return;
    }

    let settled = false;
    const done = (value) => {
      if (settled) return;
      settled = true;
      d.close();
      resolve(value);
    };

    const d = showDialog({
      title,
      body: `<p class="text-2">${esc(message)}</p>`,
      primary: confirmText,
      closeText: "Hủy",
      onClose: () => done(false),
    });

    $("[data-dialog-primary]", d.host).onclick = () => done(true);
    $("[data-dialog-close]", d.host).onclick = () => done(false);
    d.host.addEventListener("click", (e) => { if (e.target === d.host) done(false); });
  });
}

/* ------------------------------------------------------------
   NEW INSTANCE DIALOG
   ------------------------------------------------------------ */
const INSTANCE_LOADERS = [
  { id: "vanilla", name: "Vanilla", sub: "Không mod", mark: "V" },
  { id: "fabric", name: "Fabric", sub: "Mod nhẹ, phổ biến", mark: "F" },
  { id: "neoforge", name: "NeoForge", sub: "Modpack lớn", mark: "N" },
];

const INSTANCE_VERSIONS = ["1.21.1", "1.21", "1.20.1", "1.20", "1.19.2"];

/** Chuẩn hóa tên/id thành slug thư mục. */
function slugifyInstance(value) {
  return String(value || "")
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

/** Slug id không trùng instance đã có (so cả id lẫn tên đã chuẩn hóa). */
function uniqueInstanceId(name) {
  const base = slugifyInstance(name) || "instance";
  const taken = new Set();
  state.instances.forEach((inst) => {
    taken.add(inst.id);
    taken.add(slugifyInstance(inst.id));
    taken.add(slugifyInstance(inst.name));
  });
  let id = base;
  let n = 2;
  while (taken.has(id)) id = `${base}-${n++}`;
  return id;
}

function openNewInstanceDialog() {
  let step = 1;
  let creating = false;
  const form = {
    name: "",
    loader: "fabric",
    version: "1.21.1",
    loaderVersion: "",
  };

  const wizardShell = `
    <div class="wizard">
      <ol class="wiz-steps" id="niSteps"></ol>
      <div class="wiz-body" id="niStepBody"></div>
      <div class="result-status" id="niBusy" hidden>
        <span class="spinner" style="color:var(--accent)"></span>
        <span id="niBusyText">Đang tạo instance...</span>
      </div>
      <div class="infobar error" id="niError" hidden><span class="glyph">&#xE7BA;</span><div id="niErrorText"></div></div>
    </div>`;

  const footer = `
    <div class="wiz-footer">
      <span class="wiz-counter" id="niStepCounter">Bước 1 / 3</span>
      <button class="btn" id="niBackBtn" disabled>Quay lại</button>
      <button class="btn" id="niCancelBtn" data-dialog-close>Hủy</button>
      <button class="btn accent" id="niNextBtn">Tiếp tục</button>
    </div>`;

  const dialog = showDialog({
    title: "Tạo instance mới",
    body: wizardShell,
    footer,
    wide: true,
  });

  const loaderInfo = () => INSTANCE_LOADERS.find((l) => l.id === form.loader) || INSTANCE_LOADERS[1];
  const instancePath = (id) =>
    `C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\${id}`;

  function stepBodyHtml() {
    if (step === 1) {
      return `
        <label class="field"><span>Tên instance</span>
          <input class="textbox" id="niName" maxlength="48" autocomplete="off"
            placeholder="VD: SkyBlock Fabric" value="${esc(form.name)}" />
        </label>
        <div class="field"><span>Mod loader</span>
          <div class="loader-grid" id="niLoaderGrid">
            ${INSTANCE_LOADERS.map((l) => `
              <button type="button" class="loader-card${form.loader === l.id ? " is-selected" : ""}"
                data-loader="${l.id}" aria-pressed="${form.loader === l.id}">
                <span class="loader-mark" data-mark="${l.id}">${l.mark}</span>
                <span class="loader-name">${l.name}</span>
                <span class="loader-sub">${l.sub}</span>
              </button>`).join("")}
          </div>
        </div>
        <p class="wiz-note">Bạn có thể đổi loader và phiên bản sau khi tạo.</p>`;
    }

    if (step === 2) {
      const loader = loaderInfo();
      return `
        <div class="field"><span>Phiên bản Minecraft</span>
          <div class="version-grid" id="niVersionGrid">
            ${INSTANCE_VERSIONS.map((v) => `
              <button type="button" class="version-card${form.version === v ? " is-selected" : ""}"
                data-version="${v}" aria-pressed="${form.version === v}">
                <span class="version-num">${v}</span>
                <span class="version-tag">Release</span>
              </button>`).join("")}
          </div>
        </div>
        ${form.loader === "vanilla" ? `
          <div class="wiz-note">Vanilla dùng jar gốc — không cần phiên bản loader.</div>` : `
          <label class="field"><span>Phiên bản ${loader.name} <em>(để trống = mới nhất)</em></span>
            <input class="textbox" id="niLoaderVersion" placeholder="latest" value="${esc(form.loaderVersion)}" />
          </label>`}
        <p class="wiz-note">Launcher sẽ tải manifest và assets tương ứng khi bạn bấm Tạo.</p>`;
    }

    const loader = loaderInfo();
    const id = uniqueInstanceId(form.name);
    return `
      <div class="review-card">
        <div class="review-row"><span class="review-key">Tên</span><span class="review-val">${esc(form.name)}</span></div>
        <div class="review-row"><span class="review-key">Loader</span><span class="review-val">${loader.name}</span></div>
        <div class="review-row"><span class="review-key">Minecraft</span><span class="review-val">${esc(form.version)}</span></div>
        ${form.loader === "vanilla" ? "" : `
          <div class="review-row"><span class="review-key">Loader version</span><span class="review-val">${esc(form.loaderVersion || "Mới nhất")}</span></div>`}
        <div class="review-row"><span class="review-key">Instance ID</span><span class="review-val mono">${esc(id)}</span></div>
      </div>
      <p class="wiz-note">Thư mục: <code>${esc(instancePath(id))}</code></p>`;
  }

  function renderWizard() {
    $("#niSteps").innerHTML = ["Thông tin", "Phiên bản", "Xem lại"].map((label, i) => {
      const num = i + 1;
      const cls = step === num ? "is-active" : step > num ? "is-done" : "";
      return `
        <li class="wiz-step ${cls}" data-wiz-step="${num}">
          <span class="wiz-dot">${step > num ? "✓" : num}</span>
          <span>${label}</span>
        </li>`;
    }).join("");

    $("#niStepBody").innerHTML = stepBodyHtml();
    $("#niStepCounter").textContent = `Bước ${step} / 3`;
    $("#niBackBtn").disabled = step === 1 || creating;
    const next = $("#niNextBtn");
    next.textContent = step === 3 ? "Tạo instance" : "Tiếp tục";
    next.disabled = creating;
    $("#niError").hidden = true;

    if (step === 1) {
      requestAnimationFrame(() => $("#niName")?.focus());
    }
  }

  // Giữ lựa chọn khi bấm card (không re-render để không mất focus ô tên).
  $("#niStepBody").addEventListener("click", (e) => {
    const loaderBtn = e.target.closest("[data-loader]");
    if (loaderBtn) {
      form.loader = loaderBtn.dataset.loader;
      $$("#niLoaderGrid .loader-card").forEach((btn) => {
        const on = btn === loaderBtn;
        btn.classList.toggle("is-selected", on);
        btn.setAttribute("aria-pressed", String(on));
      });
      return;
    }

    const versionBtn = e.target.closest("[data-version]");
    if (versionBtn) {
      form.version = versionBtn.dataset.version;
      $$("#niVersionGrid .version-card").forEach((btn) => {
        const on = btn === versionBtn;
        btn.classList.toggle("is-selected", on);
        btn.setAttribute("aria-pressed", String(on));
      });
    }
  });

  $("#niStepBody").addEventListener("input", (e) => {
    if (e.target.id === "niName") {
      form.name = e.target.value;
      $("#niError").hidden = true;
    }
  });

  async function createInstance() {
    if (creating) return;
    creating = true;
    $("#niBackBtn").disabled = true;
    $("#niNextBtn").disabled = true;
    $("#niCancelBtn").disabled = true;
    $("#niBusy").hidden = false;

    const id = uniqueInstanceId(form.name);
    const loader = loaderInfo();
    const task = createTask(
      "create",
      `Tạo ${form.name}`,
      `${loader.name} · MC ${form.version}`,
    );

    const stages = [
      { text: "Đang resolve version manifest...", progress: 28 },
      { text: "Đang cài đặt loader...", progress: 68 },
      { text: "Đang tạo thư mục instance...", progress: 92 },
    ];
    for (const stage of stages) {
      $("#niBusyText").textContent = stage.text;
      updateTask(task, { progress: stage.progress, detail: stage.text });
      await sleep(700);
    }

    const newInst = {
      id,
      name: form.name,
      mc: form.version,
      loader: form.loader,
      loaderVersion: form.loaderVersion.trim() || null,
      path: instancePath(id),
      lastPlayed: null,
      lastPlayedTs: 0,
      createdTs: Date.now(),
      settings: { ...DEFAULT_SETTINGS },
      mcOptions: { ...DEFAULT_MC_OPTIONS },
    };
    state.instances.unshift(newInst);
    state.instances[id] = newInst;
    state.mods[id] = [];
    state.worlds[id] = [];
    saveState();
    renderInstances();
    finishTask(task, "done", `Instance ID: ${id}`);
    showToastStatus(`Đã tạo instance "${form.name}".`);
    dialog.close();
  }

  $("#niNextBtn").addEventListener("click", async () => {
    if (creating) return;

    if (step === 1) {
      const input = $("#niName");
      form.name = input.value.trim();
      if (!form.name) {
        $("#niError").hidden = false;
        $("#niErrorText").textContent = "Tên instance không được để trống.";
        input.focus();
        return;
      }
      step = 2;
      renderWizard();
      return;
    }

    if (step === 2) {
      form.loaderVersion = $("#niLoaderVersion")?.value.trim() || "";
      step = 3;
      renderWizard();
      return;
    }

    await createInstance();
  });

  $("#niBackBtn").addEventListener("click", () => {
    if (step === 1 || creating) return;
    step -= 1;
    renderWizard();
  });

  $("#niCancelBtn").onclick = () => {
    if (!creating) dialog.close();
  };

  renderWizard();
}

/* ------------------------------------------------------------
   MODPACK & INSTANCE IMPORTER WIZARD
   ------------------------------------------------------------ */
function formatFileSize(bytes) {
  if (!bytes || bytes <= 0) return "0 B";
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

const IMPORT_PRESETS = [
  {
    id: "fabulously-optimized",
    name: "Fabulously Optimized",
    format: "mrpack",
    formatLabel: "Modrinth Modpack (.mrpack)",
    badgeClass: "mrpack",
    version: "1.21.1",
    loader: "fabric",
    loaderVersion: "0.16.5",
    icon: "grass_block",
    author: "RobotKoer",
    fileSize: "3.8 MB",
    fileName: "Fabulously.Optimized-1.21.1.mrpack",
    desc: "Tối ưu hóa FPS vượt trội, hỗ trợ shader mượt mà và bổ sung các tính năng đồ họa của OptiFine.",
    modCount: 64,
    ramMb: 4096,
    overrides: ["config/", "options.txt", "openloader/"],
    mods: ["sodium", "iris", "lithium", "ferrite-core", "immediatelyfast", "entityculling", "fabric-api", "cloth-config", "modmenu", "badoptimizations", "yacl"],
  },
  {
    id: "better-mc",
    name: "Better MC [FABRIC] - BMC4",
    format: "curseforge",
    formatLabel: "CurseForge Modpack (.zip)",
    badgeClass: "curseforge",
    version: "1.20.1",
    loader: "fabric",
    loaderVersion: "0.15.11",
    icon: "diamond_block",
    author: "SHXRKIE",
    fileSize: "286 MB",
    fileName: "Better-MC-Fabric-v28.5.zip",
    desc: "Trải nghiệm Minecraft cổ điển được nâng tầm với hàng trăm di tích, boss, biome và cơ chế phiêu lưu thế giới mở mới.",
    modCount: 185,
    ramMb: 6144,
    overrides: ["config/", "defaultconfigs/", "patchouli_books/"],
    mods: ["fabric-api", "architectury-api", "jei", "jade", "appleskin", "cloth-config", "yacl", "sodium", "iris", "xaeros-minimap", "continuity"],
  },
  {
    id: "crucial-2",
    name: "Crucial 2 - Vanilla+",
    format: "prism",
    formatLabel: "Prism / MultiMC Folder",
    badgeClass: "prism",
    version: "1.18.2",
    loader: "forge",
    loaderVersion: "40.2.14",
    icon: "bookshelf",
    author: "Vazkii",
    fileSize: "142 MB",
    fileName: "Crucial-2-1.18.2-instance.zip",
    desc: "Gói modpack Vanilla+ trau chuốt từ tác giả Vazkii, chú trọng tính gắn kết, mỹ thuật và lối chơi tự nhiên.",
    modCount: 92,
    ramMb: 4096,
    overrides: ["config/", "instance.cfg", "mmc-pack.json"],
    mods: ["botania", "create", "jei", "appleskin", "jade", "modernfix", "ferrite-core"],
  },
];

function parseCustomModpackFile(file) {
  const isMrpack = file.name.toLowerCase().endsWith(".mrpack");
  const cleanName = file.name.replace(/\.(mrpack|zip)$/i, "").replace(/[-_.]+/g, " ").trim();
  const titleName = cleanName ? cleanName.charAt(0).toUpperCase() + cleanName.slice(1) : "Custom Modpack";
  return {
    id: "custom-file",
    name: titleName,
    format: isMrpack ? "mrpack" : "curseforge",
    formatLabel: isMrpack ? "Modrinth Modpack (.mrpack)" : "CurseForge / Archive (.zip)",
    badgeClass: isMrpack ? "mrpack" : "curseforge",
    version: isMrpack ? "1.21.1" : "1.20.1",
    loader: "fabric",
    loaderVersion: isMrpack ? "0.16.5" : "0.15.11",
    icon: isMrpack ? "grass_block" : "diamond_block",
    author: "Tệp tải lên",
    fileSize: formatFileSize(file.size),
    fileName: file.name,
    desc: `Gói modpack nhập từ tệp "${file.name}". Toàn bộ manifest và cấu hình sẽ được giải nén tự động.`,
    modCount: isMrpack ? 45 : 78,
    ramMb: 4096,
    overrides: isMrpack ? ["config/", "options.txt"] : ["config/", "overrides/"],
    mods: isMrpack
      ? ["sodium", "iris", "lithium", "fabric-api", "cloth-config"]
      : ["fabric-api", "architectury-api", "jei", "jade", "appleskin"],
  };
}

function openImportWizard(initialFileOrPreset = null) {
  let step = 1;
  let customFile = initialFileOrPreset instanceof File ? initialFileOrPreset : null;
  let selectedPresetId = "fabulously-optimized";
  if (typeof initialFileOrPreset === "string" && IMPORT_PRESETS.some((p) => p.id === initialFileOrPreset)) {
    selectedPresetId = initialFileOrPreset;
  }

  function getActiveMeta() {
    if (customFile) {
      return parseCustomModpackFile(customFile);
    }
    return IMPORT_PRESETS.find((p) => p.id === selectedPresetId) || IMPORT_PRESETS[0];
  }

  const initialMeta = getActiveMeta();
  const formData = {
    name: initialMeta.name,
    icon: initialMeta.icon || "grass_block",
    ramMb: initialMeta.ramMb || 4096,
    javaPath: state.globalSettings.javaPath || "",
  };

  const wizardShell = `
    <div class="wizard">
      <ol class="wiz-steps" id="wizImportSteps"></ol>
      <div class="wiz-body" id="wizImportStepBody"></div>
      <div class="infobar error" id="wizImportError" hidden>
        <span class="glyph">&#xE7BA;</span><div id="wizImportErrorText"></div>
      </div>
    </div>`;

  const footer = `
    <div class="wiz-footer">
      <span class="wiz-counter" id="wizImportStepCounter">Bước 1 / 3</span>
      <button class="btn" id="wizImportBackBtn" disabled>Quay lại</button>
      <button class="btn" id="wizImportCancelBtn" data-dialog-close>Hủy</button>
      <button class="btn accent" id="wizImportNextBtn">Tiếp tục</button>
    </div>`;

  const dialog = showDialog({
    title: "Nhập Modpack & Instance",
    body: wizardShell,
    footer,
    wide: true,
  });

  const instancePath = (id) =>
    `C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\${id}`;

  function stepBodyHtml() {
    const meta = getActiveMeta();

    if (step === 1) {
      return `
        <div class="field">
          <span>Chọn gói Modpack mẫu</span>
          <div class="import-source-grid">
            ${IMPORT_PRESETS.map((p) => {
              const isSelected = !customFile && selectedPresetId === p.id;
              return `
                <button type="button" class="import-source-card ${isSelected ? "is-selected" : ""}" data-preset-id="${p.id}" aria-pressed="${isSelected ? "true" : "false"}">
                  <span class="import-source-badge ${p.badgeClass}">${p.format.toUpperCase()}</span>
                  <div class="import-source-title">${esc(p.name)}</div>
                  <div class="import-source-desc">${esc(p.desc)}</div>
                </button>
              `;
            }).join("")}
          </div>
        </div>

        <div class="field">
          <span>Hoặc tải gói modpack từ máy tính (.mrpack, .zip)</span>
          <button type="button" class="import-upload-zone ${customFile ? "is-selected" : ""}" id="wizImportUploadZone" aria-label="Tải gói modpack từ máy tính (.mrpack, .zip)">
            <span class="glyph import-upload-icon">&#xE898;</span>
            <div class="import-upload-title">${customFile ? `Đã chọn: ${esc(customFile.name)} (${formatFileSize(customFile.size)})` : "Nhấp để duyệt tệp hoặc kéo thả tệp vào đây"}</div>
            <div class="import-upload-sub">${customFile ? "Nhấp để đổi tệp khác" : "Hỗ trợ Modrinth (.mrpack), CurseForge (.zip) hoặc Prism Launcher (.zip)"}</div>
          </button>
          <input type="file" id="wizImportLocalFile" accept=".mrpack,.zip" hidden />
        </div>

        <div class="import-meta-box">
          <div class="import-meta-row">
            <span class="import-meta-label">Định dạng gói:</span>
            <span class="import-meta-val"><span class="import-source-badge ${meta.badgeClass}">${meta.format.toUpperCase()}</span> ${esc(meta.formatLabel)}</span>
          </div>
          <div class="import-meta-row">
            <span class="import-meta-label">Phiên bản game:</span>
            <span class="import-meta-val mono">Minecraft ${esc(meta.version)} · ${esc(meta.loader.toUpperCase())} ${esc(meta.loaderVersion || "")}</span>
          </div>
          <div class="import-meta-row">
            <span class="import-meta-label">Dung lượng / Tác giả:</span>
            <span class="import-meta-val">${esc(meta.fileSize)} · ${esc(meta.author)}</span>
          </div>
          <div class="import-meta-row">
            <span class="import-meta-label">Số mod đính kèm:</span>
            <span class="import-meta-val">${meta.modCount} mods</span>
          </div>
          <div class="import-meta-row">
            <span class="import-meta-label">Thư mục Overrides:</span>
            <span class="import-meta-val mono">${esc(meta.overrides.join(", "))}</span>
          </div>
        </div>
      `;
    }

    if (step === 2) {
      return `
        <label class="field"><span>Tên instance</span>
          <input class="textbox" id="wizImportName" maxlength="48" autocomplete="off"
            placeholder="VD: Fabulously Optimized" value="${esc(formData.name)}" />
        </label>

        <div class="field"><span>Biểu tượng Block 3D đại diện</span>
          <div class="block-icon-grid" id="wizImportBlockGrid">
            ${BLOCK_ICONS.map((b) => {
              const isSelected = formData.icon === b.id;
              return `
                <button type="button" class="block-icon-option ${isSelected ? "is-selected" : ""}" data-block-id="${b.id}" aria-label="${b.name}" aria-pressed="${isSelected ? "true" : "false"}">
                  <img src="assets/blocks/${b.id}.png" alt="${b.name}" />
                  <span class="block-icon-label">${b.name}</span>
                </button>
              `;
            }).join("")}
          </div>
        </div>

        <div class="field">
          <div class="mc-ctrl-label-row">
            <label for="wizImportRam">Bộ nhớ RAM tối đa</label>
            <span id="wizImportRamLabel">${formData.ramMb} MB (${(formData.ramMb / 1024).toFixed(1)} GB)</span>
          </div>
          <input type="range" min="1024" max="16384" step="512" id="wizImportRam" value="${formData.ramMb}" />
          <p class="wiz-note">Khuyến nghị tối thiểu 4GB (4096 MB) để trải nghiệm modpack mượt mà nhất.</p>
        </div>

        <label class="field"><span>Đường dẫn Java Runtime <em>(để trống = mặc định hệ thống)</em></span>
          <input class="textbox" id="wizImportJava" placeholder="${esc(state.globalSettings.javaPath || "Mặc định (Java 21 LTS)")}" value="${esc(formData.javaPath)}" />
        </label>
      `;
    }

    const id = uniqueInstanceId(formData.name);
    return `
      <div class="review-card">
        <div class="review-row">
          <span class="review-key">Tên Instance</span>
          <span class="review-val" style="display:flex;align-items:center;gap:8px;">
            <img src="assets/blocks/${formData.icon}.png" width="22" height="22" style="image-rendering:pixelated;vertical-align:middle;" />
            <strong>${esc(formData.name)}</strong>
          </span>
        </div>
        <div class="review-row">
          <span class="review-key">Gói nguồn</span>
          <span class="review-val">${esc(meta.name)} (${esc(meta.formatLabel)})</span>
        </div>
        <div class="review-row">
          <span class="review-key">Minecraft & Loader</span>
          <span class="review-val mono">${esc(meta.version)} · ${esc(meta.loader)} ${esc(meta.loaderVersion || "")}</span>
        </div>
        <div class="review-row">
          <span class="review-key">Số mod & Overrides</span>
          <span class="review-val">${meta.modCount} mods · ${esc(meta.overrides.join(", "))}</span>
        </div>
        <div class="review-row">
          <span class="review-key">Cấu hình RAM</span>
          <span class="review-val mono">${formData.ramMb} MB (${(formData.ramMb / 1024).toFixed(1)} GB)</span>
        </div>
        <div class="review-row">
          <span class="review-key">Java Runtime</span>
          <span class="review-val">${esc(formData.javaPath || "Mặc định hệ thống")}</span>
        </div>
        <div class="review-row">
          <span class="review-key">Thư mục cài đặt</span>
          <span class="review-val mono" style="font-size:11px;">${esc(instancePath(id))}</span>
        </div>
      </div>
      <p class="wiz-note">Toàn bộ mod và cấu hình sẽ được giải nén và thiết lập tự động sau khi bấm <strong>Bắt đầu Import</strong>.</p>
    `;
  }

  function renderWizard() {
    $("#wizImportSteps").innerHTML = ["Nguồn & Gói", "Tùy biến", "Xem lại"].map((label, i) => {
      const num = i + 1;
      const cls = step === num ? "is-active" : step > num ? "is-done" : "";
      return `
        <li class="wiz-step ${cls}" data-wiz-step="${num}">
          <span class="wiz-dot">${step > num ? "✓" : num}</span>
          <span>${label}</span>
        </li>`;
    }).join("");

    $("#wizImportStepBody").innerHTML = stepBodyHtml();
    $("#wizImportStepCounter").textContent = `Bước ${step} / 3`;
    $("#wizImportBackBtn").disabled = step === 1;
    const next = $("#wizImportNextBtn");
    next.textContent = step === 3 ? "Bắt đầu Import" : "Tiếp tục";
    $("#wizImportError").hidden = true;

    if (step === 1) {
      $$(".import-source-card", dialog.host).forEach((card) => {
        const selectCard = () => {
          customFile = null;
          selectedPresetId = card.dataset.presetId;
          const p = IMPORT_PRESETS.find((x) => x.id === selectedPresetId);
          if (p) {
            formData.name = p.name;
            formData.icon = p.icon;
            formData.ramMb = p.ramMb;
          }
          renderWizard();
        };
        card.addEventListener("click", selectCard);
        card.addEventListener("keydown", (e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            selectCard();
          }
        });
      });

      const uploadZone = $("#wizImportUploadZone");
      const localInput = $("#wizImportLocalFile");
      if (uploadZone && localInput) {
        const triggerUpload = () => localInput.click();
        uploadZone.addEventListener("click", triggerUpload);
        uploadZone.addEventListener("keydown", (e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            triggerUpload();
          }
        });
        localInput.addEventListener("change", (e) => {
          const f = e.target.files?.[0];
          if (f) {
            customFile = f;
            const parsed = parseCustomModpackFile(f);
            formData.name = parsed.name;
            formData.icon = parsed.icon;
            formData.ramMb = parsed.ramMb;
            renderWizard();
          }
        });

        uploadZone.addEventListener("dragover", (e) => {
          e.preventDefault();
          uploadZone.classList.add("is-dragover");
        });
        uploadZone.addEventListener("dragleave", (e) => {
          e.preventDefault();
          uploadZone.classList.remove("is-dragover");
        });
        uploadZone.addEventListener("drop", (e) => {
          e.preventDefault();
          uploadZone.classList.remove("is-dragover");
          const f = e.dataTransfer.files?.[0];
          if (f) {
            const lower = f.name.toLowerCase();
            if (lower.endsWith(".mrpack") || lower.endsWith(".zip")) {
              customFile = f;
              const parsed = parseCustomModpackFile(f);
              formData.name = parsed.name;
              formData.icon = parsed.icon;
              formData.ramMb = parsed.ramMb;
              renderWizard();
            } else {
              $("#wizImportError").hidden = false;
              $("#wizImportErrorText").textContent = "Chỉ hỗ trợ file modpack định dạng .mrpack hoặc .zip.";
            }
          }
        });
      }
    }

    if (step === 2) {
      const nameInput = $("#wizImportName");
      if (nameInput) {
        nameInput.focus();
        nameInput.select();
        nameInput.addEventListener("input", () => {
          $("#wizImportError").hidden = true;
        });
        nameInput.addEventListener("keydown", (e) => {
          if (e.key === "Enter") {
            e.preventDefault();
            $("#wizImportNextBtn")?.click();
          }
        });
      }

      $$(".block-icon-option", dialog.host).forEach((opt) => {
        const selectIcon = () => {
          formData.icon = opt.dataset.blockId;
          $$(".block-icon-option", dialog.host).forEach((o) => {
            const isSel = o === opt;
            o.classList.toggle("is-selected", isSel);
            o.setAttribute("aria-pressed", isSel ? "true" : "false");
          });
        };
        opt.addEventListener("click", selectIcon);
        opt.addEventListener("keydown", (e) => {
          if (e.key === "Enter" || e.key === " ") {
            e.preventDefault();
            selectIcon();
          }
        });
      });

      const ramSlider = $("#wizImportRam");
      const ramLabel = $("#wizImportRamLabel");
      if (ramSlider && ramLabel) {
        ramSlider.addEventListener("input", (e) => {
          formData.ramMb = parseInt(e.target.value, 10);
          ramLabel.textContent = `${formData.ramMb} MB (${(formData.ramMb / 1024).toFixed(1)} GB)`;
        });
      }

      const javaInput = $("#wizImportJava");
      if (javaInput) {
        javaInput.addEventListener("input", (e) => {
          formData.javaPath = e.target.value.trim();
        });
      }
    }
  }

  $("#wizImportNextBtn").addEventListener("click", () => {
    if (step === 1) {
      step = 2;
      renderWizard();
      return;
    }

    if (step === 2) {
      const input = $("#wizImportName");
      const trimmed = (input ? input.value : formData.name).trim();
      if (!trimmed) {
        $("#wizImportError").hidden = false;
        $("#wizImportErrorText").textContent = "Tên instance không được để trống.";
        input?.focus();
        return;
      }

      const duplicate = state.instances.some(
        (i) => i.name.trim().toLowerCase() === trimmed.toLowerCase()
      );
      if (duplicate) {
        $("#wizImportError").hidden = false;
        $("#wizImportErrorText").textContent = `Tên instance "${trimmed}" đã tồn tại. Vui lòng chọn tên khác.`;
        input?.focus();
        return;
      }

      formData.name = trimmed;
      step = 3;
      renderWizard();
      return;
    }

    if (step === 3) {
      const meta = getActiveMeta();
      dialog.close();
      runImportProcess(formData, meta);
    }
  });

  $("#wizImportBackBtn").addEventListener("click", () => {
    if (step === 1) return;
    if (step === 2) {
      const input = $("#wizImportName");
      if (input) formData.name = input.value.trim();
    }
    step -= 1;
    renderWizard();
  });

  renderWizard();
}

async function runImportProcess(formData, sourceMeta) {
  const task = createTask("import", `Nhập modpack: ${formData.name}`, "Đang đọc và phân tích manifest...");
  openTasks();

  const stages = [
    { progress: 25, detail: "Đang đọc manifest và cấu trúc gói modpack..." },
    { progress: 50, detail: "Đang giải nén overrides (config, options, resourcepacks)..." },
    { progress: 75, detail: "Đang resolve dependencies từ Modrinth / CurseForge..." },
    { progress: 100, detail: "Đang cài đặt các file mod và hoàn tất instance..." },
  ];

  for (const stage of stages) {
    updateTask(task, { progress: stage.progress, detail: stage.detail });
    await sleep(550);
  }

  const id = uniqueInstanceId(formData.name);
  const newInst = {
    id,
    name: formData.name,
    mc: sourceMeta.version,
    loader: sourceMeta.loader,
    loaderVersion: sourceMeta.loaderVersion || null,
    icon: formData.icon || "grass_block",
    path: `C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\${id}`,
    lastPlayed: null,
    lastPlayedTs: 0,
    playtimeMin: 0,
    createdTs: Date.now(),
    packId: sourceMeta.id || null,
    shaders: [],
    settings: {
      ...DEFAULT_SETTINGS,
      ramMb: formData.ramMb,
      javaPath: (formData.javaPath || "").trim() || state.globalSettings.javaPath,
    },
    mcOptions: { ...DEFAULT_MC_OPTIONS },
  };

  state.instances.unshift(newInst);
  state.instances[id] = newInst;
  state.mods[id] = [];
  state.worlds[id] = [];
  state.screenshots[id] = [];

  (sourceMeta.mods || []).forEach((slug) => {
    const project = catalogProject(slug);
    if (project) {
      resolveDependencies(project, newInst).forEach((dep) => addInstalledMod(dep, newInst));
      addInstalledMod(project, newInst);
    } else {
      state.mods[id].push({
        projectId: slug,
        file: `${slug}-${sourceMeta.version}.jar`,
        version: "1.0.0",
        enabled: true,
        update: null,
      });
    }
  });

  saveState();
  renderInstances();
  finishTask(task, "done", `Đã nhập "${formData.name}" (${(state.mods[id] || []).length} mods)`);
  showToastStatus(`Đã nhập "${formData.name}" thành công!`);
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
        <img class="mod-icon" src="${p.icon ? esc(p.icon) : `https://cdn.modrinth.com/data/${p.id}/icon.png`}" alt="${esc(p.name)}" data-fallback="${esc(p.name[0])}" />
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

  countUp($("#mbCount", dialog.host), shown.length, {
    suffix: ` / ${TOTAL_RESULTS.toLocaleString("en-US")} results`,
  });

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
  if (deps.length > 0) {
    const depNames = deps.map((d) => d.name).join(", ");
    const ok = await confirmDialog(
      "Cài đặt kèm dependencies?",
      `Mod "${project.name}" yêu cầu thư viện bổ trợ: ${depNames}. Bạn có muốn cài đặt kèm không?`,
      "Cài đặt kèm"
    );
    if (!ok) return;
  }

  const queue = [project, ...deps];
  const row = $$(".mod-result", dialog.host).find((r) => r.dataset.slug === project.slug);
  const pipeline = row ? $(".pipeline", row) : null;
  const depBox = row ? $(".pipeline-deps", row) : null;
  const stages = row ? pipelineStages(row) : null;

  state.modBrowser.busy = true;
  state.modBrowser.installingSlug = project.slug;
  const task = createTask(
    "install",
    `Cài ${project.name}`,
    deps.length ? `Kèm ${deps.length} dependency` : "Từ Modrinth",
  );

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
      updateTask(task, { progress: overall, detail: status.textContent });
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
  finishTask(task, "done", status.textContent);

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
   ACCOUNT MODALS & DEVICE CODE FLOW
   ------------------------------------------------------------ */
let msCountdownInterval = null;
let msCountdownRemaining = 900;

function generateMsUserCode() {
  const chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
  const p1 = Array.from({ length: 4 }, () => chars[Math.floor(Math.random() * chars.length)]).join("");
  const p2 = Array.from({ length: 4 }, () => chars[Math.floor(Math.random() * chars.length)]).join("");
  return `${p1}-${p2}`;
}

function updateMsTimerDisplay() {
  const timerEl = $("#msCountdownTimer");
  if (!timerEl) return;
  const mins = Math.floor(msCountdownRemaining / 60);
  const secs = msCountdownRemaining % 60;
  timerEl.textContent = `${String(mins).padStart(2, "0")}:${String(secs).padStart(2, "0")}`;
}

function openMsAuthModal() {
  const host = $("#msAuthModalHost");
  if (!host) return;

  const code = generateMsUserCode();
  const codeEl = $("#msUserCode");
  if (codeEl) codeEl.textContent = code;

  msCountdownRemaining = 900;
  updateMsTimerDisplay();

  const statusEl = $("#msAuthStatus");
  if (statusEl) statusEl.textContent = "Đang chờ bạn nhập mã và xác thực tại microsoft.com/link...";

  if (msCountdownInterval) clearInterval(msCountdownInterval);
  msCountdownInterval = setInterval(() => {
    msCountdownRemaining--;
    if (msCountdownRemaining <= 0) {
      clearInterval(msCountdownInterval);
      msCountdownInterval = null;
      msCountdownRemaining = 0;
      updateMsTimerDisplay();
      if (statusEl) statusEl.textContent = "Mã kết nối đã hết hạn. Vui lòng đóng và tạo mã mới.";
      const copyBtn = $("#msCopyCodeBtn");
      if (copyBtn) copyBtn.disabled = true;
      const simBtn = $("#msSimulateSuccessBtn");
      if (simBtn) simBtn.disabled = true;
      return;
    }
    updateMsTimerDisplay();
  }, 1000);

  const copyBtn = $("#msCopyCodeBtn");
  if (copyBtn) copyBtn.disabled = false;
  const simBtn = $("#msSimulateSuccessBtn");
  if (simBtn) simBtn.disabled = false;

  host.hidden = false;
  setTimeout(() => $("#msCopyCodeBtn")?.focus(), 50);
}

function closeMsAuthModal() {
  const host = $("#msAuthModalHost");
  if (!host) return;
  if (msCountdownInterval) {
    clearInterval(msCountdownInterval);
    msCountdownInterval = null;
  }
  host.hidden = true;
}

async function copyMsCode() {
  const code = $("#msUserCode")?.textContent?.trim() || "V4Z8-9MC2";
  try {
    if (navigator.clipboard?.writeText) {
      await navigator.clipboard.writeText(code);
    } else {
      const ta = document.createElement("textarea");
      ta.value = code;
      document.body.appendChild(ta);
      ta.select();
      document.execCommand("copy");
      ta.remove();
    }
  } catch (_) {}
  showToastStatus(`Đã sao chép mã kết nối: ${code}`);
}

function simulateMsAuthSuccess() {
  closeMsAuthModal();

  const base = "MCPlayer";
  let name = base;
  let n = 2;
  while (state.accounts.some((a) => a.name.toLowerCase() === name.toLowerCase())) {
    name = base + n++;
  }

  const hexPart = Math.random().toString(16).substring(2, 6);
  const newAccount = {
    id: "acc-" + Date.now(),
    name,
    kind: "Microsoft",
    active: state.accounts.length === 0,
    skinId: "alex",
    uuid: `c069-42fb-8501-${hexPart}`,
    tokenStatus: "Token: Hợp lệ (14 ngày)",
    capeId: "vanilla",
  };

  state.accounts.push(newAccount);
  saveState();
  renderAccounts();
  updateDock();
  showToastStatus(`Đã thêm tài khoản Microsoft "${name}".`);
  accountStatus(`Đã thêm tài khoản Microsoft "${name}".`);
}

function openOfflineModal() {
  const host = $("#offlineModalHost");
  if (!host) return;
  const input = $("#offlineUsernameInput");
  if (input) input.value = "";
  const errBox = $("#offlineUsernameError");
  if (errBox) errBox.hidden = true;

  const steveRadio = $("#offlineSkinSteve");
  if (steveRadio) steveRadio.checked = true;
  $$(".skin-select-chip", host).forEach((chip) => {
    chip.classList.toggle("is-selected", chip.getAttribute("for") === "offlineSkinSteve");
  });

  host.hidden = false;
  setTimeout(() => input?.focus(), 50);
}

function closeOfflineModal() {
  const host = $("#offlineModalHost");
  if (!host) return;
  host.hidden = true;
}

function validateMinecraftUsername(name) {
  if (!name || name.trim().length === 0) {
    return "Tên người chơi không được để trống.";
  }
  const trimmed = name.trim();
  if (trimmed.length < 3 || trimmed.length > 16) {
    return "Tên người chơi phải từ 3 đến 16 ký tự.";
  }
  if (!/^[a-zA-Z0-9_]+$/.test(trimmed)) {
    return "Tên chỉ được chứa chữ cái, số và dấu gạch dưới (_).";
  }
  if (state.accounts.some((a) => a.name.toLowerCase() === trimmed.toLowerCase())) {
    return "Tài khoản này đã tồn tại.";
  }
  return null;
}

function submitOfflineAccount() {
  const input = $("#offlineUsernameInput");
  const rawName = input ? input.value.trim() : "";
  const errorMsg = validateMinecraftUsername(rawName);
  const errorBox = $("#offlineUsernameError");
  const errorText = $("#offlineUsernameErrorText");

  if (errorMsg) {
    if (errorBox) errorBox.hidden = false;
    if (errorText) errorText.textContent = errorMsg;
    input?.focus();
    return false;
  }

  if (errorBox) errorBox.hidden = true;

  const isAlex = $("#offlineSkinAlex")?.checked;
  const selectedSkin = isAlex ? "alex" : "steve";
  const hexPart = Math.random().toString(16).substring(2, 6);

  const newAccount = {
    id: "acc-" + Date.now(),
    name: rawName,
    kind: "Offline",
    active: state.accounts.length === 0,
    skinId: selectedSkin,
    uuid: `b62a-89f1-4890-${hexPart}`,
    tokenStatus: "Offline session",
    capeId: "none",
  };

  state.accounts.push(newAccount);
  saveState();
  renderAccounts();
  updateDock();
  closeOfflineModal();
  showToastStatus(`Đã thêm tài khoản offline "${rawName}".`);
  accountStatus(`Đã thêm tài khoản offline "${rawName}".`);
  return true;
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
    { icon: "E74C", title: "Mods", sub: "Tìm kiếm & cài đặt mod Minecraft", keywords: "mods mod tải cài modrinth curseforge", run: () => showScreen("mods") },
    { icon: "EB9F", title: "Resource Packs", sub: "Gói tài nguyên & texture", keywords: "resourcepack texture gói tài nguyên", run: () => showScreen("resourcepacks") },
    { icon: "E706", title: "Shaders", sub: "Tìm kiếm & cài đặt shaderpack", keywords: "shader shaders iris optifine đồ họa graphics", run: () => showScreen("shaders") },
    { icon: "EA69", title: "Data Packs", sub: "Gói dữ liệu tùy biến cơ chế & biome", keywords: "datapack data pack gói dữ liệu", run: () => showScreen("datapacks") },
    { icon: "E8B5", title: "Modpacks", sub: "Bộ sưu tập modpack tạo instance chơi ngay", keywords: "modpack bộ sưu tập", run: () => showScreen("modpacks") },
    { icon: "E790", title: "Skins", sub: "Đổi skin cho tài khoản", keywords: "skin nhân vật ngoại hình", run: () => showScreen("skins") },
    { icon: "E77B", title: "Accounts", sub: "Mở trang tài khoản", keywords: "account tài khoản", run: () => showScreen("accounts") },
    { icon: "E823", title: "Hoạt động", sub: "Tác vụ đang chạy & lịch sử", keywords: "activity task hoạt động tác vụ", run: openTasks },
    { icon: "E710", title: "Tạo instance mới", sub: "Mở dialog New Instance", keywords: "new tạo instance", run: openNewInstanceDialog },
    { icon: "E8B5", title: "Import Modpack / Instance", sub: "Nhập instance từ file .mrpack, .zip hoặc preset", keywords: "import nhập modpack mrpack zip prism curseforge instance", run: () => openImportWizard() },
    { icon: "E713", title: "Settings", sub: "Java, RAM và giao diện", keywords: "settings cài đặt java ram", run: () => showScreen("settings") },
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
   WAVE 5 & 7 — DISCOVER (MODPACKS & SHADERS)
   ------------------------------------------------------------ */
function filteredDiscoverPacks() {
  const query = (state.discover.query || "").trim().toLowerCase();
  return DISCOVER_PACKS.filter((pack) => {
    if (state.discover.loader !== "all" && pack.loader !== state.discover.loader) return false;
    if (!query) return true;
    return `${pack.name} ${pack.author} ${pack.desc} ${pack.tag}`.toLowerCase().includes(query);
  });
}

function filteredDiscoverShaders() {
  const query = (state.shaders.query || "").trim().toLowerCase();
  const style = state.shaders.style || "all";
  return SHADER_PACKS.filter((shader) => {
    if (style !== "all" && shader.style !== style) return false;
    if (!query) return true;
    return `${shader.name} ${shader.author} ${shader.desc} ${shader.styleLabel} ${shader.style}`.toLowerCase().includes(query);
  });
}

function isShaderInstalled(shaderId, instanceId) {
  const inst = getInstance(instanceId);
  return !!inst?.shaders?.some((s) => s.id === shaderId || s.slug === shaderId);
}

/** Dependency của cả pack, bỏ các mod đã nằm trong danh sách chính. */
function packDependencyProjects(pack) {
  if (!pack || pack.loader !== "fabric") return [];
  const base = new Set(pack.mods);
  const slugs = new Set();
  pack.mods.forEach((slug) => {
    (DEPENDENCIES[slug] || []).forEach((dep) => {
      if (!base.has(dep)) slugs.add(dep);
    });
  });
  return [...slugs].map(catalogProject).filter(Boolean);
}

function openModpackInspectorDrawer(pack) {
  if (!pack) return;
  const host = $("#modpackInspectorHost");
  if (!host) return;

  const isInstalled = state.instances.some((inst) => inst.packId === pack.id || inst.name === pack.name);

  const banner = $("#modpackInspectorBanner");
  if (banner) {
    banner.src = pack.image || "assets/modpacks/performance-banner.jpg";
    banner.alt = pack.name;
  }
  const icon = $("#modpackInspectorIcon");
  if (icon) {
    icon.src = pack.icon || "assets/modpacks/performance-icon.png";
    icon.alt = pack.name;
  }
  const title = $("#modpackInspectorTitle");
  if (title) title.textContent = pack.name;

  const author = $("#modpackInspectorAuthor");
  if (author) author.textContent = pack.author;

  const loaderTag = $("#modpackInspectorLoader");
  if (loaderTag) loaderTag.textContent = pack.loader === "neoforge" ? "NeoForge" : "Fabric";

  const mcTag = $("#modpackInspectorMc");
  if (mcTag) mcTag.textContent = pack.mc;

  const modCountEl = $("#modpackInspectorModCount");
  if (modCountEl) modCountEl.textContent = `${pack.modCount || (pack.mods ? pack.mods.length : 30)} mods`;

  const downloadsEl = $("#modpackInspectorDownloads");
  if (downloadsEl) downloadsEl.textContent = `${pack.downloads} lượt tải`;

  const sizeEl = $("#modpackInspectorSize");
  if (sizeEl) sizeEl.textContent = pack.size || "145 MB";

  const descEl = $("#modpackInspectorDesc");
  if (descEl) descEl.textContent = pack.longDesc || pack.desc;

  // Version Select
  const versionSelect = $("#modpackInspectorVersionSelect");
  if (versionSelect) {
    const versions = pack.versions || [
      { version: "1.0.0", mc: pack.mc, loader: pack.loader === "neoforge" ? "NeoForge" : "Fabric" }
    ];
    versionSelect.innerHTML = versions.map((v) => `
      <option value="${esc(v.version)}">v${esc(v.version)} (${esc(v.mc)} · ${esc(v.loader)})</option>
    `).join("");
  }

  // Changelog
  const changelogEl = $("#modpackInspectorChangelog");
  if (changelogEl) {
    const items = pack.changelog || [
      "Tối ưu hóa hiệu năng render",
      "Khắc phục sự cố tương thích",
      "Cập nhật các mod thành phần lên bản mới nhất"
    ];
    changelogEl.innerHTML = items.map((c) => `
      <div class="modpack-changelog-item">• ${esc(c)}</div>
    `).join("");
  }

  // Mods chips
  const modsListEl = $("#modpackInspectorModList");
  const modsCountHeader = $("#modpackInspectorModsHeaderCount");
  if (modsListEl) {
    const mods = pack.mods || [];
    if (modsCountHeader) modsCountHeader.textContent = mods.length;
    modsListEl.innerHTML = mods.map((m) => `
      <span class="modpack-mod-chip">
        <span class="glyph">&#xE74C;</span>
        <span>${esc(m)}</span>
      </span>
    `).join("");
  }

  // Install button
  const installBtn = $("#modpackInspectorInstallBtn");
  const installText = $("#modpackInspectorInstallText");
  if (installBtn && installText) {
    installText.textContent = isInstalled ? "Đã cài đặt" : "Cài đặt & Tạo Instance";
    installBtn.onclick = () => {
      closeModpackInspectorDrawer();
      openModpackInstallModal(pack);
    };
  }

  // Close triggers
  const closeBtn = $("#modpackInspectorClose");
  if (closeBtn) closeBtn.onclick = () => closeModpackInspectorDrawer();
  const scrim = $("#modpackInspectorScrim");
  if (scrim) scrim.onclick = () => closeModpackInspectorDrawer();

  host.hidden = false;
  requestAnimationFrame(() => host.classList.add("is-open"));
}

function closeModpackInspectorDrawer() {
  const host = $("#modpackInspectorHost");
  if (!host) return;
  host.classList.remove("is-open");
  setTimeout(() => {
    if (!host.classList.contains("is-open")) host.hidden = true;
  }, 200);
}

let modpackInstallTimer = null;
let isModpackInstallCancelled = false;

function openModpackInstallModal(pack, sourceBtn) {
  if (!pack) return;
  const host = $("#modpackInstallModalHost");
  if (!host) return;

  isModpackInstallCancelled = false;
  if (modpackInstallTimer) clearTimeout(modpackInstallTimer);

  const title = $("#modpackInstallModalTitle");
  if (title) title.textContent = `Cài đặt: ${pack.name}`;

  const subtitle = $("#modpackInstallModalSubtitle");
  if (subtitle) subtitle.textContent = `Modpack · MC ${pack.mc} · ${pack.loader === "neoforge" ? "NeoForge" : "Fabric"}`;

  const step1 = $("#modpackStepManifest");
  const step2 = $("#modpackStepMods");
  const step3 = $("#modpackStepInstance");
  if (step1) step1.className = "modpack-step is-active";
  if (step2) step2.className = "modpack-step";
  if (step3) step3.className = "modpack-step";

  const progressFill = $("#modpackProgressFill");
  const progressText = $("#modpackProgressText");
  const progressDetail = $("#modpackProgressDetail");
  if (progressFill) progressFill.style.width = "0%";
  if (progressText) progressText.textContent = "0%";
  if (progressDetail) progressDetail.textContent = "Đang tải manifest và overrides...";

  const cancelBtn = $("#modpackInstallCancelBtn");
  if (cancelBtn) {
    cancelBtn.disabled = false;
    cancelBtn.onclick = () => cancelModpackInstall();
  }
  const scrim = $("#modpackInstallScrim");
  if (scrim) {
    scrim.onclick = () => cancelModpackInstall();
  }

  host.hidden = false;
  requestAnimationFrame(() => host.classList.add("is-open"));

  // Phase 1: Manifest & Overrides (0% -> 35%)
  modpackInstallTimer = setTimeout(() => {
    if (isModpackInstallCancelled) return;
    if (progressFill) progressFill.style.width = "35%";
    if (progressText) progressText.textContent = "35%";
    if (progressDetail) progressDetail.textContent = "Đã nạp manifest. Bắt đầu tải danh sách mod...";
    if (step1) step1.className = "modpack-step is-done";
    if (step2) step2.className = "modpack-step is-active";

    // Phase 2: Download mods (35% -> 80%)
    modpackInstallTimer = setTimeout(() => {
      if (isModpackInstallCancelled) return;
      if (progressFill) progressFill.style.width = "80%";
      if (progressText) progressText.textContent = "80%";
      const modCount = pack.modCount || (pack.mods ? pack.mods.length : 30);
      if (progressDetail) progressDetail.textContent = `Đang tải ${modCount} mods & kiểm tra thư viện...`;
      if (step2) step2.className = "modpack-step is-done";
      if (step3) step3.className = "modpack-step is-active";

      // Phase 3: Create Instance (80% -> 100%)
      modpackInstallTimer = setTimeout(() => {
        if (isModpackInstallCancelled) return;
        if (progressFill) progressFill.style.width = "100%";
        if (progressText) progressText.textContent = "100%";
        if (progressDetail) progressDetail.textContent = "Hoàn tất tạo instance mới!";
        if (step3) step3.className = "modpack-step is-done";

        // Create new instance
        const id = uniqueInstanceId(pack.name);
        const inst = {
          id,
          name: pack.name,
          mc: pack.mc || "1.21.1",
          loader: pack.loader || "fabric",
          loaderVersion: pack.loaderVersion || "0.19.5",
          path: `C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\${id}`,
          lastPlayed: null,
          lastPlayedTs: 0,
          playtimeMin: 0,
          createdTs: Date.now(),
          packId: pack.id,
          shaders: [],
          settings: {
            ...DEFAULT_SETTINGS,
            ramMb: state.globalSettings.ramMb,
            javaPath: state.globalSettings.javaPath,
          },
          mcOptions: { ...DEFAULT_MC_OPTIONS },
        };
        state.instances.unshift(inst);
        state.instances[id] = inst;
        state.mods[id] = (pack.mods || []).map((m, idx) => ({
          id: `mod-${idx}`,
          projectId: m.toLowerCase().replace(/\s+/g, "-"),
          file: `${m.toLowerCase().replace(/\s+/g, "-")}.jar`,
          name: m,
          version: "1.0.0",
          enabled: true,
        }));
        state.worlds[id] = [];
        saveState();
        renderInstances();
        renderContentPage("modpacks");

        if (sourceBtn) {
          sourceBtn.textContent = "Đã cài đặt";
          sourceBtn.classList.add("is-installed");
        }

        modpackInstallTimer = setTimeout(() => {
          closeModpackInstallModal();
          showToastStatus(`Đã cài đặt thành công ${pack.name} — instance sẵn sàng.`);
        }, 180);
      }, 160);
    }, 180);
  }, 160);
}

function cancelModpackInstall() {
  isModpackInstallCancelled = true;
  if (modpackInstallTimer) {
    clearTimeout(modpackInstallTimer);
    modpackInstallTimer = null;
  }
  closeModpackInstallModal();
  showToastStatus("Đã hủy cài đặt modpack.");
}

function closeModpackInstallModal() {
  const host = $("#modpackInstallModalHost");
  if (!host) return;
  host.classList.remove("is-open");
  setTimeout(() => {
    if (!host.classList.contains("is-open")) host.hidden = true;
  }, 200);
}

function importMrpackFile(file) {
  if (!file) return;
  const isMrpackOrZip = file.name.endsWith(".mrpack") || file.name.endsWith(".zip");
  if (!isMrpackOrZip) {
    showToastStatus("Chỉ hỗ trợ file modpack định dạng .mrpack hoặc .zip.");
    return;
  }
  const cleanName = file.name.replace(/\.(mrpack|zip)$/i, "");
  const pack = {
    id: `pack-imported-${Date.now()}`,
    name: cleanName || "Custom Modpack",
    author: "Nhập từ file",
    desc: `Gói modpack nhập từ tệp "${file.name}".`,
    longDesc: `Gói modpack tùy chỉnh được nhập từ tệp "${file.name}". Toàn bộ manifest và cấu hình được giải nén tự động.`,
    loader: "fabric",
    loaderVersion: "0.19.5",
    mc: "1.21.1",
    downloads: "1",
    downloadsNum: 1,
    size: `${Math.max(1, Math.round(file.size / 1048576))} MB`,
    modCount: 32,
    categories: ["adventure", "tech"],
    tag: "Nhập ngoài",
    mark: "M",
    accent: "#22c55e",
    icon: "assets/modpacks/performance-icon.png",
    image: "assets/modpacks/performance-banner.jpg",
    mods: ["Custom Fabric Loader", "Fabric API", "Sodium", "Lithium", "FerriteCore", "Indium", "JEI", "Jade", "AppleSkin"],
  };
  openModpackInstallModal(pack);
}

function openPackDetails(pack) {
  if (!pack) return;
  const deps = packDependencyProjects(pack);
  const projects = pack.mods.map(catalogProject).filter(Boolean);
  const installed = state.instances.some((inst) => inst.packId === pack.id);
  const loaderLabel = pack.loader === "neoforge" ? "NeoForge" : pack.loader === "fabric" ? "Fabric" : "Vanilla";
  const modRows = projects.map((project) => `
    <div class="pack-mod-row">
      <span class="pack-mod-mark" aria-hidden="true">${esc(project.name.slice(0, 1))}</span>
      <div class="pack-mod-copy">
        <strong>${esc(project.name)}</strong>
        <span>${esc(project.author)}</span>
      </div>
      <span class="chip">v${esc(project.version)}</span>
    </div>`).join("");

  const dialog = showDialog({
    title: pack.name,
    wide: true,
    primary: installed ? "Cài lại modpack" : "Cài modpack",
    closeText: "Đóng",
    body: `
      <div class="pack-detail-hero" style="--pack-accent:${esc(pack.accent)}">
        <span class="discover-mark">${esc(pack.mark)}</span>
        <div class="pack-detail-copy">
          <p>${esc(pack.desc)}</p>
          <div class="discover-meta">
            <span class="chip">${loaderLabel} ${esc(pack.loaderVersion)}</span>
            <span>MC ${esc(pack.mc)}</span>
            <span>${esc(pack.downloads)} lượt tải</span>
            <span>bởi ${esc(pack.author)}</span>
          </div>
        </div>
      </div>
      <section class="pack-detail-section">
        <div class="pack-detail-head">
          <strong>Nội dung modpack</strong>
          <span class="caption text-3">${pack.mods.length} mod chính${deps.length ? ` · ${deps.length} dependency` : ""}</span>
        </div>
        <div class="pack-mod-list">${modRows}</div>
        ${deps.length ? `
          <div class="pack-dependency-note">
            <span class="glyph">&#xE8B7;</span>
            <span>Tự động thêm: ${deps.map((dep) => esc(dep.name)).join(", ")}</span>
          </div>` : ""}
      </section>
      <p class="caption text-2">Instance mới sẽ dùng ${state.globalSettings.ramMb} MB RAM và Java mặc định trong Settings.</p>`,
    onPrimary: () => {
      setTimeout(() => installDiscoverPack(pack), 220);
      return true;
    },
  });
  return dialog;
}

function openShaderDetails(shader) {
  if (!shader) return;
  const targetId = state.activeInstanceId || state.instances[0]?.id;
  const targetInst = getInstance(targetId);
  const installedInTarget = targetInst ? isShaderInstalled(shader.id, targetInst.id) : false;
  const targetOptions = state.instances.map((inst) => {
    const hasIris = (state.mods[inst.id] || []).some((m) => m.projectId === "YL57xq9U" || m.file.toLowerCase().includes("iris"));
    const isInst = isShaderInstalled(shader.id, inst.id);
    const suffix = isInst ? " (đã cài)" : hasIris ? " (Iris sẵn sàng)" : " (cần Iris)";
    return `<option value="${esc(inst.id)}"${inst.id === targetId ? " selected" : ""}>${esc(inst.name)} · MC ${esc(inst.mc)}${suffix}</option>`;
  }).join("");

  const featurePills = (shader.features || []).map((f) => `<span class="chip">${esc(f)}</span>`).join("");

  const dialog = showDialog({
    title: shader.name,
    wide: true,
    primary: installedInTarget ? "Cài lại vào instance" : "Cài vào instance",
    closeText: "Đóng",
    body: `
      <div class="shader-detail-hero">
        <img class="shader-detail-img" src="${esc(shader.image)}" alt="${esc(shader.name)}" />
        <div class="shader-detail-overlay">
          <span class="discover-tag">${esc(shader.styleLabel)}</span>
          <div class="shader-detail-title-block">
            <h3>${esc(shader.name)}</h3>
            <span class="caption text-3">bởi ${esc(shader.author)} · v${esc(shader.version)}</span>
          </div>
        </div>
      </div>
      <div class="shader-detail-meta-bar">
        <div class="discover-meta">
          <span class="chip">Iris &amp; OptiFine</span>
          <span>Dung lượng: ${esc(shader.size)}</span>
          <span>${esc(shader.downloads)} lượt tải</span>
          <span>MC ${esc(shader.mc)}</span>
        </div>
      </div>
      <section class="pack-detail-section">
        <div class="pack-detail-head">
          <strong>Mô tả &amp; đặc điểm nổi bật</strong>
        </div>
        <p class="shader-detail-desc">${esc(shader.longDesc || shader.desc)}</p>
        <div class="shader-features-wrap">${featurePills}</div>
      </section>
      <section class="pack-detail-section" style="margin-top:12px;">
        <label class="field">
          <span>Cài vào Instance mục tiêu:</span>
          <select class="select" id="shaderTargetInstanceSelect" style="width:100%">
            ${targetOptions}
          </select>
        </label>
        <div class="pack-dependency-note" id="shaderIrisNote" style="margin-top:8px;">
          <span class="glyph">&#xE73E;</span>
          <span id="shaderIrisNoteText">Đang kiểm tra Iris/Sodium...</span>
        </div>
      </section>
      <p class="caption text-2" style="margin-top:8px;">Shaderpack sẽ được thêm vào thư mục <code>shaderpacks/</code> của instance. Tương thích Iris Shaders và OptiFine.</p>`,
    onPrimary: () => {
      const select = $("#shaderTargetInstanceSelect");
      const chosenId = select ? select.value : targetId;
      setTimeout(() => installShader(shader, chosenId), 220);
      return true;
    },
  });

  const select = $("#shaderTargetInstanceSelect");
  const noteText = $("#shaderIrisNoteText");
  const updateNote = () => {
    if (!select || !noteText) return;
    const inst = getInstance(select.value);
    if (!inst) return;
    const hasIris = (state.mods[inst.id] || []).some((m) => m.projectId === "YL57xq9U" || m.file.toLowerCase().includes("iris"));
    if (hasIris) {
      noteText.textContent = `Instance "${inst.name}" đã có Iris/Sodium. Shader sẵn sàng kích hoạt trong game.`;
    } else if (inst.loader === "fabric") {
      noteText.textContent = `Instance "${inst.name}" chưa có Iris. Vazer sẽ tự động cài thêm Iris Shaders + Fabric API để dùng shader này.`;
    } else {
      noteText.textContent = `Instance "${inst.name}" (${inst.loader}). Vazer sẽ thêm file shaderpack vào thư mục shaderpacks.`;
    }
    const isInst = isShaderInstalled(shader.id, inst.id);
    const primaryBtn = $("[data-dialog-primary]");
    if (primaryBtn) primaryBtn.textContent = isInst ? "Cài lại vào instance" : "Cài vào instance";
  };
  if (select) {
    select.addEventListener("change", updateNote);
    updateNote();
  }

  return dialog;
}

function getDiscoverCollection(type = state.discover.type) {
  if (type === "mods") return MOCK.catalog;
  if (type === "resourcepacks") return RESOURCE_PACKS;
  if (type === "shaders") return SHADER_PACKS;
  if (type === "datapacks") return DATA_PACKS;
  if (type === "modpacks") return DISCOVER_PACKS;
  return MOCK.catalog;
}

function findContentItem(idOrSlug, type = state.discover.type) {
  if (!idOrSlug) return null;
  const col = getDiscoverCollection(type);
  const found = col.find((p) => p.id === idOrSlug || p.slug === idOrSlug);
  if (found) return found;

  return (
    MOCK.catalog.find((p) => p.id === idOrSlug || p.slug === idOrSlug) ||
    RESOURCE_PACKS.find((p) => p.id === idOrSlug || p.slug === idOrSlug) ||
    SHADER_PACKS.find((p) => p.id === idOrSlug || p.slug === idOrSlug) ||
    DATA_PACKS.find((p) => p.id === idOrSlug || p.slug === idOrSlug) ||
    DISCOVER_PACKS.find((p) => p.id === idOrSlug) ||
    null
  );
}

function filteredContentItems(type = "mods") {
  const items = [...getDiscoverCollection(type)];
  const filters = (state.contentFilters && state.contentFilters[type]) || state.discover || {};
  const query = (filters.query || "").trim().toLowerCase();
  const cat = filters.category || "all";
  const loader = filters.loader || "all";
  const ver = filters.version || "all";
  const sort = filters.sort || "downloads";

  const filtered = items.filter((item) => {
    // 1. Category filter
    if (cat !== "all") {
      if (type === "modpacks") {
        const pCats = item.categories || [];
        const isOpt = cat === "optimization" || cat === "performance" || cat === "Hiệu năng" || cat.toLowerCase().includes("tối ưu");
        const isTech = cat === "tech" || cat === "technology" || cat === "Kỹ thuật" || cat.toLowerCase().includes("kỹ thuật");
        const isAdv = cat === "adventure" || cat === "Phiêu lưu" || cat.toLowerCase().includes("phiêu lưu");
        const isMagic = cat === "magic" || cat === "Phép thuật" || cat.toLowerCase().includes("phép thuật");
        const isQol = cat === "qol" || cat === "QoL";

        let match = false;
        if (isOpt && (pCats.includes("optimization") || pCats.includes("performance") || item.tag?.includes("Tối ưu") || item.tag === "Hiệu năng")) match = true;
        else if (isTech && (pCats.includes("tech") || pCats.includes("technology") || item.tag?.includes("Kỹ thuật") || item.tag === "Modded")) match = true;
        else if (isAdv && (pCats.includes("adventure") || item.tag?.includes("Phiêu lưu") || item.tag === "Adventure")) match = true;
        else if (isMagic && (pCats.includes("magic") || item.tag?.includes("Phép thuật"))) match = true;
        else if (isQol && (pCats.includes("qol") || item.tag === "QoL")) match = true;
        else if (pCats.includes(cat) || item.tag === cat) match = true;

        if (!match) return false;
      } else {
        const itemCat =
          item.category ||
          (item.style === "performance" ? "performance"
          : item.style === "fantasy" ? "magic"
          : item.style === "realistic" ? "graphics"
          : item.tag === "Hiệu năng" ? "performance"
          : item.tag === "Adventure" ? "adventure"
          : item.tag === "Modded" ? "technology"
          : item.tag === "QoL" ? "survival"
          : "all");
        if (itemCat !== cat) return false;
      }
    }

    // 2. Loader filter
    if (loader !== "all") {
      if (item.loaders && !item.loaders.includes(loader)) return false;
      if (item.loader && item.loader !== loader) return false;
    }

    // 3. Version filter
    if (ver !== "all") {
      if (item.versions && Array.isArray(item.versions) && !item.versions.some((v) => (typeof v === "string" ? v : v.mc).includes(ver))) return false;
      if (item.mc && !item.mc.includes(ver)) return false;
    }

    // 4. Query search
    if (query) {
      const modsListText = (item.mods || []).join(" ");
      const matchText = `${item.name} ${item.author} ${item.desc} ${item.longDesc || ""} ${item.categoryLabel || ""} ${item.tag || ""} ${item.styleLabel || ""} ${modsListText}`.toLowerCase();
      if (!matchText.includes(query)) return false;
    }

    return true;
  });

  // 5. Sorting
  filtered.sort((a, b) => {
    if (sort === "downloads") {
      const da = typeof a.downloadsNum === "number" ? a.downloadsNum : (typeof a.downloads === "number" ? a.downloads : parseFloat(String(a.downloads).replace(/[^\d.]/g, "")) * 1000);
      const db = typeof b.downloadsNum === "number" ? b.downloadsNum : (typeof b.downloads === "number" ? b.downloads : parseFloat(String(b.downloads).replace(/[^\d.]/g, "")) * 1000);
      return db - da;
    }
    if (sort === "newest") {
      const va = (a.versions && a.versions[0]?.version) || a.version || "";
      const vb = (b.versions && b.versions[0]?.version) || b.version || "";
      return vb.localeCompare(va, undefined, { numeric: true });
    }
    if (sort === "name") {
      return (a.name || "").localeCompare(b.name || "");
    }
    if (sort === "relevance") {
      return (a.name || "").localeCompare(b.name || "");
    }
    return 0;
  });

  return filtered;
}

function renderContentPage(type = "mods") {
  const grid = $(`#${type}Grid`) || $("#discoverGrid");
  const empty = $(`#${type}Empty`) || $("#discoverEmpty");
  const status = $(`#${type}Status`) || $("#discoverStatus");
  const countEl = $(`#${type}Count`) || $("#discoverCount");
  if (!grid) return;

  const typeLabel =
    type === "mods" ? "Mod"
    : type === "resourcepacks" ? "Resource Pack"
    : type === "shaders" ? "Shader"
    : type === "datapacks" ? "Data Pack"
    : "Modpack";

  const items = filteredContentItems(type);
  if (countEl) countEl.textContent = items.length;

  const inst = currentInstance();

  grid.innerHTML = items.map((item) => {
    const itemId = item.id || item.slug;

    const isInstalled =
      type === "mods" ? (state.mods[inst.id] || []).some((m) => m.projectId === item.id || m.file.toLowerCase().includes(item.slug))
      : type === "shaders" ? isShaderInstalled(item.id, inst.id)
      : type === "modpacks" ? state.instances.some((i) => i.packId === item.id || i.name === item.name)
      : !!(inst[type] && inst[type].some((x) => x.id === itemId));

    const downloadsFormatted = typeof item.downloads === "number"
      ? (item.downloads >= 1_000_000 ? `${(item.downloads / 1_000_000).toFixed(1)}M` : `${Math.round(item.downloads / 1000)}K`)
      : item.downloads;

    const loadersArr = item.loaders || (item.loader ? [item.loader] : ["fabric"]);
    const loaderDisplay = loadersArr.map((l) => `<span class="chip loader-chip modpack-loader-badge loader-${l}">${getLoaderIconSvg(l)}${l === "neoforge" ? "NeoForge" : l === "fabric" ? "Fabric" : "Vanilla"}</span>`).join(" ");

    const mcDisplay = (item.versions && item.versions[0]?.mc) ? item.versions[0].mc : item.mc || "1.21.1";
    const sizeDisplay = item.size || "1.0 MB";

    let artMarkup = "";
    if (item.image) {
      artMarkup = `
        <div class="shader-art modpack-cover-wrap" style="--shader-accent:${esc(item.accent || '#22c55e')}">
          <img class="shader-art-img modpack-cover-img" src="${esc(item.image)}" alt="${esc(item.name)}" />
          ${item.icon ? `<img class="shader-badge-icon modpack-card-icon" src="${esc(item.icon)}" alt="${esc(item.name)}" />` : ""}
          <span class="discover-tag modpack-tag">${esc(item.categoryLabel || item.styleLabel || item.tag || typeLabel)}</span>
        </div>
      `;
    } else if (item.icon) {
      artMarkup = `
        <div class="mod-art modpack-cover-wrap" style="--pack-accent:${esc(item.accent || '#22c55e')}">
          <img class="mod-art-icon modpack-card-icon" src="${esc(item.icon)}" alt="${esc(item.name)}" />
          <span class="discover-tag modpack-tag">${esc(item.categoryLabel || item.tag || typeLabel)}</span>
        </div>
      `;
    } else {
      artMarkup = `
        <div class="discover-art modpack-cover-wrap" style="--pack-accent:${esc(item.accent || '#22c55e')}">
          <span class="discover-mark">${esc(item.mark || (item.name ? item.name.slice(0, 1) : "M"))}</span>
          <span class="discover-tag modpack-tag">${esc(item.categoryLabel || item.tag || typeLabel)}</span>
        </div>
      `;
    }

    const actionLabel = isInstalled ? (type === "modpacks" ? "Đã cài đặt" : "Cài lại") : (type === "modpacks" ? "Cài đặt" : "Cài đặt");

    return `
      <article class="discover-card modpack-card" data-content-id="${esc(itemId)}" data-content-type="${esc(type)}">
        ${artMarkup}
        <div class="discover-body">
          <div class="discover-title-row">
            <h3>${esc(item.name)}</h3>
            <span class="discover-type-badge type-${esc(type)}">${esc(typeLabel)}</span>
            ${isInstalled ? '<span class="badge modpack-installed-badge">Đã cài</span>' : ""}
          </div>
          <p class="discover-desc">${esc(item.desc)}</p>
          <div class="discover-meta">
            ${loaderDisplay}
            <span class="modpack-meta-mc modpack-mc-version">MC ${esc(mcDisplay)}</span>
            <span class="modpack-meta-modcount modpack-mod-count">${esc(item.modCount || (item.mods ? item.mods.length : 30))} mods</span>
            <span class="modpack-meta-size modpack-size">${esc(sizeDisplay)}</span>
            <span class="modpack-meta-downloads modpack-downloads">${esc(downloadsFormatted)} lượt tải</span>
          </div>
          <div class="discover-actions">
            <span class="caption text-3 modpack-author">bởi ${esc(item.author)}</span>
            <div class="discover-action-buttons">
              <button class="btn ghost modpack-details-btn" data-content-details="${esc(itemId)}" data-content-type="${esc(type)}">Chi tiết</button>
              <button class="btn accent modpack-install-btn ${isInstalled ? 'is-installed' : ''}" data-install-content="${esc(itemId)}" data-content-type="${esc(type)}">${actionLabel}</button>
            </div>
          </div>
        </div>
      </article>
    `;
  }).join("");

  if (empty) empty.hidden = items.length !== 0;
  if (status) {
    status.textContent = items.length
      ? `${items.length} ${typeLabel.toLowerCase()} khả dụng · bấm "Chi tiết" để xem thêm.`
      : `Không tìm thấy ${typeLabel.toLowerCase()} phù hợp với bộ lọc hiện tại.`;
  }
}

function renderDiscover() {
  const current = (state.screen && state.screen !== "instances" && state.screen !== "detail") ? state.screen : "mods";
  renderContentPage(current);
}

function openModrinthDetailModal(item, type = state.discover.type) {
  if (!item) return;
  const inst = currentInstance();
  const itemId = item.id || item.slug;

  const isCompatible =
    (!item.loaders || item.loaders.includes(inst.loader)) &&
    (!item.loader || item.loader === inst.loader) &&
    (!item.versions || item.versions.includes(inst.mc)) &&
    (!item.mc || item.mc.includes(inst.mc));

  const compatBadge = isCompatible
    ? `<span class="modrinth-compat-badge compat-yes"><span class="glyph">&#xE73E;</span>Tương thích với ${esc(inst.name)} (${esc(inst.loader)} · ${esc(inst.mc)})</span>`
    : `<span class="modrinth-compat-badge compat-no"><span class="glyph">&#xE783;</span>Cần ${esc((item.loaders || [item.loader || "Fabric"]).join("/"))} · MC ${esc((item.versions || [item.mc || "1.21.1"]).join(", "))}</span>`;

  const typeLabel =
    type === "mods" ? "Mod"
    : type === "resourcepacks" ? "Resource Pack"
    : type === "shaders" ? "Shader Pack"
    : type === "datapacks" ? "Data Pack"
    : "Modpack";

  const isInstalled =
    type === "mods" ? (state.mods[inst.id] || []).some((m) => m.projectId === item.id || m.file.toLowerCase().includes(item.slug))
    : type === "shaders" ? isShaderInstalled(item.id, inst.id)
    : type === "modpacks" ? state.instances.some((i) => i.packId === item.id)
    : !!(inst[type] && inst[type].some((x) => x.id === itemId));

  const screenshots = (item.gallery && item.gallery.length) ? item.gallery : [
    {
      url: item.image || item.icon || "assets/shaders/bsl-shaders.jpg",
      title: `${item.name} - Phối cảnh trong game`,
      name: `${item.slug || "preview"}-screen-1.png`,
      date: "Hôm nay, 14:20",
      world: inst.name,
      width: 1920,
      height: 1080,
    },
    {
      url: "assets/shaders/complementary-reimagined.jpg",
      title: `${item.name} - Giao diện & Hiệu ứng`,
      name: `${item.slug || "preview"}-screen-2.png`,
      date: "Hôm qua, 09:15",
      world: inst.name,
      width: 1920,
      height: 1080,
    },
    {
      url: "assets/shaders/photon-shader.jpg",
      title: `${item.name} - Cảnh quan ban đêm`,
      name: `${item.slug || "preview"}-screen-3.png`,
      date: "3 ngày trước",
      world: inst.name,
      width: 1920,
      height: 1080,
    },
  ];

  const galleryCardsHtml = screenshots.map((ss, i) => `
    <div class="gallery-card" data-gallery-index="${i}">
      <img src="${esc(ss.url)}" alt="${esc(ss.title)}" />
      <div class="gallery-card-overlay">
        <span class="gallery-card-caption">${esc(ss.title)}</span>
      </div>
    </div>
  `).join("");

  const changelogData = (MOD_CHANGELOGS[item.slug] || MOD_CHANGELOGS[item.id])?.items || [
    { type: "add", text: `Tối ưu hóa pipeline hỗ trợ Minecraft ${inst.mc}` },
    { type: "perf", text: "Cải thiện tốc độ tải tài nguyên và phản hồi giao diện" },
    { type: "fix", text: "Khắc phục hiện tượng xung đột bộ nhớ ngầm" },
  ];

  const versionsMatrix = [
    {
      version: item.version || "1.0.0",
      channel: "release",
      channelLabel: "Release",
      mc: inst.mc,
      loader: inst.loader === "neoforge" ? "NeoForge" : "Fabric",
      size: item.size || "1.2 MB",
    },
    {
      version: prevVersion(item.version || "1.0.0"),
      channel: "release",
      channelLabel: "Release",
      mc: "1.20.1",
      loader: "Fabric, NeoForge",
      size: item.size || "1.1 MB",
    },
    {
      version: `${item.version || "1.0.0"}-beta.1`,
      channel: "beta",
      channelLabel: "Beta",
      mc: "1.21.1",
      loader: "Fabric",
      size: item.size || "1.3 MB",
    },
  ];

  const versionRowsHtml = versionsMatrix.map((v, idx) => `
    <tr>
      <td><strong>v${esc(v.version)}</strong></td>
      <td><span class="version-channel-badge ${v.channel}">${esc(v.channelLabel)}</span></td>
      <td>MC ${esc(v.mc)}</td>
      <td>${esc(v.loader)}</td>
      <td>${esc(v.size)}</td>
      <td>
        <div style="display:flex;align-items:center;gap:6px;">
          <button class="btn small ${idx === 0 ? 'accent' : ''}" data-version-install="${esc(v.version)}">${idx === 0 && isInstalled ? 'Cài lại' : 'Cài đặt'}</button>
          <button class="version-changelog-toggle" data-version-toggle="${idx}">Changelog ▼</button>
        </div>
        <div class="version-changelog-drawer" id="changelogDrawer_${idx}" hidden>
          ${changelogData.map((c) => {
            const prefix = c.type === "add" ? "+ [Mới]" : c.type === "perf" ? "* [Tối ưu]" : "- [Sửa lỗi]";
            return `<div><strong>${prefix}</strong> ${esc(c.text)}</div>`;
          }).join("")}
        </div>
      </td>
    </tr>
  `).join("");

  const body = `
    <div class="modrinth-detail-hero">
      ${(item.icon || item.image) ? `<img class="modrinth-hero-icon" src="${esc(item.icon || item.image)}" alt="${esc(item.name)}" />` : `
        <div class="modrinth-hero-icon-fallback" style="--accent:${item.accent || '#22c55e'}">
          ${esc((item.name || "M").slice(0, 1))}
        </div>
      `}
      <div class="modrinth-hero-info">
        <div class="modrinth-hero-title-row">
          <h2>${esc(item.name)}</h2>
          <span class="discover-type-badge type-${esc(type)}">${esc(typeLabel)}</span>
        </div>
        <div class="modrinth-hero-meta">
          <span>bởi <strong>${esc(item.author)}</strong></span>
          <span>·</span>
          <span>v${esc(item.version || "1.0.0")}</span>
          <span>·</span>
          <span>${Number(item.downloads || 0).toLocaleString("en-US")} lượt tải</span>
          <span>·</span>
          <span>${esc(item.size || "1.0 MB")}</span>
        </div>
        <div style="margin-top:4px;">${compatBadge}</div>
      </div>
    </div>

    <div class="modrinth-modal-tabs" role="tablist">
      <button type="button" class="modrinth-tab-btn is-active" data-detail-tab="overview">
        <span class="glyph">&#xE8A5;</span><span>Tổng quan</span>
      </button>
      <button type="button" class="modrinth-tab-btn" data-detail-tab="gallery">
        <span class="glyph">&#xEB9F;</span><span>Hình ảnh (Gallery)</span>
      </button>
      <button type="button" class="modrinth-tab-btn" data-detail-tab="versions">
        <span class="glyph">&#xE7B8;</span><span>Phiên bản (Matrix)</span>
      </button>
    </div>

    <!-- Panel 1: Overview -->
    <div class="modrinth-tab-panel" id="modalPanelOverview">
      <div class="md-content">
        <h2>Mô tả dự án</h2>
        <p>${esc(item.longDesc || item.desc)}</p>
        <h3>Đặc điểm &amp; Tính năng nổi bật</h3>
        <ul>
          ${(item.features || [
            "Tối ưu hóa kiến trúc xử lý tài nguyên và luồng dựng hình",
            "Tương thích sâu với các mod và hệ sinh thái hiện đại",
            "Cung cấp menu cấu hình trực quan trong game",
            "Duy trì FPS ổn định kể cả khi xây dựng thế giới quy mô lớn"
          ]).map(f => `<li>${esc(f)}</li>`).join("")}
        </ul>
        <div class="md-callout warning">
          <span class="glyph">&#xE783;</span>
          <div>
            <strong>Lưu ý tương thích &amp; Thư viện phụ thuộc</strong><br />
            Khi cài đặt qua Vazer Launcher, các thư viện phụ thuộc bắt buộc sẽ được tự động phân tích và thêm vào một cách liền mạch.
          </div>
        </div>
        <div class="md-code-block">// Cấu hình nhanh (vazer-${esc(item.slug || "config")}.json)&#10;{ "enabled": true, "highPerformanceMode": true, "preset": "optimized" }</div>
      </div>
    </div>

    <!-- Panel 2: Gallery -->
    <div class="modrinth-tab-panel" id="modalPanelGallery" hidden>
      <div class="content-gallery-grid">
        ${galleryCardsHtml}
      </div>
      <p class="caption text-3" style="margin-top:8px;">Bấm vào bất kỳ ảnh nào để mở trình xem Lightbox toàn màn hình.</p>
    </div>

    <!-- Panel 3: Versions -->
    <div class="modrinth-tab-panel" id="modalPanelVersions" hidden>
      <table class="version-matrix-table">
        <thead>
          <tr>
            <th>Phiên bản</th>
            <th>Kênh</th>
            <th>Minecraft</th>
            <th>Loader</th>
            <th>Dung lượng</th>
            <th>Hành động</th>
          </tr>
        </thead>
        <tbody>
          ${versionRowsHtml}
        </tbody>
      </table>
    </div>
  `;

  showDialog({
    title: item.name,
    wide: true,
    primary: isInstalled ? `Cài lại vào ${inst.name}` : `Cài vào ${inst.name}`,
    closeText: "Đóng",
    body,
    onPrimary: () => {
      setTimeout(() => {
        if (type === "mods") installContentWithDependencies(item, inst);
        else if (type === "shaders") installShader(item, inst.id);
        else if (type === "modpacks") installDiscoverPack(item);
        else executeDirectInstall(item, inst);
      }, 150);
      return true;
    },
  });

  const host = $("#dialogHost");
  if (!host) return;

  // Tabs switching in modal
  const tabBtns = $$(".modrinth-tab-btn", host);
  const panels = {
    overview: $("#modalPanelOverview", host),
    gallery: $("#modalPanelGallery", host),
    versions: $("#modalPanelVersions", host),
  };

  tabBtns.forEach((btn) => {
    btn.addEventListener("click", () => {
      tabBtns.forEach((b) => b.classList.remove("is-active"));
      btn.classList.add("is-active");
      const target = btn.dataset.detailTab;
      Object.entries(panels).forEach(([k, panel]) => {
        if (panel) panel.hidden = k !== target;
      });
    });
  });

  // Gallery click -> openLightbox
  $$(".gallery-card", host).forEach((card) => {
    card.addEventListener("click", () => {
      const idx = Number(card.dataset.galleryIndex);
      const ss = screenshots[idx];
      if (ss) openLightbox(ss);
    });
  });

  // Version Matrix changelog drawers
  $$(".version-changelog-toggle", host).forEach((btn) => {
    btn.addEventListener("click", () => {
      const idx = btn.dataset.versionToggle;
      const drawer = $(`#changelogDrawer_${idx}`, host);
      if (drawer) {
        drawer.hidden = !drawer.hidden;
        btn.textContent = drawer.hidden ? "Changelog ▼" : "Changelog ▲";
      }
    });
  });

  // Version Matrix individual install buttons
  $$("[data-version-install]", host).forEach((btn) => {
    btn.addEventListener("click", () => {
      const ver = btn.dataset.versionInstall;
      showToastStatus(`Đang tiến hành cài đặt phiên bản v${ver}...`);
      $("[data-dialog-close]", host)?.click();
      setTimeout(() => {
        if (type === "mods") installContentWithDependencies({ ...item, version: ver }, inst);
        else if (type === "shaders") installShader(item, inst.id);
        else if (type === "modpacks") installDiscoverPack(item);
        else executeDirectInstall({ ...item, version: ver }, inst);
      }, 180);
    });
  });
}

function promptSelectInstanceForInstall({ item, itemsToInstall, defaultInstId = state.activeInstanceId, onConfirm }) {
  const toInstall = itemsToInstall || (item ? [item] : []);
  let selectedId = defaultInstId || state.instances[0]?.id;

  const renderCards = () => {
    return state.instances.map((inst) => {
      const isSelected = inst.id === selectedId;

      let isCompat = true;
      let compatReason = "Tương thích";
      if (item && item.loaders && !item.loaders.includes(inst.loader)) {
        isCompat = false;
        compatReason = `Cần ${item.loaders.join("/")}`;
      } else if (item && item.loader && item.loader !== inst.loader) {
        isCompat = false;
        compatReason = `Cần ${item.loader}`;
      }

      const modCount = (state.mods[inst.id] || []).length;

      return `
        <div class="instance-select-card ${isSelected ? "is-selected" : ""}" data-target-id="${esc(inst.id)}">
          ${getInstanceBlockIcon(inst.loader, "instance-select-icon", 34, inst.icon)}
          <div class="instance-select-info">
            <div class="instance-select-name">${esc(inst.name)}</div>
            <div class="instance-select-meta">Minecraft ${esc(inst.mc)} · ${esc(inst.loader.toUpperCase())} · ${modCount} mod hiện có</div>
          </div>
          <div class="instance-select-badge">
            ${isCompat
              ? `<span class="chip loader-chip loader-${inst.loader}"><span class="glyph">&#xE73E;</span> ${esc(compatReason)}</span>`
              : `<span class="chip" style="background:rgba(239,68,68,0.2);color:#f87171;"><span class="glyph">&#xE783;</span> ${esc(compatReason)}</span>`}
          </div>
        </div>
      `;
    }).join("");
  };

  const body = `
    <div style="padding:4px 0;">
      <p class="caption text-2" style="margin-bottom:10px;">
        Chọn instance bạn muốn cài đặt <strong>${toInstall.length > 1 ? `${toInstall.length} gói mod` : `"${esc(item?.name || "Gói nội dung")}"`}</strong>:
      </p>
      <div class="instance-select-list" id="instanceSelectListHost">
        ${renderCards()}
      </div>
      <div style="margin-top:14px;padding-top:10px;border-top:1px solid rgba(255,255,255,0.08);display:flex;align-items:center;gap:8px;">
        <input type="checkbox" id="setActiveInstanceCheckbox" checked style="accent-color:var(--accent);cursor:pointer;" />
        <label for="setActiveInstanceCheckbox" style="font-size:12px;color:var(--text-2);cursor:pointer;">
          Tự động đặt làm Instance đang chơi (Active) ở thanh dock dưới
        </label>
      </div>
    </div>
  `;

  showDialog({
    title: "Chọn Instance muốn cài đặt",
    wide: false,
    primary: "Xác nhận cài đặt",
    closeText: "Hủy",
    body,
    onPrimary: async () => {
      const chosenInst = state.instances.find((i) => i.id === selectedId) || currentInstance();
      const makeActive = $("#setActiveInstanceCheckbox")?.checked ?? true;
      if (makeActive && chosenInst && state.activeInstanceId !== chosenInst.id) {
        state.activeInstanceId = chosenInst.id;
        saveState();
        updateGlobalQuickPlay();
        if (state.screen === "instances") renderInstances();
      }
      if (onConfirm) await onConfirm(chosenInst);
      return true;
    },
  });

  const host = $("#dialogHost");
  if (host) {
    const listHost = $("#instanceSelectListHost", host);
    if (listHost) {
      listHost.addEventListener("click", (e) => {
        const card = e.target.closest("[data-target-id]");
        if (!card) return;
        selectedId = card.dataset.targetId;
        $$(".instance-select-card", listHost).forEach((c) => c.classList.remove("is-selected"));
        card.classList.add("is-selected");
      });
    }
  }
}

async function installContentWithDependencies(item, targetInst = currentInstance()) {
  if (!item || !targetInst) return;
  const slug = item.slug || item.id;
  const depRule = CONTENT_DEPENDENCIES[slug] || { required: [], optional: [] };
  const installedMods = state.mods[targetInst.id] || [];

  const isModInstalled = (modSlug) => {
    const proj = catalogProject(modSlug);
    return installedMods.some((m) =>
      (proj && m.projectId === proj.id) ||
      m.file.toLowerCase().includes(modSlug.toLowerCase())
    );
  };

  const reqList = (depRule.required || []).map((s) => ({
    slug: s,
    project: catalogProject(s) || { name: s, version: "Latest", author: "Modrinth", slug: s, id: s },
    installed: isModInstalled(s),
  }));

  const optList = (depRule.optional || []).map((s) => ({
    slug: s,
    project: catalogProject(s) || { name: s, version: "Latest", author: "Modrinth", slug: s, id: s },
    installed: isModInstalled(s),
  }));

  // If there are dependencies that need resolution or optional dependencies available:
  if (reqList.length > 0 || optList.length > 0) {
    openDependencyResolutionModal(item, targetInst, { reqList, optList });
    return;
  }

  // Direct install if no dependencies -> prompt instance selector first:
  promptSelectInstanceForInstall({
    item,
    itemsToInstall: [item],
    defaultInstId: targetInst.id,
    onConfirm: async (chosenInst) => {
      await executeDirectInstall(item, chosenInst);
    },
  });
}

function openDependencyResolutionModal(item, targetInst, { reqList, optList }) {
  const rootInstalled = (state.mods[targetInst.id] || []).some(
    (m) => m.projectId === item.id || m.file.toLowerCase().includes(item.slug || item.id)
  );

  let selectedOptSlugs = new Set(optList.filter((r) => !r.installed).map((r) => r.slug));

  const calcTotalNew = () => {
    let count = rootInstalled ? 0 : 1;
    count += reqList.filter((r) => !r.installed).length;
    count += selectedOptSlugs.size;
    return count;
  };

  const reqRows = reqList.map((r) => `
    <div class="dep-item-card">
      <div style="display:flex;align-items:center;gap:10px;">
        <span class="glyph" style="font-size:18px;color:${r.installed ? 'var(--text-3)' : '#f87171'};">&#xE74C;</span>
        <div>
          <div style="font-weight:600;font-size:13px;color:var(--text);">${esc(r.project.name)}</div>
          <div class="caption text-3">bởi ${esc(r.project.author)} · v${esc(r.project.version || "Latest")}</div>
        </div>
      </div>
      <div>
        ${r.installed
          ? '<span class="dep-status-pill is-installed"><span class="glyph">&#xE73E;</span>Đã có sẵn</span>'
          : '<span class="dep-status-pill is-new"><span class="glyph">&#xE896;</span>Sẽ cài mới</span>'}
      </div>
    </div>
  `).join("");

  const optRows = optList.map((r) => `
    <div class="dep-item-card">
      <div style="display:flex;align-items:center;gap:10px;">
        ${r.installed ? '<span class="glyph" style="font-size:18px;color:var(--text-3);">&#xE74C;</span>' : `
          <input type="checkbox" class="dep-opt-check" data-opt-slug="${esc(r.slug)}" checked />
        `}
        <div>
          <div style="font-weight:600;font-size:13px;color:var(--text);">${esc(r.project.name)}</div>
          <div class="caption text-3">bởi ${esc(r.project.author)} · v${esc(r.project.version || "Latest")}</div>
        </div>
      </div>
      <div>
        ${r.installed
          ? '<span class="dep-status-pill is-installed"><span class="glyph">&#xE73E;</span>Đã có sẵn</span>'
          : '<span class="dep-status-pill is-new">Khuyến nghị</span>'}
      </div>
    </div>
  `).join("");

  const body = `
    <div class="dep-tree-container">
      <div class="dep-root-card">
        <div class="modrinth-hero-icon-fallback" style="width:44px;height:44px;font-size:18px;">
          ${esc((item.name || "M").slice(0, 1))}
        </div>
        <div style="flex:1;">
          <div style="font-weight:700;font-size:14px;color:var(--text);">${esc(item.name)}</div>
          <div class="caption text-2">Mục tiêu cài đặt · v${esc(item.version || "1.0.0")}</div>
        </div>
        <span class="badge">${rootInstalled ? "Đã cài" : "Mục tiêu"}</span>
      </div>

      ${reqList.length ? `
        <div class="dep-branch-block">
          <div class="dep-branch-label required">
            <span class="glyph">&#xE783;</span><span>Phụ thuộc bắt buộc (Required)</span>
          </div>
          ${reqRows}
        </div>
      ` : ""}

      ${optList.length ? `
        <div class="dep-branch-block">
          <div class="dep-branch-label optional">
            <span class="glyph">&#xE735;</span><span>Khuyến nghị đi kèm (Optional)</span>
          </div>
          ${optRows}
        </div>
      ` : ""}

      <div class="dep-summary-box">
        <span class="glyph" style="color:var(--accent);font-size:16px;">&#xE946;</span>
        <span id="depModalSummary">Sẽ tải và cài đặt ${calcTotalNew()} mod mới vào instance "${esc(targetInst.name)}".</span>
      </div>
    </div>
  `;

  showDialog({
    title: "1-Click Dependency Resolution Engine",
    wide: true,
    primary: `Cài đặt tất cả (${calcTotalNew()} mod)`,
    closeText: "Hủy bỏ",
    body,
    onPrimary: () => {
      const toInstall = [];
      if (!rootInstalled) toInstall.push(item);
      reqList.filter((r) => !r.installed).forEach((r) => toInstall.push(r.project));
      optList.filter((r) => !r.installed && selectedOptSlugs.has(r.slug)).forEach((r) => toInstall.push(r.project));

      setTimeout(() => {
        promptSelectInstanceForInstall({
          item,
          itemsToInstall: toInstall,
          defaultInstId: targetInst.id,
          onConfirm: async (chosenInst) => {
            await executeBatchInstall(toInstall, chosenInst);
          },
        });
      }, 100);
      return true;
    },
  });

  const host = $("#dialogHost");
  if (host) {
    const checks = $$(".dep-opt-check", host);
    const summaryEl = $("#depModalSummary", host);
    const primaryBtn = $("[data-dialog-primary]", host);
    checks.forEach((chk) => {
      chk.addEventListener("change", () => {
        const s = chk.dataset.optSlug;
        if (chk.checked) selectedOptSlugs.add(s);
        else selectedOptSlugs.delete(s);

        const total = calcTotalNew();
        if (summaryEl) summaryEl.textContent = `Sẽ tải và cài đặt ${total} mod mới vào instance "${targetInst.name}".`;
        if (primaryBtn) primaryBtn.textContent = `Cài đặt tất cả (${total} mod)`;
      });
    });
  }
}

async function executeBatchInstall(projects, inst) {
  if (!projects.length || !inst) return;
  const task = createTask(
    "install",
    `Cài đặt ${projects.length} mod (+phụ thuộc)`,
    `Đích: ${inst.name} (${inst.loader} · ${inst.mc})`
  );

  const instMods = (state.mods[inst.id] ||= []);
  for (let i = 0; i < projects.length; i++) {
    const p = projects[i];
    const pct = Math.round(((i + 1) / projects.length) * 100);
    updateTask(task, { progress: pct, detail: `Đang tải ${p.name}... ${pct}%` });
    await sleep(150);

    if (!instMods.some((m) => m.projectId === p.id || m.file.toLowerCase().includes((p.slug || "").toLowerCase()))) {
      instMods.push({
        projectId: p.id || p.slug,
        file: modFileName(p, inst),
        version: p.version || "1.0.0",
        enabled: true,
        update: null,
      });
    }
  }

  finishTask(task, "done", `Đã cài xong ${projects.length} mod`);
  saveState();
  if (state.screen === "instances" && state.activeInstanceId === inst.id && state.tab === "mods") {
    renderMods();
  }
  renderContentPage(state.screen);
  updateGlobalQuickPlay();
  showToastStatus(`Đã cài thành công ${projects.length} mod vào "${inst.name}".`);
}

async function executeDirectInstall(item, inst) {
  const type = item.type || state.discover.type || "mods";
  const typeLabel =
    type === "resourcepacks" ? "Resource Pack"
    : type === "datapacks" ? "Data Pack"
    : type === "shaders" ? "Shader"
    : "Mod";

  const task = createTask("install", `Cài ${item.name}`, `${typeLabel} · ${inst.name}`);
  updateTask(task, { progress: 35, detail: `Đang tải ${item.name}...` });
  await sleep(180);
  updateTask(task, { progress: 85, detail: "Xác thực gói nội dung..." });
  await sleep(120);

  if (type === "mods") {
    const instMods = (state.mods[inst.id] ||= []);
    if (!instMods.some((m) => m.projectId === item.id || m.file.toLowerCase().includes((item.slug || "").toLowerCase()))) {
      instMods.push({
        projectId: item.id || item.slug,
        file: modFileName(item, inst),
        version: item.version || "1.0.0",
        enabled: true,
        update: null,
      });
    }
  } else {
    inst[type] ||= [];
    if (!inst[type].some((x) => x.id === item.id)) {
      inst[type].push({ id: item.id, name: item.name, version: item.version, enabled: true });
    }
    if (type === "resourcepacks") {
      const packs = resourcePacksFor(inst);
      if (!packs.some((x) => x.id === item.id || x.slug === item.id)) {
        packs.push({
          id: item.id,
          name: item.name,
          version: item.version,
          resolution: item.resolution || "16x",
          size: item.size || "10 MB",
          icon: item.icon || "assets/blocks/grass_block.png",
          enabled: true,
          mc: item.versions?.[0] || inst.mc,
          mcVersions: item.versions || [inst.mc],
        });
      }
    }
  }

  finishTask(task, "done", `Đã cài ${item.name}`);
  saveState();
  if (state.screen === "instances" && state.activeInstanceId === inst.id && state.tab === "mods") {
    renderMods();
  }
  renderContentPage(state.screen);
  updateGlobalQuickPlay();
  showToastStatus(`Đã cài đặt "${item.name}" vào "${inst.name}".`);
}

function renderShaders() {
  const grid = $("#shaderGrid");
  const empty = $("#shaderEmpty");
  const status = $("#shaderStatus");
  if (!grid) return;

  const shaders = filteredDiscoverShaders();
  $("#shadersCount").textContent = shaders.length;

  grid.innerHTML = shaders.map((shader) => {
    const installedInAny = state.instances.some((inst) => isShaderInstalled(shader.id, inst.id));
    const installedInActive = isShaderInstalled(shader.id, state.activeInstanceId);
    return `
      <article class="discover-card shader-card" data-shader-id="${esc(shader.id)}">
        <div class="shader-art" style="--shader-accent:${esc(shader.accent)}">
          <img class="shader-art-img" src="${esc(shader.image)}" alt="${esc(shader.name)}" loading="lazy" />
          <span class="discover-tag">${esc(shader.styleLabel)}</span>
        </div>
        <div class="discover-body">
          <div class="discover-title-row">
            <h3>${esc(shader.name)}</h3>
            ${installedInActive ? '<span class="badge">Đang dùng</span>' : installedInAny ? '<span class="badge">Đã cài</span>' : ""}
          </div>
          <p class="discover-desc">${esc(shader.desc)}</p>
          <div class="discover-meta">
            <span class="chip">Iris / OptiFine</span>
            <span>MC ${esc(shader.mc)}</span>
            <span>${esc(shader.size)}</span>
            <span>${esc(shader.downloads)} lượt tải</span>
          </div>
          <div class="discover-actions">
            <span class="caption text-3">bởi ${esc(shader.author)}</span>
            <div class="discover-action-buttons">
              <button class="btn ghost" data-shader-details="${esc(shader.id)}">Chi tiết</button>
              <button class="btn accent" data-install-shader="${esc(shader.id)}">${installedInActive ? "Cài lại" : "Cài shader"}</button>
            </div>
          </div>
        </div>
      </article>`;
  }).join("");

  if (empty) empty.hidden = shaders.length !== 0;
  if (status) {
    status.textContent = shaders.length
      ? `${shaders.length} / ${SHADER_PACKS.length} shader · bấm "Chi tiết" để xem ảnh preview.`
      : "Không có shader khớp bộ lọc hiện tại.";
  }
  updateGlobalQuickPlay();
}

async function installDiscoverPack(pack, button) {
  if (!pack || (button && button.disabled)) return;
  const originalLabel = button ? button.textContent : "";
  if (button) {
    button.disabled = true;
    button.textContent = "Đang cài...";
  }
  const task = createTask("create", `Cài ${pack.name}`, `Modpack · MC ${pack.mc}`);
  updateTask(task, { progress: 8, detail: "Chuẩn bị instance..." });
  try {
    await sleep(420);
    updateTask(task, { progress: 36, detail: "Tải metadata modpack..." });
    await sleep(520);
    const id = uniqueInstanceId(pack.name);
    const inst = {
      id,
      name: pack.name,
      mc: pack.mc,
      loader: pack.loader,
      loaderVersion: pack.loaderVersion || null,
      path: `C:\\Users\\TOSHIBA\\AppData\\Roaming\\Vazer\\Minecraft\\instances\\${id}`,
      lastPlayed: null,
      lastPlayedTs: 0,
      playtimeMin: 0,
      createdTs: Date.now(),
      packId: pack.id,
      shaders: [],
      settings: {
        ...DEFAULT_SETTINGS,
        ramMb: state.globalSettings.ramMb,
        javaPath: state.globalSettings.javaPath,
      },
      mcOptions: { ...DEFAULT_MC_OPTIONS },
    };
    state.instances.unshift(inst);
    state.instances[id] = inst;
    state.mods[id] = [];
    state.worlds[id] = [];
    updateTask(task, { progress: 72, detail: "Cài mod và dependency..." });
    await sleep(480);
    pack.mods.forEach((slug) => {
      const project = catalogProject(slug);
      if (!project) return;
      resolveDependencies(project, inst).forEach((dep) => addInstalledMod(dep, inst));
      addInstalledMod(project, inst);
    });
    const modCount = (state.mods[id] || []).length;
    const depCount = Math.max(0, modCount - pack.mods.length);
    const depTask = depCount ? ` + ${depCount} dependency` : "";
    const depStatus = depCount ? `, gồm ${depCount} dependency` : "";
    saveState();
    renderInstances();
    renderDiscover();
    finishTask(task, "done", `${inst.id} · ${modCount} mod${depTask}`);
    showToastStatus(`Đã cài ${pack.name} — instance sẵn sàng.`);
    const status = $("#discoverStatus");
    if (status) status.textContent = `Đã tạo instance "${pack.name}" (${modCount} mod${depStatus}).`;
  } catch {
    finishTask(task, "error", "Không thể cài modpack");
    showToastStatus("Cài modpack thất bại (demo).");
  } finally {
    if (button) {
      button.disabled = false;
      button.textContent = originalLabel || "Cài modpack";
    }
  }
}

async function installShader(shader, targetInstanceId, button) {
  const inst = getInstance(targetInstanceId || state.activeInstanceId);
  if (!shader || !inst || (button && button.disabled)) return;
  const originalLabel = button ? button.textContent : "";
  if (button) {
    button.disabled = true;
    button.textContent = "Đang cài...";
  }

  const task = createTask("install", `Cài ${shader.name}`, `Shaderpack · ${inst.name}`);
  updateTask(task, { progress: 15, detail: `Kiểm tra loader của ${inst.name}...` });

  try {
    await sleep(380);
    let addedIris = false;
    const hasIris = (state.mods[inst.id] || []).some((m) => m.projectId === "YL57xq9U" || m.file.toLowerCase().includes("iris"));
    if (!hasIris && inst.loader === "fabric") {
      updateTask(task, { progress: 45, detail: "Tự động cài Iris Shaders + Fabric API..." });
      await sleep(440);
      const irisProject = catalogProject("iris");
      if (irisProject) {
        resolveDependencies(irisProject, inst).forEach((dep) => addInstalledMod(dep, inst));
        addInstalledMod(irisProject, inst);
        addedIris = true;
      }
    }
    updateTask(task, { progress: 75, detail: `Tải ${shader.name} (${shader.size})...` });
    await sleep(460);

    inst.shaders = Array.isArray(inst.shaders) ? inst.shaders : [];
    if (!inst.shaders.some((s) => s.id === shader.id)) {
      inst.shaders.push({
        id: shader.id,
        name: shader.name,
        file: `${shader.id}-${shader.version}.zip`,
        version: shader.version,
        installedTs: Date.now(),
      });
    }

    const sState = instanceShadersFor(inst);
    if (!sState.installed.some((s) => s.id === shader.id)) {
      sState.installed.push({
        id: shader.id,
        name: shader.name,
        version: shader.version,
        author: shader.author || "Community",
        size: shader.size || "1.0 MB",
        image: shader.image || "assets/shaders/bsl-shaders.jpg",
        desc: shader.desc || "",
      });
    }

    saveState();
    renderShaders();
    if (state.screen === "detail" && state.activeInstanceId === inst.id) {
      renderMods();
      renderInstanceShaders();
    }
    const irisMsg = addedIris ? " (kèm Iris Shaders)" : "";
    finishTask(task, "done", `${shader.name} đã thêm vào ${inst.name}${irisMsg}`);
    showToastStatus(`Đã cài ${shader.name} vào "${inst.name}".`);
    const status = $("#shaderStatus");
    if (status) status.textContent = `Đã cài "${shader.name}" vào instance "${inst.name}"${irisMsg}.`;
  } catch {
    finishTask(task, "error", `Không thể cài shader ${shader.name}`);
    showToastStatus("Cài shader thất bại (demo).");
  } finally {
    if (button) {
      button.disabled = false;
      button.textContent = originalLabel || "Cài shader";
    }
  }
}

/* ------------------------------------------------------------
   WAVE 5 — SERVERS
   ------------------------------------------------------------ */
function getCategoryIcon(category) {
  switch (category) {
    case "creative": return "assets/blocks/diamond_block.png";
    case "minigames": return "assets/blocks/tnt.png";
    case "modded": return "assets/blocks/furnace.png";
    case "survival":
    default:
      return "assets/blocks/grass_block.png";
  }
}

function normalizeServer(server, index) {
  const online = server?.online !== false;
  const pingValue = Number(server?.ping);
  const validCategories = ["survival", "minigames", "creative", "modded"];
  let category = server?.category;
  if (!validCategories.includes(category)) {
    const n = (server?.name || "").toLowerCase();
    const id = (server?.id || "").toLowerCase();
    if (id.includes("creative") || n.includes("creative")) category = "creative";
    else if (id.includes("hypixel") || n.includes("minigame") || n.includes("bed war")) category = "minigames";
    else if (id.includes("modded") || n.includes("modded") || server?.loader === "fabric" || server?.loader === "neoforge") category = "modded";
    else category = "survival";
  }
  return {
    id: String(server?.id || `server-${index + 1}`),
    name: String(server?.name || `Server ${index + 1}`),
    address: String(server?.address || ""),
    version: String(server?.version || "1.21.1"),
    loader: ["vanilla", "fabric", "neoforge"].includes(server?.loader) ? server.loader : "vanilla",
    category,
    icon: String(server?.icon || getCategoryIcon(category)),
    favorite: !!server?.favorite,
    online,
    players: Math.max(0, Number(server?.players) || 0),
    max: Math.max(0, Number(server?.max) || 0),
    ping: online ? (Number.isFinite(pingValue) ? pingValue : rand(28, 95)) : null,
    lastPlayed: String(server?.lastPlayed || "Chưa vào"),
    motd: String(server?.motd || "§7Một máy chủ Minecraft"),
    playerSample: Array.isArray(server?.playerSample) ? server.playerSample : [],
  };
}

function parseMinecraftMotd(raw) {
  if (!raw) return '<div class="mc-motd-line"><span style="color:#aaaaaa">A Minecraft Server</span></div>';
  const lines = String(raw).split("\n");
  const MC_COLORS = {
    "0": "#000000", "1": "#0000aa", "2": "#00aa00", "3": "#00aaaa",
    "4": "#aa0000", "5": "#aa00aa", "6": "#ffaa00", "7": "#aaaaaa",
    "8": "#555555", "9": "#5555ff", "a": "#55ff55", "b": "#55ffff",
    "c": "#ff5555", "d": "#ff55ff", "e": "#ffff55", "f": "#ffffff",
  };
  const MC_STYLES = {
    "k": "mc-obfuscated",
    "l": "mc-bold",
    "m": "mc-strikethrough",
    "n": "mc-underline",
    "o": "mc-italic",
  };

  return lines.map((line) => {
    let curColor = "#aaaaaa";
    let curStyles = new Set();
    let out = "";
    let buf = "";

    function flush() {
      if (!buf) return;
      const classes = Array.from(curStyles).join(" ");
      const classAttr = classes ? ` class="${classes}"` : "";
      const styleAttr = curColor ? ` style="color:${curColor}"` : "";
      out += `<span${styleAttr}${classAttr}>${esc(buf)}</span>`;
      buf = "";
    }

    for (let i = 0; i < line.length; i++) {
      const ch = line[i];
      if ((ch === "§" || ch === "&") && i + 1 < line.length) {
        const code = line[i + 1].toLowerCase();
        if (MC_COLORS[code] !== undefined) {
          flush();
          curColor = MC_COLORS[code];
          curStyles.clear();
          i++;
          continue;
        } else if (MC_STYLES[code] !== undefined) {
          flush();
          curStyles.add(MC_STYLES[code]);
          i++;
          continue;
        } else if (code === "r") {
          flush();
          curColor = "#aaaaaa";
          curStyles.clear();
          i++;
          continue;
        }
      }
      buf += ch;
    }
    flush();
    return `<div class="mc-motd-line">${out || "&nbsp;"}</div>`;
  }).join("");
}

function renderPingBars(online, ping) {
  if (!online || ping == null) {
    return `
      <div class="server-ping-wrap is-offline" title="Offline">
        <div class="mc-ping-bars" aria-hidden="true">
          <span class="bar bar-1"></span>
          <span class="bar bar-2"></span>
          <span class="bar bar-3"></span>
          <span class="bar bar-4"></span>
          <span class="bar bar-5"></span>
        </div>
        <span>Offline</span>
      </div>`;
  }
  let colorClass = "is-green";
  let litCount = 5;
  if (ping < 80) {
    colorClass = "is-green";
    litCount = 5;
  } else if (ping < 150) {
    colorClass = "is-yellow";
    litCount = 3;
  } else {
    colorClass = "is-red";
    litCount = 1;
  }
  const bars = [1, 2, 3, 4, 5]
    .map((num) => `<span class="bar bar-${num}${num <= litCount ? " is-lit" : ""}"></span>`)
    .join("");
  return `
    <div class="server-ping-wrap ${colorClass}" title="${ping} ms">
      <div class="mc-ping-bars" aria-hidden="true">${bars}</div>
      <span>${ping} ms</span>
    </div>`;
}

function renderPlayerSample(server) {
  if (!server.online) {
    return `<span class="text-3">— người chơi</span>`;
  }
  const sample = Array.isArray(server.playerSample) && server.playerSample.length
    ? server.playerSample
    : [];
  const tooltipHtml = sample.length
    ? `
      <div class="server-player-tooltip" role="tooltip">
        <div class="player-tooltip-head">Người chơi gần đây</div>
        <div class="player-tooltip-list">
          ${sample.map((p) => `
            <div class="player-tooltip-item">
              <span class="player-tooltip-avatar" style="background-image:url('${esc(p.skin || "assets/skins/steve.png")}')"></span>
              <span>${esc(p.name)}</span>
            </div>
          `).join("")}
        </div>
      </div>`
    : "";

  return `
    <div class="server-players-wrap" tabindex="0">
      <span class="server-players-badge">${server.players}/${server.max} người chơi</span>
      ${tooltipHtml}
    </div>`;
}

function filteredServers() {
  const query = state.serverQuery.trim().toLowerCase();
  return state.servers.filter((server) => {
    if (state.serverCategory && state.serverCategory !== "all" && server.category !== state.serverCategory) {
      return false;
    }
    if (state.serverFilter === "favorite" && !server.favorite) return false;
    if (state.serverFilter === "online" && !server.online) return false;
    if (!query) return true;
    return `${server.name} ${server.address} ${server.version} ${server.category || ""}`.toLowerCase().includes(query);
  });
}

function serverStatus(text) {
  const el = $("#serverStatus");
  if (el) el.textContent = text;
}

function syncServerCategoryChips() {
  const container = $("#serverCategoryChips");
  if (!container) return;
  const current = state.serverCategory || "all";
  container.querySelectorAll("[data-server-category]").forEach((chip) => {
    const isCurrent = chip.dataset.serverCategory === current;
    chip.classList.toggle("is-active", isCurrent);
    chip.setAttribute("aria-selected", isCurrent ? "true" : "false");
  });
}

function renderServers() {
  const list = $("#serverList");
  const empty = $("#serversEmpty");
  if (!list) return;
  syncServerCategoryChips();
  const servers = filteredServers();
  $("#serversCount").textContent = servers.length;
  const categoryLabels = {
    survival: "Sinh tồn",
    minigames: "Minigames",
    creative: "Sáng tạo",
    modded: "Modpack",
  };
  list.innerHTML = servers.map((server) => {
    const loaderLabel = server.loader === "neoforge" ? "NeoForge" : server.loader === "fabric" ? "Fabric" : "Vanilla";
    const catLabel = categoryLabels[server.category] || "Sinh tồn";
    const iconSrc = server.icon || getCategoryIcon(server.category);
    return `
      <div class="server-row${server.online ? "" : " is-offline"}" data-server-id="${esc(server.id)}">
        <div class="server-avatar-wrap">
          <img class="server-icon" src="${esc(iconSrc)}" alt="${esc(server.name)}" width="40" height="40" />
          <span class="server-status ${server.online ? "is-online" : "is-offline"}" aria-hidden="true"></span>
        </div>
        <div class="server-main">
          <div class="server-name-row">
            <strong class="server-name">${esc(server.name)}</strong>
            ${server.favorite ? '<span class="server-fav-mark" aria-label="Yêu thích">★</span>' : ""}
            <span class="server-category-badge chip compact">${esc(catLabel)}</span>
          </div>
          <div class="server-address-line">
            <code class="server-ip">${esc(server.address)}</code>
            <button class="server-inline-copy-btn" data-server-copy="${esc(server.id)}" title="Sao chép địa chỉ IP" aria-label="Sao chép địa chỉ IP">
              <span class="glyph">&#xE8C8;</span>
            </button>
            <span class="badge server-version-badge">${esc(server.version)} · ${loaderLabel}</span>
          </div>
          <div class="server-motd-preview">
            ${parseMinecraftMotd(server.motd)}
          </div>
        </div>
        <div class="server-meta">
          ${renderPingBars(server.online, server.ping)}
          ${renderPlayerSample(server)}
          <span class="text-3">Lần vào: ${esc(server.lastPlayed)}</span>
        </div>
        <div class="server-actions">
          <button class="server-icon-btn${server.favorite ? " is-active" : ""}" data-server-favorite="${esc(server.id)}" title="${server.favorite ? "Bỏ yêu thích" : "Yêu thích"}" aria-label="${server.favorite ? "Bỏ yêu thích" : "Yêu thích"}">
            <span class="glyph">&#xE735;</span>
          </button>
          <button class="server-icon-btn" data-server-copy="${esc(server.id)}" title="Sao chép địa chỉ IP" aria-label="Sao chép địa chỉ IP">
            <span class="glyph">&#xE8C8;</span>
          </button>
          <button class="server-icon-btn" data-server-edit="${esc(server.id)}" title="Sửa server" aria-label="Sửa server">
            <span class="glyph">&#xE70F;</span>
          </button>
          <button class="server-icon-btn danger" data-server-delete="${esc(server.id)}" title="Xóa server" aria-label="Xóa server">
            <span class="glyph">&#xE74D;</span>
          </button>
          <button class="btn accent" data-server-join="${esc(server.id)}" ${server.online ? "" : "disabled"}>Chơi ngay</button>
        </div>
      </div>`;
  }).join("");
  if (empty) empty.hidden = servers.length !== 0;
  const online = state.servers.filter((s) => s.online).length;
  serverStatus(`${servers.length} / ${state.servers.length} server · ${online} đang online.`);
}

async function refreshServerStatuses() {
  const btn = $("#refreshServersBtn");
  if (!btn || btn.disabled) return;
  const label = btn.querySelector("span:not(.glyph)");
  const originalLabel = label?.textContent || "Làm mới";
  btn.disabled = true;
  btn.classList.add("is-busy");
  if (label) label.textContent = "Đang kiểm tra...";
  serverStatus("Đang kiểm tra trạng thái server...");

  await sleep(600);
  state.servers.forEach((server) => {
    const wasOnline = server.online;
    server.online = Math.random() > 0.18;
    if (server.online) {
      server.ping = clamp((server.ping || rand(24, 90)) + rand(-10, 16), 18, 180);
      server.players = wasOnline
        ? clamp(server.players + rand(-14, 20), 0, server.max || 100)
        : rand(4, Math.max(5, Math.floor((server.max || 100) * 0.42)));
    } else {
      server.players = 0;
      server.ping = null;
    }
  });
  saveState();
  renderServers();
  const online = state.servers.filter((server) => server.online).length;
  serverStatus(`Đã làm mới ${state.servers.length} server · ${online} đang online.`);
  showToastStatus("Đã làm mới trạng thái server.");
  btn.disabled = false;
  btn.classList.remove("is-busy");
  if (label) label.textContent = originalLabel;
}

function openServerDialog(id = null) {
  const server = id ? state.servers.find((s) => s.id === id) : null;
  const isEdit = !!server;
  const versions = ["1.21.1", "1.21", "1.20.1", "1.20", "1.19.2"];
  const categories = [
    { value: "survival", label: "Sinh tồn (Survival)" },
    { value: "minigames", label: "Minigames" },
    { value: "creative", label: "Sáng tạo (Creative)" },
    { value: "modded", label: "Modpack (Modded)" },
  ];
  showDialog({
    title: isEdit ? "Sửa máy chủ" : "Thêm máy chủ mới",
    primary: isEdit ? "Lưu thay đổi" : "Thêm máy chủ",
    closeText: "Hủy",
    body: `
      <div class="server-form">
        <label class="setting-field">
          <span>Tên máy chủ</span>
          <input class="textbox" id="serverNameInput" placeholder="Ví dụ: Aurora SMP" value="${esc(server?.name || "")}" autocomplete="off" />
        </label>
        <label class="setting-field">
          <span>Địa chỉ IP</span>
          <div style="display:flex;gap:8px;">
            <input class="textbox" id="serverAddressInput" placeholder="play.example.net" value="${esc(server?.address || "")}" autocomplete="off" style="flex:1;" />
            <button type="button" class="btn" id="serverPingCheckBtn" title="Kiểm tra ping ngay">
              <span class="glyph">&#xE72C;</span><span>Kiểm tra ping</span>
            </button>
          </div>
          <div class="caption text-2" id="serverPingResult" style="margin-top:4px;"></div>
        </label>
        <div class="form-row">
          <label class="setting-field">
            <span>Phiên bản</span>
            <select class="select" id="serverVersionInput">
              ${versions.map((v) => `<option value="${v}"${(server?.version || "1.21.1") === v ? " selected" : ""}>${v}</option>`).join("")}
            </select>
          </label>
          <label class="setting-field">
            <span>Thể loại</span>
            <select class="select" id="serverCategoryInput">
              ${categories.map((c) => `<option value="${c.value}"${(server?.category || "survival") === c.value ? " selected" : ""}>${c.label}</option>`).join("")}
            </select>
          </label>
        </div>
        <div class="form-row">
          <label class="setting-field">
            <span>Loader</span>
            <select class="select" id="serverLoaderInput">
              ${[["vanilla", "Vanilla"], ["fabric", "Fabric"], ["neoforge", "NeoForge"]].map(([value, label]) => `<option value="${value}"${(server?.loader || "vanilla") === value ? " selected" : ""}>${label}</option>`).join("")}
            </select>
          </label>
        </div>
        <label class="setting-toggle">
          <input type="checkbox" id="serverFavoriteInput" ${server?.favorite ? "checked" : ""} />
          <span>Đánh dấu yêu thích</span>
        </label>
        <div class="caption server-form-error" id="serverFormError"></div>
      </div>`,
    onPrimary: () => {
      const name = $("#serverNameInput").value.trim();
      const address = $("#serverAddressInput").value.trim().replace(/\s+/g, "");
      const version = $("#serverVersionInput").value;
      const category = $("#serverCategoryInput") ? $("#serverCategoryInput").value : "survival";
      const loader = $("#serverLoaderInput").value;
      const favorite = $("#serverFavoriteInput").checked;
      const error = $("#serverFormError");
      if (!name || !address) {
        error.textContent = "Vui lòng nhập đầy đủ tên và địa chỉ IP máy chủ.";
        return false;
      }
      if (state.servers.some((s) => s.id !== server?.id && s.address.toLowerCase() === address.toLowerCase())) {
        error.textContent = "Địa chỉ này đã có trong danh sách.";
        return false;
      }
      const icon = server?.icon || getCategoryIcon(category);
      const next = normalizeServer({
        id: server?.id || `server-${Date.now()}`,
        name,
        address,
        version,
        loader,
        category,
        icon,
        favorite,
        online: server ? server.online : true,
        players: server?.players ?? (server ? 0 : rand(12, 80)),
        max: server?.max || 250,
        ping: server?.ping ?? rand(30, 75),
        lastPlayed: server?.lastPlayed || "Chưa vào",
        motd: server?.motd || `§a${name} §7- §fSẵn sàng tham gia!`,
        playerSample: server?.playerSample || [],
      });
      if (server) {
        const index = state.servers.findIndex((s) => s.id === server.id);
        if (index >= 0) state.servers[index] = next;
        else state.servers.unshift(next);
      } else {
        state.servers.unshift(next);
      }
      saveState();
      renderServers();
      serverStatus(isEdit ? `Đã cập nhật "${name}".` : `Đã thêm "${name}".`);
      showToastStatus(isEdit ? `Đã lưu thay đổi máy chủ "${name}".` : `Đã thêm máy chủ mới: ${name}`);
      return true;
    },
  });

  setTimeout(() => {
    const pingBtn = $("#serverPingCheckBtn");
    const pingResult = $("#serverPingResult");
    if (pingBtn && pingResult) {
      pingBtn.onclick = async () => {
        const addr = $("#serverAddressInput")?.value.trim();
        if (!addr) {
          pingResult.textContent = "Vui lòng nhập địa chỉ IP trước khi kiểm tra.";
          pingResult.style.color = "var(--danger)";
          return;
        }
        pingBtn.disabled = true;
        pingBtn.innerHTML = '<span class="glyph is-busy">&#xE72C;</span><span>Đang ping...</span>';
        pingResult.textContent = "Đang kiểm tra kết nối tới " + addr + "...";
        pingResult.style.color = "var(--text-2)";
        await sleep(350);
        const testPing = rand(24, 78);
        pingResult.innerHTML = `<span class="glyph" style="color:var(--success);">&#xE73E;</span> <strong>Trực tuyến</strong> · ${testPing} ms`;
        pingResult.style.color = "var(--success)";
        pingBtn.disabled = false;
        pingBtn.innerHTML = '<span class="glyph">&#xE72C;</span><span>Kiểm tra lại</span>';
      };
    }
  }, 20);
}

function openAddServerDialog() {
  return openServerDialog(null);
}

function openEditServerDialog(id) {
  return openServerDialog(id);
}

function toggleServerFavorite(id) {
  const server = state.servers.find((s) => s.id === id);
  if (!server) return;
  server.favorite = !server.favorite;
  saveState();
  renderServers();
  serverStatus(server.favorite ? `Đã yêu thích "${server.name}".` : `Đã bỏ yêu thích "${server.name}".`);
}

function copyServerAddress(id) {
  const server = state.servers.find((s) => s.id === id);
  if (!server) return;
  const done = () => {
    serverStatus(`Đã copy ${server.address}.`);
    showToastStatus(`Đã sao chép địa chỉ: ${server.address}`);
  };
  if (navigator.clipboard?.writeText) {
    navigator.clipboard.writeText(server.address).then(done).catch(done);
  } else {
    done();
  }
}

async function openDeleteServerDialog(id) {
  const server = state.servers.find((s) => s.id === id);
  if (!server) return;
  const ok = await confirmDialog(`Xác nhận xóa máy chủ "${server.name}"?`, "Máy chủ sẽ bị xóa khỏi danh sách lưu trữ trên máy tính.");
  if (!ok) return;
  state.servers = state.servers.filter((s) => s.id !== id);
  saveState();
  renderServers();
  serverStatus(`Đã xóa "${server.name}".`);
  showToastStatus(`Đã xóa máy chủ "${server.name}".`);
}

function deleteServer(id) {
  return openDeleteServerDialog(id);
}

async function joinServer(id, button) {
  const server = state.servers.find((s) => s.id === id);
  if (!server) return;
  if (!server.online) {
    showToastStatus("Máy chủ đang offline, không thể kết nối.");
    return;
  }

  // Tự tìm instance khớp version và loader
  const activeInst = state.instances.find((i) => i.id === state.activeInstanceId);
  let matched = state.instances.find((inst) => {
    const vMatch = (inst.version || inst.mc) === server.version;
    const sLoader = (server.loader || "vanilla").toLowerCase();
    const iLoader = (inst.loader || "vanilla").toLowerCase();
    return vMatch && sLoader === iLoader;
  });
  if (!matched && (server.loader || "vanilla").toLowerCase() === "vanilla") {
    matched = state.instances.find((inst) => (inst.version || inst.mc) === server.version);
  }
  if (!matched && activeInst && (activeInst.version || activeInst.mc) === server.version) {
    matched = activeInst;
  }

  if (!matched) {
    showDialog({
      title: "Không tìm thấy Instance phù hợp",
      body: `
        <div style="display:flex;flex-direction:column;gap:10px;">
          <p>Máy chủ <strong>${esc(server.name)}</strong> yêu cầu phiên bản <strong>Minecraft ${esc(server.version)}</strong> với loader <strong>${esc((server.loader || "vanilla").toUpperCase())}</strong>.</p>
          <p class="caption text-2">Hiện chưa có instance nào trong máy khớp với cấu hình này. Bạn có muốn tạo instance mới không?</p>
        </div>
      `,
      primary: "Tạo Instance mới",
      closeText: "Hủy",
      onPrimary: () => {
        openNewInstanceDialog();
        return true;
      },
    });
    return;
  }

  if (button) {
    button.disabled = true;
    button.textContent = "Đang kết nối...";
  }

  showToastStatus(`Đang kết nối ${server.name} qua ${matched.name}...`);
  const task = createTask("launch", `Kết nối ${server.name}`, `${matched.name} · ${server.address}`);
  updateTask(task, { progress: 25, detail: `Kiểm tra tương thích ${matched.name}...` });

  try {
    await sleep(400);
    updateTask(task, { progress: 65, detail: `Direct Connect tới ${server.address}...` });
    await sleep(450);
    server.lastPlayed = "Vừa xong";
    server.players = Math.min(server.max || (server.players + 1), server.players + 1);
    saveState();
    renderServers();
    finishTask(task, "done", `Đã kết nối ${server.address}`);
    serverStatus(`Đang kết nối "${server.name}" bằng instance ${matched.name}.`);
    showToastStatus(`Đã kết nối ${server.name} qua ${matched.name}!`);

    selectInstance(matched.id);
    if (!matched.running && !matched.launching) {
      launchInstance(matched.id);
    }
  } catch {
    finishTask(task, "error", "Kết nối thất bại");
    showToastStatus("Không thể kết nối server.");
  } finally {
    if (button) {
      button.disabled = false;
      button.textContent = "Chơi ngay";
    }
  }
}

/* ------------------------------------------------------------
   WAVE 5 — COMPREHENSIVE GLOBAL SETTINGS
   ------------------------------------------------------------ */
const ACCENT_PALETTES = {
  emerald: { primary: "#10b981", subtle: "rgba(16, 185, 129, 0.15)" },
  cyan: { primary: "#06b6d4", subtle: "rgba(6, 182, 212, 0.15)" },
  violet: { primary: "#8b5cf6", subtle: "rgba(139, 92, 246, 0.15)" },
  amber: { primary: "#f59e0b", subtle: "rgba(245, 158, 11, 0.15)" },
  rose: { primary: "#f43f5e", subtle: "rgba(244, 63, 94, 0.15)" },
};

function applyAccentColor(accent) {
  const pal = ACCENT_PALETTES[accent] || ACCENT_PALETTES.emerald;
  document.documentElement.style.setProperty("--accent", pal.primary);
  document.documentElement.style.setProperty("--accent-subtle", pal.subtle);
  $$(".accent-swatch").forEach((btn) => {
    const isSelected = btn.dataset.accent === accent;
    btn.classList.toggle("is-selected", isSelected);
    btn.setAttribute("aria-checked", isSelected ? "true" : "false");
  });
}

const JVM_PRESETS = {
  standard: "-XX:+UseG1GC -XX:+ParallelRefProcEnabled",
  aikar: "-XX:+UseG1GC -XX:+ParallelRefProcEnabled -XX:MaxGCPauseMillis=200 -XX:+UnlockExperimentalVMOptions -XX:+DisableExplicitGC -XX:+AlwaysPreTouch",
  zgc: "-XX:+UseZGC -XX:+ZGenerational -XX:+UnlockExperimentalVMOptions",
  shenandoah: "-XX:+UseShenandoahGC -XX:ShenandoahGCMode=iu -XX:+UnlockExperimentalVMOptions",
  custom: "",
};

function globalSettingsStatus(text) {
  const el = $("#globalSettingsStatus");
  if (el) el.textContent = text;
}

function filterSettingsCategory(group) {
  state.activeSettingsGroup = group;
  $$("#settingsCategoryChips .chip").forEach((chip) => {
    const active = chip.dataset.settingsGroup === group;
    chip.classList.toggle("is-active", active);
    chip.setAttribute("aria-selected", active ? "true" : "false");
  });
  $$(".settings-card[data-settings-group]").forEach((card) => {
    card.hidden = group !== "all" && card.dataset.settingsGroup !== group;
  });
}

function syncGlobalSettingsUI() {
  const s = state.globalSettings;
  if (!$("#globalJavaPath")) return;

  // 1. Launcher
  if ($("#settingLanguage")) $("#settingLanguage").value = s.language || "vi";
  if ($("#settingLaunchAction")) $("#settingLaunchAction").value = s.launchAction || "hide";
  if ($("#settingCloseToTray")) $("#settingCloseToTray").checked = s.closeToTray !== false;
  if ($("#settingAutoCheckUpdates")) $("#settingAutoCheckUpdates").checked = s.autoCheckUpdates !== false;

  // 2. Java & Minecraft
  $("#globalJavaPath").value = s.javaPath || "";
  if ($("#settingMinRam")) $("#settingMinRam").value = s.minRamMb || 2048;
  if ($("#settingMinRamValue")) $("#settingMinRamValue").value = s.minRamMb || 2048;
  $("#globalRam").value = s.ramMb || 4096;
  $("#globalRamValue").value = s.ramMb || 4096;
  if ($("#settingJvmPreset")) $("#settingJvmPreset").value = s.jvmPreset || "standard";
  if ($("#settingJvmArgs")) $("#settingJvmArgs").value = s.jvmArgs || JVM_PRESETS.standard;
  if ($("#settingWindowWidth")) $("#settingWindowWidth").value = s.windowWidth || 1280;
  if ($("#settingWindowHeight")) $("#settingWindowHeight").value = s.windowHeight || 720;
  if ($("#settingStartFullscreen")) $("#settingStartFullscreen").checked = !!s.startFullscreen;

  // 3. Appearance
  $("#globalTheme").value = s.theme === "light" ? "light" : s.theme === "obsidian" ? "obsidian" : "dark";
  applyAccentColor(s.accentColor || "emerald");
  $("#globalReduceMotion").checked = !!s.reduceMotion;
  if ($("#settingUiSound")) $("#settingUiSound").checked = s.uiSoundEnabled !== false;
  if ($("#themeToggle")) $("#themeToggle").textContent = s.theme === "light" ? "Light" : "Dark";

  // 4. Network
  if ($("#settingDownloadThreads")) $("#settingDownloadThreads").value = String(s.downloadThreads || 8);
  if ($("#settingDownloadMirror")) $("#settingDownloadMirror").value = s.downloadMirror || "cloudflare";
  if ($("#settingBandwidthLimit")) $("#settingBandwidthLimit").value = s.bandwidthLimit || "unlimited";

  // 5. Discord & Privacy
  if ($("#discordRpcToggle")) $("#discordRpcToggle").checked = s.discordRpc !== false;
  if ($("#discordStatusInput")) $("#discordStatusInput").value = s.discordStatus || "Đang sinh tồn ở thế giới mới";
  if ($("#settingDiscordShowInstance")) $("#settingDiscordShowInstance").checked = s.discordShowInstance !== false;
  if ($("#settingTelemetry")) $("#settingTelemetry").checked = s.anonymousTelemetry !== false;
  updateDiscordRpcPreview();

  // 6. Storage & Data
  $("#globalDataPath").value = s.dataPath || "";
  if ($("#settingBackupPath")) $("#settingBackupPath").value = s.backupPath || "";
  if ($("#settingCacheSizeLabel")) $("#settingCacheSizeLabel").textContent = `${s.cacheSizeMb || 248} MB`;

  // 7. Category Filter
  filterSettingsCategory(state.activeSettingsGroup || "all");
}

function applyGlobalSettings() {
  const javaPath = $("#globalJavaPath")?.value.trim() || "";
  const maxRam = clamp(Math.round(Number($("#globalRamValue")?.value) || state.globalSettings.ramMb || 4096), 1024, 16384);
  const minRam = clamp(Math.round(Number($("#settingMinRamValue")?.value) || state.globalSettings.minRamMb || 2048), 1024, maxRam);
  const theme = $("#globalTheme")?.value || "dark";
  const reduceMotion = !!$("#globalReduceMotion")?.checked;
  const accentColor = state.globalSettings.accentColor || "emerald";

  state.globalSettings = {
    ...state.globalSettings,
    language: $("#settingLanguage")?.value || "vi",
    launchAction: $("#settingLaunchAction")?.value || "hide",
    closeToTray: !!$("#settingCloseToTray")?.checked,
    autoCheckUpdates: !!$("#settingAutoCheckUpdates")?.checked,
    javaPath,
    minRamMb: minRam,
    ramMb: maxRam,
    jvmPreset: $("#settingJvmPreset")?.value || "standard",
    jvmArgs: $("#settingJvmArgs")?.value || "",
    windowWidth: Number($("#settingWindowWidth")?.value) || 1280,
    windowHeight: Number($("#settingWindowHeight")?.value) || 720,
    startFullscreen: !!$("#settingStartFullscreen")?.checked,
    theme,
    accentColor,
    reduceMotion,
    uiSoundEnabled: !!$("#settingUiSound")?.checked,
    downloadThreads: Number($("#settingDownloadThreads")?.value) || 8,
    downloadMirror: $("#settingDownloadMirror")?.value || "cloudflare",
    bandwidthLimit: $("#settingBandwidthLimit")?.value || "unlimited",
    discordRpc: !!$("#discordRpcToggle")?.checked,
    discordStatus: $("#discordStatusInput")?.value || "Đang sinh tồn ở thế giới mới",
    discordShowInstance: !!$("#settingDiscordShowInstance")?.checked,
    anonymousTelemetry: !!$("#settingTelemetry")?.checked,
    dataPath: $("#globalDataPath")?.value || state.globalSettings.dataPath,
    backupPath: $("#settingBackupPath")?.value || state.globalSettings.backupPath,
  };

  document.documentElement.dataset.theme = theme;
  document.documentElement.classList.toggle("reduce-motion", reduceMotion);
  applyAccentColor(accentColor);
  syncSkinViewerMotion();
  DEFAULT_SETTINGS.ramMb = maxRam;
  DEFAULT_SETTINGS.javaPath = javaPath;
  if ($("#themeToggle")) $("#themeToggle").textContent = theme === "light" ? "Light" : "Dark";
  syncGlobalSettingsUI();
  saveState();
}

function saveGlobalSettings() {
  applyGlobalSettings();
  globalSettingsStatus("Đã lưu cài đặt chung.");
  showToastStatus("Đã lưu cài đặt.");
}

async function resetGlobalSettings() {
  const ok = await confirmDialog("Khôi phục cài đặt mặc định?", "Tất cả thiết lập launcher, Java, RAM, giao diện và mạng sẽ trở về mặc định.", "Xác nhận");
  if (!ok) return;
  state.globalSettings = structuredClone(DEFAULT_GLOBAL_SETTINGS);
  document.documentElement.dataset.theme = state.globalSettings.theme;
  document.documentElement.classList.toggle("reduce-motion", state.globalSettings.reduceMotion);
  applyAccentColor(state.globalSettings.accentColor);
  syncSkinViewerMotion();
  DEFAULT_SETTINGS.ramMb = state.globalSettings.ramMb;
  DEFAULT_SETTINGS.javaPath = state.globalSettings.javaPath;
  syncGlobalSettingsUI();
  saveState();
  globalSettingsStatus("Đã khôi phục cài đặt mặc định.");
  showToastStatus("Đã khôi phục cài đặt mặc định.");
}

function detectInstalledJava() {
  const detected = "C:/Program Files/Eclipse Adoptium/jdk-21.0.4.7-hotspot/bin/javaw.exe";
  $("#globalJavaPath").value = detected;
  state.globalSettings.javaPath = detected;
  showToastStatus("Đã phát hiện: Java 21 LTS Adoptium (64-bit)");
  globalSettingsStatus("Đã tự động chọn Java 21 LTS.");
}

function checkForLauncherUpdates() {
  showToastStatus("Đang kiểm tra cập nhật...");
  setTimeout(() => {
    showToastStatus("V.A.Z v2.4.0 đang là phiên bản mới nhất!");
    globalSettingsStatus("Hệ thống đã cập nhật mới nhất (v2.4.0).");
  }, 350);
}

function exportDiagnosticReport() {
  const report = {
    app: "V.A.Z Launcher",
    version: "2.4.0",
    timestamp: new Date().toISOString(),
    os: "Windows 11 Pro 64-bit (Build 26100)",
    cpu: "AMD Ryzen 7 7800X3D (16 Threads)",
    ram: "32 GB DDR5 6000MHz",
    gpu: "NVIDIA GeForce RTX 4070 Super",
    settings: state.globalSettings,
    activeInstance: state.activeInstanceId,
    instancesCount: (state.instances || []).length,
  };
  const json = JSON.stringify(report, null, 2);
  const blob = new Blob([json], { type: "application/json" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = `vaz-diagnostic-${Date.now()}.json`;
  a.click();
  URL.revokeObjectURL(url);
  showToastStatus("Đã xuất báo cáo chẩn đoán (vaz-diagnostic.json).");
  globalSettingsStatus("Đã xuất tệp báo cáo chẩn đoán.");
}

async function clearDownloadCache() {
  const ok = await confirmDialog("Xóa cache tải xuống?", "Các gói tải tạm sẽ bị xóa. Instance và mod đã cài vẫn giữ nguyên.");
  if (!ok) return;
  const task = createTask("install", "Xóa cache tải xuống", "Modpack, mod, asset...");
  updateTask(task, { progress: 20, detail: "Đang quét cache..." });
  await sleep(460);
  updateTask(task, { progress: 64, detail: "Đang giải phóng dung lượng..." });
  await sleep(520);
  finishTask(task, "done", "Đã giải phóng 248 MB");
  state.globalSettings.cacheSizeMb = 0;
  if ($("#settingCacheSizeLabel")) $("#settingCacheSizeLabel").textContent = "0 MB";
  globalSettingsStatus("Đã xóa cache tải xuống · giải phóng 248 MB.");
  showToastStatus("Đã xóa cache tải xuống (demo).");
}

/* ------------------------------------------------------------
   PILLAR 3 — DISCORD RICH PRESENCE & MINECRAFT NEWS CAROUSEL
   ------------------------------------------------------------ */
function updateDiscordRpcPreview() {
  const card = $("#discordProfileCard");
  const toggle = $("#discordRpcToggle");
  const input = $("#discordStatusInput");
  if (!card) return;

  const enabled = state.discordRpc?.enabled !== false;
  if (toggle) toggle.checked = enabled;
  if (input && input !== document.activeElement) {
    input.value = state.discordRpc?.customStatus || "Đang sinh tồn ở thế giới mới";
  }

  card.classList.toggle("is-disabled", !enabled);

  const inst = currentInstance();
  const loaderLabel = inst?.loader === "neoforge" ? "NeoForge" : inst?.loader === "forge" ? "Forge" : inst?.loader === "fabric" ? "Fabric" : "Vanilla";
  const instText = inst ? `${inst.name} · ${loaderLabel} ${inst.version}` : "Minecraft 1.21.1";
  const instanceEl = $("#discordInstanceText");
  if (instanceEl) instanceEl.textContent = instText;

  const customEl = $("#discordCustomText");
  if (customEl) customEl.textContent = state.discordRpc?.customStatus || "Đang phiêu lưu trong Minecraft";

  const largeArt = $("#discordLargeArt");
  if (largeArt && inst?.icon) {
    largeArt.src = inst.icon;
  }

  const account = selectedSkinAccount() || state.accounts.find((a) => a.active);
  const skinPreset = getSkinPreset(account?.skinId);
  const smallArt = $("#discordSmallArt");
  if (smallArt && skinPreset?.texture) {
    smallArt.src = skinPreset.texture;
  }
}

function initDiscordRpc() {
  const toggle = $("#discordRpcToggle");
  const input = $("#discordStatusInput");

  if (toggle) {
    toggle.addEventListener("change", () => {
      state.discordRpc.enabled = toggle.checked;
      saveState();
      updateDiscordRpcPreview();
      globalSettingsStatus(toggle.checked ? "Đã bật Discord Rich Presence." : "Đã tắt Discord Rich Presence.");
    });
  }

  if (input) {
    input.addEventListener("input", () => {
      state.discordRpc.customStatus = input.value;
      saveState();
      updateDiscordRpcPreview();
    });
  }

  updateDiscordRpcPreview();
}

const MINECRAFT_NEWS = [
  {
    id: "news-1",
    badge: "Minecraft 1.21.1",
    badgeColor: "#22c55e",
    title: "Bản cập nhật Tricky Trials: Khám phá Trial Chambers & Vũ khí Chùy (Mace)",
    summary: "Thử thách hầm ngục mới với quái vật Breeze, cơ chế Wind Charge và vũ khí Mace hủy diệt khi tấn công từ trên cao.",
    date: "22/10/2026",
    tag: "Chính thức",
    actionLabel: "Tạo Instance 1.21.1",
    actionType: "instance",
    image: "assets/blocks/grass_block.png",
    content: `
      <p>Bản cập nhật <strong>Minecraft 1.21 Tricky Trials</strong> chính thức mang tới không gian khám phá hầm ngục hoàn toàn mới: <strong>Trial Chambers</strong>.</p>
      <p>Người chơi có thể đối đầu với loài quái vật <strong>Breeze</strong> tinh quái, né tránh các cạm bẫy Trial Spawner và mở khóa phần thưởng bí ẩn qua <em>Ominous Vaults</em>.</p>
      <p>Đặc biệt, vũ khí cận chiến hoàn toàn mới — <strong>Chùy (Mace)</strong> — cho phép khuếch đại sát thương nhảy tấn công tỉ lệ thuận với độ cao rơi, tạo nên những pha combat mãn nhãn!</p>
    `,
  },
  {
    id: "news-2",
    badge: "Vazer Launcher",
    badgeColor: "#38bdf8",
    title: "Vazer Launcher v2.4 ra mắt: Hỗ trợ 3D Wardrobe Studio & Live Server Ping",
    summary: "Nâng cấp giao diện người dùng mượt mà, phòng thử đồ 3D thời gian thực với hiệu ứng vạt áo phất phơ và MOTD server.",
    date: "18/10/2026",
    tag: "Cập nhật Launcher",
    actionLabel: "Khám phá Máy chủ",
    actionType: "servers",
    image: "assets/skins/steve.png",
    content: `
      <p>Phiên bản <strong>Vazer Launcher v2.4</strong> đánh dấu bước tiến lớn trong trải nghiệm quản lý phiên bản Minecraft hiện đại:</p>
      <ul>
        <li><strong>Interactive 3D Wardrobe Studio:</strong> Tùy biến skin Alex/Steve, 5 tư thế hoạt cảnh chân thực và mô phỏng vật lý áo choàng (Cape Flutter).</li>
        <li><strong>Live Multiplayer Server Hub:</strong> Hiển thị cột sóng ping 5 vạch màu, MOTD Minecraft đa sắc màu và tooltip danh sách người chơi mẫu.</li>
        <li><strong>Discord Rich Presence:</strong> Tích hợp hiển thị hoạt động chơi game chi tiết theo thời gian thực.</li>
      </ul>
    `,
  },
  {
    id: "news-3",
    badge: "Cộng đồng Mods",
    badgeColor: "#f59e0b",
    title: "Top Shaders & Modpacks tối ưu hiệu năng đồ họa cực đại trên Fabric 1.21",
    summary: "Khám phá danh sách Iris Shaders, Sodium và Complementary Reimagined mang lại mức FPS 144+ siêu mượt mà.",
    date: "12/10/2026",
    tag: "Cộng đồng",
    actionLabel: "Xem Modpacks",
    actionType: "modpacks",
    image: "assets/blocks/grass_block.png",
    content: `
      <p>Sự kết hợp giữa <strong>Sodium</strong> và <strong>Iris Shaders</strong> trên nền tảng <strong>Fabric 1.21</strong> tiếp tục thống trị bảng xếp hạng mod tối ưu hóa đồ họa.</p>
      <p>Với gói đổ bóng <em>Complementary Reimagined</em>, ánh sáng thể tích (volumetric lighting), mặt nước phản chiếu và bóng đổ chân thực giờ đây có thể chạy mượt mà ngay cả trên card đồ họa phổ thông.</p>
      <p>Bạn có thể cài đặt trực tiếp modpack này chỉ với một cú nhấp chuột tại trang <strong>Modpacks</strong> của Vazer Launcher!</p>
    `,
  },
];

let newsCarouselTimer = null;
let currentNewsSlide = 0;

function renderNewsCarousel() {
  const track = $("#newsCarouselTrack");
  const dots = $("#newsDots");
  if (!track || !dots) return;

  track.innerHTML = MINECRAFT_NEWS.map((item, idx) => `
    <div class="news-slide" role="group" aria-roledescription="slide" aria-label="${idx + 1} / ${MINECRAFT_NEWS.length}" data-news-id="${esc(item.id)}" style="--badge-color: ${esc(item.badgeColor)};">
      <div class="news-slide-backdrop"></div>
      <div class="news-slide-content">
        <span class="news-slide-badge">${esc(item.badge)}</span>
        <h3 class="news-slide-title">${esc(item.title)}</h3>
        <p class="news-slide-summary">${esc(item.summary)}</p>
        <div class="news-slide-footer">
          <span class="news-slide-meta">${esc(item.date)} · ${esc(item.tag)}</span>
          <button type="button" class="btn compact news-read-btn">
            <span>Đọc chi tiết</span>
            <span class="glyph">&#xE76C;</span>
          </button>
        </div>
      </div>
    </div>
  `).join("");

  dots.innerHTML = MINECRAFT_NEWS.map((_, idx) => `
    <button type="button" class="news-dot${idx === currentNewsSlide ? " is-active" : ""}" data-news-dot="${idx}" role="tab" aria-selected="${idx === currentNewsSlide ? "true" : "false"}" aria-label="Slide ${idx + 1}"></button>
  `).join("");

  updateNewsSlidePosition();
}

function updateNewsSlidePosition() {
  const track = $("#newsCarouselTrack");
  if (track) {
    track.style.transform = `translateX(-${currentNewsSlide * 100}%)`;
  }
  $$("#newsDots .news-dot").forEach((dot, idx) => {
    const isActive = idx === currentNewsSlide;
    dot.classList.toggle("is-active", isActive);
    dot.setAttribute("aria-selected", isActive ? "true" : "false");
  });
}

function goToNewsSlide(idx) {
  const total = MINECRAFT_NEWS.length;
  currentNewsSlide = ((idx % total) + total) % total;
  updateNewsSlidePosition();
}

function startNewsCarouselAutoPlay() {
  stopNewsCarouselAutoPlay();
  newsCarouselTimer = setInterval(() => {
    goToNewsSlide(currentNewsSlide + 1);
  }, 6000);
}

function stopNewsCarouselAutoPlay() {
  if (newsCarouselTimer) {
    clearInterval(newsCarouselTimer);
    newsCarouselTimer = null;
  }
}

function openNewsArticle(news) {
  showDialog({
    title: news.badge,
    wide: true,
    primary: news.actionLabel || "Đến tính năng",
    closeText: "Đóng",
    onPrimary: () => {
      if (news.actionType === "modpacks") {
        showScreen("modpacks");
      } else if (news.actionType === "servers") {
        showScreen("servers");
      } else if (news.actionType === "instance") {
        openNewInstanceDialog();
      }
      return true;
    },
    body: `
      <div class="news-article-dialog">
        <div class="news-article-hero" style="background-image: radial-gradient(circle at 70% 30%, ${news.badgeColor}33, transparent 70%), url('${esc(news.image)}');">
          <div style="position:relative;z-index:2;">
            <span class="news-slide-badge" style="--badge-color:${news.badgeColor}">${esc(news.badge)}</span>
            <h2 class="news-article-title" style="font-size:18px;margin:6px 0 0;color:#fff;">${esc(news.title)}</h2>
          </div>
        </div>
        <div class="caption text-2 news-article-meta">${esc(news.date)} · ${esc(news.tag)}</div>
        <div class="news-article-body">
          ${news.content}
        </div>
        <div class="news-article-actions" style="margin-top:14px;display:flex;gap:8px;">
          <button type="button" class="btn accent" id="newsInnerActionBtn" data-news-action="${esc(news.actionType || "")}">
            ${esc(news.actionLabel || "Xem thêm")}
          </button>
        </div>
      </div>
    `,
  });

  setTimeout(() => {
    const actBtn = $("#newsInnerActionBtn");
    if (actBtn) {
      actBtn.onclick = () => {
        const closeBtn = $("[data-dialog-close]", $("#dialogHost"));
        if (closeBtn) closeBtn.click();
        if (news.actionType === "modpacks") showScreen("modpacks");
        else if (news.actionType === "servers") showScreen("servers");
        else if (news.actionType === "instance") openNewInstanceDialog();
      };
    }
  }, 20);
}

function initNewsCarousel() {
  const shell = $("#newsCarousel");
  if (!shell) return;
  renderNewsCarousel();
  startNewsCarouselAutoPlay();

  shell.addEventListener("mouseenter", stopNewsCarouselAutoPlay);
  shell.addEventListener("mouseleave", startNewsCarouselAutoPlay);

  $("#newsNavPrev")?.addEventListener("click", () => {
    goToNewsSlide(currentNewsSlide - 1);
  });
  $("#newsNavNext")?.addEventListener("click", () => {
    goToNewsSlide(currentNewsSlide + 1);
  });

  $("#newsDots")?.addEventListener("click", (e) => {
    const dot = e.target.closest("[data-news-dot]");
    if (dot) {
      goToNewsSlide(parseInt(dot.dataset.newsDot, 10));
    }
  });

  shell.addEventListener("click", (e) => {
    const slide = e.target.closest(".news-slide");
    if (!slide) return;
    const newsId = slide.dataset.newsId;
    const news = MINECRAFT_NEWS.find((n) => n.id === newsId);
    if (news) openNewsArticle(news);
  });
}

/* ------------------------------------------------------------
   MINECRAFT AUDIO ENGINE (Native Web Audio API)
   ------------------------------------------------------------ */
let audioCtx = null;
function playMinecraftSound(type = "click") {
  if (state.soundEnabled === false) return;
  try {
    const AudioContextClass = window.AudioContext || window.webkitAudioContext;
    if (!AudioContextClass) return;
    if (!audioCtx) audioCtx = new AudioContextClass();
    if (audioCtx.state === "suspended") audioCtx.resume();

    const now = audioCtx.currentTime;

    if (type === "click") {
      const osc = audioCtx.createOscillator();
      const gain = audioCtx.createGain();
      osc.type = "triangle";
      osc.frequency.setValueAtTime(1250, now);
      osc.frequency.exponentialRampToValueAtTime(320, now + 0.035);
      gain.gain.setValueAtTime(0.12, now);
      gain.gain.exponentialRampToValueAtTime(0.001, now + 0.035);
      osc.connect(gain);
      gain.connect(audioCtx.destination);
      osc.start(now);
      osc.stop(now + 0.038);
    } else if (type === "launch") {
      const notes = [523.25, 659.25, 783.99, 1046.50];
      notes.forEach((freq, idx) => {
        const noteOsc = audioCtx.createOscillator();
        const noteGain = audioCtx.createGain();
        noteOsc.type = "sine";
        noteOsc.frequency.setValueAtTime(freq, now + idx * 0.07);
        noteGain.gain.setValueAtTime(0.09, now + idx * 0.07);
        noteGain.gain.exponentialRampToValueAtTime(0.001, now + idx * 0.07 + 0.12);
        noteOsc.connect(noteGain);
        noteGain.connect(audioCtx.destination);
        noteOsc.start(now + idx * 0.07);
        noteOsc.stop(now + idx * 0.07 + 0.13);
      });
    }
  } catch {
    // Autoplay policy or unsupported context
  }
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

  // Activity center
  $("#tasksBtn").addEventListener("click", () => {
    if ($("#tasksHost").classList.contains("is-open")) closeTasks();
    else openTasks();
  });
  $$("[data-tasks-close]").forEach((el) => el.addEventListener("click", closeTasks));
  $("#tasksFilters").addEventListener("click", (e) => {
    const chip = e.target.closest("[data-task-filter]");
    if (!chip) return;
    state.taskFilter = chip.dataset.taskFilter;
    renderTasks();
  });
  $("#clearTasksBtn").addEventListener("click", clearTaskHistory);

  // Instances
  $("#newInstanceBtn").addEventListener("click", openNewInstanceDialog);
  $("#importInstanceBtn")?.addEventListener("click", () => openImportWizard());

  const dropZone = $("#instanceGridDropZone");
  const dropOverlay = $("#instanceDropOverlay");
  if (dropZone && dropOverlay) {
    let dragCounter = 0;

    dropZone.addEventListener("dragenter", (e) => {
      e.preventDefault();
      dragCounter++;
      dropOverlay.hidden = false;
      dropZone.classList.add("is-dragover");
    });

    dropZone.addEventListener("dragover", (e) => {
      e.preventDefault();
      e.dataTransfer.dropEffect = "copy";
    });

    dropZone.addEventListener("dragleave", (e) => {
      e.preventDefault();
      dragCounter--;
      if (dragCounter <= 0) {
        dragCounter = 0;
        dropOverlay.hidden = true;
        dropZone.classList.remove("is-dragover");
      }
    });

    dropZone.addEventListener("drop", (e) => {
      e.preventDefault();
      dragCounter = 0;
      dropOverlay.hidden = true;
      dropZone.classList.remove("is-dragover");

      const files = e.dataTransfer.files;
      if (files && files.length > 0) {
        const file = files[0];
        const lower = file.name.toLowerCase();
        if (lower.endsWith(".mrpack") || lower.endsWith(".zip")) {
          openImportWizard(file);
        } else {
          showToastStatus("Chỉ hỗ trợ file modpack định dạng .mrpack hoặc .zip.");
        }
      }
    });
  }

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
      state.activeInstanceId = id;
      saveState();
      updateGlobalQuickPlay();
      if (getInstance(id)?.running) stopInstance(id);
      else await launchInstance(id);
    } else if (action === "edit") {
      e.stopPropagation();
      state.activeInstanceId = id;
      saveState();
      updateGlobalQuickPlay();
      openDetail(id, card);
    } else {
      state.activeInstanceId = id;
      saveState();
      updateGlobalQuickPlay();
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
      case "duplicate": duplicateInstance(id); break;
      case "export": openExportDialog(id); break;
      case "folder": showToastStatus("Đã mở thư mục instance (demo)."); break;
      case "delete":
        if (await confirmDialog(`Xóa "${inst.name}"?`, "Toàn bộ thư mục instance sẽ bị xóa khỏi disk. Không thể hoàn tác.")) {
          state.instances = state.instances.filter((i) => i.id !== id);
          delete state.mods[id];
          delete state.worlds[id];
          delete state.screenshots[id];
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
  ($("#btnBackToInstances") || $("#detailBackBtn"))?.addEventListener("click", closeDetail);
  $("#detailExportBtn")?.addEventListener("click", () => openExportDialog(state.activeInstanceId));
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
  const viewChangelogBtn = $("#viewChangelogBtn");
  if (viewChangelogBtn) viewChangelogBtn.addEventListener("click", openChangelogDiffModal);
  const updatesBarChangelogBtn = $("#updatesBarChangelogBtn");
  if (updatesBarChangelogBtn) updatesBarChangelogBtn.addEventListener("click", openChangelogDiffModal);
  const updatesBarUpdateBtn = $("#updatesBarUpdateBtn");
  if (updatesBarUpdateBtn) updatesBarUpdateBtn.addEventListener("click", updateAll);
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

  // Mod filters — đổi filter/sort thì bỏ chọn để bulk không dính mod đang ẩn.
  $("#modSearch").addEventListener("input", (e) => {
    state.modFilter.query = e.target.value;
    clearModSelection();
    renderMods();
  });

  $("#modStatusChips").addEventListener("click", (e) => {
    const btn = e.target.closest("[data-mod-status]");
    if (!btn || btn.dataset.modStatus === state.modFilter.status) return;
    state.modFilter.status = btn.dataset.modStatus;
    clearModSelection();
    renderMods();
  });

  $("#modSort").addEventListener("change", (e) => {
    state.modFilter.sort = e.target.value;
    clearModSelection();
    renderMods();
  });

  const resetFilters = (focusSearch = false) => {
    resetModFilter();
    clearModSelection();
    renderMods();
    if (focusSearch) $("#modSearch").focus();
  };
  $("#modClearFiltersBtn").addEventListener("click", () => resetFilters());
  $("#modsClearFiltersBtn").addEventListener("click", () => resetFilters(true));

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
      const willEnable = input.checked;
      mod.enabled = willEnable;
      saveState();
      if (!willEnable) {
        const affected = getDependentMods(mod, currentInstance());
        if (affected.length > 0) {
          const names = affected.map(modDisplayName).join(", ");
          showToastStatus(`Cảnh báo: Tắt ${modDisplayName(mod)} có thể làm lỗi các mod phụ thuộc (${names}).`);
        } else {
          setDetailStatus(`Đã tắt ${mod.file}.`);
        }
      } else {
        setDetailStatus(`Đã bật ${mod.file}.`);
      }
      renderMods();
    }
  });

  // Mod row menu + multi-select + inspector (delegated)
  $("#modList").addEventListener("click", (e) => {
    const btn = e.target.closest("[data-mod-menu]");
    if (btn) {
      const row = btn.closest("[data-mod-row]");
      const index = Number(row?.dataset.modRow ?? btn.dataset.modMenu);
      openModMenu(e.clientX, e.clientY, index);
      return;
    }
    if (e.target.closest(".switch, .mod-check, .mod-menu-btn")) return;
    const row = e.target.closest("[data-mod-row]");
    if (!row) return;
    const index = Number(row.dataset.modRow);
    if (state.modSelection.active) {
      toggleModSelection(index, { shift: e.shiftKey });
    } else {
      openModInspector(index);
    }
  });

  $("#modList").addEventListener("keydown", (e) => {
    if (e.key === "Enter" || e.key === " ") {
      const row = e.target.closest("[data-mod-row]");
      if (row && !e.target.closest(".switch, input, button")) {
        e.preventDefault();
        openModInspector(Number(row.dataset.modRow));
      }
    }
  });

  // Mod Category Chips
  $("#modCategoryChips")?.addEventListener("click", (e) => {
    const btn = e.target.closest("[data-mod-category]");
    if (!btn || btn.dataset.modCategory === state.modFilter.category) return;
    state.modFilter.category = btn.dataset.modCategory;
    clearModSelection();
    renderMods();
  });

  // Mod Inspector controls
  $("#modInspectorClose")?.addEventListener("click", closeModInspector);
  $("#modInspectorScrim")?.addEventListener("click", closeModInspector);
  $("#modInspectorResolveDepBtn")?.addEventListener("click", resolveInspectorDependencies);

  // Import .jar external flow
  $("#modAddJarBtn")?.addEventListener("click", () => {
    $("#modFileInput")?.click();
  });
  $("#modFileInput")?.addEventListener("change", (e) => {
    const file = e.target.files?.[0];
    if (file) {
      importModJarFile(file.name);
    }
    e.target.value = "";
  });

  // Drag and drop .jar into Tab Mods
  const modsTab = $("#tab-mods");
  if (modsTab) {
    modsTab.addEventListener("dragover", (e) => {
      e.preventDefault();
      if (e.dataTransfer) e.dataTransfer.dropEffect = "copy";
      modsTab.classList.add("is-drag-over");
    });
    modsTab.addEventListener("dragleave", () => {
      modsTab.classList.remove("is-drag-over");
    });
    modsTab.addEventListener("drop", (e) => {
      e.preventDefault();
      modsTab.classList.remove("is-drag-over");
      const files = Array.from(e.dataTransfer?.files || []);
      const jarFile = files.find((f) => f.name.toLowerCase().endsWith(".jar"));
      if (jarFile) {
        importModJarFile(jarFile.name);
      } else if (files.length > 0) {
        showToastStatus("Chỉ hỗ trợ tệp định dạng .jar.");
      }
    });
  }

  // Resource Packs & Shaders
  initResourcePacksAndShaders();

  // Worlds
  initWorldsManager();

  // Screenshots
  initScreenshotsGallery();

  // Settings
  wireSettingsEvents();

  // Logs toolbar
  $("#clearLogBtn")?.addEventListener("click", () => {
    state.logs[state.activeInstanceId] = [];
    state.logRenderKey = null;
    renderLogs();
    updateLogJump();
  });

  $("#generateLogBtn")?.addEventListener("click", () => {
    const inst = getInstance(state.activeInstanceId);
    state.logs[state.activeInstanceId] = generateSampleLogs(inst);
    state.logRenderKey = null;
    renderLogs();
    showToastStatus("Đã tạo 25 dòng log mẫu");
  });

  $("#copyLogBtn")?.addEventListener("click", async () => {
    const lines = filteredLogLines();
    const text = lines.join("\n");
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
    showToastStatus(`Đã sao chép ${lines.length} dòng log vào Clipboard`);
  });

  $("#exportLogBtn")?.addEventListener("click", () => {
    const lines = filteredLogLines();
    const text = lines.join("\n");
    const blob = new Blob([text], { type: "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "latest.log";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
    showToastStatus("Đã tải file latest.log về máy");
  });

  $("#openLogsFolderBtn")?.addEventListener("click", () => {
    showToastStatus("Đã mở thư mục logs: .minecraft/logs/latest.log");
  });

  $("#showLineNumbersChk")?.addEventListener("change", (e) => {
    state.showLogLineNumbers = e.target.checked;
    localStorage.setItem("vaz_show_log_line_numbers", String(state.showLogLineNumbers));
    $("#logView")?.classList.toggle("hide-line-numbers", !state.showLogLineNumbers);
  });

  $("#logSearch")?.addEventListener("input", (e) => {
    state.logFilter.query = e.target.value;
    renderLogs();
  });

  $("#logLevels")?.addEventListener("click", (e) => {
    const chip = e.target.closest("[data-log-level]");
    if (!chip) return;
    state.logFilter.level = chip.dataset.logLevel;
    $$(".sev-chip").forEach((c) => c.classList.toggle("is-selected", c === chip));
    renderLogs();
  });

  $("#jumpFirstErrorBtn")?.addEventListener("click", jumpToFirstError);
  $("#logJumpBtn")?.addEventListener("click", () => {
    const view = $("#logView");
    view.scrollTo({ top: view.scrollHeight, behavior: prefersReducedMotion() ? "auto" : "smooth" });
  });
  $("#logView")?.addEventListener("scroll", updateLogJump, { passive: true });

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
  $("#addOfflineBtn")?.addEventListener("click", openOfflineModal);
  $("#addMicrosoftBtn")?.addEventListener("click", openMsAuthModal);

  // Microsoft Device Code modal handlers
  $("#msCopyCodeBtn")?.addEventListener("click", copyMsCode);
  $("#msSimulateSuccessBtn")?.addEventListener("click", simulateMsAuthSuccess);
  $$("[data-ms-close]").forEach((btn) => btn.addEventListener("click", closeMsAuthModal));

  // Offline modal handlers
  $("#offlineSubmitBtn")?.addEventListener("click", submitOfflineAccount);
  $("#offlineUsernameInput")?.addEventListener("keydown", (e) => {
    if (e.key === "Enter") {
      e.preventDefault();
      submitOfflineAccount();
    }
  });
  $$("[data-offline-close]").forEach((btn) => btn.addEventListener("click", closeOfflineModal));
  $$(".skin-select-chip input[type='radio']").forEach((radio) => {
    radio.addEventListener("change", () => {
      $$(".skin-select-chip").forEach((chip) => {
        const forId = chip.getAttribute("for");
        const r = forId ? document.getElementById(forId) : null;
        chip.classList.toggle("is-selected", !!r?.checked);
      });
    });
  });

  $("#refreshActiveBtn")?.addEventListener("click", () => {
    const active = state.accounts.find((a) => a.active);
    if (active) {
      if (active.kind === "Microsoft") {
        active.tokenStatus = "Token: Hợp lệ (14 ngày)";
        saveState();
        renderAccounts();
      }
      showToastStatus(`Đã làm mới token cho "${active.name}".`);
      accountStatus(`Đã làm mới token cho "${active.name}".`);
    } else {
      accountStatus("Chưa có tài khoản active.");
    }
  });

  $("#reloadAccountsBtn")?.addEventListener("click", () => {
    renderAccounts();
    updateDock();
    accountStatus("Đã tải lại danh sách tài khoản (demo).");
  });

  $("#accountList")?.addEventListener("click", async (e) => {
    const card = e.target.closest("[data-account]");
    if (!card) return;
    const id = card.dataset.account;
    const action = e.target.closest("[data-acc-action]")?.dataset.accAction;
    const acc = getAccount(id);
    if (!acc) return;

    if (action === "activate") setActiveAccount(id);
    if (action === "refresh") {
      acc.tokenStatus = "Token: Hợp lệ (14 ngày)";
      saveState();
      renderAccounts();
      showToastStatus(`Đã làm mới token cho "${acc.name}".`);
      accountStatus(`Đã làm mới token cho "${acc.name}".`);
    }
    if (action === "remove") {
      if (await confirmDialog(`Xóa tài khoản "${acc.name}"?`, "Tài khoản sẽ bị xóa khỏi danh sách lưu trên máy.")) {
        const wasActive = acc.active;
        state.accounts = state.accounts.filter((a) => a.id !== id);
        if (wasActive && state.accounts.length > 0) {
          state.accounts[0].active = true;
        }
        if (state.skinAccountId === id) {
          state.skinAccountId = (state.accounts.find((a) => a.active) || state.accounts[0])?.id || null;
        }
        saveState();
        renderAccounts();
        updateDock();
        showToastStatus(`Đã xóa tài khoản "${acc.name}".`);
        accountStatus(`Đã xóa tài khoản "${acc.name}".`);
      }
    }
  });

  // Content Hub Pages (Mods, Resource Packs, Shaders, Data Packs, Modpacks)
  function attachContentGridEvents(gridEl, type) {
    if (!gridEl) return;
    gridEl.addEventListener("click", (e) => {
      const detailsBtn = e.target.closest("[data-content-details]");
      if (detailsBtn) {
        const id = detailsBtn.dataset.contentDetails;
        const itemType = detailsBtn.dataset.contentType || type;
        const item = findContentItem(id, itemType);
        if (item) {
          if (itemType === "modpacks") openModpackInspectorDrawer(item);
          else openModrinthDetailModal(item, itemType);
        }
        return;
      }

      const installBtn = e.target.closest("[data-install-content]");
      if (installBtn) {
        const id = installBtn.dataset.installContent;
        const itemType = installBtn.dataset.contentType || type;
        const item = findContentItem(id, itemType);
        if (!item) return;

        if (itemType === "mods") {
          installContentWithDependencies(item, currentInstance());
        } else if (itemType === "modpacks") {
          openModpackInstallModal(item, installBtn);
        } else {
          promptSelectInstanceForInstall({
            item,
            itemsToInstall: [item],
            defaultInstId: state.activeInstanceId,
            onConfirm: async (chosenInst) => {
              if (itemType === "shaders") installShader(item, chosenInst.id);
              else executeDirectInstall(item, chosenInst);
            },
          });
        }
        return;
      }
    });
  }

  // Mods Page
  const modsSearch = $("#modsSearch") || $("#discoverSearch");
  if (modsSearch) {
    modsSearch.addEventListener("input", (e) => {
      state.contentFilters.mods.query = e.target.value;
      renderContentPage("mods");
    });
  }
  const modsLoader = $("#modsLoaderFilter") || $("#discoverLoader");
  if (modsLoader) {
    modsLoader.addEventListener("change", (e) => {
      state.contentFilters.mods.loader = e.target.value;
      renderContentPage("mods");
    });
  }
  const modsVersion = $("#modsVersionFilter") || $("#discoverVersion");
  if (modsVersion) {
    modsVersion.addEventListener("change", (e) => {
      state.contentFilters.mods.version = e.target.value;
      renderContentPage("mods");
    });
  }
  const modsSort = $("#modsSortSelect") || $("#discoverSort");
  if (modsSort) {
    modsSort.addEventListener("change", (e) => {
      state.contentFilters.mods.sort = e.target.value;
      renderContentPage("mods");
    });
  }
  const modsCategoryChips = $("#modsCategoryChips") || $("#discoverCategoryChips");
  if (modsCategoryChips) {
    modsCategoryChips.addEventListener("click", (e) => {
      const chip = e.target.closest(".cat-chip");
      if (!chip) return;
      $$(".cat-chip", modsCategoryChips).forEach((c) => c.classList.remove("is-selected"));
      chip.classList.add("is-selected");
      state.contentFilters.mods.category = chip.dataset.cat;
      renderContentPage("mods");
    });
  }
  attachContentGridEvents($("#modsGrid") || $("#discoverGrid"), "mods");

  // Resource Packs Page
  const rpSearch = $("#resourcepacksSearch");
  if (rpSearch) {
    rpSearch.addEventListener("input", (e) => {
      state.contentFilters.resourcepacks.query = e.target.value;
      renderContentPage("resourcepacks");
    });
  }
  const rpVersion = $("#resourcepacksVersionFilter");
  if (rpVersion) {
    rpVersion.addEventListener("change", (e) => {
      state.contentFilters.resourcepacks.version = e.target.value;
      renderContentPage("resourcepacks");
    });
  }
  const rpSort = $("#resourcepacksSortSelect");
  if (rpSort) {
    rpSort.addEventListener("change", (e) => {
      state.contentFilters.resourcepacks.sort = e.target.value;
      renderContentPage("resourcepacks");
    });
  }
  attachContentGridEvents($("#resourcepacksGrid"), "resourcepacks");

  // Data Packs Page
  const dpSearch = $("#datapacksSearch");
  if (dpSearch) {
    dpSearch.addEventListener("input", (e) => {
      state.contentFilters.datapacks.query = e.target.value;
      renderContentPage("datapacks");
    });
  }
  const dpVersion = $("#datapacksVersionFilter");
  if (dpVersion) {
    dpVersion.addEventListener("change", (e) => {
      state.contentFilters.datapacks.version = e.target.value;
      renderContentPage("datapacks");
    });
  }
  const dpSort = $("#datapacksSortSelect");
  if (dpSort) {
    dpSort.addEventListener("change", (e) => {
      state.contentFilters.datapacks.sort = e.target.value;
      renderContentPage("datapacks");
    });
  }
  attachContentGridEvents($("#datapacksGrid"), "datapacks");

  // Modpacks Page
  const mpSearch = $("#modpacksSearch");
  if (mpSearch) {
    mpSearch.addEventListener("input", (e) => {
      state.contentFilters.modpacks.query = e.target.value;
      renderContentPage("modpacks");
    });
  }
  const mpLoader = $("#modpacksLoader") || $("#modpacksLoaderFilter");
  if (mpLoader) {
    mpLoader.addEventListener("change", (e) => {
      state.contentFilters.modpacks.loader = e.target.value;
      renderContentPage("modpacks");
    });
  }
  const mpVersion = $("#modpacksVersion") || $("#modpacksVersionFilter");
  if (mpVersion) {
    mpVersion.addEventListener("change", (e) => {
      state.contentFilters.modpacks.version = e.target.value;
      renderContentPage("modpacks");
    });
  }
  const mpSort = $("#modpacksSort") || $("#modpacksSortSelect");
  if (mpSort) {
    mpSort.addEventListener("change", (e) => {
      state.contentFilters.modpacks.sort = e.target.value;
      renderContentPage("modpacks");
    });
  }
  const mpCategoryChips = $("#modpackCategoryChips");
  if (mpCategoryChips) {
    mpCategoryChips.addEventListener("click", (e) => {
      const chip = e.target.closest(".chip");
      if (!chip) return;
      $$(".chip", mpCategoryChips).forEach((c) => {
        c.classList.remove("is-active");
        c.setAttribute("aria-selected", "false");
      });
      chip.classList.add("is-active");
      chip.setAttribute("aria-selected", "true");
      state.contentFilters.modpacks.category = chip.dataset.modpackCategory || "all";
      renderContentPage("modpacks");
    });
  }
  const importMrpackBtn = $("#importMrpackBtn");
  const modpackFileInput = $("#modpackFileInput");
  if (importMrpackBtn && modpackFileInput) {
    importMrpackBtn.addEventListener("click", () => {
      modpackFileInput.click();
    });
    modpackFileInput.addEventListener("change", (e) => {
      const file = e.target.files && e.target.files[0];
      if (file) {
        importMrpackFile(file);
        modpackFileInput.value = "";
      }
    });
  }
  const mpPage = $("#page-modpacks");
  const mpDropZone = $("#modpackDropZone");
  if (mpPage) {
    let dragCounter = 0;
    mpPage.addEventListener("dragenter", (e) => {
      e.preventDefault();
      dragCounter++;
      if (mpDropZone) {
        mpDropZone.hidden = false;
        mpDropZone.classList.add("is-dragover");
      }
    });
    mpPage.addEventListener("dragover", (e) => {
      e.preventDefault();
      if (mpDropZone) mpDropZone.classList.add("is-dragover");
    });
    mpPage.addEventListener("dragleave", (e) => {
      e.preventDefault();
      dragCounter--;
      if (dragCounter <= 0) {
        dragCounter = 0;
        if (mpDropZone) {
          mpDropZone.classList.remove("is-dragover");
          mpDropZone.hidden = true;
        }
      }
    });
    mpPage.addEventListener("drop", (e) => {
      e.preventDefault();
      dragCounter = 0;
      if (mpDropZone) {
        mpDropZone.classList.remove("is-dragover");
        mpDropZone.hidden = true;
      }
      const file = e.dataTransfer && e.dataTransfer.files && e.dataTransfer.files[0];
      if (file) {
        importMrpackFile(file);
      }
    });
  }
  attachContentGridEvents($("#modpacksGrid"), "modpacks");

  // Titlebar Quick Play & Instance Switcher
  const globalInstSelect = $("#globalActiveInstance");
  if (globalInstSelect) {
    globalInstSelect.addEventListener("change", (e) => {
      const id = e.target.value;
      if (!id || id === state.activeInstanceId) return;
      state.activeInstanceId = id;
      saveState();
      updateGlobalQuickPlay();
      if (state.screen === "instances") renderInstances();
      if (state.screen === "shaders") renderShaders();
      if (state.screen === "detail") openDetail(id);
    });
  }
  const globalPlayBtn = $("#globalPlayBtn");
  if (globalPlayBtn) {
    globalPlayBtn.addEventListener("click", () => {
      const inst = currentInstance();
      if (!inst || inst.launching) return;
      if (inst.running) {
        stopInstance(inst.id);
      } else {
        launchInstance(inst.id, { fromDetail: state.screen === "detail" });
      }
    });
  }

  // Wave 7 — Shaders
  const shaderTargetSelect = $("#shaderTargetInstance");
  if (shaderTargetSelect) {
    shaderTargetSelect.addEventListener("change", (e) => {
      const id = e.target.value;
      if (!id || id === state.activeInstanceId) return;
      state.activeInstanceId = id;
      saveState();
      updateGlobalQuickPlay();
      renderShaders();
      if (state.screen === "instances") renderInstances();
      if (state.screen === "detail") openDetail(id);
    });
  }
  const shaderSearch = $("#shaderSearch");
  if (shaderSearch) {
    shaderSearch.addEventListener("input", (e) => {
      state.shaders.query = e.target.value;
      renderShaders();
    });
  }
  const shaderStyle = $("#shaderStyle");
  if (shaderStyle) {
    shaderStyle.addEventListener("change", (e) => {
      state.shaders.style = e.target.value;
      renderShaders();
    });
  }
  const shaderGrid = $("#shaderGrid");
  if (shaderGrid) {
    shaderGrid.addEventListener("click", (e) => {
      const shaderDetails = e.target.closest("[data-shader-details]");
      if (shaderDetails) {
        const shader = SHADER_PACKS.find((item) => item.id === shaderDetails.dataset.shaderDetails);
        if (shader) openShaderDetails(shader);
        return;
      }
      const shaderBtn = e.target.closest("[data-install-shader]");
      if (shaderBtn) {
        const shader = SHADER_PACKS.find((item) => item.id === shaderBtn.dataset.installShader);
        if (shader) installShader(shader, state.activeInstanceId, shaderBtn);
        return;
      }
    });
  }

  // Wave 6 & 8 — Skins (Capes, Controls, Custom Upload, Grab)
  $("#skinAccountSelect").addEventListener("change", (e) => selectSkinAccount(e.target.value));
  $("#skinGrid").addEventListener("click", (e) => {
    const delBtn = e.target.closest("[data-delete-custom-skin]");
    if (delBtn) {
      e.stopPropagation();
      deleteCustomSkin(delBtn.dataset.deleteCustomSkin);
      return;
    }
    const card = e.target.closest("[data-skin-id]");
    if (card) selectSkinPreset(card.dataset.skinId);
  });
  $("#applySkinBtn").addEventListener("click", applySkinSelection);
  $("#resetSkinBtn").addEventListener("click", resetSelectedSkin);
  $("#randomSkinBtn").addEventListener("click", randomSkinSelection);

  // Cape selector
  const capeList = $("#skinCapeList");
  if (capeList) {
    capeList.addEventListener("click", (e) => {
      const delBtn = e.target.closest("[data-delete-custom-cape]");
      if (delBtn) {
        e.stopPropagation();
        deleteCustomCape(delBtn.dataset.deleteCustomCape);
        return;
      }
      const item = e.target.closest("[data-cape-id]");
      if (item) selectCape(item.dataset.capeId);
    });
  }

  // Model toggle (Alex Slim ↔ Steve Classic)
  $("#skinModelToggleBtn")?.addEventListener("click", toggleSkinModel);

  // Custom Cape file upload
  const capeFileInput = $("#capeFileInput");
  const capeUploadBtn = $("#capeUploadBtn");
  if (capeUploadBtn && capeFileInput) {
    capeUploadBtn.addEventListener("click", () => capeFileInput.click());
    capeFileInput.addEventListener("change", (e) => {
      if (e.target.files && e.target.files[0]) {
        handleCapeUpload(e.target.files[0]);
        e.target.value = "";
      }
    });
  }

  // Stage controls
  const animGroup = $("#skinAnimGroup");
  if (animGroup) {
    animGroup.addEventListener("click", (e) => {
      const btn = e.target.closest("[data-skin-anim]");
      if (btn) setSkinAnimation(btn.dataset.skinAnim);
    });
  }
  $("#skinAutoRotateBtn")?.addEventListener("click", toggleSkinAutoRotate);

  // Custom PNG file upload
  const fileInput = $("#skinFileInput");
  const uploadBtn = $("#skinUploadBtn");
  if (uploadBtn && fileInput) {
    uploadBtn.addEventListener("click", () => fileInput.click());
    fileInput.addEventListener("change", (e) => {
      if (e.target.files && e.target.files[0]) {
        handleSkinFileUpload(e.target.files[0]);
        e.target.value = "";
      }
    });
  }

  // Drag and drop onto stage
  const skinStage = $("#skinStage");
  const skinDropZone = $("#skinDropZone");
  if (skinStage && skinDropZone) {
    ["dragenter", "dragover"].forEach((evt) => {
      skinStage.addEventListener(evt, (e) => {
        e.preventDefault();
        e.stopPropagation();
        skinDropZone.classList.add("is-active");
      });
    });
    ["dragleave", "dragend"].forEach((evt) => {
      skinStage.addEventListener(evt, (e) => {
        e.preventDefault();
        e.stopPropagation();
        skinDropZone.classList.remove("is-active");
      });
    });
    skinStage.addEventListener("drop", (e) => {
      e.preventDefault();
      e.stopPropagation();
      skinDropZone.classList.remove("is-active");
      const files = e.dataTransfer?.files;
      if (files && files.length > 0) {
        handleSkinFileUpload(files[0]);
      }
    });
  }

  // Grab skin by username
  const grabBtn = $("#skinGrabBtn");
  const grabInput = $("#skinGrabInput");
  if (grabBtn && grabInput) {
    grabBtn.addEventListener("click", () => grabSkinByUsername(grabInput.value));
    grabInput.addEventListener("keydown", (e) => {
      if (e.key === "Enter") {
        e.preventDefault();
        grabSkinByUsername(grabInput.value);
      }
    });
  }

  // Cosmetics & Capes Store (Shop)
  const shopSearch = $("#shopSearch");
  if (shopSearch) {
    shopSearch.addEventListener("input", (e) => {
      state.shopFilter.query = e.target.value;
      renderShop();
    });
  }

  const shopCategoryChips = $("#shopCategoryChips");
  if (shopCategoryChips) {
    shopCategoryChips.addEventListener("click", (e) => {
      const chip = e.target.closest("[data-shop-cat]");
      if (!chip) return;
      $$("#shopCategoryChips .chip").forEach((c) => c.classList.remove("is-active"));
      chip.classList.add("is-active");
      state.shopFilter.category = chip.dataset.shopCat;
      renderShop();
    });
  }

  const shopSortSelect = $("#shopSortSelect");
  if (shopSortSelect) {
    shopSortSelect.addEventListener("change", (e) => {
      state.shopFilter.sort = e.target.value;
      renderShop();
    });
  }

  $("#claimDailyCoinBtn")?.addEventListener("click", claimDailyCoin);

  const shopGrid = $("#shopGrid");
  if (shopGrid) {
    shopGrid.addEventListener("click", (e) => {
      const actionBtn = e.target.closest("[data-shop-action]");
      if (!actionBtn) return;
      const action = actionBtn.dataset.shopAction;
      const itemId = actionBtn.dataset.itemId;
      if (action === "tryon") {
        openShopTryOnModal(itemId);
      } else if (action === "buy") {
        buyShopItem(itemId);
      } else if (action === "equip") {
        toggleEquipShopItem(itemId);
      }
    });
  }

  $("#shopTryOnCloseBtn")?.addEventListener("click", closeShopTryOnModal);
  $("#shopTryOnScrim")?.addEventListener("click", closeShopTryOnModal);

  // Wave 5 — Comprehensive Global Settings
  $("#saveGlobalSettingsBtn")?.addEventListener("click", saveGlobalSettings);
  $("#resetGlobalSettingsBtn")?.addEventListener("click", resetGlobalSettings);

  // Category chips
  $("#settingsCategoryChips")?.addEventListener("click", (e) => {
    const chip = e.target.closest("[data-settings-group]");
    if (!chip) return;
    filterSettingsCategory(chip.dataset.settingsGroup);
  });

  // Java & RAM Sync
  $("#detectJavaBtn")?.addEventListener("click", detectInstalledJava);
  $("#settingMinRam")?.addEventListener("input", (e) => {
    if ($("#settingMinRamValue")) $("#settingMinRamValue").value = e.target.value;
  });
  $("#settingMinRamValue")?.addEventListener("input", (e) => {
    if ($("#settingMinRam")) $("#settingMinRam").value = e.target.value;
  });
  $("#globalRam")?.addEventListener("input", (e) => {
    if ($("#globalRamValue")) $("#globalRamValue").value = e.target.value;
  });
  $("#globalRamValue")?.addEventListener("input", (e) => {
    if ($("#globalRam")) $("#globalRam").value = e.target.value;
  });

  // JVM Preset select
  $("#settingJvmPreset")?.addEventListener("change", (e) => {
    const preset = e.target.value;
    if (JVM_PRESETS[preset] && $("#settingJvmArgs")) {
      $("#settingJvmArgs").value = JVM_PRESETS[preset];
    }
  });

  // Appearance & Accents
  $("#globalTheme")?.addEventListener("change", (e) => {
    const theme = e.target.value;
    document.documentElement.dataset.theme = theme;
    if ($("#themeToggle")) $("#themeToggle").textContent = theme === "light" ? "Light" : "Dark";
  });
  $("#settingsAccentPicker")?.addEventListener("click", (e) => {
    const swatch = e.target.closest(".accent-swatch");
    if (!swatch) return;
    const accent = swatch.dataset.accent;
    state.globalSettings.accentColor = accent;
    applyAccentColor(accent);
  });
  $("#globalReduceMotion")?.addEventListener("change", (e) => {
    document.documentElement.classList.toggle("reduce-motion", e.target.checked);
    syncSkinViewerMotion();
  });

  // Storage & Diagnostics
  $("#openDataFolderBtn")?.addEventListener("click", () => {
    globalSettingsStatus("Đã mở thư mục dữ liệu (demo).");
    showToastStatus("Đã mở thư mục dữ liệu (demo).");
  });
  $("#openBackupFolderBtn")?.addEventListener("click", () => {
    globalSettingsStatus("Đã mở thư mục sao lưu (demo).");
    showToastStatus("Đã mở thư mục sao lưu (demo).");
  });
  $("#clearCacheBtn")?.addEventListener("click", clearDownloadCache);
  $("#checkForUpdatesBtn")?.addEventListener("click", checkForLauncherUpdates);
  $("#exportDiagnosticBtn")?.addEventListener("click", exportDiagnosticReport);

  // Minecraft Play Bar & Dev Tools
  const devBtn = $("#mcDevToolsBtn");
  const devPopover = $("#mcDevPopover");
  const devClose = $("#mcDevCloseBtn");
  if (devBtn && devPopover) {
    devBtn.addEventListener("click", (e) => {
      e.stopPropagation();
      devPopover.hidden = !devPopover.hidden;
    });
    if (devClose) {
      devClose.addEventListener("click", () => {
        devPopover.hidden = true;
      });
    }
    document.addEventListener("click", (e) => {
      if (!devPopover.hidden && !devPopover.contains(e.target) && e.target !== devBtn) {
        devPopover.hidden = true;
      }
    });
  }

  // Account badge -> Skins page
  $("#mcAccountBadge")?.addEventListener("click", () => showScreen("skins"));

  // Dev tools actions
  $("#simulateCrashBtn")?.addEventListener("click", () => {
    const scenario = $("#crashScenarioSelect")?.value || "oom";
    simulateCrash(scenario);
    if (devPopover) devPopover.hidden = true;
  });
  const soundBtn = $("#soundToggleBtn");
  if (soundBtn) {
    soundBtn.textContent = state.soundEnabled ? "Âm thanh: Bật" : "Âm thanh: Tắt";
    soundBtn.addEventListener("click", () => {
      state.soundEnabled = !state.soundEnabled;
      soundBtn.textContent = state.soundEnabled ? "Âm thanh: Bật" : "Âm thanh: Tắt";
      if (state.soundEnabled) playMinecraftSound("click");
      saveState();
    });
  }
  $("#themeToggle")?.addEventListener("click", () => {
    const html = document.documentElement;
    const next = html.dataset.theme === "dark" ? "light" : "dark";
    html.dataset.theme = next;
    state.globalSettings.theme = next;
    $("#themeToggle").textContent = next === "dark" ? "Dark" : "Light";
    syncGlobalSettingsUI();
    saveState();
  });
  $("#resetDemo")?.addEventListener("click", () => {
    try { localStorage.removeItem(STORAGE_KEY); } catch { /* ignore */ }
    location.reload();
  });

  document.addEventListener("pointerdown", attachRipple);
  document.addEventListener("click", (e) => {
    const clickable = e.target.closest("button, .btn, .nav-item, .instance-card, .chip, .mc-instance-selector-card, .mc-play-btn, .tab, .demo-btn");
    if (clickable) {
      playMinecraftSound("click");
    }
  }, { passive: true });

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
    // Activity Center (Ctrl+Shift+T)
    if ((e.ctrlKey || e.metaKey) && e.shiftKey && e.key.toLowerCase() === "t") {
      e.preventDefault();
      if ($("#tasksHost").classList.contains("is-open")) closeTasks();
      else openTasks();
      return;
    }

    // Command Palette (Ctrl+K)
    if ((e.ctrlKey || e.metaKey) && !e.shiftKey && !e.altKey && e.key.toLowerCase() === "k") {
      e.preventDefault();
      if (state.palette.open) closePalette();
      else openPalette();
      return;
    }

    // Launch / Stop active instance (Ctrl+P)
    if ((e.ctrlKey || e.metaKey) && !e.shiftKey && !e.altKey && e.key.toLowerCase() === "p") {
      e.preventDefault();
      const inst = currentInstance();
      if (inst && !inst.launching) {
        if (inst.running) stopInstance(inst.id);
        else launchInstance(inst.id, { fromDetail: state.screen === "detail" });
      }
      return;
    }

    // Screen switching: Alt+1 .. Alt+0
    if (e.altKey && !e.ctrlKey && !e.metaKey && ((e.key >= "1" && e.key <= "9") || e.key === "0")) {
      const screens = [
        "instances",      // Alt+1
        "mods",           // Alt+2
        "resourcepacks",  // Alt+3
        "shaders",        // Alt+4
        "datapacks",      // Alt+5
        "modpacks",       // Alt+6
        "skins",          // Alt+7
        "accounts",       // Alt+8
        "settings",       // Alt+9
      ];
      const idx = e.key === "0" ? 8 : Number(e.key) - 1;
      const targetScreen = screens[idx];
      if (targetScreen) {
        e.preventDefault();
        showScreen(targetScreen);
        return;
      }
    }

    // Focus search input: Ctrl+F or '/' (when not typing in an input/textarea)
    const activeTag = document.activeElement ? document.activeElement.tagName.toLowerCase() : "";
    const isTyping = activeTag === "input" || activeTag === "textarea" || activeTag === "select";
    if (((e.ctrlKey || e.metaKey) && !e.shiftKey && !e.altKey && e.key.toLowerCase() === "f") ||
        (e.key === "/" && !isTyping && !e.ctrlKey && !e.altKey && !e.metaKey)) {
      let targetInput = null;
      if (state.screen === "instances") targetInput = $("#instanceSearch");
      else if (state.screen === "mods") targetInput = $("#modsSearch");
      else if (state.screen === "resourcepacks") targetInput = $("#resourcepacksSearch");
      else if (state.screen === "shaders") targetInput = $("#shaderSearch");
      else if (state.screen === "datapacks") targetInput = $("#datapacksSearch");
      else if (state.screen === "modpacks") targetInput = $("#modpacksSearch");
      else if (state.screen === "detail") targetInput = $("#modSearch");
      if (targetInput) {
        e.preventDefault();
        targetInput.focus();
        targetInput.select?.();
        return;
      }
    }

    // Escape handling
    if (e.key === "Escape") {
      if ($("#msAuthModalHost") && !$("#msAuthModalHost").hidden) {
        closeMsAuthModal();
        e.preventDefault();
        return;
      }
      if ($("#offlineModalHost") && !$("#offlineModalHost").hidden) {
        closeOfflineModal();
        e.preventDefault();
        return;
      }
      if (isTyping && document.activeElement && document.activeElement.value) {
        document.activeElement.value = "";
        document.activeElement.dispatchEvent(new Event("input", { bubbles: true }));
        document.activeElement.blur();
        e.preventDefault();
        return;
      }
      if ($("#contextMenu")) $("#contextMenu").hidden = true;
      if (state.palette.open) closePalette();
      if ($("#tasksHost") && $("#tasksHost").classList.contains("is-open")) closeTasks();
      if ($("#modInspectorHost") && !$("#modInspectorHost").hidden) closeModInspector();
      if ($("#modpackInstallModalHost") && $("#modpackInstallModalHost").classList.contains("is-open")) {
        cancelModpackInstall();
        e.preventDefault();
        return;
      }
      if ($("#modpackInspectorHost") && $("#modpackInspectorHost").classList.contains("is-open")) {
        closeModpackInspectorDrawer();
        e.preventDefault();
        return;
      }
      if ($("#shopTryOnModalHost") && !$("#shopTryOnModalHost").hidden) {
        closeShopTryOnModal();
        e.preventDefault();
        return;
      }
    }
  });
}

function setDetailStatus(text) {
  $("#detailStatus").textContent = text;
}

/** Stack chứa toast: tạo lười, xếp dọc để nhiều toast không đè lên nhau. */
function toastStack() {
  let stack = $(".toast-stack");
  if (!stack) {
    stack = document.createElement("div");
    stack.className = "toast-stack";
    stack.setAttribute("role", "status");
    stack.setAttribute("aria-live", "polite");
    document.body.appendChild(stack);
  }
  return stack;
}

function showToastStatus(text) {
  const toast = document.createElement("div");
  toast.className = "toast";
  toast.textContent = text;
  toastStack().appendChild(toast);
  setTimeout(() => {
    toast.classList.add("is-out");
    setTimeout(() => toast.remove(), 450);
  }, 1900);
}

/* ------------------------------------------------------------
   MOBILE NAV DRAWER (viewport <= 620px)
   Sidebar trượt từ trái; đóng bằng Escape / click ngoài / chọn mục.
   Khoá scroll nội dung khi drawer mở để không bị trôi phía sau.
   ------------------------------------------------------------ */
const MOBILE_NAV_QUERY = "(max-width: 620px)";

function mobileNavMedia() {
  return window.matchMedia(MOBILE_NAV_QUERY);
}

function isMobileNavOpen() {
  return document.documentElement.classList.contains("nav-open");
}

let contentScrollLock = null;

function lockContentScroll(lock) {
  const content = $("#content");
  if (lock) {
    if (!contentScrollLock) {
      contentScrollLock = {
        body: document.body.style.overflow,
        content: content ? content.style.overflow : "",
      };
    }
    document.body.style.overflow = "hidden";
    if (content) content.style.overflow = "hidden";
    return;
  }
  if (!contentScrollLock) return;
  document.body.style.overflow = contentScrollLock.body;
  if (content) content.style.overflow = contentScrollLock.content;
  contentScrollLock = null;
}

function setMobileNavOpen(open, { restoreFocus = false } = {}) {
  const shouldOpen = !!open && mobileNavMedia().matches;
  document.documentElement.classList.toggle("nav-open", shouldOpen);

  const toggle = $(".nav-mobile-toggle");
  if (toggle) {
    toggle.setAttribute("aria-expanded", shouldOpen ? "true" : "false");
    toggle.setAttribute("aria-label", shouldOpen ? "Đóng điều hướng" : "Mở điều hướng");
  }

  const scrim = $(".nav-scrim");
  if (scrim) {
    scrim.classList.toggle("is-open", shouldOpen);
    scrim.setAttribute("aria-hidden", shouldOpen ? "false" : "true");
  }

  lockContentScroll(shouldOpen);

  if (shouldOpen) {
    const first = $(".nav-item", $(".nav"));
    if (first) first.focus({ preventScroll: true });
  } else if (restoreFocus && toggle) {
    toggle.focus({ preventScroll: true });
  }
}

function initMobileNav() {
  const nav = $(".nav");
  const titlebar = $(".titlebar");
  const shell = $(".shell");
  if (!nav || !titlebar || !shell || $(".nav-mobile-toggle")) return;

  if (!nav.id) nav.id = "appNav";

  const toggle = document.createElement("button");
  toggle.type = "button";
  toggle.className = "nav-mobile-toggle";
  toggle.setAttribute("aria-label", "Mở điều hướng");
  toggle.setAttribute("aria-expanded", "false");
  toggle.setAttribute("aria-controls", nav.id);
  toggle.innerHTML = '<span class="glyph" aria-hidden="true">&#xE700;</span>';
  titlebar.prepend(toggle);

  const scrim = document.createElement("div");
  scrim.className = "nav-scrim";
  scrim.setAttribute("aria-hidden", "true");
  // Nằm trong .shell để cùng stacking context với .nav (z 880 > 870),
  // nếu đặt ở .window thì scrim sẽ phủ lên cả drawer.
  shell.appendChild(scrim);

  toggle.addEventListener("click", () => setMobileNavOpen(!isMobileNavOpen()));
  scrim.addEventListener("click", () => setMobileNavOpen(false, { restoreFocus: true }));

  nav.addEventListener("click", (e) => {
    if (e.target.closest(".nav-item") && isMobileNavOpen()) setMobileNavOpen(false);
  });

  window.addEventListener("keydown", (e) => {
    if (e.key === "Escape" && isMobileNavOpen()) {
      e.preventDefault();
      setMobileNavOpen(false, { restoreFocus: true });
    }
  });

  const closeOnDesktop = () => {
    if (!mobileNavMedia().matches && isMobileNavOpen()) setMobileNavOpen(false);
  };
  const mq = mobileNavMedia();
  if (mq.addEventListener) mq.addEventListener("change", closeOnDesktop);
  else if (mq.addListener) mq.addListener(closeOnDesktop);
  window.addEventListener("resize", closeOnDesktop);
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
initMobileNav();
initMotionOverride();
initAmbient();
renderInstances();
updateGlobalQuickPlay();
renderAccounts();
renderSkins();
if ($("#discoverSearch")) $("#discoverSearch").value = state.discover?.query || "";
if ($("#discoverLoader")) $("#discoverLoader").value = state.discover?.loader || "all";
if ($("#discoverVersion")) $("#discoverVersion").value = state.discover?.version || "all";
if ($("#discoverSort")) $("#discoverSort").value = state.discover?.sort || "downloads";
if ($("#modsSearch")) $("#modsSearch").value = state.contentFilters?.mods?.query || "";
if ($("#resourcepacksSearch")) $("#resourcepacksSearch").value = state.contentFilters?.resourcepacks?.query || "";
if ($("#datapacksSearch")) $("#datapacksSearch").value = state.contentFilters?.datapacks?.query || "";
if ($("#modpacksSearch")) $("#modpacksSearch").value = state.contentFilters?.modpacks?.query || "";
const shaderSearchEl = $("#shaderSearch");
if (shaderSearchEl) shaderSearchEl.value = state.shaders.query;
const shaderStyleEl = $("#shaderStyle");
if (shaderStyleEl) shaderStyleEl.value = state.shaders.style;
renderDiscover();
renderContentPage("mods");
renderContentPage("resourcepacks");
renderContentPage("datapacks");
renderContentPage("modpacks");
renderShaders();
syncGlobalSettingsUI();
initNewsCarousel();
initDiscordRpc();
requestAnimationFrame(moveNavGlider);

window.__vazState = state;
window.__vazRenderMods = renderMods;
window.__vazPushLog = pushLog;
window.__vazRenderLogs = renderLogs;
