"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "../types";

interface PatientDetailsProps {
  patient: PatientViewDto;
}

function formatDate(dateString: string | null | undefined): string {
  if (!dateString) return "—";
  return new Date(dateString).toLocaleDateString();
}

function formatEnum(value: string | null | undefined): string {
  if (!value || value === "Unspecified") return "—";
  return value.replace(/([A-Z])/g, " $1").trim();
}

export function PatientDetails({ patient }: PatientDetailsProps) {
  return (
    <div className="flex flex-col gap-4">
      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">Personal Information</h2>
        </Card.Header>
        <Card.Content className="grid gap-3 sm:grid-cols-2">
          <DetailField label="First Name" value={patient.firstName} />
          <DetailField label="Last Name" value={patient.lastName} />
          <DetailField
            label="Date of Birth"
            value={formatDate(patient.dateOfBirth)}
          />
          <DetailField label="Gender" value={formatEnum(patient.gender)} />
          <DetailField label="Place of Birth" value={patient.placeOfBirth} />
          <DetailField label="Father's Name" value={patient.fatherName} />
          <DetailField label="Education" value={formatEnum(patient.education)} />
          <DetailField label="Occupation" value={patient.occupation} />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">Contact Information</h2>
        </Card.Header>
        <Card.Content className="grid gap-3 sm:grid-cols-2">
          <DetailField label="Phone" value={patient.phone} />
          <DetailField label="Email" value={patient.email} />
          <DetailField label="Address" value={patient.address} />
          <DetailField
            label="Emergency Contact"
            value={patient.emergencyContactName}
          />
          <DetailField
            label="Emergency Phone"
            value={patient.emergencyContactPhone}
          />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">Medical Information</h2>
        </Card.Header>
        <Card.Content className="grid gap-3 sm:grid-cols-2">
          <DetailField
            label="Date of Surgery"
            value={formatDate(patient.dateOfSurgery)}
          />
          <DetailField label="Height (cm)" value={patient.height} />
          <DetailField label="Weight (kg)" value={patient.weight} />
          <DetailField label="Allergies" value={patient.allergies} />
          <DetailField
            label="Current Medications"
            value={patient.currentMedications}
          />
          <DetailField label="Clinical Notes" value={patient.clinicalNotes} />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">Insurance & Other</h2>
        </Card.Header>
        <Card.Content className="grid gap-3 sm:grid-cols-2">
          <DetailField
            label="Insurance Provider"
            value={patient.insuranceProvider}
          />
          <DetailField label="Referrer" value={patient.referrer} />
          <DetailField label="Visit Reason" value={patient.visitReason} />
          <DetailField
            label="Identification Code"
            value={patient.identificationCode}
          />
        </Card.Content>
      </Card>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">System</h2>
        </Card.Header>
        <Card.Content className="grid gap-3 sm:grid-cols-2">
          <DetailField label="Patient ID" value={patient.id} />
          <DetailField label="Template" value={patient.template ? "Yes" : "No"} />
          <DetailField label="Created" value={formatDate(patient.createdAt)} />
          <DetailField label="Updated" value={formatDate(patient.updatedAt)} />
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({
  label,
  value,
}: {
  label: string;
  value: string | number | boolean | null | undefined;
}) {
  const displayValue =
    value === null || value === undefined || value === "" ? "—" : String(value);

  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm text-default-500">{label}</span>
      <span className="text-sm">{displayValue}</span>
    </div>
  );
}
