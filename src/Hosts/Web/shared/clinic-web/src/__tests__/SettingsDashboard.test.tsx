import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import { SettingsDashboard } from "../components/settings/SettingsDashboard";

afterEach(cleanup);

describe("SettingsDashboard", () => {
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
