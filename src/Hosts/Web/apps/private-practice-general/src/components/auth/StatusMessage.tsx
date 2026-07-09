import { StatusAlert, type StatusAlertProps } from "@vitarish/web-ui";

export function StatusMessage(props: StatusAlertProps) {
  return <StatusAlert className="mb-4" {...props} />;
}
