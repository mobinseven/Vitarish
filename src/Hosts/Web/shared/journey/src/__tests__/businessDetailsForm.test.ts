import { describe, expect, it } from "vitest";
import { DayOfWeek } from "../api/generated/parties/model";
import {
  emptyBusinessDetailsForm,
  toBusinessDetailsDto,
  weekdaysFor,
} from "../features/tenantSetup/businessDetailsForm";

describe("businessDetailsForm", () => {
  it("maps an untouched form to null", () => {
    const form = emptyBusinessDetailsForm("en");

    expect(toBusinessDetailsDto(form)).toBeNull();
  });

  it("drops blank phone and social link rows and trims values", () => {
    const form = {
      ...emptyBusinessDetailsForm("en"),
      phones: ["  021 1234 5678  ", "   ", "0912 000 0000 "],
      website: "  https://rezaei.clinic.test  ",
      socialLinks: ["", " https://instagram.com/rezaeiclinic ", "  "],
    };

    expect(toBusinessDetailsDto(form)).toEqual({
      address: null,
      phones: ["021 1234 5678", "0912 000 0000"],
      website: "https://rezaei.clinic.test",
      socialLinks: ["https://instagram.com/rezaeiclinic"],
      workingHours: [],
      photoFileIds: [],
    });
  });

  it("omits unset days, maps closed days to empty intervals, and keeps open periods", () => {
    const form = {
      ...emptyBusinessDetailsForm("fa"),
      workingDays: [
        {
          day: DayOfWeek.Saturday,
          state: "open" as const,
          periods: [
            { opens: "09:00", closes: "13:00" },
            { opens: "16:00", closes: "20:00" },
          ],
        },
        {
          day: DayOfWeek.Sunday,
          state: "unset" as const,
          periods: [{ opens: "09:00", closes: "17:00" }],
        },
        {
          day: DayOfWeek.Friday,
          state: "closed" as const,
          periods: [{ opens: "09:00", closes: "17:00" }],
        },
      ],
    };

    expect(toBusinessDetailsDto(form)).toEqual({
      address: null,
      phones: [],
      website: null,
      socialLinks: [],
      workingHours: [
        {
          day: DayOfWeek.Saturday,
          intervals: [
            { opens: "09:00", closes: "13:00" },
            { opens: "16:00", closes: "20:00" },
          ],
        },
        {
          day: DayOfWeek.Friday,
          intervals: [],
        },
      ],
      photoFileIds: [],
    });
  });

  it("produces an address with only coordinates when a location alone is provided", () => {
    const form = {
      ...emptyBusinessDetailsForm("en"),
      location: { latitude: 35.6892, longitude: 51.389 },
    };

    expect(toBusinessDetailsDto(form)).toEqual({
      address: {
        province: null,
        city: null,
        street: null,
        postalCode: null,
        latitude: 35.6892,
        longitude: 51.389,
      },
      phones: [],
      website: null,
      socialLinks: [],
      workingHours: [],
      photoFileIds: [],
    });
  });

  it("orders weekdays starting on Saturday for fa-IR and Monday for en", () => {
    const faWeekdays = weekdaysFor("fa-IR");
    const enWeekdays = weekdaysFor("en");

    expect(faWeekdays[0]).toBe(DayOfWeek.Saturday);
    expect(faWeekdays).toHaveLength(7);
    expect(enWeekdays[0]).toBe(DayOfWeek.Monday);
    expect(enWeekdays).toHaveLength(7);
  });
});
