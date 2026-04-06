const express = require("express");

function createControlsRouter({ controlStore }) {
  const router = express.Router();

  router.get("/", (req, res) => {
    res.json(controlStore.get());
  });

  router.post("/start", (req, res) => {
    controlStore.start();
    res.json({ ok: true, ...controlStore.get() });
  });

  router.post("/pause", (req, res) => {
    controlStore.pause();
    res.json({ ok: true, ...controlStore.get() });
  });

  router.post("/stop", (req, res) => {
    controlStore.stop();
    res.json({ ok: true, ...controlStore.get() });
  });

  router.post("/reset", (req, res) => {
    controlStore.reset();
    res.json({ ok: true, ...controlStore.get() });
  });

  router.post("/consume-reset", (req, res) => {
    const consumed = controlStore.consumeReset();
    res.json({ ok: true, consumed, ...controlStore.get() });
  });

  return router;
}

module.exports = { createControlsRouter };

