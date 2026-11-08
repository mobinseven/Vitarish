import { notFound } from "next/navigation";

/** Routes every unknown clinic URL to the in-shell not-found page instead of the bare framework 404. */
export default function UnmatchedClinicRoute(): never {
  notFound();
}
