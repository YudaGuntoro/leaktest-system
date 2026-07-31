export const dynamic = "force-dynamic";

type RouteContext = {
  params: Promise<{
    path?: string[];
  }>;
};

const hopByHopHeaders = new Set([
  "connection",
  "content-length",
  "expect",
  "host",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailer",
  "transfer-encoding",
  "upgrade",
]);

function getServerApiBaseUrl() {
  return (process.env.SERVER_API_BASE_URL || "http://localhost:5241").replace(/\/+$/, "");
}

function copyHeaders(headers: Headers) {
  const copied = new Headers();

  headers.forEach((value, key) => {
    if (!hopByHopHeaders.has(key.toLowerCase())) {
      copied.set(key, value);
    }
  });

  return copied;
}

async function proxyApiRequest(request: Request, context: RouteContext) {
  const sourceUrl = new URL(request.url);
  const { path = [] } = await context.params;
  const targetUrl = new URL(`/api/${path.map(encodeURIComponent).join("/")}`, getServerApiBaseUrl());
  targetUrl.search = sourceUrl.search;

  const method = request.method.toUpperCase();
  const hasBody = method !== "GET" && method !== "HEAD";
  const body = hasBody ? await request.arrayBuffer() : undefined;

  try {
    const response = await fetch(targetUrl, {
      body,
      cache: "no-store",
      headers: copyHeaders(request.headers),
      method,
      redirect: "manual",
    });

    return new Response(response.body, {
      headers: copyHeaders(response.headers),
      status: response.status,
      statusText: response.statusText,
    });
  } catch (error) {
    const message = error instanceof Error ? error.message : "Backend request failed.";

    return Response.json(
      {
        success: false,
        statusCode: 502,
        message,
        data: null,
      },
      { status: 502 }
    );
  }
}

export function GET(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}

export function HEAD(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}

export function OPTIONS(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}

export function POST(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}

export function PUT(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}

export function PATCH(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}

export function DELETE(request: Request, context: RouteContext) {
  return proxyApiRequest(request, context);
}
