function createSnapshotStore() {
  /** @type {any | null} */
  let latest = null;
  /** @type {string | null} */
  let lastUpdatedUtc = null;

  function set(snapshot) {
    latest = snapshot;
    lastUpdatedUtc = new Date().toISOString();
  }

  function getLatest() {
    return { latest, lastUpdatedUtc };
  }

  return { set, getLatest };
}

module.exports = { createSnapshotStore };

