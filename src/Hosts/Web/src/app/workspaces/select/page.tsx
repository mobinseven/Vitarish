"use client";

import { useRouter } from "next/navigation";
import { useCurrentJourney, resolvePostLoginNavigation } from "@/hooks/journey";

export default function WorkspaceSelectPage() {
  const router = useRouter();
  const { data: decision } = useCurrentJourney();

  const handleContinue = () => {
    const destination = resolvePostLoginNavigation(decision);
    router.push(destination);
  };

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="w-full max-w-md p-6 bg-white rounded-lg shadow-md">
        <h1 className="text-2xl font-bold mb-4">Select Workspace</h1>
        <p className="text-gray-600 mb-6">
          Please select a workspace or app to continue.
        </p>

        <div className="space-y-4">
          <button
            onClick={handleContinue}
            className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
          >
            Continue
          </button>
        </div>
      </div>
    </div>
  );
}
