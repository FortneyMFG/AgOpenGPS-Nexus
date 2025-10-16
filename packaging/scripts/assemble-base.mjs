#!/usr/bin/env node
import fs from 'fs';
import fsp from 'fs/promises';
import path from 'path';
import child from 'child_process';

const [,, artifactsDir, outDir, bundleJsonPath, version, ...runtimeArgs] = process.argv;
const b = JSON.parse(fs.readFileSync(bundleJsonPath,'utf8'));
const runtimeFilter = runtimeArgs.filter(Boolean);
const runtimeSet = runtimeFilter.length ? new Set(runtimeFilter) : null;

function ensureMatch(comp, matched) {
  if (runtimeSet && !matched) {
    throw new Error(`No artifacts resolved for ${comp} with runtimes [${runtimeFilter.join(', ')}]`);
  }
}

// helper to extract one archive to target
function extract(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  if (src.endsWith('.zip')) {
    child.execSync(`unzip -q "${src}" -d "${dest}"`);
  } else {
    child.execSync(`tar -xzf "${src}" -C "${dest}"`);
  }
}

fs.mkdirSync(outDir, { recursive: true });

// components
for (const [comp, cfg] of Object.entries(b.components)) {
  const compDir = path.join(outDir, comp);
  fs.mkdirSync(compDir, { recursive: true });
  let matched = false;
  for (const rt of cfg.runtime) {
    if (runtimeSet && !runtimeSet.has(rt)) continue;
    const ext = rt.startsWith('win-') ? 'zip' : 'tar.gz';
    const filename = `${comp}_v${version}_${rt}.${ext}`;
    const found = child.execSync(`ls ${artifactsDir}/**/${filename}`).toString().trim().split('\n')[0];
    extract(found, compDir);
    matched = true;
  }
  ensureMatch(comp, matched);
}

// plugins
const pluginsDir = path.join(outDir, 'plugins');
fs.mkdirSync(pluginsDir, { recursive: true });
for (const p of b.plugins) {
  const id = p.id;
  // prefer linux-x64 by default for files (the extracted content is platform-agnostic plugin blobs)
  const candidateRuntimes = runtimeFilter.length ? runtimeFilter : ['linux-x64', 'win-x64', 'linux-arm64'];
  const preferred = candidateRuntimes.map(rt => {
    const ext = rt.startsWith('win-') ? 'zip' : 'tar.gz';
    return `Plugin-${id}_v${version}_${rt}.${ext}`;
  });
  let found = null;
  for (const name of preferred) {
    try {
      const pth = child.execSync(`ls ${artifactsDir}/**/${name}`).toString().trim().split('\n')[0];
      if (pth) { found = pth; break; }
    } catch {}
  }
  if (!found) throw new Error(`Plugin missing for bundle: ${id}`);
  extract(found, path.join(pluginsDir, id));
}

// docs & root files
for (const doc of (b.docs||[])) {
  if (fs.existsSync(doc)) fs.copyFileSync(doc, path.join(outDir, path.basename(doc)));
}
for (const root of (b.rootFiles||[])) {
  if (fs.existsSync(`packaging/common/${root}`)) fs.copyFileSync(`packaging/common/${root}`, path.join(outDir, root));
}

// write release.json
fs.writeFileSync(path.join(outDir, 'release.json'), JSON.stringify({
  name: b.name, version, createdUtc: new Date().toISOString()
}, null, 2));
