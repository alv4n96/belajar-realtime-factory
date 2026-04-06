const express = require("express");

const { createHealthRouter } = require("./routes/healthRoutes");
const { createConfigRouter } = require("./routes/configRoutes");
const { createSnapshotRouter } = require("./routes/snapshotRoutes");
const { createControlsRouter } = require("./routes/controlRoutes");

function createApp({ configLoader, snapshotStore, controlStore }) {
  const app = express();

  // Minimal CORS for MVP (no auth yet).
  app.use((req, res, next) => {
    res.setHeader("Access-Control-Allow-Origin", "*");
    res.setHeader("Access-Control-Allow-Methods", "GET,POST,OPTIONS");
    res.setHeader("Access-Control-Allow-Headers", "Content-Type");
    if (req.method === "OPTIONS") return res.status(204).end();
    return next();
  });

  app.use(express.json({ limit: "1mb" }));

  app.use(createHealthRouter());
  app.use("/api/config", createConfigRouter({ configLoader, controlStore }));
  app.use("/api/snapshot", createSnapshotRouter({ snapshotStore }));
  app.use("/api/controls", createControlsRouter({ controlStore }));

  app.use((req, res) => {
    res.status(404).json({ error: "not_found", path: req.path });
  });

  // eslint-disable-next-line no-unused-vars
  app.use((err, req, res, next) => {
    res.status(500).json({
      error: "internal_error",
      message: err && err.message ? err.message : "Unknown error"
    });
  });

  return app;
}

module.exports = { createApp };
