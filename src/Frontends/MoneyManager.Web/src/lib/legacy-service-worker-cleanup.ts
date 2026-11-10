/**
 * Limpeza TEMPORÁRIA do service worker de notificações push, removido na especificação 004.
 *
 * Navegadores que chegaram a registrar o antigo `/sw.js` continuariam com ele instalado (e
 * com a assinatura push ativa). Ao abrir o dashboard, cancela a assinatura e o registro,
 * sem pedir permissão e sem chamar a API. Pode ser apagado quando não houver mais
 * navegadores com o worker antigo (ex.: algumas versões depois da 004).
 */
const LEGACY_SERVICE_WORKER_PATH = "/sw.js";

export async function removeLegacyPushServiceWorker(): Promise<void> {
  if (typeof navigator === "undefined" || !("serviceWorker" in navigator)) return;

  const registrations = await navigator.serviceWorker.getRegistrations();
  for (const registration of registrations) {
    const scriptUrl =
      registration.active?.scriptURL ??
      registration.waiting?.scriptURL ??
      registration.installing?.scriptURL;
    if (!scriptUrl || new URL(scriptUrl).pathname !== LEGACY_SERVICE_WORKER_PATH) continue;

    const subscription = registration.pushManager
      ? await registration.pushManager.getSubscription()
      : null;
    await subscription?.unsubscribe();
    await registration.unregister();
  }
}
