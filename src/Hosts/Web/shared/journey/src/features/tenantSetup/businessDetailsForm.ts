import { DayOfWeek, type TenantBusinessDetailsDto, type TenantWorkingDayDto } from "../../api/generated/parties/model";

/** Whether a weekday's hours are unknown (not shared), closed, or open in the listed periods. */
export type WorkingDayState = "unset" | "closed" | "open";

export interface OpeningPeriodForm {
  opens: string;
  closes: string;
}

export interface WorkingDayForm {
  day: DayOfWeek;
  state: WorkingDayState;
  periods: OpeningPeriodForm[];
}

export interface BusinessLocation {
  latitude: number;
  longitude: number;
}

/** The editable business details; the server owns their rules and normalization. */
export interface BusinessDetailsForm {
  province: string;
  city: string;
  street: string;
  postalCode: string;
  location: BusinessLocation | null;
  phones: string[];
  website: string;
  socialLinks: string[];
  workingDays: WorkingDayForm[];
}

/** Mirrors the server's per-day limit, so the editor stops offering more periods. */
export const maximumPeriodsPerDay = 3;
/** Mirrors the server's list limits, so the editor stops offering more rows. */
export const maximumPhones = 5;
export const maximumSocialLinks = 5;

/** Where the map opens before a pin exists: Tehran. */
export const defaultBusinessLocation: BusinessLocation = { latitude: 35.689, longitude: 51.389 };

export const defaultOpeningPeriod: OpeningPeriodForm = { opens: "09:00", closes: "17:00" };

const saturdayFirstWeek: DayOfWeek[] = [
  DayOfWeek.Saturday,
  DayOfWeek.Sunday,
  DayOfWeek.Monday,
  DayOfWeek.Tuesday,
  DayOfWeek.Wednesday,
  DayOfWeek.Thursday,
  DayOfWeek.Friday,
];

const mondayFirstWeek: DayOfWeek[] = [
  DayOfWeek.Monday,
  DayOfWeek.Tuesday,
  DayOfWeek.Wednesday,
  DayOfWeek.Thursday,
  DayOfWeek.Friday,
  DayOfWeek.Saturday,
  DayOfWeek.Sunday,
];

/** Weekdays in the order the locale's calendar shows them. */
export function weekdaysFor(locale: string): DayOfWeek[] {
  return locale.startsWith("fa") ? saturdayFirstWeek : mondayFirstWeek;
}

export function emptyBusinessDetailsForm(locale: string): BusinessDetailsForm {
  return {
    province: "",
    city: "",
    street: "",
    postalCode: "",
    location: null,
    phones: [""],
    website: "",
    socialLinks: [""],
    workingDays: weekdaysFor(locale).map((day) => ({ day, state: "unset", periods: [{ ...defaultOpeningPeriod }] })),
  };
}

function trimmedOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed === "" ? null : trimmed;
}

function nonBlank(values: string[]): string[] {
  return values.map((value) => value.trim()).filter((value) => value !== "");
}

/**
 * The details as the API takes them, or null when the owner shared nothing, so an untouched
 * section never creates an empty listing.
 */
export function toBusinessDetailsDto(form: BusinessDetailsForm): TenantBusinessDetailsDto | null {
  const province = trimmedOrNull(form.province);
  const city = trimmedOrNull(form.city);
  const street = trimmedOrNull(form.street);
  const postalCode = trimmedOrNull(form.postalCode);
  const hasAddress = province !== null || city !== null || street !== null || postalCode !== null || form.location !== null;
  const phones = nonBlank(form.phones);
  const website = trimmedOrNull(form.website);
  const socialLinks = nonBlank(form.socialLinks);
  const workingHours: TenantWorkingDayDto[] = form.workingDays
    .filter((day) => day.state !== "unset")
    .map((day) => ({ day: day.day, intervals: day.state === "closed" ? [] : day.periods.map((period) => ({ ...period })) }));

  if (!hasAddress && phones.length === 0 && website === null && socialLinks.length === 0 && workingHours.length === 0) {
    return null;
  }

  return {
    address: hasAddress
      ? {
          province,
          city,
          street,
          postalCode,
          latitude: form.location?.latitude ?? null,
          longitude: form.location?.longitude ?? null,
        }
      : null,
    phones,
    website,
    socialLinks,
    workingHours,
    photoFileIds: [],
  };
}
