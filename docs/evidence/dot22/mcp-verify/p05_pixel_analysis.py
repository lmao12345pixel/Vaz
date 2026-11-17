# P0.5 - phan tich pixel khach quan (khong can vision)
# Chay: python p05_pixel_analysis.py
import os, subprocess, io
from PIL import Image
from collections import Counter

EV = r"C:\OptizimeProject\Vaz\docs\evidence\dot22\mcp-verify"
REPO = r"C:\OptizimeProject\Vaz"
ALL = ["00-launch.png", "01-overlay-fix.png", "02-toast.png",
       "03-preview-after-restart.png", "04-rp-progress.png", "05-b5-regression.png"]
COMMIT = "21978a7"


def load(path):
    im = Image.open(path)
    im.verify()
    return Image.open(path).convert("RGB")


def load_bytes(b):
    im = Image.open(io.BytesIO(b))
    im.verify()
    return Image.open(io.BytesIO(b)).convert("RGB")


def hruns(mask, w, h, minlen=8):
    out = []
    for y in range(h):
        base = y * w
        x = 0
        while x < w:
            if mask[base + x]:
                x0 = x
                while x < w and mask[base + x]:
                    x += 1
                if x - x0 >= minlen:
                    out.append((y, x0, x - 1))
            else:
                x += 1
    return out


print("=" * 72)
print("PHAN 0 - 6 anh tren dia: hop le? size? so mau?")
print("=" * 72)
for n in ALL:
    p = os.path.join(EV, n)
    try:
        im = load(p)
        c = Counter(im.getdata())
        t = c.most_common(1)[0]
        print("%-34s OK %4dx%-4d unique=%6d top1=%s(%d) bytes=%d"
              % (n, im.size[0], im.size[1], len(c), t[0], t[1], os.path.getsize(p)))
    except Exception as e:
        print("%-34s LOI: %r" % (n, e))

print()
print("=" * 72)
print("PHAN 1 - blob PNG ben trong git commit %s (khong phai file working tree)" % COMMIT)
print("=" * 72)
for n in ALL:
    rel = "docs/evidence/dot22/mcp-verify/" + n
    try:
        blob = subprocess.run(["git", "show", "%s:%s" % (COMMIT, rel)], cwd=REPO,
                              capture_output=True).stdout
        im = load_bytes(blob)
        c = Counter(im.getdata())
        wt = os.path.getsize(os.path.join(EV, n))
        print("%-34s blob=%7dB wt=%7dB %-6s %4dx%-4d unique=%d"
              % (n, len(blob), wt, "MATCH" if len(blob) == wt else "KHAC",
                 im.size[0], im.size[1], len(c)))
    except Exception as e:
        print("%-34s LOI blob: %r" % (n, e))

# ---------- PHAN 2: 03-preview-after-restart.png ----------
print()
print("=" * 72)
print("PHAN 2 - 03-preview-after-restart.png: vung 'photo-like' (thumbnail render)")
print("  = o luoi 12x12 co >90 mau rieng biet (anh that, khong phai UI phang)")
print("=" * 72)
im = load(os.path.join(EV, "03-preview-after-restart.png"))
w, h = im.size
data = list(im.getdata())
C = 12
cells = []
for gy in range(0, h - C + 1, C):
    for gx in range(0, w - C + 1, C):
        s = set()
        for yy in range(gy, gy + C):
            base = yy * w
            for xx in range(gx, gx + C):
                s.add(data[base + xx])
        if len(s) > 90:
            cells.append((gx, gy, len(s)))
