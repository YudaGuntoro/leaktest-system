"use client";

import YanmarMark from "@/components/brand/YanmarMark";
import { EyeCloseIcon, EyeIcon } from "@/icons";
import { useRouter, useSearchParams } from "next/navigation";
import { FormEvent, useEffect, useId, useState } from "react";
import { apiPost } from "@/lib/api";
import { hasValidAuthSession, saveAuthSession } from "@/lib/auth";
import type { LoginResponse } from "@/lib/types";

function safeNextPath(value: string | null) {
  return value?.startsWith("/") && !value.startsWith("//") && !value.startsWith("/signin") ? value : "/";
}

export default function SignInPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const nextPath = safeNextPath(searchParams.get("next"));
  const usernameFieldId = useId().replace(/:/g, "");
  const passwordFieldId = useId().replace(/:/g, "");
  const [showPassword, setShowPassword] = useState(false);
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [autofillLocked, setAutofillLocked] = useState(true);
  const [fieldNonce, setFieldNonce] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (hasValidAuthSession()) {
      router.replace(nextPath);
    }
  }, [nextPath, router]);

  useEffect(() => {
    const clearAutofill = () => {
      setUsername("");
      setPassword("");
      document.querySelectorAll<HTMLInputElement>("[data-login-field]").forEach((field) => {
        field.value = "";
      });
    };

    setFieldNonce(crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2)}`);
    clearAutofill();
    const timers = [150, 600, 1200, 2400].map((delay) => window.setTimeout(clearAutofill, delay));
    return () => timers.forEach(window.clearTimeout);
  }, []);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!username.trim() || !password) {
      setError("Username and password are required.");
      return;
    }

    setLoading(true);
    setError(null);
    try {
      const response = await apiPost<LoginResponse>("/api/auth/login", {
        username: username.trim(),
        password,
      });
      saveAuthSession(response);
      router.push(nextPath);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed.");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="relative flex min-h-screen w-full items-center justify-center bg-white px-4 py-10 dark:bg-gray-950 sm:px-6 lg:w-1/2 lg:px-10">
      <div className="relative w-full max-w-[448px]">
        <div className="rounded-2xl border border-[#d9e2ef] bg-white px-8 py-10 shadow-[0_24px_60px_rgba(15,23,42,0.14)] dark:border-gray-800 dark:bg-gray-900 dark:shadow-[0_24px_60px_rgba(0,0,0,0.28)] sm:px-8">
          <div className="mb-8 text-center">
            <YanmarMark className="mx-auto h-24 w-24 scale-x-125 text-[#e60028]" />
            <h1 className="mt-6 text-3xl font-extrabold text-[#111827] dark:text-white/90">Yanmar</h1>
            <p className="mt-3 text-sm text-[#263a56] dark:text-gray-400">Sign in to access your dashboard</p>
          </div>

          {error ? (
            <div className="mb-5 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-700 dark:border-red-500/30 dark:bg-red-500/15 dark:text-red-300">
              {error}
            </div>
          ) : null}

          <form autoComplete="off" data-form-type="other" onSubmit={(event) => void submit(event)}>
            <div className="space-y-4">
              <div>
                <div className="relative">
                  <input
                    autoCapitalize="none"
                    autoComplete="new-password"
                    className="h-[50px] w-full rounded-md border border-[#c7d4e8] bg-white px-10 text-sm text-[#172033] outline-none transition placeholder:text-[#8498b6] focus:border-[#e60028] focus:ring-3 focus:ring-red-500/10 dark:border-gray-700 dark:bg-gray-950 dark:text-white/90 dark:placeholder:text-gray-500"
                    data-1p-ignore="true"
                    data-form-type="other"
                    data-login-field="true"
                    data-lpignore="true"
                    disabled={loading}
                    id={`production-user-${usernameFieldId}`}
                    inputMode="text"
                    name={fieldNonce ? `production-operator-${fieldNonce}` : `production-operator-${usernameFieldId}`}
                    onChange={(event) => setUsername(event.target.value)}
                    onFocus={() => setAutofillLocked(false)}
                    placeholder="Username"
                    readOnly={autofillLocked}
                    spellCheck={false}
                    type="search"
                    value={username}
                  />
                </div>
              </div>

              <div>
                <div className="relative">
                  <input
                    autoComplete="new-password"
                    className="h-[50px] w-full rounded-md border border-[#c7d4e8] bg-white px-10 pr-12 text-sm text-[#172033] outline-none transition placeholder:text-[#8498b6] focus:border-[#e60028] focus:ring-3 focus:ring-red-500/10 dark:border-gray-700 dark:bg-gray-950 dark:text-white/90 dark:placeholder:text-gray-500"
                    data-1p-ignore="true"
                    data-form-type="other"
                    data-login-field="true"
                    data-lpignore="true"
                    disabled={loading}
                    id={`production-secret-${passwordFieldId}`}
                    name={fieldNonce ? `production-key-${fieldNonce}` : `production-key-${passwordFieldId}`}
                    onChange={(event) => setPassword(event.target.value)}
                    onFocus={() => setAutofillLocked(false)}
                    placeholder="Password"
                    readOnly={autofillLocked}
                    type={showPassword ? "text" : "password"}
                    value={password}
                  />
                  <button
                    aria-label={showPassword ? "Hide password" : "Show password"}
                    className="absolute right-4 top-1/2 z-30 -translate-y-1/2 cursor-pointer text-[#8498b6] transition-colors hover:text-[#536982] disabled:cursor-not-allowed disabled:opacity-50 dark:text-gray-500 dark:hover:text-gray-300"
                    disabled={loading}
                    onClick={() => setShowPassword((current) => !current)}
                    type="button"
                  >
                    {showPassword ? (
                      <EyeIcon className="fill-current" />
                    ) : (
                      <EyeCloseIcon className="fill-current" />
                    )}
                  </button>
                </div>
              </div>

              <button
                className="mt-1 inline-flex h-12 w-full items-center justify-center rounded-lg bg-[#ec101d] px-4 text-sm font-bold text-white shadow-[0_10px_18px_rgba(236,16,29,0.24)] transition-colors hover:bg-[#d90d19] focus:outline-none focus:ring-3 focus:ring-red-500/20 disabled:cursor-not-allowed disabled:opacity-60"
                disabled={loading}
                type="submit"
              >
                {loading ? "Processing..." : "Sign In"}
              </button>
            </div>
          </form>

          <p className="mt-8 text-center text-[11px] font-medium text-[#536982] dark:text-gray-500">Protected by enterprise-grade security protocols</p>
        </div>
      </div>
    </div>
  );
}
