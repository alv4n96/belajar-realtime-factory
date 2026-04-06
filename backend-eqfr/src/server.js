const http = require("node:http");
const path = require("node:path");

const { createApp } = require("./app");
const { findConfigDirectory } = require("./config/findConfigDirectory");
const { createConfigLoader } = require("./config/configLoader");
const { createSnapshotStore } = require("./state/snapshotStore");
const { createControlStore } = require("./state/controlStore");

const host = process.env.HOST || "0.0.0.0";
const port = Number.parseInt(process.env.PORT || "3001", 10);

const configDir =
  process.env.CONFIG_DIR ||
  findConfigDirectory(path.resolve(__dirname, "..", "..")) ||
  null;

const configLoader = createConfigLoader({ configDir });
const snapshotStore = createSnapshotStore();
const controlStore = createControlStore();

const app = createApp({ configLoader, snapshotStore, controlStore });

const server = http.createServer(app);

server.listen(port, host, () => {
  const resolvedConfigDir = configDir || "(not found)";
  // eslint-disable-next-line no-console
  console.log(`[backend-eqfr] listening on http://${host}:${port}`);
  // eslint-disable-next-line no-console
  console.log(`[backend-eqfr] configDir: ${resolvedConfigDir}`);
});

