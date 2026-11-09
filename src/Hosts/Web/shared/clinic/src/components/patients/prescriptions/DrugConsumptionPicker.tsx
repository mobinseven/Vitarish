"use client";

import { useState } from "react";
import type { Key } from "@heroui/react";
import {
  Button,
  Label,
  ListBox,
  Select,
  Surface,
  Tooltip,
} from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import type {
  DrugConsumptionOptionResponse,
  DrugConsumptionOptionsResponse,
} from "../../../api/generated/medical/model";
import { useLocalizedText } from "../../../localization";
import {
  buildDrugConsumptionMetadata,
  type DrugConsumptionSelection,
} from "../../../models/prescriptions/drugConsumption";

interface Props {
  drugName: string;
  options: DrugConsumptionOptionsResponse;
  isBusy: boolean;
  onConfirm: (metadataJson: string, summary: string) => void;
  onCancel: () => void;
}

interface OptionSelectProps {
  label: string;
  options: DrugConsumptionOptionResponse[];
  value: string;
  isRequired?: boolean;
  onChange: (value: string) => void;
}

function OptionSelect({
  label,
  options,
  value,
  isRequired,
  onChange,
}: OptionSelectProps) {
  return (
    <Select
      isRequired={isRequired}
      value={value || null}
      onChange={(next: Key | null) => onChange(next ? String(next) : "")}
    >
      <Label>{label}</Label>
      <Select.Trigger>
        <Select.Value />
        <Select.Indicator />
      </Select.Trigger>
      <Select.Popover>
        <ListBox>
          {options
            .filter(
              (
                option,
              ): option is DrugConsumptionOptionResponse & { id: string } =>
                Boolean(option.id),
            )
            .map((option) => (
              <ListBox.Item
                key={option.id}
                id={option.id}
                textValue={option.name ?? option.id}
              >
                {option.name ?? option.id}
                <ListBox.ItemIndicator />
              </ListBox.Item>
            ))}
        </ListBox>
      </Select.Popover>
    </Select>
  );
}

export function DrugConsumptionPicker({
  drugName,
  options,
  isBusy,
  onConfirm,
  onCancel,
}: Props) {
  const { t } = useLocalizedText();
  const units = options.units ?? [];
  const instructions = options.instructions ?? [];
  const usages = options.usages ?? [];
  const methods = options.methods ?? [];
  const [unitId, setUnitId] = useState(
    units.length === 1 ? (units[0].id ?? "") : "",
  );
  const [selection, setSelection] = useState<Partial<DrugConsumptionSelection>>(
    {},
  );
  const amounts = units.find((unit) => unit.id === unitId)?.amounts ?? [];
  const complete =
    Boolean(selection.amountId) &&
    Boolean(selection.instructionId) &&
    (usages.length === 0 || Boolean(selection.routeCode));

  function confirm() {
    if (!selection.amountId || !selection.instructionId) return;
    const amount = amounts.find((item) => item.id === selection.amountId);
    const instruction = instructions.find(
      (item) => item.id === selection.instructionId,
    );
    onConfirm(
      buildDrugConsumptionMetadata({
        amountId: selection.amountId,
        instructionId: selection.instructionId,
        routeCode: selection.routeCode,
        methodId: selection.methodId,
      }),
      t("clinic.prescriptionSubmission.consumption.summary", {
        amount: amount?.name ?? selection.amountId,
        instruction: instruction?.name ?? selection.instructionId,
      }),
    );
  }

  return (
    <Surface
      variant="secondary"
      className="mt-3 flex flex-col gap-4 rounded-2xl p-4"
    >
      <h3 className="text-base font-semibold text-foreground">
        {t("clinic.prescriptionSubmission.consumption.title", {
          drug: drugName,
        })}
      </h3>
      <div className="grid gap-4 sm:grid-cols-2">
        {units.length > 1 ? (
          <OptionSelect
            isRequired
            label={t("clinic.prescriptionSubmission.consumption.unit")}
            options={units}
            value={unitId}
            onChange={(next) => {
              setUnitId(next);
              setSelection((current) => ({ ...current, amountId: undefined }));
            }}
          />
        ) : null}
        <OptionSelect
          isRequired
          label={t("clinic.prescriptionSubmission.consumption.amount")}
          options={amounts}
          value={selection.amountId ?? ""}
          onChange={(next) =>
            setSelection((current) => ({ ...current, amountId: next }))
          }
        />
        <OptionSelect
          isRequired
          label={t("clinic.prescriptionSubmission.consumption.instruction")}
          options={instructions}
          value={selection.instructionId ?? ""}
          onChange={(next) =>
            setSelection((current) => ({ ...current, instructionId: next }))
          }
        />
        {usages.length ? (
          <OptionSelect
            isRequired
            label={t("clinic.prescriptionSubmission.consumption.usage")}
            options={usages}
            value={selection.routeCode ?? ""}
            onChange={(next) =>
              setSelection((current) => ({ ...current, routeCode: next }))
            }
          />
        ) : null}
        {methods.length ? (
          <OptionSelect
            label={t("clinic.prescriptionSubmission.consumption.method")}
            options={methods}
            value={selection.methodId ?? ""}
            onChange={(next) =>
              setSelection((current) => ({ ...current, methodId: next }))
            }
          />
        ) : null}
      </div>
      <div className="flex justify-end gap-2">
        <Tooltip>
          <Button
            isIconOnly
            variant="tertiary"
            aria-label={t("clinic.prescriptionSubmission.consumption.cancel")}
            isDisabled={isBusy}
            onPress={onCancel}
          >
            <FaIcon icon="xmark" aria-hidden="true" />
          </Button>
          <Tooltip.Content>
            {t("clinic.prescriptionSubmission.consumption.cancel")}
          </Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button
            isIconOnly
            variant="primary"
            aria-label={t("clinic.prescriptionSubmission.consumption.add")}
            isDisabled={!complete || isBusy}
            isPending={isBusy}
            onPress={confirm}
          >
            <FaIcon icon="plus" aria-hidden="true" />
          </Button>
          <Tooltip.Content>
            {t("clinic.prescriptionSubmission.consumption.add")}
          </Tooltip.Content>
        </Tooltip>
      </div>
    </Surface>
  );
}
