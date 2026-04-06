const express = require("express");

function createConfigRouter({ configLoader, controlStore }) {
  const router = express.Router();

  router.get("/", async (req, res) => {
    try {
      const data = await configLoader.loadBundle();
      res.json(data);
    } catch (e) {
      res.status(500).json({
        error: e && e.code ? e.code : "CONFIG_LOAD_FAILED",
        message: e && e.message ? e.message : "Failed to load config"
      });
    }
  });

  router.post("/:section", async (req, res) => {
    try {
      const saved = await configLoader.saveSection(req.params.section, req.body);
      controlStore.reset();
      const data = await configLoader.loadBundle();
      res.json({ ok: true, saved, resetRequested: true, ...data });
    } catch (e) {
      const status = e && e.code === "CONFIG_SECTION_UNSUPPORTED" ? 400 : 500;
      res.status(status).json({
        error: e && e.code ? e.code : "CONFIG_SAVE_FAILED",
        message: e && e.message ? e.message : "Failed to save config"
      });
    }
  });

  return router;
}

module.exports = { createConfigRouter };
