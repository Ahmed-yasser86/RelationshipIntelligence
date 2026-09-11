// Backend DTO mirrors. Property names match the API's JSON exactly (PascalCase).

export interface AuthResponse {
  token: string;
  personeName: string;
  personeEmail: string;
  refreshToken: string;
  RefreshTokenExpirationTime: string;
  ExpirationTime: string;
}

export interface CircleResponse {
  CircleId: string;
  Name: string;
}
export interface ContactItemRoleResponse {
  ContactsRoleId: string;
  Role: string;
}
export interface ConnectionChannelResponse {
  ConnectionChannelId: string;
  ConnectionChannelName: string;
}
export interface SystemStatusTagResponse {
  StatusTagId: number;
  Name: string;
  Description: string | null;
}
export interface UserDefinedTagsResponse {
  TagId: string;
  TagName: string;
}
export interface SocialMediaAccountResponse {
  SocialMediaAccountId: string;
  Platform: string | null;
  Url: string;
}
export interface NoteResponse {
  NoteId: string;
  NoteType: number;
  Content: string;
}
export interface InteractionResponse {
  InteractionId: string;
  PersonId: string;
  InteractionType: number;
  InteractionTitle: string;
  InteractionDescription: string | null;
  TimeOfInteraction: string;
}

export interface PersonView {
  PersonId: string;
  Name: string;
  email: string | null;
  phone: string;
  CountryName: string;
  Circles: CircleResponse[];
  ContactItemRoles: ContactItemRoleResponse[];
  ConnectionChannels: ConnectionChannelResponse[];
  SystemStatusTags: SystemStatusTagResponse[];
  UserDefinedTags: UserDefinedTagsResponse[];
  Interactions: InteractionResponse[];
}

export interface PersonDetail extends PersonView {
  Age: number;
  DateOfBirth: string | null;
  Gender: string;
  Address: string | null;
  CountryId: string | null;
  NewsLetter: boolean | null;
  ContextMemory: string | null;
  ProfileImagePath: string | null;
  Origin: string | null;
  LinkedInProfile: string | null;
  OtherInformation: string | null;
  Organizations: CircleResponse[];
  CurrentRoles: ContactItemRoleResponse[];
  SocialMediaAccounts: SocialMediaAccountResponse[];
  Notes: NoteResponse[];
}

export interface PagedResult<T> {
  Items: T[];
  TotalCount: number;
  PageNumber: number;
  PageSize: number;
  HasMore: boolean;
}

export interface RelationshipHealth {
  PersonId: string;
  Name: string;
  TieStrength: number;
  LastContactAtUtc: string | null;
  CadenceReferenceDays: number | null;
  UrgencyScore: number;
  Band: string;
  IsBridge: boolean;
  IsImportant: boolean;
}

export interface DigestEntry {
  Health: RelationshipHealth;
  Suggestion: string;
  ActionUrl: string;
  RemindUrl: string;
}

export interface DigestPayload {
  DeliveryId: string;
  WeekStartUtc: string;
  Entries: DigestEntry[];
  NetworkHealth: number;
}

export interface DigestPreference {
  ApplicationUserId: string;
  Enabled: boolean;
  Threshold: number;
  Count: number;
}

export interface NetworkNode {
  PersonId: string;
  Name: string;
  Degree: number;
  IsBridge: boolean;
  IsIsolated: boolean;
  UrgencyScore: number;
}

export interface NetworkEdge {
  From: string;
  To: string;
  Reason: string;
}

export interface NetworkGraph {
  Nodes: NetworkNode[];
  Edges: NetworkEdge[];
  ClusterCount: number;
}

export interface CountryResponse {
  CountryId: string;
  CountryName: string;
}

export const InteractionTypes = ["Call", "Email", "Meeting", "Message"] as const;
