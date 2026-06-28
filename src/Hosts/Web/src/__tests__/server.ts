import { setupServer } from "msw/node";
import { handlers } from "@/api/generated/authentication/authentication.msw";

export const server = setupServer(...handlers);
