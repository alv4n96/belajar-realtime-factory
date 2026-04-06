const VALID_STATUSES = ["Stopped", "Running", "Paused"];

function createControlStore() {
  let desiredStatus = "Running";
  let resetRequested = false;
  let lastChangedUtc = new Date().toISOString();

  function get() {
    return { desiredStatus, resetRequested, lastChangedUtc };
  }

  function setStatus(next) {
    if (!VALID_STATUSES.includes(next)) {
      const err = new Error(`Invalid desiredStatus: ${next}`);
      err.code = "INVALID_STATUS";
      throw err;
    }
    desiredStatus = next;
    lastChangedUtc = new Date().toISOString();
  }

  function start() {
    setStatus("Running");
  }

  function pause() {
    setStatus("Paused");
  }

  function stop() {
    setStatus("Stopped");
  }

  function reset() {
    resetRequested = true;
    desiredStatus = "Running";
    lastChangedUtc = new Date().toISOString();
  }

  function consumeReset() {
    const wasRequested = resetRequested;
    resetRequested = false;
    if (wasRequested) lastChangedUtc = new Date().toISOString();
    return wasRequested;
  }

  return { get, start, pause, stop, reset, consumeReset };
}

module.exports = { createControlStore };
