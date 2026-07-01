"use client";

import { Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";

export function UserReservationsList() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.userAppointments.reservations.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.userAppointments.reservations.description")}</p>
      </div>

      <Card className="glass-card">
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">
            {t("clinic.userAppointments.reservations.comingSoon")}
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}
