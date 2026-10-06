import type { MissionTimeZone } from './api';

// Values some engines report when the zone is unknown; sending them would only earn a 400.
const UNKNOWN_ZONES = new Set(['Etc/Unknown', 'Unknown']);

/**
 * Device time zone for day-bound features (daily mission). The IANA name can be missing on some
 * Hermes Android builds without full Intl support, so the UTC offset is always sent as a fallback.
 */
export function getDeviceTimeZone(): MissionTimeZone {
  let timeZone: string | undefined;
  try {
    const resolved = Intl.DateTimeFormat().resolvedOptions().timeZone;
    if (typeof resolved === 'string' && resolved.length > 0 && resolved.length <= 64 && !UNKNOWN_ZONES.has(resolved)) timeZone = resolved;
  } catch { timeZone = undefined; }
  return { timeZone, utcOffsetMinutes: -new Date().getTimezoneOffset() };
}
