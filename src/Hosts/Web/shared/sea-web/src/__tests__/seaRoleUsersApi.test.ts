import { beforeEach, describe, expect, it, vi } from "vitest";
import { usersSearchUsers } from "@vitarish/auth-web";
import { searchSeaRoleUsers } from "../features/sea/roles/api/seaRoleUsersApi";

vi.mock("@vitarish/auth-web", () => ({
  usersSearchUsers: vi.fn(),
}));

const mockedUsersSearchUsers = vi.mocked(usersSearchUsers);

describe("SEA role users API", () => {
  beforeEach(() => {
    mockedUsersSearchUsers.mockReset();
    mockedUsersSearchUsers.mockResolvedValue([]);
  });

  it("does not call the users API for empty search text", async () => {
    await expect(searchSeaRoleUsers("  ")).resolves.toEqual([]);

    expect(mockedUsersSearchUsers).not.toHaveBeenCalled();
  });

  it("searches tenant users with a trimmed query", async () => {
    await searchSeaRoleUsers("  Avery  ");

    expect(mockedUsersSearchUsers).toHaveBeenCalledWith({
      Query: "Avery",
      MaxResults: 10,
      IncludeTenantUsers: true,
    });
  });
});
