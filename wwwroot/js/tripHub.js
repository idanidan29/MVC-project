// Minimal client that listens for availability updates
// Requires @microsoft/signalr client script to be loaded on the page
// Example CDN: <script src="https://cdn.jsdelivr.net/npm/@microsoft/signalr@latest/dist/browser/signalr.min.js"></script>

(function() {
  if (!window.signalR && !window.signalr && !window.signalRConnection) {
    // Try global SignalR from CDN
  }
  const connection = new signalR.HubConnectionBuilder()
    .withUrl('/tripHub')
    .withAutomaticReconnect()
    .build();

  connection.on('availabilityUpdated', (payload) => {
    // payload: { tripId, availableRooms, selectedDateIndex }
    const { tripId, availableRooms, selectedDateIndex = -1 } = payload || {};

    // Dispatch a custom event so page-specific code can react (e.g., modal updates)
    const event = new CustomEvent('trip-availability-updated', { detail: { tripId, availableRooms, selectedDateIndex } });
    window.dispatchEvent(event);

    // If the page uses data-role hooks, update inline counts
    const mainEl = document.querySelector(`[data-trip-id="${tripId}"][data-role="available-rooms"]`);
    if (mainEl && selectedDateIndex === -1) {
      mainEl.textContent = availableRooms;
    }

    // Update date variation blocks if present (uses data-date-index)
    if (selectedDateIndex >= 0) {
      const variationEl = document.querySelector(`.date-variation-item[data-date-index="${selectedDateIndex}"] .date-rooms-display`);
      if (variationEl) {
        variationEl.textContent = `${availableRooms} rooms available`;
      }
    }
  });

  connection.start().catch(err => console.error('SignalR start error:', err));
  window.signalRConnection = connection;
})();
