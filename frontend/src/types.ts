export interface ServiceResponse<T = unknown> {
  isSuccess: boolean;
  statusCode: number;
  errorMessage?: string;
  data?: T;
}

export interface LoginResponse {
  accessToken: string;
  expiresIn: number;
}
export interface MicrosoftGraphOAuthStatus {
  isConnected: boolean;
  username?: string;
  accountId?: string;
}
export interface MicrosoftGraphAuthorizationUrl {
  authorizationUrl: string;
}
export interface Team {
  id: string;
  displayName?: string;
  description?: string;
}
export interface Channel {
  id: string;
  displayName?: string;
  description?: string;
  membershipType?: string;
  webUrl?: string;
}
export interface ChannelResponse {
  channels: Channel[];
}
export interface TeamWithChannels {
  team?: Team;
  channels: Channel[];
}
export interface Media {
  id: string;
  contentType: string;
  sizeBytes: number;
  objectName: string;
}
export interface StoredMessage {
  id: string;
  graphMessageId: string;
  teamId: string;
  channelId: string;
  replyToId?: string;
  subject?: string;
  htmlContent?: string;
  senderDisplayName?: string;
  messageCreatedAt?: string;
  messageLastModifiedAt?: string;
  messageDeletedAt?: string;
  webUrl?: string;
  media: Media[];
}
export interface GraphMessage {
  id?: string;
  subject?: string;
  body?: { content?: string; contentType?: string };
  from?: {
    user?: { displayName?: string };
    application?: { displayName?: string };
  };
  createdDateTime?: string;
  lastModifiedDateTime?: string;
  deletedDateTime?: string;
  webUrl?: string;
  hostedContents?: GraphHostedContent[];
}
export interface GraphHostedContent {
  id?: string;
  contentType?: string;
}
export interface SyncResult {
  receivedMessageCount: number;
  insertedMessageCount: number;
  updatedMessageCount: number;
  unchangedMessageCount: number;
  skippedMessageCount: number;
  failedMessageCount: number;
  synchronizedMediaCount: number;
  synchronizedAt: string;
}
export interface FailedMessageDeletion {
  messageId: string;
  graphMessageId: string;
  reason: string;
}

export interface MessageDeletionResult {
  teamId: string;
  channelId: string;
  fromDate: string;
  toDate: string;
  matchedMessageCount: number;
  deletedMessageCount: number;
  deletedMediaCount: number;
  failedMessageCount: number;
  failures: FailedMessageDeletion[];
  completedAt: string;
}

export interface ApplicationLog {
  id: string;
  createdAt: string;
  level: string;
  category: string;
  eventId: number;
  eventName: string | null;
  message: string | null;
  exceptionType: string | null;
  exceptionMessage: string | null;
  stackTrace: string | null;
  traceId: string | null;
  spanId: string | null;
  requestPath: string | null;
  httpMethod: string | null;
  propertiesJson: string | null;
  environment: string;
  machineName: string;
}

export interface PagedResponse<T> {
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  items: T[];
}
