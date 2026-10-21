/** The surgery detail page and its complete/cancel dialogs. Keys live under clinic.surgeries.detail. Keep en and fa in the same shape. */
export const surgeryDetailMessages = {
  en: {
    loading: "Loading the surgery…",
    openPatient: "Open patient",
    backToSurgeries: "Back to surgeries",
    notFoundTitle: "Surgery not found",
    completeUnavailable:
      "Completion becomes available once the scheduled start time has passed.",
    sections: {
      schedule: "Schedule",
      team: "Surgical team",
      tools: "Tools and instruments",
      reminders: "Reminders",
    },
    schedule: {
      expectedEnd: "Expected end",
      notSet: "Not set",
      unknownType: "Unknown type",
    },
    team: {
      empty: "No team members have been added.",
    },
    tools: {
      empty: "No tools have been listed.",
    },
    reminders: {
      empty: "No reminders are set.",
      atStart: "At the scheduled time",
      daysBefore: "{count} days before",
      hoursBefore: "{count} hours before",
      minutesBefore: "{count} minutes before",
      automaticNote:
        "Reminders go automatically to the patient and every team member.",
      settled:
        "No further reminders are sent for a completed or cancelled surgery.",
    },
    dialogs: {
      completeTitle: "Mark this surgery as completed",
      completeMessage:
        "The surgery is marked as done and its pending reminders stop. A completed surgery can no longer be cancelled.",
      completeConfirm: "Mark completed",
      cancelTitle: "Cancel this surgery",
      cancelMessage:
        "The patient and the team are notified that the surgery is cancelled, and pending reminders stop.",
      cancelConfirm: "Yes, cancel surgery",
      conflict:
        "This surgery changed elsewhere. Review the latest version and try again.",
    },
  },
  fa: {
    loading: "در حال بارگذاری جراحی…",
    openPatient: "بازکردن بیمار",
    backToSurgeries: "بازگشت به جراحی‌ها",
    notFoundTitle: "جراحی یافت نشد",
    completeUnavailable:
      "ثبت انجام جراحی پس از گذشت زمان شروع امکان‌پذیر می‌شود.",
    sections: {
      schedule: "برنامه",
      team: "تیم جراحی",
      tools: "ابزارها و تجهیزات",
      reminders: "یادآوری‌ها",
    },
    schedule: {
      expectedEnd: "پایان مورد انتظار",
      notSet: "تعیین نشده",
      unknownType: "نوع نامشخص",
    },
    team: {
      empty: "هیچ عضوی به تیم اضافه نشده است.",
    },
    tools: {
      empty: "هیچ ابزاری فهرست نشده است.",
    },
    reminders: {
      empty: "هیچ یادآوری‌ای تنظیم نشده است.",
      atStart: "در زمان مقرر",
      daysBefore: "{count} روز قبل",
      hoursBefore: "{count} ساعت قبل",
      minutesBefore: "{count} دقیقه قبل",
      automaticNote:
        "یادآوری‌ها به‌طور خودکار برای بیمار و همه اعضای تیم ارسال می‌شود.",
      settled:
        "برای جراحی انجام‌شده یا لغوشده یادآوری دیگری ارسال نمی‌شود.",
    },
    dialogs: {
      completeTitle: "ثبت انجام این جراحی",
      completeMessage:
        "جراحی به‌عنوان انجام‌شده ثبت می‌شود و یادآوری‌های در انتظار متوقف می‌شوند. جراحی انجام‌شده دیگر لغو نمی‌شود.",
      completeConfirm: "ثبت انجام",
      cancelTitle: "لغو این جراحی",
      cancelMessage:
        "به بیمار و تیم اطلاع داده می‌شود که جراحی لغو شده است و یادآوری‌های در انتظار متوقف می‌شوند.",
      cancelConfirm: "بله، لغو شود",
      conflict:
        "این جراحی جای دیگری تغییر کرده است. آخرین نسخه را بررسی کنید و دوباره تلاش کنید.",
    },
  },
} as const;
