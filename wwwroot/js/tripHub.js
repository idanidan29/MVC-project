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
    // payload: { tripId, availableRooms }
    const { tripId, availableRooms } = payload || {};
    // Simple demo: dispatch a custom event so app code can react
    const event = new CustomEvent('trip-availability-updated', { detail: { tripId, availableRooms } });
    window.dispatchEvent(event);
    // Optionally update DOM if elements follow a convention
    const el = document.querySelector(`[data-trip-id="${tripId}"][data-role="available-rooms"]`);
    if (el) {
      el.textContent = availableRooms;
    }
  });

  connection.start().catch(err => console.error('SignalR start error:', err));
  window.signalRConnection = connection;
})();
