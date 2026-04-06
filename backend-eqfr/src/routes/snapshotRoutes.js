const express = require("express");

function createSnapshotRouter({ snapshotStore }) {
  const router = express.Router();

  router.get("/", (req, res) => {
    const { latest, lastUpdatedUtc } = snapshotStore.getLatest();
    if (!latest) return res.status(204).end();
    return res.json({ lastUpdatedUtc, snapshot: latest });
  });

  // Producer endpoint: EQFR (atau simulasi engine lain) bisa push snapshot terbaru.
  router.post("/", (req, res) => {
    const snapshot = req.body;
    if (!snapshot || typeof snapshot !== "object") {
      return res.status(400).json({ error: "INVALID_SNAPSHOT", message: "Body must be a JSON object." });
    }

    snapshotStore.set(snapshot);
    return res.status(202).json({ ok: true });
  });

  return router;
}

module.exports = { createSnapshotRouter };

