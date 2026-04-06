const fs = require("node:fs/promises");
const path = require("node:path");

const SECTION_FILE_MAP = {
  layout: "factory-layout.json",
  routes: "factory-routes.json",
  process: "factory-process.json",
  simulation: "factory-simulation.json",
  transport: "factory-transport.json"
};

const REQUIRED_FILES = Object.values(SECTION_FILE_MAP);

function createConfigLoader({ configDir }) {
  async function loadBundle() {
    ensureConfigDir();

    const result = {};
    for (const [section, file] of Object.entries(SECTION_FILE_MAP)) {
      result[section] = await readJsonFile(path.join(configDir, file));
    }

    return {
      configDir,
      files: REQUIRED_FILES,
      bundle: result
    };
  }

  async function saveSection(section, payload) {
    ensureConfigDir();

    const normalizedSection = normalizeSection(section);
    const file = SECTION_FILE_MAP[normalizedSection];
    if (!file) {
      const err = new Error(`Unsupported config section: '${section}'.`);
      err.code = "CONFIG_SECTION_UNSUPPORTED";
      throw err;
    }

    const fullPath = path.join(configDir, file);
    const raw = `${JSON.stringify(payload, null, 2)}\n`;
    await fs.writeFile(fullPath, raw, "utf8");

    return {
      section: normalizedSection,
      file,
      fullPath
    };
  }

  return { loadBundle, saveSection };

  function ensureConfigDir() {
    if (!configDir) {
      const err = new Error("Config directory not found. Set CONFIG_DIR or ensure a 'config' folder exists.");
      err.code = "CONFIG_DIR_NOT_FOUND";
      throw err;
    }
  }
}

async function readJsonFile(fullPath) {
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
    return JSON.parse(raw);
  } catch (e) {
    const err = new Error(`Invalid JSON in config file: ${fullPath}`);
    err.code = "CONFIG_JSON_INVALID";
    err.cause = e;
    throw err;
  }
}

function normalizeSection(section) {
  return String(section || "")
    .trim()
    .toLowerCase()
    .replaceAll("-", "_")
    .replaceAll(" ", "_");
}

module.exports = { createConfigLoader };
