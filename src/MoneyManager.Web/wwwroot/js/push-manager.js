/* ==========================================================

   MoneyManager - Push Manager

   Manages Service Worker registration and Push subscriptions.

   Called from Blazor via JS Interop.

   ========================================================== */



window.pushManager = (function () {



  // ?? Helpers ?????????????????????????????????????????????



  /** Converts a URL-safe base64 string to a Uint8Array (needed for applicationServerKey). */

  function urlBase64ToUint8Array(base64String) {

    const padding = '='.repeat((4 - base64String.length % 4) % 4);

    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');

    const raw = window.atob(base64);

    return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));

  }



  // ?? Service Worker ???????????????????????????????????????



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



  // ?? Notification permission ??????????????????????????????



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



  // ?? Push subscription ????????????????????????????????????



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



  // ?? Full registration flow ???????????????????????????????



  /**

   * Full flow: register SW ? request permission ? subscribe ? send to API.

   * @param {string} vapidPublicKey  VAPID public key (URL-safe base64).

   * @param {string} authToken       JWT bearer token.

   * Returns 'success', 'permission-denied', 'not-supported', or 'error'.

   */

  async function initPush(vapidPublicKey, authToken) {

    const registered = await registerServiceWorker();

    if (!registered) return 'not-supported';



    const permission = await requestPermission();

    if (permission !== 'granted') return 'permission-denied';



    const subscription = await subscribeToPush(vapidPublicKey);

    if (!subscription) return 'error';



    const saved = await sendSubscriptionToServer(subscription, authToken);

    return saved ? 'success' : 'error';

  }



  /**

   * Removes the push subscription from the browser and notifies the API.

   * @param {string} authToken  JWT bearer token.

   */

  async function unsubscribeFromPush(authToken) {

    if (!('serviceWorker' in navigator)) return false;



    const registration = await navigator.serviceWorker.ready;

    const subscription = await registration.pushManager.getSubscription();

    if (!subscription) return true;



    const endpoint = subscription.endpoint;

    await subscription.unsubscribe();



    try {

      await fetch('/api/push/unsubscribe', {

        method: 'DELETE',

        headers: {

          'Content-Type': 'application/json',

          'Authorization': 'Bearer ' + authToken

        },

        body: JSON.stringify({ endpoint })

      });

    } catch (err) {

      console.warn('[PushManager] Could not remove subscription from server:', err);

    }



    return true;

  }



  /**

   * Returns the current push permission state: 'granted', 'denied', or 'default'.

   */

  function getPermissionState() {

    if (!('Notification' in window)) return 'not-supported';

    return Notification.permission;

  }



  // ?? Public API ???????????????????????????????????????????

  return {

    registerServiceWorker,

    requestPermission,

    initPush,

    unsubscribeFromPush,

    getPermissionState

  };

})();

