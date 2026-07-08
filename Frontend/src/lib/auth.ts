import type { LoginResponse, UserResponse } from "./types";

const tokenKey = "pcms_access_token";
const userKey = "pcms_user";
const expiresKey = "pcms_expires_at";

function readStorage(key: string) {
  if (typeof window === "undefined") {
    return null;
  }

  return window.localStorage.getItem(key);
}

function parseJwtPayload(token: string) {
  try {
    const payload = token.split(".")[1];
    if (!payload) {
      return null;
    }

    const normalized = payload.replace(/-/g, "+").replace(/_/g, "/").padEnd(Math.ceil(payload.length / 4) * 4, "=");
    return JSON.parse(window.atob(normalized)) as { exp?: number };
  } catch {
    return null;
  }
}

export function saveAuthSession(response: LoginResponse) {
  window.localStorage.setItem(tokenKey, response.access_token);
  window.localStorage.setItem(userKey, JSON.stringify(response.user));
  window.localStorage.setItem(expiresKey, response.expires_at);
}

export function clearAuthSession() {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(tokenKey);
  window.localStorage.removeItem(userKey);
  window.localStorage.removeItem(expiresKey);
}

export function getStoredToken() {
  return readStorage(tokenKey);
}

export function getStoredUser(): UserResponse | null {
  const raw = readStorage(userKey);
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw) as UserResponse;
  } catch {
    return null;
  }
}

export function isTokenExpired(token: string) {
  const payload = parseJwtPayload(token);
  const expiresAt = payload?.exp ? payload.exp * 1000 : Date.parse(readStorage(expiresKey) || "");

  if (!Number.isFinite(expiresAt)) {
    return true;
  }

  return Date.now() >= expiresAt - 30_000;
}

export function hasValidAuthSession() {
  const token = getStoredToken();
  if (!token || isTokenExpired(token)) {
    clearAuthSession();
    return false;
  }

  return true;
}

export function redirectToSignIn() {
  if (typeof window === "undefined") {
    return;
  }

  const next = `${window.location.pathname}${window.location.search}`;
  window.location.href = `/signin?next=${encodeURIComponent(next)}`;
}
