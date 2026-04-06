const fs = require("node:fs");
const path = require("node:path");

function findConfigDirectory(startDir) {
  let current = path.resolve(startDir);

  for (let i = 0; i < 10; i += 1) {
    const candidate = path.join(current, "config");
    try {
      if (fs.existsSync(candidate) && fs.statSync(candidate).isDirectory()) {
        return candidate;
      }
    } catch {
      // ignore
    }

    const parent = path.dirname(current);
    if (!parent || parent === current) return null;
    current = parent;
  }

  return null;
}

module.exports = { findConfigDirectory };

