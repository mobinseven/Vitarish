import { setupServer } from "msw/node";
import { handlers } from "./handlers/authentication.msw";

export const server = setupServer(...handlers);
