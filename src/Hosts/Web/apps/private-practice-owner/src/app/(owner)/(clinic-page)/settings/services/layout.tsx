import { ServicesTabs } from "@/components/settings/ServicesTabs";

export default function ServicesLayout({ children }: { children: React.ReactNode }) {
  return <ServicesTabs>{children}</ServicesTabs>;
}
