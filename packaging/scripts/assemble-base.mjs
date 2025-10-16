#!/usr/bin/env node
import fs from 'fs';
import fsp from 'fs/promises';
import path from 'path';
import child from 'child_process';

const [,, artifactsDir, outDir, bundleJsonPath, version] = process.argv;
const b = JSON.parse(fs.readFileSync(bundleJsonPath,'utf8'));

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
  for (const rt of cfg.runtime) {
    const ext = rt.startsWith('win-') ? 'zip' : 'tar.gz';
    const filename = `${comp}_v${version}_${rt}.${ext}`;
    const found = child.execSync(`ls ${artifactsDir}/**/${filename}`).toString().trim().split('\n')[0];
    extract(found, compDir);
  }
}

// plugins
const pluginsDir = path.join(outDir, 'plugins');
fs.mkdirSync(pluginsDir, { recursive: true });
for (const p of b.plugins) {
  const id = p.id;
  // prefer linux-x64 by default for files (the extracted content is platform-agnostic plugin blobs)
  const preferred = [
    `Plugin-${id}_v${version}_linux-x64.tar.gz`,
    `Plugin-${id}_v${version}_win-x64.zip`,
    `Plugin-${id}_v${version}_linux-arm64.tar.gz`
  ];
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
