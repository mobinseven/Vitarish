export const communicationsMessageCatalogs = {
  en: {
    communications: {
      contactForm: {
        label: "Contact form",
        fields: {
          name: "Name",
          email: "Email",
          message: "Message",
        },
        validation: {
          required: "This field is required.",
          email: "Enter a valid email address.",
        },
        actions: {
          submit: "Send message",
          submitting: "Sending message",
          submitHint: "Send this request to Vitarish.",
          pendingHint: "Please wait while your message is sent.",
        },
        success: {
          title: "Message sent",
          description: "Thank you. Your message has been sent.",
        },
        failure: {
          title: "Message not sent",
          description: "We could not send your message. Please try again.",
        },
      },
    },
  },
  fa: {
    communications: {
      contactForm: {
        label: "فرم تماس",
        fields: {
          name: "نام",
          email: "ایمیل",
          message: "پیام",
        },
        validation: {
          required: "تکمیل این فیلد الزامی است.",
          email: "یک نشانی ایمیل معتبر وارد کنید.",
        },
        actions: {
          submit: "ارسال پیام",
          submitting: "در حال ارسال پیام",
          submitHint: "این درخواست را برای ویتاریش ارسال کنید.",
          pendingHint: "لطفاً تا ارسال پیام منتظر بمانید.",
        },
        success: {
          title: "پیام ارسال شد",
          description: "سپاسگزاریم. پیام شما ارسال شد.",
        },
        failure: {
          title: "پیام ارسال نشد",
          description: "ارسال پیام ممکن نبود. لطفاً دوباره تلاش کنید.",
        },
      },
    },
  },
} as const;
