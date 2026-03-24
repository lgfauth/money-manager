/* ==========================================================

   MoneyManager - Push Manager

   Manages Service Worker registration and Push subscriptions.

   Called from Blazor via JS Interop.

   ========================================================== */
   
window.pushManager = (function () {

  /** Converts a URL-safe base64 string to a Uint8Array (needed for applicationServerKey). */

  function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = window.atob(base64);

    return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
  }

  /**
   * Registers the service worker located at /service-worker.js.
   * Returns true on success, false if unsupported or registration fails.
   */

  async function registerServiceWorker() {
    if (!('serviceWorker' in navigator)) {
      console.warn('[PushManager] Service Workers not supported.');

      return false;
    }

    try {
      const registration = await navigator.serviceWorker.register('/service-worker.js', {
        scope: '/'
      });

      console.info('[PushManager] Service worker registered:', registration.scope);

      return true;
    } catch (err) {
      console.error('[PushManager] Service worker registration failed:', err);

      return false;
    }
  }

  /**
   * Requests notification permission from the user.
   * Returns 'granted', 'denied' or 'default'.
   */

  async function requestPermission() {
    if (!('Notification' in window)) {
      console.warn('[PushManager] Notifications not supported.');

      return 'denied';
    }

    if (Notification.permission === 'granted') return 'granted';

    const result = await Notification.requestPermission();

    console.info('[PushManager] Notification permission:', result);

    return result;
  }

  /**
   * Subscribes to push notifications using the provided VAPID public key.
   * Returns the PushSubscription object, or null on failure.
   */

  async function subscribeToPush(vapidPublicKey) {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
      console.warn('[PushManager] Push not supported.');
      return null;
    }

    const registration = await navigator.serviceWorker.ready;
    console.info('[PushManager] Service worker is ready, creating push subscription...');

    // Remove any stale subscription before creating a fresh one
    try {
      const existing = await registration.pushManager.getSubscription();
      if (existing) {
        console.info('[PushManager] Removing existing subscription before re-subscribing.');
        await existing.unsubscribe();
      }
    } catch (err) {
      console.warn('[PushManager] Could not remove existing subscription:', err);
    }

    try {
      const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
      });

      console.info('[PushManager] Push subscription created:', subscription.endpoint);
      return subscription;
    } catch (err) {
      console.error('[PushManager] Push subscription failed:', err);
      return null;
    }
  }

  /**
   * Sends the PushSubscription to the API endpoint POST /api/push/subscribe.
   * @param {PushSubscription} subscription
   * @param {string} authToken  Bearer token for the authenticated user.
   * Returns true on success.
   */

  async function sendSubscriptionToServer(subscription, authToken) {
    const keys = subscription.getKey ? {
      p256dh: btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('p256dh')))),
      auth: btoa(String.fromCharCode(...new Uint8Array(subscription.getKey('auth'))))
    } : { p256dh: '', auth: '' };

    const payload = {
      endpoint: subscription.endpoint,
      p256dh: keys.p256dh,
      auth: keys.auth,
      userAgent: navigator.userAgent
    };

    try {
      const response = await fetch('/api/push/subscribe', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ' + authToken
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        console.error('[PushManager] Server rejected subscription:', response.status);

        return false;
      }

      console.info('[PushManager] Subscription saved on server.');

      return true;
    } catch (err) {
      console.error('[PushManager] Failed to send subscription to server:', err);

      return false;
    }
  }

  /**
   * Full flow: register SW ? request permission ? subscribe ? send to API.
   * @param {string} vapidPublicKey  VAPID public key (URL-safe base64).
   * @param {string} authToken       JWT bearer token.
   * Returns 'success', 'permission-denied', 'not-supported', or 'error'.
   */

  async function initPush(vapidPublicKey, authToken) {
    console.info('[PushManager] initPush: starting full activation flow...');

    console.info('[PushManager] Step 1: Registering service worker...');
    const registered = await registerServiceWorker();
    if (!registered) {
      console.warn('[PushManager] initPush: service worker registration failed.');
      return 'not-supported';
    }

    console.info('[PushManager] Step 2: Requesting notification permission...');
    const permission = await requestPermission();
    if (permission !== 'granted') {
      console.warn('[PushManager] initPush: permission not granted:', permission);
      return 'permission-denied';
    }

    console.info('[PushManager] Step 3: Creating push subscription...');
    const subscription = await subscribeToPush(vapidPublicKey);
    if (!subscription) {
      console.error('[PushManager] initPush: push subscription creation failed.');
      return 'error';
    }

    console.info('[PushManager] Step 4: Sending subscription to server...');
    const saved = await sendSubscriptionToServer(subscription, authToken);
    if (!saved) {
      console.error('[PushManager] initPush: failed to save subscription on server.');
      return 'error';
    }

    console.info('[PushManager] initPush: activation complete.');
    return 'success';
  }

  /**
   * Removes the push subscription from the browser and notifies the API.
   * @param {string} authToken  JWT bearer token.
   */

  async function unsubscribeFromPush(authToken) {
    console.info('[PushManager] unsubscribeFromPush: starting...');

    if (!('serviceWorker' in navigator)) {
      console.warn('[PushManager] Service workers not supported.');
      return false;
    }

    const registration = await navigator.serviceWorker.ready;
    const subscription = await registration.pushManager.getSubscription();

    if (!subscription) {
      console.info('[PushManager] No active browser subscription found — already unsubscribed.');
      return true;
    }

    const endpoint = subscription.endpoint;
    console.info('[PushManager] Removing browser subscription:', endpoint);

    const browserOk = await subscription.unsubscribe();
    if (!browserOk) {
      console.error('[PushManager] Browser failed to unsubscribe.');
      return false;
    }
    console.info('[PushManager] Browser subscription removed.');

    try {
      const response = await fetch('/api/push/unsubscribe', {
        method: 'DELETE',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer ' + authToken
        },
        body: JSON.stringify({ endpoint })
      });

      if (!response.ok) {
        console.warn('[PushManager] Server could not remove subscription:', response.status);
        return false;
      }

      console.info('[PushManager] Subscription removed from server.');
      return true;
    } catch (err) {
      console.warn('[PushManager] Could not reach server to remove subscription:', err);
      return false;
    }
  }

  /**
   * Returns the current push permission state: 'granted', 'denied', or 'default'.
   */

  function getPermissionState() {
    if (!('Notification' in window)) return 'not-supported';

    return Notification.permission;
  }

  return {
    registerServiceWorker,
    requestPermission,
    initPush,
    unsubscribeFromPush,
    getPermissionState
  };
})();