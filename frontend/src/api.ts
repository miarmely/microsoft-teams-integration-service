import type {
  ChannelResponse,
  GraphMessage,
  LoginResponse,
  MicrosoftGraphAuthorizationUrl,
  MicrosoftGraphOAuthStatus,
  MessageDeletionResult,
  SendResult,
  ServiceResponse,
  StoredMessage,
  SyncResult,
  Team,
  WebhookUrl,
} from "./types";

/** Preserves structured API details when an operation fails or partly succeeds. */
export class ApiResponseError<T = unknown> extends Error {
  constructor(
    message: string,
    public readonly statusCode: number,
    public readonly data?: T,
  ) {
    super(message);
    this.name = "ApiResponseError";
  }
}

const API_URL =
  (import.meta.env.VITE_API_URL as string | undefined)?.replace(/\/$/, "") ??
  "http://localhost:8080";

/**
 * Sends a request to the API and unwraps its standard ServiceResponse envelope.
 * The bearer token is optional because the login endpoint is anonymous.
 */
async function request<T>(
  path: string,
  options: RequestInit = {},
  token?: string | null
): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    },
  });

  let body: ServiceResponse<T>;

  // Every API action should return JSON, including unsuccessful responses.
  try {
    body = (await response.json()) as ServiceResponse<T>;
  } catch {
    throw new Error(
      `The server returned an invalid response (${response.status}).`,
    );
  }

  // Check both HTTP status and the application's success flag.
  if (!response.ok || !body.isSuccess) {
    throw new ApiResponseError(
      body.errorMessage || `Request failed (${response.status}).`,
      response.status,
      body.data,
    );
  }

  return body.data as T;
}

/** Sends multipart form data without overriding the browser-generated boundary. */
async function requestForm<T>(
  path: string,
  form: FormData,
  token: string,
): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });
  const body = (await response.json()) as ServiceResponse<T>;
  if (!response.ok || !body.isSuccess) {
    throw new ApiResponseError(
      body.errorMessage || `Request failed (${response.status}).`,
      response.status,
      body.data,
    );
  }
  return body.data as T;
}

export interface DownloadedMedia {
  blob: Blob;
  fileName?: string;
}

