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
  value: string | null;
}
export interface MemoryEntryResponse {
  memoryEntryId: string;
  personId: string;
  kind: number;
  title: string;
  detail: string | null;
  status: number;
  provenance: number;
  sourceMeetingId: string | null;
  sourceFindingId: string | null;
  sourceExcerpt: string | null;
  sourceMeetingDeleted: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
}
export const MemoryKinds = [
  "Fact",
  "Relationship type",
  "Origin",
  "Shared project",
  "Topic",
  "Commitment",
  "Goal",
  "Intent",
  "Preference",
  "Milestone",
] as const;
export const MemoryProvenanceLabels = ["Your note", "Suggested", "Confirmed", "From meeting"] as const;
export interface PersonEvent {
  eventId: string;
  personId: string;
  personName: string | null;
  type: number;
  title: string;
  occursOn: string;
  repeatsYearly: boolean;
  importance: number;
  notes: string | null;
}
export const EventTypes = [
  "Birthday",
  "Anniversary",
  "Holiday",
  "Job change",
  "Professional milestone",
  "Project milestone",
  "Personal milestone",
  "Custom",
] as const;
export interface EventOccurrence {
  eventId: string;
  personId: string;
  personName: string | null;
  type: number;
  title: string;
  occurrenceDate: string;
  inDays: number;
  importance: number;
}
export interface CopilotCitation {
  kind: string;
  id: string | null;
  label: string;
}
export interface CopilotAnswer {
  text: string;
  citations: CopilotCitation[];
  limitedContext: boolean;
}
export interface ChatTurn {
  role: string;
  text: string;
}
export interface BriefingAttentionItem {
  personId: string;
  name: string;
  reason: string;
  band: string;
}
export interface BriefingEventItem {
  personId: string;
  personName: string | null;
  title: string;
  inDays: number;
  silenceLine: string;
}
export interface BriefingPayload {
  generatedAtUtc: string;
  summary: string;
  attentionNow: BriefingAttentionItem[];
  upcomingEvents: BriefingEventItem[];
  followUpsDue: string[];
  changes: unknown[];
  suggestedActions: string[];
}
export interface AiProviderSettings {
  provider: string;
  model: string;
  baseUrl: string | null;
  hasKey: boolean;
  updatedAtUtc: string | null;
}
export interface MeetingPersonDto {
  meetingPersonId: string;
  detectedName: string;
  mappedPersonId: string | null;
  mappedPersonName: string | null;
  matchStatus: number;
  selectedForLogging: boolean;
}
export interface MeetingFindingDto {
  meetingFindingId: string;
  mappedPersonId: string | null;
  mappedPersonName: string | null;
  kind: number;
  title: string;
  detail: string | null;
  status: number;
  sourceExcerpt: string | null;
  acceptedAsEntryId: string | null;
}
export interface MeetingBriefDto {
  goal: string | null;
  briefJson: string;
  updatedAtUtc: string;
}
export interface MeetingResponse {
  meetingId: string;
  title: string;
  occurredAtUtc: string;
  actualOccurredAtUtc: string | null;
  description: string | null;
  agenda: string | null;
  userInstructions: string | null;
  hasTranscript: boolean;
  hasNotes: boolean;
  processedSummary: string | null;
  status: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  people: MeetingPersonDto[];
  findings: MeetingFindingDto[];
  brief: MeetingBriefDto | null;
}
export const MeetingStatuses = ["Preparation", "Draft", "Processing", "Processed", "Confirmed", "Discarded"] as const;
export const FindingKinds = ["Topic", "Decision", "Commitment", "Action item", "Follow-up", "Question", "Event", "Person fact", "Project", "Date mention"] as const;
export interface ContactChannelRequest {
  name: string;
  value: string | null;
}
export interface SystemStatusTagResponse {
  statusTagId: number;
  name: string;
  description: string | null;
}
export interface OrganizationResponse {
  circleId: string;
  name: string;
  memberCount: number;
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
  interactionCount: number;
  evidenceStatus: string;
  cadenceReferenceDays: number | null;
  silenceQuantile: number | null;
  urgencyScore: number;
  band: string;
  isBridge: boolean;
  isImportant: boolean;
  upcomingEvents: EventOccurrence[];
  hasEventSignal: boolean;
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
  evidenceStatus: string;
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
