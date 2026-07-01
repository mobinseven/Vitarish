import type { SupportedCulture } from "./cultures";

const enMessages = {
  "App.BrandName": "Vitarish",
  "App.Description": "Vitarish platform authentication",
  "Common.Save": "Save",
  "Common.BackToSettings": "Back to settings",
  "Account.SettingsTitle": "Account Settings",
  "Account.SignedInAs": "Signed in as {name}",
  "Account.Name": "Name",
  "Account.Email": "Email",
  "Account.Phone": "Phone",
  "Account.Password": "Password",
  "Account.Passkeys": "Passkeys",
  "Account.Theme": "Theme",
  "Account.Language": "Language",
  "Culture.Language": "Language",
  "Culture.English": "English",
  "Culture.Persian": "Persian",
  "Culture.UpdateFailed": "Failed to update language",
  "Culture.UpdateSucceeded": "Language updated successfully",
} as const;

export type MessageKey = keyof typeof enMessages;

const faMessages: Record<MessageKey, string> = {
  "App.BrandName": "Vitarish",
  "App.Description": "احراز هویت پلتفرم ویتاریش",
  "Common.Save": "ذخیره",
  "Common.BackToSettings": "بازگشت به تنظیمات",
  "Account.SettingsTitle": "تنظیمات حساب",
  "Account.SignedInAs": "وارد شده با {name}",
  "Account.Name": "نام",
  "Account.Email": "ایمیل",
  "Account.Phone": "تلفن",
  "Account.Password": "رمز عبور",
  "Account.Passkeys": "کلیدهای ورود",
  "Account.Theme": "پوسته",
  "Account.Language": "زبان",
  "Culture.Language": "زبان",
  "Culture.English": "انگلیسی",
  "Culture.Persian": "فارسی",
  "Culture.UpdateFailed": "به‌روزرسانی زبان ناموفق بود",
  "Culture.UpdateSucceeded": "زبان با موفقیت به‌روزرسانی شد",
};

export const messages: Record<SupportedCulture, Record<MessageKey, string>> = {
  "en-US": enMessages,
  "fa-IR": faMessages,
};

export const fallbackMessages = enMessages;
