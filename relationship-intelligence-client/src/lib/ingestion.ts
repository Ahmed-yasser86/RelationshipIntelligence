import { api } from "@/lib/api";
import { IngestionSourceTypes } from "@/lib/types";
import type { IngestionBatch } from "@/lib/types";

/** Source types match the server IngestionSourceType enum order. */
export const IngestionSources = {
  PersonText: 0,
  ConversationUpdate: 1,
  GroupText: 2,
  MeetingText: 3,
} as const;

export function sourceLabel(sourceType: number): string {
  return IngestionSourceTypes[sourceType] ?? `Source ${sourceType}`;
}

export function sourceHint(sourceType: number): string {
  switch (sourceType) {
    case IngestionSources.PersonText:
      return "Profile, bio, signature, or notes about one person.";
    case IngestionSources.ConversationUpdate:
      return "A conversation or message thread with new relationship signals.";
    case IngestionSources.GroupText:
      return "Group chat or multi-person notes — findings are separated per person.";
    case IngestionSources.MeetingText:
      return "Actual meeting evidence (transcript / notes). Planned content never enters here.";
    default:
      return "Raw relationship information.";
  }
}

/**
 * Submit raw text, then run extraction. Returns the processed batch.
 * NOTE: every POST here must carry a JSON body (even `{}`): the API
 * has a global Consumes("application/json") filter, so a bodyless POST is
 * rejected with 415 before it ever reaches the controller.
 */
const EMPTY_JSON = {};

export async function submitAndProcess(
  sourceType: number,
  rawText: string,
  sourceMeetingId?: string | null,
): Promise<IngestionBatch> {
  const batch = await api.post<IngestionBatch>("/api/Ingestion/PostSubmit", {
    SourceType: sourceType,
    RawText: rawText,
    SourceMeetingId: sourceMeetingId ?? null,
  });
  return api.post<IngestionBatch>(
    `/api/Ingestion/PostProcess?id=${batch.ingestionBatchId}`,
    EMPTY_JSON,
  );
}

/** Submit a meeting's actual content server-side (transcript + notes only). */
export async function submitMeetingForReview(meetingId: string): Promise<IngestionBatch> {
  const batch = await api.post<IngestionBatch>(
    `/api/Ingestion/PostSubmitForMeeting?meetingId=${meetingId}`,
    EMPTY_JSON,
  );
  return api.post<IngestionBatch>(
    `/api/Ingestion/PostProcess?id=${batch.ingestionBatchId}`,
    EMPTY_JSON,
  );
}
