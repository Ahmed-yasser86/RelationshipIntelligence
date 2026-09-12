// Backend DTO mirrors. The API serializes JSON in camelCase - property names
// below match the actual wire format exactly.

export interface AuthResponse {
  token: string;
  personeName: string;
  personeEmail: string;
  refreshToken: string;
  RefreshTokenExpirationTime: string;
  ExpirationTime: string;
}

export interface CircleResponse {
  circleId: string;
  name: string;
}
export interface ContactItemRoleResponse {
  contactsRoleId: string;
  role: string;
}
export interface ConnectionChannelResponse {
  connectionChannelId: string;
  connectionChannelName: string;
}
export interface SystemStatusTagResponse {
  statusTagId: number;
  name: string;
  description: string | null;
}
export interface UserDefinedTagsResponse {
  tagId: string;
  tagName: string;
}
export interface SocialMediaAccountResponse {
  socialMediaAccountId: string;
  platform: string | null;
  url: string;
}
export interface NoteResponse {
  noteId: string;
  noteType: number;
  content: string;
}
export interface InteractionResponse {
  interactionId: string;
  personId: string;
  interactionType: number;
  interactionTitle: string;
  interactionDescription: string | null;
  timeOfInteraction: string;
}

export interface PersonView {
  personId: string;
  name: string | null;
  email: string | null;
  phone: string;
  countryName: string;
  circles: CircleResponse[];
  contactItemRoles: ContactItemRoleResponse[];
  connectionChannels: ConnectionChannelResponse[];
  systemStatusTags: SystemStatusTagResponse[];
  userDefinedTags: UserDefinedTagsResponse[];
  interactions: InteractionResponse[];
}

export interface PersonDetail extends PersonView {
  age: number;
  dateOfBirth: string | null;
  gender: string;
  address: string | null;
  countryId: string | null;
  newsLetter: boolean | null;
  contextMemory: string | null;
  profileImagePath: string | null;
  origin: string | null;
  linkedInProfile: string | null;
  otherInformation: string | null;
  organizations: CircleResponse[];
  currentRoles: ContactItemRoleResponse[];
  socialMediaAccounts: SocialMediaAccountResponse[];
  notes: NoteResponse[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  hasMore: boolean;
}

export interface RelationshipHealth {
  personId: string;
  name: string | null;
  tieStrength: number;
  lastContactAtUtc: string | null;
  cadenceReferenceDays: number | null;
  silenceQuantile: number | null;
  urgencyScore: number;
  band: string;
  isBridge: boolean;
  isImportant: boolean;
}

export interface DigestEntry {
  health: RelationshipHealth;
  suggestion: string;
  actionUrl: string;
  remindUrl: string;
}

export interface DigestPayload {
  deliveryId: string;
  weekStartUtc: string;
  entries: DigestEntry[];
  networkHealth: number;
}

export interface DigestPreference {
  applicationUserId: string;
  enabled: boolean;
  threshold: number;
  count: number;
}

export interface NetworkNode {
  personId: string;
  name: string | null;
  degree: number;
  isBridge: boolean;
  isIsolated: boolean;
  urgencyScore: number;
}

export interface NetworkEdge {
  from: string;
  to: string;
  reason: string;
}

export interface NetworkGraph {
  nodes: NetworkNode[];
  edges: NetworkEdge[];
  clusterCount: number;
}

export interface CountryResponse {
  countryId: string;
  countryName: string;
}

export const InteractionTypes = ["Call", "Email", "Meeting", "Message"] as const;
