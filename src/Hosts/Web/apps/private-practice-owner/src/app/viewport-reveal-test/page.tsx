import { notFound } from "next/navigation";
import { ViewportRevealTestClient } from "./ViewportRevealTestClient";

export default function ViewportRevealTestPage() {
  if (process.env.VITARISH_LAYOUT_TEST_FIXTURES !== "1") {
    notFound();
  }

  return <ViewportRevealTestClient />;
}
