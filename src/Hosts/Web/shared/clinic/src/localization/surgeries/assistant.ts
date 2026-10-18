/** The assistant's surgery review card. Keys live under clinic.surgeries.assistant. Keep en and fa in the same shape. */
export const surgeryAssistantMessages = {
  en: {
    confirm: "Confirm change",
    action: "Action",
    actions: {
      createType: "Create surgery type",
      editType: "Edit surgery type",
      deleteType: "Delete surgery type",
      editCancellationMessage: "Change cancellation message",
    },
    fields: {
      identification: "Identification",
      defaultTeamRoles: "Default team roles",
      cancellationSubject: "Cancellation subject",
      cancellationMessage: "Cancellation message",
    },
    change: "{before} → {after}",
    none: "None",
    teamMember: "{name} ({role})",
    reminder: "{offset}: {subject}",
    notifiesPatientAndTeam:
      "Confirming sends the patient and the surgical team the clinic's cancellation message.",
  },
  fa: {
    confirm: "تأیید تغییر",
    action: "اقدام",
    actions: {
      createType: "ایجاد نوع جراحی",
      editType: "ویرایش نوع جراحی",
      deleteType: "حذف نوع جراحی",
      editCancellationMessage: "تغییر پیام لغو",
    },
    fields: {
      identification: "شناسه",
      defaultTeamRoles: "نقش‌های پیش‌فرض تیم",
      cancellationSubject: "موضوع پیام لغو",
      cancellationMessage: "متن پیام لغو",
    },
    change: "{before} ← {after}",
    none: "هیچ",
    teamMember: "{name} ({role})",
    reminder: "{offset}: {subject}",
    notifiesPatientAndTeam:
      "با تأیید، پیام لغو کلینیک برای بیمار و تیم جراحی فرستاده می‌شود.",
  },
} as const;