print("kich thuoc anh: %dx%d ; so o photo-like: %d" % (w, h, len(cells)))
if cells:
    rows = {}
    for gx, gy, cnt in cells:
        rows.setdefault(gy // 40, []).append((gx, gy, cnt))
    for k in sorted(rows):
        v = rows[k]
        gxs = [a[0] for a in v]
        gys = [a[1] for a in v]
        avg = sum(a[2] for a in v) // len(v)
        print("  cum y~%4d: so_o=%4d x[%4d..%4d] y[%4d..%4d] mauTB/o=%d"
              % (k * 40, len(v), min(gxs), max(gxs), min(gys), max(gys), avg))
else:
    print("  KHONG tim thay o photo-like nao -> thumbnail co the khong render")

# ---------- PHAN 3: 02-toast.png ----------
print()
print("=" * 72)
print("PHAN 3 - 02-toast.png: tim toast o goc duoi phai + mau accent")
print("=" * 72)
im = load(os.path.join(EV, "02-toast.png"))
w, h = im.size
data = list(im.getdata())
cc = Counter(data)
bg = cc.most_common(1)[0][0]
print("size %dx%d ; nen app (mau top1) = %s ; unique=%d" % (w, h, bg, len(cc)))
print("top 8 mau:", cc.most_common(8))

x0, y0 = max(0, w - 560), max(0, h - 340)
print("\nvung khao sat goc duoi phai: (%d,%d)-(%d,%d)" % (x0, y0, w, h))
diff = [p != bg for p in data]
bands = {}
for y in range(y0, h):
    base = y * w
    x = x0
    while x < w:
        if diff[base + x]:
            a = x
            while x < w and diff[base + x]:
                x += 1
            if x - a >= 40:
                bands.setdefault(y // 10, []).append((a, x - 1))
        else:
            x += 1
print("so dai 'khac nen' (run>=40px) nhom theo y//10:")
for k in sorted(bands):
    v = bands[k]
    aa = min(q[0] for q in v)
    bb = max(q[1] for q in v)
    print("  dai y~%4d: %3d run  x[%4d..%4d] rong=%4d" % (k * 10, len(v), aa, bb, bb - aa + 1))

print()
for target, tol, label in [((27, 217, 106), 10, "Modrinth #1BD96A"),
                           ((16, 185, 129), 10, "emerald (16,185,129)"),
                           ((41, 127, 222), 12, "biru (41,127,222)"),
                           ((255, 255, 255), 0, "trang tuyet doi")]:
    mask = [abs(p[0] - target[0]) <= tol and abs(p[1] - target[1]) <= tol
            and abs(p[2] - target[2]) <= tol for p in data]
    n = sum(mask)
    print("%-24s : %6d px (tol %d)" % (label, n, tol))
    if n:
        idx = [i for i, m in enumerate(mask) if m]
        xs = [i % w for i in idx]
        ys = [i // w for i in idx]
        print("    bbox x[%d..%d] y[%d..%d]" % (min(xs), max(xs), min(ys), max(ys)))
        rr = [r for r in hruns(mask, w, h, 20)]
        print("    run ngang >=20px: %d %s" % (len(rr), rr[:8]))

# ---------- PHAN 4: 04-rp-progress.png ----------
print()
print("=" * 72)
print("PHAN 4 - 04-rp-progress.png: phan biet badge tinh vs progress bar that")
print("=" * 72)
im = load(os.path.join(EV, "04-rp-progress.png"))
w, h = im.size
data = list(im.getdata())
cc = Counter(data)
print("size %dx%d ; top 8 mau:", cc.most_common(8))
target = (27, 217, 106)
mask = [abs(p[0] - target[0]) <= 10 and abs(p[1] - target[1]) <= 10
        and abs(p[2] - target[2]) <= 10 for p in data]
n = sum(mask)
print("\n#1BD96A (tol 10): %d px" % n)
if n:
    idx = [i for i, m in enumerate(mask) if m]
    ys = sorted(set(i // w for i in idx))
    print("  cac dong y co pixel: %s" % (ys[:60]))
    # phan tich theo tung dong: run dai nhat
    print("  run ngang >=20px theo dong:")
    rr = hruns(mask, w, h, 20)
    per_y = {}
    for y, a, b in rr:
        per_y.setdefault(y, []).append((a, b, b - a + 1))
    for y in sorted(per_y):
        v = per_y[y]
        mx = max(q[2] for q in v)
        print("    y=%4d: %2d run, run dai nhat=%4d px, %s" % (y, len(v), mx, v[:4]))
    # vung duoi y=320 (top bar badge thuong o tren cung)
    low = [i for i in idx if i // w > 320]
    print("\n  so pixel #1BD96A o y>320 (vung noi dung, khong phai top bar): %d" % len(low))
    if low:
        xs = [i % w for i in low]
        ys2 = [i // w for i in low]
        print("    bbox x[%d..%d] y[%d..%d]" % (min(xs), max(xs), min(ys2), max(ys2)))

print()
print("=" * 72)
print("PHAN 5 - So sanh cum #1BD96A giua 02/04 va cac anh khac (badge tinh?)")
print("=" * 72)
for n2 in ALL:
    try:
        im2 = load(os.path.join(EV, n2))
        w2, h2 = im2.size
        d2 = list(im2.getdata())
        m2 = [abs(p[0] - 27) <= 10 and abs(p[1] - 217) <= 10 and abs(p[2] - 106) <= 10
              for p in d2]
        cnt = sum(m2)
        info = ""
        if cnt:
            ii = [i for i, m in enumerate(m2) if m]
            xs = [i % w2 for i in ii]
            ys = [i // w2 for i in ii]
            info = "bbox x[%d..%d] y[%d..%d]" % (min(xs), max(xs), min(ys), max(ys))
        print("%-34s #1BD96A=%6d %s" % (n2, cnt, info))
    except Exception as e:
        print("%-34s LOI: %r" % (n2, e))
