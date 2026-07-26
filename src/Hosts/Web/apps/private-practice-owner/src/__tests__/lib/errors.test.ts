import { parseAuthError, getErrorMessage } from "@/lib/errors";
import { ApiError } from "@vitarish/web-api-client";

describe("parseAuthError", () => {
  it("should parse ApiError with code", () => {
    const apiError = new ApiError({
      status: 401,
      title: "InvalidCredentials",
      detail: "Invalid username or password",
      code: "invalid_credentials",
    });

    const result = parseAuthError(apiError);

    expect(result.code).toBe("invalid_credentials");
    expect(result.message).toBe("Invalid username or password");
    expect(result.status).toBe(401);
  });

  it("should parse generic Error", () => {
    const error = new Error("Network failure");
    const result = parseAuthError(error);

    expect(result.code).toBe("invalid_input");
    expect(result.message).toBe("Network failure");
    expect(result.status).toBe(500);
  });

  it("should handle unknown error types", () => {
    const result = parseAuthError("string error");

    expect(result.code).toBe("invalid_input");
    expect(result.status).toBe(500);
  });
});

describe("getErrorMessage", () => {
  it("should return user-friendly message for known codes", () => {
    expect(
      getErrorMessage({ code: "invalid_credentials", message: "", status: 401 })
    ).toBe("Invalid username or password");
  });

  it("should return original message for unknown codes", () => {
    expect(
      getErrorMessage({
        code: "unknown_code" as never,
        message: "Something happened",
        status: 500,
      })
    ).toBe("Something happened");
  });
});
