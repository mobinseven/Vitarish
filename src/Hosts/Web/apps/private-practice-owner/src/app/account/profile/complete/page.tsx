"use client";

import { useAuthSession } from "@vitarish/auth";
import { ProfileCompletionPage } from "@vitarish/journey";

export default function ProfileCompleteRoute() {
  const { user } = useAuthSession();
  return <ProfileCompletionPage userId={user?.userId} />;
}
