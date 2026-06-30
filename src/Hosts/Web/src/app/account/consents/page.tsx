"use client";

import { useRouter } from "next/navigation";
import { useCurrentJourney, resolvePostLoginNavigation } from "@/hooks/journey";

export default function ConsentsPage() {
  const router = useRouter();
  const { data: decision } = useCurrentJourney();

  const handleAccept = () => {
    const destination = resolvePostLoginNavigation(decision);
    router.push(destination);
  };

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="w-full max-w-md p-6 bg-white rounded-lg shadow-md">
        <h1 className="text-2xl font-bold mb-4">Required Consents</h1>
        <p className="text-gray-600 mb-6">
          Please review and accept the required terms to continue.
        </p>

        <div className="space-y-4">
          <div className="p-4 border border-gray-200 rounded-md">
            <p className="text-sm text-gray-700">
              By continuing, you agree to our Terms of Service and Privacy
              Policy.
            </p>
          </div>

          <button
            onClick={handleAccept}
            className="w-full flex justify-center py-2 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
          >
            Accept and Continue
          </button>
        </div>
      </div>
    </div>
  );
}
