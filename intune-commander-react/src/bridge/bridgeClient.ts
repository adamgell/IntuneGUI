import type { BridgeCommand, BridgeMessage } from './types';

interface PendingRequest {
  resolve: (value: unknown) => void;
  reject: (reason: Error) => void;
  timer: ReturnType<typeof setTimeout>;
}

const pendingRequests = new Map<string, PendingRequest>();
const eventListeners = new Map<string, Set<(payload: unknown) => void>>();

const DEFAULT_TIMEOUT = 10_000;
const AUTH_TIMEOUT = 120_000;

function isWebView2Available(): boolean {
  return !!window.chrome?.webview;
}

export function sendCommand<T = unknown>(command: string, payload?: unknown): Promise<T> {
  if (!isWebView2Available()) {
    // In dev mode without WebView2, return mock data
    console.warn(`[Bridge] WebView2 not available, command "${command}" will use mock data`);
    return Promise.reject(new Error('WebView2 not available'));
  }

  const id = crypto.randomUUID();
  const timeout = command.startsWith('auth.') ? AUTH_TIMEOUT : DEFAULT_TIMEOUT;

  const msg: BridgeCommand = { protocol: 'ic/1', id, command, payload };

  return new Promise<T>((resolve, reject) => {
    const timer = setTimeout(() => {
      pendingRequests.delete(id);
      reject(new Error(`Bridge command "${command}" timed out after ${timeout}ms`));
    }, timeout);

    pendingRequests.set(id, {
      resolve: resolve as (value: unknown) => void,
      reject,
      timer,
    });

    window.chrome!.webview!.postMessage(msg);
  });
}

export function onEvent(event: string, callback: (payload: unknown) => void): () => void {
  let listeners = eventListeners.get(event);
  if (!listeners) {
    listeners = new Set();
    eventListeners.set(event, listeners);
  }
  listeners.add(callback);

  return () => {
    listeners!.delete(callback);
    if (listeners!.size === 0) {
      eventListeners.delete(event);
    }
  };
}

function handleMessage(e: MessageEvent) {
  let msg: BridgeMessage;
  try {
    msg = typeof e.data === 'string' ? JSON.parse(e.data) : e.data;
  } catch {
    return;
  }

  if (msg.protocol !== 'ic/1') return;

  if (msg.type === 'response') {
    const pending = pendingRequests.get(msg.id);
    if (pending) {
      clearTimeout(pending.timer);
      pendingRequests.delete(msg.id);

      if (msg.success) {
        pending.resolve(msg.payload);
      } else {
        pending.reject(new Error(msg.error ?? 'Unknown bridge error'));
      }
    }
  } else if (msg.type === 'event') {
    const listeners = eventListeners.get(msg.event);
    if (listeners) {
      for (const cb of listeners) {
        try {
          cb(msg.payload);
        } catch (err) {
          console.error(`[Bridge] Event listener error for "${msg.event}":`, err);
        }
      }
    }
  }
}

// Initialize listener
if (isWebView2Available()) {
  window.chrome!.webview!.addEventListener('message', handleMessage);
}
