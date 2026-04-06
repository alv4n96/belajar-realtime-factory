const http = require("node:http");
const path = require("node:path");

const { createApp } = require("./app");
const { findConfigDirectory } = require("./config/findConfigDirectory");
const { createConfigLoader } = require("./config/configLoader");
const { createSnapshotStore } = require("./state/snapshotStore");
const { createControlStore } = require("./state/controlStore");
const { createMockFactoryRuntime } = require("./mockFactoryRuntime");

async function main() {
  const host = process.env.HOST || "0.0.0.0";
  const port = Number.parseInt(process.env.PORT || "3001", 10);

  const configDir =
    process.env.CONFIG_DIR ||
    findConfigDirectory(path.resolve(__dirname, "..", "..")) ||
    null;

  const configLoader = createConfigLoader({ configDir });
  const snapshotStore = createSnapshotStore();
  const controlStore = createControlStore();
  const runtime = createMockFactoryRuntime({ configLoader, snapshotStore, controlStore, logger: console });

  await runtime.start();

  const app = createApp({ configLoader, snapshotStore, controlStore });
  const server = http.createServer(app);

  server.listen(port, host, () => {
    const resolvedConfigDir = configDir || "(not found)";
    console.log(`[backend-eqfr] listening on http://${host}:${port}`);
    console.log(`[backend-eqfr] configDir: ${resolvedConfigDir}`);
  });

  const shutdown = () => {
    runtime.stop();
    server.close(() => process.exit(0));
  };

  process.on("SIGINT", shutdown);
  process.on("SIGTERM", shutdown);
}

main().catch((error) => {
  console.error("[backend-eqfr] failed to start", error);
  process.exit(1);
});
