import { communicationsSubmitContactInquiry } from "./generated/communications/communications";
import type { ContactFormSubmit } from "../hooks/useContactForm";

export const submitContactInquiry: ContactFormSubmit = async (
  values,
  { signal },
) => {
  if (!values.goal || !values.need) {
    throw new Error("Contact inquiry choices are required.");
  }

  await communicationsSubmitContactInquiry(
    {
      name: values.name,
      email: values.email,
      goal: values.goal,
      need: values.need,
      message: values.message,
    },
    undefined,
    signal,
  );
};
