"use client";

import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { SeaProjectForm } from "@/features/sea/projects/components";
import { useLocalizedText } from "@/localization";

export default function SeaProjectCreateRoute() {
  const router = useRouter();
  const { t } = useLocalizedText();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Sea.Projects.Index)}
        >
          {t("sea.projects.backToProjects")}
        </Button>
        <h1 className="text-2xl font-bold">{t("sea.projects.createTitle")}</h1>
      </div>
      <SeaProjectForm
        isOpen={true}
        onClose={() => router.push(Routes.Sea.Projects.Index)}
      />
    </div>
  );
}