/** Downloads protected binary content and preserves its API-provided filename. */
async function requestBlob(
  path: string,
  token: string,
): Promise<DownloadedMedia> {
  const response = await fetch(`${API_URL}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
  });

  if (!response.ok) {
    let message = `Download failed (${response.status}).`;
    try {
      const error = (await response.json()) as ServiceResponse;
      if (error.errorMessage) message = error.errorMessage;
    } catch {
      // Binary endpoints may return an empty or non-JSON error response.
    }
    throw new Error(message);
  }

  const disposition = response.headers.get("Content-Disposition") ?? "";
  const encodedName = disposition.match(/filename\*=UTF-8''([^;]+)/i)?.[1];
  const quotedName = disposition.match(/filename="([^"]+)"/i)?.[1];
  const fileName = encodedName
    ? decodeURIComponent(encodedName)
    : quotedName || undefined;

  return { blob: await response.blob(), fileName };
}

/** Builds an encoded query string while omitting optional undefined values. */
const query = (values: Record<string, string | number | undefined>) => {
  const params = new URLSearchParams();

  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined) params.set(key, String(value));
  });

  return params.toString();
};

export const api = {
  /** Exchanges AccessHub credentials for a JWT used by protected endpoints. */
  login: (
    username: string,
    password: string
  ) =>
    request<LoginResponse>(
      "/api/Auth/login",
      {
        method: "POST",
        body: JSON.stringify({ username, password }),
      }),
  microsoftGraphStatus: (token: string) =>
    request<MicrosoftGraphOAuthStatus>(
      "/api/microsoft-graph/oauth/status",
      {},
      token,
    ),
  microsoftGraphAuthorizationUrl: (token: string) =>
    request<MicrosoftGraphAuthorizationUrl>(
      "/api/microsoft-graph/oauth/authorization-url",
      {},
      token,
    ),
  disconnectMicrosoftGraph: (token: string) =>
    request<undefined>(
      "/api/microsoft-graph/oauth",
      { method: "DELETE" },
      token,
    ),
  /** Loads the teams available to the current user. */
  teams: (token: string) => request<Team[]>("/api/Teams", {}, token),
  /** Loads channels only for the team selected by the user. */
  channels: (token: string, teamId: string) =>
    request<ChannelResponse>(
      `/api/Teams/${encodeURIComponent(teamId)}/channels`,
      {},
      token,
    ),
  /** Returns all database-backed channel webhook assignments. */
  webhooks: (token: string) =>
    request<WebhookUrl[]>("/api/WebhookUrl", {}, token),
  /** Creates a workflow webhook assignment for one channel. */
  createWebhook: (
    token: string,
    teamId: string,
    channelId: string,
    url: string,
  ) =>
    request<WebhookUrl>(
      "/api/WebhookUrl",
      { method: "POST", body: JSON.stringify({ teamId, channelId, url }) },
      token,
    ),
  /** Updates an existing channel webhook assignment. */
  updateWebhook: (
    token: string,
    id: string,
    teamId: string,
    channelId: string,
    url: string,
  ) =>
    request<WebhookUrl>(
      `/api/WebhookUrl/${encodeURIComponent(id)}`,
      { method: "PUT", body: JSON.stringify({ teamId, channelId, url }) },
      token,
    ),
  /** Deletes a channel webhook assignment. */
  deleteWebhook: (token: string, id: string) =>
    request<undefined>(
      `/api/WebhookUrl/${encodeURIComponent(id)}`,
      { method: "DELETE" },
      token,
    ),
  /** Downloads a live message attachment from Graph through the API. */
  liveMedia: (
    token: string,
    teamId: string,
    channelId: string,
    messageId: string,
    hostedContentId: string,
  ) =>
    requestBlob(
      `/api/Teams/${encodeURIComponent(teamId)}/channels/${encodeURIComponent(channelId)}/messages/${encodeURIComponent(messageId)}/media/${encodeURIComponent(hostedContentId)}`,
      token,
    ),
  /** Downloads synchronized message media from MinIO through the API. */
  storedMedia: (token: string, mediaId: string) =>
    requestBlob(`/api/Message/media/${encodeURIComponent(mediaId)}`, token),
  /** Downloads synchronized channel messages and images as a ZIP archive. */
  exportMessages: (
    token: string,
    teamId: string,
    channelId: string,
    fromDate?: string,
    toDate?: string,
  ) =>
    requestBlob(
      `/api/Message/team/${encodeURIComponent(teamId)}/channel/${encodeURIComponent(channelId)}/export?${query({ fromDate, toDate })}`,
      token,
    ),
  /** Permanently deletes synchronized messages and their MinIO media. */
  deleteSynchronizedMessages: (
    token: string,
    teamId: string,
    channelId: string,
    fromDate: string,
    toDate: string,
  ) =>
    request<MessageDeletionResult>(
      `/api/Message/team/${encodeURIComponent(teamId)}/channel/${encodeURIComponent(channelId)}?${query({ fromDate, toDate })}`,
      { method: "DELETE" },
      token,
    ),
  /** Fetches channel messages directly from Microsoft Graph. */
  liveMessages: (
    token: string,
    teamId: string,
    channelId: string,
    fromDate: string,
    toDate?: string,
    pageNumber = 1,
    pageSize = 50,
  ) =>
    request<GraphMessage[]>(
      `/api/Teams/${encodeURIComponent(teamId)}/channels/${encodeURIComponent(channelId)}/messages?${query({ fromDate, toDate, pageNumber, pageSize })}`,
      {},
      token,
    ),
  /** Reads messages already synchronized into PostgreSQL. */
  storedMessages: (
    token: string,
    teamId: string,
    channelId: string,
    pageNumber = 1,
    pageSize = 50,
  ) =>
    request<StoredMessage[]>(
      `/api/Message/team/${encodeURIComponent(teamId)}/channel/${encodeURIComponent(channelId)}?${query({ pageNumber, pageSize })}`,
      {},
      token,
    ),
  /** Starts synchronization of a Teams channel for the selected date range. */
  sync: (
    token: string,
    teamId: string,
    channelId: string,
    fromDate: string,
    toDate?: string,
  ) =>
    request<SyncResult>(
      `/api/TeamsSync/${encodeURIComponent(teamId)}/channels/${encodeURIComponent(channelId)}/sync?${query({ fromDate, toDate })}`,
      {
        method: "POST"
      },
      token,
    ),
  /** Sends one Adaptive Card through the configured Teams workflow. */
  send: (
    token: string,
    teamId: string,
    channelId: string,
    title: string,
    content: string[],
    imageUrl?: string,
    imageAltText?: string,
  ) =>
    request<SendResult>(
      "/api/Teams/message/send",
      {
        method: "POST",
        body: JSON.stringify({
          teamId,
          channelId,
          messages: [
            {
              title: title || null,
              content,
              images: imageUrl
                ? [{ imageUrl, imageAltText: imageAltText || null }]
                : [],
            },
          ],
        }),
      },
      token,
    ),
  sendHostedAdaptiveCard: (
    token: string,
    teamId: string,
    channelId: string,
    title: string,
    description: string,
    image: File,
  ) => {
    const form = new FormData();
    form.set("teamId", teamId);
    form.set("channelId", channelId);
    form.set("title", title);
    form.set("description", description);
    form.set("image", image);
    return requestForm<GraphMessage>("/api/Teams/adaptive-card", form, token);
  },
};
