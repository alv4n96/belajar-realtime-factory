const fs = require("node:fs/promises");
const path = require("node:path");

const REQUIRED_FILES = [
  "factory-layout.json",
  "factory-routes.json",
  "factory-process.json",
  "factory-simulation.json",
  "factory-transport.json"
];

function createConfigLoader({ configDir }) {
  async function loadBundle() {
    if (!configDir) {
      const err = new Error("Config directory not found. Set CONFIG_DIR or ensure a 'config' folder exists.");
      err.code = "CONFIG_DIR_NOT_FOUND";
      throw err;
    }

    const result = {};
    for (const file of REQUIRED_FILES) {
      const fullPath = path.join(configDir, file);
      let raw;
      try {
        raw = await fs.readFile(fullPath, "utf8");
      } catch (e) {
        const err = new Error(`Missing config file: ${fullPath}`);
        err.code = "CONFIG_FILE_MISSING";
        err.cause = e;
        throw err;
      }

      try {
        const key = file.replace(".json", "").replace("factory-", "").replaceAll("-", "_");
        result[key] = JSON.parse(raw);
      } catch (e) {
        const err = new Error(`Invalid JSON in config file: ${fullPath}`);
        err.code = "CONFIG_JSON_INVALID";
        err.cause = e;
        throw err;
      }
    }

    return {
      configDir,
      files: REQUIRED_FILES,
      bundle: result
    };
  }

  return { loadBundle };
}

module.exports = { createConfigLoader };

