const express = require("express");

function createConfigRouter({ configLoader }) {
  const router = express.Router();

  router.get("/", async (req, res, next) => {
    try {
      const data = await configLoader.loadBundle();
      res.json(data);
    } catch (e) {
      // Use 500 for MVP, but include a stable error code for debugging.
      res.status(500).json({
        error: e && e.code ? e.code : "CONFIG_LOAD_FAILED",
        message: e && e.message ? e.message : "Failed to load config"
      });
    }
  });

  return router;
}

module.exports = { createConfigRouter };

