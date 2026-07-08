export type QueryParamValue =
  | string
  | number
  | boolean
  | null
  | undefined
  | Array<string | number | boolean>;

export type QueryParams = Record<string, QueryParamValue>;

export type ApiRequestOptions = Omit<RequestInit, "body" | "signal"> & {
  body?: BodyInit | object | null;
  params?: QueryParams;
  signal?: AbortSignal;
  timeout?: number;
};

export type ApiClientResponse<T> = {
  data: T;
  headers: Headers;
  ok: boolean;
  status: number;
};

export class ApiError<T = unknown> extends Error {
  data?: T;
  status: number;

  constructor(message: string, status: number, data?: T) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.data = data;
  }
}

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "";
const DEFAULT_TIMEOUT = 30000;
const AUTH_COOKIE_NAMES = ["token", "accessToken", "authToken"];

const getCookie = (name: string) => {
  if (typeof document === "undefined") {
    return null;
  }

  const cookie = document.cookie
    .split("; ")
    .find((item) => item.startsWith(`${name}=`));

  return cookie ? decodeURIComponent(cookie.split("=")[1]) : null;
};

const getAuthToken = () => {
  for (const cookieName of AUTH_COOKIE_NAMES) {
    const token = getCookie(cookieName);
    if (token) {
      return token;
    }
  }

  return null;
};

const buildQueryString = (params?: QueryParams) => {
  if (!params) {
    return "";
  }

  const searchParams = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value === undefined || value === null || value === "") {
      return;
    }

    if (Array.isArray(value)) {
      value.forEach((item) => searchParams.append(key, String(item)));
      return;
    }

    searchParams.append(key, String(value));
  });

  const queryString = searchParams.toString();
  return queryString ? `?${queryString}` : "";
};

const buildUrl = (endpoint: string, params?: QueryParams) => {
  if (/^https?:\/\//i.test(endpoint)) {
    return `${endpoint}${buildQueryString(params)}`;
  }

  const baseUrl = API_BASE_URL.replace(/\/$/, "");
  const path = endpoint.startsWith("/") ? endpoint : `/${endpoint}`;

  return `${baseUrl}${path}${buildQueryString(params)}`;
};

const isFormData = (body: unknown): body is FormData =>
  typeof FormData !== "undefined" && body instanceof FormData;

const createHeaders = (body: ApiRequestOptions["body"], headers?: HeadersInit) => {
  const requestHeaders = new Headers(headers);
  const token = getAuthToken();

  if (body !== undefined && body !== null && !isFormData(body)) {
    requestHeaders.set("Content-Type", "application/json");
  }

  if (!requestHeaders.has("Accept")) {
    requestHeaders.set("Accept", "application/json");
  }

  if (token && !requestHeaders.has("Authorization")) {
    requestHeaders.set("Authorization", `Bearer ${token}`);
  }

  return requestHeaders;
};

const parseResponse = async <T>(response: Response): Promise<T> => {
  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get("content-type");

  if (contentType?.includes("application/json")) {
    return response.json() as Promise<T>;
  }

  return response.text() as Promise<T>;
};

const request = async <T>(
  endpoint: string,
  options: ApiRequestOptions = {}
): Promise<ApiClientResponse<T>> => {
  const { body, params, signal, timeout = DEFAULT_TIMEOUT, ...fetchOptions } =
    options;
  const controller = new AbortController();
  const timeoutId = globalThis.setTimeout(() => controller.abort(), timeout);
  const abortRequest = () => controller.abort();

  signal?.addEventListener("abort", abortRequest);

  try {
    const response = await fetch(buildUrl(endpoint, params), {
      ...fetchOptions,
      body:
        body === undefined || body === null || isFormData(body)
          ? (body as BodyInit | null | undefined)
          : JSON.stringify(body),
      credentials: fetchOptions.credentials ?? "include",
      headers: createHeaders(body, fetchOptions.headers),
      signal: controller.signal,
    });
    const data = await parseResponse<T>(response);

    if (!response.ok) {
      const message =
        typeof data === "object" &&
        data !== null &&
        "message" in data &&
        typeof data.message === "string"
          ? data.message
          : response.statusText;

      throw new ApiError(message, response.status, data);
    }

    return {
      data,
      headers: response.headers,
      ok: response.ok,
      status: response.status,
    };
  } finally {
    globalThis.clearTimeout(timeoutId);
    signal?.removeEventListener("abort", abortRequest);
  }
};

const api = {
  delete: <T>(endpoint: string, options?: ApiRequestOptions) =>
    request<T>(endpoint, { ...options, method: "DELETE" }),
  get: <T>(endpoint: string, options?: ApiRequestOptions) =>
    request<T>(endpoint, { ...options, method: "GET" }),
  patch: <T>(
    endpoint: string,
    body?: ApiRequestOptions["body"],
    options?: ApiRequestOptions
  ) => request<T>(endpoint, { ...options, body, method: "PATCH" }),
  post: <T>(
    endpoint: string,
    body?: ApiRequestOptions["body"],
    options?: ApiRequestOptions
  ) => request<T>(endpoint, { ...options, body, method: "POST" }),
  put: <T>(
    endpoint: string,
    body?: ApiRequestOptions["body"],
    options?: ApiRequestOptions
  ) => request<T>(endpoint, { ...options, body, method: "PUT" }),
  request,
};

export default api;
