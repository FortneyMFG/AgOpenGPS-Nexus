#!/usr/bin/env node
import fs from 'fs';
import path from 'path';
import child from 'child_process';

const [,, artifactsDir, outDir, version, ...runtimeArgs] = process.argv;
fs.mkdirSync(outDir, { recursive: true });

function extract(src, dest) {
  fs.mkdirSync(dest, { recursive: true });
  if (src.endsWith('.zip')) child.execSync(`unzip -q "${src}" -d "${dest}"`);
  else child.execSync(`tar -xzf "${src}" -C "${dest}"`);
}

const tryFind = (pattern) => {
  try { return child.execSync(`ls ${artifactsDir}/**/${pattern}`).toString().trim().split('\n')[0]; }
  catch { return null; }
};

const order = runtimeArgs.filter(Boolean);
const runtimeOrder = order.length ? order : ['linux-x64', 'win-x64'];
let found = null;
for (const rt of runtimeOrder) {
  const ext = rt.startsWith('win-') ? 'zip' : 'tar.gz';
  found = tryFind(`ui_v${version}_${rt}.${ext}`);
  if (found) break;
}
if (!found) throw new Error('UI artifact not found for UI-only bundle.');
extract(found, outDir);

// write release.json
fs.writeFileSync(path.join(outDir, 'release.json'), JSON.stringify({ name: 'Nexus UI', version }, null, 2));
