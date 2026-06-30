import { resolveUserDisplayProfile } from "@/features/auth/session/userDisplayProfile";

describe("resolveUserDisplayProfile", () => {
  it("uses the normalized display name before contact values", () => {
    const profile = resolveUserDisplayProfile({
      userId: "user-1",
      displayName: " Avery Stone ",
      email: "avery@example.com",
      phoneNumber: "+15550100",
    });

    expect(profile).toMatchObject({
      displayLabel: "Avery Stone",
      givenName: "Avery",
      familyName: "Stone",
      initials: "AS",
      contactLabel: "avery@example.com",
    });
  });

  it("uses the email local part when no display name exists", () => {
    const profile = resolveUserDisplayProfile({
      userId: "user-2",
      displayName: " ",
      email: " email-only@example.com ",
      phoneNumber: "+15550101",
    });

    expect(profile.displayLabel).toBe("email-only");
    expect(profile.contactLabel).toBe("email-only@example.com");
    expect(profile.initials).toBe("EO");
  });

  it("uses the phone number when name and email are missing", () => {
    const profile = resolveUserDisplayProfile({
      userId: "user-3",
      displayName: null,
      email: null,
      phoneNumber: " +15550102 ",
    });

    expect(profile.displayLabel).toBe("+15550102");
    expect(profile.contactLabel).toBe("+15550102");
    expect(profile.initials).toBeNull();
  });

  it("uses friend when no user profile data exists", () => {
    const profile = resolveUserDisplayProfile(null);

    expect(profile.displayLabel).toBe("friend");
    expect(profile.contactLabel).toBeNull();
    expect(profile.initials).toBeNull();
  });

  it("resolves initials from separator-delimited email local parts", () => {
    const profile = resolveUserDisplayProfile({
      userId: "user-4",
      displayName: null,
      email: "john.doe@example.com",
      phoneNumber: null,
    });

    expect(profile.displayLabel).toBe("john.doe");
    expect(profile.initials).toBe("JD");
  });
});
