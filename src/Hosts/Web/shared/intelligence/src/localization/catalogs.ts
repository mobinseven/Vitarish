export const intelligenceMessageCatalogs = {
  en: {
    intelligence: {
      interactive: {
        submit: "Send answer",
        submitTooltip: "Send the options you chose",
        limit: "Pick up to {count}",
        references: "Related features",
      },
      dictation: {
        language: "Dictation language",
        languages: {
          "en-US": "English",
          "fa-IR": "Persian",
        },
        actions: {
          start: "Start voice dictation",
          stop: "Stop voice dictation",
          connecting: "Connecting voice dictation",
          completing: "Finishing voice dictation",
          unavailable: "Voice dictation unavailable",
          cancelLanguage: "Cancel language selection",
        },
        status: {
          checking: "Checking voice dictation availability...",
          connecting: "Connecting voice dictation...",
          recording: "Recording. Select stop when you are finished.",
          completing: "Finishing the transcription...",
        },
        failures: {
          unavailable: "Voice dictation is currently unavailable.",
          insecureContext: "Voice dictation requires a secure HTTPS connection.",
          browserUnsupported: "This browser does not support voice dictation.",
          microphoneUnavailable: "No available microphone was found.",
          permissionDenied: "Microphone access was not granted.",
          codecUnavailable: "This browser cannot record a supported audio format.",
          networkUnavailable: "The dictation connection was interrupted.",
          providerTimeout: "The transcription provider did not respond in time.",
          invalidAudio: "The recorded audio could not be transcribed.",
          limitReached: "The dictation session reached its limit.",
          canceled: "Voice dictation was canceled.",
          failed: "Voice dictation failed.",
        },
      },
    },
  },
  fa: {
    intelligence: {
      interactive: {
        submit: "ارسال پاسخ",
        submitTooltip: "ارسال گزینه‌هایی که انتخاب کردید",
        limit: "حداکثر {count} مورد",
        references: "امکانات مرتبط",
      },
      dictation: {
        language: "زبان دیکته",
        languages: {
          "en-US": "انگلیسی",
          "fa-IR": "فارسی",
        },
        actions: {
          start: "شروع دیکته صوتی",
          stop: "پایان دیکته صوتی",
          connecting: "در حال اتصال دیکته صوتی",
          completing: "در حال تکمیل دیکته صوتی",
          unavailable: "دیکته صوتی در دسترس نیست",
          cancelLanguage: "لغو انتخاب زبان",
        },
        status: {
          checking: "در حال بررسی دسترسی به دیکته صوتی...",
          connecting: "در حال اتصال دیکته صوتی...",
          recording: "در حال ضبط است. پس از پایان، توقف را انتخاب کنید.",
          completing: "در حال تکمیل رونویسی...",
        },
        failures: {
          unavailable: "دیکته صوتی در حال حاضر در دسترس نیست.",
          insecureContext: "دیکته صوتی به اتصال امن HTTPS نیاز دارد.",
          browserUnsupported: "این مرورگر از دیکته صوتی پشتیبانی نمی‌کند.",
          microphoneUnavailable: "میکروفون قابل استفاده‌ای پیدا نشد.",
          permissionDenied: "اجازه دسترسی به میکروفون داده نشد.",
          codecUnavailable: "این مرورگر نمی‌تواند صدا را با قالب پشتیبانی‌شده ضبط کند.",
          networkUnavailable: "اتصال دیکته قطع شد.",
          providerTimeout: "سرویس رونویسی به‌موقع پاسخ نداد.",
          invalidAudio: "رونویسی صدای ضبط‌شده ممکن نبود.",
          limitReached: "دیکته به محدودیت جلسه رسید.",
          canceled: "دیکته صوتی لغو شد.",
          failed: "دیکته صوتی انجام نشد.",
        },
      },
    },
  },
} as const;
