import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import { SettingsDashboard } from "../components/settings/SettingsDashboard";

afterEach(cleanup);

describe("SettingsDashboard", () => {
  it("emits the security-audit review intent", () => {
    const onNavigate = vi.fn();
    render(
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
        <SettingsDashboard onNavigate={onNavigate} />
      </LocalizationProvider>,
    );

    const audit = screen.getByRole("button", { name: /Security audit/ });
    expect(audit).toHaveAttribute("title", "Security audit");
    expect(audit.querySelector(".fa-shield-halved")).toBeInTheDocument();
    fireEvent.click(audit);
    expect(onNavigate).toHaveBeenCalledWith({ kind: "clinicSettingsAudit" });
  });

  it("emits the insurance-provider settings intent", () => {
    const onNavigate = vi.fn();
    render(
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
        <SettingsDashboard onNavigate={onNavigate} />
      </LocalizationProvider>,
    );

    const insurance = screen.getByRole("button", {
      name: /Insurance providers/,
    });
    expect(insurance).toHaveAttribute("title", "Insurance providers");
    expect(insurance.querySelector(".fa-shield-check")).toBeInTheDocument();
    fireEvent.click(insurance);
    expect(onNavigate).toHaveBeenCalledWith({
      kind: "clinicSettingsInsurance",
    });
  });

  it("emits the procedure-template management intent", () => {
    const onNavigate = vi.fn();
    render(
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
        <SettingsDashboard onNavigate={onNavigate} />
      </LocalizationProvider>,
    );

    const procedureTemplates = screen.getByRole("button", {
      name: /Procedure Templates/,
    });
    expect(procedureTemplates).toHaveAttribute("title", "Procedure Templates");
    expect(
      procedureTemplates.querySelector(".fa-folder-tree"),
    ).toBeInTheDocument();
    fireEvent.click(procedureTemplates);
    expect(onNavigate).toHaveBeenCalledWith({
      kind: "procedureTemplateManagement",
    });
  });

  it("emits the prescription-template management intent", () => {
    const onNavigate = vi.fn();
    render(
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
        <SettingsDashboard onNavigate={onNavigate} />
      </LocalizationProvider>,
    );

    const prescriptionTemplates = screen.getByRole("button", {
      name: /Prescription templates/,
    });
    expect(prescriptionTemplates).toHaveAttribute(
      "title",
      "Prescription templates",
    );
    expect(
      prescriptionTemplates.querySelector(".fa-prescription"),
    ).toBeInTheDocument();
    fireEvent.click(prescriptionTemplates);
    expect(onNavigate).toHaveBeenCalledWith({
      kind: "prescriptionTemplateManagement",
    });
  });

  it.each([
    [
      "en",
      "Procedure Templates",
      "Create and manage reusable procedure routines and ordered steps.",
    ],
    [
      "fa",
      "قالب‌های روند",
      "ایجاد و مدیریت روال‌ها و مراحل مرتب‌شده قابل استفاده مجدد.",
    ],
  ] as const)(
    "resolves procedure-template copy for %s",
    (locale, title, description) => {
      render(
        <LocalizationProvider
          locale={locale}
          messages={clinicMessageCatalogs[locale]}
        >
          <SettingsDashboard />
        </LocalizationProvider>,
      );

      expect(screen.getByText(title)).toBeInTheDocument();
      expect(screen.getByText(description)).toBeInTheDocument();
    },
  );
});
