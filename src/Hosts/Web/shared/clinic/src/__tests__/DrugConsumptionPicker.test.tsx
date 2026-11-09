import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import { DrugConsumptionPicker } from "../components/patients/prescriptions/DrugConsumptionPicker";

const options = {
  isSupported: true,
  isSuccess: true,
  units: [
    { id: "24", name: "Each", amounts: [{ id: "580", name: "One quarter" }] },
  ],
  instructions: [{ id: "2546009", name: "Night" }],
  usages: [{ id: "231271000", name: "Intra-articular" }],
  methods: [],
};

function renderPicker(onConfirm = vi.fn(), onCancel = vi.fn()) {
  render(
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      <DrugConsumptionPicker
        drugName="Ibuprofen"
        options={options}
        isBusy={false}
        onConfirm={onConfirm}
        onCancel={onCancel}
      />
    </LocalizationProvider>,
  );
  return { onConfirm, onCancel };
}

describe("DrugConsumptionPicker", () => {
  afterEach(cleanup);

  it("names the drug and blocks adding it until amount, time and route are chosen", () => {
    const { onConfirm } = renderPicker();

    expect(
      screen.getByRole("heading", { name: "Drug usage for Ibuprofen" }),
    ).toBeInTheDocument();
    const add = screen.getByRole("button", { name: "Add drug" });
    expect(add).toBeDisabled();
    fireEvent.click(add);
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it("lets the doctor cancel without adding the drug", () => {
    const { onConfirm, onCancel } = renderPicker();

    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));

    expect(onCancel).toHaveBeenCalledTimes(1);
    expect(onConfirm).not.toHaveBeenCalled();
  });
});
