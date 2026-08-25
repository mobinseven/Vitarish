"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  medicalBeginProviderAuthorization,
  medicalCompleteProviderAuthorization,
  medicalConfigureProviderCredential,
  medicalGetCredentialSetupGrid,
  medicalGetProviderCredentialSetupContext,
} from "../../../api/generated/medical/medical";

const oauthCredentialKey = "vitarish:tamin-oauth-credential";

export interface TaminCredentialInput {
  credentialId?: string;
  providerDoctorIdentifier: string;
  providerNationalCode: string;
  providerClinicIdentifier: string;
}

export function useInsuranceProviderSetup() {
  const queryClient = useQueryClient();
  const context = useQuery({
    queryKey: ["medical", "provider-credential-context"],
    queryFn: ({ signal }) =>
      medicalGetProviderCredentialSetupContext(undefined, signal),
  });
  const credentials = useQuery({
    queryKey: [
      "medical",
      "provider-credentials",
      context.data?.clinicId,
      "TAMIN",
    ],
    enabled: Boolean(context.data?.clinicId),
    queryFn: ({ signal }) =>
      medicalGetCredentialSetupGrid(
        { clinicId: context.data!.clinicId, providerKey: "TAMIN" },
        undefined,
        signal,
      ),
  });

  const authorize = useMutation({
    mutationFn: async (input: TaminCredentialInput) => {
      if (!context.data?.doctorId || !context.data.clinicId) {
        throw new Error("Credential setup context is unavailable.");
      }

      const saved = await medicalConfigureProviderCredential({
        credentialId: input.credentialId ?? null,
        doctorId: context.data.doctorId,
        clinicId: context.data.clinicId,
        providerKey: "TAMIN",
        configurationValues: {
          providerDoctorIdentifier: input.providerDoctorIdentifier.trim(),
          providerUsername: input.providerNationalCode.trim(),
          providerClinicIdentifier: input.providerClinicIdentifier.trim(),
        },
      });
      if (!saved.accepted || !saved.entityId) {
        throw new Error(
          saved.userSafeMessage ??
            saved.errorMessage ??
            "Credential configuration failed.",
        );
      }

      const authorization = await medicalBeginProviderAuthorization({
        credentialId: saved.entityId,
      });
      if (!authorization.isSuccess || !authorization.authorizationUri) {
        throw new Error(
          authorization.errorMessage ?? "Provider authorization could not start.",
        );
      }

      const authorizationUrl = new URL(authorization.authorizationUri);
      if (authorizationUrl.protocol !== "https:") {
        throw new Error("Provider authorization URL must use HTTPS.");
      }

      sessionStorage.setItem(oauthCredentialKey, saved.entityId);
      window.location.assign(authorizationUrl.toString());
    },
    onSuccess: () =>
      queryClient.invalidateQueries({
        queryKey: ["medical", "provider-credentials"],
      }),
  });

  return { context, credentials, authorize };
}

export function useCompleteInsuranceProviderAuthorization() {
  const complete = useMutation({
    mutationFn: async () => {
      const parameters = new URLSearchParams(window.location.search);
      const code = parameters.get("code");
      const state = parameters.get("state");
      const providerError = parameters.get("error");
      const credentialId = sessionStorage.getItem(oauthCredentialKey);

      if (providerError) {
        throw new Error(providerError);
      }
      if (!credentialId || !code || !state) {
        throw new Error("Provider callback is incomplete or no longer valid.");
      }

      const result = await medicalCompleteProviderAuthorization({
        credentialId,
        authorizationCode: code,
        state,
      });
      if (!result.accepted) {
        throw new Error(
          result.userSafeMessage ??
            result.errorMessage ??
            "Provider authorization failed.",
        );
      }

      sessionStorage.removeItem(oauthCredentialKey);
    },
  });

  return complete;
}
